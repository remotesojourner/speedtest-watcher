using SpeedtestWatcher.Application.Integrations;

namespace SpeedtestWatcher.UnitTests.Application.Integrations;

public class HealthyAgainToggleTests
{
    [Theory]
    [InlineData("""{"send_unhealthy":false}""", false)]
    [InlineData("""{"send_unhealthy":"False"}""", false)]
    [InlineData("""{"send_unhealthy":true}""", true)]
    [InlineData("""{"url":"https://localhost/hook"}""", true)]
    public void AMissingToggleFollowsTheUnhealthyToggle(string data, bool expected)
    {
        var settings = IntegrationSettings.Parse(HealthyAgainToggle.FollowUnhealthy(data))!;

        Assert.Equal(expected, settings.GetBool(HealthyAgainToggle.Key, !expected));
    }

    [Fact]
    public void AToggleThatIsAlreadySetIsKept()
    {
        const string Data = """{"send_unhealthy":true,"send_healthy_again":false}""";

        Assert.Equal(Data, HealthyAgainToggle.FollowUnhealthy(Data));
    }
}
