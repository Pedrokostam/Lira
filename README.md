# Lira
Lira is a library that takes care of interfacing with Jira Server/Data Center REST API. Lira's main goal is to simplify checking worklogs for the given period, tracking how much was spent on an issue and adding new worklogs.

### Caching
The library caches issue reponses from the server. Requesting data about an issue (or worklogs of that issue) will first look in the cache.

The cache records are invalidated after 15 minutes from fetching. They can also be invalidated manually by the user.

### Logging
The library makes use of [`Microsoft.Extensions.Logging`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) and it comes with a default implementation of a logger.

## LiraPS - PowerShell module
The project comes with an actual implementation of the Lira library - as a module for PowerShell.

### Authorization
LiraPS allows authorization with both username/password and with [Personal Access Tokens](https://developer.atlassian.com/server/jira/platform/personal-access-token/). Out of those two, tokens are recommended - they are both faster and more secure.

The authorization data persists between sessions - you need to only set it once (and when it changes).

#### Security
The data is stored on the disk in an encrypted format. The encryption is tied to the Windows account - other users will not be able to use your configuration.

However, due to the nature of PowerShell and .NET, once loaded the authorization data is available in the PowerShell session.

#### Multiple users
LiraPS allows creation of multiple configurations. They are independent from each other. Only one configuration can be active at the given time.

More information about configurations can be found in the appropriate [Cmdlets sections](#set-configuration)

### Cmdlets

#### Get-Issue

#### Get-Worklog

#### Set-Configuration

### Installation

## Pronunciation
*Lira* should rhyme with *Jira*. So it should be **LEE-ruh** ( /ˈliɹə/).

Unless of course you say **JY-ruh**, then feel free to call it **LY-ruh** (/ˈlaɪɹə/).

But who says **JY-ruh**?

<sup><sub>Did you know that the name of Jira comes from Godzilla (which in Japanese is pronounced Gojira)? The name was chosen as an allusion to another bug-tracking tool called Bugzilla.</sub></sup>
