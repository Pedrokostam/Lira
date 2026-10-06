#Requires -Version 7
[CmdletBinding()]
param (
  [Parameter()]
  [string]
  $ModuleName = (Get-Item $PSScriptRoot).BaseName
)
$ErrorActionPreference = 'Stop'

$versionedFolders = Get-ChildItem $PSScriptRoot -Directory
$newestVersion = $versionedFolders | Sort-Object BaseName | Select-Object -First 1

$paths = $env:PSModulePath -split ';'
foreach ($path in $paths)
{
  try
  {
    $installedModulePath = Join-Path $path $ModuleName
    New-Item -ItemType Directory -Path $installedModulePath -Force
    Copy-Item -Path $newestVersion -Destination $installedModulePath -Recurse -Verbose -Force 
    Write-Host "`nInstalled moduled in $path" -ForegroundColor Cyan
    return
  }
  catch
  {
    Write-Warning "Failed to install in $path"
    <#Do this if a terminating exception happens#>
  }
}
Write-Error 'Could not install module'
