using System.Net;
using System.Text.Json;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Web.Controllers;

namespace SpeedtestWatcher.Tests;

public class IntegrationsControllerTests
{
    private const string WebhookSettings = """{"url":"https://localhost/hook","send_finished":false}""";

    private readonly RecordingHandler _handler = new();
    private readonly InMemoryIntegrations _integrations = new([]);
    private readonly ISpeedtestRepository _speedtests = A.Fake<ISpeedtestRepository>();

    [Fact]
    public async Task SendTest_UsesTheLatestCompletedResult_WithoutSavingAnything()
    {
        A.CallTo(() => _speedtests.GetLatestCompletedAsync(A<CancellationToken>._))
            .Returns(new Speedtest { ServerName = "Acme Fibre", Ping = 12, Download = 941.25, Upload = 110.5 });

        var response = await SendTestAsync("webhook", WebhookSettings);

        Assert.Equal((true, null), Outcome(response));
        Assert.Contains("Acme Fibre", Assert.Single(_handler.Requests).Body);
        Assert.Empty(_integrations.ActivityErrors);
    }

    [Fact]
    public async Task SendTest_UsesSampleValues_BeforeAnyTestHasCompleted()
    {
        A.CallTo(() => _speedtests.GetLatestCompletedAsync(A<CancellationToken>._)).Returns((Speedtest?)null);

        var response = await SendTestAsync("webhook", WebhookSettings);

        Assert.Equal((true, null), Outcome(response));
        Assert.Contains("Sample server", Assert.Single(_handler.Requests).Body);
    }

    [Fact]
    public async Task SendTest_AnswersWithTheReason_WhenTheServiceRefusesIt()
    {
        _handler.ResponseStatus = HttpStatusCode.NotFound;
        _handler.ResponseBody = "Unknown Webhook";

        var response = await SendTestAsync("discord", """{"url":"https://localhost/discord.com/api/webhooks/1/x"}""");

        var (success, message) = Outcome(response);
        Assert.False(success);
        Assert.Equal("Discord answered HTTP 404: Unknown Webhook", message);
    }

    [Fact]
    public async Task SendTest_IsRefused_InViewMode()
    {
        var response = await SendTestAsync("webhook", WebhookSettings, viewMode: true);

        Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task SendTest_ForAnUnknownType_IsNotFound()
    {
        var response = await SendTestAsync("carrierPigeon", WebhookSettings);

        Assert.IsType<NotFoundObjectResult>(response);
        Assert.Empty(_handler.Requests);
    }

    private Task<IActionResult> SendTestAsync(string name, string settings, bool viewMode = false)
    {
        var httpContext = new DefaultHttpContext();
        if (viewMode) httpContext.Items["ViewMode"] = true;

        var controller = new IntegrationsController(_integrations, TestIntegrations.Dispatcher(_integrations, _handler), _speedtests)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var body = JsonSerializer.Deserialize<Dictionary<string, object?>>(settings)!;
        return controller.SendTest(name, body, "abc123", TestContext.Current.CancellationToken);
    }

    private static (bool Success, string? Message) Outcome(IActionResult response)
    {
        var dto = Assert.IsType<IntegrationTestResultDto>(Assert.IsType<OkObjectResult>(response).Value);
        return (dto.Success, dto.Message);
    }
}
