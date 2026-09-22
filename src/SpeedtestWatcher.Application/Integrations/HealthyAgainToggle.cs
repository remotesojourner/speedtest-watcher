using System.Text.Json.Nodes;

namespace SpeedtestWatcher.Application.Integrations;

internal static class HealthyAgainToggle
{
    public const string Key = "send_healthy_again";

    private const string UnhealthyKey = "send_unhealthy";

    public static string FollowUnhealthy(string dataJson)
    {
        if (JsonNode.Parse(dataJson) is not JsonObject data || data.ContainsKey(Key)) return dataJson;

        data[Key] = IntegrationSettings.Parse(dataJson)?.GetBool(UnhealthyKey, true) ?? true;
        return data.ToJsonString();
    }
}
