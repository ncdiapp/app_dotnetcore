$ErrorActionPreference = 'Stop'
$rootDir = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $rootDir 'output'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$configPath = Join-Path $PSScriptRoot 'dwTabImportConfig.json'
if (-not (Test-Path $configPath)) {
    throw "Missing $configPath - copy dwTabImportConfig.example.json and fill from Phase B (see PROMPT.md)."
}
$config = Get-Content $configPath -Raw | ConvertFrom-Json

$SqlServer = $config.sqlServer
$DwDatabase = $config.dwDatabase
$refScope = if ($config.referenceScope) { $config.referenceScope } else { $config.referenceCode }

$TabSystemColumns = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('TabID', 'ProductReferenceID'),
    [StringComparer]::OrdinalIgnoreCase
)
$GridSystemColumns = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('ProductReferenceID', 'BlockID', 'GridID', 'RowID', 'RowValueGUID', 'Sort'),
    [StringComparer]::OrdinalIgnoreCase
)

function Invoke-DwQuery([string]$Query) {
    $tmp = [System.IO.Path]::GetTempFileName() + '.sql'
    $out = [System.IO.Path]::GetTempFileName() + '.txt'
    try {
        Set-Content -Path $tmp -Value $Query -Encoding UTF8
        $sqlUser = if ($config.sqlUser) { $config.sqlUser } else { $env:PLM_DW_SQL_USER }
        $sqlPassword = if ($config.sqlPassword) { $config.sqlPassword } else { $env:PLM_DW_SQL_PASSWORD }
        $args = @('-S', $SqlServer, '-d', $DwDatabase, '-i', $tmp, '-o', $out, '-W', '-s', '|', '-h', '-1')
        if ($sqlUser -and $sqlPassword) {
            $args = @('-S', $SqlServer, '-d', $DwDatabase, '-U', $sqlUser, '-P', $sqlPassword) + $args[4..($args.Length - 1)]
        }
        else {
            $args = @('-S', $SqlServer, '-d', $DwDatabase, '-E') + $args[4..($args.Length - 1)]
        }
        $p = Start-Process -FilePath 'sqlcmd' -ArgumentList $args -Wait -PassThru -NoNewWindow
        if ($p.ExitCode -ne 0) { throw "sqlcmd failed ($($p.ExitCode)): $Query" }
        $lines = Get-Content $out | Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' }
        return ,$lines
    }
    finally {
        Remove-Item $tmp, $out -ErrorAction SilentlyContinue
    }
}

function Infer-PlmControlType($meta, $dataType) {
    if ($meta.FkTarget) { return 1 }
    if ($dataType -and $dataType -match 'date') { return 7 }
    return 2
}

function Infer-PlmEntityId($fkTarget) {
    if ($null -eq $fkTarget) { return $null }
    if ($fkTarget -match '^\d+$') { return [int]$fkTarget }
    return $null
}

function Format-TabDisplayName([string]$appTable) {
    if ([string]::IsNullOrWhiteSpace($appTable)) { return $appTable }
    return ($appTable -replace '_', ' ').Trim()
}

