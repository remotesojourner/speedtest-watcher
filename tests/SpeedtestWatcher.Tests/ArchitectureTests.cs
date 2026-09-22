using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Tests;

public class ArchitectureTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Assembly Core = typeof(Speedtest).Assembly;
    private static readonly Assembly ApplicationLayer = typeof(OperationResult).Assembly;
    private static readonly Assembly Infrastructure = typeof(SpeedtestWatcherDbContext).Assembly;
    private static readonly Assembly Web = typeof(Program).Assembly;

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    [Fact]
    public void ApplicationAndInfrastructure_ReferenceOnlyCore()
    {
        Assert.Empty(ProjectReferences(Core));
        Assert.Equal(["SpeedtestWatcher.Core"], ProjectReferences(ApplicationLayer));
        Assert.Equal(["SpeedtestWatcher.Core"], ProjectReferences(Infrastructure));
    }

    [Fact]
    public void Controllers_UseApplicationServices_NotRepositoriesOrInfrastructure()
    {
        var offending = Web.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(controller => controller.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Where(parameter => parameter.ParameterType.Assembly == Infrastructure || IsRepository(parameter.ParameterType))
                .Select(parameter => $"{controller.Name} takes {parameter.ParameterType.Name}"))
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Components_InjectServices_NotRepositoriesOrInfrastructure()
    {
        var offending = Web.GetTypes()
            .Where(type => type.IsAssignableTo(typeof(ComponentBase)))
            .SelectMany(component => component.GetProperties(Declared)
                .Where(property => property.IsDefined(typeof(InjectAttribute)))
                .Where(property => property.PropertyType.Assembly == Infrastructure || IsRepository(property.PropertyType) || property.PropertyType == typeof(HttpClient))
                .Select(property => $"{component.Name} injects {property.PropertyType.Name}"))
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void OnlyProgram_UsesInfrastructureTypes()
    {
        var offending = Web.GetTypes()
            .Where(type => !BelongsToProgram(type))
            .SelectMany(type => TypesUsedBy(type)
                .Where(used => used.Assembly == Infrastructure)
                .Select(used => $"{type.FullName} uses {used.FullName}"))
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
        type.Assembly == Core && (type.Name.EndsWith("Repository", StringComparison.Ordinal) || type == typeof(ISettingsStore));

    private static bool BelongsToProgram(Type type)
    {
        for (var current = type; current != null; current = current.DeclaringType)
        {
            if (current == typeof(Program)) return true;
        }

        return false;
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
