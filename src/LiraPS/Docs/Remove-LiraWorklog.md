---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Remove-LiraWorklog
---

# Remove-LiraWorklog

## SYNOPSIS

See related parameters

## SYNTAX

### __AllParameterSets

```
Remove-LiraWorklog [-Worklogs] <Worklog[]> [-Force] [<CommonParameters>]
```

## DESCRIPTION

Removes one or more worklogs from Jira with optional confirmation prompts.
Use -Force to skip confirmation prompts.

## EXAMPLES

### Example 1

$worklog | Remove-LiraWorklog

Removes the specified worklog after confirmation.

### Example 2

Get-LiraWorklog -Period Today | Remove-LiraWorklog -Force

Removes all worklogs from today without confirmation.

## PARAMETERS

### -Force

See related parameters

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

See related parameters

```yaml
Type: Lira.Objects.Worklog[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
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

N/A

## OUTPUTS

### System.Object

N/A

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




