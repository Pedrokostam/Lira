using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lira;
using Lira.Objects;
using Lira.StateMachines;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "LiraIssue")]
    [OutputType(typeof(Issue))]
    [Alias("Get-Issue")]
    public sealed class GetLiraIssue : LiraCmdlet

    {
        private const int ActivityId = 1379;
        private const int SubActivityId = 1380;

        [Alias("Key","Issue")]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Id { get; set; } = default!;
        //[Parameter]
        //public SwitchParameter NoSubtasks { get; set; }

        [Parameter]
        [Alias("Refresh", "Force")]
        public SwitchParameter ForceRefresh { get; set; }

        private readonly List<Issue> _issues = [];



        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            FetchIssues(Id, ForceRefresh.IsPresent, _issues, this);
        }

        internal static void FetchIssues(IList<string> ids, bool forceRefresh, List<Issue> issues, LiraCmdlet cmdlet)
        {
            int percentComplete = 0;
            var machine = LiraSession.Client.GetFetchIssueStateMachine();
            foreach (var issueId in ids)
            {
                if (forceRefresh)
                {
                    LiraSession.Client.RemoveFromIssueCache(issueId);
                }
                cmdlet.WriteProgress(new ProgressRecord(ActivityId, $"Fetching issues...", issueId) { PercentComplete = percentComplete });
                var state = machine.GetStartState(issueId);
                while (!state.IsFinished)
                {
                    var t = machine.Process(state).GetAwaiter();
                    state = t.GetResult();
                    var subtaskCount = state.IssueLite?.ShallowSubtasks.Count ?? 0;
                    if (state.NextStep == FetchIssueStateMachine.Steps.LoadWorklogs && subtaskCount > 0)
                    {
                        cmdlet.WriteProgress(new ProgressRecord(SubActivityId, $"Fetching subtasks of {issueId}...", $"{subtaskCount} subtasks") { ParentActivityId = ActivityId });
                    }
                    cmdlet.PrintLogs();
                }
                if (state.Issue is null)
                {
                    continue;
                }
                issues.Add(state.Issue);
                percentComplete += 100 / ids.Count;
                cmdlet.WriteProgress(new ProgressRecord(SubActivityId, $"Fetched  subtasks...", "Subtasks fetched") { ParentActivityId = ActivityId, RecordType = ProgressRecordType.Completed });
            }
            cmdlet.WriteProgress(new ProgressRecord(ActivityId, $"Fetched issues...", "All") { RecordType = ProgressRecordType.Completed });
        }

        protected override void EndProcessing()
        {
            WriteObject(_issues, enumerateCollection: true);
            base.EndProcessing();
        }

    }

}
