using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Get, "LiraAvailableConfigurations")]
[OutputType(typeof(LiraPS.Configuration.Information))]
public sealed class GetAvailableConfigurations : LiraCmdlet
{
    protected override void BeginProcessing()
    {
        Console.CancelKeyPress += DumpLogEvent;

        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        foreach (var confInfo in GetAvailable())
        {
            WriteObject(confInfo);
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