function Build-BlueprintFromConfig($config, $allFieldRows) {
    $prefix = $config.tablePrefixDefault
    if (-not $prefix.EndsWith('_')) { $prefix += '_' }
    $rootSuffix = $config.rootTableSuffix
    $refScope = if ($config.referenceScope) { $config.referenceScope } else { $config.referenceCode }
    $templateName = if ($config.blueprint.transactionGroupName) { $config.blueprint.transactionGroupName }
        elseif ($config.blueprint.templateName) { $config.blueprint.templateName }
        elseif ($config.plmTemplate.templateName) { $config.plmTemplate.templateName }
        elseif ($config.plmTemplateName) { $config.plmTemplateName }
        else { 'PLM Import' }
    $tgIntegration = if ($config.blueprint.transactionGroupIntegrationId) { $config.blueprint.transactionGroupIntegrationId }
        else { 'TG_' + ($templateName -replace '[^a-zA-Z0-9]', '') }

    $tabSharedGroups = @()
    $infoTab = $config.tabs | Where-Object { $_.mode -eq 'excludeSubItemsFromDwTable' } | Select-Object -First 1
    $headerTab = $null
    if ($infoTab -and $infoTab.excludeSubItemsFromDwTable) {
        $headerTab = $config.tabs | Where-Object { $_.dwTable -eq $infoTab.excludeSubItemsFromDwTable } | Select-Object -First 1
        if ($headerTab) {
            $tabSharedGroups += [ordered]@{
                groupId             = 'shared_' + ($headerTab.appTable -replace '[^a-zA-Z0-9]', '_').ToLowerInvariant()
                sharedAppTableName  = $prefix + $headerTab.appTable
                primaryPlmTabId     = [int]$headerTab.tabId
                secondaryPlmTabIds  = @([int]$infoTab.tabId)
                rule                = 'SharedSubItemsOnPrimaryOnly'
            }
        }
    }

    $transactions = @()
    $sortedTabs = @($config.tabs | Sort-Object { if ($null -ne $_.tabSort) { $_.tabSort } else { 9999 } }, { $_.tabId })
    foreach ($tab in $sortedTabs) {
        $tabName = if ($tab.plmTabName) { $tab.plmTabName } else { Format-TabDisplayName $tab.appTable }
        $importStatus = if ($tab.importStatus) { $tab.importStatus } else { 'Ready' }
        $siblingUnits = [System.Collections.Generic.List[object]]::new()

        if ($tab.mode -eq 'excludeSubItemsFromDwTable' -and $tab.excludeSubItemsFromDwTable) {
            $headerTab = $config.tabs | Where-Object { $_.dwTable -eq $tab.excludeSubItemsFromDwTable } | Select-Object -First 1
            if ($headerTab) {
                $siblingUnits.Add([ordered]@{
                    appTableName    = $prefix + $headerTab.appTable
                    isMasterSibling = $true
                    fieldPolicy     = 'AllMappedColumns'
                })
            }
        }

        $fieldPolicy = if ($tab.mode -eq 'excludeSubItemsFromDwTable') { 'ExclusiveSubItemsOnly' } else { 'AllMappedColumns' }
        $ownSibling = [ordered]@{
            appTableName    = $prefix + $tab.appTable
            isMasterSibling = ($siblingUnits.Count -eq 0)
            fieldPolicy     = $fieldPolicy
        }
        if ($tab.excludeSubItemsFromDwTable) {
            $ownSibling.excludeSubItemsFromDwTable = $tab.excludeSubItemsFromDwTable
        }
        $siblingUnits.Add($ownSibling)

        $transactions += [ordered]@{
            plmTabId         = [int]$tab.tabId
            plmTabName       = $tabName
            integrationId    = "Tab_$($tab.tabId)"
            transactionName  = $tabName
            importStatus     = $importStatus
            plmTabSort       = if ($null -ne $tab.tabSort) { [int]$tab.tabSort } else { $null }
            isTemplateHeaderTab = if ($null -ne $tab.isTemplateHeaderTab) { [bool]$tab.isTemplateHeaderTab } else { $null }
            unitStructure    = [ordered]@{
                mode          = 'RootPlusMasterSibling'
                rootTableName = $prefix + $rootSuffix
                siblingUnits  = @($siblingUnits.ToArray())
                childUnits    = @()
            }
        }
    }

    $gridBindings = @()
    foreach ($grid in ($config.grids | ForEach-Object { $_ })) {
        $parentTabId = if ($grid.parentPlmTabId) { [int]$grid.parentPlmTabId } else { $null }
        $gridBindings += [ordered]@{
            plmGridId                  = [int]$grid.gridId
            appTableName               = $prefix + $grid.appTable
            parentPlmTabId             = $parentTabId
            attachToRoot               = (-not $parentTabId)
            integrationId              = "Grid_$($grid.gridId)"
            transactionIntegrationId   = if ($grid.transactionIntegrationId) { $grid.transactionIntegrationId } else { "Grid_$($grid.gridId)" }
        }
    }

    $blueprintFields = @()
    $orderByTable = @{}
    foreach ($r in $allFieldRows) {
        $appTableFull = if ($r.AppTable -eq $rootSuffix) { $prefix + $rootSuffix } else { $prefix + $r.AppTable }
        if (-not $orderByTable.ContainsKey($appTableFull)) { $orderByTable[$appTableFull] = 0 }
        $orderByTable[$appTableFull]++
        $plmCtrl = if ($null -ne $r.PlmControlType) { [int]$r.PlmControlType } else { Infer-PlmControlType ([pscustomobject]@{ FkTarget = $r.FkTarget }) $r.DwDataType }
        $entityId = if ($null -ne $r.PlmEntityId) { $r.PlmEntityId } else { Infer-PlmEntityId $r.FkTarget }
        $tabIds = @()
        if ($r.PlmTabId) { $tabIds = @([int]$r.PlmTabId) }
        $blueprintFields += [ordered]@{
            appTableName   = $appTableFull
            appColumnName  = $r.AppColumn
            plmTabIds      = $tabIds
            plmControlType = $plmCtrl
            plmEntityId    = $entityId
            displayLabel   = $r.AppColumn
            displayOrder   = $orderByTable[$appTableFull]
            includeInSearch = $false
        }
        $r | Add-Member -NotePropertyName PlmControlType -NotePropertyValue $plmCtrl -Force
        $r | Add-Member -NotePropertyName PlmEntityId -NotePropertyValue $entityId -Force
        if (-not $r.PSObject.Properties['DwDataType']) {
            $r | Add-Member -NotePropertyName DwDataType -NotePropertyValue $null -Force
        }
    }

    $searchName = if ($config.blueprint.searchName) { $config.blueprint.searchName } else { "$templateName References" }
    $searchIntegration = if ($config.blueprint.searchIntegrationId) { $config.blueprint.searchIntegrationId }
        else { 'Search_' + ($templateName -replace '[^a-zA-Z0-9]', '') }

    $templateHeaderTabIds = @($config.tabs | Where-Object { $_.isTemplateHeaderTab -eq $true } | ForEach-Object { [int]$_.tabId })
    if ($config.plmTemplate -and $config.plmTemplate.templateHeaderTabIds) {
        $templateHeaderTabIds = @($config.plmTemplate.templateHeaderTabIds | ForEach-Object { [int]$_ })
    }
    $plmTemplateId = $null
    if ($null -ne $config.plmTemplateId -and [int]$config.plmTemplateId -gt 0) {
        $plmTemplateId = [int]$config.plmTemplateId
    }
    elseif ($config.plmTemplate -and $null -ne $config.plmTemplate.templateId -and [int]$config.plmTemplate.templateId -gt 0) {
        $plmTemplateId = [int]$config.plmTemplate.templateId
    }

    return [ordered]@{
        schemaVersion         = 1
        generatedAt           = (Get-Date).ToUniversalTime().ToString('o')
        source                = [ordered]@{
            plmTemplateId = $plmTemplateId
            plmDatabase   = $config.plmDatabase
            dwDatabase    = $config.dwDatabase
            importTabIds  = @($config.importTabIds | ForEach-Object { [int]$_ })
            tablePrefix   = $prefix
            configFile    = 'source/dwTabImportConfig.json'
        }
        plmTemplate           = if ($plmTemplateId) { [ordered]@{
            templateId           = $plmTemplateId
            templateName         = $templateName
            templateHeaderTabIds = $templateHeaderTabIds
        } } else { $null }
        transactionGroup      = [ordered]@{
            name           = $templateName
            integrationId  = $tgIntegration
            saasApplicationId = $null
        }
        rootUnit              = [ordered]@{
            appTableName   = $prefix + $rootSuffix
            integrationId  = 'Unit_ReferenceBasicInfo'
            referenceScope = [ordered]@{
                dwTable      = $refScope.dwTable
                dwColumn     = $refScope.dwColumn
                plmTabId     = [int]$refScope.plmTabId
                plmSubItemId = [int]$refScope.plmSubItemId
            }
        }
        tabSharedTableGroups  = $tabSharedGroups
        transactions          = $transactions
        gridBindings          = $gridBindings
        blueprintFields       = $blueprintFields
        searchView            = [ordered]@{
            search     = [ordered]@{
                name           = $searchName
                integrationId  = $searchIntegration
                usageType      = 'DataModelTemplate'
                rootTableName  = $prefix + $rootSuffix
            }
            searchView = [ordered]@{
                integrationId = $searchIntegration + '_View'
                fields        = 'DefaultReferenceBasicInfo'
            }
        }
        navigation            = [ordered]@{
            folderName               = if ($config.blueprint.folderName) { $config.blueprint.folderName } else { $templateName }
            parentFolderIntegrationId = $null
            menuOrder                = 100
        }
    }
}

