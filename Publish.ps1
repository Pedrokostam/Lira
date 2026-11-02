#Requires -Version 7
[CmdletBinding()]
param (
  [Parameter()]
  [Switch]
  $Nuget
)
$ErrorActionPreference = 'Stop'

$suiteFolder = "$PSScriptRoot/published"
if (Test-Path $suiteFolder)
{
  Remove-Item -Recurse -Force $suiteFolder
}
New-Item -ItemType Directory -Path $suiteFolder | Out-Null

# LiraPS first: LiraPS.Toggl requires it
$moduleNames = 'LiraPS', 'LiraPS.Toggl'
foreach ($moduleName in $moduleNames)
{
  $projectFolder = "$PSScriptRoot/src/$moduleName"
  & "$projectFolder/Scripts/Publish.ps1" -Nuget:$Nuget

  $projectPublished = "$projectFolder/published"
  $newestVersion = Get-ChildItem $projectPublished -Directory |
    Sort-Object { [version]$_.BaseName } -Descending | Select-Object -First 1
  $moduleFolder = New-Item -ItemType Directory -Path "$suiteFolder/$moduleName"
  Copy-Item $newestVersion -Destination $moduleFolder -Recurse
  Copy-Item "$projectPublished/*.ps1" -Destination $moduleFolder
}

@"
#Requires -Version 7
`$ErrorActionPreference = 'Stop'

foreach (`$moduleName in $(($moduleNames | ForEach-Object { "'$_'" }) -join ', '))
{
  & (Get-Item "`$PSScriptRoot/`$moduleName/Install-*.ps1")
}
"@ | Set-Content "$suiteFolder/Install-Suite.ps1"

Get-ChildItem $suiteFolder | Compress-Archive -DestinationPath "$suiteFolder/LiraSuite.zip"
