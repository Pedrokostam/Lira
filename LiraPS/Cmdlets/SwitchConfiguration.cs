using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using LiraPS.Completers;

namespace LiraPS.Cmdlets;
[Cmdlet(VerbsCommon.Switch, "Configuration")]
[OutputType(typeof(LiraPS.Configuration.Information))]
public class SwitchConfiguration : LiraCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    [ArgumentCompleter(typeof(ConfigurationCompletionAttribute))]
    [MinLength(1)]
    public string? Name { get; set; }

    protected override void BeginProcessing()
    {
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        var path = Configuration.GetProfilePath_Null(Name);
        if (!File.Exists(path))
        {
            Terminate(
                    new Exception("Specified configuration does not exist"),
                    "InvalidConfigName",
                    ErrorCategory.InvalidArgument);
        }
        LiraSession.Config = Configuration.Load(Name);
        WriteObject(LiraSession.Config.ToInformation());
    }
}
