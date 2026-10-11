namespace Imova.Application.Common;

// The calendar the statistics count in: a "day" is a day in Moldova (Europe/Chisinau), whatever the
// server's clock zone. Falls back to UTC on a system without the time zone data.
public static class MoldovaTime
{
    public static readonly TimeZoneInfo Zone = Find();

    public static DateOnly Day(DateTimeOffset moment) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, Zone).DateTime);

    private static TimeZoneInfo Find()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Chisinau");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
