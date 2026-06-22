$ErrorActionPreference = 'Stop'
$outDir = Split-Path $PSScriptRoot -Parent
$srcPath = Join-Path $PSScriptRoot 'fabricTables.sql'
$productColorPath = Join-Path $PSScriptRoot 'productColorGrid.sql'
$sql = Get-Content $srcPath -Raw
$productColorSql = Get-Content $productColorPath -Raw

# PLM Colorways tab grid subitem (Colors_7354 on export wide table)
$ProductColorGridSubItemId = 7354

function Parse-Table($sql, $tableName) {
    $pattern = "CREATE TABLE \[dbo\]\.\[$tableName\]\(([\s\S]*?)\) ON \[PRIMARY\]"
    $m = [regex]::Match($sql, $pattern)
    if (-not $m.Success) { throw "Table not found: $tableName" }
    $body = $m.Groups[1].Value
    $cols = @()
    foreach ($line in ($body -split "`n")) {
        $cm = [regex]::Match($line, '^\s*\[([^\]]+)\]\s+((?:\[[^\]]+\]|[a-zA-Z_]+)(?:\([^\)]*\))?)')
        if ($cm.Success) {
            $full = $cm.Groups[1].Value
            $sqlType = $cm.Groups[2].Value.Trim()
            if ($full -in @('ReferenceId','RowId','Sort')) {
                $cols += [pscustomobject]@{ PlmFull=$full; Base=$full; SubItemId=$null; SqlType=$sqlType; IsSystem=$true }
            } elseif ($full -match '^(.+)_(\d+)$') {
                $cols += [pscustomobject]@{ PlmFull=$full; Base=$Matches[1]; SubItemId=[int]$Matches[2]; SqlType=$sqlType; IsSystem=$false }
            } else {
                $cols += [pscustomobject]@{ PlmFull=$full; Base=$full; SubItemId=$null; SqlType=$sqlType; IsSystem=$false }
            }
        }
    }
    return $cols
}

function Get-AppColumnNames($cols) {
    $baseCounts = @{}
    foreach ($c in ($cols | Where-Object { -not $_.IsSystem })) {
        if (-not $baseCounts.ContainsKey($c.Base)) { $baseCounts[$c.Base] = 0 }
        $baseCounts[$c.Base]++
    }
    $result = @{}
    foreach ($c in $cols) {
        if ($c.IsSystem) { $result[$c.PlmFull] = $c.PlmFull; continue }
        if ($baseCounts[$c.Base] -eq 1) { $result[$c.PlmFull] = $c.Base }
        else { $result[$c.PlmFull] = $c.PlmFull }
    }
    return $result
}

