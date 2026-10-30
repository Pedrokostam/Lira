#Requires -Version 7
[CmdletBinding()]
param ()
$ErrorActionPreference = 'Stop'

# LiraPS first: LiraPS.Toggl requires it
& "$PSScriptRoot/src/LiraPS/Scripts/Install-BuiltModule.ps1"
& "$PSScriptRoot/src/LiraPS.Toggl/Scripts/Install-BuiltModule.ps1"
