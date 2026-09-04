using System.Globalization;

namespace SptBattlePass.Server.Services;

internal static class SeasonClock
{
    public static DateTime UtcNow => DateTime.UtcNow;

    public static string DailyKey(DateTime utc) => utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string WeeklyKey(DateTime utc)
    {
        int week = ISOWeek.GetWeekOfYear(utc);
        int year = ISOWeek.GetYear(utc);
        return $"{year}-W{week:D2}";
    }

    public static string MonthlyKey(DateTime utc) => utc.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public static string SeasonName(DateTime utc) => utc.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public static int DaysRemainingInMonth(DateTime utc)
    {
        var nextMonth = new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return Math.Max(0, (int)Math.Ceiling((nextMonth - utc).TotalDays));
    }

    public static string DailyExpiryIso(DateTime utc)
    {
        var next = utc.Date.AddDays(1);
        return ToIso(DateTime.SpecifyKind(next, DateTimeKind.Utc));
    }

    public static string WeeklyExpiryIso(DateTime utc)
    {
        int week = ISOWeek.GetWeekOfYear(utc);
        int year = ISOWeek.GetYear(utc);
        var monday = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var nextMonday = DateTime.SpecifyKind(monday.AddDays(7), DateTimeKind.Utc);
        return ToIso(nextMonday);
    }

    public static string MonthlyExpiryIso(DateTime utc)
    {
        var next = new DateTime(utc.Year, utc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return ToIso(next);
    }

    public static string ToIso(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}
