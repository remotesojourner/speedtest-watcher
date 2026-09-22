using SpeedtestWatcher.Application.Security;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;

namespace SpeedtestWatcher.Application.Storage;

public sealed class StorageService
{
    private readonly ISpeedtestRepository _results;
    private readonly IStorageRepository _storage;
    private readonly ISettingsStore _settings;
    private readonly IIntegrationRepository _integrations;
    private readonly IRecommendationRepository _recommendations;
    private readonly ISignInState _signIn;
    private readonly ICurrentAccess _access;

    public StorageService(
        ISpeedtestRepository results,
        IStorageRepository storage,
        ISettingsStore settings,
        IIntegrationRepository integrations,
        IRecommendationRepository recommendations,
        ISignInState signIn,
        ICurrentAccess access)
    {
        _results = results;
        _storage = storage;
        _settings = settings;
        _integrations = integrations;
        _recommendations = recommendations;
        _signIn = signIn;
        _access = access;
    }

    public async Task<OperationResult<StorageInfoDto>> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return OperationResult.Ok(new StorageInfoDto
        {
            Size = await _storage.GetDatabaseSizeAsync(cancellationToken),
            TestCount = await _results.CountAsync(cancellationToken)
        });
    }

    public async Task<OperationResult<ExportFile>> ExportResultsAsync(string format, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return OperationResult.Ok(ResultsService.Export(await _results.ListAllAsync(cancellationToken), format));
    }

    public async Task<OperationResult> DeleteAllResultsAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        await _results.DeleteAllAsync(cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> FactoryResetAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        await _settings.ResetToDefaultsAsync(cancellationToken);
        await _integrations.ClearAllAsync(cancellationToken);
        await _recommendations.ClearAllAsync(cancellationToken);
        await _signIn.ReloadAsync(cancellationToken);
        return OperationResult.Ok();
    }
}
