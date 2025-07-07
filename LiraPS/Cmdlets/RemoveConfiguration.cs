using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using LiraPS.Completers;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Remove, "LiraConfiguration", DefaultParameterSetName = "MANUAL")]
[OutputType(typeof(LiraPS.Configuration.Information))]
public class RemoveConfiguration : LiraCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ParameterSetName = "MANUAL")]
    [ArgumentCompleter(typeof(ConfigurationCompletionAttribute))]
    [MinLength(1)]
    public string? Name { get; set; }
    [Parameter(Mandatory = true, ParameterSetName = "ALL")]
    public SwitchParameter All { get; set; }

    protected override void BeginProcessing()
    {
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        if (ParameterSetName == "MANUAL")
        {
            var path = Configuration.GetProfilePath_Null(Name);
            if (!File.Exists(path))
            {
                Terminate(
                        new Exception("Specified configuration does not exist"),
                        "InvalidConfigName",
                        ErrorCategory.InvalidArgument
                       );
            }
            File.Delete(path);
        }
        else
        {
            foreach (var f in Configuration.GetAvailableProfiles())
            {
                File.Delete(f);
            }
        }
        LiraSession.Config = null!;
    }
}
