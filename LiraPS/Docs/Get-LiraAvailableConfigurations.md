---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Get-LiraAvailableConfigurations
---

# Get-LiraAvailableConfigurations

## SYNOPSIS

Lists all available LiraPS configurations.

## SYNTAX

### __AllParameterSets

```
Get-LiraAvailableConfigurations [<CommonParameters>]
```

## DESCRIPTION

Lists all available LiraPS configurations that have been saved locally.
Use Switch-LiraConfiguration to activate any of these configurations.

## EXAMPLES

### Example 1

Get-LiraAvailableConfigurations

Lists all available configurations with their details.

## PARAMETERS

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

None

## OUTPUTS

### LiraPS.Configuration+Information

Configuration information objects containing name, server address, and authentication type.

## NOTES

- This cmdlet displays all available configurations but does not activate any of them.
- Use Get-LiraConfiguration to see the currently active configuration.
- Use Switch-LiraConfiguration to activate a different configuration.

## RELATED LINKS

- Get-LiraConfiguration
- Switch-LiraConfiguration
See Get-LiraConfiguration




