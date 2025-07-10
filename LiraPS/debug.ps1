#"Write-Host $pwd;Import-Module ./LiraPS.psd1 -verbose"
[CmdletBinding()]
param (
	[Parameter()]
	[string]
	$Framework
)

$version = $PSVersionTable.PSVersion
Write-Host "Debugging session: PS $($version.ToString())"

if ($Framework -eq '') {
	if ($version.Major -eq 5) {
		$framework = 'netstandard2.0'
	} else {
		$framework = 'net8.0'
	}
}
$releaseAParams = @(
	'publish',
	'-c',
	'Debug',
	'-f'
	$framework
)
Write-Host "Using build params: $($releaseAParams -join ' ')"
$output = @()
dotnet @releaseAParams | Tee-Object -Variable output

$output = $output | Select-Object -Last 1
$path = ($output -split '->')[1].Trim()

$modulepath = Join-Path $path 'LiraPS.psd1'
$DebugPreference = 'Continue'
$VerbosePreference = 'Continue'
Import-Module $modulepath -Verbose