function Get-DwTableColumns([string]$TableName) {
    $q = @"
SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = N'$TableName'
ORDER BY c.ORDINAL_POSITION;
"@
    $rows = Invoke-DwQuery $q
    $cols = @()
    foreach ($line in $rows) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 6) { continue }
        $cols += [pscustomobject]@{
            DwColumn     = $parts[0].Trim()
            DataType     = $parts[1].Trim().ToLowerInvariant()
            CharMaxLen   = $parts[2].Trim()
            NumPrecision = $parts[3].Trim()
            NumScale     = $parts[4].Trim()
            Ordinal      = [int]$parts[5].Trim()
        }
    }
    if (-not $cols.Count) { throw "No columns found for DW table: $TableName" }
    return $cols
}

function Get-DwColumnMeta([string]$DwColumn) {
    $fkTarget = $null
    $namePart = $DwColumn
    $fkIdx = $DwColumn.IndexOf('_FK_')
    if ($fkIdx -ge 0) {
        $fkTarget = $DwColumn.Substring($fkIdx + 4)
        $namePart = $DwColumn.Substring(0, $fkIdx)
    }
    $subItemId = $null
    $stem = $namePart
    if ($namePart -match '^(.+)_(\d+)$') {
        $stem = $Matches[1].TrimEnd('_')
        $subItemId = [int]$Matches[2]
    }
    return [pscustomobject]@{
        DwColumn  = $DwColumn
        Stem      = $stem
        SubItemId = $subItemId
        FkTarget  = $fkTarget
        NamePart  = $namePart
    }
}

