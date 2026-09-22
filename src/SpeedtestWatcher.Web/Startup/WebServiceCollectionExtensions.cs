using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Web.Api;
using SpeedtestWatcher.Web.Api.OpenApi;
using SpeedtestWatcher.Web.SignIn;
using SpeedtestWatcher.Web.Ui.State;

namespace SpeedtestWatcher.Web.Startup;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddHosting(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<SpeedtestWatcherOptions>, SpeedtestWatcherOptionsSetup>();

        services.AddDataProtection().SetApplicationName("SpeedtestWatcher");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IOptions<SpeedtestWatcherOptions>, ILoggerFactory>((keyManagement, options, loggerFactory) =>
                keyManagement.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(options.Value.KeysDirectory), loggerFactory));

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddAccessControl(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OidcOptionsSetup>();

        services.AddSingleton<AuthSettings>();
        services.AddSingleton<ISignInState>(provider => provider.GetRequiredService<AuthSettings>());
        services.AddSingleton<RequestAccess>();
        services.AddHttpContextAccessor();
        services.AddScoped<HttpCurrentAccess>();
        services.AddScoped<CircuitAccess>();
        services.AddScoped<ICurrentAccess, CurrentAccess>();

        services.AddAuthorization(AccessPolicies.Configure);
        services.AddSingleton<IAuthorizationHandler, AccessRequirementHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AccessDeniedResponder>();
        services.AddCascadingAuthenticationState();
        return services;
    }

    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(InvalidRequest.Describe(context.ModelState));
        });
        services.AddApiDocumentation();
        return services;
    }

    public static IServiceCollection AddWebUi(this IServiceCollection services)
    {
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddMudServices();

        services.AddScoped<BrowserInterop>();
        services.AddScoped<PreferencesService>();
        services.AddScoped<StatusStateService>();
        services.AddScoped<SettingsState>();
        services.AddScoped<RecentResults>();
        services.AddScoped<LiveUpdates>();
        return services;
    }
}
