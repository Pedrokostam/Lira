using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Lira.Objects;

namespace LiraPS.Cmdlets;

public readonly record struct WorklogSum
{
    public ImmutableArray<string> GroupProperties { get; init; }
    public string Header { get; init; } = "";
    private ImmutableArray<Worklog> Worklogs { get; init; }
    public int WorklogCount => Worklogs.Length;
    public TimeSpan TimeSpent => TimeSpan.FromTicks(Worklogs.Select(x => x.TimeSpent.Ticks).Sum());
    public WorklogSum(IEnumerable<string> groupProps, string header,IEnumerable<Worklog> worklogs)
    {
        Worklogs = worklogs.ToImmutableArray();
        GroupProperties = groupProps.ToImmutableArray();
        Header = header;
    }

}
[Alias("logsum")]
[Cmdlet(VerbsCommon.Get, "WorklogSum", DefaultParameterSetName = "STRUCT")]
[OutputType(typeof(TimeSpan),ParameterSetName =["SIMPLE"])]
[OutputType(typeof(WorklogSum),ParameterSetName =["STRUCT"])]
public class Get_WorklogSum:LiraCmdlet
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    public Worklog[] Worklogs { get; set; } = [];

    [Parameter(ParameterSetName ="SIMPLE")]
    public SwitchParameter Simple {  get; set; }

    private List<Worklog> _worklogs = [];

    protected override void BeginProcessing()
    {
        // no need to test session
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        foreach (var worklog in Worklogs)
        { 
            _worklogs.Add(worklog);
        }
        base.ProcessRecord();
    }
    protected override void EndProcessing()
    {
        var summedLogs = new WorklogSum([],"test",_worklogs);
        if (Simple.IsPresent)
        {
            WriteObject( summedLogs.TimeSpent);
        }
        else
        {
            WriteObject( summedLogs);

        }
        SetGlobal("LiraLastWorklogSum", summedLogs);
        base.EndProcessing();
    }
}
