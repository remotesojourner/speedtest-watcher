using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Integrations;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Infrastructure.SpeedTest;
using SpeedtestWatcher.Web.Background;
using SpeedtestWatcher.Web.Components;
using SpeedtestWatcher.Web.Hosting;
using SpeedtestWatcher.Web.Hubs;
using SpeedtestWatcher.Web.Middleware;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Services.Auth;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://0.0.0.0:{SpeedtestWatcherOptionsSetup.Read(builder.Configuration).Port}");

builder.Services.AddSingleton<IConfigureOptions<SpeedtestWatcherOptions>, SpeedtestWatcherOptionsSetup>();

builder.Services.AddDataProtection()
    .SetApplicationName("SpeedtestWatcher");
builder.Services.AddOptions<KeyManagementOptions>()
    .Configure<IOptions<SpeedtestWatcherOptions>, ILoggerFactory>((keyManagement, options, loggerFactory) =>
        keyManagement.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(options.Value.KeysDirectory), loggerFactory));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<SpeedtestWatcherDbContext>((services, options) =>
{
    var databasePath = services.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value.DatabasePath;
    options.UseSqlite($"Data Source={databasePath};Cache=Shared");
});

builder.Services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
builder.Services.AddScoped<ISettingsStore, SettingsStore>();
builder.Services.AddScoped<IIntegrationRepository, IntegrationRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<IStorageRepository, StorageRepository>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICliManager, CliManager>();
builder.Services.AddScoped<ISpeedtestRunner, SpeedtestRunner>();
builder.Services.AddIntegrations();
builder.Services.AddSingleton<INetworkInterfaceDetector, InterfaceDetector>();
builder.Services.AddSingleton<ServerListProvider>();
builder.Services.AddScoped<ServerSelector>();
builder.Services.AddScoped<ConnectivityChecker>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<SettingsBackup>();

var pauseStateService = new PauseStateService();
builder.Services.AddSingleton<IPauseStateService>(pauseStateService);
builder.Services.AddSingleton(pauseStateService);

builder.Services.AddSingleton<SpeedtestSchedulerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SpeedtestSchedulerService>());

builder.Services.AddHostedService<RunStatusBroadcaster>();
builder.Services.AddHostedService<RetentionCleanupService>();
builder.Services.AddHostedService<IntegrationTickerService>();
builder.Services.AddHostedService<InterfaceRefreshService>();
builder.Services.AddHostedService<CliDownloadService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SpeedtestWatcher.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.LoginPath = "/auth/login";
    })
    .AddOpenIdConnect(AuthSettings.OidcScheme, _ => { });
builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OidcOptionsSetup>();
builder.Services.AddSingleton<AuthSettings>();
builder.Services.AddSingleton<InternalAccessToken>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();

builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<BrowserInterop>();
builder.Services.AddScoped<PreferencesService>();
builder.Services.AddScoped<StatusStateService>();
builder.Services.AddScoped<SpeedtestStateService>();
builder.Services.AddScoped<ConfigStateService>();

builder.Services.AddScoped(sp => new HttpClient(new InternalAuthHandler(
    sp.GetRequiredService<AuthenticationStateProvider>(),
    sp.GetRequiredService<InternalAccessToken>()))
{
    BaseAddress = new Uri($"http://127.0.0.1:{sp.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value.Port}/")
});

var app = builder.Build();

var hosting = app.Services.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value;
Directory.CreateDirectory(hosting.DataDirectory);
Directory.CreateDirectory(hosting.ServersDirectory);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");

    scope.ServiceProvider.GetRequiredService<ISettingsStore>().InsertDefaultsAsync().GetAwaiter().GetResult();
    scope.ServiceProvider.GetRequiredService<IRecommendationRepository>().RemovePlaceholderAsync().GetAwaiter().GetResult();
}

app.Services.GetRequiredService<AuthSettings>().ReloadAsync().GetAwaiter().GetResult();

app.UseForwardedHeaders();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") && context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
        statusCodePages.Enabled = false;
    await next();
});

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<AccessMiddleware>();
app.UseAntiforgery();

app.MapHub<SpeedtestHub>("/speedtestHub");
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
