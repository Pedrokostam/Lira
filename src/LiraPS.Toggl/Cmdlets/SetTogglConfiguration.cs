using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace LiraPS.Toggl.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "LiraTogglConfiguration", DefaultParameterSetName = "SET")]
    public sealed  class SetTogglConfiguration : TogglCmdlet
    {
        [Parameter(ParameterSetName="SET")]
        public string? ApiKey { get; set; }

        [Parameter(ParameterSetName="CLEAR", Mandatory =true)]
        public SwitchParameter Clear { get; set; }

        protected override void BeginProcessing()
        {
            if (!LiraSession.TestSessionDataAvailable())
            {
                throw new PSInvalidOperationException("You have to configure Jira session parameters first. Call Set-LiraConfiguration.");
            }
        }

        protected override void ProcessRecord()
        {
            if (Clear.IsPresent)
            {
                LiraSession.Config.Addons.Remove(TogglAddonKey);
            }
            else { 
            if (string.IsNullOrWhiteSpace(ApiKey)) {
                ApiKey = ReadInput("Provide API key for Toggl", asSecure : true);
                EnsureNotEmpty(ApiKey, "Api key");
            }
            LiraSession.Config.Addons[TogglAddonKey] = ApiKey;
            }
            LiraSession.Config.Save();
        }
    }
}
