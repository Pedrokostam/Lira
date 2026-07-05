---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Set-LiraConfiguration
---

# Set-LiraConfiguration

## SYNOPSIS

See related parameters

## SYNTAX

### MANUAL (Default)

```
Set-LiraConfiguration [-Type <ConfigurationType>] [<CommonParameters>]
```

### PAT

```
Set-LiraConfiguration -PersonalAccessToken <string> [<CommonParameters>]
```

### ATLASSIAN

```
Set-LiraConfiguration -AtlassianApiKey <string> -UserEmail <string> [<CommonParameters>]
```

## DESCRIPTION

Creates and saves a new LiraPS configuration for connecting to Jira.
Supports multiple authentication methods: Personal Access Token, Username/Password, or Atlassian API Key.
Interactively prompts for missing values.

## EXAMPLES

### Example 1

Set-LiraConfiguration

Interactively prompts for authentication method and Jira server details.

### Example 2

Set-LiraConfiguration -PAT (ConvertTo-SecureString 'mytoken' -AsPlainText) -ServerAddress 'https://jira.company.com'

Creates a configuration using a personal access token.

## PARAMETERS

### -AtlassianApiKey

See related parameters

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ATLASSIAN
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -PersonalAccessToken

See related parameters

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PAT
ParameterSets:
- Name: PAT
  Position: Named
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Type

See related parameters

```yaml
Type: LiraPS.Cmdlets.ConfigurationType
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: MANUAL
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -UserEmail

See related parameters

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: ATLASSIAN
  Position: Named
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

### Lira.Objects.UserDetails

The authenticated user details confirming successful configuration.

## NOTES

- The configuration is automatically activated unless -NoSwitch is specified.
- Multiple configurations can be created and switched between.
- Sensitive data (tokens, passwords) are securely stored.

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




