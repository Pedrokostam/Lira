$projectDir = Split-Path $PSScriptRoot
$outDir = Join-Path $projectDir 'bin\DebugModule'

Write-Host "Debugging session: PS $($PSVersionTable.PSVersion)"
# publish, not build: library builds don't copy NuGet dependencies (Microsoft.Extensions.Logging etc.)
dotnet publish $projectDir -c Debug -f net8.0 -o $outDir
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }

$DebugPreference = 'Continue'
$VerbosePreference = 'Continue'
Import-Module (Join-Path $outDir 'LiraPS.psd1') -Verbose
