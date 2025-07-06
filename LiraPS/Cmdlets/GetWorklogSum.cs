using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Lira.Objects;
using LiraPS.Arguments;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets;

public class WorklogComparer : Comparer<Worklog>
{
    public WorklogComparer(IEnumerable<Property> props)
    {
        Properties = props.ToImmutableArray();
    }

    public ImmutableArray<Property> Properties { get; }
    public override int Compare(Worklog? x, Worklog? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        foreach (var group in Properties)
        {
            int cmp = group switch
            {
                Property.Issue => string.Compare(x.IssueId, y.IssueId, StringComparison.Ordinal),
                Property.User => string.Compare(x.Author.Name, y.Author.Name, StringComparison.Ordinal),
                Property.Day => x.Started.Date.CompareTo(y.Started.Date),
                Property.Month => x.Started.ToString("yyyyMM", CultureInfo.InvariantCulture).CompareTo(y.Started.ToString("yyyyMM", CultureInfo.InvariantCulture)),
                Property.Year => x.Started.Year.CompareTo(y.Started.Year),
                _ => 0,
            };
            if (cmp != 0)
                return cmp;
        }
        return 0;
    }
}
public readonly record struct WorklogSum
{
    public ImmutableArray<Property> Properties { get; init; }
    public string Header { get; init; } = "";
    public ImmutableArray<Worklog> Worklogs { get; init; }
    public TimeSpan TimeSpent => TimeSpan.FromTicks(Worklogs.Select(x => x.TimeSpent.Ticks).Sum());
    private WorklogSum(IEnumerable<Property> groupProps, string header, IEnumerable<Worklog> worklogs)
    {
        Worklogs = worklogs.ToImmutableArray();
        Properties = groupProps.ToImmutableArray();
        Header = header;
    }
    private static string GetValue(Worklog log, Property prop)
    {
        return prop switch
        {
            Property.Issue => log.Issue.Key,
            Property.User => log.Author.Name,
            Property.Day => log.Started.ToString("yyyy-MM-dd"),
            Property.Month => log.Started.ToString("yyyy-MM"),
            Property.Year => log.Started.ToString("yyyy"),
            _ => "",
        };
    }
    private static IEnumerable<string> GetProps(Worklog log, IEnumerable<Property> prop)
    {
        foreach (var item in prop)
        {
            yield return item switch
            {
                Property.Issue => log.Issue.Key,
                Property.User => log.Author.Name,
                Property.Day => log.Started.ToString("yyyy-MM-dd"),
                Property.Month => log.Started.ToString("yyyy-MM"),
                Property.Year => log.Started.ToString("yyyy"),
                _ => "",
            };
        }
    }
    private static Func<Worklog, string> GetSelector(IList<Property> props)
    {
        return (log) =>
        {
            if (props.Count == 0)
            {
                return "";
            }
            var parts = GetProps(log, props);
            return string.Join("->", parts);
        };
    }
    public static List<WorklogSum> Sum(IEnumerable<Worklog> worklogs, params Property[] groups)
    {
        var props = groups.Where(x => x != Property.None).ToImmutableArray();
        var datePropsCount = props.Sum(x => (x == Property.Day || x == Property.Month || x == Property.Year) ? 1 : 0);
        if (datePropsCount > 1)
        {
            throw new ArgumentException("Cannot use more than one date-based property for grouping");
        }
        var selector = GetSelector(props);
        var grouped = worklogs.GroupBy(selector);
        List<WorklogSum> output = [];
        foreach (IGrouping<string, Worklog> item in grouped)
        {
            var s = new WorklogSum(props, item.Key, item);
            output.Add(s);
        }
        return output;
    }
    public PSObject ToPsObject()
    {
        var pso = new PSObject();
        foreach (var prop in Properties)
        {
            pso.Members.Add(new PSNoteProperty(prop.ToString(), GetValue(Worklogs[0], prop)));
        }
        pso.Members.Add(new PSNoteProperty(nameof(Worklogs), Worklogs.Length));
        pso.Members.Add(new PSNoteProperty(nameof(TimeSpent), TimeSpent));
        pso.Members.Add(new PSScriptProperty("TimeSpentFormatted",ScriptBlock.Create(@"(""{0:D2}:{1:D2}"" -f [int]$_.TotalTimeSpent.TotalHours, $_.TimeSpent.Minutes)")));
        return pso;
    }

}
[Alias("logsum")]
[Cmdlet(VerbsCommon.Get, "WorklogSum", DefaultParameterSetName = "STRUCT")]
[OutputType(typeof(PSObject), ParameterSetName = ["SIMPLE"])]
[OutputType(typeof(WorklogSum), ParameterSetName = ["STRUCT"])]
public class GetWorklogSum : LiraCmdlet
{
    [Parameter(Mandatory = false, ValueFromPipeline = true)]
    public Worklog[] Worklogs { get; set; } = [];

    [Parameter(Position = 0, ValueFromPipeline = false)]
    public Property[] Groups { get; set; } = [Property.Month];

    [Parameter(ParameterSetName = "SIMPLE")]
    public SwitchParameter Simple { get; set; }

    [Parameter()]
    public SwitchParameter PassThru { get; set; }

    private readonly List<Worklog> _worklogs = [];

    protected override void BeginProcessing()
    {
        // no need to test session
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        if (Worklogs.Length == 0)
        {
            if (GetGlobal("LiraLastWorklogs") is IEnumerable<Worklog> logs)
            {
                Worklogs = logs.ToArray();
                LiraSession.Logger.LogWarning("Using previously fetched logs");
            }
            else
            {
                Terminate(new ArgumentException("No worklogs provided"), "NoWorklogs", ErrorCategory.InvalidArgument);
            }
        }
        PrintLogs();
        foreach (var worklog in Worklogs)
        {
            _worklogs.Add(worklog);
            if (PassThru.IsPresent)
            {
                WriteObject(worklog);
            }
        }
        base.ProcessRecord();
    }
    protected override void EndProcessing()
    {
        var summedLogs = WorklogSum.Sum(_worklogs, Groups);
        if (Simple.IsPresent)
        {
            
            WriteObject(summedLogs.Select(x => x.ToPsObject()).ToList());
        }
        else
        {
            WriteObject(summedLogs);

        }
        SetGlobal("LiraLastWorklogSum", summedLogs);
        base.EndProcessing();
    }
}
