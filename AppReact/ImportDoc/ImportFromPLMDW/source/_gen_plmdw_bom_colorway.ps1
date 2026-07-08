# BOM Colorway pivot helpers — dot-sourced from _gen_plmdw_import_sql.ps1

function Test-IsBomColorwayStagingColumn([string]$AppColumn) {
    if ([string]::IsNullOrWhiteSpace($AppColumn)) { return $false }
    if ($AppColumn -match '^Colorway_\d+$') { return $true }
    if ($AppColumn -match '^Image\d+$') { return $true }
    return $false
}

function Get-PlmEntityIdBySysTableName([string]$sysTableName) {
    if ([string]::IsNullOrWhiteSpace($sysTableName)) { return $null }
    $safe = $sysTableName.Replace("'", "''")
    $q = @"
SELECT TOP 1 e.EntityID
FROM dbo.pdmEntity e
WHERE e.EntityType = 1
  AND LTRIM(RTRIM(e.SysTableName)) = N'$safe'
ORDER BY e.EntityID
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $t = if ($null -eq $line) { '' } else { $line.Trim() }
        if ($t -and $t -ne 'NULL') { return [int]$t }
    }
    return $null
}

function Get-PlmGridMetaColumnsForGrids([int[]]$GridIds) {
    $map = @{}
    if (-not $GridIds -or $GridIds.Count -eq 0) { return $map }
    $inList = ($GridIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT g.GridID, g.GridColumnID, LEFT(LTRIM(RTRIM(g.ColumnName)), 200) AS ColumnName,
       ISNULL(g.ColumnOrder, 9999) AS ColumnOrder, g.ColumnTypeId, g.EntityId,
       ISNULL(g.IsDCUForProductGridRef, 0) AS IsDCUForProductGridRef, g.DCUColumnBlockID,
       g.DcucolumnId, g.MasterDcucolumnId
FROM dbo.pdmGridMetaColumn g
WHERE g.GridID IN ($inList)
ORDER BY g.GridID, ISNULL(g.ColumnOrder, 9999), g.GridColumnID
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 9) { continue }
        $gid = [int]$parts[0].Trim()
        if (-not $map.ContainsKey($gid)) { $map[$gid] = [System.Collections.Generic.List[object]]::new() }
        $map[$gid].Add([pscustomobject]@{
            GridId                   = $gid
            GridColumnId             = [int]$parts[1].Trim()
            ColumnName               = $parts[2].Trim()
            ColumnOrder              = [int]$parts[3].Trim()
            ColumnTypeId             = [int]$parts[4].Trim()
            EntityId                 = (Parse-SqlIntOrNull $parts[5].Trim())
            IsDCUForProductGridRef   = ([int]$parts[6].Trim() -eq 1)
            DCUColumnBlockId         = (Parse-SqlIntOrNull $parts[7].Trim())
            DcucolumnId              = (Parse-SqlIntOrNull $parts[8].Trim())
            MasterDcucolumnId        = if ($parts.Count -ge 10) { (Parse-SqlIntOrNull $parts[9].Trim()) } else { $null }
        })
    }
    return $map
}

function Get-PivotValueMetaColumnsForDcuKey($gridMetaList, [int]$dcuKeyColumnId) {
    if (-not $gridMetaList) { return @() }
    $keyCol = $gridMetaList | Where-Object { $_.GridColumnId -eq $dcuKeyColumnId } | Select-Object -First 1
    if (-not $keyCol) { return @() }

    $values = [System.Collections.Generic.List[object]]::new()
    [void]$values.Add($keyCol)

    $children = @($gridMetaList | Where-Object {
        $_.GridColumnId -ne $dcuKeyColumnId -and (
            ($_.DcucolumnId -eq $dcuKeyColumnId) -or ($_.MasterDcucolumnId -eq $dcuKeyColumnId)
        )
    } | Sort-Object ColumnOrder, GridColumnId)

    # Fallback: PLM sometimes wires MasterDcucolumnId to another template's ColorwayN
    # (e.g. Fabric 20 Status1 -> 10-colorway Colorway1). Pair ImageN/StatusN by slot number.
    if ($children.Count -eq 0 -and $keyCol.ColumnName -match '(?<slot>\d+)\s*$') {
        $slot = $Matches['slot']
        $children = @($gridMetaList | Where-Object {
            $_.GridColumnId -ne $dcuKeyColumnId -and (
                ($_.ColumnName -match "^(Image|Status)\s*_?\s*$slot\s*$") -or
                ($_.ColumnName -match "^(Image|Status)$slot\s*$")
            )
        } | Sort-Object ColumnOrder, GridColumnId)
    }

    foreach ($c in $children) { [void]$values.Add($c) }
    return @($values)
}

function Get-PlmColumnBaseName([string]$columnName) {
    if ([string]::IsNullOrWhiteSpace($columnName)) { return 'PivotValue' }
    $n = $columnName.Trim()
    if ($n -match '^(?<base>.+?)[_\s]*(?<slot>\d+)$') {
        $base = $Matches['base'].TrimEnd('_').Trim()
        if (-not [string]::IsNullOrWhiteSpace($base)) { return $base }
    }
    return $n
}

function Get-PivotValueBusinessRole($vm, [int]$dcuKeyColumnId) {
    if ($vm.GridColumnId -eq $dcuKeyColumnId) { return 'SlotColorValue' }
    if ($vm.ColumnTypeId -eq 5) { return 'SlotChildImage' }
    if ($vm.ColumnTypeId -eq 1) { return 'SlotChildDdl' }
    return 'SlotChildOther'
}

function Get-BomColorwayPivotColumnNameOverrides($config, [int]$gridId) {
    if (-not $config -or -not $config.bomColorwayPivotColumnNames) { return $null }
    $gridKey = [string]$gridId
    if ($config.bomColorwayPivotColumnNames.PSObject.Properties.Name -notcontains $gridKey) { return $null }
    # ConvertFrom-Json unwraps single-element arrays to a scalar string; force a real Object[]
    # so indexers return whole names (not chars). Unary comma prevents return unwrap.
    $raw = $config.bomColorwayPivotColumnNames.$gridKey
    if ($null -eq $raw) { return $null }
    if ($raw -is [string]) { return , @([string]$raw) }
    return , @($raw)
}

