---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Export-LiraWorklog
---

# Export-LiraWorklog

## SYNOPSIS

Exports worklogs to a specified format (JSON or CSV).

## SYNTAX

### __AllParameterSets

```
Export-LiraWorklog [[-As] <ExportMode>] -Worklogs <Worklog[]> [<CommonParameters>]
```

## DESCRIPTION

Exports worklogs in multiple formats including JSON and CSV for reporting or backup purposes.
Supports both single worklog and bulk export scenarios.
JSON export can optionally output as an array or compress output. CSV export supports different separators.

## EXAMPLES

### Example 1

Get-LiraWorklog -Period ThisMonth | Export-LiraWorklog -As Json

Exports this month's worklogs as formatted JSON.

### Example 2

Get-LiraWorklog -Period ThisMonth | Export-LiraWorklog -As Csv -Separator Semicolon

Exports this month's worklogs as CSV with semicolon separator.

## PARAMETERS

### -As

Specifies the export format. Valid values are Json and Csv. Defaults to Json.

```yaml
Type: LiraPS.Arguments.ExportMode
DefaultValue: 'Json'
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- Json
- Csv
HelpMessage: ''
```

### -AsArray

When exporting to JSON with a single worklog, outputs as an array instead of a single object. Only applicable when -As Json is used.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Compress

When exporting to JSON, outputs compressed (minified) JSON without formatting. Only applicable when -As Json is used.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Worklogs

The worklogs to export. Can be piped from Get-LiraWorklog or other cmdlets that return Worklog objects.

```yaml
Type: Lira.Objects.Worklog[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### Lira.Objects.Worklog

N/A

### Lira.Objects.Worklog[]

You can pipe an array of worklog objects to the -Worklogs parameter.

## OUTPUTS

### System.Object

Returns the formatted worklog data as JSON or CSV string.

## NOTES

- When exporting to CSV, you can specify different separators: Comma, Semicolon, or Pipe.
- The -NoHeader switch (for CSV) prevents the header row from being included.
- JSON export includes all worklog properties in their full form.

## RELATED LINKS

- Get-LiraWorklog





