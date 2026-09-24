using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Application.Settings;
using SpeedtestWatcher.Application.Storage;
using SpeedtestWatcher.Web.Api.Contracts;

namespace SpeedtestWatcher.Web.Api;

[ApiController]
[Route("api/storage")]
[Tags("Storage")]
[Produces("application/json")]
public class StorageController : ControllerBase
{
    private const long MaxDataBackupBytes = 256 * 1024 * 1024;

    private readonly StorageService _storage;
    private readonly DataBackupService _data;
    private readonly SettingsBackupService _backup;

    public StorageController(StorageService storage, DataBackupService data, SettingsBackupService backup)
    {
        _storage = storage;
        _data = data;
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
    /// Export a data backup
    /// </summary>
    /// <remarks>
    /// Downloads <c>speedtest-watcher-data.json</c>: every result, and the connection monitoring history (outages, watch sessions and probe rounds). It can be imported again with <c>PUT /api/storage/data</c>.
    /// </remarks>
    /// <response code="200">The file.</response>
    [HttpGet("data")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> ExportData(CancellationToken cancellationToken) =>
        (await _data.ExportAsync(cancellationToken)).ToFileResult();

    /// <summary>
    /// Restore a data backup
    /// </summary>
    /// <remarks>
    /// Adds every result and every piece of connection monitoring history from a backup. Ids in the file are ignored, an entry whose timestamp is already stored is skipped, and an outage still open when the backup was made is skipped too, so importing the same file twice is safe.
    /// </remarks>
    /// <param name="backup">A backup from <c>GET /api/storage/data</c> or the Storage tab. A results-only file wraps as <c>{ "speedtests": [...] }</c>.</param>
    /// <response code="200">How much was imported and how much was skipped.</response>
    /// <response code="400">The backup is empty, isn't valid, or was made by a newer version.</response>
    [HttpPut("data")]
    [Consumes("application/json")]
    [RequestSizeLimit(MaxDataBackupBytes)]
    [ProducesResponseType<DataImportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DataImportResponse>> ImportData([FromBody] DataBackup backup, CancellationToken cancellationToken) =>
        (await _data.ImportAsync(backup.ToDto(), cancellationToken)).ToActionResult(DataImportResponse.From);

    /// <summary>
    /// Delete all data
    /// </summary>
    /// <remarks>
    /// Deletes every result, outage, watch session and probe round. Settings, integrations and recommendations stay. This can't be undone.
    /// </remarks>
    /// <response code="200">All data was deleted.</response>
    [HttpDelete("data")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageResponse>> DeleteData(CancellationToken cancellationToken) =>
        (await _data.DeleteAllAsync(cancellationToken)).ToActionResult("Data deleted");

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
}