function Get-DefaultPivotValueColumnName([string]$businessRole, [string]$plmColumnName, [hashtable]$usedNames) {
    $base = Get-PlmColumnBaseName $plmColumnName
    $candidate = switch ($businessRole) {
        'SlotColorValue' {
            # Wide-slot cell: selected artwork color (FK pdmRGBColor). Not the pivot-key column "Colorway".
            'ArtworkColor'
        }
        'SlotChildImage' {
            'ArtworkPhoto'
        }
        default {
            if ($base -ieq 'Colorway') { 'ArtworkColor' } else { $base }
        }
    }
    return (Sanitize-PlmGrandchildColumnName $candidate @('Colorway', 'ParentRowId', 'RowId') $usedNames)
}

function Get-PivotValueBusinessDescription([string]$businessRole) {
    switch ($businessRole) {
        'SlotColorValue' { return 'Per-colorway artwork color selection (wide slot ColorwayN -> normalized pivot value)' }
        'SlotChildImage' { return 'Per-colorway artwork image/sketch (wide slot ImageN -> normalized pivot value)' }
        'SlotChildDdl'   { return 'Per-colorway DDL value column linked under DCU slot' }
        default          { return 'Per-colorway child value column under DCU slot' }
    }
}

function Write-BomColorwayPivotColumnNameReport($bomGrid) {
    if (-not $bomGrid.pivotValueColumns -or $bomGrid.pivotValueColumns.Count -eq 0) { return }
    Write-Host ''
    Write-Host "  === BOM colorway grandchild pivot column names (Grid $($bomGrid.plmGridId)) ==="
    foreach ($pvc in $bomGrid.pivotValueColumns) {
        $desc = if ($pvc.businessDescription) { $pvc.businessDescription } else { '' }
        Write-Host "    [$($pvc.businessRole)] PLM '$($pvc.plmColumnName)' -> $($pvc.appColumn)  ($desc)"
    }
    Write-Host '  Confirm or override in source/dwTabImportConfig.json -> bomColorwayPivotColumnNames'
    Write-Host "    Example: `"3167`": [`"ArtworkColor`", `"ArtworkPhoto`"]"
}

function Sanitize-PlmGrandchildColumnName([string]$plmColumnName, [string[]]$reservedNames, [hashtable]$usedNames) {
    $reserved = @('Colorway', 'ParentRowId', 'RowId') + @($reservedNames)
    $name = if ([string]::IsNullOrWhiteSpace($plmColumnName)) { 'PivotValue' } else { $plmColumnName.Trim() }
    $name = ($name -replace '[^\w]', '_') -replace '_+', '_' -replace '^_|_$', ''
    if ([string]::IsNullOrWhiteSpace($name)) { $name = 'PivotValue' }
    if ($name -match '^\d') { $name = "C_$name" }
    if ($name.Length -gt 100) { $name = $name.Substring(0, 100) }

    $base = $name
    $suffix = 0
    while ($true) {
        $conflict = $false
        foreach ($r in $reserved) {
            if ($name -ieq $r) { $conflict = $true; break }
        }
        if (-not $conflict -and $usedNames.ContainsKey($name)) { $conflict = $true }
        if (-not $conflict) { break }
        $suffix++
        $name = "${base}_$suffix"
        if ($name.Length -gt 100) { $name = ($base.Substring(0, [Math]::Max(1, 100 - $suffix.ToString().Length - 1)) + "_$suffix") }
    }
    $usedNames[$name] = $true
    return $name
}

function Resolve-PivotValueFieldRow($slotFieldRows, [int]$plmMetaColumnId) {
    if (-not $slotFieldRows) { return $null }
    return @($slotFieldRows | Where-Object { $_.PlmMetaColumnId -eq $plmMetaColumnId } | Select-Object -First 1)
}

