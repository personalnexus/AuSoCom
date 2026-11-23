using System.Globalization;

namespace AuSoCom.Application;

public static class FadeDurationParser
{
    private static readonly string[] MinuteSecondFormats =
    [
        @"m\:ss\.fff",
        @"mm\:ss\.fff"
    ];

    public static bool TryParse(string? value, out TimeSpan? duration)
    {
        duration = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        var text = value.Trim();
        if (text.Contains(':'))
        {
            if (!TimeSpan.TryParseExact(
                    text,
                    MinuteSecondFormats,
                    CultureInfo.InvariantCulture,
                    out var parsedDuration) ||
                parsedDuration < TimeSpan.Zero)
                return false;

            duration = parsedDuration;
            return true;
        }

        var unit = FadeDurationUnit.Milliseconds;
        if (text.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^2].Trim();
        }
        else if (text.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^1].Trim();
            unit = FadeDurationUnit.Seconds;
        }

        if (!TryParseNumber(text, out var amount) || amount < 0 || !double.IsFinite(amount))
            return false;

        var ticksPerUnit = unit == FadeDurationUnit.Seconds
            ? TimeSpan.TicksPerSecond
            : TimeSpan.TicksPerMillisecond;
        var ticks = amount * ticksPerUnit;
        var roundedTicks = Math.Round(ticks, MidpointRounding.AwayFromZero);
        if (!double.IsFinite(roundedTicks) || roundedTicks > TimeSpan.MaxValue.Ticks)
            return false;

        duration = TimeSpan.FromTicks((long)roundedTicks);
        return true;
    }

    private static bool TryParseNumber(string value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
        double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number);

}
