using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Installers;
using SpeedtestWatcher.Application.Services;
using SpeedtestWatcher.Web.Configuration;
using SpeedtestWatcher.Web.Installers;
using SpeedtestWatcher.Web.Middleware;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Utils;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Web.Ui;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://0.0.0.0:{SpeedtestWatcherOptionsSetup.Read(builder.Configuration).Port}");

builder.Services
    .AddHosting()
    .AddApplication()
    .AddAccessControl()
    .AddWebApi()
    .AddWebUi();

var app = builder.Build();

var hosting = app.Services.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value;
Directory.CreateDirectory(hosting.DataDirectory);
Directory.CreateDirectory(hosting.ServersDirectory);

await app.Services.InitializeDatabaseAsync();
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<RecommendationService>().RemovePlaceholderAsync();
}

await app.Services.GetRequiredService<AuthSettingsService>().ReloadAsync();

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
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapApiDocumentation();
app.MapHealthChecks(HealthEndpoint.Path).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization(AccessPolicies.Read);

await app.RunAsync();
