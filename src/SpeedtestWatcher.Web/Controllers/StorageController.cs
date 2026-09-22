using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Web.Api;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly StorageService _storage;
    private readonly ResultsService _results;
    private readonly SettingsBackupService _backup;

    public StorageController(StorageService storage, ResultsService results, SettingsBackupService backup)
    {
        _storage = storage;
        _results = results;
        _backup = backup;
    }

    [HttpGet]
    public async Task<IActionResult> GetStorageInfo(CancellationToken cancellationToken) =>
        (await _storage.GetInfoAsync(cancellationToken)).ToActionResult();

    [HttpGet("tests/history/json")]
    public Task<IActionResult> ExportTestsJson(CancellationToken cancellationToken) => ExportAsync("json", cancellationToken);

    [HttpGet("tests/history/csv")]
    public Task<IActionResult> ExportTestsCsv(CancellationToken cancellationToken) => ExportAsync("csv", cancellationToken);

    [HttpDelete("tests/history")]
    public async Task<IActionResult> DeleteTestHistory(CancellationToken cancellationToken) =>
        (await _storage.DeleteAllResultsAsync(cancellationToken)).ToActionResult("Tests cleared");

    [HttpPut("tests/history")]
    public async Task<IActionResult> ImportTests([FromBody] List<SpeedtestImportRow>? tests, CancellationToken cancellationToken) =>
        (await _results.ImportAsync(tests, cancellationToken)).ToActionResult();

    [HttpGet("config")]
    public async Task<IActionResult> ExportFullConfig(CancellationToken cancellationToken) =>
        (await _backup.ExportAsync(cancellationToken)).ToActionResult();

    [HttpPut("config")]
    public async Task<IActionResult> ImportFullConfig([FromBody] SettingsBackupDto backup, CancellationToken cancellationToken) =>
        (await _backup.ImportAsync(backup, cancellationToken)).ToActionResult();

    [HttpDelete("config")]
    public async Task<IActionResult> FactoryReset(CancellationToken cancellationToken) =>
        (await _storage.FactoryResetAsync(cancellationToken)).ToActionResult("Factory reset completed successfully");

    private async Task<IActionResult> ExportAsync(string format, CancellationToken cancellationToken) =>
        (await _storage.ExportResultsAsync(format, cancellationToken)).ToActionResult(file => File(file.Content, file.ContentType, file.FileName));
}
