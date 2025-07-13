using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Lira.Grouping;
using Lira.Objects;
using LiraPS.Arguments;
using LiraPS.Extensions;
using LiraPS.Wrappers;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets;
[Alias("logsum", "sum", "Get-WorklogTimespanCalculatedGroup")]
[Cmdlet(VerbsCommon.Get, "LiraWorklogSum", DefaultParameterSetName = "STRUCT")]
[OutputType(typeof(WorklogTimespanCalculatedGroup), ParameterSetName = ["STRUCT"])]
public class GetWorklogSum : LiraCmdlet
{
    [Parameter(Mandatory = false, ValueFromPipeline = true)]
    public Worklog[] Worklogs { get; set; } = [];

    [Parameter(Position = 0, ValueFromPipeline = false)]
    public Property[] Properties { get; set; } = [Property.None];

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
                Terminate(new PSArgumentException("No worklogs provided"), "NoWorklogs", ErrorCategory.InvalidArgument);
            }
        }
        PrintLogs();
        foreach (var worklog in Worklogs)
        {
            _worklogs.Add(worklog);
        }
        base.ProcessRecord();
    }

    protected override void EndProcessing()
    {
        WorklogGroupingTimeSummator summer = [];
        foreach (var g in Properties)
        {
            switch (g)
            {
                case Property.Issue:
                    summer.Add(WorklogIssueGrouper.Instance);
                    break;
                case Property.User:
                    summer.Add(WorklogAuthorGrouper.Instance);
                    break;
                case Property.Day:
                    summer.Add(WorklogDayGrouper.Started);
                    break;
                case Property.Week:
                    summer.Add(WorklogWeekGrouper.Started);
                    break;
                case Property.Month:
                    summer.Add(WorklogMonthGrouper.Started);
                    break;
                case Property.Year:
                    summer.Add(WorklogYearGrouper.Started);
                    break;
                case Property.None:
                default:
                    break;
            }
        }
        var groups = summer
            .Group(_worklogs)
            .Select(WorklogTimespanCalculatedGroup.Wrap)
            .ToList();
        WriteObject(groups, enumerateCollection: false);
        SetGlobal("LiraLastSum", groups);
        base.EndProcessing();
    }
}
