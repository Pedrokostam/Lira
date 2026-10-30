@{
RootModule = 'LiraPS.Toggl.dll'
ModuleVersion = '1.0.0'
GUID = '024c05bd-3f80-440d-b2ee-36cd98a7ad0c'
Author = 'Maciej Krosta'
CompanyName = 'Unknown'
Copyright = '(c) 2026 Maciej Krosta. All rights reserved.'
Description = 'Toggl Track addon for LiraPS.'
RequiredModules = @(@{ ModuleName = 'LiraPS'; ModuleVersion = '1.1.0' })
FunctionsToExport = @()
CmdletsToExport = @(
    "Clear-LiraTogglCache"
    "Compare-LiraTogglWorklog"
    "Get-LiraTogglEntry"
    "Set-LiraTogglConfiguration"
    "Sync-LiraJiraToggl"
)
TypesToProcess = @(
    "TypeData/TypeData.ps1xml"
)
FormatsToProcess = @(
    "Formatters/TogglJiraComparison.ps1xml"
    "Formatters/TogglTimeEntry.ps1xml"
)
VariablesToExport = @()
AliasesToExport = @(
    "Get-TogglEntries",
    "Get-TogglEntry",
    "Get-LiraTogglEntries"
)
PrivateData = @{
    PSData = @{
    }
}
}
