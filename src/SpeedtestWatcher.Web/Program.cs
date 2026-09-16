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

// Port configuration
string portStr = Environment.GetEnvironmentVariable("PORT") ?? "5216";
int port = int.TryParse(portStr, out int p) ? p : 5216;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Data Directory & SQLite Setup
string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(Path.Combine(dataDir, "servers"));

string dbFileName = Environment.GetEnvironmentVariable("PREVIEW_MODE") == "true" ? "storage_preview.db" : "storage.db";
string dbPath = Path.Combine(dataDir, dbFileName);

// Sign-in cookies are encrypted with these keys. Keeping them with the data means a restart doesn't sign everyone out.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")))
    .SetApplicationName("SpeedtestWatcher");

// Behind a reverse proxy, sign-in has to build its callback URL from the address people actually use.
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

// Repositories
builder.Services.AddScoped<ISpeedtestRepository, SpeedtestRepository>();
builder.Services.AddScoped<IConfigRepository, ConfigRepository>();
builder.Services.AddScoped<IIntegrationRepository, IntegrationRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();

// Network & CLI Services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ICliManager, CliManager>();
builder.Services.AddScoped<ISpeedtestRunner, SpeedtestRunner>();
builder.Services.AddScoped<IIntegrationDispatcher, IntegrationDispatcher>();
builder.Services.AddSingleton<INetworkInterfaceDetector, InterfaceDetector>();
builder.Services.AddSingleton<ServerListProvider>();
builder.Services.AddScoped<ServerSelector>();
builder.Services.AddScoped<ConnectivityChecker>();

// Background and Coordination Services
var pauseStateService = new PauseStateService();
builder.Services.AddSingleton<IPauseStateService>(pauseStateService);
builder.Services.AddSingleton<PauseStateService>(pauseStateService);

builder.Services.AddSingleton<SpeedtestSchedulerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SpeedtestSchedulerService>());

builder.Services.AddHostedService<RetentionCleanupService>();
builder.Services.AddHostedService<IntegrationTickerService>();
builder.Services.AddHostedService<InterfaceRefreshService>();
builder.Services.AddHostedService<CliDownloadService>();

// Blazor UI & State Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddControllers();

// Sign-in with an OpenID Connect provider. AuthSettings loads the saved settings, and adds or removes the
// OpenID Connect scheme to match, so switching sign-in on or off needs no restart.
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

// MudBlazor Services
builder.Services.AddMudServices();

// Client App State Services
builder.Services.AddScoped<PreferencesService>();
builder.Services.AddScoped<StatusStateService>();
builder.Services.AddScoped<SpeedtestStateService>();
builder.Services.AddScoped<ConfigStateService>();

// The UI's calls to the app's own API. They stay on loopback, and carry the internal token while the person is signed in.
builder.Services.AddScoped(sp => new HttpClient(new InternalAuthHandler(
    sp.GetRequiredService<AuthenticationStateProvider>(),
    sp.GetRequiredService<InternalAccessToken>()))
{
    BaseAddress = new Uri($"http://127.0.0.1:{port}/")
});

var app = builder.Build();

// Database initialization
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

// Unknown page URLs show the app's not-found page.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// API clients keep plain status codes instead of that HTML page.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") && context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
        statusCodePages.Enabled = false;
    await next();
});

// Middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

// Static files carry nothing private and must load for the sign-in pages, so they're served before any access check.
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<AccessMiddleware>();
app.UseAntiforgery();

app.MapHub<SpeedtestHub>("/speedtestHub");
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
