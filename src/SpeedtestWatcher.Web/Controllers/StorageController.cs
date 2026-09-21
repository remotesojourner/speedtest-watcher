using System.Text;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Web.Services;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly ISpeedtestRepository _speedtestRepo;
    private readonly ISettingsStore _settingsStore;
    private readonly IIntegrationRepository _integrationRepo;
    private readonly IRecommendationRepository _recommendationRepo;
    private readonly IStorageRepository _storageRepo;
    private readonly SettingsBackup _settingsBackup;
    private readonly AuthSettings _auth;

    public StorageController(
        ISpeedtestRepository speedtestRepo,
        ISettingsStore settingsStore,
        IIntegrationRepository integrationRepo,
        IRecommendationRepository recommendationRepo,
        IStorageRepository storageRepo,
        SettingsBackup settingsBackup,
        AuthSettings auth)
    {
        _speedtestRepo = speedtestRepo;
        _settingsStore = settingsStore;
        _integrationRepo = integrationRepo;
        _recommendationRepo = recommendationRepo;
        _storageRepo = storageRepo;
        _settingsBackup = settingsBackup;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetStorageInfo()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        return Ok(new StorageInfoDto
        {
            Size = await _storageRepo.GetDatabaseSizeAsync(),
            TestCount = await _speedtestRepo.CountAsync()
        });
    }

    [HttpGet("tests/history/json")]
    public async Task<IActionResult> ExportTestsJson()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var all = await _speedtestRepo.ListAllAsync();
        return File(Encoding.UTF8.GetBytes(SpeedtestExport.ToJson(all)), "application/json", "speedtests.json");
    }

    [HttpGet("tests/history/csv")]
    public async Task<IActionResult> ExportTestsCsv()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var all = await _speedtestRepo.ListAllAsync();
        return File(Encoding.UTF8.GetBytes(SpeedtestExport.ToCsv(all)), "text/csv", "speedtests.csv");
    }

    [HttpDelete("tests/history")]
    public async Task<IActionResult> DeleteTestHistory()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        await _speedtestRepo.DeleteAllAsync();
        return Ok(new { message = "Tests cleared" });
    }

    [HttpPut("tests/history")]
    public async Task<IActionResult> ImportTests([FromBody] List<SpeedtestImportRow>? tests)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        if (tests == null || tests.Count == 0)
            return BadRequest(new { message = "No tests provided" });

        var imported = await _speedtestRepo.ImportTestsAsync(tests.Select(row => row.ToSpeedtest()));
        return Ok(new TestImportResultDto { Imported = imported, Skipped = tests.Count - imported });
    }

    [HttpGet("config")]
    public async Task<IActionResult> ExportFullConfig()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        return Ok(await _settingsBackup.ExportAsync());
    }

    [HttpPut("config")]
    public async Task<IActionResult> ImportFullConfig([FromBody] SettingsBackupDto backup)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        return Ok(await _settingsBackup.ImportAsync(backup));
    }

    [HttpDelete("config")]
    public async Task<IActionResult> FactoryReset()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        await _settingsStore.ResetToDefaultsAsync();
        await _integrationRepo.ClearAllAsync();
        await _recommendationRepo.ClearAllAsync();
        await _auth.ReloadAsync();

        return Ok(new { message = "Factory reset completed successfully" });
    }
}
