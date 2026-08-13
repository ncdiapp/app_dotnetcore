# Simple QC (QX1) helpers - dot-sourced from _gen_plmdw_import_sql.ps1
# SpecQCGrid -> Plm_SimpleQC (POM) + Plm_SimpleQCResult (size rows); no flat QCSize1..N on host.

$script:SimpleQcPomColumns = @(
    'CriticalPoint', 'BodyPartDetailIDWDimDetailID', 'Code', 'BodyPartName', 'BodyPartDesc',
    'HowToMeasure', 'Tolerance', 'GradingBaseSize', 'Commtents', 'AddDesc',
    'DimensionDetail', 'Dimension', 'NeedToApplyGradingRule', 'Sort'
)

$script:SimpleQcResultMeasureStems = @(
    'GradingSize', 'QCSize', 'Difference',
    'QCSizeBeforeWash', 'DiffBeforeWashAndGrading',
    'QCAfterWashIron', 'DiffAfterIronAndGrading',
    'QCAfterIron'
)

function Get-SimpleQcSystemBlock($config) {
    if (-not $config.techPack -or -not $config.techPack.systemBlockGrids) { return $null }
    return @($config.techPack.systemBlockGrids) | Where-Object { $_.role -eq 'SpecQC' } | Select-Object -First 1
}

function Get-SimpleQcBinding($config) {
    if (-not $config.techPack -or -not $config.techPack.bindings) { return $null }
    return @($config.techPack.bindings) | Where-Object { $_.role -eq 'SimpleQC' } | Select-Object -First 1
}

function Test-IsSimpleQcSizeSlotColumn([string]$Stem) {
    if ([string]::IsNullOrWhiteSpace($Stem)) { return $false }
    foreach ($prefix in $script:SimpleQcResultMeasureStems) {
        if ($Stem -match ("^" + [regex]::Escape($prefix) + "\d+$")) { return $true }
    }
    return $false
}

function Get-SimpleQcAppColumnFromStem([string]$Stem) {
    if ([string]::IsNullOrWhiteSpace($Stem)) { return $null }
    # Add.Desc / Add_Desc -> AddDesc
    if ($Stem -eq 'Add_Desc' -or $Stem -eq 'Add.Desc' -or $Stem -eq 'AddDesc') { return 'AddDesc' }
    if ($Stem -eq 'Selected_Size' -or $Stem -eq 'SelectedSize' -or $Stem -eq 'SelectedSizes') { return 'SelectedSizes' }
    if ($Stem -match '^(Add_Desc|Add\.Desc)') { return 'AddDesc' }
    return $Stem
}

function Get-SimpleQcMaxSizeSlotFromDwCols($dwCols) {
    $max = 0
    foreach ($c in @($dwCols)) {
        $stem = (Get-DwColumnMeta $c.DwColumn).Stem
        if ($stem -match '^QCSize(\d+)$') {
            $n = [int]$Matches[1]
            if ($n -gt $max) { $max = $n }
        }
    }
    if ($max -lt 1) { $max = 20 }
    return $max
}

