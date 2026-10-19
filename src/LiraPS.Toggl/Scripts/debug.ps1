$projectDir = Split-Path $PSScriptRoot
$outDir = Join-Path $projectDir 'bin\DebugModule'
$liraPSDir = Join-Path (Split-Path $projectDir) 'LiraPS'
$liraPSOutDir = Join-Path $liraPSDir 'bin\DebugModule'

Write-Host "Debugging session: PS $($PSVersionTable.PSVersion)"
# publish, not build: library builds don't copy NuGet dependencies (Microsoft.Extensions.Logging etc.)
dotnet publish $liraPSDir -c Debug -f net8.0 -o $liraPSOutDir
if ($LASTEXITCODE -ne 0) { throw 'LiraPS publish failed' }
dotnet publish $projectDir -c Debug -f net8.0 -o $outDir
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }

$DebugPreference = 'Continue'
$VerbosePreference = 'Continue'
# RequiredModules is satisfied by the already imported LiraPS, so it does not have to be on PSModulePath
Import-Module (Join-Path $liraPSOutDir 'LiraPS.psd1') -Verbose
Import-Module (Join-Path $outDir 'LiraPS.Toggl.psd1') -Verbose
