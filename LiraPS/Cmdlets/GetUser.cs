using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using Lira.StateMachines;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "LiraUser")]
    [OutputType(typeof(UserDetails))]
    public class GetUser : LiraCmdlet
    {
        [Alias("ID", "DisplayName")]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Name { get; set; } = default!;

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            foreach (var userName in Name)
            {
                var machine = new UsersMachine(LiraSession.Client);
                var state = machine.GetStartState(ReplaceCurrentUserAlias(userName));
                while (!state.IsFinished)
                {
                    var t = machine.Process(state).GetAwaiter();
                    state = t.GetResult();
                    PrintLogs();
                }

                //var task = LiraSession.Client.GetUsers(userName).GetAwaiter();
                //var user = task.GetResult();
                //PrintLogs();
                WriteObject(state.Users);
            }
        }

    }

}