function Build-SimpleQcHostFieldRows($dwCols, [string]$DwTable, $TabId, [string]$AppTable, $gridSubItemId, $gridId) {
    $rows = New-Object System.Collections.Generic.List[object]
    $skip = @('ProductReferenceID', 'BlockID', 'GridID', 'RowID', 'RowValueGUID', 'Sort')
    foreach ($c in @($dwCols)) {
        $meta = Get-DwColumnMeta $c.DwColumn
        $stem = $meta.Stem
        if ([string]::IsNullOrWhiteSpace($stem)) { continue }
        if ($skip -contains $c.DwColumn -or $skip -contains $stem) { continue }
        if (Test-IsSimpleQcSizeSlotColumn $stem) { continue }

        $appCol = Get-SimpleQcAppColumnFromStem $stem
        if (-not ($script:SimpleQcPomColumns -contains $appCol) -and $appCol -ne 'AddDesc') {
            # Allow unknown non-size columns through (future PLM columns)
            if ($stem -match '\d+$' -and (Test-IsSimpleQcSizeSlotColumn $stem)) { continue }
        }

        $sqlType = if (Get-Command Get-AppSqlType -ErrorAction SilentlyContinue) {
            Get-AppSqlType $c $c.DwColumn
        } elseif ($c.SqlType) { $c.SqlType } else { 'nvarchar(4000)' }
        $dwDt = if ($c.DataType) { $c.DataType } elseif ($c.DwDataType) { $c.DwDataType } else { 'nvarchar' }
        $plmCtrl = if (Get-Command Infer-PlmControlType -ErrorAction SilentlyContinue) {
            Infer-PlmControlType $meta $dwDt
        } else { 2 }
        $plmEnt = if (Get-Command Infer-PlmEntityId -ErrorAction SilentlyContinue) {
            Infer-PlmEntityId $meta.FkTarget
        } else { $null }

        [void]$rows.Add([pscustomobject]@{
            AppTable         = $AppTable
            AppColumn        = $appCol
            DwTable          = $DwTable
            DwColumn         = $c.DwColumn
            Stem             = $stem
            NamePart         = $appCol
            SubItemId        = $meta.SubItemId
            FkTarget         = $meta.FkTarget
            SqlType          = $sqlType
            PlmTabId         = $TabId
            PlmGridSubItemId = $gridSubItemId
            PlmGridId        = $gridId
            PlmMetaColumnId  = $meta.SubItemId
            FieldKind        = 'GridColumn'
            DwDataType       = $dwDt
            PlmControlType   = $plmCtrl
            PlmEntityId      = $plmEnt
        })
    }
    if ($rows.Count -eq 0) { return @() }
    return @($rows.ToArray())
}

function Build-SimpleQcResultTableBlock([string]$LogicalTable, [string]$HostLogicalTable) {
    $hostLogical = $HostLogicalTable
    if ($hostLogical -match '^Plm_') { $hostLogical = $hostLogical.Substring(4) }
    $gcLogical = $LogicalTable
    if ($gcLogical -match '^Plm_') { $gcLogical = $gcLogical.Substring(4) }

    $alterLines = New-Object System.Collections.Generic.List[string]
    [void]$alterLines.Add("    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'ParentRowId')")
    [void]$alterLines.Add("    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [ParentRowId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END")
    [void]$alterLines.Add("    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'SizeRunSizeId')")
    [void]$alterLines.Add("    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [SizeRunSizeId] INT NOT NULL DEFAULT 0;'; EXEC sp_executesql @sql; END")
    foreach ($stem in $script:SimpleQcResultMeasureStems) {
        [void]$alterLines.Add("    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'$stem')")
        [void]$alterLines.Add("    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [$stem] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql; END")
        [void]$alterLines.Add("    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [$stem] NVARCHAR(4000) NULL;'; EXEC sp_executesql @sql;")
    }

    $measureColSql = ($script:SimpleQcResultMeasureStems | ForEach-Object { "        [$_] NVARCHAR(4000) NULL," }) -join "`r`n"

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("-- SimpleQCResult (QX1): one size row under $gcLogical; SizeRunSizeId = SizeOrder position N")
    [void]$sb.AppendLine("SET @TableName = @TablePrefix + N'$gcLogical';")
    [void]$sb.AppendLine("SET @HostTable = @TablePrefix + N'$hostLogical';")
    [void]$sb.AppendLine("SET @ParentFkName = N'FK_' + @TableName + N'_Parent';")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NULL")
    [void]$sb.AppendLine('BEGIN')
    [void]$sb.AppendLine("    SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@TableName) + N' (")
    [void]$sb.AppendLine('        [RowId] INT IDENTITY(1,1) NOT NULL,')
    [void]$sb.AppendLine('        [ParentRowId] INT NOT NULL,')
    [void]$sb.AppendLine('        [SizeRunSizeId] INT NOT NULL,')
    [void]$sb.AppendLine($measureColSql)
    [void]$sb.AppendLine("        CONSTRAINT [PK_$gcLogical] PRIMARY KEY CLUSTERED ([RowId])")
    [void]$sb.AppendLine("    );';")
    [void]$sb.AppendLine('    EXEC sp_executesql @sql;')
    [void]$sb.AppendLine("    PRINT N'Created ' + @TableName;")
    [void]$sb.AppendLine('END')
    [void]$sb.AppendLine('ELSE')
    [void]$sb.AppendLine('BEGIN')
    [void]$sb.AppendLine(($alterLines -join "`r`n"))
    [void]$sb.AppendLine('END')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NOT NULL")
    [void]$sb.AppendLine('   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)')
    [void]$sb.AppendLine('BEGIN')
    [void]$sb.AppendLine("    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName)")
    [void]$sb.AppendLine("        + N' WITH CHECK ADD CONSTRAINT ' + QUOTENAME(@ParentFkName)")
    [void]$sb.AppendLine("        + N' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.' + QUOTENAME(@HostTable) + N' ([RowId]);';")
    [void]$sb.AppendLine('    EXEC sp_executesql @sql;')
    [void]$sb.AppendLine('END')
    [void]$sb.AppendLine('')
    return $sb.ToString()
}

