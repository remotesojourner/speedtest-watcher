using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Web.Services.Auth;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/storage")]
public class StorageController : ControllerBase
{
    private readonly ISpeedtestRepository _speedtestRepo;
    private readonly IConfigRepository _configRepo;
    private readonly IIntegrationRepository _integrationRepo;
    private readonly IRecommendationRepository _recommendationRepo;
    private readonly AuthSettings _auth;

    public StorageController(
        ISpeedtestRepository speedtestRepo,
        IConfigRepository configRepo,
        IIntegrationRepository integrationRepo,
        IRecommendationRepository recommendationRepo,
        AuthSettings auth)
    {
        _speedtestRepo = speedtestRepo;
        _configRepo = configRepo;
        _integrationRepo = integrationRepo;
        _recommendationRepo = recommendationRepo;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> GetStorageInfo()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "storage.db");
        long size = 0;
        if (System.IO.File.Exists(dbPath))
        {
            size = new FileInfo(dbPath).Length;
        }

        var count = await _speedtestRepo.CountAsync();
        return Ok(new StorageInfoDto { Size = size, TestCount = count });
    }

    [HttpGet("tests/history/json")]
    public async Task<IActionResult> ExportTestsJson()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var all = await _speedtestRepo.ListAllAsync();
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
        return File(bytes, "application/json", "tests.json");
    }

    [HttpGet("tests/history/csv")]
    public async Task<IActionResult> ExportTestsCsv()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var all = await _speedtestRepo.ListAllAsync();
        var sb = new StringBuilder();
        sb.AppendLine("id,serverId,serverName,serverHost,ping,jitter,download,upload,error,type,resultId,time,created");
        foreach (var t in all)
        {
            sb.AppendLine($"{t.Id},{t.ServerId},\"{t.ServerName}\",\"{t.ServerHost}\",{t.Ping},{t.Jitter},{t.Download},{t.Upload},\"{t.Error}\",{t.Type},{t.ResultId},{t.Time},{t.Created:o}");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "tests.csv");
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
    public async Task<IActionResult> ImportTests([FromBody] List<Speedtest> tests)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        if (tests == null || tests.Count == 0)
            return BadRequest(new { message = "No tests provided" });

        var count = await _speedtestRepo.ImportTestsAsync(tests);
        return Ok(new { message = $"{count} tests imported successfully" });
    }

    [HttpGet("config")]
    public async Task<IActionResult> ExportFullConfig()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var configs = await _configRepo.ListAllAsync();
        var integrations = await _integrationRepo.ListAllAsync();
        var recommendations = await _recommendationRepo.GetAsync();

        return Ok(new
        {
            config = configs.Where(c => !AuthSettings.Keys.Contains(c.Key)),
            integrations,
            recommendations
        });
    }

    [HttpPut("config")]
    public async Task<IActionResult> ImportFullConfig([FromBody] JsonElement element)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        if (element.TryGetProperty("config", out var cfgElem) && cfgElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in cfgElem.EnumerateArray())
            {
                if (item.TryGetProperty("key", out var k) && item.TryGetProperty("value", out var v)
                    && !AuthSettings.Keys.Contains(k.GetString() ?? ""))
                {
                    await _configRepo.UpdateValueAsync(k.GetString()!, v.GetString()!);
                }
            }
        }

        return Ok(new { message = "Configuration imported successfully" });
    }

    [HttpDelete("config")]
    public async Task<IActionResult> FactoryReset()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        await _configRepo.ResetToDefaultsAsync();
        await _integrationRepo.ClearAllAsync();
        await _recommendationRepo.ClearAllAsync();
        await _auth.ReloadAsync();

        return Ok(new { message = "Factory reset completed successfully" });
    }
}
