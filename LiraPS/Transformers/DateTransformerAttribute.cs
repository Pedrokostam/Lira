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
using Lira.Jql;
namespace LiraPS.Transformers;
internal class DateTransformerAttribute(bool wrapInIJqlDate) : ArgumentTransformationAttribute()
{
    public bool WrapInIJqlDate { get; } = wrapInIJqlDate;

    public override object? Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is PSObject pso)
        {
            inputData = pso.BaseObject;
        }
        if (inputData is IJqlDate jqlDate)
        {
            if (WrapInIJqlDate)
            {
                return jqlDate;

            }
            else
            {
                return jqlDate.ToAccountDatetime(TimeZoneInfo.Local);
            }
        }
        if (inputData is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return null;
            }
            if (DateCompletionHelper.GetDateFromNonPositiveInt(s, out var date, out _))
            {
                return date;
            }
            if (JqlKeywordDate.TryParse(s.Replace(" ", ""), out var keywordDate))
            {
                return keywordDate;
            }
            if (DateTimeOffset.TryParse(s, null, DateTimeStyles.AssumeLocal, out var parsedDate))
            {
                inputData = parsedDate;
            }
            else
            {
                throw new ArgumentException($"Could not parse string {s} to DateTimeOffset");
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
            if (WrapInIJqlDate)
            {
                return new JqlManualDate(dateTimeOffset);

            }
            else
            {
                return dateTimeOffset;
            }
        }

        throw new ArgumentException($"Could not convert {inputData} to IJqlDate");
    }
}
