using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Lira.Objects;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Show, "LiraIssue")]
[Alias("Invoke-Issue")]
public sealed class InvokeIssue : LiraCmdlet
{
    [Parameter(Position = 0, ValueFromPipeline = true)]
    public IssueStem[] Item { get; set; } = default!;

    [Parameter]
    [Alias("Link")]
    public SwitchParameter ShowLink { get; set; }

    protected override void BeginProcessing()
    {
        Console.CancelKeyPress += DumpLogEvent;

        // base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        foreach (var stem in Item)
        {
            var url = stem.WebLink;
            if (ShowLink.IsPresent)
            {
                WriteObject(url.ToString());
            }
            else
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo(url.ToString()) { UseShellExecute = true });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", url.ToString());
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", url.ToString());
                }
            }
        }
        base.ProcessRecord();
    }
}
