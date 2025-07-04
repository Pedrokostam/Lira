using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Lira.Objects;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets;

public readonly record struct WorklogSum
{
    public ImmutableArray<Group> GroupProperties { get; init; }
    public string Header { get; init; } = "";
    private ImmutableArray<Worklog> Worklogs { get; init; }
    public int WorklogCount => Worklogs.Length;
    public TimeSpan TimeSpent => TimeSpan.FromTicks(Worklogs.Select(x => x.TimeSpent.Ticks).Sum());
    public WorklogSum(IEnumerable<Group> groupProps, string header, IEnumerable<Worklog> worklogs)
    {
        Worklogs = worklogs.ToImmutableArray();
        GroupProperties = groupProps.ToImmutableArray();
        Header = header;
    }

}
public enum Group
{
    User,
    Day,
    Month,
    Issue,
    Year,
}
[Alias("logsum")]
[Cmdlet(VerbsCommon.Get, "WorklogSum", DefaultParameterSetName = "STRUCT")]
[OutputType(typeof(TimeSpan), ParameterSetName = ["SIMPLE"])]
[OutputType(typeof(WorklogSum), ParameterSetName = ["STRUCT"])]
public class GetWorklogSum : LiraCmdlet
{
    [Parameter(Mandatory = false, ValueFromPipeline = true)]
    public Worklog[] Worklogs { get; set; } = [];

    public Group[] Groups { get; set; } = [Group.Month];

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

        var summedLogs = new WorklogSum([], "test", _worklogs);
        if (Simple.IsPresent)
        {
            WriteObject(summedLogs.TimeSpent);
        }
        else
        {
            WriteObject(summedLogs);

        }
        SetGlobal("LiraLastWorklogSum", summedLogs);
        base.EndProcessing();
    }
}