function Get-AppSqlType($col, [string]$DwColumn) {
    $dt = $col.DataType
    switch ($dt) {
        'int' { return '[int]' }
        'bigint' { return '[bigint]' }
        'smallint' { return '[smallint]' }
        'bit' { return '[bit]' }
        'datetime' { return '[datetime]' }
        'datetime2' { return '[datetime2]' }
        'date' { return '[date]' }
        'float' {
            if ($DwColumn -match '(?i)Composition\d|Comp\d__|Comp\d_|Composition\d__') { return '[decimal](18, 1)' }
            if ($DwColumn -match '(?i)Weight_') { return '[decimal](18, 2)' }
            return '[decimal](18, 2)'
        }
        'decimal' {
            $p = if ($col.NumPrecision -and $col.NumPrecision -ne 'NULL') { [int]$col.NumPrecision } else { 18 }
            $s = if ($col.NumScale -and $col.NumScale -ne 'NULL') { [int]$col.NumScale } else { 2 }
            return "[decimal]($p, $s)"
        }
        'nvarchar' { return '[nvarchar](255)' }
        'varchar' { return '[nvarchar](255)' }
        'nchar' { return '[nchar](255)' }
        'char' { return '[nvarchar](255)' }
        default { return '[nvarchar](255)' }
    }
}

function Get-AppColumnNames($fieldRows) {
    $stemCounts = @{}
    foreach ($r in $fieldRows) {
        if (-not $stemCounts.ContainsKey($r.Stem)) { $stemCounts[$r.Stem] = 0 }
        $stemCounts[$r.Stem]++
    }
    foreach ($r in $fieldRows) {
        if ($stemCounts[$r.Stem] -eq 1) {
            $r | Add-Member -NotePropertyName AppColumn -NotePropertyValue $r.Stem -Force
        }
        else {
            $r | Add-Member -NotePropertyName AppColumn -NotePropertyValue ($r.NamePart) -Force
        }
    }
    return $fieldRows
}

function Get-SubItemIdSet([string]$DwTable) {
    $set = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($c in (Get-DwTableColumns $DwTable)) {
        if ($TabSystemColumns.Contains($c.DwColumn)) { continue }
        $meta = Get-DwColumnMeta $c.DwColumn
        if ($null -ne $meta.SubItemId) { [void]$set.Add($meta.SubItemId) }
    }
    return $set
}

