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
$info = . $PSScriptRoot/Publish.ps1 -Module $ModuleName -Project $ProjectName
$paths = $env:PSModulePath -split ';'
foreach ($path in $paths) {
    try {
        $installedModulePath = Join-Path $path $ModuleName
        New-Item -ItemType Directory -Path $installedModulePath -Force -ea Stop
        Copy-Item -Path $info.Path -Destination $installedModulePath -Recurse -Verbose -force -ea stop
        Write-Host "Installed moduled in $path"
        return
    } catch {
        <#Do this if a terminating exception happens#>
    }
}
Write-Error -ea Stop 'Could not install module'
