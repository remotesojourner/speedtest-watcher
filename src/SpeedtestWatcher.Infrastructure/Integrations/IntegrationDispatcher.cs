using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Enums;
using SpeedtestWatcher.Core.Helpers;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;

namespace SpeedtestWatcher.Infrastructure.Integrations;

public class IntegrationDispatcher : IIntegrationDispatcher
{
    private readonly IIntegrationRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IntegrationDispatcher> _logger;
    private readonly ConcurrentDictionary<string, long> _lastPings = new();

    public IntegrationDispatcher(
        IIntegrationRepository repository,
        IHttpClientFactory httpClientFactory,
        ILogger<IntegrationDispatcher> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Dictionary<string, IntegrationTypeSchemaDto> GetRegisteredIntegrationSchemas()
    {
        return new Dictionary<string, IntegrationTypeSchemaDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["discord"] = new IntegrationTypeSchemaDto
            {
                Name = "discord",
                Title = "Discord",
                Description = "Send test results and alerts via Discord webhooks",
                Fields =
                [
                    new() { Name = "url", Type = "text", Required = true, Regex = @"https://.*discord\.com/api/webhooks/\d+/.+", Placeholder = "Webhook URL" },
                    new() { Name = "display_name", Type = "text", Required = false, Placeholder = "Speedtest Watcher" },
                    new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
                    new() { Name = "finished_message", Type = "textarea", Required = false },
                    new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
                    new() { Name = "error_message", Type = "textarea", Required = false },
                    new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
                    new() { Name = "unhealthy_message", Type = "textarea", Required = false },
                    new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
                    new() { Name = "skipped_message", Type = "textarea", Required = false }
                ]
            },
            ["telegram"] = new IntegrationTypeSchemaDto
            {
                Name = "telegram",
                Title = "Telegram",
                Description = "Send test results via Telegram bot",
                Fields =
                [
                    new() { Name = "token", Type = "text", Required = true, Regex = @"(\d+):[a-zA-Z0-9_-]+", Placeholder = "Bot Token" },
                    new() { Name = "chat_id", Type = "text", Required = true, Regex = @"\d+", Placeholder = "Chat ID" },
                    new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
                    new() { Name = "finished_message", Type = "textarea", Required = false },
                    new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
                    new() { Name = "error_message", Type = "textarea", Required = false },
                    new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
                    new() { Name = "unhealthy_message", Type = "textarea", Required = false },
                    new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
                    new() { Name = "skipped_message", Type = "textarea", Required = false }
                ]
            },
            ["gotify"] = new IntegrationTypeSchemaDto
            {
                Name = "gotify",
                Title = "Gotify",
                Description = "Send notifications to a Gotify server",
                Fields =
                [
                    new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://gotify.example.com" },
                    new() { Name = "key", Type = "text", Required = true, Regex = @"^.{15}$", Placeholder = "App Token" },
                    new() { Name = "priority", Type = "text", Required = true, Regex = @"^[0-9]$", Default = "5" },
                    new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
                    new() { Name = "finished_message", Type = "textarea", Required = false },
                    new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
                    new() { Name = "error_message", Type = "textarea", Required = false },
                    new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
                    new() { Name = "unhealthy_message", Type = "textarea", Required = false },
                    new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
                    new() { Name = "skipped_message", Type = "textarea", Required = false }
                ]
            },
            ["ntfy"] = new IntegrationTypeSchemaDto
            {
                Name = "ntfy",
                Title = "ntfy",
                Description = "Send push alerts via ntfy.sh or custom server",
                Fields =
                [
                    new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+", Default = "https://ntfy.sh" },
                    new() { Name = "topic", Type = "text", Required = true, Regex = @"^[A-Za-z0-9_\-]{1,64}$", Placeholder = "speedtest-watcher-alerts" },
                    new() { Name = "token", Type = "text", Required = false },
                    new() { Name = "title", Type = "text", Required = false, Default = "Speedtest Watcher Alert" },
                    new() { Name = "tags", Type = "text", Required = false },
                    new() { Name = "priority", Type = "text", Required = false, Regex = @"^[1-5]$", Default = "3" },
                    new() { Name = "error_priority", Type = "text", Required = false, Regex = @"^[1-5]$", Default = "5" },
                    new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
                    new() { Name = "finished_message", Type = "textarea", Required = false },
                    new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
                    new() { Name = "error_message", Type = "textarea", Required = false },
                    new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
                    new() { Name = "unhealthy_message", Type = "textarea", Required = false },
                    new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
                    new() { Name = "skipped_message", Type = "textarea", Required = false }
                ]
            },
            ["pushover"] = new IntegrationTypeSchemaDto
            {
                Name = "pushover",
                Title = "Pushover",
                Description = "Send push notifications via Pushover API",
                Fields =
                [
                    new() { Name = "token", Type = "text", Required = true, Regex = @"^[a-z0-9]{30}$", Placeholder = "API Token" },
                    new() { Name = "user_key", Type = "text", Required = true, Regex = @"^[a-z0-9]{30}$", Placeholder = "User Key" },
                    new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
                    new() { Name = "finished_message", Type = "textarea", Required = false },
                    new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
                    new() { Name = "error_message", Type = "textarea", Required = false },
                    new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
                    new() { Name = "unhealthy_message", Type = "textarea", Required = false },
                    new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
                    new() { Name = "skipped_message", Type = "textarea", Required = false }
                ]
            },
            ["webhook"] = new IntegrationTypeSchemaDto
            {
                Name = "webhook",
                Title = "Webhook",
                Description = "Generic JSON webhook for custom integrations",
                Fields =
                [
                    new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://example.com/webhook" },
                    new() { Name = "send_started", Type = "boolean", Required = false, Default = false },
                    new() { Name = "send_finished", Type = "boolean", Required = false, Default = true },
                    new() { Name = "send_alive", Type = "boolean", Required = false, Default = false },
                    new() { Name = "send_failed", Type = "boolean", Required = false, Default = true },
                    new() { Name = "send_recommendations", Type = "boolean", Required = false, Default = false },
                    new() { Name = "send_unhealthy", Type = "boolean", Required = false, Default = true },
                    new() { Name = "send_skipped", Type = "boolean", Required = false, Default = true },
                    new() { Name = "send_config_updates", Type = "boolean", Required = false, Default = false },
                    new() { Name = "interval", Type = "number", Required = false, Default = 1 }
                ]
            },
            ["healthChecks"] = new IntegrationTypeSchemaDto
            {
                Name = "healthChecks",
                Title = "Healthchecks.io",
                Description = "Send heartbeats and test results to Healthchecks.io",
                Fields =
                [
                    new() { Name = "url", Type = "text", Required = true, Regex = @"https?://.+", Placeholder = "https://hc-ping.com/uuid" },
                    new() { Name = "interval", Type = "number", Required = false, Default = 1 }
                ]
            },
            ["influxdb"] = new IntegrationTypeSchemaDto
            {
                Name = "influxdb",
                Title = "InfluxDB v2",
                Description = "Export metrics to InfluxDB v2 via Line Protocol",
                Fields =
                [
                    new() { Name = "url", Type = "text", Required = true, Regex = @"^https?://.+", Placeholder = "http://localhost:8086" },
                    new() { Name = "org", Type = "text", Required = true, Placeholder = "organization" },
                    new() { Name = "bucket", Type = "text", Required = true, Placeholder = "bucket" },
                    new() { Name = "token", Type = "text", Required = true, Placeholder = "api-token" },
                    new() { Name = "measurement", Type = "text", Required = false, Default = "speedtests" },
                    new() { Name = "host", Type = "text", Required = false },
                    new() { Name = "tags", Type = "text", Required = false, Placeholder = "env=prod,server=node1" }
                ]
            }
        };
    }