function Build-FieldRows($dwCols, [string]$DwTable, [int]$TabId, [string]$AppTable, [string]$FieldKind, $gridSubItemId, $gridId) {
    $rows = @()
    foreach ($c in $dwCols) {
        if ($FieldKind -eq 'TabField' -and $TabSystemColumns.Contains($c.DwColumn)) { continue }
        if ($FieldKind -eq 'GridColumn' -and $GridSystemColumns.Contains($c.DwColumn)) { continue }
        $meta = Get-DwColumnMeta $c.DwColumn
        $rows += [pscustomobject]@{
            AppTable         = $AppTable
            DwTable          = $DwTable
            DwColumn         = $c.DwColumn
            Stem             = $meta.Stem
            NamePart         = $meta.NamePart
            SubItemId        = $meta.SubItemId
            FkTarget         = $meta.FkTarget
            SqlType          = (Get-AppSqlType $c $c.DwColumn)
            PlmTabId         = if ($FieldKind -eq 'GridColumn') { $null } else { $TabId }
            PlmGridSubItemId = if ($FieldKind -eq 'GridColumn') { $gridSubItemId } else { $null }
            PlmGridId        = if ($FieldKind -eq 'GridColumn') { $gridId } else { $null }
            PlmMetaColumnId  = if ($FieldKind -eq 'GridColumn') { $meta.SubItemId } else { $null }
            FieldKind        = $FieldKind
            DwDataType       = $c.DataType
            PlmControlType   = (Infer-PlmControlType $meta $c.DataType)
            PlmEntityId      = (Infer-PlmEntityId $meta.FkTarget)
        }
    }
    return Get-AppColumnNames $rows
}

function Build-CreateTableBlock([string]$LogicalTable, $fieldRows, [bool]$IsGrid) {
    $colDefs = New-Object System.Collections.Generic.List[string]
    if ($IsGrid) {
        [void]$colDefs.Add('[RowId] INT IDENTITY(1,1) NOT NULL')
        [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
        [void]$colDefs.Add('[Sort] INT NULL')
    }
    else {
        [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
    }
    foreach ($r in $fieldRows) {
        [void]$colDefs.Add("[$($r.AppColumn)] $($r.SqlType) NULL")
    }
    $pk = if ($IsGrid) { 'RowId' } else { 'ReferenceId' }
    [void]$colDefs.Add("CONSTRAINT [PK_$LogicalTable] PRIMARY KEY CLUSTERED ([$pk])")
    $innerCols = ($colDefs -join ', ')
    return @"
-- $LogicalTable
SET @TableName = @TablePrefix + N'$LogicalTable';
SET @RootTable = @TablePrefix + @RootTableSuffix;
SET @FkName = N'FK_' + @TableName + N'_Reference';

IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' ($innerCols );';
    EXEC sp_executesql @sql;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @FkName)
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)
        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@FkName)
        + N' FOREIGN KEY ([ReferenceId]) REFERENCES dbo.' + QUOTENAME(@RootTable) + N' ([ReferenceId]);';
    EXEC sp_executesql @sql;
END
ELSE IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NULL
BEGIN
    PRINT N'Skipped FK ' + @FkName + N': root table dbo.' + @RootTable + N' does not exist.';
END

"@
}

function SqlStrDyn([string]$s) {
    if ($null -eq $s) { return 'NULL' }
    return "N''$($s -replace "'", "''")''"
}

function SqlInt($n) {
    if ($null -eq $n) { return 'NULL' }
    return [string]$n
}

$allFieldRows = New-Object System.Collections.Generic.List[object]
$scopeAppTables = New-Object System.Collections.Generic.List[string]
[void]$scopeAppTables.Add($config.rootTableSuffix)

$ddlParts = New-Object System.Collections.Generic.List[string]

