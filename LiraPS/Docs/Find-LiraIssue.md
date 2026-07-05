---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Find-LiraIssue
---

# Find-LiraIssue

## SYNOPSIS

Searches for issues using JQL (Jira Query Language) with filter criteria.

## SYNTAX

### NoFullFetch (Default)

```
Find-LiraIssue [-ReportedDateFrom <IJqlDate>] [-ReportedDateTo <IJqlDate>]
 [-ModifiedDateFrom <IJqlDate>] [-ModifiedDateTo <IJqlDate>] [-Reporter <string[]>]
 [-Assignee <string[]>] [-Labels <string[]>] [-Components <string[]>] [-Status <string[]>]
 [-Chunk <int>] [-ForceRefresh] [<CommonParameters>]
```

### FullFetch

```
Find-LiraIssue -FullFetch [-ReportedDateFrom <IJqlDate>] [-ReportedDateTo <IJqlDate>]
 [-ModifiedDateFrom <IJqlDate>] [-ModifiedDateTo <IJqlDate>] [-Reporter <string[]>]
 [-Assignee <string[]>] [-Labels <string[]>] [-Components <string[]>] [-Status <string[]>]
 [-Chunk <int>] [-ForceRefresh] [<CommonParameters>]
```

## DESCRIPTION

Searches for issues in Jira using various filter criteria including dates, users, labels, components, and status.
By default returns lightweight issue information. Use -FullFetch to retrieve complete issue details including worklogs.

## EXAMPLES

### Example 1

Find-LiraIssue -Assignee "me" -Status "In Progress"

Finds all issues currently assigned to the current user that are in progress.

### Example 2

Find-LiraIssue -ReportedDateFrom "-30" -Labels "bug" -FullFetch

Finds all bug issues reported in the last 30 days with full details.

## PARAMETERS

### -Assignee

Filter results to issues assigned to specific users. Supports "me" as alias for current user.

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

### -Chunk

Limits the number of issues fetched per query request. Use -1 (default) for no limit.

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

Filter results to issues with specific components.

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

### -ForceRefresh

Skips the cache and forces a fresh query from the server.

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

### -FullFetch

Retrieves complete issue details including all properties and worklogs. Without this switch, only lightweight issue information is returned.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: FullFetch
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Labels

Filter results to issues with specific labels.

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

### -ModifiedDateFrom

Filter results to issues modified after this date. Accepts JQL date expressions.

```yaml
Type: Lira.Jql.IJqlDate
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

### -ModifiedDateTo

Filter results to issues modified before this date. Accepts JQL date expressions.

### -ReportedDateFrom

Filter results to issues reported after this date. Accepts JQL date expressions.

### -ReportedDateTo

Filter results to issues reported before this date. Accepts JQL date expressions.

### -Reporter

Filter results to issues reported by specific users. Supports "me" as alias for current user.

### -Status

Filter results to issues with specific statuses.

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

### Lira.Jql.IJqlDate

You can pipe JQL date expressions to date parameters.

### System.String

You can pipe usernames, labels, components, or statuses to the respective parameters.

### System.String[]

You can pipe arrays of filter values to the respective parameters.

## OUTPUTS

### Lira.Objects.IssueCommon

Returned when -FullFetch is not specified. Contains lightweight issue information.

### Lira.Objects.Issue

Returned when -FullFetch is specified. Contains complete issue details including worklogs.

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




