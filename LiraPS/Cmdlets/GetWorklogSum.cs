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
using LiraPS.Outputs;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets;
[Alias("logsum", "sum", "Get-WorklogSum")]
[Cmdlet(VerbsCommon.Get, "LiraWorklogSum", DefaultParameterSetName = "STRUCT")]
[OutputType(typeof(WorklogSum), ParameterSetName = ["STRUCT"])]
public class GetWorklogSum : LiraCmdlet
{
    [Parameter(Mandatory = false, ValueFromPipeline = true)]
    public Worklog[] Worklogs { get; set; } = [];

    [Parameter(Position = 0, ValueFromPipeline = false)]
    public Property[] Groups { get; set; } = [Property.None];

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
        WriteObject(summedLogs,enumerateCollection:true);
        SetGlobal("LiraLastWorklogSum", summedLogs);
        base.EndProcessing();
    }
}
