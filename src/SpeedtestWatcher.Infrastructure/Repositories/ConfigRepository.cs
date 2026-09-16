using System.Text.RegularExpressions;
using Cronos;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;
using System.Net;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class ConfigRepository : IConfigRepository
{
    private readonly SpeedtestWatcherDbContext _db;

    public static readonly Dictionary<string, string> ConfigDefaults = new()
    {
        ["ping"] = "25",
        ["download"] = "100",
        ["upload"] = "50",
        ["cron"] = "0 * * * *",
        ["scheduleOffset"] = "true",
        ["provider"] = "none",
        ["ooklaId"] = "none",
        ["libreId"] = "none",
        ["libreUrl"] = "none",
        ["interface"] = "none",
        ["retentionDays"] = "365",

        // Server choice: auto (provider picks), random (from the list below) or single (ooklaId/libreId).
        ["serverMode"] = "auto",
        // How the per-provider list is used in random mode: allow (pick from it) or deny (pick from anything else).
        ["serverListMode"] = "allow",
        ["ooklaServerIds"] = "none",
        ["libreServerIds"] = "none",

        // Pre-test checks. The check URL returns the public IP, which is also what skipIps is compared against.
        ["internetCheckEnabled"] = "true",
        ["internetCheckUrl"] = "https://icanhazip.com",
        ["skipIps"] = "none",

        // Display
        ["chartRange"] = "7d",
        ["chartBeginAtZero"] = "false",
        ["dateFormat"] = "dmy",

        // Sign-in. These change only through /api/auth, which checks them together before saving.
        ["authEnabled"] = "false",
        ["visitorAccess"] = "none",
        ["oidcAuthority"] = "none",
        ["oidcClientId"] = "none",
        ["oidcClientSecret"] = "none",
        ["oidcScopes"] = "openid profile email",
        ["apiTokenHash"] = "none"
    };

    // Settings of features that no longer exist, removed so they don't linger in older databases.
    private static readonly string[] ObsoleteKeys = ["password", "passwordLevel"];

    public ConfigRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<List<ConfigEntry>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Configs.ToListAsync(cancellationToken);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = await _db.Configs.FindAsync([key], cancellationToken);
        return entry?.Value;
    }

    public async Task<bool> UpdateValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var entry = await _db.Configs.FindAsync([key], cancellationToken);
        if (entry == null)
        {
            _db.Configs.Add(new ConfigEntry { Key = key, Value = value });
        }
        else
        {
            entry.Value = value;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task InsertDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var obsolete = await _db.Configs.Where(c => ObsoleteKeys.Contains(c.Key)).ToListAsync(cancellationToken);
        _db.Configs.RemoveRange(obsolete);

        var existingKeys = (await _db.Configs.Select(c => c.Key).ToListAsync(cancellationToken)).ToHashSet();

        foreach (var (k, v) in ConfigDefaults)
        {
            if (!existingKeys.Contains(k))
            {
                _db.Configs.Add(new ConfigEntry { Key = k, Value = v });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<string?> ValidateInputAsync(string key, object? value, CancellationToken cancellationToken = default)
    {
        string? valStr = value?.ToString();
        if (string.IsNullOrWhiteSpace(valStr))
            return Task.FromResult<string?>("You need to provide the new value");

        if ((key == "ping" || key == "download" || key == "upload") && !Regex.IsMatch(valStr, @"^[0-9]+(\.[0-9]+)?$"))
            return Task.FromResult<string?>("You need to provide a number in order to change this");

        if ((key == "ooklaId" || key == "libreId") && valStr != "none" && !Regex.IsMatch(valStr, @"^[0-9]+$"))
            return Task.FromResult<string?>("You need to provide a number in order to change this");

        if (key == "libreUrl" && valStr != "none")
        {
            if (!Uri.TryCreate(valStr, UriKind.Absolute, out _))
                return Task.FromResult<string?>("You need to provide a valid URL in order to change this");
        }

        if (key == "cron")
        {
            try
            {
                CronExpression.Parse(valStr, CronFormat.Standard);
            }
            catch
            {
                return Task.FromResult<string?>("You need to provide a valid cron expression");
            }
        }

        if (key == "provider" && !new[] { "none", "ookla", "libre", "cloudflare" }.Contains(valStr))
            return Task.FromResult<string?>("You need to provide a valid provider");

        if (key == "scheduleOffset" && valStr != "true" && valStr != "false")
            return Task.FromResult<string?>("You need to provide a boolean in order to change this");

        if (key == "visitorAccess" && !new[] { "none", "read" }.Contains(valStr))
            return Task.FromResult<string?>("You need to provide a valid visitor access level");

        if (key == "retentionDays")
        {
            if (!int.TryParse(valStr, out int r) || r < 0 || r > 10000)
                return Task.FromResult<string?>("You need to provide a number between 0 and 10000 in order to change this");
        }

        if (key == "serverMode" && !new[] { "auto", "random", "single" }.Contains(valStr))
            return Task.FromResult<string?>("You need to provide a valid server mode");

        if (key == "serverListMode" && !new[] { "allow", "deny" }.Contains(valStr))
            return Task.FromResult<string?>("You need to provide a valid server list mode");

        // Stored as a comma separated list of ids, or "none" for an empty one.
        if ((key == "ooklaServerIds" || key == "libreServerIds") && valStr != "none"
            && !Regex.IsMatch(valStr, @"^[0-9]+(,[0-9]+)*$"))
            return Task.FromResult<string?>("Server IDs need to be numbers separated by commas");

        if ((key == "internetCheckEnabled" || key == "chartBeginAtZero" || key == "authEnabled") && valStr != "true" && valStr != "false")
            return Task.FromResult<string?>("You need to provide a boolean in order to change this");

        if (key == "internetCheckUrl" && !Uri.TryCreate(valStr, UriKind.Absolute, out _))
            return Task.FromResult<string?>("You need to provide a valid URL in order to change this");

        if (key == "skipIps" && valStr != "none"
            && !valStr.Split(',').All(part => IPAddress.TryParse(part.Trim(), out _)))
            return Task.FromResult<string?>("The skip list needs IP addresses separated by commas");

        if (key == "chartRange" && !new[] { "24h", "7d", "30d" }.Contains(valStr))
            return Task.FromResult<string?>("You need to provide a valid chart range");

        if (key == "dateFormat" && !new[] { "dmy", "mdy", "ymd" }.Contains(valStr))
            return Task.FromResult<string?>("You need to provide a valid date format");

        return Task.FromResult<string?>(null);
    }

    public async Task ResetToDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM config", cancellationToken);
        await InsertDefaultsAsync(cancellationToken);
    }
}