    public async Task TriggerEventAsync(IntegrationEvent eventType, object? eventData, CancellationToken cancellationToken = default)
    {
        var activeIntegrations = await _repository.ListAllAsync(cancellationToken);
        if (activeIntegrations.Count == 0) return;

        foreach (var integration in activeIntegrations)
        {
            if (ShouldThrottle(eventType, integration))
                continue;

            try
            {
                var config = ParseConfig(integration.Data);
                var success = await DispatchToProviderAsync(integration.Name, eventType, config, eventData, cancellationToken);
                await _repository.UpdateActivityAsync(integration.Id, !success, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch integration {Name} ({Id})", integration.Name, integration.Id);
                await _repository.UpdateActivityAsync(integration.Id, true, cancellationToken);
            }
        }
    }

    private bool ShouldThrottle(IntegrationEvent eventType, IntegrationData integration)
    {
        if (eventType != IntegrationEvent.MinutePassed) return false;

        var config = ParseConfig(integration.Data);
        var interval = 1;
        if (config.TryGetValue("interval", out var intObj))
        {
            if (intObj is JsonElement je && je.TryGetInt32(out var i)) interval = Math.Max(1, i);
            else if (int.TryParse(intObj?.ToString(), out var parsed)) interval = Math.Max(1, parsed);
        }

        if (interval <= 1) return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (_lastPings.TryGetValue(integration.Id, out var lastPing))
        {
            long threshold = interval * 60 * 1000 - 30 * 1000;
            if (now - lastPing < threshold)
                return true;
        }

        _lastPings[integration.Id] = now;
        return false;
    }

    private static Dictionary<string, object?> ParseConfig(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task<bool> DispatchToProviderAsync(
        string providerName,
        IntegrationEvent eventType,
        Dictionary<string, object?> config,
        object? eventData,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        var templateVars = ExtractVariables(eventData);

        switch (providerName.ToLowerInvariant())
        {
            case "discord":
                return await DispatchDiscordAsync(client, eventType, config, templateVars, cancellationToken);
            case "telegram":
                return await DispatchTelegramAsync(client, eventType, config, templateVars, cancellationToken);
            case "gotify":
                return await DispatchGotifyAsync(client, eventType, config, templateVars, cancellationToken);
            case "ntfy":
                return await DispatchNtfyAsync(client, eventType, config, templateVars, cancellationToken);
            case "pushover":
                return await DispatchPushoverAsync(client, eventType, config, templateVars, cancellationToken);
            case "webhook":
                return await DispatchWebhookAsync(client, eventType, config, eventData, cancellationToken);
            case "healthchecks":
                return await DispatchHealthChecksAsync(client, eventType, config, eventData, cancellationToken);
            case "influxdb":
                return await DispatchInfluxDbAsync(client, eventType, config, eventData, cancellationToken);
            default:
                return false;
        }
    }

    private static Dictionary<string, string> ExtractVariables(object? data)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (data == null) return vars;

        if (data is Speedtest st)
        {
            vars["ping"] = st.Ping.ToString();
            vars["jitter"] = st.Jitter?.ToString("F2") ?? "0";
            vars["download"] = st.Download.ToString("F2");
            vars["upload"] = st.Upload.ToString("F2");
            vars["error"] = st.Error ?? string.Empty;
            vars["status"] = st.Status;
            vars["healthy"] = st.Healthy switch { true => "yes", false => "no", null => "unknown" };
            vars["server"] = st.ServerName ?? string.Empty;
            vars["threshold_ping"] = st.ThresholdPing?.ToString() ?? "-";
            vars["threshold_download"] = st.ThresholdDownload?.ToString("F2") ?? "-";
            vars["threshold_upload"] = st.ThresholdUpload?.ToString("F2") ?? "-";
        }
        else if (data is SpeedtestExecutionResult res)
        {
            vars["ping"] = res.Ping.ToString();
            vars["jitter"] = res.Jitter?.ToString("F2") ?? "0";
            vars["download"] = res.Download.ToString("F2");
            vars["upload"] = res.Upload.ToString("F2");
            vars["error"] = res.Error ?? string.Empty;
        }
        else if (data is string err)
        {
            vars["error"] = err;
        }

        return vars;
    }

    private static bool GetBool(Dictionary<string, object?> config, string key, bool def = false)
    {
        if (!config.TryGetValue(key, out var val) || val == null) return def;
        if (val is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.True) return true;
            if (je.ValueKind == JsonValueKind.False) return false;
        }
        return bool.TryParse(val.ToString(), out var b) ? b : def;
    }

