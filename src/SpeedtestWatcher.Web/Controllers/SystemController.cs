using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Infrastructure.Network;
using SpeedtestWatcher.Web.Helpers;

namespace SpeedtestWatcher.Web.Controllers;

[ApiController]
[Route("api/info")]
public class SystemController : ControllerBase
{
    private readonly INetworkInterfaceDetector _interfaceDetector;
    private readonly ServerListProvider _serverListProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SystemController(
        INetworkInterfaceDetector interfaceDetector,
        ServerListProvider serverListProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _interfaceDetector = interfaceDetector;
        _serverListProvider = serverListProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet("version")]
    public async Task<IActionResult> GetVersion()
    {
        bool isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        string localVersion = ProjectInfo.Version;
        // There is nothing to compare against until a repository is configured (Project:Repository in appsettings.json).
        string? repository = ProjectInfo.Repository(_configuration);
        if (repository == null || Environment.GetEnvironmentVariable("PREVIEW_MODE") == "true")
            return Ok(new VersionInfoDto { Local = localVersion, Remote = "0" });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "SpeedtestWatcher");

            var response = await client.GetAsync($"https://api.github.com/repos/{repository}/releases/latest");
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                if (doc.RootElement.TryGetProperty("tag_name", out var tag))
                {
                    string tagStr = tag.GetString()?.Replace("v", "") ?? "0";
                    return Ok(new VersionInfoDto { Local = localVersion, Remote = tagStr });
                }
            }
        }
        catch { }

        return Ok(new VersionInfoDto { Local = localVersion, Remote = "0" });
    }

    [HttpGet("server/{provider}")]
    public async Task<IActionResult> GetServers(string provider)
    {
        bool isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        if (provider != "ookla" && provider != "libre")
            return BadRequest(new { message = "Invalid provider" });

        var servers = await _serverListProvider.GetServersAsync(provider);
        return Ok(servers);
    }

    [HttpGet("interfaces")]
    public async Task<IActionResult> GetInterfaces()
    {
        bool isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var ifaces = await _interfaceDetector.GetInterfacesAsync();
        return Ok(ifaces);
    }
}
