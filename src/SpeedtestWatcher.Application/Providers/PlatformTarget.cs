using System.Runtime.InteropServices;

namespace SpeedtestWatcher.Application.Providers;

public sealed record PlatformTarget(OSPlatform OperatingSystem, Architecture Architecture)
{
    public static PlatformTarget Current { get; } = new(CurrentOperatingSystem(), CurrentArchitecture());

    private static OSPlatform CurrentOperatingSystem() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX
        : OSPlatform.Linux;

    private static Architecture CurrentArchitecture() =>
        RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64 or Architecture.Arm or Architecture.X86
            ? RuntimeInformation.ProcessArchitecture
            : Architecture.X64;
}
