---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Clear-LiraIssueCache
---

# Clear-LiraIssueCache

## SYNOPSIS

Clears the issue cache for the specified issue(s) or all issues.

## SYNTAX

### MANUAL (Default)

```
Clear-LiraIssueCache [-Id] <string[]> [<CommonParameters>]
```

### ALL

```
Clear-LiraIssueCache -All [<CommonParameters>]
```

## DESCRIPTION

Clears the issue cache for the specified issue(s) or all issues.

## EXAMPLES

### Example 1

Clear-LiraIssueCache -Id "XYZ-123"

### Example 2

Clear-LiraIssueCache -All

## PARAMETERS

### -All

Makes it clear the whole cache.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ALL
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Id

Issues to be cleared from the cache.

Accepts issue keys.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- Key
ParameterSets:
- Name: MANUAL
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

You can pipe issue keys to the -Id parameter.

### System.String[]

You can pipe an array of issue keys to the -Id parameter.

## OUTPUTS

### System.Object

No output is produced. This cmdlet performs a cache clearing operation.

## NOTES

- Use the -All parameter to clear the entire issue cache at once.
- Use the -Id parameter to clear specific issues from the cache.
- This cmdlet is useful when cached issue data becomes stale and needs to be refreshed.

## RELATED LINKS

See Get-LiraConfiguration




