using Lira.Toggl;
using Microsoft.Extensions.Logging;
using System.Management.Automation;

namespace LiraPS.Toggl.Cmdlets
{
    [Cmdlet(VerbsCommon.Clear, "LiraTogglCache")]
    public sealed class ClearTogglCache : TogglCmdlet
    {
        [Parameter]
        public SwitchParameter All { get; set; }

        protected override void BeginProcessing()
        {
            // No need to load anything here. No client - no cache.
        }

        protected override void ProcessRecord()
        {
            TogglClient.ClearAllCaches(All.IsPresent);
            LiraSession.Logger.LogDebug(All.IsPresent ? "Cleared all cached Toggl data" : "Cleared cached Toggl entries");
        }
    }
}