    private static string GetString(Dictionary<string, object?> config, string key, string def = "")
    {
        if (!config.TryGetValue(key, out var val) || val == null) return def;
        return val.ToString() ?? def;
    }

    private async Task<bool> DispatchDiscordAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, Dictionary<string, string> vars, CancellationToken ct)
    {
        var url = GetString(config, "url");
        if (string.IsNullOrEmpty(url)) return false;

        var username = GetString(config, "display_name", "Speedtest Watcher");

        string description;
        int color;
        if (eventType == IntegrationEvent.TestFinished && GetBool(config, "send_finished", true))
        {
            const string defMsg = ":sparkles: **A speedtest is finished**\n > :ping_pong: `Ping`: %ping% ms (±%jitter% ms)\n > :arrow_up: `Upload`: %upload% Mbps\n > :arrow_down: `Download`: %download% Mbps";
            description = TemplateHelper.ReplaceVariables(GetString(config, "finished_message", defMsg), vars);
            color = 4572762;
        }
        else if (eventType == IntegrationEvent.TestFailed && GetBool(config, "send_failed", true))
        {
            const string defMsg = ":x: **A speedtest has failed**\n > `Reason`: %error%";
            description = TemplateHelper.ReplaceVariables(GetString(config, "error_message", defMsg), vars);
            color = 12993861;
        }
        else if (eventType == IntegrationEvent.TestUnhealthy && GetBool(config, "send_unhealthy", true))
        {
            const string defMsg = ":warning: **A speedtest missed your targets**\n > :ping_pong: `Ping`: %ping% ms (target %threshold_ping% ms)\n > :arrow_down: `Download`: %download% Mbps (target %threshold_download% Mbps)\n > :arrow_up: `Upload`: %upload% Mbps (target %threshold_upload% Mbps)";
            description = TemplateHelper.ReplaceVariables(GetString(config, "unhealthy_message", defMsg), vars);
            color = 16098851;
        }
        else if (eventType == IntegrationEvent.TestSkipped && GetBool(config, "send_skipped", true))
        {
            const string defMsg = ":fast_forward: **A speedtest was skipped**\n > `Reason`: %error%";
            description = TemplateHelper.ReplaceVariables(GetString(config, "skipped_message", defMsg), vars);
            color = 9807270;
        }
        else
        {
            return true;
        }

        var payload = new
        {
            username,
            embeds = new[]
            {
                new { description, color, footer = new { text = "Speedtest Watcher" }, timestamp = DateTime.UtcNow.ToString("o") }
            }
        };

        var res = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchTelegramAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, Dictionary<string, string> vars, CancellationToken ct)
    {
        var token = GetString(config, "token");
        var chatId = GetString(config, "chat_id");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId)) return false;

        var text = "";
        if (eventType == IntegrationEvent.TestFinished && GetBool(config, "send_finished", true))
        {
            const string defMsg = "✨ *A speedtest is finished*\n🏓 `Ping`: %ping% ms (±%jitter% ms)\n🔼 `Upload`: %upload% Mbps\n🔽 `Download`: %download% Mbps";
            text = TemplateHelper.ReplaceVariables(GetString(config, "finished_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestFailed && GetBool(config, "send_failed", true))
        {
            const string defMsg = "❌ *A speedtest has failed*\n`Reason`: %error%";
            text = TemplateHelper.ReplaceVariables(GetString(config, "error_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestUnhealthy && GetBool(config, "send_unhealthy", true))
        {
            const string defMsg = "⚠️ *A speedtest missed your targets*\n🏓 `Ping`: %ping% ms (target %threshold_ping% ms)\n🔽 `Download`: %download% Mbps (target %threshold_download% Mbps)\n🔼 `Upload`: %upload% Mbps (target %threshold_upload% Mbps)";
            text = TemplateHelper.ReplaceVariables(GetString(config, "unhealthy_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestSkipped && GetBool(config, "send_skipped", true))
        {
            const string defMsg = "⏭️ *A speedtest was skipped*\n`Reason`: %error%";
            text = TemplateHelper.ReplaceVariables(GetString(config, "skipped_message", defMsg), vars);
        }
        else
        {
            return true;
        }

        var payload = new { chat_id = chatId, text, parse_mode = "markdown" };
        var res = await client.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchGotifyAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, Dictionary<string, string> vars, CancellationToken ct)
    {
        var url = GetString(config, "url").TrimEnd('/');
        var key = GetString(config, "key");
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key)) return false;

        var message = "";
        var priority = int.TryParse(GetString(config, "priority", "5"), out var p) ? p : 5;

        if (eventType == IntegrationEvent.TestFinished && GetBool(config, "send_finished", true))
        {
            const string defMsg = "A speedtest is finished:\nPing: %ping% ms (±%jitter% ms)\nUpload: %upload% Mbps\nDownload: %download% Mbps";
            message = TemplateHelper.ReplaceVariables(GetString(config, "finished_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestFailed && GetBool(config, "send_failed", true))
        {
            const string defMsg = "A speedtest has failed. Reason: %error%";
            message = TemplateHelper.ReplaceVariables(GetString(config, "error_message", defMsg), vars);
            priority = 8;
        }
        else if (eventType == IntegrationEvent.TestUnhealthy && GetBool(config, "send_unhealthy", true))
        {
            const string defMsg = "A speedtest missed your targets:\nPing: %ping% ms (target %threshold_ping% ms)\nDownload: %download% Mbps (target %threshold_download% Mbps)\nUpload: %upload% Mbps (target %threshold_upload% Mbps)";
            message = TemplateHelper.ReplaceVariables(GetString(config, "unhealthy_message", defMsg), vars);
            priority = 7;
        }
        else if (eventType == IntegrationEvent.TestSkipped && GetBool(config, "send_skipped", true))
        {
            const string defMsg = "A speedtest was skipped. Reason: %error%";
            message = TemplateHelper.ReplaceVariables(GetString(config, "skipped_message", defMsg), vars);
        }
        else
        {
            return true;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/message")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { message, priority }), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {key}");

        var res = await client.SendAsync(request, ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchNtfyAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, Dictionary<string, string> vars, CancellationToken ct)
    {
        var url = GetString(config, "url", "https://ntfy.sh").TrimEnd('/');
        var topic = GetString(config, "topic");
        if (string.IsNullOrEmpty(topic)) return false;

        var message = "";
        var priority = GetString(config, "priority", "3");

        if (eventType == IntegrationEvent.TestFinished && GetBool(config, "send_finished", true))
        {
            const string defMsg = "A speedtest is finished:\nPing: %ping% ms (±%jitter% ms)\nUpload: %upload% Mbps\nDownload: %download% Mbps";
            message = TemplateHelper.ReplaceVariables(GetString(config, "finished_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestFailed && GetBool(config, "send_failed", true))
        {
            const string defMsg = "A speedtest has failed. Reason: %error%";
            message = TemplateHelper.ReplaceVariables(GetString(config, "error_message", defMsg), vars);
            priority = GetString(config, "error_priority", "5");
        }
        else if (eventType == IntegrationEvent.TestUnhealthy && GetBool(config, "send_unhealthy", true))
        {
            const string defMsg = "A speedtest missed your targets:\nPing: %ping% ms (target %threshold_ping% ms)\nDownload: %download% Mbps (target %threshold_download% Mbps)\nUpload: %upload% Mbps (target %threshold_upload% Mbps)";
            message = TemplateHelper.ReplaceVariables(GetString(config, "unhealthy_message", defMsg), vars);
            priority = GetString(config, "error_priority", "5");
        }
        else if (eventType == IntegrationEvent.TestSkipped && GetBool(config, "send_skipped", true))
        {
            const string defMsg = "A speedtest was skipped. Reason: %error%";
            message = TemplateHelper.ReplaceVariables(GetString(config, "skipped_message", defMsg), vars);
        }
        else
        {
            return true;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"{url}/{topic}")
        {
            Content = new StringContent(message, Encoding.UTF8, "text/plain")
        };

        request.Headers.Add("Priority", priority);
        var title = GetString(config, "title");
        if (!string.IsNullOrEmpty(title)) request.Headers.Add("Title", title);
        var tags = GetString(config, "tags");
        if (!string.IsNullOrEmpty(tags)) request.Headers.Add("Tags", tags);
        var token = GetString(config, "token");
        if (!string.IsNullOrEmpty(token)) request.Headers.Add("Authorization", $"Bearer {token}");

        var res = await client.SendAsync(request, ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchPushoverAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, Dictionary<string, string> vars, CancellationToken ct)
    {
        var token = GetString(config, "token");
        var userKey = GetString(config, "user_key");
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userKey)) return false;

        var message = "";
        if (eventType == IntegrationEvent.TestFinished && GetBool(config, "send_finished", true))
        {
            const string defMsg = "A speedtest is finished:\nPing: %ping% ms (±%jitter% ms)\nUpload: %upload% Mbps\nDownload: %download% Mbps";
            message = TemplateHelper.ReplaceVariables(GetString(config, "finished_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestFailed && GetBool(config, "send_failed", true))
        {
            const string defMsg = "A speedtest has failed. Reason: %error%";
            message = TemplateHelper.ReplaceVariables(GetString(config, "error_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestUnhealthy && GetBool(config, "send_unhealthy", true))
        {
            const string defMsg = "A speedtest missed your targets:\nPing: %ping% ms (target %threshold_ping% ms)\nDownload: %download% Mbps (target %threshold_download% Mbps)\nUpload: %upload% Mbps (target %threshold_upload% Mbps)";
            message = TemplateHelper.ReplaceVariables(GetString(config, "unhealthy_message", defMsg), vars);
        }
        else if (eventType == IntegrationEvent.TestSkipped && GetBool(config, "send_skipped", true))
        {
            const string defMsg = "A speedtest was skipped. Reason: %error%";
            message = TemplateHelper.ReplaceVariables(GetString(config, "skipped_message", defMsg), vars);
        }
        else
        {
            return true;
        }

        var payload = new { token, user = userKey, message };
        var res = await client.PostAsync("https://api.pushover.net/1/messages.json", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchWebhookAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, object? eventData, CancellationToken ct)
    {
        var url = GetString(config, "url");
        if (string.IsNullOrEmpty(url)) return false;

        var typeStr = eventType switch
        {
            IntegrationEvent.TestStarted when GetBool(config, "send_started") => "TEST_STARTED",
            IntegrationEvent.MinutePassed when GetBool(config, "send_alive") => "KEEP_ALIVE",
            IntegrationEvent.TestFinished when GetBool(config, "send_finished", true) => "TEST_FINISHED",
            IntegrationEvent.TestFailed when GetBool(config, "send_failed", true) => "TEST_FAILED",
            IntegrationEvent.TestUnhealthy when GetBool(config, "send_unhealthy", true) => "TEST_UNHEALTHY",
            IntegrationEvent.TestSkipped when GetBool(config, "send_skipped", true) => "TEST_SKIPPED",
            IntegrationEvent.RecommendationsUpdated when GetBool(config, "send_recommendations") => "RECOMMENDATIONS_UPDATED",
            IntegrationEvent.ConfigUpdated when GetBool(config, "send_config_updates") => "CONFIG_UPDATED",
            _ => null
        };

        if (typeStr == null) return true;

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { @event = typeStr, data = eventData }), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("User-Agent", "SpeedtestWatcher/WebhookAgent");

        var res = await client.SendAsync(request, ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchHealthChecksAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, object? eventData, CancellationToken ct)
    {
        var url = GetString(config, "url").TrimEnd('/');
        if (string.IsNullOrEmpty(url)) return false;

        var path = eventType switch
        {
            IntegrationEvent.TestStarted => $"{url}/start",
            IntegrationEvent.TestFailed => $"{url}/fail",
            IntegrationEvent.TestSkipped => $"{url}/log",
            _ => url
        };

        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(eventData ?? new { }), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("User-Agent", "SpeedtestWatcher/HealthAgent");

        var res = await client.SendAsync(request, ct);
        return res.IsSuccessStatusCode;
    }

    private async Task<bool> DispatchInfluxDbAsync(HttpClient client, IntegrationEvent eventType, Dictionary<string, object?> config, object? eventData, CancellationToken ct)
    {
        if (eventType != IntegrationEvent.TestFinished) return true;
        if (eventData is not Speedtest && eventData is not SpeedtestExecutionResult) return true;

        var url = GetString(config, "url").TrimEnd('/');
        var org = GetString(config, "org");
        var bucket = GetString(config, "bucket");
        var token = GetString(config, "token");
        var measurement = GetString(config, "measurement", "speedtests");

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(org) || string.IsNullOrEmpty(bucket)) return false;

        double down = 0, up = 0, ping = 0, jitter = 0;
        if (eventData is Speedtest st)
        {
            down = st.Download;
            up = st.Upload;
            ping = st.Ping;
            jitter = st.Jitter ?? 0;
        }
        else if (eventData is SpeedtestExecutionResult res)
        {
            down = res.Download;
            up = res.Upload;
            ping = res.Ping;
            jitter = res.Jitter ?? 0;
        }

        var host = GetString(config, "host", Environment.MachineName);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var line = $"{measurement},host={host} download={down:F2},upload={up:F2},ping={ping:F0},jitter={jitter:F2} {timestamp}";

        var writeUrl = $"{url}/api/v2/write?org={Uri.EscapeDataString(org)}&bucket={Uri.EscapeDataString(bucket)}&precision=s";
        var request = new HttpRequestMessage(HttpMethod.Post, writeUrl)
        {
            Content = new StringContent(line, Encoding.UTF8, "text/plain")
        };
        request.Headers.Add("Authorization", $"Token {token}");

        var httpRes = await client.SendAsync(request, ct);
        return httpRes.IsSuccessStatusCode;
    }
}
