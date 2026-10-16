---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Add-LiraWorklog
---

# Add-LiraWorklog

## SYNOPSIS

Adds a new worklog to the specified issue.

## SYNTAX

### __AllParameterSets

```
Add-LiraWorklog [-Issue <string>] [-Started <DateTimeOffset>] [-Duration <timespan>]
 [-Comment <string>] [-NoConfirm] [<CommonParameters>]
```

## DESCRIPTION

Adds a new worklog to the specified issue.
The details of the worklog can be specified as parameters or interactively.
It is possible to specify some arguments and complete the rest interactively.
All parameters can be parsed from text with the recommended form being how Jira does it.
It is possible to specify some arguments and complete the rest interactively.
All parameters can be parsed from text with the recommended form being how Jira does it.

## EXAMPLES

### Example 1

Add-LiraWorklog

### Example 2

Add-LiraWorklog -Issue "XYZ-123" -Started -7 -Duration "2h30m" -Comment "Worked on the feature implementation."


## PARAMETERS

### -Comment

The comment (description) of the worklog. Can be empty. 

```yaml
Type: System.String
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

### -Duration

How much time was spent on the logged work. 

It accepts instances of TimeSpan or Jira-formatted duration values (e.g. 2h30m, 2h 30m, 150m, 2.5h).

If not specified, the user will be prompted to select an issue.

```yaml
Type: System.TimeSpan
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Time
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

Which issue to add the worklog to. 

It accepts the issue key (e.g. XYZ-123) or instances of Lira.Objects.Issue. 

If not specified, the user will be prompted to select an issue.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Key
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -NoConfirm

Skips the confirmation prompt when adding a worklog.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Yes
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

### -Started

The date and time when the work was started.

Accepts various string representations of date and time:
 - multiple formats of date and time (e.g. "2023-01-01 10:00", "01/01/2023 10:00 AM", "2023-01-01T10:00:00Z")
 - relative date expressed in days, non-positive (0 for today, -1 for yesterday, -7 for a week ago)
 - Jira date functions (e.g. startOfDay(), endOfWeek(), startOfMonth())
	- Parentheses are optional
	- Functions accept arguments, e.g. endOfDay(-1) for the end of yesterday
 - keywords such as now, today, yesterday

```yaml
Type: System.DateTimeOffset
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Date
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

### System.String

You can pipe an issue key or issue name to the -Issue parameter.

## OUTPUTS

### Lira.Objects.Worklog

Returns the newly created worklog object containing the worklog details.

## NOTES

- If any parameter is not provided, you will be prompted interactively to enter it.
- The Issue parameter accepts completion from recently logged issues.
- Date and time values are flexible and support multiple formats.

## RELATED LINKS

See Get-LiraConfiguration




