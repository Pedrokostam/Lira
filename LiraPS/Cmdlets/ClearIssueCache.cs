using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using Microsoft.Extensions.Logging;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Clear, "LiraIssueCache", DefaultParameterSetName = "MANUAL")]
public class ClearIssueCache : LiraCmdlet
{
    [Alias("Key")]
    [Parameter(
           Mandatory = true,
           Position = 0,
           ValueFromPipeline = true,
           ParameterSetName = "MANUAL",
           ValueFromPipelineByPropertyName = true)]
    public string[] Id { get; set; } = default!;
    [Parameter(Mandatory = true, ParameterSetName = "ALL")]
    public SwitchParameter All { get; set; }
    protected override void BeginProcessing()
    {
        // No need to load anything here. No session - no cache.
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        if (All.IsPresent)
        {
            LiraSession.Client?.ClearCache();
                LiraSession.Logger.LogDebug("Cleared all cached entries");
        }
        else
        {
            foreach (var id in Id)
            {
                LiraSession.Client?.RemoveFromIssueCache(id);
                LiraSession.Logger.LogDebug("Cleared entry for {issue}", id);

            }
        }
    }
}