function Add-SimpleQcResultFieldRows($allFieldRows, [string]$AppTable, [string]$HostAppTable, [string]$DwTable, $TabId, $gridSubItemId, $gridId) {
    $base = @(
        @{ AppColumn = 'ParentRowId'; FieldKind = 'GrandchildPivot'; SqlType = 'int' },
        @{ AppColumn = 'SizeRunSizeId'; FieldKind = 'GrandchildPivot'; SqlType = 'int' }
    )
    foreach ($b in $base) {
        [void]$allFieldRows.Add([pscustomobject]@{
            AppTable         = $AppTable
            AppColumn        = $b.AppColumn
            DwTable          = $DwTable
            DwColumn         = $null
            Stem             = $b.AppColumn
            NamePart         = $b.AppColumn
            SubItemId        = $gridSubItemId
            FkTarget         = if ($b.AppColumn -eq 'ParentRowId') { $HostAppTable } else { $null }
            SqlType          = $b.SqlType
            PlmTabId         = $TabId
            PlmGridSubItemId = $gridSubItemId
            PlmGridId        = $gridId
            PlmMetaColumnId  = $null
            FieldKind        = $b.FieldKind
            DwDataType       = $b.SqlType
            PlmControlType   = 2
            PlmEntityId      = $null
        })
    }
    foreach ($stem in $script:SimpleQcResultMeasureStems) {
        [void]$allFieldRows.Add([pscustomobject]@{
            AppTable         = $AppTable
            AppColumn        = $stem
            DwTable          = $DwTable
            DwColumn         = $null
            Stem             = $stem
            NamePart         = $stem
            SubItemId        = $gridSubItemId
            FkTarget         = $null
            SqlType          = 'nvarchar(4000)'
            PlmTabId         = $TabId
            PlmGridSubItemId = $gridSubItemId
            PlmGridId        = $gridId
            PlmMetaColumnId  = $null
            FieldKind        = 'GrandchildPivot'
            DwDataType       = 'nvarchar'
            PlmControlType   = 2
            PlmEntityId      = $null
        })
    }
}

function Build-SimpleQcPivotBindings($config, $transactions) {
    $list = New-Object System.Collections.Generic.List[object]
    $binding = Get-SimpleQcBinding $config
    $sb = Get-SimpleQcSystemBlock $config
    if (-not $binding -or -not $sb) { return @($list.ToArray()) }

    $txId = if ($binding.transactionIntegrationId) { [string]$binding.transactionIntegrationId } else { "Tab_$($binding.plmTabId)" }
    $tx = @($transactions) | Where-Object { $_.integrationId -eq $txId -or [string]$_.plmTabId -eq [string]$binding.plmTabId } | Select-Object -First 1
    if (-not $tx) { return @($list.ToArray()) }

    [void]$list.Add([pscustomobject]@{
        plmTabId                = [int]$binding.plmTabId
        hostAppTableName        = 'SimpleQC'
        grandchildAppTableName  = 'SimpleQCResult'
        sourceAppTableName      = 'View_TchpSimpleQcSelectedSizes'
        sourcePivotKeyColumn    = 'SizeRunSizeId'
        pivotColumnField        = 'SizeRunSizeId'
        pivotValueFields        = @($script:SimpleQcResultMeasureStems)
        skipMatrixKeyVisibleFilter = $false
        skipTablePrefixOnHost   = $false
        skipTablePrefixOnGrandchild = $false
        skipTablePrefixOnSource = $true
    })
    return @($list.ToArray())
}

