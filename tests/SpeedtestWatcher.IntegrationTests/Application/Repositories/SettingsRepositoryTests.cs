using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.IntegrationTests.Fixtures;
using SpeedtestWatcher.Application.Configuration;
using SpeedtestWatcher.Application.Data;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models.Entities;
using SpeedtestWatcher.Application.Repositories;

namespace SpeedtestWatcher.IntegrationTests.Application.Repositories;

public sealed class SettingsRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public async Task DefaultsAreInsertedForEveryDefinitionAndRetiredKeysAreDropped()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var db = _database.NewContext())
        {
            db.Configs.Add(new ConfigEntry { Key = "password", Value = "$2a$11$hash" });
            db.Configs.Add(new ConfigEntry { Key = "passwordLevel", Value = "read" });
            await db.SaveChangesAsync(cancellationToken);
            await new SettingsRepository(db).InsertDefaultsAsync(cancellationToken);
        }

        await using var check = _database.NewContext();
        var stored = await check.Configs.ToDictionaryAsync(entry => entry.Key, entry => entry.Value, cancellationToken);
        Assert.Equal(SettingDefinitions.All.ToDictionary(definition => definition.Key, definition => definition.Default), stored);
        Assert.Equal(("false", "none", "0 * * * *"), (stored["authEnabled"], stored["visitorAccess"], stored["cron"]));
    }

    [Theory]
    [InlineData("ping", "25", true)]
    [InlineData("ping", "abc", false)]
    [InlineData("ping", " ", false)]
    [InlineData("cron", "0 * * * *", true)]
    [InlineData("cron", "invalid-cron", false)]
    [InlineData("provider", "ookla", true)]
    [InlineData("provider", "unknown", false)]
    [InlineData("serverMode", "random", true)]
    [InlineData("serverMode", "single", true)]
    [InlineData("serverMode", "pinned", false)]
    [InlineData("serverMode", "sideways", false)]
    [InlineData("serverListMode", "deny", true)]
    [InlineData("serverListMode", "maybe", false)]
    [InlineData("ooklaServerIds", "none", true)]
    [InlineData("ooklaServerIds", "12345,6789", true)]
    [InlineData("ooklaServerIds", "nearest", false)]
    [InlineData("iperf3Servers", "none", true)]
    [InlineData("iperf3Servers", "192.168.1.10:5201,speedtest.lan:5202", true)]
    [InlineData("iperf3Servers", "192.168.1.10", false)]
    [InlineData("iperf3Servers", "192.168.1.10:70000", false)]
    [InlineData("internetCheckEnabled", "false", true)]
    [InlineData("internetCheckEnabled", "yes", false)]
    [InlineData("internetCheckUrl", "https://icanhazip.com", true)]
    [InlineData("internetCheckUrl", "icanhazip", false)]
    [InlineData("internetCheckUrl", "ftp://icanhazip.com", false)]
    [InlineData("libreUrl", "https://speed.example/backend/", true)]
    [InlineData("libreUrl", "file:///etc/passwd", false)]
    [InlineData("skipIps", "none", true)]
    [InlineData("skipIps", "203.0.113.9, 2a00:23c8:870c:bf00::1", true)]
    [InlineData("skipIps", "my-router", false)]
    [InlineData("chartRange", "24h", true)]
    [InlineData("chartRange", "12h", false)]
    [InlineData("dateFormat", "ymd", true)]
    [InlineData("dateFormat", "iso", false)]
    [InlineData("retentionDays", "0", true)]
    [InlineData("retentionDays", "10001", false)]
    [InlineData("visitorAccess", "read", true)]
    [InlineData("visitorAccess", "everything", false)]
    [InlineData("authEnabled", "true", true)]
    [InlineData("authEnabled", "on", false)]
    [InlineData("oidcAuthority", "none", true)]
    [InlineData("oidcAuthority", "auth.example.com", false)]
    public void EachSettingAcceptsOnlyValuesItCanUse(string key, string value, bool accepted)
    {
        Assert.Equal(accepted, SettingDefinitions.Find(key)!.ProblemWith(value) == null);
    }

    [Fact]
    public async Task ABatchWithOneInvalidValueChangesNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = await RepositoryWithDefaultsAsync(db, cancellationToken);

        var result = await repository.SaveAsync(new Dictionary<string, string> { ["chartRange"] = "24h", ["ping"] = "fast" }, cancellationToken);

        Assert.Equal("You need to provide a number in order to change this", result.Error);
        Assert.Equal("7d", (await repository.GetValuesAsync(cancellationToken))["chartRange"]);
    }

    [Fact]
    public async Task ABatchOfValidValuesIsSavedTogether()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = await RepositoryWithDefaultsAsync(db, cancellationToken);

        var result = await repository.SaveAsync(new Dictionary<string, string> { ["chartRange"] = "24h", ["ping"] = "18" }, cancellationToken);

        Assert.True(result.Succeeded);
        var values = await repository.GetValuesAsync(cancellationToken);
        Assert.Equal(("24h", "18"), (values["chartRange"], values["ping"]));
    }

    [Theory]
    [InlineData("authEnabled", "true", "Sign-in settings are changed on the Security tab")]
    [InlineData("apiTokenHash", "ABC123", "Sign-in settings are changed on the Security tab")]
    [InlineData("madeUpSetting", "1", "There's no setting called madeUpSetting")]
    public async Task GeneralSavesRefuseSignInSettingsAndKeysThatDoNotExist(string key, string value, string expectedError)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = await RepositoryWithDefaultsAsync(db, cancellationToken);

        var result = await repository.SaveAsync(new Dictionary<string, string> { [key] = value }, cancellationToken);

        Assert.Equal(expectedError, result.Error);
        Assert.Null(await db.Configs.AsNoTracking().SingleOrDefaultAsync(entry => entry.Key == key && entry.Value == value, cancellationToken));
    }

    [Fact]
    public async Task SignInSavesRefuseOtherSettings()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = await RepositoryWithDefaultsAsync(db, cancellationToken);

        var result = await repository.SaveSignInAsync(new Dictionary<string, string> { ["authEnabled"] = "true", ["cron"] = "0,30 * * * *" }, cancellationToken);

        Assert.Equal("Only sign-in settings are changed on the Security tab", result.Error);
        Assert.False((await repository.GetAsync(cancellationToken)).SignIn.Enabled);
    }

    [Fact]
    public async Task TypedSettingsTurnNoneIntoNullAndReadEveryGroup()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = await RepositoryWithDefaultsAsync(db, cancellationToken);
        await repository.SaveAsync(new Dictionary<string, string>
        {
            ["provider"] = "libre",
            ["libreUrl"] = "https://speed.example/backend/",
            ["serverMode"] = "random",
            ["serverListMode"] = "deny",
            ["libreServerIds"] = "12,34",
            ["skipIps"] = "203.0.113.9",
            ["ping"] = "0",
            ["retentionDays"] = "90"
        }, cancellationToken);
        await repository.SaveSignInAsync(new Dictionary<string, string> { ["visitorAccess"] = "read", ["oidcScopes"] = "profile" }, cancellationToken);

        var settings = await repository.GetAsync(cancellationToken);

        Assert.Equal(new TargetSettings(null, 100, 50), settings.Targets);
        Assert.Equal(("0 * * * *", true), (settings.Schedule.Cron, settings.Schedule.RandomOffset));
        Assert.Equal((SpeedtestProvider.Libre, ServerMode.Random, ServerListMode.Deny), (settings.Provider.Selected, settings.Provider.ServerMode, settings.Provider.ServerListMode));
        Assert.Equal(("https://speed.example/backend/", null), (settings.Provider.LibreUrl, settings.Provider.Interface));
        Assert.Equal(["12", "34"], settings.Provider.Libre.ListedIds);
        Assert.Null(settings.Provider.Ookla.PinnedId);
        Assert.Equal((true, "https://icanhazip.com"), (settings.PreTestChecks.InternetCheckEnabled, settings.PreTestChecks.InternetCheckUrl));
        Assert.Equal(["203.0.113.9"], settings.PreTestChecks.SkipIps);
        Assert.Equal(new DisplaySettings("7d", false, "dmy"), settings.Display);
        Assert.Equal(90, settings.RetentionDays);
        Assert.Equal((VisitorAccess.Read, null, null), (settings.SignIn.VisitorAccess, settings.SignIn.Authority, settings.SignIn.ApiTokenHash));
        Assert.Equal(["openid", "profile"], settings.SignIn.Scopes);
    }

    [Fact]
    public async Task ValuesStoredByHandThatDoNotParseFallBackSafely()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = _database.NewContext();
        var repository = await RepositoryWithDefaultsAsync(db, cancellationToken);
        foreach (var entry in await db.Configs.Where(entry => entry.Key == "provider" || entry.Key == "retentionDays").ToListAsync(cancellationToken))
            entry.Value = "garbage";
        await db.SaveChangesAsync(cancellationToken);

        var settings = await repository.GetAsync(cancellationToken);

        Assert.Equal((SpeedtestProvider.None, 0), (settings.Provider.Selected, settings.RetentionDays));
    }

    private static async Task<SettingsRepository> RepositoryWithDefaultsAsync(SpeedtestWatcherDbContext db, CancellationToken cancellationToken)
    {
        var repository = new SettingsRepository(db);
        await repository.InsertDefaultsAsync(cancellationToken);
        return repository;
    }

    public void Dispose() => _database.Dispose();
}
