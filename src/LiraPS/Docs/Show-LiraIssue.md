---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Show-LiraIssue
---

# Show-LiraIssue

## SYNOPSIS

See related parameters

## SYNTAX

### __AllParameterSets

```
Show-LiraIssue [[-Item] <IssueStem[]>] [-ShowLink] [<CommonParameters>]
```

## DESCRIPTION

Opens an issue in the default web browser to view in Jira.
Alternatively, outputs the issue URL using -ShowLink parameter.

## EXAMPLES

### Example 1

Get-LiraIssue -Id "XYZ-123" | Show-LiraIssue

Opens the issue in the default web browser.

### Example 2

Get-LiraIssue -Id "XYZ-123" | Show-LiraIssue -ShowLink

Outputs the issue URL without opening the browser.

## PARAMETERS

### -Item

See related parameters

```yaml
Type: Lira.Objects.IssueStem[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ShowLink

See related parameters

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Link
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### Lira.Objects.IssueStem

N/A

### Lira.Objects.IssueStem[]

N/A

## OUTPUTS

### System.Object

N/A

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




