using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Lira.Jql.JqlKeywordDate;

namespace Lira.Jql;

[StructLayout(LayoutKind.Auto)]
public readonly record struct JqlRelativeDate : IJqlDate
{
    public JqlRelativeDate(Unit timeUnit, int offset, DateTimeOffset? @base = null) : this()
    {
        TimeUnit = timeUnit;
        Offset = offset;
    }

    public enum Unit
    {
        Day,
        Week,
        Month,
        Year,
        Hour,
        Minutes,
    }
    public Unit TimeUnit { get; init; }
    public int Offset { get; init; }
    public string GetJqlValue(TimeZoneInfo accountTimezone) => GetJqlValue();
    public string GetJqlValue()
    {
        string num = Offset.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string unit = UnitToString(TimeUnit);
        return $"{num}{unit}";
    }

    public DateTimeOffset ToAccountDatetime(TimeZoneInfo accountTimezone)
    {
        return TimeZoneInfo.ConvertTime(ApplyOffset(DateTimeOffset.UtcNow), accountTimezone);
    }

    private DateTimeOffset ApplyOffset(DateTimeOffset baseDate)
    {
        if (Offset == 0)
            return baseDate;
        return TimeUnit switch
        {
            Unit.Day => baseDate.AddDays(Offset),
            Unit.Week => baseDate.AddDays(Offset * 7),
            Unit.Month => baseDate.AddMonths(Offset),
            Unit.Year => baseDate.AddYears(Offset),
            Unit.Hour => baseDate.AddHours(Offset),
            Unit.Minutes => baseDate.AddMinutes(Offset),
            _ => throw new NotSupportedException(),
        };
    }


    public static string UnitToString(Unit unit)
    {
        return unit switch
        {
            Unit.Day => "d",
            Unit.Week => "w",
            Unit.Month => "M",
            Unit.Year => "y",
            Unit.Hour => "h",
            Unit.Minutes => "m",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, message: null),
        };
    }
    private static Unit StringToUnit(ReadOnlySpan<char> s)
    {
        return s switch
        {
            [] => Unit.Day,
            ['d', ..] => Unit.Day,
            ['D', ..] => Unit.Day,

            ['w', ..] => Unit.Week,
            ['W', ..] => Unit.Week,

            ['M', ..] => Unit.Month,

            ['y', ..] => Unit.Year,
            ['Y', ..] => Unit.Year,

            ['h', ..] => Unit.Hour,
            ['H', ..] => Unit.Hour,

            ['m', ..] => Unit.Minutes,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s.ToString(), message: null),
        };
    }
    public static bool TryParse(ReadOnlySpan<char> value, out JqlRelativeDate keywordDate)
    {
        keywordDate = default;
        value = value.Trim();
        int numberEnd = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (MemoryExtensions.Contains("-0123456789", c))
            {
                numberEnd++;
            }
            else
            {
                break;
            }
        }
        var numberPart = value[..numberEnd];
        var unitPart = value[numberEnd..].Trim();
        if (!int.TryParse(numberPart, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int offset))
        {
            return false;
        }
        try
        {
            var unit = StringToUnit(unitPart);
            keywordDate = new JqlRelativeDate(unit, offset);
            var _ = keywordDate.ToAccountDatetime(TimeZoneInfo.Utc);
            return true;

        }
        catch (ArgumentOutOfRangeException)
        {
        }
        return false;
    }
    public string PrettyForm()
    {
        if (Offset == 0)
        {
            return "Today";
        }
        string offsetStr = Math.Abs(Offset).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var noun = (Math.Abs(Offset), TimeUnit) switch
        {
            (1, Unit.Day) => "day",
            (1, Unit.Week) => "week",
            (1, Unit.Month) => "month",
            (1, Unit.Year) => "year",
            (1, Unit.Hour) => "hour",
            (1, Unit.Minutes) => "minute",
            (_, Unit.Day) => "days",
            (_, Unit.Week) => "weeks",
            (_, Unit.Month) => "months",
            (_, Unit.Year) => "years",
            (_, Unit.Hour) => "hours",
            (_, Unit.Minutes) => "minutes",
            _ => throw new NotSupportedException(),
        };
        if (Offset < 0)
        {
            return $"{offsetStr} {noun} ago";
        }
        return $"in {offsetStr} {noun}";

    }
}
