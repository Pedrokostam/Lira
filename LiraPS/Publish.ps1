#Requires -Version 7
[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $ModuleName = (Split-Path $PSScriptRoot -Leaf),
    [Parameter()]
    [string]
    $ProjectName = (Split-Path $PSScriptRoot -Leaf)
)
Push-Location $PSScriptRoot
$version = ([xml](Get-Content "$ProjectName.csproj")).Project.PropertyGroup.Version ?? '0.0.1'
$versionedFolder = "$PSScriptRoot/published/$version"
New-Item -ItemType Directory -Path $versionedFolder -Force
Write-Host "Building $ProjectName with version $version"
dotnet publish -o $versionedFolder -f net8.0 -c Release --self-contained
if ($LASTEXITCODE) {
    Write-Error -ea stop 'COULD NOT BUILD'
}
$modulePath = "$versionedFolder/$ModuleName.psd1"
$newLines = Get-Content $modulePath | ForEach-Object { $_ -replace '^\s*ModuleVersion\s?=.*', "ModuleVersion = '$Version'" } 
$newLines | Set-Content $modulePath
[PSCustomObject]@{
    Path    = $versionedFolder
    Version = $version
}
