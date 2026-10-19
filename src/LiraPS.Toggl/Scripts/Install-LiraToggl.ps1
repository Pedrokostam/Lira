#Requires -Version 7
[CmdletBinding()]
param (
  [Parameter()]
  [string]
  $ModuleName = 'LiraPS.Toggl'
)
$ErrorActionPreference = 'Stop'

if (-not (Get-Module -ListAvailable LiraPS))
{
  Write-Warning 'LiraPS is not installed. LiraPS.Toggl will not import until it is.'
}

$versionedFolders = Get-ChildItem $PSScriptRoot -Directory
$newestVersion = $versionedFolders | Sort-Object { [version]$_.BaseName } -Descending | Select-Object -First 1

$paths = $env:PSModulePath -split [System.IO.Path]::PathSeparator
foreach ($path in $paths)
{
  try
  {
    $installedModulePath = Join-Path $path $ModuleName
    New-Item -ItemType Directory -Path $installedModulePath -Force
    Copy-Item -Path $newestVersion -Destination $installedModulePath -Recurse -Verbose -Force
    Write-Host "`nInstalled module in $path" -ForegroundColor Cyan
    return
  }
  catch
  {
    Write-Warning "Failed to install in $path"
  }
}
Write-Error 'Could not install module'
