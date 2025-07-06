using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lira.Objects;
using Lira.StateMachines;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Issue")]
    [OutputType(typeof(Issue))]
    public class GetIssue : LiraCmdlet
    {
        private const int ActivityId = 1379;
        private const int SubActivityId = 1380;

        [Alias("Key")]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Id { get; set; } = default!;

        public SwitchParameter NoSubtasks { get; set; }

        private readonly List<Issue> _issues = [];

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            int percentComplete = 0;
            var machine = new IssueMachine(LiraSession.Client);
            foreach (var issueId in Id)
            {
                WriteProgress(new ProgressRecord(ActivityId, $"Fetching issues...", issueId) { PercentComplete = percentComplete });
                var state = machine.GetStartState(issueId);
                while (!state.IsFinished)
                {
                    var t = machine.Process(state).GetAwaiter();
                    state = t.GetResult();
                    var subtaskCount = state.IssueLite?.ShallowSubtasks.Count ?? 0;
                    if (state.NextStep == IssueMachine.Steps.LoadWorklogs && subtaskCount > 0)
                    {
                        WriteProgress(new ProgressRecord(SubActivityId, $"Fetching subtasks of {issueId}...", $"{subtaskCount} subtasks") { ParentActivityId = ActivityId });
                    }
                    PrintLogs();
                }
                if (state.Issue is null)
                {
                    continue;
                }
                _issues.Add(state.Issue);
                percentComplete += 100 / Id.Length;
                WriteProgress(new ProgressRecord(SubActivityId, $"Fetched  subtasks...", "Subtasks fetched") { ParentActivityId = ActivityId, RecordType = ProgressRecordType.Completed });
            }
            WriteProgress(new ProgressRecord(ActivityId, $"Fetched issues...", "All") { RecordType = ProgressRecordType.Completed });
        }
        protected override void EndProcessing()
        {
            WriteObject(_issues);
        }

    }

}
