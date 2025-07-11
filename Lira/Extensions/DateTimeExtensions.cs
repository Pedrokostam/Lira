using System;
using System.Globalization;
using Lira.Jql;
namespace Lira.Extensions;

public static class DateTimeExtensions
{
    // TODO Testings
    private static readonly TimeSpan OneTick = new(1);
    public static DateTimeOffset StartOfDay(this DateTimeOffset baseDate)
    {
        var date = baseDate;
        return new DateTimeOffset(date.Year, date.Month, date.Day,0,0,0,baseDate.Offset );
    }

    public static DateTimeOffset EndOfDay(this DateTimeOffset baseDate)
    {
        return baseDate.AddDays(1).StartOfDay() - OneTick;
    }
    public static DateTimeOffset StartOfWeek(this DateTimeOffset baseDate)
    {
        // Jira's week starts on Sunday (xD)

        // Sunday is 0; Saturday is 6
        var dayOffset = (int)baseDate.DayOfWeek;
        // we want the date to be on the first day of jira's week (sunday)
        // so, if the current date is thursday (4) we need to subtract 4 days
        var weekStartUnclean = baseDate.AddDays(-dayOffset);
        return StartOfDay(weekStartUnclean);
    }
    public static DateTimeOffset EndOfWeek(this DateTimeOffset baseDate)
    {
        return baseDate.AddDays(7).StartOfWeek() - OneTick;

    }
    public static DateTimeOffset StartOfMonth(this DateTimeOffset baseDate)
    {
        var offset = baseDate.Day - 1; // -1 becasue month starts at day 1, not 0;
        return baseDate.AddDays(-offset).StartOfDay();

    }
    public static DateTimeOffset EndOfMonth(this DateTimeOffset baseDate)
    {
        return baseDate.AddMonths(1).StartOfMonth() - OneTick;
    }
    public static DateTimeOffset StartOfYear(this DateTimeOffset baseDate)
    {
        var offset = baseDate.DayOfYear - 1; // -1 becasue year starts at day 1, not 0;
        return baseDate.AddDays(-offset).StartOfDay();
    }
    public static DateTimeOffset EndOfYear(this DateTimeOffset baseDate)
    {
        return baseDate.AddYears(1).StartOfYear() - OneTick;
    }

    public static DateTimeOffset ToDateTimeOffset(this JqlKeywordDate.Keywords keyword, DateTimeOffset baseDate)
    {
        return FromKeyword(baseDate, keyword);
    }
    public static DateTimeOffset FromKeyword(DateTimeOffset baseDate, JqlKeywordDate.Keywords keyword)
    {
        return keyword switch
        {
            JqlKeywordDate.Keywords.StartOfDay => StartOfDay(baseDate),
            JqlKeywordDate.Keywords.EndOfDay => EndOfDay(baseDate),
            JqlKeywordDate.Keywords.StartOfWeek => StartOfWeek(baseDate),
            JqlKeywordDate.Keywords.EndOfWeek => EndOfWeek(baseDate),
            JqlKeywordDate.Keywords.StartOfMonth => StartOfMonth(baseDate),
            JqlKeywordDate.Keywords.EndOfMonth => EndOfMonth(baseDate),
            JqlKeywordDate.Keywords.StartOfYear => StartOfYear(baseDate),
            JqlKeywordDate.Keywords.EndOfYear => EndOfYear(baseDate),
            _ => throw new NotSupportedException(),
        };
    }
    public static int WeekNumber(this DateTimeOffset dto)
    {
        return ISOWeek.GetWeekOfYear(dto.Date);
    }
}