using System.Globalization;
using Microsoft.Extensions.Options;
using SpeedtestWatcher.Application.Common;

namespace SpeedtestWatcher.Web.Startup;

public sealed class SpeedtestWatcherOptionsSetup : IConfigureOptions<SpeedtestWatcherOptions>
{
    public const string PortVariable = "PORT";
    public const string DataDirectoryVariable = "DATA_DIRECTORY";
    public const string BinDirectoryVariable = "BIN_DIRECTORY";
    public const string DisableAuthVariable = "DISABLE_AUTH";
    public const string RunTestOnStartupVariable = "RUN_TEST_ON_STARTUP";

    private readonly IConfiguration _configuration;

    public SpeedtestWatcherOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static SpeedtestWatcherOptions Read(IConfiguration configuration)
    {
        var options = new SpeedtestWatcherOptions();
        new SpeedtestWatcherOptionsSetup(configuration).Configure(options);
        return options;
    }

    public void Configure(SpeedtestWatcherOptions options)
    {
        if (int.TryParse(Value(PortVariable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)) options.Port = port;
        options.DataDirectory = Path.GetFullPath(Value(DataDirectoryVariable) ?? options.DataDirectory);
        options.BinDirectory = Path.GetFullPath(Value(BinDirectoryVariable) ?? options.BinDirectory);
        options.DisableAuth = IsSwitchedOn(Value(DisableAuthVariable));
        options.RunTestOnStartup = IsSwitchedOn(Value(RunTestOnStartupVariable));
    }

    private string? Value(string variable) =>
        _configuration[variable] is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static bool IsSwitchedOn(string? value) =>
        value?.ToLowerInvariant() is "true" or "1" or "yes";
}
