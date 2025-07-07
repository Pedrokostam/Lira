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
namespace LiraPS.Transformers;
internal enum DateMode
{
    /// <summary>
    /// Assumes current day; If time of day is needed - assumes current time of day (even if the day is different)
    /// </summary>
    Current,
    Start,
    End,

}
internal class DateTransformerAttribute(bool outputIJqlDate, DateMode mode) : ArgumentTransformationAttribute()
{
    public bool OutputIJqlDate { get; } = outputIJqlDate;
    public DateMode Mode { get; } = mode;

    private object WrapUnwrap(object? dateObj)=>WrapUnwrap(dateObj,OutputIJqlDate);
    
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
                DateMode.Start => new JqlKeywordDate(JqlKeywordDate.JqlDateKeywords.StartOfDay,i),
                DateMode.End => new JqlKeywordDate(JqlKeywordDate.JqlDateKeywords.EndOfDay, i),
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
            if (DateCompletionHelper.GetDateFromNonPositiveInt(s,Mode, out var date, out _))
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
                        _ => throw new NotImplementedException(),
                    };
                }
                inputData = parsedDate;
            }
            else
            {
                throw new ArgumentTransformationMetadataException($"Could not parse string {s} to DateTimeOffset");
            }
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
}
