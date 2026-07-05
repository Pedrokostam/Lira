---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Get-LiraIssue
---

# Get-LiraIssue

## SYNOPSIS

See related parameters

## SYNTAX

### __AllParameterSets

```
Get-LiraIssue [-Id] <string[]> [-ForceRefresh] [<CommonParameters>]
```

## DESCRIPTION

Fetches complete issue details from Jira by issue key.
Caches results locally to improve performance. Use -ForceRefresh to bypass the cache.

## EXAMPLES

### Example 1

Get-LiraIssue -Id "XYZ-123"

Retrrieves the issue XYZ-123 with all details.

### Example 2

Get-LiraIssue -Id "XYZ-123" -ForceRefresh

Fetches the issue fresh from the server, bypassing cache.

## PARAMETERS

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

### -Id

See related parameters

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Key
- Issue
ParameterSets:
- Name: (All)
  Position: 0
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

### System.String

N/A

### System.String[]

N/A

## OUTPUTS

### Lira.Objects.Issue

The requested issue object with all details including subtasks and worklogs.

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




