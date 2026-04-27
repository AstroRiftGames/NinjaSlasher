using System;
using System.Globalization;

internal static class DailyAvailabilityTimeUtility
{
    public static readonly TimeSpan CooldownInterval = TimeSpan.FromHours(24);
    public static readonly TimeSpan MissedWindowThreshold = TimeSpan.FromHours(48);

    public static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static bool TryParseIsoUtc(string value, out DateTime utcDateTime)
    {
        if (DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            utcDateTime = NormalizeUtc(parsed);
            return true;
        }

        utcDateTime = DateTime.MinValue;
        return false;
    }

    public static bool TryParseUtcDate(string value, out DateTime utcDateTime)
    {
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
        {
            utcDateTime = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
            return true;
        }

        utcDateTime = DateTime.MinValue;
        return false;
    }

    public static string FormatCountdown(TimeSpan remaining)
    {
        int totalHours = Math.Max(0, (int)Math.Floor(remaining.TotalHours));
        return $"{totalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }
}