function Generate-SimpleQcImportSqlFile($config, [string]$outDir) {
    $sb = Get-SimpleQcSystemBlock $config
    if (-not $sb) { return $null }

    $dwDb = [string]$config.dwDatabase
    $dwRef = '[' + ($dwDb -replace ']', ']]') + '].dbo'
    $sgDw = [string]$sb.dwTable
    $hostApp = 'SimpleQC'
    $resultApp = 'SimpleQCResult'
    $hostPhys = $config.tablePrefixDefault + $hostApp
    $resultPhys = $config.tablePrefixDefault + $resultApp

    $dwCols = @(Get-DwTableColumns $sgDw)
    $maxN = Get-SimpleQcMaxSizeSlotFromDwCols $dwCols
    $qcTabId = [int]$sb.parentPlmTabId
    $plmDb = if ($config.plmDatabase) { [string]$config.plmDatabase } else { 'PLM' }
    $plmRef = '[' + ($plmDb -replace ']', ']]') + '].dbo'

    $pomColMap = @{}
    foreach ($c in $dwCols) {
        $stem = (Get-DwColumnMeta $c.DwColumn).Stem
        $appCol = Get-SimpleQcAppColumnFromStem $stem
        if ($script:SimpleQcPomColumns -contains $appCol -or $appCol -eq 'AddDesc') {
            $pomColMap[$appCol] = $c.DwColumn
        }
    }

    $L = New-Object System.Collections.Generic.List[string]
    $add = { param($s) [void]$L.Add($s) }

    & $add '-- ============================================================================='
    & $add '-- Simple QC (QX1): SpecQCGrid -> Plm_SimpleQC + Plm_SimpleQCResult UNPIVOT'
    & $add "-- Size Index N = SizeOrder position in SizeRun (PLM GetDictSortSizeRelatedRotateSizeId)."
    & $add "-- Source grid: $sgDw  MaxSlot=$maxN"
    & $add '-- ============================================================================='
    & $add 'SET NOCOUNT ON;'
    & $add 'SET XACT_ABORT ON;'
    & $add ''

    # QcSelectedSizes from PLM PdmProductQcSize (Size Selector checkboxes), not DW Selected_Size (full Size Run).
    & $add '-- StyleSpec.QcSelectedSizes <- PdmProductQcSize (checked sizes only; DW Selected_Size is the full Size Run)'
    & $add 'IF COL_LENGTH(N''dbo.TchpStyleSpec'', N''QcSelectedSizes'') IS NULL'
    & $add '    ALTER TABLE dbo.TchpStyleSpec ADD QcSelectedSizes NVARCHAR(4000) NULL;'
    & $add ''
    & $add 'UPDATE ss SET'
    & $add '  ss.QcSelectedSizes = x.SelectedCsv,'
    & $add '  ss.AppModifiedDate = GETDATE()'
    & $add 'FROM dbo.TchpStyleSpec ss'
    & $add 'INNER JOIN ('
    & $add '    SELECT'
    & $add '        q.ProductReferenceID,'
    & $add "        STRING_AGG(CONVERT(NVARCHAR(20), q.SizeRunRotateID), N'|')"
    & $add '            WITHIN GROUP (ORDER BY q.SizeRunRotateID) AS SelectedCsv'
    & $add "    FROM $plmRef.PdmProductQcSize q"
    & $add "    WHERE q.TabID = $qcTabId"
    & $add '      AND q.ProductReferenceID IS NOT NULL'
    & $add '      AND q.SizeRunRotateID IS NOT NULL'
    & $add '    GROUP BY q.ProductReferenceID'
    & $add ') x ON x.ProductReferenceID = ss.StyleSpecId;'
    & $add "PRINT N'TchpStyleSpec.QcSelectedSizes updated from PdmProductQcSize. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));"
    & $add ''

    # Merge SimpleQC host rows
    $insertCols = New-Object System.Collections.Generic.List[string]
    $selectCols = New-Object System.Collections.Generic.List[string]
    [void]$insertCols.Add('ReferenceId')
    [void]$selectCols.Add('TRY_CONVERT(INT, g.ProductReferenceID) AS ReferenceId')
    foreach ($appCol in $script:SimpleQcPomColumns) {
        if (-not $pomColMap.ContainsKey($appCol)) { continue }
        $dwc = $pomColMap[$appCol]
        [void]$insertCols.Add($appCol)
        [void]$selectCols.Add("CONVERT(NVARCHAR(4000), g.$dwc) AS [$appCol]")
    }

    & $add "-- Host POM rows -> $hostPhys"
    & $add "DELETE FROM dbo.[$resultPhys];"
    & $add "DELETE FROM dbo.[$hostPhys];"
    & $add "INSERT INTO dbo.[$hostPhys] ("
    & $add ('  ' + ($insertCols -join ', '))
    & $add ')'
    & $add 'SELECT'
    & $add ('  ' + ($selectCols -join ",`r`n  "))
    & $add "FROM $dwRef.$sgDw g"
    & $add 'WHERE TRY_CONVERT(INT, g.ProductReferenceID) IS NOT NULL;'
    & $add "PRINT N'$hostPhys insert. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));"
    & $add ''

    # UNPIVOT size slots
    & $add "-- Size rows -> $resultPhys (N = SizeOrder)"
    $unionParts = New-Object System.Collections.Generic.List[string]
    for ($n = 1; $n -le $maxN; $n++) {
        $measureSelects = New-Object System.Collections.Generic.List[string]
        foreach ($stem in $script:SimpleQcResultMeasureStems) {
            $dwColObj = Find-DwColumnByStem $dwCols ($stem + $n)
            if ($dwColObj) {
                [void]$measureSelects.Add("CONVERT(NVARCHAR(4000), g.$($dwColObj.DwColumn)) AS [$stem]")
            }
            else {
                [void]$measureSelects.Add("CAST(NULL AS NVARCHAR(4000)) AS [$stem]")
            }
        }
        $part = @"
SELECT
  h.RowId AS ParentRowId,
  $n AS SizeOrdinal,
  $($measureSelects -join ",`r`n  ")
FROM $dwRef.$sgDw g
INNER JOIN dbo.[$hostPhys] h
  ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
 AND ISNULL(h.Sort, -1) = ISNULL(TRY_CONVERT(INT, g.Sort), -1)
"@
        # Sort join may fail if Sort missing - fallback ReferenceId + row identity via RowID
        if (-not $pomColMap.ContainsKey('Sort')) {
            $part = @"
SELECT
  h.RowId AS ParentRowId,
  $n AS SizeOrdinal,
  $($measureSelects -join ",`r`n  "),
  TRY_CONVERT(INT, g.RowID) AS DwRowId
FROM $dwRef.$sgDw g
INNER JOIN dbo.[$hostPhys] h ON h.ReferenceId = TRY_CONVERT(INT, g.ProductReferenceID)
"@
        }
        [void]$unionParts.Add($part)
    }

    # Prefer RowID match when available: rebuild host with DwRowId staging - keep simple ReferenceId+Sort or first match
    & $add ';WITH slot AS ('
    & $add (($unionParts -join "`r`nUNION ALL`r`n"))
    & $add ')'
    & $add "INSERT INTO dbo.[$resultPhys] (ParentRowId, SizeRunSizeId, $($script:SimpleQcResultMeasureStems -join ', '))"
    & $add 'SELECT s.ParentRowId, sz.SizeRunSizeId,'
    & $add ('  ' + (($script:SimpleQcResultMeasureStems | ForEach-Object { "s.[$_]" }) -join ', '))
    & $add 'FROM slot s'
    & $add "INNER JOIN dbo.[$hostPhys] h ON h.RowId = s.ParentRowId"
    & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = h.ReferenceId'
    & $add 'INNER JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunId = ss.SizeRunId AND ISNULL(sz.SizeOrder, 0) = s.SizeOrdinal;'
    & $add "PRINT N'$resultPhys insert. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));"
    & $add ''
    & $add 'PRINT N''Simple QC import finished.'';'
    & $add 'GO'

    $outPath = Join-Path $outDir '3c_PlmDw_ImportSimpleQc.sql'
    [System.IO.File]::WriteAllText($outPath, ($L -join "`r`n"), [System.Text.UTF8Encoding]::new($false))
    Write-Host "  Wrote $outPath"
    return $outPath
}