$ddlParts.Add(@"
-- =============================================================================
-- PLM DW → APP physical tables (generated — see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. PlmDw_Tables.sql          (this file)
--   2. PlmDw_FieldMapping.sql
-- USER SETTINGS (single batch - do not split with GO):
--   @TablePrefix     table prefix, include trailing underscore (default Plm_)
--   @RootTableSuffix root table name after prefix (default ReferenceBasicInfo)
-- Source: plmDW Tab/Grid wide tables for user TabId set
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix     NVARCHAR(32)  = N'$($config.tablePrefixDefault)';  -- <<< USER SETTING
DECLARE @RootTableSuffix NVARCHAR(128) = N'$($config.rootTableSuffix)';     -- <<< USER SETTING
DECLARE @TableName       NVARCHAR(128);
DECLARE @RootTable       NVARCHAR(128);
DECLARE @FkName          NVARCHAR(128);
DECLARE @sql             NVARCHAR(MAX);

"@)

$ddlParts.Add(@"
-- ReferenceBasicInfo (root)
SET @RootTable = @TablePrefix + @RootTableSuffix;

IF OBJECT_ID(N'dbo.' + QUOTENAME(@RootTable), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@RootTable) + N' (
        [ReferenceId] INT IDENTITY(1,1) NOT NULL,
        [ReferenceCode] NVARCHAR(255) NULL,
        [MasterReferenceId] INT NULL,
        [FolderId] INT NULL,
        [AppCreatedByID] INT NULL,
        [AppCreatedDate] DATETIME NULL,
        [AppModifiedByID] INT NULL,
        [AppModifiedDate] DATETIME NULL,
        CONSTRAINT [PK_' + @RootTableSuffix + N'] PRIMARY KEY CLUSTERED ([ReferenceId])
    );';
    EXEC sp_executesql @sql;
END

"@)

foreach ($tab in $config.tabs) {
    [void]$scopeAppTables.Add($tab.appTable)
    $dwCols = Get-DwTableColumns $tab.dwTable
    $fieldRows = @()
    if ($tab.mode -eq 'all') {
        $fieldRows = Build-FieldRows $dwCols $tab.dwTable $tab.tabId $tab.appTable 'TabField' $null $null
    }
    elseif ($tab.mode -eq 'excludeSubItemsFromDwTable') {
        $excludeIds = Get-SubItemIdSet $tab.excludeSubItemsFromDwTable
        $filtered = @($dwCols | Where-Object {
            -not $TabSystemColumns.Contains($_.DwColumn) -and (
                $null -eq (Get-DwColumnMeta $_.DwColumn).SubItemId -or
                -not $excludeIds.Contains((Get-DwColumnMeta $_.DwColumn).SubItemId)
            )
        })
        $fieldRows = Build-FieldRows $filtered $tab.dwTable $tab.tabId $tab.appTable 'TabField' $null $null
    }
    else { throw "Unknown tab mode: $($tab.mode)" }

    $ddlParts.Add((Build-CreateTableBlock $tab.appTable $fieldRows $false))
    foreach ($r in $fieldRows) { [void]$allFieldRows.Add($r) }
}

foreach ($grid in $config.grids) {
    [void]$scopeAppTables.Add($grid.appTable)
    $dwCols = Get-DwTableColumns $grid.dwTable
    $fieldRows = Build-FieldRows $dwCols $grid.dwTable $null $grid.appTable 'GridColumn' $grid.gridSubItemId $grid.gridId
    $ddlParts.Add((Build-CreateTableBlock $grid.appTable $fieldRows $true))
    foreach ($r in $fieldRows) { [void]$allFieldRows.Add($r) }
}

[void]$allFieldRows.Add([pscustomobject]@{
    AppTable         = $config.rootTableSuffix
    AppColumn        = 'ReferenceCode'
    DwTable          = $refScope.dwTable
    DwColumn         = $refScope.dwColumn
    Stem             = 'ReferenceCode'
    NamePart         = 'ReferenceCode'
    SubItemId        = $refScope.plmSubItemId
    FkTarget         = $null
    SqlType          = $null
    PlmTabId         = $refScope.plmTabId
    PlmGridSubItemId = $null
    PlmGridId        = $null
    PlmMetaColumnId  = $null
    FieldKind        = 'ReferenceField'
    DwDataType       = 'nvarchar'
    PlmControlType   = 2
    PlmEntityId      = $null
})

$ddlParts.Add('GO')
$ddlParts.Add('')

$tablesPath = Join-Path $outDir 'PlmDw_Tables.sql'
Set-Content -Path $tablesPath -Value ($ddlParts -join "`n") -Encoding UTF8

$valuesLines = New-Object System.Collections.Generic.List[string]
foreach ($r in $allFieldRows) {
    $appTable = $r.AppTable
    $fkSql = if ($r.FkTarget) { SqlStrDyn $r.FkTarget } else { 'NULL' }
    $plmCtrl = SqlInt $r.PlmControlType
    $plmEnt = SqlInt $r.PlmEntityId
    $dwDt = if ($r.DwDataType) { SqlStrDyn $r.DwDataType } else { 'NULL' }
    $line = "(N''@P@$appTable'', N''$($r.AppColumn -replace "'", "''")'', $(SqlStrDyn $r.DwTable), $(SqlStrDyn $r.DwColumn), $(SqlInt $r.PlmTabId), $(SqlInt $r.SubItemId), $(SqlInt $r.PlmGridSubItemId), $(SqlInt $r.PlmGridId), $(SqlInt $r.PlmMetaColumnId), NULL, $fkSql, $(SqlStrDyn $r.FieldKind), $plmCtrl, $plmEnt, $dwDt)"
    [void]$valuesLines.Add($line)
}
$valuesBlock = $valuesLines -join ",`n        "

$deleteInList = ($scopeAppTables | Select-Object -Unique | ForEach-Object { "N''@P@$_''" }) -join ', '

$mappingSql = @"
-- =============================================================================
-- PLM DW → APP field mapping (generated — see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. PlmDw_Tables.sql
--   2. PlmDw_FieldMapping.sql    (this file)
-- USER SETTING: @TablePrefix (must match PlmDw_Tables.sql). Default: Plm_
-- Table: {prefix}FieldMapping
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix NVARCHAR(32) = N'$($config.tablePrefixDefault)';   -- <<< USER SETTING
DECLARE @MappingTable NVARCHAR(128) = @TablePrefix + N'FieldMapping';
DECLARE @sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable))
          AND name = N'DwTableName'
   )