function Complete-BomColorwayGridPivotSchema($bomGrid, $slotFieldRows, $gridMetaList, $rgbColorPlmEntityId, $config) {
    if (-not $bomGrid -or -not $bomGrid.slots -or $bomGrid.slots.Count -eq 0) { return }

    $firstKeyId = [int]$bomGrid.slots[0].colorWayGridColumnId
    $valueMetaTemplate = @(Get-PivotValueMetaColumnsForDcuKey $gridMetaList $firstKeyId)
    if ($valueMetaTemplate.Count -eq 0) {
        Write-Host "  BOM colorway: no pivot value columns for grid $($bomGrid.plmGridId) DCU key $firstKeyId"
        return
    }

    $nameOverrides = Get-BomColorwayPivotColumnNameOverrides $config $bomGrid.plmGridId
    $usedNames = @{}
    $pivotValueColumns = [System.Collections.Generic.List[object]]::new()
    $roleIdx = 0
    foreach ($vm in $valueMetaTemplate) {
        $roleIdx++
        $slot1Row = Resolve-PivotValueFieldRow $slotFieldRows $vm.GridColumnId
        $businessRole = Get-PivotValueBusinessRole $vm $firstKeyId
        $appColumn = if ($nameOverrides -and $roleIdx -le $nameOverrides.Count -and $nameOverrides[$roleIdx - 1]) {
            Sanitize-PlmGrandchildColumnName $nameOverrides[$roleIdx - 1] @('Colorway', 'ParentRowId', 'RowId') $usedNames
        }
        else {
            Get-DefaultPivotValueColumnName $businessRole $vm.ColumnName $usedNames
        }
        $plmControlType = if ($vm.ColumnTypeId) { [int]$vm.ColumnTypeId } elseif ($slot1Row) { $slot1Row.PlmControlType } else { 2 }
        $plmEntityId = if ($vm.EntityId) { $vm.EntityId } elseif ($slot1Row) { $slot1Row.PlmEntityId } else { $null }
        $fkTarget = if ($slot1Row) { $slot1Row.FkTarget } else { $null }
        $sqlType = if ($slot1Row) { $slot1Row.SqlType } else { '[int]' }
        $dwDataType = if ($slot1Row) { $slot1Row.DwDataType } else { 'int' }

        [void]$pivotValueColumns.Add([pscustomobject]@{
            roleIdx             = $roleIdx
            appColumn           = $appColumn
            plmColumnName       = $vm.ColumnName
            plmMetaColumnId     = $vm.GridColumnId
            plmControlType      = $plmControlType
            plmEntityId         = $plmEntityId
            fkTarget            = $fkTarget
            sqlType             = $sqlType
            dwDataType          = $dwDataType
            businessRole        = $businessRole
            businessDescription = (Get-PivotValueBusinessDescription $businessRole)
        })
    }

    $enrichedSlots = [System.Collections.Generic.List[object]]::new()
    foreach ($slot in ($bomGrid.slots | Sort-Object slotNo)) {
        $keyId = [int]$slot.colorWayGridColumnId
        $valueMetaForSlot = @(Get-PivotValueMetaColumnsForDcuKey $gridMetaList $keyId)
        $slotValues = [System.Collections.Generic.List[object]]::new()
        $vi = 0
        foreach ($vm in $valueMetaForSlot) {
            $vi++
            $fr = Resolve-PivotValueFieldRow $slotFieldRows $vm.GridColumnId
            if (-not $fr) {
                Write-Host "  BOM colorway warn: grid $($bomGrid.plmGridId) slot $($slot.slotNo) missing DW field for PlmMetaColumnId $($vm.GridColumnId)"
                continue
            }
            $pvc = $pivotValueColumns | Where-Object { $_.roleIdx -eq $vi } | Select-Object -First 1
            if (-not $pvc) { continue }
            [void]$slotValues.Add([pscustomobject]@{
                roleIdx       = $vi
                appColumn     = $pvc.appColumn
                dwColumnName  = $fr.DwColumn
                plmMetaColumnId = $vm.GridColumnId
            })
        }
        [void]$enrichedSlots.Add([pscustomobject]@{
            slotNo               = $slot.slotNo
            colorWayGridColumnId = $keyId
            plmColumnName        = $slot.plmColumnName
            slotValues           = @($slotValues)
        })
    }

    $bomGrid | Add-Member -NotePropertyName pivotValueColumns -NotePropertyValue @($pivotValueColumns) -Force
    $bomGrid | Add-Member -NotePropertyName slots -NotePropertyValue @($enrichedSlots) -Force
    $bomGrid | Add-Member -NotePropertyName rgbColorPlmEntityId -NotePropertyValue $rgbColorPlmEntityId -Force

    $names = ($pivotValueColumns | ForEach-Object { $_.appColumn }) -join ', '
    Write-Host "  BOM colorway grid $($bomGrid.plmGridId): pivot values [$names], RGBColor EntityId=$rgbColorPlmEntityId"
}

function Get-BomColorwayGridsFromPlm([array]$Grids, [string]$TablePrefix) {
    $result = @()
    if (-not $Grids -or $Grids.Count -eq 0) { return $result }

    $gridIds = @($Grids | ForEach-Object { [int]$_.gridId } | Sort-Object -Unique)
    if ($gridIds.Count -eq 0) { return $result }
    $inList = ($gridIds -join ',')

    $pdcBlockQ = @"
SELECT BlockID FROM dbo.pdmBlock WHERE InternalCode = N'ProductDesignColor'
"@
    $pdcBlockIds = @()
    foreach ($line in (Invoke-PlmQuery $pdcBlockQ)) {
        $t = if ($null -eq $line) { '' } else { $line.Trim() }
        if ($t -and $t -ne 'NULL') { $pdcBlockIds += [int]$t }
    }
    if ($pdcBlockIds.Count -eq 0) {
        Write-Host '  BOM colorway probe: no ProductDesignColor block found.'
        return $result
    }
    $pdcIn = ($pdcBlockIds -join ',')

    $gridMetaByGrid = Get-PlmGridMetaColumnsForGrids $gridIds
    $rgbColorPlmEntityId = Get-PlmEntityIdBySysTableName 'pdmRGBColor'

    $keyColQ = @"
SELECT g.GridID, g.GridColumnID, g.ColumnName, g.ColumnOrder, g.DCUColumnBlockID
FROM dbo.pdmGridMetaColumn g
INNER JOIN dbo.pdmGrid gr ON gr.GridID = g.GridID
WHERE gr.GridType = 2
  AND g.IsDCUForProductGridRef = 1
  AND g.DCUColumnBlockID IN ($pdcIn)
  AND g.GridID IN ($inList)
ORDER BY g.GridID, g.ColumnOrder, g.GridColumnID
"@
    $keysByGrid = @{}
    foreach ($line in (Invoke-PlmQuery $keyColQ)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 5) { continue }
        $gid = [int]$parts[0].Trim()
        if (-not $keysByGrid.ContainsKey($gid)) { $keysByGrid[$gid] = @() }
        $keysByGrid[$gid] += [pscustomobject]@{
            GridColumnId = [int]$parts[1].Trim()
            ColumnName   = $parts[2].Trim()
            ColumnOrder  = [int]$parts[3].Trim()
        }
    }

    if ($keysByGrid.Count -eq 0) {
        Write-Host '  BOM colorway probe: no ProductDesignColor DCU key columns on template grids.'
        return $result
    }

    $sourceGridQ = @"
