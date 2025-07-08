using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using LiraPS.Extensions;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Remove, "LiraWorklog")]
[Alias("Remove-Worklog")]
public class RemoveWorklog : LiraCmdlet
{
    [Parameter(ValueFromPipeline = true, Mandatory = true)]
    public Worklog[] Worklogs { get; set; } = [];
    [Parameter]
    public SwitchParameter Force { get; set; }
    private List<Worklog> _worklogsAccumulated = [];
    protected override void ProcessRecord()
    {
        _worklogsAccumulated.AddRange(Worklogs);
    }
    protected override void EndProcessing()
    {
        bool yesToAll = Force.IsPresent;
        bool noToAll = false;
        var pluralWorklogs = _worklogsAccumulated.Count == 1 ? "worklog" : "worklogs";
        List<Worklog> _reallyToBeGone = [];
        if (!yesToAll)
        {
            this.ShouldContinue($"Removing {_worklogsAccumulated.Count} {pluralWorklogs}", $"Do you want to remove the {pluralWorklogs} displayed above?", ref yesToAll, ref noToAll);
        }
        foreach (var worklog in _worklogsAccumulated)
        {
            if (noToAll)
            {
                UserCancel("worklog adding");
            }
            if (yesToAll || this.ShouldContinue($"Remove {worklog.Started.UnambiguousForm()} - {worklog.TimeSpent.PrettyTime()}  {worklog.Comment}", $"Do you want to remove this worklog?", ref yesToAll, ref noToAll))
            {
                _reallyToBeGone.Add(worklog);
            }
        }
        foreach (var worklog in _reallyToBeGone)
        {
            ENSURE_TESTING(worklog.Issue.Key);

            LiraSession.Logger.LogInformation("Removing worklog {id}", worklog.ID);
            var machine = LiraSession.Client.GetRemoveWorklogMachine();
            var state = machine.GetStartState(worklog);
            while (!state.IsFinished)
            {
                var t = machine.Process(state).GetAwaiter();
                state = t.GetResult();
                PrintLogs();
            }

            if (state.RemovalSuccess)
            {
                WriteObject($"Worklog {worklog.ID} has been deleted");
                LiraSession.Logger.LogInformation("Removed worklog {id}", worklog.ID);
            }
            else
            {
                WriteObject($"Worklog {worklog.ID} has NOT been deleted");
                LiraSession.Logger.LogWarning("Failed to removing worklog {id}", worklog.ID);
            }
        }
        base.EndProcessing();
    }
}

