using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Models.Dtos;
using SpeedtestWatcher.Application.Repositories.Interfaces;
using SpeedtestWatcher.Application.Services.Interfaces;

namespace SpeedtestWatcher.Application.Services;

public sealed class StorageService
{
    private readonly ISpeedtestRepository _results;
    private readonly IStorageRepository _storage;
    private readonly ISettingsRepository _settings;
    private readonly IIntegrationRepository _integrations;
    private readonly IRecommendationRepository _recommendations;
    private readonly ISignInStateService _signIn;
    private readonly ICurrentAccessService _access;

    public StorageService(
        ISpeedtestRepository results,
        IStorageRepository storage,
        ISettingsRepository settings,
        IIntegrationRepository integrations,
        IRecommendationRepository recommendations,
        ISignInStateService signIn,
        ICurrentAccessService access)
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
