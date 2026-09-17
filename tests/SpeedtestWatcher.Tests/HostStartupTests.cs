using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Core.Hosting;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Tests;

public sealed class HostStartupTests : IClassFixture<SignInOffApp>
{
    private readonly SignInOffApp _app;

    public HostStartupTests(SignInOffApp app)
    {
        _app = app;
    }

    [Fact]
    public async Task Startup_MigratesTheDatabaseToTheCurrentModel()
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SpeedtestWatcherDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.False(db.Database.HasPendingModelChanges(), "The model has changes that no migration covers");
    }

    [Fact]
    public void RuntimeFiles_GoToTheConfiguredDirectories()
    {
        var options = _app.Services.GetRequiredService<IOptions<SpeedtestWatcherOptions>>().Value;

        Assert.Equal(Path.Combine(_app.RootDirectory, "data"), options.DataDirectory);
        Assert.Equal(Path.Combine(_app.RootDirectory, "bin"), options.BinDirectory);
        Assert.True(File.Exists(options.DatabasePath));
        Assert.True(Directory.Exists(options.ServersDirectory));
    }
}
