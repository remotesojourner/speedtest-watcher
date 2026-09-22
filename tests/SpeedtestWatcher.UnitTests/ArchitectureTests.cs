using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Settings;

namespace SpeedtestWatcher.UnitTests;

public class ArchitectureTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const string StorageNamespace = "SpeedtestWatcher.Application.Storage";

    private static readonly Assembly ApplicationLayer = typeof(OperationResult).Assembly;
    private static readonly Assembly Web = typeof(Program).Assembly;
    private static readonly Assembly UnitTests = typeof(ArchitectureTests).Assembly;
    private static readonly Type[] InputOutput = [typeof(HttpClient), typeof(IHttpClientFactory), typeof(Process)];
    private static readonly Type[] RealParts = [typeof(DbContext), typeof(DbConnection), typeof(IHost), typeof(IHostBuilder), typeof(IHostApplicationBuilder)];

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    [Fact]
    public void TheWebsiteReferencesOnlyApplication_AndApplicationKnowsNothingOfTheWeb()
    {
        Assert.Equal(["SpeedtestWatcher.Application"], ProjectReferences(Web));
        Assert.Empty(ProjectReferences(ApplicationLayer));
        Assert.DoesNotContain(ApplicationLayer.GetReferencedAssemblies(), reference => reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Controllers_UseApplicationServices_NotRepositories()
    {
        var offending = Web.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(controller => controller.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Where(parameter => IsRepository(parameter.ParameterType))
                .Select(parameter => $"{controller.Name} takes {parameter.ParameterType.Name}"))
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Components_InjectServices_NotRepositoriesOrHttpClients()
    {
        var offending = Web.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(ComponentBase)))
            .SelectMany(component => component.GetProperties(Declared)
                .Where(property => property.IsDefined(typeof(InjectAttribute)))
                .Where(property => IsRepository(property.PropertyType) || property.PropertyType == typeof(HttpClient))
                .Select(property => $"{component.Name} injects {property.PropertyType.Name}"))
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void InApplication_OnlyRepositoriesAndStorage_UseTheDbContext()
    {
        var offending = ApplicationLayer.GetTypes()
            .Select(type => (Type: type, Owner: Outermost(type)))
            .Where(entry => !IsRepositoryImplementation(entry.Owner) && !InStorage(entry.Owner) && !IsRegistration(entry.Owner))
            .Where(entry => TypesUsedBy(entry.Type).Any(used => used.IsAssignableTo(typeof(DbContext))))
            .Select(entry => entry.Owner.FullName)
            .Distinct()
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void ApplicationTypesThatTalkToTheOutsideWorld_AreInternal()
    {
        var offending = ApplicationLayer.GetTypes()
            .Select(type => (Type: type, Owner: Outermost(type)))
            .Where(entry => entry.Owner.IsPublic && !IsRegistration(entry.Owner))
            .Where(entry => TypesUsedBy(entry.Type).Any(used => InputOutput.Contains(used)))
            .Select(entry => entry.Owner.FullName)
            .Distinct()
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void UnitTests_OpenNoDatabase_AndStartNoHost()
    {
        var offending = UnitTests.GetTypes()
            .Select(type => (Type: type, Owner: Outermost(type)))
            .Where(entry => entry.Owner != typeof(ArchitectureTests))
            .Where(entry => TypesUsedBy(entry.Type).Any(used => RealParts.Any(used.IsAssignableTo)))
            .Select(entry => entry.Owner.FullName)
            .Distinct()
            .ToList();

        Assert.Empty(offending);
    }

    private static List<string> ProjectReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith("SpeedtestWatcher.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

    private static bool IsRepository(Type type) =>
        type.Assembly == ApplicationLayer && (type.Name.EndsWith("Repository", StringComparison.Ordinal) || type == typeof(ISettingsStore));

    private static bool IsRepositoryImplementation(Type type) => type.GetInterfaces().Any(IsRepository);

    private static bool InStorage(Type type) =>
        type.Namespace is { } name && (name == StorageNamespace || name.StartsWith(StorageNamespace + ".", StringComparison.Ordinal));

    private static bool IsRegistration(Type type) => type.Name.EndsWith("ServiceCollectionExtensions", StringComparison.Ordinal);

    private static Type Outermost(Type type)
    {
        var current = type;
        while (current.DeclaringType != null) current = current.DeclaringType;
        return current;
    }

    private static IEnumerable<Type> TypesUsedBy(Type type)
    {
        var signatures = type.GetFields(Declared).Select(field => field.FieldType)
            .Concat(type.GetProperties(Declared).Select(property => property.PropertyType));

        var methods = type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared));
        var fromMethods = methods.SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)
            .Concat(method is MethodInfo info ? [info.ReturnType] : [])
            .Concat(TypesInBody(method)));

        return signatures.Concat(fromMethods).SelectMany(Expand);
    }

    private static IEnumerable<Type> TypesInBody(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null) yield break;

        var typeArguments = method.DeclaringType is { IsGenericType: true } declaring ? declaring.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

        for (var position = 0; position < il.Length;)
        {
            var value = il[position] == 0xFE ? (short)(0xFE00 | il[position + 1]) : il[position];
            position += il[position] == 0xFE ? 2 : 1;
            var opCode = OpCodesByValue[value];

            if (opCode.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
            {
                foreach (var used in Resolve(method.Module, BitConverter.ToInt32(il, position), typeArguments, methodArguments)) yield return used;
            }

            position += OperandSize(opCode.OperandType, il, position);
        }
    }

    private static IEnumerable<Type> Resolve(Module module, int token, Type[]? typeArguments, Type[]? methodArguments)
    {
        try
        {
            return module.ResolveMember(token, typeArguments, methodArguments) switch
            {
                Type type => [type],
                MethodInfo { IsGenericMethod: true } method => method.GetGenericArguments().Append(method.DeclaringType!),
                MemberInfo member when member.DeclaringType != null => [member.DeclaringType],
                _ => []
            };
        }
        catch (Exception ex) when (ex is ArgumentException or TypeLoadException or MissingMemberException or BadImageFormatException)
        {
            return [];
        }
    }

    private static int OperandSize(OperandType operandType, byte[] il, int position) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, position)),
        _ => 4
    };

    private static IEnumerable<Type> Expand(Type type)
    {
        yield return type;
        if (type.HasElementType && type.GetElementType() is { } element)
        {
            foreach (var inner in Expand(element)) yield return inner;
        }

        if (!type.IsGenericType) yield break;
        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var inner in Expand(argument)) yield return inner;
        }
    }
}
