---
document type: cmdlet
external help file: LiraPS.dll-Help.xml
HelpUri: ''
Locale: en-GB
Module Name: LiraPS
ms.date: 07/05/2026
PlatyPS schema version: 2024-05-01
title: Get-LiraUser
---

# Get-LiraUser

## SYNOPSIS

See related parameters

## SYNTAX

### __AllParameterSets

```
Get-LiraUser [[-Name] <string[]>] [<CommonParameters>]
```

## DESCRIPTION

Searches for and retrieves user details from Jira by name, email, or account ID.
Use "me" to refer to the currently authenticated user.

## EXAMPLES

### Example 1

Get-LiraUser -Name "john.doe"

Retrrieves user details for john.doe.

### Example 2

Get-LiraUser -Name "me"

Retrieves details of the currently authenticated user.

## PARAMETERS

### -Name

See related parameters

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- ID
- DisplayName
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
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

### Lira.Objects.UserDetails

User details including account ID, display name, email, and other profile information.

## NOTES

- Use "me" as a special keyword to refer to the currently authenticated user.

## NOTES

Use this cmdlet to manage Jira items.

## RELATED LINKS

See Get-LiraConfiguration




