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
    private readonly ILogger<SystemController> _logger;

    public SystemController(
        INetworkInterfaceDetector interfaceDetector,
        ServerListProvider serverListProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SystemController> logger)
    {
        _interfaceDetector = interfaceDetector;
        _serverListProvider = serverListProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("version")]
    public async Task<IActionResult> GetVersion()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var localVersion = ProjectInfo.Version;
        var repository = ProjectInfo.Repository(_configuration);
        if (repository == null)
            return Ok(new VersionInfoDto { Local = localVersion, Remote = "0" });

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "SpeedtestWatcher");

            using var response = await client.GetAsync($"https://api.github.com/repos/{repository}/releases/latest");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub answered {Status} when checking {Repository} for a newer release", (int)response.StatusCode, repository);
                return Ok(new VersionInfoDto { Local = localVersion, Remote = "0" });
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("tag_name", out var tag))
            {
                var tagStr = tag.GetString()?.Replace("v", "") ?? "0";
                return Ok(new VersionInfoDto { Local = localVersion, Remote = tagStr });
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Could not check {Repository} for a newer release", repository);
        }

        return Ok(new VersionInfoDto { Local = localVersion, Remote = "0" });
    }

    [HttpGet("server/{provider}")]
    public async Task<IActionResult> GetServers(string provider)
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        if (provider != "ookla" && provider != "libre")
            return BadRequest(new { message = "Invalid provider" });

        var servers = await _serverListProvider.GetServersAsync(provider);
        return Ok(servers);
    }

    [HttpGet("interfaces")]
    public async Task<IActionResult> GetInterfaces()
    {
        var isViewMode = HttpContext.Items.TryGetValue("ViewMode", out var vm) && vm is true;
        if (isViewMode) return Unauthorized(new { message = "Authentication required" });

        var ifaces = await _interfaceDetector.GetInterfacesAsync();
        return Ok(ifaces);
    }
}
