using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using LiraPS.Extensions;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Remove, "LiraWorklog")]
[Alias("Remove-Worklog")]
public class RemoveWorklog : LiraCmdlet
{
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private void UserCancel()
    {
        Terminate(new InvalidOperationException("User cancelled worklog delettion"), "WorklogDeletionCancel", ErrorCategory.InvalidOperation);
    }
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
                UserCancel();
            }
            if (yesToAll || this.ShouldContinue($"Remove {worklog.Started.UnambiguousForm()} - {worklog.TimeSpent.PrettyTime()}  {worklog.Comment}", $"Do you want to remove this worklog?", ref yesToAll, ref noToAll))
            {
                _reallyToBeGone.Add(worklog);
            }
        }
        foreach ( var worklog in _reallyToBeGone)
        {

        }
        base.EndProcessing();
    }
}

