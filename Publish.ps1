#Requires -Version 7
[CmdletBinding()]
param (
  [Parameter()]
  [Switch]
  $Nuget
)
$ErrorActionPreference = 'Stop'

# LiraPS first: LiraPS.Toggl requires it
& "$PSScriptRoot/src/LiraPS/Scripts/Publish.ps1" -Nuget:$Nuget
& "$PSScriptRoot/src/LiraPS.Toggl/Scripts/Publish.ps1" -Nuget:$Nuget
