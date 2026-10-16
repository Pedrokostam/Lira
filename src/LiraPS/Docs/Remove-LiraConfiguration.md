---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Remove-LiraConfiguration
---

# Remove-LiraConfiguration

## SYNOPSIS

See related parameters

## SYNTAX

### MANUAL (Default)

```
Remove-LiraConfiguration [-Name] <string> [<CommonParameters>]
```

### ALL

```
Remove-LiraConfiguration -All [<CommonParameters>]
```

## DESCRIPTION

Removes a saved LiraPS configuration or all configurations.
Use the -All switch to remove all configurations at once.

## EXAMPLES

### Example 1

Remove-LiraConfiguration -Name "OldConfig"

Removes the configuration named OldConfig.

### Example 2

Remove-LiraConfiguration -All

Removes all configurations.

## PARAMETERS

### -All

See related parameters

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

### -Name

See related parameters

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: MANUAL
  Position: 0
  IsRequired: true
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

## OUTPUTS

### LiraPS.Configuration+Information

N/A

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




