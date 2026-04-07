using System;
using System.Globalization;

namespace VTrailer.Models;

public static class BookingTimeSlotMetadata
{
    public const string MorningDisplay = "Délelőtt (08:00 - 12:00)";
    public const string AfternoonDisplay = "Délután (13:00 - 17:00)";
    public const string FullDayDisplay = "Egész nap (08:00 - 17:00)";

    private const string MultiDayPrefix = "MULTI_DAY";
    private const char Separator = '|';

    public static string CreateMultiDayValue(DateTime startDate, DateTime endDate) =>
        string.Join(
            Separator,
            MultiDayPrefix,
            startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    public static bool TryParseMultiDay(string? value, out DateTime startDate, out DateTime endDate)
    {
        startDate = default;
        endDate = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3 || !string.Equals(parts[0], MultiDayPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return DateTime.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate)
            && DateTime.TryParseExact(parts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);
    }

    public static bool IsFullDay(string? value) =>
        TryParseMultiDay(value, out _, out _)
        || (!string.IsNullOrWhiteSpace(value) && value.Contains("Egész nap", StringComparison.OrdinalIgnoreCase));

    public static string GetDisplayLabel(string? value) =>
        TryParseMultiDay(value, out _, out _) ? FullDayDisplay : value ?? string.Empty;
}
