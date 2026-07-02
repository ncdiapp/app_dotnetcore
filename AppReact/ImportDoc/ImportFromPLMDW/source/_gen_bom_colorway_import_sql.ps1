# Generates output/{templateId}/PlmDw_ImportBomColorwayGrandchild.sql from bomColorwayImportConfig.json
param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'bomColorwayImportConfig.json')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$templatePath = Join-Path $PSScriptRoot 'PlmDw_ImportBomColorwayGrandchild.sql'

if (-not (Test-Path $ConfigPath)) {
    throw "Config not found: $ConfigPath"
}
if (-not (Test-Path $templatePath)) {
    throw "Template not found: $templatePath"
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$templateId = [string]$config.plmTemplateId
$outDir = Join-Path $root "output\$templateId"
$outFile = Join-Path $outDir 'PlmDw_ImportBomColorwayGrandchild.sql'

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sql = Get-Content $templatePath -Raw

$gc = $config.grandchildColumns
$replacements = @{
    "DECLARE @PlmDatabase           NVARCHAR(128) = N'PLM';" = "DECLARE @PlmDatabase           NVARCHAR(128) = N'$($config.plmDatabase)';"
    "DECLARE @DwDatabase            NVARCHAR(128) = N'plmDW';" = "DECLARE @DwDatabase            NVARCHAR(128) = N'$($config.dwDatabase)';"
    "DECLARE @PlmTemplateId         INT           = NULL;" = "DECLARE @PlmTemplateId         INT           = $($config.plmTemplateId);"
    "DECLARE @PlmTabId              INT           = NULL;" = "DECLARE @PlmTabId              INT           = $($config.plmTabId);"
    "DECLARE @PlmGridId             INT           = NULL;" = "DECLARE @PlmGridId             INT           = $($config.plmGridId);"
    "DECLARE @ProductGridBlockId    INT           = NULL;" = "DECLARE @ProductGridBlockId    INT           = $($config.productGridBlockId);"
    "DECLARE @DwGridTable           NVARCHAR(256) = NULL;" = "DECLARE @DwGridTable           NVARCHAR(256) = N'$($config.dwGridTable)';"
    "DECLARE @HostAppTable          NVARCHAR(128) = NULL;" = "DECLARE @HostAppTable          NVARCHAR(128) = N'$($config.hostAppTable)';"
    "DECLARE @GrandchildAppTable    NVARCHAR(128) = NULL;" = "DECLARE @GrandchildAppTable    NVARCHAR(128) = N'$($config.grandchildAppTable)';"
}

foreach ($k in $replacements.Keys) {
    $sql = $sql.Replace($k, $replacements[$k])
}

if ($gc) {
    if ($gc.colorwayKey) { $sql = $sql -replace "DECLARE @GcColorwayColumn\s+NVARCHAR\(128\)\s+=\s+N'Colorway';", "DECLARE @GcColorwayColumn      NVARCHAR(128) = N'$($gc.colorwayKey)';" }
    if ($gc.artworkColor) { $sql = $sql -replace "DECLARE @GcArtworkColorColumn\s+NVARCHAR\(128\)\s+=\s+N'ArtworkColor';", "DECLARE @GcArtworkColorColumn  NVARCHAR(128) = N'$($gc.artworkColor)';" }
    if ($gc.artworkPhoto) { $sql = $sql -replace "DECLARE @GcArtworkPhotoColumn\s+NVARCHAR\(128\)\s+=\s+N'ArtworkPhoto';", "DECLARE @GcArtworkPhotoColumn  NVARCHAR(128) = N'$($gc.artworkPhoto)';" }
    if ($gc.parentLink) { $sql = $sql -replace "DECLARE @GcParentLinkColumn\s+NVARCHAR\(128\)\s+=\s+N'ParentRowId';", "DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'$($gc.parentLink)';" }
}

if ($config.importModeDefault) {
    $sql = $sql -replace "DECLARE @ImportMode\s+NVARCHAR\(16\)\s+=\s+N'APPEND';", "DECLARE @ImportMode            NVARCHAR(16)  = N'$($config.importModeDefault)';"
}

$header = @"
-- Template $templateId — BOM Grid $($config.plmGridId) / Block $($config.productGridBlockId) → $($config.tablePrefix)$($config.grandchildAppTable)
-- Generated from source/PlmDw_ImportBomColorwayGrandchild.sql + bomColorwayImportConfig.json
"@

$sql = $sql -replace '(?m)^-- Template: source/PlmDw_ImportBomColorwayGrandchild\.sql\r?\n-- Deliverable: output/\{templateId\}/PlmDw_ImportBomColorwayGrandchild\.sql', $header.TrimEnd()

[System.IO.File]::WriteAllText($outFile, $sql, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Wrote $outFile"
