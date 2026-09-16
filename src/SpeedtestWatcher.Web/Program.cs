using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Integrations;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Infrastructure.Repositories;
using SpeedtestWatcher.Infrastructure.SpeedTest;
using SpeedtestWatcher.Web.Background;
using SpeedtestWatcher.Web.Components;
using SpeedtestWatcher.Web.Hubs;
using SpeedtestWatcher.Web.Middleware;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Services.Auth;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

var portStr = Environment.GetEnvironmentVariable("PORT");
var port = int.TryParse(portStr, out var p) ? p : 2003;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(Path.Combine(dataDir, "servers"));

var dbPath = Path.Combine(dataDir, "storage.db");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")))
    .SetApplicationName("SpeedtestWatcher");

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<SpeedtestWatcherDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath};Cache=Shared");
});

builder.Services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
builder.Services.AddScoped<IConfigRepository, ConfigRepository>();
builder.Services.AddScoped<IIntegrationRepository, IntegrationRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICliManager, CliManager>();
builder.Services.AddScoped<ISpeedtestRunner, SpeedtestRunner>();
builder.Services.AddScoped<IIntegrationDispatcher, IntegrationDispatcher>();
builder.Services.AddSingleton<INetworkInterfaceDetector, InterfaceDetector>();
builder.Services.AddSingleton<ServerListProvider>();
builder.Services.AddScoped<ServerSelector>();
builder.Services.AddScoped<ConnectivityChecker>();

var pauseStateService = new PauseStateService();
builder.Services.AddSingleton<IPauseStateService>(pauseStateService);
builder.Services.AddSingleton(pauseStateService);

builder.Services.AddSingleton<SpeedtestSchedulerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SpeedtestSchedulerService>());

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

builder.Services.AddScoped<PreferencesService>();
builder.Services.AddScoped<StatusStateService>();
builder.Services.AddScoped<SpeedtestStateService>();
builder.Services.AddScoped<ConfigStateService>();

builder.Services.AddScoped(sp => new HttpClient(new InternalAuthHandler(
    sp.GetRequiredService<AuthenticationStateProvider>(),
    sp.GetRequiredService<InternalAccessToken>()))
{
    BaseAddress = new Uri($"http://127.0.0.1:{port}/")
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");

    var configRepo = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
    configRepo.InsertDefaultsAsync().GetAwaiter().GetResult();
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
