using System.Text.Json;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Integrations;
using SpeedtestWatcher.Application.Recommendations;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.SignIn;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.TestSupport;

namespace SpeedtestWatcher.IntegrationTests.Application.Settings;

public sealed class SettingsBackupTests : IDisposable
{
    private const string DiscordData = """{"url":"https://localhost/discord.com/api/webhooks/1/x","send_skipped":false}""";

    private static readonly JsonSerializerOptions _apiJson = new(JsonSerializerDefaults.Web);

    private readonly TestDatabase _database = new();
    private readonly AppEvents _events = new();
    private readonly List<IReadOnlyDictionary<string, string>> _broadcasts = [];

    public SettingsBackupTests()
    {
        using var db = _database.NewContext();
        new SettingsStore(db).InsertDefaultsAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ABackupRestoresSettingsIntegrationsAndRecommendationsAfterAFactoryReset()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        string integrationId;
        await using (var db = _database.NewContext())
        {
            await new SettingsStore(db).SaveAsync(new Dictionary<string, string> { ["cron"] = "0,30 * * * *", ["chartRange"] = "24h" }, cancellationToken);
            integrationId = await new IntegrationRepository(db).CreateAsync("discord", "Family server", DiscordData, cancellationToken);
            await new RecommendationRepository(db).SaveAsync(12, 940.5, 110.25, cancellationToken);
        }

        var backupJson = await ExportJsonAsync(cancellationToken);
        await FactoryResetAsync(cancellationToken);

        SettingsImportResultDto result;
        await using (var db = _database.NewContext())
        {
            result = (await Backup(db).ImportAsync(JsonSerializer.Deserialize<SettingsBackupDto>(backupJson, _apiJson)!, cancellationToken)).Value!;
        }

        await using var check = _database.NewContext();
        var values = await new SettingsStore(check).GetValuesAsync(cancellationToken);
        Assert.Equal(("0,30 * * * *", "24h"), (values["cron"], values["chartRange"]));

        var integration = Assert.Single(await new IntegrationRepository(check).ListAllAsync(cancellationToken));
        Assert.Equal((integrationId, "discord", "Family server", DiscordData), (integration.Id, integration.Name, integration.DisplayName, integration.Data));

        var recommendation = await new RecommendationRepository(check).GetAsync(cancellationToken);
        Assert.Equal((12, 940.5, 110.25), (recommendation!.Ping, recommendation.Download, recommendation.Upload));

        Assert.Equal(1, result.Integrations);
        Assert.True(result.Recommendations);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(JsonSerializer.Deserialize<SettingsBackupDto>(backupJson, _apiJson)!.Config.Count, result.Settings);
    }

    [Fact]
    public async Task ImportingTheSameBackupTwiceDoesNotDuplicateIntegrations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = _database.NewContext())
        {
            await new IntegrationRepository(db).CreateAsync("discord", "Family server", DiscordData, cancellationToken);
        }

        var backup = JsonSerializer.Deserialize<SettingsBackupDto>(await ExportJsonAsync(cancellationToken), _apiJson)!;
        await using (var db = _database.NewContext()) await Backup(db).ImportAsync(backup, cancellationToken);
        await using (var db = _database.NewContext()) await Backup(db).ImportAsync(backup, cancellationToken);

        await using var check = _database.NewContext();
        Assert.Single(await new IntegrationRepository(check).ListAllAsync(cancellationToken));
    }

    [Fact]
    public async Task ABackupNeverContainsSignInSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = _database.NewContext())
        {
            await new SettingsStore(db).SaveSignInAsync(new Dictionary<string, string> { ["oidcClientSecret"] = "hunter2", ["apiTokenHash"] = "ABC123" }, cancellationToken);
        }

        var backupJson = await ExportJsonAsync(cancellationToken);

        Assert.DoesNotContain("hunter2", backupJson);
        Assert.DoesNotContain(JsonSerializer.Deserialize<SettingsBackupDto>(backupJson, _apiJson)!.Config, entry => SettingDefinitions.Find(entry.Key)!.IsManagedOnSecurityTab);
    }

    [Fact]
    public async Task AnImportSkipsSignInSettingsUnknownKeysInvalidValuesAndIntegrationsItCannotRun()
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
        await using (var db = _database.NewContext())
        {
            result = (await Backup(db).ImportAsync(backup, cancellationToken)).Value!;
        }

        Assert.Equal((1, 1, false, 7), (result.Settings, result.Integrations, result.Recommendations, result.Skipped));
        Assert.Equal(new Dictionary<string, string> { ["chartRange"] = "30d" }, Assert.Single(_broadcasts));

        await using var check = _database.NewContext();
        var values = await new SettingsStore(check).GetValuesAsync(cancellationToken);
        Assert.Equal(("false", "none", "0 * * * *", "30d"), (values["authEnabled"], values["oidcClientSecret"], values["cron"], values["chartRange"]));
        Assert.False(await check.Configs.AnyAsync(entry => entry.Key == "madeUpSetting", cancellationToken));
        Assert.Equal("Phone", Assert.Single(await new IntegrationRepository(check).ListAllAsync(cancellationToken)).DisplayName);
        Assert.Null(await new RecommendationRepository(check).GetAsync(cancellationToken));
    }

    [Fact]
    public async Task BackupsExportedByEarlierVersionsStillImport()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string EarlierBackup = """
            {"config":[{"key":"chartRange","value":"24h"}],
             "integrations":[{"id":"b77102ac0f13","displayName":"Backup check","name":"discord","data":"{\"url\":\"https://localhost/discord.com/api/webhooks/1/x\"}","lastActivity":null,"activityFailed":false}],
             "recommendations":{"id":1,"ping":25,"download":100,"upload":50}}
            """;

        await using (var db = _database.NewContext())
        {
            await Backup(db).ImportAsync(JsonSerializer.Deserialize<SettingsBackupDto>(EarlierBackup, _apiJson)!, cancellationToken);
        }

        await using var check = _database.NewContext();
        Assert.Equal("24h", (await new SettingsStore(check).GetValuesAsync(cancellationToken))["chartRange"]);
        Assert.Equal("Backup check", (await new IntegrationRepository(check).GetByIdAsync("b77102ac0f13", cancellationToken))!.DisplayName);
        Assert.Equal(25, (await new RecommendationRepository(check).GetAsync(cancellationToken))!.Ping);
    }

    private async Task<string> ExportJsonAsync(CancellationToken cancellationToken)
    {
        await using var db = _database.NewContext();
        return JsonSerializer.Serialize((await Backup(db).ExportAsync(cancellationToken)).Value, _apiJson);
    }

    private async Task FactoryResetAsync(CancellationToken cancellationToken)
    {
        await using var db = _database.NewContext();
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

    public void Dispose() => _database.Dispose();
}
