using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Get, "AvailableConfigurations")]
[OutputType(typeof(LiraPS.Configuration.Information))]
public class GetAvailableConfigurations : LiraCmdlet
{
    [Parameter(Position = 0)]
    public string? Name { get; set; }

    protected override void BeginProcessing()
    {
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            foreach (var confInfo in GetAvailable())
            {
                WriteObject(confInfo);
            }
        }
    }
    internal static IEnumerable<Configuration.Information> GetAvailable()
    {
        foreach (var path in Configuration.GetAvailableProfiles())
        {
            yield return Configuration.Load(Path.GetFileNameWithoutExtension(path)).ToInformation();
        }
    }
}
