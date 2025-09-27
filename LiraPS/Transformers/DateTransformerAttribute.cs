using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LiraPS.Extensions;
using Lira.Jql;
using ConsoleMenu;
namespace LiraPS.Transformers;
public enum DateMode
{
    /// <summary>
    /// Assumes current day; If time of day is needed - assumes current time of day (even if the day is different)
    /// </summary>
    Current,
    Start,
    End,

}
public class DateTransformerAttribute3(bool outputIJqlDate, DateMode mode) : ArgumentTransformationAttribute()
{
    public bool OutputIJqlDate { get; } = outputIJqlDate;
    public DateMode Mode { get; } = mode;

    private object WrapUnwrap(object? dateObj) => WrapUnwrap(dateObj, OutputIJqlDate);

    internal static object WrapUnwrap(object? dateObj, bool outputIjqlDate)
    {
        return (dateObj, outputIjqlDate) switch
        {
            (IJqlDate ijql, true) => ijql,
            (IJqlDate ijql, false) => ijql.ToAccountDatetime(TimeZoneInfo.Local),
            (DateTimeOffset dto, true) => new JqlManualDate(dto),
            (DateTimeOffset dto, false) => dto,
            (_, _) => throw new ArgumentTransformationMetadataException(),
        };
    }
    public override object? Transform(EngineIntrinsics engineIntrinsics, object inputData) => Transform(inputData);
    public object? Transform(object inputData)
    {
        if (inputData is int i && i <= 0)
        {
            IJqlDate? todayo = Mode switch
            {
                DateMode.Current => new JqlManualDate(DateTimeOffset.Now.AddDays(i)),
                DateMode.Start => new JqlKeywordDate(JqlKeywordDate.Keywords.StartOfDay, i),
                DateMode.End => new JqlKeywordDate(JqlKeywordDate.Keywords.EndOfDay, i),
                _ => null,
            };
            return WrapUnwrap(todayo);
        }
        if (inputData is PSObject pso)
        {
            inputData = pso.BaseObject;
        }
        if (inputData is IJqlDate jqlDate)
        {
            return WrapUnwrap(jqlDate);
        }
        if (inputData is string s)
        {
            return TransformString(s);
        }

        DateTimeOffset dateTimeOffset = default;
        if (inputData is DateTimeOffset offset)
        {
            dateTimeOffset = offset;
        }
        else if (inputData is DateTime date)
        {
            dateTimeOffset = date;
        }
        if (dateTimeOffset != default)
        {
            return WrapUnwrap(dateTimeOffset);
        }

        throw new ArgumentTransformationMetadataException($"Could not convert {inputData} to IJqlDate");
    }

    public object? TransformString(string s)
    {
        if (s.Contains("yester", StringComparison.OrdinalIgnoreCase))
        {
            IJqlDate? yesterday = Mode switch
            {
                DateMode.Current => new JqlManualDate(DateTimeOffset.Now.AddDays(-1)),
                DateMode.Start => new JqlKeywordDate(JqlKeywordDate.Keywords.StartOfDay, -1),
                DateMode.End => new JqlKeywordDate(JqlKeywordDate.Keywords.EndOfDay, -1),
                _ => null,
            };
            return WrapUnwrap(yesterday);
        }
        if (s.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            IJqlDate? todayo = Mode switch
            {
                DateMode.Current => new JqlManualDate(DateTimeOffset.Now),
                DateMode.Start => JqlKeywordDate.StartOfDay,
                DateMode.End => JqlKeywordDate.EndOfDay,
                _ => null,
            };
            return WrapUnwrap(todayo);
        }
        if (s.Equals(value: "now", StringComparison.OrdinalIgnoreCase))
        {
            return WrapUnwrap(new JqlManualDate(DateTimeOffset.Now));
        }
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }
        if (DateCompletionHelper.GetDateFromNonPositiveInt(s, Mode, out var date, out _))
        {
            return WrapUnwrap(date);
        }
        if (JqlKeywordDate.TryParse(s.Replace(" ", ""), out var keywordDate))
        {
            return WrapUnwrap(keywordDate);
        }
        if (TimeExtensions.TryParseDateTimeOffset(s, out var parsedDate))
        {
            if (parsedDate.TimeOfDay == TimeSpan.Zero)
            {
                parsedDate = Mode switch
                {
                    DateMode.Current => parsedDate.AddHours(DateTime.Now.TimeOfDay.TotalHours),
                    DateMode.Start => parsedDate,
                    DateMode.End => parsedDate.AddDays(1).Subtract(TimeSpan.FromTicks(1)),
                    _ => throw new PSNotImplementedException(),
                };
            }
            return WrapUnwrap(parsedDate);
        }
        else
        {
            throw new ArgumentTransformationMetadataException($"Could not parse string {s} to DateTimeOffset");
        }
    }
}
public sealed class JqlDateTransformerAttribute(DateMode mode) : DateTransformerAttribute<IJqlDate>(mode)
{
    protected override IJqlDate WrapUnwrap(object? dateObject)
    {
        return dateObject switch
        {
            IJqlDate ijql => ijql,
            DateTimeOffset dto => new JqlManualDate(dto),
            DateTime dt => new JqlManualDate(dt),
            _ => throw new ArgumentTransformationMetadataException(),
        };
    }
}
public sealed class DateTimeOffsetDateTransformerAttribute(DateMode mode) : DateTransformerAttribute<DateTimeOffset>(mode)
{
    protected override DateTimeOffset WrapUnwrap(object? dateObject)
    {
        return dateObject switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => dt,
            IJqlDate ijql => ijql.ToAccountDatetime(TimeZoneInfo.Local),
            _ => throw new ArgumentTransformationMetadataException(),
        };
    }
}
public abstract class DateTransformerAttribute<T>(DateMode mode) : ArgumentTransformationAttribute(), ITransform<T>
{

