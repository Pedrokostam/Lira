#Requires -Version 7
[CmdletBinding()]
param (
  [Parameter()]
  [string]
  $ModuleName = (Split-Path (Split-Path $PSScriptRoot) -Leaf),
  [Parameter()]
  [string]
  $ProjectName = (Split-Path (Split-Path $PSScriptRoot) -Leaf)
)
$ErrorActionPreference = 'Stop'

Write-Debug "Module name: $ModuleName"
Write-Debug "Project name: $ProjectName"

$info = . $PSScriptRoot/Publish.ps1 -Module $ModuleName -Project $ProjectName
$installScript = Join-Path (Split-Path $info.Path) 'Install-Lira.ps1'
. $installScript
