using System;
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
        [Alias("Key")]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Id { get; set; } = default!;

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {

            var machine = new IssueMachine(LiraSession.Client);
            foreach (var issueId in Id)
            {
                WriteProgress(new ProgressRecord(1379, $"Fetching issues...", issueId));
                var state = machine.GetStartState(issueId);
                while (!state.IsFinished)
                {
                    var t = machine.Process(state).GetAwaiter();
                    state = t.GetResult();
                    PrintLogs();
                }
                if (state.Issue is null)
                {
                    continue;
                }
                WriteObject(state.Issue);
            }
        }

    }

}
