---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Switch-LiraConfiguration
---

# Switch-LiraConfiguration

## SYNOPSIS

See related parameters

## SYNTAX

### __AllParameterSets

```
Switch-LiraConfiguration [-Name] <string> [<CommonParameters>]
```

## DESCRIPTION

Activates a saved LiraPS configuration for use by subsequent cmdlets.

## EXAMPLES

### Example 1

Switch-LiraConfiguration -Name "ProductionConfig"

Activates the configuration named ProductionConfig.

## PARAMETERS

### -Name

See related parameters

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
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




