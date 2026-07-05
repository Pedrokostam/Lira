---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Update-LiraWorklog
---

# Update-LiraWorklog

## SYNOPSIS

See related parameters

## SYNTAX

### DEFAULT (Default)

```
Update-LiraWorklog [-Worklog] <Worklog> [-NewStarted <Object>] [-NewDuration <Object>]
 [-NewComment <Object>] [-Force] [<CommonParameters>]
```

### ADDTIME

```
Update-LiraWorklog [-Worklog] <Worklog> [-NewStarted <Object>] [-AddDuration <Object>]
 [-NewComment <Object>] [-Force] [<CommonParameters>]
```

## DESCRIPTION

Modifies an existing worklog with new start date, duration, and/or comment.
Supports adding time to existing duration with -AddDuration parameter.

## EXAMPLES

### Example 1

$worklog | Update-LiraWorklog -NewComment "Updated comment"

Updates the comment of an existing worklog.

### Example 2

$worklog | Update-LiraWorklog -AddDuration "1h"

Adds 1 hour to the existing worklog duration.

## PARAMETERS

### -AddDuration

See related parameters

```yaml
Type: System.Object
DefaultValue: ''
SupportsWildcards: false
Aliases:
- AddTime
ParameterSets:
- Name: ADDTIME
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

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

### -NewComment

See related parameters

```yaml
Type: System.Object
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Comment
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

### -NewDuration

See related parameters

```yaml
Type: System.Object
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Time
- NewTime
ParameterSets:
- Name: DEFAULT
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -NewStarted

See related parameters

```yaml
Type: System.Object
DefaultValue: ''
SupportsWildcards: false
Aliases:
- NewDate
- Started
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

### -Worklog

See related parameters

```yaml
Type: Lira.Objects.Worklog
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

## OUTPUTS

### System.Object

N/A

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




