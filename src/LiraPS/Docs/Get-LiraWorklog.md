---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Get-LiraWorklog
---

# Get-LiraWorklog

## SYNOPSIS

See related parameters

## SYNTAX

### PERIOD (Default)

```
Get-LiraWorklog [[-Period] <Period>] [-User <string[]>] [-Issue <string[]>] [-Labels <string[]>]
 [-Components <string[]>] [-Status <string[]>] [-Chunk <int>] [-ForceRefresh] [<CommonParameters>]
```

### MANUALDATE

```
Get-LiraWorklog [-DateFrom <IJqlDate>] [-DateTo <IJqlDate>] [-User <string[]>] [-Issue <string[]>]
 [-Labels <string[]>] [-Components <string[]>] [-Status <string[]>] [-Chunk <int>] [-ForceRefresh]
 [<CommonParameters>]
```

## DESCRIPTION

Retrieves worklogs from Jira with flexible filtering by date range, user, issue, labels, and components.
Supports predefined periods (ThisMonth, LastWeek, etc.) or manual date ranges.
Caches results for improved performance with subsequent queries.

## EXAMPLES

### Example 1

Get-LiraWorklog -Period ThisMonth

Retrrieves all worklogs for this month.

### Example 2

Get-LiraWorklog -DateFrom "-7" -DateTo "0" -User "me"

Retrrieves worklogs logged by the current user for the past week.

## PARAMETERS

### -Chunk

See related parameters

```yaml
Type: System.Int32
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

### -Components

See related parameters

```yaml
Type: System.String[]
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

### -DateFrom

See related parameters

```yaml
Type: Lira.Jql.IJqlDate
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: MANUALDATE
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DateTo

See related parameters

```yaml
Type: Lira.Jql.IJqlDate
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: MANUALDATE
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ForceRefresh

See related parameters

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Refresh
- Force
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

### -Issue

See related parameters

```yaml
Type: System.String[]
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

### -Labels

See related parameters

```yaml
Type: System.String[]
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

### -Period

See related parameters

```yaml
Type: LiraPS.Arguments.Period
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: PERIOD
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Status

See related parameters

```yaml
Type: System.String[]
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

### -User

See related parameters

```yaml
Type: System.String[]
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

### System.String

N/A

### System.String[]

N/A

## OUTPUTS

### Lira.Objects.Worklog

Worklog objects containing duration, start date, issue reference, and user details.

## NOTES

- Worklogs are cached locally for performance. Use -ForceRefresh to bypass the cache.
- The last query results are stored in the PowerShell global variable $LiraLastWorklogs.

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