BEGIN
    SET @sql = N'DROP TABLE dbo.' + QUOTENAME(@MappingTable) + N';';
    EXEC sp_executesql @sql;
END

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@MappingTable) + N' (
        [AppTableName]      NVARCHAR(128) NOT NULL,
        [AppColumnName]     NVARCHAR(128) NOT NULL,
        [DwTableName]       NVARCHAR(256) NOT NULL,
        [DwColumnName]      NVARCHAR(256) NOT NULL,
        [PlmTabId]          INT NULL,
        [PlmSubItemId]      INT NULL,
        [PlmGridSubItemId]  INT NULL,
        [PlmGridId]         INT NULL,
        [PlmMetaColumnId]   INT NULL,
        [PlmBlockId]        INT NULL,
        [DwFkTarget]        NVARCHAR(256) NULL,
        [FieldKind]         NVARCHAR(16)  NOT NULL,
        [PlmControlType]    INT NULL,
        [PlmEntityId]       INT NULL,
        [DwDataType]        NVARCHAR(32)  NULL,
        CONSTRAINT [PK_FieldMapping] PRIMARY KEY CLUSTERED ([AppTableName], [AppColumnName])
    );';
    EXEC sp_executesql @sql;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable)) AND name = N'PlmControlType')
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ADD [PlmControlType] INT NULL;';
        EXEC sp_executesql @sql;
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable)) AND name = N'PlmEntityId')
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ADD [PlmEntityId] INT NULL;';
        EXEC sp_executesql @sql;
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable)) AND name = N'DwDataType')
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ADD [DwDataType] NVARCHAR(32) NULL;';
        EXEC sp_executesql @sql;
    END
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] IN ($($deleteInList));';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind],[PlmControlType],[PlmEntityId],[DwDataType]
)
VALUES
        $valuesBlock
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

"@

$mappingPath = Join-Path $outDir 'PlmDw_FieldMapping.sql'
Set-Content -Path $mappingPath -Value $mappingSql -Encoding UTF8

