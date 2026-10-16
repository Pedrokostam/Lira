---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Get-LiraWorklogSum
---

# Get-LiraWorklogSum

## SYNOPSIS

Sums and groups worklog time by specified properties.

## SYNTAX

### STRUCT (Default)

```
Get-LiraWorklogSum [[-Properties] <Property[]>] [-Worklogs <Worklog[]>] [<CommonParameters>]
```

## DESCRIPTION

Calculates total time spent grouped by one or more properties such as issue, user, day, week, month, or year.
Useful for reporting and time tracking analysis. Requires worklogs as input (from Get-LiraWorklog).

## EXAMPLES

### Example 1

Get-LiraWorklog -Period ThisMonth | Get-LiraWorklogSum -Properties Issue, Day

Sums worklogs grouped by issue and day.

### Example 2

Get-LiraWorklog -Period ThisMonth | Get-LiraWorklogSum

Calculates total time for all worklogs in this month.

## PARAMETERS

### -Properties

See related parameters

```yaml
Type: LiraPS.Arguments.Property[]
DefaultValue: ''
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
  Position: Named
  IsRequired: false
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

### LiraPS.Wrappers.WorklogTimespanCalculatedGroup

Grouped worklog data with calculated total time spans for each group.

## NOTES

- Requires worklogs as input (typically from Get-LiraWorklog).
- Results are stored in the PowerShell global variable $LiraLastSum.

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




