namespace SpeedtestWatcher.Core.Settings;

public sealed record AppSettings(
    TargetSettings Targets,
    ScheduleSettings Schedule,
    ProviderSettings Provider,
    PreTestCheckSettings PreTestChecks,
    DisplaySettings Display,
    int RetentionDays,
    SignInSettings SignIn);