function Build-CreateTableBlock($logicalTable, $cols, $isGrid) {
    $appNames = Get-AppColumnNames $cols
    $colDefs = New-Object System.Collections.Generic.List[string]
    if ($isGrid) {
        [void]$colDefs.Add('[RowId] INT IDENTITY(1,1) NOT NULL')
        [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
        [void]$colDefs.Add('[Sort] INT NULL')
    } else {
        [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
    }
    foreach ($c in ($cols | Where-Object { -not $_.IsSystem })) {
        $appCol = $appNames[$c.PlmFull]
        [void]$colDefs.Add("[$appCol] $($c.SqlType) NULL")
    }
    if ($isGrid) {
        [void]$colDefs.Add("CONSTRAINT [PK_$logicalTable] PRIMARY KEY CLUSTERED ([RowId])")
    } else {
        [void]$colDefs.Add("CONSTRAINT [PK_$logicalTable] PRIMARY KEY CLUSTERED ([ReferenceId])")
    }
    $innerCols = ($colDefs -join ', ')
    return @"
-- $logicalTable
SET @TableName = @TablePrefix + N'$logicalTable';
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

"@ , $appNames
}

$headerCols = Parse-Table $sql 'Plm_Fabric_Header'
$infoCols = Parse-Table $sql 'Plm_Fabric_Info'
$headerSubIds = @{}
foreach ($c in ($headerCols | Where-Object { $_.SubItemId })) { $headerSubIds[$c.SubItemId] = $true }
$infoOnlyCols = @($infoCols | Where-Object { $_.IsSystem -or ($_.SubItemId -and -not $headerSubIds.ContainsKey($_.SubItemId)) })

$tableDefs = @(
    @{ Logical='Fabric_Header'; Cols=$headerCols; Grid=$false; GridSubItemId=$null }
    @{ Logical='Fabric_Info'; Cols=$infoOnlyCols; Grid=$false; GridSubItemId=$null }
    @{ Logical='Fabric_Attributes'; Cols=(Parse-Table $sql 'Plm_Attributes'); Grid=$false; GridSubItemId=$null }
    @{ Logical='Fabric_Cost'; Cols=(Parse-Table $sql 'Plm_Fabric_Cost'); Grid=$false; GridSubItemId=$null }
    @{ Logical='Fabric_Policy'; Cols=(Parse-Table $sql 'Plm_Fabric_Policy'); Grid=$false; GridSubItemId=$null }
    @{ Logical='Fabric_Testing'; Cols=(Parse-Table $sql 'Plm_Testing_Compliance'); Grid=$false; GridSubItemId=$null }
    @{ Logical='Fabric_DenimTracker'; Cols=(Parse-Table $sql 'Plm_Denim_Non_Denim_Tracker_5151'); Grid=$true; GridSubItemId=5151 }
    @{ Logical='ProductColor'; Cols=(Parse-Table $productColorSql 'Plm_Product_Colors'); Grid=$true; GridSubItemId=$ProductColorGridSubItemId }
)

$ddlHeader = @"
-- =============================================================================
-- FABRIC 02 - physical tables (from source/fabricTables.sql)
-- EXECUTION ORDER:
--   1. Fabric_Tables.sql          (this file)
--   2. Fabric_FieldMapping.sql
-- USER SETTINGS (single batch - do not split with GO):
--   @TablePrefix     table prefix, include trailing underscore (default Plm_)
--   @RootTableSuffix root table name after prefix (default ReferenceBasicInfo)
-- Creates: {prefix}ReferenceBasicInfo, {prefix}Fabric_*, {prefix}ProductColor (shared grid)
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix     NVARCHAR(32)  = N'Plm_';               -- <<< USER SETTING
DECLARE @RootTableSuffix NVARCHAR(128) = N'ReferenceBasicInfo'; -- <<< USER SETTING
DECLARE @TableName       NVARCHAR(128);
DECLARE @RootTable       NVARCHAR(128);
DECLARE @FkName          NVARCHAR(128);
DECLARE @sql             NVARCHAR(MAX);

"@

$ddlParts = New-Object System.Collections.Generic.List[string]
[void]$ddlParts.Add($ddlHeader)
[void]$ddlParts.Add(@"
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
$allMappings = New-Object System.Collections.Generic.List[object]

foreach ($def in $tableDefs) {
    $block, $appNames = Build-CreateTableBlock $def.Logical $def.Cols $def.Grid
    [void]$ddlParts.Add($block)
    foreach ($c in ($def.Cols | Where-Object { -not $_.IsSystem })) {
        $appCol = $appNames[$c.PlmFull]
        if ($def.Grid) {
            $allMappings.Add([pscustomobject]@{
                LogicalTable=$def.Logical; AppColumn=$appCol
                PlmBlockId=$null; PlmSubItemId=$null; PlmGridId=$def.GridSubItemId; PlmMetaColumnId=$c.SubItemId
            })
        } else {
            $allMappings.Add([pscustomobject]@{
                LogicalTable=$def.Logical; AppColumn=$appCol
                PlmBlockId=$null; PlmSubItemId=$c.SubItemId; PlmGridId=$null; PlmMetaColumnId=$null
            })
        }
    }
}

$tablesPath = Join-Path $outDir 'Fabric_Tables.sql'
[void]$ddlParts.Add('GO')
[void]$ddlParts.Add('')

Set-Content -Path $tablesPath -Value ($ddlParts -join "`n") -Encoding UTF8

function SqlInt($n) {
    if ($null -eq $n) { return 'NULL' }
    return [string]$n
}
function Esc([string]$s) { return ($s -replace "'", "''") }

$valuesLines = New-Object System.Collections.Generic.List[string]
foreach ($m in $allMappings) {
    $ac = Esc $m.AppColumn
    $lt = Esc $m.LogicalTable
    [void]$valuesLines.Add("(N''@P@$lt'', N''$ac'', $(SqlInt $m.PlmBlockId), $(SqlInt $m.PlmSubItemId), $(SqlInt $m.PlmGridId), $(SqlInt $m.PlmMetaColumnId))")
}
$valuesBlock = $valuesLines -join ",`n        "

$mappingSql = @"
-- =============================================================================
-- FABRIC 02 - field mapping table + seed data
-- EXECUTION ORDER:
--   1. Fabric_Tables.sql
--   2. Fabric_FieldMapping.sql    (this file)
-- USER SETTING: @TablePrefix (must match Fabric_Tables.sql). Default: Plm_
-- Table: {prefix}FieldMapping
-- Columns: AppTableName, AppColumnName, PlmBlockId, PlmSubItemId, PlmGridId, PlmMetaColumnId
-- =============================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

DECLARE @TablePrefix NVARCHAR(32) = N'Plm_';   -- <<< USER SETTING (must match Fabric_Tables.sql)
DECLARE @MappingTable NVARCHAR(128) = @TablePrefix + N'FieldMapping';
DECLARE @sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable), N'U') IS NULL
BEGIN
    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@MappingTable) + N' (
        [AppTableName]    NVARCHAR(128) NOT NULL,
        [AppColumnName]   NVARCHAR(128) NOT NULL,
        [PlmBlockId]      INT NULL,
        [PlmSubItemId]    INT NULL,
        [PlmGridId]       INT NULL,
        [PlmMetaColumnId] INT NULL,
        CONSTRAINT [PK_FieldMapping] PRIMARY KEY CLUSTERED ([AppTableName], [AppColumnName])
    );';
    EXEC sp_executesql @sql;
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] LIKE @p + N''Fabric_%'''
    + N' OR [AppTableName] = @p + N''ProductColor'';';
EXEC sp_executesql @sql, N'@p nvarchar(32)', @p = @TablePrefix;

SET @sql = N'
INSERT INTO dbo.' + QUOTENAME(@MappingTable) + N' ([AppTableName],[AppColumnName],[PlmBlockId],[PlmSubItemId],[PlmGridId],[PlmMetaColumnId])
VALUES
        $valuesBlock
';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;
GO

"@

$mappingPath = Join-Path $outDir 'Fabric_FieldMapping.sql'
Set-Content -Path $mappingPath -Value $mappingSql -Encoding UTF8

Write-Host "Generated: $tablesPath, $mappingPath ($($allMappings.Count) mappings)"