SELECT TOP 1 g.GridID, g.GridName
FROM dbo.pdmGrid g
WHERE g.GridName = N'ProductDesignColorGrid'
ORDER BY g.GridID
"@
    $sourceGridId = $null
    $sourceAppTable = 'ProductDesignColorGrid'
    foreach ($line in (Invoke-PlmQuery $sourceGridQ)) {
        $parts = $line -split '\|'
        if ($parts.Count -ge 1) { $sourceGridId = [int]$parts[0].Trim() }
    }
    $sourceGridCfg = $Grids | Where-Object { $sourceGridId -and [int]$_.gridId -eq $sourceGridId } | Select-Object -First 1
    if ($sourceGridCfg) { $sourceAppTable = $sourceGridCfg.appTable }

    foreach ($gid in ($keysByGrid.Keys | Sort-Object)) {
        $gridCfg = $Grids | Where-Object { [int]$_.gridId -eq $gid } | Select-Object -First 1
        if (-not $gridCfg) { continue }

        $blockQ = @"
SELECT TOP 1 tb.TabID, b.BlockID, bsi.SubItemID
FROM dbo.PdmTabBlock tb
INNER JOIN dbo.pdmBlock b ON b.BlockID = tb.BlockID
INNER JOIN dbo.pdmBlockSubItem bsi ON bsi.BlockID = b.BlockID AND bsi.ControlType = 6 AND bsi.GridID = $gid
ORDER BY tb.TabID, tb.OrderId
"@
        $tabId = $null
        $blockId = $null
        $gridSubItemId = $null
        foreach ($line in (Invoke-PlmQuery $blockQ)) {
            $parts = $line -split '\|'
            if ($parts.Count -lt 3) { continue }
            $tabId = [int]$parts[0].Trim()
            $blockId = [int]$parts[1].Trim()
            $gridSubItemId = [int]$parts[2].Trim()
            break
        }
        if (-not $blockId) {
            Write-Host "  BOM colorway probe: skip Grid $gid - no tab block with GridID."
            continue
        }

        $hostApp = $gridCfg.appTable
        $grandchildApp = $hostApp + 'GrandColorway'
        $slots = @()
        $slotNo = 0
        foreach ($kc in ($keysByGrid[$gid] | Sort-Object ColumnOrder)) {
            $slotNo++
            $slots += [pscustomobject]@{
                slotNo               = $slotNo
                colorWayGridColumnId = $kc.GridColumnId
                plmColumnName        = $kc.ColumnName
            }
        }

        $result += [pscustomobject]@{
            plmTabId               = $tabId
            plmGridId              = $gid
            productGridBlockId     = $blockId
            gridSubItemId          = $gridSubItemId
            hostAppTable           = $hostApp
            grandchildAppTable     = $grandchildApp
            sourceGridId           = $sourceGridId
            sourceAppTable         = $sourceAppTable
            dwGridTable            = $gridCfg.dwTable
            slots                  = $slots
            gcParentLink           = 'ParentRowId'
            gcColorwayKey          = 'Colorway'
            sourcePivotKeyColumn   = 'Color'
            rgbColorPlmEntityId    = $rgbColorPlmEntityId
            gridMetaList           = if ($gridMetaByGrid.ContainsKey($gid)) { @($gridMetaByGrid[$gid]) } else { @() }
        }
    }

    return $result
}

