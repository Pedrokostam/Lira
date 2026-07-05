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
Push-Location (Split-Path $PSScriptRoot)
$csprojXmlProperties = ([xml](Get-Content "$ProjectName.csproj")).Project.PropertyGroup
$version = $csprojXmlProperties.Version ?? '0.0.1'
$versionedFolder = "$PSScriptRoot/../Published/$version"
New-Item -ItemType Directory -Path $versionedFolder -Force
Write-Host "Building $ProjectName with version $version"
dotnet publish -o $versionedFolder -f net8.0 -c Release --self-contained

$docDir = Measure-PlatyPSMarkdown -Path .\Docs\*.md | ? FileType -Match "CommandHelp"  |
Import-MarkdownCommandHelp -Path {$_.FilePath} | Export-MamlCommandHelp -output $versionedFolder -force
Rename-Item $docDir.Directory -NewName "en-US"

write-host  "$versionedFolder/$ProjectName - $versionedFolder/en-US"

if ($LASTEXITCODE) {
    Write-Error -ea stop 'COULD NOT BUILD'
}
$modulePath = "$versionedFolder/$ModuleName.psd1"
$newLines = Get-Content $modulePath | ForEach-Object { $_ -replace '^\s*ModuleVersion\s?=.*', "ModuleVersion = '$Version'" } 
$newLines | Set-Content $modulePath
$nugetPresent = Get-Command nuget -ea SilentlyContinue
if ($nugetPresent) {
    Push-Location 'published'
    $nuspecPath = 'LastNuspec.nuspec'
    '' > $nuspecPath
    $nuspecPath = Get-Item $nuspecPath | ForEach-Object fullname
    $xmlsettings = New-Object System.Xml.XmlWriterSettings
    $xmlsettings.Indent = $true
    $xmlWriter = [System.Xml.XmlWriter]::Create($nuspecPath, $xmlsettings)
    $xmlWriter.WriteStartElement('package')

    $xmlWriter.WriteStartElement('metadata')
    $xmlWriter.WriteElementString('id', $ModuleName)
    $xmlWriter.WriteElementString('version', $version)

    $copyPasteElement = @('title', 'authors', 'owners', 'licenseUrl', 'projectUrl', 'iconUrl', 'requireLicenseAcceptance', 'description', 'summary', 'releaseNotes', 'copyright', 'language', 'tags')
    foreach ($elem in $copyPasteElement) {
        $val = $csprojXmlProperties."$Elem"
        if ($val) {
            $xmlWriter.WriteElementString($elem, $val)
        }
    }
    $repoUrl = $csprojXmlProperties.RepositoryUrl
    $repoType = $csprojXmlProperties.RepositoryType
    if ($repoUrl -and $repoType) {
        $xmlWriter.WriteStartElement('repository')
        $xmlWriter.WriteAttributeString('type', $repoType)
        $xmlWriter.WriteAttributeString('url', $repoUrl)
        $xmlWriter.WriteEndElement()
    }
    $xmlWriter.WriteEndElement()

    $xmlWriter.WriteStartElement('files')

    $xmlWriter.WriteStartElement('file')
    $src = (Join-Path $versionedFolder '**') -replace '\\', '/'
    $xmlWriter.WriteAttributeString('src', $src)
    $xmlWriter.WriteAttributeString('target', "content/$ModuleName")
    $xmlWriter.WriteEndElement()

    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()

    $xmlWriter.Flush()
    $xmlWriter.Close()

    nuget pack $nuspecPath
    Get-Content $nuspecPath
    Pop-Location 
}

Pop-Location
[PSCustomObject]@{
    Path    = $versionedFolder
    Version = $version
    Nuget   = if ($nugetPresent) { "$ModuleName.$version.nupkg" } else { $null }
}
