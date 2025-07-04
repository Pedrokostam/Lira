using System;
using System.Management.Automation;
using Lira.Objects;

namespace LiraPS.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Configuration")]
    [OutputType(typeof(LiraPS.Configuration.Information))]
    public class GetConfiguration : LiraCmdlet
    {
        protected override void ProcessRecord()
        {
            // Ensure configuration is loaded and valid
            if (LiraSession.Config is null)
            {
                Terminate(
                        new InvalidOperationException("No active configuration found. Please run Set-Configuration first."),
                        "NoActiveConfiguration",
                        ErrorCategory.ResourceUnavailable
                );
            }

            WriteObject(LiraSession.Config.ToInformation());
        }
    }
}
