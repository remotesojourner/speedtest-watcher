using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Services.Interfaces;

public interface ICurrentAccessService
{
    Access Level { get; }

    bool HasFullAccess => Level == Access.Full;
}
