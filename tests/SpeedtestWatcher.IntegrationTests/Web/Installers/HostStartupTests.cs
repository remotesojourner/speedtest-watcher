using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Data;

namespace SpeedtestWatcher.IntegrationTests.Web.Installers;

public sealed class HostStartupTests : IClassFixture<SignInOffApp>
{
    private readonly SignInOffApp _app;

    public HostStartupTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task StartupMigratesTheDatabaseToTheCurrentModel()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.False(db.Database.HasPendingModelChanges(), "The model has changes that no migration covers");
    }

    [Fact]
    public void RuntimeFilesGoToTheConfiguredDirectories()
    {
        var options = _app.Services.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value;

        Assert.Equal(Path.Combine(_app.RootDirectory, "data"), options.DataDirectory);
        Assert.Equal(Path.Combine(_app.RootDirectory, "bin"), options.BinDirectory);
        Assert.True(File.Exists(options.DatabasePath));
        Assert.True(Directory.Exists(options.ServersDirectory));
    }
}
