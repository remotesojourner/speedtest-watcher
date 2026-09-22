using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.Web.Api;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/storage")]
[Tags("Storage")]
[Produces("application/json")]
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

    /// <summary>
    /// Get storage information
    /// </summary>
    /// <response code="200">The database size and how many results are stored.</response>
    [HttpGet]
    [ProducesResponseType<StorageInfoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageInfoResponse>> GetStorageInfo(CancellationToken cancellationToken) =>
        (await _storage.GetInfoAsync(cancellationToken)).ToActionResult(StorageInfoResponse.From);

    /// <summary>
    /// Export every result as JSON
    /// </summary>
    /// <remarks>
    /// Downloads <c>speedtests.json</c>, oldest first. It can be imported again with <c>PUT /api/storage/tests/history</c>.
    /// </remarks>
    /// <response code="200">The file.</response>
    [HttpGet("tests/history/json")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> ExportTestsJson(CancellationToken cancellationToken) => ExportAsync("json", cancellationToken);

    /// <summary>
    /// Export every result as CSV
    /// </summary>
    /// <remarks>
    /// Downloads <c>speedtests.csv</c>, oldest first. Text that a spreadsheet would run as a formula is escaped.
    /// </remarks>
    /// <response code="200">The file.</response>
    [HttpGet("tests/history/csv")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "text/csv")]
    public Task<IActionResult> ExportTestsCsv(CancellationToken cancellationToken) => ExportAsync("csv", cancellationToken);

    /// <summary>
    /// Import results
    /// </summary>
    /// <remarks>
    /// Adds results from a JSON export. Ids in the file are ignored, and results whose timestamp is already stored are skipped, so importing the same file twice is safe.
    /// </remarks>
    /// <param name="tests">The results, in the format of a JSON export.</param>
    /// <response code="200">How many results were imported and skipped.</response>
    /// <response code="400">The list is empty or isn't valid.</response>
    [HttpPut("tests/history")]
    [Consumes("application/json")]
    [ProducesResponseType<TestImportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TestImportResponse>> ImportTests([FromBody] List<TestImportRow>? tests, CancellationToken cancellationToken) =>
        (await _results.ImportAsync(tests?.Select(test => test.ToImportRow()).ToList(), cancellationToken)).ToActionResult(TestImportResponse.From);

    /// <summary>
    /// Delete every result
    /// </summary>
    /// <remarks>
    /// Clears the history. Settings, integrations and recommendations stay. This can't be undone.
    /// </remarks>
    /// <response code="200">Every result was deleted.</response>
    [HttpDelete("tests/history")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> DeleteTestHistory(CancellationToken cancellationToken) =>
        (await _storage.DeleteAllResultsAsync(cancellationToken)).ToActionResult("Tests cleared");

    /// <summary>
    /// Export a settings backup
    /// </summary>
    /// <remarks>
    /// Returns the settings, integrations and recommendations in the format of the Storage tab's backup file. Sign-in settings are never included.
    /// </remarks>
    /// <response code="200">The backup.</response>
    [HttpGet("config")]
    [ProducesResponseType<SettingsBackup>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsBackup>> ExportFullConfig(CancellationToken cancellationToken) =>
        (await _backup.ExportAsync(cancellationToken)).ToActionResult(SettingsBackup.From);

    /// <summary>
    /// Restore a settings backup
    /// </summary>
    /// <remarks>
    /// Saves the valid settings together, adds or replaces integrations by id, and restores the recommendations when all three values are positive. Anything that isn't valid, and any sign-in setting, is skipped and counted.
    /// </remarks>
    /// <param name="backup">A backup from <c>GET /api/storage/config</c> or the Storage tab.</param>
    /// <response code="200">What was restored and how much was skipped.</response>
    /// <response code="400">The request body isn't valid.</response>
    [HttpPut("config")]
    [Consumes("application/json")]
    [ProducesResponseType<SettingsImportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SettingsImportResponse>> ImportFullConfig([FromBody] SettingsBackup backup, CancellationToken cancellationToken) =>
        (await _backup.ImportAsync(backup.ToDto(), cancellationToken)).ToActionResult(SettingsImportResponse.From);

    private async Task<IActionResult> ExportAsync(string format, CancellationToken cancellationToken) =>
        (await _storage.ExportResultsAsync(format, cancellationToken)).ToFileResult();
}