$importTemplate = Join-Path $PSScriptRoot 'PlmDw_ImportFromDW.sql'
$importPath = Join-Path $outDir 'PlmDw_ImportFromDW.sql'
if (-not (Test-Path $importTemplate)) {
    throw "Missing import template: $importTemplate"
}
$importContent = Get-Content $importTemplate -Raw
$importContent = $importContent -replace "DECLARE @DwDatabase\s+NVARCHAR\(128\)\s+= N'plmDW'", "DECLARE @DwDatabase        NVARCHAR(128) = N'$($config.dwDatabase)'"
$plmDb = if ($config.plmDatabase) { $config.plmDatabase } else { 'PLM' }
$importContent = $importContent -replace "DECLARE @PlmDatabase\s+NVARCHAR\(128\)\s+= N'PLM'", "DECLARE @PlmDatabase       NVARCHAR(128) = N'$plmDb'"
$tplId = if ($null -ne $config.plmTemplateId -and [int]$config.plmTemplateId -gt 0) { [int]$config.plmTemplateId }
    elseif ($config.plmTemplate -and $null -ne $config.plmTemplate.templateId -and [int]$config.plmTemplate.templateId -gt 0) { [int]$config.plmTemplate.templateId }
    else { 'NULL' }
$importContent = $importContent -replace '(DECLARE @PlmTemplateId\s+INT\s+=\s*)NULL', "`${1}$tplId"
Set-Content -Path $importPath -Value $importContent -Encoding UTF8

if (-not $config.blueprint) {
    $config | Add-Member -NotePropertyName blueprint -NotePropertyValue ([ordered]@{
        templateName = 'Fabric'
        transactionGroupName = 'Fabric'
        folderName = 'Fabric'
    }) -Force
}
$blueprintObj = Build-BlueprintFromConfig $config $allFieldRows
$blueprintPath = Join-Path $outDir 'PlmDw_ImportBlueprint.json'
$blueprintJson = $blueprintObj | ConvertTo-Json -Depth 20 -Compress
$blueprintJsonPretty = $blueprintObj | ConvertTo-Json -Depth 20
$blueprintJsonPretty | Set-Content -Path $blueprintPath -Encoding UTF8

$escapedBlueprint = $blueprintJson -replace "'", "''"
$blueprintSql = @"
-- =============================================================================
-- Optional: store Import Blueprint JSON in tenant DB (Phase D helper)
-- Run after PlmDw_FieldMapping.sql if you want DB-stored blueprint copy.
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix NVARCHAR(32) = N'$($config.tablePrefixDefault)';
DECLARE @BlueprintTable NVARCHAR(128) = @TablePrefix + N'ImportBlueprint';
DECLARE @sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.' + QUOTENAME(@BlueprintTable), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@BlueprintTable) + N' (
        [BlueprintKey]   NVARCHAR(64)  NOT NULL DEFAULT N''default'',
        [SchemaVersion]  INT           NOT NULL,
        [BlueprintJson]  NVARCHAR(MAX) NOT NULL,
        [UpdatedAt]      DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_ImportBlueprint] PRIMARY KEY CLUSTERED ([BlueprintKey])
    );';
    EXEC sp_executesql @sql;
END

SET @sql = N'
MERGE dbo.' + QUOTENAME(@BlueprintTable) + N' AS t
USING (SELECT N''default'' AS BlueprintKey, 1 AS SchemaVersion, N''$escapedBlueprint'' AS BlueprintJson) AS s
ON t.BlueprintKey = s.BlueprintKey
WHEN MATCHED THEN UPDATE SET SchemaVersion = s.SchemaVersion, BlueprintJson = s.BlueprintJson, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (BlueprintKey, SchemaVersion, BlueprintJson) VALUES (s.BlueprintKey, s.SchemaVersion, s.BlueprintJson);';
EXEC sp_executesql @sql;
GO
"@
$blueprintSqlPath = Join-Path $outDir 'PlmDw_ImportBlueprint.sql'
Set-Content -Path $blueprintSqlPath -Value $blueprintSql -Encoding UTF8

Write-Host "Output folder: $outDir"
Write-Host "Generated: $tablesPath"
Write-Host "Generated: $mappingPath ($($allFieldRows.Count) mappings)"
Write-Host "Generated: $blueprintPath"
Write-Host "Generated: $blueprintSqlPath"
Write-Host "Generated: $importPath"
foreach ($t in $config.tabs) {
    $n = @($allFieldRows | Where-Object { $_.AppTable -eq $t.appTable }).Count
    Write-Host "  $($t.appTable): $n columns"
}
foreach ($g in $config.grids) {
    $n = @($allFieldRows | Where-Object { $_.AppTable -eq $g.appTable }).Count
    Write-Host "  $($g.appTable): $n columns"
}
Write-Host "  $($config.rootTableSuffix): 1 mapping (ReferenceCode)"
