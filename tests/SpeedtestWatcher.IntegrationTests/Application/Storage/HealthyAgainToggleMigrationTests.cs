using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.IntegrationTests.Fixtures;

namespace SpeedtestWatcher.IntegrationTests.Application.Storage;

public sealed class HealthyAgainToggleMigrationTests : IDisposable
{
    private const string MigrationBefore = "20260915231423_AddResultStatusAndThresholds";

    private readonly TestDatabase _database = TestDatabase.WithoutSchema();

    [Fact]
    public async Task EveryIntegrationWithTheToggleFollowsItsUnhealthyToggle()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(MigrationBefore, cancellationToken);

        db.Integrations.AddRange(
            new IntegrationData { Id = "off", Name = "discord", Data = """{"url":"https://localhost/discord","send_unhealthy":false}""" },
            new IntegrationData { Id = "on", Name = "ntfy", Data = """{"topic":"alerts","send_unhealthy":true}""" },
            new IntegrationData { Id = "unset", Name = "telegram", Data = """{"token":"t","chat_id":"1"}""" },
            new IntegrationData { Id = "text", Name = "webhook", Data = """{"url":"https://localhost/hook","send_unhealthy":"False"}""" },
            new IntegrationData { Id = "chosen", Name = "gotify", Data = """{"url":"https://localhost/gotify","send_unhealthy":true,"send_healthy_again":false}""" },
            new IntegrationData { Id = "other", Name = "healthChecks", Data = """{"url":"https://localhost/hc"}""" });
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        await migrator.MigrateAsync(cancellationToken: cancellationToken);

        var data = await db.Integrations.AsNoTracking().ToDictionaryAsync(integration => integration.Id, integration => JsonNode.Parse(integration.Data)!.AsObject(), cancellationToken);
        Assert.False(data["off"]["send_healthy_again"]!.GetValue<bool>());
        Assert.True(data["on"]["send_healthy_again"]!.GetValue<bool>());
        Assert.True(data["unset"]["send_healthy_again"]!.GetValue<bool>());
        Assert.False(data["text"]["send_healthy_again"]!.GetValue<bool>());
        Assert.False(data["chosen"]["send_healthy_again"]!.GetValue<bool>());
        Assert.False(data["other"].ContainsKey("send_healthy_again"));
    }

    public void Dispose() => _database.Dispose();
}
