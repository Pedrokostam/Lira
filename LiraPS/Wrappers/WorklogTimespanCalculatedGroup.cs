﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using Lira.Grouping;
using Lira.Objects;
using LiraPS.Arguments;

namespace LiraPS.Wrappers;

// Couldn't really find a simple way to specify formatting for a generic type (I'd rather avoid specifying assembly version, tyvm)
// Also, since the group implements GetEnumerator, Powershell would rather display the inner collection than the object...
// So here is a simple wrapper, which exposes all properties of the Group and does NOT implement IEnumerable (nor IGrouping)
public sealed record WorklogTimespanCalculatedGroup
{
    public CalculatedGroup<Worklog?, TimeSpan> Base { get; }
    public ImmutableArray<Worklog?> Items => Base.Items;
    public string Header => Base.Header;
    public ImmutableArray<string> Columns => Base.Columns;
    public ImmutableArray<object?> ColumnValues => Base.ColumnValues;
    public ImmutableArray<string> ColumnValuesTexts => Base.ColumnValuesTexts;
    public TimeSpan CalculatedValue => Base.CalculatedValue;
    public WorklogTimespanCalculatedGroup(CalculatedGroup<Worklog?, TimeSpan> donor)
    {
        Base = donor;
    }
    public static WorklogTimespanCalculatedGroup Wrap(CalculatedGroup<Worklog?, TimeSpan> donor) => new(donor);

}
//public readonly record struct WorklogSum
//{
//    public const string PropertySeparator = " => ";
//    public ImmutableArray<Property> Properties { get; init; }
//    public string Grouping { get; init; } = "";
//    public ImmutableArray<Worklog> Worklogs { get; init; }
//    public TimeSpan TimeSpent => TimeSpan.FromTicks(Worklogs.Sum(x => x.TimeSpent.Ticks));
//    public ImmutableArray<IssueCommon> Issues { get; private init; }
//    private WorklogSum(IEnumerable<Property> groupProps, string grouping, IEnumerable<Worklog> worklogs)
//    {
//        Worklogs = worklogs.ToImmutableArray();
//        Properties = groupProps.ToImmutableArray();
//        Grouping = grouping;
//        Issues = [.. Worklogs.Select(x => x.Issue).Distinct().OrderBy(x => x.Key)];
//    }
//    private static string GetValue(Worklog log, Property prop)
//    {
//        return prop switch
//        {
//            Property.Issue => log.Issue.Key,
//            Property.User => log.Author.Name,
//            Property.Day => log.Started.ToString("yyyy-MM-dd"),
//            Property.Month => log.Started.ToString("yyyy-MM"),
//            Property.Year => log.Started.ToString("yyyy"),
//            _ => "",
//        };
//    }
//    private static IEnumerable<string> GetProps(Worklog log, IEnumerable<Property> prop)
//    {
//        foreach (var item in prop)
//        {
//            yield return item switch
//            {
//                Property.Issue => log.Issue.Key,
//                Property.User => log.Author.Name,
//                Property.Day => log.Started.ToString("yyyy-MM-dd"),
//                Property.Month => log.Started.ToString("yyyy-MM"),
//                Property.Year => log.Started.ToString("yyyy"),
//                _ => "",
//            };
//        }
//    }
//    private static Func<Worklog, string> GetSelector(IList<Property> props)
//    {
//        return (log) =>
//        {
//            if (props.Count == 0)
//            {
//                return "";
//            }
//            var parts = GetProps(log, props);
//            return string.Join(PropertySeparator, parts);
//        };
//    }
//    public static List<WorklogSum> Sum(IEnumerable<Worklog> worklogs, params Property[] groups)
//    {
//        var props = groups.Where(x => x != Property.None).ToImmutableArray();
//        var datePropsCount = props.Sum(x => x == Property.Day || x == Property.Month || x == Property.Year ? 1 : 0);
//        if (datePropsCount > 1)
//        {
//            throw new PSArgumentException("Cannot use more than one date-based property for grouping");
//        }
//        var selector = GetSelector(props);
//        var grouped = worklogs.GroupBy(selector);
//        List<WorklogSum> output = [];
//        foreach (IGrouping<string, Worklog> item in grouped)
//        {
//            var grouping = string.IsNullOrWhiteSpace(item.Key) ? "None" : item.Key;
//            var s = new WorklogSum(props, grouping, item);
//            output.Add(s);
//        }
//        return output;
//    }
//    public PSObject ToPsObject()
//    {
//        var pso = new PSObject();
//        foreach (var prop in Properties)
//        {
//            pso.Members.Add(new PSNoteProperty(prop.ToString(), GetValue(Worklogs[0], prop)));
//        }
//        pso.Members.Add(new PSNoteProperty(nameof(Worklogs), Worklogs.Length));
//        pso.Members.Add(new PSNoteProperty(nameof(TimeSpent), TimeSpent));
//        pso.Members.Add(new PSScriptProperty("TimeSpentFormatted", ScriptBlock.Create(@"(""{0:D2}:{1:D2}"" -f [int]$_.TotalTimeSpent.TotalHours, $_.TimeSpent.Minutes)")));
//        return pso;
//    }

//}
