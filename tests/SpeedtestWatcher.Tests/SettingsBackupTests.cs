using System.Text.Json;
using FakeItEasy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Events;
using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Core.Settings;
using SpeedtestWatcher.Infrastructure.Data;
using SpeedtestWatcher.Infrastructure.Repositories;

namespace SpeedtestWatcher.Tests;

public class SettingsBackupTests : IDisposable
{
    private const string DiscordData = """{"url":"https://localhost/discord.com/api/webhooks/1/x","send_skipped":false}""";

    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web);

    private readonly SqliteConnection _database = new("DataSource=:memory:");
    private readonly AppEvents _events = new();
    private readonly List<IReadOnlyDictionary<string, string>> _broadcasts = [];

    public SettingsBackupTests()
    {
        _database.Open();
        using var db = Context();
        db.Database.EnsureCreated();
        new SettingsStore(db).InsertDefaultsAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ABackup_RestoresSettingsIntegrationsAndRecommendations_AfterAFactoryReset()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        string integrationId;
        await using (var db = Context())
        {
            await new SettingsStore(db).SaveAsync(new Dictionary<string, string> { ["cron"] = "0,30 * * * *", ["chartRange"] = "24h" }, cancellationToken);
            integrationId = await new IntegrationRepository(db).CreateAsync("discord", "Family server", DiscordData, cancellationToken);
            await new RecommendationRepository(db).SaveAsync(12, 940.5, 110.25, cancellationToken);
        }

        var backupJson = await ExportJsonAsync(cancellationToken);
        await FactoryResetAsync(cancellationToken);

        SettingsImportResultDto result;
        await using (var db = Context())
        {
            result = (await Backup(db).ImportAsync(JsonSerializer.Deserialize<SettingsBackupDto>(backupJson, ApiJson)!, cancellationToken)).Value!;
        }

        await using var check = Context();
        var values = await new SettingsStore(check).GetValuesAsync(cancellationToken);
        Assert.Equal(("0,30 * * * *", "24h"), (values["cron"], values["chartRange"]));

        var integration = Assert.Single(await new IntegrationRepository(check).ListAllAsync(cancellationToken));
        Assert.Equal((integrationId, "discord", "Family server", DiscordData), (integration.Id, integration.Name, integration.DisplayName, integration.Data));

        var recommendation = await new RecommendationRepository(check).GetAsync(cancellationToken);
        Assert.Equal((12, 940.5, 110.25), (recommendation!.Ping, recommendation.Download, recommendation.Upload));

        Assert.Equal(1, result.Integrations);
        Assert.True(result.Recommendations);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(JsonSerializer.Deserialize<SettingsBackupDto>(backupJson, ApiJson)!.Config.Count, result.Settings);
    }

    [Fact]
    public async Task ImportingTheSameBackupTwice_DoesNotDuplicateIntegrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = Context())
        {
            await new IntegrationRepository(db).CreateAsync("discord", "Family server", DiscordData, cancellationToken);
        }

        var backup = JsonSerializer.Deserialize<SettingsBackupDto>(await ExportJsonAsync(cancellationToken), ApiJson)!;
        await using (var db = Context()) await Backup(db).ImportAsync(backup, cancellationToken);
        await using (var db = Context()) await Backup(db).ImportAsync(backup, cancellationToken);

        await using var check = Context();
        Assert.Single(await new IntegrationRepository(check).ListAllAsync(cancellationToken));
    }

    [Fact]
    public async Task ABackup_NeverContainsSignInSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = Context())
        {
            await new SettingsStore(db).SaveSignInAsync(new Dictionary<string, string> { ["oidcClientSecret"] = "hunter2", ["apiTokenHash"] = "ABC123" }, cancellationToken);
        }

        var backupJson = await ExportJsonAsync(cancellationToken);

        Assert.DoesNotContain("hunter2", backupJson);
        Assert.DoesNotContain(JsonSerializer.Deserialize<SettingsBackupDto>(backupJson, ApiJson)!.Config, entry => SettingDefinitions.Find(entry.Key)!.IsManagedOnSecurityTab);
    }

    [Fact]
    public async Task AnImport_SkipsSignInSettings_UnknownKeys_InvalidValues_AndIntegrationsItCannotRun()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var backup = new SettingsBackupDto
        {
            Config =
            [
                new ConfigEntry { Key = "authEnabled", Value = "true" },
                new ConfigEntry { Key = "oidcClientSecret", Value = "stolen" },
                new ConfigEntry { Key = "madeUpSetting", Value = "1" },
                new ConfigEntry { Key = "cron", Value = "every so often" },
                new ConfigEntry { Key = "chartRange", Value = "30d" }
            ],
            Integrations =
            [
                new IntegrationData { Name = "carrierPigeon", Data = "{}" },
                new IntegrationData { Name = "ntfy", Data = "not json" },
                new IntegrationData { Name = "ntfy", DisplayName = "Phone", Data = """{"url":"https://localhost/ntfy","topic":"alerts"}""" }
            ],
            Recommendations = new Recommendation { Ping = 0, Download = -1, Upload = 5 }
        };

        SettingsImportResultDto result;
        await using (var db = Context())
        {
            result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;
        }

        Assert.Equal((1, 1, false, 7), (result.Settings, result.Integrations, result.Recommendations, result.Skipped));
        Assert.Equal(new Dictionary<string, string> { ["chartRange"] = "30d" }, Assert.Single(_broadcasts));

        await using var check = Context();
        var values = await new SettingsStore(check).GetValuesAsync(cancellationToken);
        Assert.Equal(("false", "none", "0 * * * *", "30d"), (values["authEnabled"], values["oidcClientSecret"], values["cron"], values["chartRange"]));
        Assert.False(await check.Configs.AnyAsync(entry => entry.Key == "madeUpSetting", cancellationToken));
        Assert.Equal("Phone", Assert.Single(await new IntegrationRepository(check).ListAllAsync(cancellationToken)).DisplayName);
        Assert.Null(await new RecommendationRepository(check).GetAsync(cancellationToken));
    }

    [Fact]
    public async Task BackupsExportedByEarlierVersions_StillImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string earlierBackup = """
            {"config":[{"key":"chartRange","value":"24h"}],
             "integrations":[{"id":"b77102ac0f13","displayName":"Backup check","name":"discord","data":"{\"url\":\"https://localhost/discord.com/api/webhooks/1/x\"}","lastActivity":null,"activityFailed":false}],
             "recommendations":{"id":1,"ping":25,"download":100,"upload":50}}
            """;

        await using (var db = Context())
        {
            await Backup(db).ImportAsync(JsonSerializer.Deserialize<SettingsBackupDto>(earlierBackup, ApiJson)!, cancellationToken);
        }

        await using var check = Context();
        Assert.Equal("24h", (await new SettingsStore(check).GetValuesAsync(cancellationToken))["chartRange"]);
        Assert.Equal("Backup check", (await new IntegrationRepository(check).GetByIdAsync("b77102ac0f13", cancellationToken))!.DisplayName);
        Assert.Equal(25, (await new RecommendationRepository(check).GetAsync(cancellationToken))!.Ping);
    }

    private async Task<string> ExportJsonAsync(CancellationToken cancellationToken)
    {
        await using var db = Context();
        return JsonSerializer.Serialize((await Backup(db).ExportAsync(cancellationToken)).Value, ApiJson);
    }

    private async Task FactoryResetAsync(CancellationToken cancellationToken)
    {
        await using var db = Context();
        await new SettingsStore(db).ResetToDefaultsAsync(cancellationToken);
        await new IntegrationRepository(db).ClearAllAsync(cancellationToken);
        await new RecommendationRepository(db).ClearAllAsync(cancellationToken);
    }

    private SettingsBackupService Backup(SpeedtestWatcherDbContext db)
    {
        var dispatcher = TestIntegrations.Dispatcher(new IntegrationRepository(db), new RecordingHandler());
        var store = new SettingsStore(db);
        var settings = new SettingsService(store, dispatcher, _events, A.Fake<ISignInState>(), FixedAccess.Full);
        _events.SettingsChanged += changes => _broadcasts.Add(changes);

        return new SettingsBackupService(store, settings, new IntegrationRepository(db), new RecommendationRepository(db), dispatcher, FixedAccess.Full);
    }

    private SpeedtestWatcherDbContext Context() =>
        new(new DbContextOptionsBuilder<SpeedtestWatcherDbContext>().UseSqlite(_database).Options);

    public void Dispose() => _database.Dispose();
}
