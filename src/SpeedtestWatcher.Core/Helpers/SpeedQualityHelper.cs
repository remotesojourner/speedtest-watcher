using SpeedtestWatcher.Core.Enums;

namespace SpeedtestWatcher.Core.Helpers;

public static class SpeedQualityHelper
{
    public static SpeedQuality GetQuality(double current, double optimal, bool higherIsBetter)
    {
        if (current < 0) return SpeedQuality.Error;
        if (optimal <= 0) return SpeedQuality.Green;

        var speedPercent = Math.Floor((current / optimal) * 100);

        if (higherIsBetter)
        {
            if (speedPercent >= 75) return SpeedQuality.Green;
            if (speedPercent >= 30) return SpeedQuality.Orange;
            return SpeedQuality.Red;
        }
        else
        {
            if (speedPercent >= 180) return SpeedQuality.Red;
            if (speedPercent >= 130) return SpeedQuality.Orange;
            return SpeedQuality.Green;
        }
    }
}