    public DateMode Mode { get; } = mode;

    protected abstract T WrapUnwrap(object? dateObject);

    public override object? Transform(EngineIntrinsics engineIntrinsics, object inputData) => Transform(inputData);
    public object? Transform(object inputData)
    {
        if (inputData is int i && i <= 0)
        {
            IJqlDate? todayo = Mode switch
            {
                DateMode.Current => new JqlManualDate(DateTimeOffset.Now.AddDays(i)),
                DateMode.Start => new JqlKeywordDate(JqlKeywordDate.Keywords.StartOfDay, i),
                DateMode.End => new JqlKeywordDate(JqlKeywordDate.Keywords.EndOfDay, i),
                _ => null,
            };
            return WrapUnwrap(todayo);
        }
        if (inputData is PSObject pso)
        {
            inputData = pso.BaseObject;
        }
        if (inputData is IJqlDate jqlDate)
        {
            return WrapUnwrap(jqlDate);
        }
        if (inputData is string s)
        {
            return Transform(s);
        }

        DateTimeOffset dateTimeOffset = default;
        if (inputData is DateTimeOffset offset)
        {
            dateTimeOffset = offset;
        }
        else if (inputData is DateTime date)
        {
            dateTimeOffset = date;
        }
        if (dateTimeOffset != default)
        {
            return WrapUnwrap(dateTimeOffset);
        }

        throw new ArgumentTransformationMetadataException($"Could not convert {inputData} to IJqlDate");
    }

    public T Transform(string s)
    {
        if (s.Contains("yester", StringComparison.OrdinalIgnoreCase))
        {
            IJqlDate? yesterday = Mode switch
            {
                DateMode.Current => new JqlManualDate(DateTimeOffset.Now.AddDays(-1)),
                DateMode.Start => new JqlKeywordDate(JqlKeywordDate.Keywords.StartOfDay, -1),
                DateMode.End => new JqlKeywordDate(JqlKeywordDate.Keywords.EndOfDay, -1),
                _ => null,
            };
            return WrapUnwrap(yesterday);
        }
        if (s.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            IJqlDate? todayo = Mode switch
            {
                DateMode.Current => new JqlManualDate(DateTimeOffset.Now),
                DateMode.Start => JqlKeywordDate.StartOfDay,
                DateMode.End => JqlKeywordDate.EndOfDay,
                _ => null,
            };
            return WrapUnwrap(todayo);
        }
        if (s.Equals(value: "now", StringComparison.OrdinalIgnoreCase))
        {
            return WrapUnwrap(new JqlManualDate(DateTimeOffset.Now));
        }
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new ArgumentNullException(nameof(s));
        }
        if (DateCompletionHelper.GetDateFromNonPositiveInt(s, Mode, out var date, out _))
        {
            return WrapUnwrap(date);
        }
        if (JqlKeywordDate.TryParse(s.Replace(" ", ""), out var keywordDate))
        {
            return WrapUnwrap(keywordDate);
        }
        if (TimeExtensions.TryParseDateTimeOffset(s, out var parsedDate))
        {
            if (parsedDate.TimeOfDay == TimeSpan.Zero)
            {
                parsedDate = Mode switch
                {
                    DateMode.Current => parsedDate.AddHours(DateTime.Now.TimeOfDay.TotalHours),
                    DateMode.Start => parsedDate,
                    DateMode.End => parsedDate.AddDays(1).Subtract(TimeSpan.FromTicks(1)),
                    _ => throw new PSNotImplementedException(),
                };
            }
            return WrapUnwrap(parsedDate);
        }
        else
        {
            throw new ArgumentTransformationMetadataException($"Could not parse string {s} to DateTimeOffset");
        }
    }

    public string? DescriptiveTransform(string? item)
    {
        if (item is null)
        {
            return null;
        }
        try
        {
            return Transform(item)?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