function Build-GrandchildColorwayTableBlock([string]$GrandchildLogicalTable, [string]$HostLogicalTable, $pivotValueColumns) {
    $valueColDefs = @()
    $alterLines = @()
    foreach ($pvc in @($pivotValueColumns)) {
        $col = $pvc.appColumn
        $sqlType = if ($pvc.sqlType) { $pvc.sqlType } else { '[int]' }
        $valueColDefs += "        [$col] $sqlType NULL,"
        $alterLines += @(
            "    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'$col')",
            "    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [$col] $sqlType NULL;'; EXEC sp_executesql @sql; END"
        )
    }
    $valueColsSql = if ($valueColDefs.Count -gt 0) { ($valueColDefs -join "`r`n") } else { '' }
    $alterPivotSql = if ($alterLines.Count -gt 0) { ($alterLines -join "`r`n") } else { '' }

    $lines = @(
        ''
        "-- Grandchild table: $GrandchildLogicalTable (BOM colorway pivot storage; links to host $HostLogicalTable via ParentRowId)"
        'SET @TableName = @TablePrefix + N''' + $GrandchildLogicalTable + ''';'
        'SET @HostTable = @TablePrefix + N''' + $HostLogicalTable + ''';'
        'SET @ParentFkName = N''FK_'' + @TableName + N''_Parent'';'
        'SET @OldRefFkName = N''FK_'' + @TableName + N''_Reference'';'
        ''
        'IF OBJECT_ID(N''dbo.'' + QUOTENAME(@TableName), N''U'') IS NULL'
        'BEGIN'
        '    SET @sql = N''CREATE TABLE dbo.'' + QUOTENAME(@TableName) + N'' ('
        '        [RowId] INT IDENTITY(1,1) NOT NULL,'
        '        [ParentRowId] INT NOT NULL,'
        '        [Colorway] INT NOT NULL,'
        $valueColsSql
        "        CONSTRAINT [PK_$GrandchildLogicalTable] PRIMARY KEY CLUSTERED ([RowId])"
        '    );'';'
        '    EXEC sp_executesql @sql;'
        'END'
        'ELSE'
        'BEGIN'
        '    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.'' + @TableName) AND name = N''ParentRowId'')'
        '    BEGIN SET @sql = N''ALTER TABLE dbo.'' + QUOTENAME(@TableName) + N'' ADD [ParentRowId] INT NOT NULL DEFAULT 0;''; EXEC sp_executesql @sql; END'
        '    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.'' + @TableName) AND name = N''Colorway'')'
        '    BEGIN SET @sql = N''ALTER TABLE dbo.'' + QUOTENAME(@TableName) + N'' ADD [Colorway] INT NOT NULL DEFAULT 0;''; EXEC sp_executesql @sql; END'
        $alterPivotSql
        '    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @OldRefFkName)'
        '    BEGIN SET @sql = N''ALTER TABLE dbo.'' + QUOTENAME(@TableName) + N'' DROP CONSTRAINT '' + QUOTENAME(@OldRefFkName) + N'';''; EXEC sp_executesql @sql; END'
        '    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N''dbo.'' + @TableName) AND name = N''ReferenceId'')'
        '    BEGIN SET @sql = N''ALTER TABLE dbo.'' + QUOTENAME(@TableName) + N'' DROP COLUMN [ReferenceId];''; EXEC sp_executesql @sql; END'
        'END'
        ''
        'IF OBJECT_ID(N''dbo.'' + QUOTENAME(@HostTable), N''U'') IS NOT NULL'
        '   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = @ParentFkName)'
        'BEGIN'
        '    SET @sql = N''ALTER TABLE dbo.'' + QUOTENAME(@TableName)'
        '        + N'' WITH CHECK ADD CONSTRAINT '' + QUOTENAME(@ParentFkName)'
        '        + N'' FOREIGN KEY ([ParentRowId]) REFERENCES dbo.'' + QUOTENAME(@HostTable) + N'' ([RowId]);'';'
        '    EXEC sp_executesql @sql;'
        'END'
        ''
    )
    return ($lines -join "`r`n")
}

function New-GrandchildPivotFieldRow($bomGrid, [string]$appColumn, $plmMetaColumnId, $fkTarget, $sqlType, $dwDataType, $plmEntityId, $plmControlType) {
    $entityId = $plmEntityId
    if ($null -eq $entityId -and $fkTarget) { $entityId = Infer-PlmEntityId $fkTarget }
    $ctrl = $plmControlType
    if ($null -eq $ctrl) { $ctrl = if ($fkTarget) { 1 } else { 2 } }
    return [pscustomobject]@{
        AppTable         = $bomGrid.grandchildAppTable
        AppColumn        = $appColumn
        DwTable          = $null
        DwColumn         = $null
        Stem             = $appColumn
        NamePart         = $appColumn
        SubItemId        = $null
        FkTarget         = $fkTarget
        SqlType          = $sqlType
        PlmTabId         = $bomGrid.plmTabId
        PlmGridSubItemId = $null
        PlmGridId        = $bomGrid.plmGridId
        PlmMetaColumnId  = $plmMetaColumnId
        FieldKind        = 'GrandchildPivot'
        DwDataType       = $dwDataType
        PlmControlType   = $ctrl
        PlmEntityId      = $entityId
    }
}

function Add-GrandchildColorwayFieldRows($allFieldRows, $bomGrid) {
    [void]$allFieldRows.Add((New-GrandchildPivotFieldRow $bomGrid 'ParentRowId' $null $null '[int]' 'int' $null 2))

    $pivotKeyMetaId = if ($bomGrid.pivotKeyPlmMetaColumnId) { $bomGrid.pivotKeyPlmMetaColumnId } else { $null }
    $rgbEntityId = if ($bomGrid.rgbColorPlmEntityId) { $bomGrid.rgbColorPlmEntityId } else { $null }
    [void]$allFieldRows.Add((New-GrandchildPivotFieldRow $bomGrid 'Colorway' $pivotKeyMetaId 'pdmRGBColor' '[int]' 'int' $rgbEntityId 1))

    foreach ($pvc in @($bomGrid.pivotValueColumns)) {
        [void]$allFieldRows.Add((New-GrandchildPivotFieldRow $bomGrid $pvc.appColumn $pvc.plmMetaColumnId $pvc.fkTarget $pvc.sqlType $pvc.dwDataType $pvc.plmEntityId $pvc.plmControlType))
    }
}

function Build-BomColorwayPivotBindings($bomGrids, [string]$Prefix) {
    $bindings = @()
    foreach ($bg in $bomGrids) {
        $hostFull = $Prefix + $bg.hostAppTable
        $gcFull = $Prefix + $bg.grandchildAppTable
        $srcFull = $Prefix + $bg.sourceAppTable
        $valueFields = @()
        foreach ($pvc in @($bg.pivotValueColumns)) {
            $valueFields += [ordered]@{ column = $pvc.appColumn; isPivotValue = $true }
        }
        $bindings += [ordered]@{
            plmTabId               = [int]$bg.plmTabId
            plmGridId              = [int]$bg.plmGridId
            productGridBlockId     = [int]$bg.productGridBlockId
            hostAppTableName       = $hostFull
            grandchildAppTableName = $gcFull
            sourceAppTableName     = $srcFull
            sourcePivotKeyColumn   = $bg.sourcePivotKeyColumn
            grandchildColumns      = [ordered]@{
                parentLink   = $bg.gcParentLink
                colorwayKey  = $bg.gcColorwayKey
                valueFields  = $valueFields
            }
            stagingHostColumnPatterns = @('Colorway_%', 'Image%')
        }
    }
    return $bindings
}

function SqlQuoteName([string]$name) {
    if ($null -eq $name) { return 'NULL' }
    return "N'$($name.Replace("'", "''"))'"
}

function SqlBracketName([string]$name) {
    if ([string]::IsNullOrWhiteSpace($name)) { return '[]' }
    return '[' + ($name.Replace(']', ']]')) + ']'
}

function Build-BomColorwayGrandchildImportSql($bomGrid, $config, $templateId) {
    if (-not $bomGrid.pivotValueColumns -or $bomGrid.pivotValueColumns.Count -eq 0) {
        throw "BOM grid $($bomGrid.plmGridId) has no pivot value columns - run Complete-BomColorwayGridPivotSchema first."
    }

    $pfx = if ($config.tablePrefixDefault) { $config.tablePrefixDefault } else { 'Plm_' }
    $plmDb = if ($config.plmDatabase) { $config.plmDatabase } else { 'PLM' }
    $dwDb = $config.dwDatabase
    $gcParent = $bomGrid.gcParentLink
    $gcColorway = $bomGrid.gcColorwayKey
    # Precompute — cannot call SqlBracketName() inside @" "@ (only $var expands; call becomes literal T-SQL)
    $gcParentBracket = SqlBracketName $gcParent
    $gcColorwayBracket = SqlBracketName $gcColorway

    $pivotCols = @($bomGrid.pivotValueColumns)
    $dwCellsColDefs = ($pivotCols | ForEach-Object { "        [$($_.appColumn)] INT NULL" }) -join ",`r`n"
    $dwCellsInsertCols = ($pivotCols | ForEach-Object { "[$($_.appColumn)]" }) -join ', '
    $dwCellsSelectCols = ($pivotCols | ForEach-Object { "u.[$($_.appColumn)]" }) -join ', '
    $gcInsertCols = ($pivotCols | ForEach-Object { '[' + $_.appColumn + ']' }) -join ', '
    $gcSelectCols = ($pivotCols | ForEach-Object { "c.[$($_.appColumn)]" }) -join ', '
    $notNullPred = ($pivotCols | ForEach-Object { "u.[$($_.appColumn)] IS NOT NULL" }) -join ' OR '

    $unionParts = [System.Collections.Generic.List[string]]::new()
    foreach ($slot in ($bomGrid.slots | Sort-Object slotNo)) {
        $selectBits = [System.Collections.Generic.List[string]]::new()
        [void]$selectBits.Add("$($slot.slotNo) AS SlotNo")
        [void]$selectBits.Add("$($slot.colorWayGridColumnId) AS ColorWayGridColumnId")
        foreach ($pvc in $pivotCols) {
            $sv = @($slot.slotValues) | Where-Object { $_.appColumn -eq $pvc.appColumn } | Select-Object -First 1
            if ($sv -and $sv.dwColumnName) {
                $dwQ = '[dw].' + (SqlBracketName $sv.dwColumnName)
                [void]$selectBits.Add("TRY_CAST($dwQ AS INT) AS [$($pvc.appColumn)]")
            }
            else {
                [void]$selectBits.Add("CAST(NULL AS INT) AS [$($pvc.appColumn)]")
            }
        }
        [void]$unionParts.Add('SELECT ' + ($selectBits -join ', '))
    }
    $unionSqlEscaped = ($unionParts -join ' UNION ALL ').Replace("'", "''")

    $slotMapInserts = [System.Collections.Generic.List[string]]::new()
    foreach ($slot in ($bomGrid.slots | Sort-Object slotNo)) {
        [void]$slotMapInserts.Add("($($slot.slotNo), $($slot.colorWayGridColumnId))")
    }

    return @"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

DECLARE @TablePrefix           NVARCHAR(32)  = N'$($pfx.Replace("'", "''"))';
DECLARE @PlmDatabase           NVARCHAR(128) = N'$($plmDb.Replace("'", "''"))';
DECLARE @DwDatabase            NVARCHAR(128) = N'$($dwDb.Replace("'", "''"))';
DECLARE @PlmTemplateId         INT           = $templateId;
DECLARE @PlmTabId              INT           = $($bomGrid.plmTabId);
DECLARE @PlmGridId             INT           = $($bomGrid.plmGridId);
DECLARE @ProductGridBlockId    INT           = $($bomGrid.productGridBlockId);
DECLARE @DwGridTable           NVARCHAR(256) = N'$($bomGrid.dwGridTable.Replace("'", "''"))';
DECLARE @HostAppTable          NVARCHAR(128) = N'$($bomGrid.hostAppTable.Replace("'", "''"))';
DECLARE @GrandchildAppTable    NVARCHAR(128) = N'$($bomGrid.grandchildAppTable.Replace("'", "''"))';
DECLARE @GcColorwayColumn      NVARCHAR(128) = N'$($gcColorway.Replace("'", "''"))';
DECLARE @GcParentLinkColumn    NVARCHAR(128) = N'$($gcParent.Replace("'", "''"))';
DECLARE @ImportMode            NVARCHAR(16)  = N'APPEND';
DECLARE @ReferenceIdList       NVARCHAR(MAX) = NULL;
DECLARE @DryRun                BIT           = 0;

DECLARE @MappingTable          NVARCHAR(128);
DECLARE @HostTable             NVARCHAR(128);
DECLARE @GrandchildTable       NVARCHAR(128);
DECLARE @sql                   NVARCHAR(MAX);
DECLARE @RowCnt                INT;
DECLARE @unionSql              NVARCHAR(MAX) = N'$unionSqlEscaped';

IF OBJECT_ID(N'tempdb..#ImportLog') IS NOT NULL DROP TABLE #ImportLog;
IF OBJECT_ID(N'tempdb..#RefFilter') IS NOT NULL DROP TABLE #RefFilter;
IF OBJECT_ID(N'tempdb..#SlotMap') IS NOT NULL DROP TABLE #SlotMap;
IF OBJECT_ID(N'tempdb..#DwBom') IS NOT NULL DROP TABLE #DwBom;
IF OBJECT_ID(N'tempdb..#HostRanked') IS NOT NULL DROP TABLE #HostRanked;
IF OBJECT_ID(N'tempdb..#DwCells') IS NOT NULL DROP TABLE #DwCells;

-- Column name [TableName] must match PlmDw_ImportFromDW.sql #ImportLog (same SSMS session).
CREATE TABLE #ImportLog (
    [Step]      NVARCHAR(128) NOT NULL,
    [TableName] NVARCHAR(256) NULL,
    [RowCount]  INT NOT NULL
);

CREATE TABLE #RefFilter ([ReferenceId] INT NOT NULL PRIMARY KEY);

CREATE TABLE #SlotMap (
    [SlotNo]                 INT NOT NULL,
    [ColorWayGridColumnId]   INT NOT NULL,
    PRIMARY KEY ([SlotNo])
);

INSERT INTO #SlotMap ([SlotNo], [ColorWayGridColumnId]) VALUES
    $($slotMapInserts -join ",`r`n    ");

SET @MappingTable    = @TablePrefix + N'FieldMapping';
SET @HostTable       = @TablePrefix + @HostAppTable;
SET @GrandchildTable = @TablePrefix + @GrandchildAppTable;

IF DB_ID(@PlmDatabase) IS NULL BEGIN RAISERROR(N'PLM database not found: %s', 16, 1, @PlmDatabase); RETURN; END
IF DB_ID(@DwDatabase) IS NULL BEGIN RAISERROR(N'DW database not found: %s', 16, 1, @DwDatabase); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@HostTable), N'U') IS NULL BEGIN RAISERROR(N'Host table dbo.%s missing.', 16, 1, @HostTable); RETURN; END
IF OBJECT_ID(N'dbo.' + QUOTENAME(@GrandchildTable), N'U') IS NULL BEGIN RAISERROR(N'Grandchild table dbo.%s missing.', 16, 1, @GrandchildTable); RETURN; END
IF OBJECT_ID(QUOTENAME(@PlmDatabase) + N'.dbo.pdmStyleColorWayMapping', N'U') IS NULL BEGIN RAISERROR(N'PLM table pdmStyleColorWayMapping not found.', 16, 1); RETURN; END

IF @ReferenceIdList IS NOT NULL AND LTRIM(RTRIM(@ReferenceIdList)) <> N''
BEGIN
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT TRY_CAST(LTRIM(RTRIM([value])) AS INT)
    FROM STRING_SPLIT(@ReferenceIdList, N',')
    WHERE TRY_CAST(LTRIM(RTRIM([value])) AS INT) IS NOT NULL;
END
ELSE
BEGIN
    SET @sql = N'
    INSERT INTO #RefFilter ([ReferenceId])
    SELECT DISTINCT pt.[ProductReferenceID]
    FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmProductTemplate] AS pt
    WHERE pt.[TemplateID] = @tid AND pt.[ProductReferenceID] IS NOT NULL;';
    EXEC sp_executesql @sql, N'@tid int', @tid = @PlmTemplateId;
END

SELECT @RowCnt = COUNT(*) FROM #RefFilter;
IF @RowCnt = 0 BEGIN RAISERROR(N'No references in import scope.', 16, 1); RETURN; END
PRINT N'BOM Colorway grandchild import scope: ' + CAST(@RowCnt AS NVARCHAR(20)) + N' reference(s). Mode=' + @ImportMode;
INSERT INTO #ImportLog VALUES (N'SLOT_MAP', @DwGridTable, (SELECT COUNT(*) FROM #SlotMap));

DECLARE @CntDeleteGc INT = 0, @CntDwBom INT = 0, @CntHost INT = 0, @CntDwCells INT = 0, @CntInsertGc INT = 0, @CntSkipMap INT = 0, @CntSkipHost INT = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    IF @ImportMode = N'REPLACE'
    BEGIN
        SET @sql = N'
        DELETE gc FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        INNER JOIN dbo.' + QUOTENAME(@HostTable) + N' AS h ON h.[RowId] = gc.' + QUOTENAME(@GcParentLinkColumn) + N'
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId];';
        IF @DryRun = 0 EXEC sp_executesql @sql;
        SET @CntDeleteGc = CASE WHEN @DryRun = 0 THEN @@ROWCOUNT ELSE 0 END;
    END

    CREATE TABLE #DwBom (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [Sort] INT NULL, [HostMatchRn] INT NOT NULL,
        PRIMARY KEY ([ProductReferenceID], [RowID])
    );

    SET @sql = N'
    ;WITH src AS (
        SELECT dw.[ProductReferenceID], dw.[RowID], dw.[Sort],
            ROW_NUMBER() OVER (PARTITION BY dw.[ProductReferenceID] ORDER BY ISNULL(dw.[Sort], 2147483647), dw.[RowID]) AS HostMatchRn
        FROM ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        INNER JOIN #RefFilter rf ON rf.[ReferenceId] = dw.[ProductReferenceID]
    )
    INSERT INTO #DwBom SELECT [ProductReferenceID], [RowID], [Sort], [HostMatchRn] FROM src;';
    EXEC sp_executesql @sql;
    SET @CntDwBom = @@ROWCOUNT;

    CREATE TABLE #HostRanked ([RowId] INT NOT NULL PRIMARY KEY, [ReferenceId] INT NOT NULL, [HostMatchRn] INT NOT NULL);
    SET @sql = N'
    ;WITH h AS (
        SELECT h.[RowId], h.[ReferenceId],
            ROW_NUMBER() OVER (PARTITION BY h.[ReferenceId] ORDER BY ISNULL(h.[Sort], 2147483647), h.[RowId]) AS HostMatchRn
        FROM dbo.' + QUOTENAME(@HostTable) + N' AS h INNER JOIN #RefFilter rf ON rf.[ReferenceId] = h.[ReferenceId]
    )
    INSERT INTO #HostRanked SELECT [RowId], [ReferenceId], [HostMatchRn] FROM h;';
    EXEC sp_executesql @sql;
    SET @CntHost = @@ROWCOUNT;

    CREATE TABLE #DwCells (
        [ProductReferenceID] INT NOT NULL, [RowID] INT NOT NULL, [HostMatchRn] INT NOT NULL,
        [SlotNo] INT NOT NULL, [ColorWayGridColumnId] INT NOT NULL,
$dwCellsColDefs
    );

    SET @sql = N'
    INSERT INTO #DwCells ([ProductReferenceID], [RowID], [HostMatchRn], [SlotNo], [ColorWayGridColumnId], $dwCellsInsertCols)
    SELECT b.[ProductReferenceID], b.[RowID], b.[HostMatchRn], u.[SlotNo], u.[ColorWayGridColumnId], $dwCellsSelectCols
    FROM #DwBom AS b
    INNER JOIN ' + QUOTENAME(@DwDatabase) + N'.dbo.' + QUOTENAME(@DwGridTable) + N' AS dw
        ON dw.[ProductReferenceID] = b.[ProductReferenceID] AND dw.[RowID] = b.[RowID]
    CROSS APPLY (' + @unionSql + N') AS u
    WHERE $notNullPred;';
    EXEC sp_executesql @sql;
    SET @CntDwCells = @@ROWCOUNT;

    SET @sql = N'
    SELECT @cnt = COUNT(*)
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntInsertGc OUTPUT;

    SET @sql = N'
    INSERT INTO dbo.' + QUOTENAME(@GrandchildTable) + N' (
        $gcParentBracket,
        $gcColorwayBracket,
        $gcInsertCols
    )
    SELECT hr.[RowId], m.[StyleColorID], $gcSelectCols
    FROM #DwCells AS c
    INNER JOIN ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        ON m.[ProductReferenceID] = c.[ProductReferenceID]
       AND m.[ProductGridBlockID] = @blockId
       AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
       AND m.[StyleColorID] IS NOT NULL
    INNER JOIN #HostRanked AS hr ON hr.[ReferenceId] = c.[ProductReferenceID] AND hr.[HostMatchRn] = c.[HostMatchRn]
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.' + QUOTENAME(@GrandchildTable) + N' AS gc
        WHERE gc.' + QUOTENAME(@GcParentLinkColumn) + N' = hr.[RowId]
          AND gc.' + QUOTENAME(@GcColorwayColumn) + N' = m.[StyleColorID]
    );';

    IF @DryRun = 0 EXEC sp_executesql @sql, N'@blockId int', @blockId = @ProductGridBlockId;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwCells AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM ' + QUOTENAME(@PlmDatabase) + N'.dbo.[pdmStyleColorWayMapping] AS m
        WHERE m.[ProductReferenceID] = c.[ProductReferenceID]
          AND m.[ProductGridBlockID] = @blockId
          AND m.[ColorWayGridColumnID] = c.[ColorWayGridColumnId]
          AND m.[StyleColorID] IS NOT NULL
    );';
    EXEC sp_executesql @sql, N'@blockId int, @cnt int OUTPUT', @blockId = @ProductGridBlockId, @cnt = @CntSkipMap OUTPUT;

    SET @sql = N'
    SELECT @cnt = COUNT(*) FROM #DwBom AS b
    WHERE NOT EXISTS (
        SELECT 1 FROM #HostRanked AS hr
        WHERE hr.[ReferenceId] = b.[ProductReferenceID] AND hr.[HostMatchRn] = b.[HostMatchRn]
    );';
    EXEC sp_executesql @sql, N'@cnt int OUTPUT', @cnt = @CntSkipHost OUTPUT;

    IF @DryRun = 1 ROLLBACK TRANSACTION;
    ELSE COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrLine INT = ERROR_LINE();
    RAISERROR(N'PlmDw_ImportBomColorwayGrandchild failed (line %d): %s', 16, 1, @ErrLine, @ErrMsg);
    RETURN;
END CATCH;

IF @ImportMode = N'REPLACE'
    INSERT INTO #ImportLog VALUES (N'DELETE_GRANDCHILD', @GrandchildTable, @CntDeleteGc);
INSERT INTO #ImportLog VALUES (N'DW_BOM_ROWS', @DwGridTable, @CntDwBom);
INSERT INTO #ImportLog VALUES (N'HOST_ROWS', @HostTable, @CntHost);
INSERT INTO #ImportLog VALUES (N'DW_CELLS', N'non-empty slots', @CntDwCells);
INSERT INTO #ImportLog VALUES (N'INSERT_GRANDCHILD', @GrandchildTable, @CntInsertGc);
INSERT INTO #ImportLog VALUES (N'SKIP_NO_MAPPING', N'cells with value but no pdmStyleColorWayMapping', @CntSkipMap);
INSERT INTO #ImportLog VALUES (N'SKIP_NO_HOST', N'DW BOM rows without host match', @CntSkipHost);

EXEC (N'SELECT [Step], [TableName], [RowCount] FROM #ImportLog ORDER BY [Step], [TableName];');
PRINT N'PlmDw_ImportBomColorwayGrandchild completed.';
"@
}

function Patch-CleanupBomColorwayTemplate([string]$TemplateSql, $bomGrid, $config) {
    $sql = $TemplateSql
    $pfx = if ($config.tablePrefixDefault) { $config.tablePrefixDefault } else { 'Plm_' }
    $sql = $sql.Replace('DECLARE @TablePrefix     NVARCHAR(32)  = N''Plm_'';', ('DECLARE @TablePrefix     NVARCHAR(32)  = N''' + $pfx + ''';'))
    $sql = $sql.Replace('DECLARE @HostAppTable    NVARCHAR(128) = NULL;', ('DECLARE @HostAppTable    NVARCHAR(128) = N''' + $bomGrid.hostAppTable + ''';'))
    $sql = $sql.Replace('DECLARE @PlmTabId        INT           = NULL;', ('DECLARE @PlmTabId        INT           = ' + $bomGrid.plmTabId + ';'))
    return $sql
}

function Generate-BomColorwaySqlFiles($bomGrids, $config, $templateId, [string]$OutDir) {
    if (-not $bomGrids -or $bomGrids.Count -eq 0) { return }

    $cleanupTpl = Join-Path $PSScriptRoot 'PlmDw_CleanupBomColorwayStaging.sql'
    if (-not (Test-Path $cleanupTpl)) { throw "Missing $cleanupTpl" }
    $cleanupBody = Get-Content $cleanupTpl -Raw

    $importParts = New-Object System.Collections.Generic.List[string]
    $importParts.Add('')
    $importParts.Add('-- =============================================================================')
    $importParts.Add("-- 5_PlmDw_ImportBomColorwayGrandchild.sql - Template $templateId")
    $importParts.Add('-- Pivot value columns from PLM pdmGridMetaColumn (DCU slot + DcucolumnId children).')
    $importParts.Add('-- =============================================================================')
    $importParts.Add('')

    $cleanupParts = New-Object System.Collections.Generic.List[string]
    $cleanupParts.Add('')
    $cleanupParts.Add('-- =============================================================================')
    $cleanupParts.Add("-- 6_PlmDw_CleanupBomColorwayStaging.sql - Template $templateId")
    $cleanupParts.Add('-- Optional legacy cleanup: drops host Colorway_N / ImageN if present from older imports.')
    $cleanupParts.Add('-- =============================================================================')
    $cleanupParts.Add('')

    $idx = 0
    foreach ($bg in $bomGrids) {
        $idx++
        $section = Build-BomColorwayGrandchildImportSql $bg $config $templateId
        if ($idx -gt 1) { [void]$importParts.Add("GO`r`n") }
        [void]$importParts.Add("-- ----- BOM grid $($bg.plmGridId) / block $($bg.productGridBlockId) -> $($bg.grandchildAppTable) -----`r`n")
        [void]$importParts.Add($section)

        $csec = Patch-CleanupBomColorwayTemplate $cleanupBody $bg $config
        $csec = $csec -replace '(?m)^-- =+.*\r?\n-- PLM BOM Colorway staging.*\r?\n(?:--.*\r?\n)*-- =+\r?\n', ''
        if ($idx -gt 1) { [void]$cleanupParts.Add("GO`r`n") }
        [void]$cleanupParts.Add("-- ----- Host $($bg.hostAppTable) -----`r`n")
        [void]$cleanupParts.Add($csec)
    }

    $importPath = Join-Path $OutDir '5_PlmDw_ImportBomColorwayGrandchild.sql'
    $cleanupPath = Join-Path $OutDir '6_PlmDw_CleanupBomColorwayStaging.sql'
    [System.IO.File]::WriteAllText($importPath, ($importParts -join "`r`n"), (New-Object System.Text.UTF8Encoding $false))
    [System.IO.File]::WriteAllText($cleanupPath, ($cleanupParts -join "`r`n"), (New-Object System.Text.UTF8Encoding $false))
    Write-Host ('Generated: ' + $importPath + ' (' + $bomGrids.Count + ' BOM grid(s))')
    Write-Host "Generated: $cleanupPath"
}
