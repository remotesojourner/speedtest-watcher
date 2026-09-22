using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Web.SignIn;
using SpeedtestWatcher.Web.Startup;

namespace SpeedtestWatcher.Tests;

public abstract class TestApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "speedtest-watcher-tests", Guid.NewGuid().ToString("N"));

    public string RootDirectory => _root;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using (var scope = Services.CreateScope())
        {
            await SeedAsync(scope.ServiceProvider, cancellationToken);
        }

        await Services.GetRequiredService<AuthSettings>().ReloadAsync(cancellationToken);
    }

    public HttpClient CreateClientWithoutRedirects() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    protected abstract Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(SpeedtestWatcherOptionsSetup.DataDirectoryVariable, Path.Combine(_root, "data"));
        builder.UseSetting(SpeedtestWatcherOptionsSetup.BinDirectoryVariable, Path.Combine(_root, "bin"));
        builder.UseSetting(SpeedtestWatcherOptionsSetup.DisableAuthVariable, "false");
        builder.UseSetting(SpeedtestWatcherOptionsSetup.RunTestOnStartupVariable, "false");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveTheAppsBackgroundServices(services);
            services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new NoNetworkHandler()));
        });
    }

    private static void RemoveTheAppsBackgroundServices(IServiceCollection services)
    {
        Assembly[] app = [typeof(Program).Assembly, typeof(ApplicationServiceCollectionExtensions).Assembly];
        var backgroundServices = services
            .Where(service => service.ServiceType == typeof(IHostedService) && !service.IsKeyedService && app.Any(assembly => DeclaredIn(service, assembly)))
            .ToList();

        foreach (var service in backgroundServices) services.Remove(service);
    }

    private static bool DeclaredIn(ServiceDescriptor service, Assembly assembly) =>
        service.ImplementationType?.Assembly == assembly
        || service.ImplementationFactory?.Method.DeclaringType?.Assembly == assembly;

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException($"Tests have no network access, so {request.RequestUri} wasn't called"));
    }
}
