using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using Lira.Objects;
using LiraPS.Arguments;
using Lira.Extensions;

namespace LiraPS.Cmdlets;

public class CsvDynamicParameters
{
    [Parameter]
    public Separator Separator { get; set; } =Separator.Comma;
    [Parameter]
    public SwitchParameter NoHeader { get; set; }
}
public class JsonDynamicParameters
{
    [Parameter]
    public SwitchParameter AsArray { get; set; }
    [Parameter]
    public SwitchParameter Compress { get; set; }
}

public enum Separator
{
    Comma,
    Semicolon,
    Pipe,
}
[Cmdlet(VerbsData.Export, "Worklog")]
public class ExportWorklog : LiraCmdlet, IDynamicParameters
{
    [Parameter(Position = 0)]
    public ExportMode As { get; set; } = ExportMode.Json;

    //[Parameter()]
    //public SwitchParameter UseDisplayName { get; set; }

    [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    public Worklog[] Worklogs { get; set; } = [];

    private List<Worklog> _allWorklogs = [];

    private CsvDynamicParameters? _csvDynamicParameters;
    private JsonDynamicParameters? _jsonDynamicParameters;

    public object? GetDynamicParameters()
    {
        if (As == ExportMode.Json)
        {
            _jsonDynamicParameters = new();
            return _jsonDynamicParameters;
        }
        else if (As == ExportMode.Csv)
        {
            _csvDynamicParameters = new();
            return _csvDynamicParameters;
        }
        return null;
    }

    protected override void BeginProcessing()
    {
        //base.BeginProcessing();
    }
    protected override void ProcessRecord()
    {
        foreach (var worklog in Worklogs)
        {
            _allWorklogs.Add(worklog);
        }
    }
    protected override void EndProcessing()
    {
        if (_allWorklogs.Count == 0)
        {
            return;
        }
        if (As == ExportMode.Csv)
        {
            var sep = _csvDynamicParameters!.Separator switch
            {
                Separator.Comma => ",",
                Separator.Semicolon => ";",
                Separator.Pipe => "|",
                _ => throw new InvalidOperationException(),
            };
            if (!_csvDynamicParameters!.NoHeader.IsPresent)
            {
                WriteObject(Lira.Extensions.WorklogExtensions.GetCsvHeaderLine(sep));
            }
            WriteObject(_allWorklogs.Select(x => x.GetCsvLine(sep)));
        }
        else if (As == ExportMode.Json)
        {
            var opt = new JsonSerializerOptions() { WriteIndented = !_jsonDynamicParameters!.Compress.IsPresent };
            if (_allWorklogs.Count == 1 && !_jsonDynamicParameters!.AsArray.IsPresent)
            {
                if (!_jsonDynamicParameters!.AsArray.IsPresent)
                {
                    WriteObject(JsonSerializer.Serialize(_allWorklogs[0].GetDict(), opt));
                    return;
                }
            }
            WriteObject(JsonSerializer.Serialize(_allWorklogs.Select(x => x.GetDict()), opt));
        }
    }

}
