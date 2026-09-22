using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Infrastructure;
using SpeedtestWatcher.Web.Components;
using SpeedtestWatcher.Web.Hosting;
using SpeedtestWatcher.Web.Hubs;
using SpeedtestWatcher.Web.Middleware;
using SpeedtestWatcher.Web.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://0.0.0.0:{SpeedtestWatcherOptionsSetup.Read(builder.Configuration).Port}");

builder.Services
    .AddHosting()
    .AddApplication()
    .AddInfrastructure()
    .AddAccessControl()
    .AddWebApi()
    .AddWebUi()
    .AddBackgroundServices();

var app = builder.Build();

var hosting = app.Services.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value;
Directory.CreateDirectory(hosting.DataDirectory);
Directory.CreateDirectory(hosting.ServersDirectory);

await app.Services.InitializeDatabaseAsync();
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<RecommendationService>().RemovePlaceholderAsync();
}

await app.Services.GetRequiredService<AuthSettings>().ReloadAsync();

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

app.MapHub<SpeedtestHub>(SpeedtestHub.HubUrl).RequireAuthorization(AccessPolicies.Read);
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization(AccessPolicies.Read);

await app.RunAsync();
