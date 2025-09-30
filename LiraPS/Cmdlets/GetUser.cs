using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;
using Lira.StateMachines;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "LiraUser")]
    [OutputType(typeof(UserDetails))]
    public class GetUser : LiraCmdlet
    {
        [Alias("ID", "DisplayName")]
        [AllowEmptyCollection]
        [Parameter(
            Mandatory = false,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Name { get; set; } = [];

        protected override void ProcessRecord()
        {
            if (Name.Length == 0)
            {
                throw new ArgumentException("No user specified! Hint: use \"me\" to choose currently logged user", nameof(Name));
            }
            foreach (var userName in Name)
            {
                var replacement = ReplaceCurrentUserAlias(userName);
                if (replacement.Equals(LiraSession.Client.Myself.Name))
                {
                    LiraSession.Logger.LogDebug("Skipped fetching user - used client's owner");
                    PrintLogs();
                    WriteObject(LiraSession.Client.Myself);
                    continue;
                }
                var machine = LiraSession.Client.GetUsersStateMachine();
                var state = machine.GetStartState(replacement);
                while (!state.IsFinished)
                {
                    var t = machine.Process(state).GetAwaiter();
                    state = t.GetResult();
                    PrintLogs();
                }
                WriteObject(state.Users,enumerateCollection:true);
            }
        }

    }

}
