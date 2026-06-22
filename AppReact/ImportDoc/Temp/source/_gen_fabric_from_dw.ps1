$ErrorActionPreference = 'Stop'
$outDir = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $PSScriptRoot 'dwFabricTabConfig.json'
$config = Get-Content $configPath -Raw | ConvertFrom-Json

$SqlServer = $config.sqlServer
$DwDatabase = $config.dwDatabase
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
        $args = @('-S', $SqlServer, '-d', $DwDatabase, '-E', '-i', $tmp, '-o', $out, '-W', '-s', '|', '-h', '-1')
        $p = Start-Process -FilePath 'sqlcmd' -ArgumentList $args -Wait -PassThru -NoNewWindow
        if ($p.ExitCode -ne 0) { throw "sqlcmd failed ($($p.ExitCode)): $Query" }
        $lines = Get-Content $out | Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' }
        return ,$lines
    }
    finally {
        Remove-Item $tmp, $out -ErrorAction SilentlyContinue
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
$ddlParts = New-Object System.Collections.Generic.List[string]

$ddlParts.Add(@"
-- =============================================================================
-- FABRIC 02 - physical tables (generated from plmDW by source/_gen_fabric_from_dw.ps1)
-- EXECUTION ORDER:
--   1. Fabric_Tables.sql          (this file)
--   2. Fabric_FieldMapping.sql
-- USER SETTINGS (single batch - do not split with GO):
--   @TablePrefix     table prefix, include trailing underscore (default Plm_)
--   @RootTableSuffix root table name after prefix (default ReferenceBasicInfo)
-- Source: plmDW Tab/Grid wide tables (latest DW schema)
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
    $dwCols = Get-DwTableColumns $grid.dwTable
    $fieldRows = Build-FieldRows $dwCols $grid.dwTable $null $grid.appTable 'GridColumn' $grid.gridSubItemId $grid.gridId
    $ddlParts.Add((Build-CreateTableBlock $grid.appTable $fieldRows $true))
    foreach ($r in $fieldRows) { [void]$allFieldRows.Add($r) }
}

$ref = $config.referenceCode
[void]$allFieldRows.Add([pscustomobject]@{
    AppTable         = $config.rootTableSuffix
    AppColumn        = 'ReferenceCode'
    DwTable          = $ref.dwTable
    DwColumn         = $ref.dwColumn
    Stem             = 'ReferenceCode'
    NamePart         = 'ReferenceCode'
    SubItemId        = $ref.plmSubItemId
    FkTarget         = $null
    SqlType          = $null
    PlmTabId         = $ref.plmTabId
    PlmGridSubItemId = $null
    PlmGridId        = $null
    PlmMetaColumnId  = $null
    FieldKind        = 'ReferenceField'
})

$ddlParts.Add('GO')
$ddlParts.Add('')

$tablesPath = Join-Path $outDir 'Fabric_Tables.sql'
Set-Content -Path $tablesPath -Value ($ddlParts -join "`n") -Encoding UTF8

$valuesLines = New-Object System.Collections.Generic.List[string]
foreach ($r in $allFieldRows) {
    $appTable = $r.AppTable
    $fkSql = if ($r.FkTarget) { SqlStrDyn $r.FkTarget } else { 'NULL' }
    $line = "(N''@P@$appTable'', N''$($r.AppColumn -replace "'", "''")'', $(SqlStrDyn $r.DwTable), $(SqlStrDyn $r.DwColumn), $(SqlInt $r.PlmTabId), $(SqlInt $r.SubItemId), $(SqlInt $r.PlmGridSubItemId), $(SqlInt $r.PlmGridId), $(SqlInt $r.PlmMetaColumnId), NULL, $fkSql, $(SqlStrDyn $r.FieldKind))"
    [void]$valuesLines.Add($line)
}
$valuesBlock = $valuesLines -join ",`n        "

$mappingSql = @"
-- =============================================================================
-- FABRIC 02 - field mapping table + seed data (generated from plmDW)
-- EXECUTION ORDER:
--   1. Fabric_Tables.sql
--   2. Fabric_FieldMapping.sql    (this file)
-- USER SETTING: @TablePrefix (must match Fabric_Tables.sql). Default: Plm_
-- Table: {prefix}FieldMapping
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix NVARCHAR(32) = N'$($config.tablePrefixDefault)';   -- <<< USER SETTING (must match Fabric_Tables.sql)
DECLARE @MappingTable NVARCHAR(128) = @TablePrefix + N'FieldMapping';
DECLARE @sql NVARCHAR(MAX);

-- Recreate mapping table when upgrading from legacy 6-column schema
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
        CONSTRAINT [PK_FieldMapping] PRIMARY KEY CLUSTERED ([AppTableName], [AppColumnName])
    );';
    EXEC sp_executesql @sql;
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] LIKE @p + N''Fabric_%'''
    + N' OR [AppTableName] = @p + N''ProductColor'''
    + N' OR [AppTableName] = @p + N''ReferenceBasicInfo'';';
EXEC sp_executesql @sql, N'@p nvarchar(32)', @p = @TablePrefix;

SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' (
    [AppTableName],[AppColumnName],[DwTableName],[DwColumnName],
    [PlmTabId],[PlmSubItemId],[PlmGridSubItemId],[PlmGridId],[PlmMetaColumnId],
    [PlmBlockId],[DwFkTarget],[FieldKind]
)
VALUES
        $valuesBlock
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

"@

$mappingPath = Join-Path $outDir 'Fabric_FieldMapping.sql'
Set-Content -Path $mappingPath -Value $mappingSql -Encoding UTF8

$tabCounts = $config.tabs | ForEach-Object {
    $n = @($allFieldRows | Where-Object { $_.AppTable -eq $_.appTable }).Count
    "$($_.appTable): $n"
}
Write-Host "Generated: $tablesPath"
Write-Host "Generated: $mappingPath ($($allFieldRows.Count) mappings)"
foreach ($t in $config.tabs) {
    $n = @($allFieldRows | Where-Object { $_.AppTable -eq $t.appTable }).Count
    Write-Host "  $($t.appTable): $n columns"
}
$pc = @($allFieldRows | Where-Object { $_.AppTable -eq 'ProductColor' }).Count
Write-Host "  ProductColor: $pc columns"
Write-Host "  ReferenceBasicInfo: 1 mapping (ReferenceCode)"
