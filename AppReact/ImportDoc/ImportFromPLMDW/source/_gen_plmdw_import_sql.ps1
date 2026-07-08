$ErrorActionPreference = 'Stop'
$rootDir = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $PSScriptRoot 'dwTabImportConfig.json'
if (-not (Test-Path $configPath)) {
    throw "Missing $configPath - copy dwTabImportConfig.example.json and fill from Phase B (see PROMPT.md)."
}
$config = Get-Content $configPath -Raw | ConvertFrom-Json

. (Join-Path $PSScriptRoot '_gen_plmdw_bom_colorway.ps1')

$templateId = $null
if ($null -ne $config.plmTemplateId -and [int]$config.plmTemplateId -gt 0) {
    $templateId = [int]$config.plmTemplateId
}
elseif ($config.plmTemplate -and $null -ne $config.plmTemplate.templateId -and [int]$config.plmTemplate.templateId -gt 0) {
    $templateId = [int]$config.plmTemplate.templateId
}
if (-not $templateId) {
    throw 'dwTabImportConfig.json must set plmTemplateId (or plmTemplate.templateId) for output folder output/{templateId}/.'
}

$outDir = Join-Path (Join-Path $rootDir 'output') ([string]$templateId)
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$SqlServer = $config.sqlServer
$DwDatabase = $config.dwDatabase
$PlmDatabase = if ($config.plmDatabase) { $config.plmDatabase } else { 'PLM' }
$refScope = if ($config.referenceScope) { $config.referenceScope } else { $config.referenceCode }

$TabSystemColumns = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('TabID', 'ProductReferenceID'),
    [StringComparer]::OrdinalIgnoreCase
)
$GridSystemColumns = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('ProductReferenceID', 'BlockID', 'GridID', 'RowID', 'RowValueGUID', 'Sort'),
    [StringComparer]::OrdinalIgnoreCase
)

function Invoke-SqlQuery([string]$Database, [string]$Query) {
    $tmp = [System.IO.Path]::GetTempFileName() + '.sql'
    $out = [System.IO.Path]::GetTempFileName() + '.txt'
    try {
        Set-Content -Path $tmp -Value $Query -Encoding UTF8
        $sqlUser = if ($config.sqlUser) { $config.sqlUser } else { $env:PLM_DW_SQL_USER }
        $sqlPassword = if ($config.sqlPassword) { $config.sqlPassword } else { $env:PLM_DW_SQL_PASSWORD }
        $args = @('-S', $SqlServer, '-d', $Database, '-i', $tmp, '-o', $out, '-W', '-s', '|', '-h', '-1')
        if ($sqlUser -and $sqlPassword) {
            $args = @('-S', $SqlServer, '-d', $Database, '-U', $sqlUser, '-P', $sqlPassword) + $args[4..($args.Length - 1)]
        }
        else {
            $args = @('-S', $SqlServer, '-d', $Database, '-E') + $args[4..($args.Length - 1)]
        }
        $p = Start-Process -FilePath 'sqlcmd' -ArgumentList $args -Wait -PassThru -NoNewWindow
        if ($p.ExitCode -ne 0) { throw "sqlcmd failed ($($p.ExitCode)) on $Database`: $Query" }
        $lines = Get-Content $out | Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' }
        return ,$lines
    }
    finally {
        Remove-Item $tmp, $out -ErrorAction SilentlyContinue
    }
}

function Invoke-DwQuery([string]$Query) {
    return Invoke-SqlQuery -Database $DwDatabase -Query $Query
}

function Invoke-PlmQuery([string]$Query) {
    return Invoke-SqlQuery -Database $PlmDatabase -Query $Query
}

function Get-PlmSubItemExtraInfoMap([int[]]$TabIds) {
    $map = @{}
    if (-not $TabIds -or $TabIds.Count -eq 0) { return $map }
    $inList = ($TabIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT ei.TabID, ei.SubItemID, ei.AliasName, ei.Visible
FROM dbo.pdmTabBlockSubItemExtraInfo ei
WHERE ei.TabID IN ($inList)
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 4) { continue }
        $tabId = [int]$parts[0].Trim()
        $subItemId = [int]$parts[1].Trim()
        $alias = $parts[2].Trim()
        $visibleRaw = $parts[3].Trim()
        $visible = $false
        if ($visibleRaw -ne '' -and $visibleRaw -ne 'NULL') {
            try { $visible = ([int]$visibleRaw -eq 1) } catch { $visible = $false }
        }
        $key = "$tabId|$subItemId"
        $map[$key] = [pscustomobject]@{
            AliasName = if ([string]::IsNullOrWhiteSpace($alias)) { $null } else { $alias }
            Visible   = $visible
        }
    }
    return $map
}

function Parse-SqlIntOrNull([string]$Raw) {
    $t = if ($null -eq $Raw) { '' } else { $Raw.Trim() }
    if ([string]::IsNullOrWhiteSpace($t) -or $t -eq 'NULL') { return $null }
    return [int]$t
}

function Get-PlmSubItemMetadataMap([int[]]$TabIds) {
    $map = @{}
    if (-not $TabIds -or $TabIds.Count -eq 0) { return $map }
    $inList = ($TabIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT tb.TabID, bsi.SubItemID, bsi.ControlType, bsi.EntityId, bsi.Nbdecimal
FROM dbo.PdmTabBlock tb
INNER JOIN dbo.pdmBlockSubItem bsi ON bsi.BlockID = tb.BlockID
WHERE tb.TabID IN ($inList)
ORDER BY tb.TabID, tb.OrderId, bsi.SortOrder, bsi.SubItemID
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 5) { continue }
        $tabId = [int]$parts[0].Trim()
        $subItemId = [int]$parts[1].Trim()
        $key = "$tabId|$subItemId"
        if ($map.ContainsKey($key)) { continue }
        $map[$key] = [pscustomobject]@{
            ControlType = [int]$parts[2].Trim()
            EntityId    = Parse-SqlIntOrNull $parts[3]
            Nbdecimal   = Parse-SqlIntOrNull $parts[4]
        }
    }
    return $map
}

function Get-PlmGridColumnMetadataMap([int[]]$GridIds) {
    $map = @{}
    if (-not $GridIds -or $GridIds.Count -eq 0) { return $map }
    $inList = ($GridIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT gmc.GridID, gmc.GridColumnID, gmc.ColumnTypeId, gmc.EntityId, gmc.Nbdecimal, gmc.ColumnOrder
FROM dbo.pdmGridMetaColumn gmc
WHERE gmc.GridID IN ($inList)
ORDER BY gmc.GridID, gmc.ColumnOrder, gmc.GridColumnID
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 5) { continue }
        $gridId = [int]$parts[0].Trim()
        $gridColumnId = [int]$parts[1].Trim()
        $key = "$gridId|$gridColumnId"
        if ($map.ContainsKey($key)) { continue }
        $map[$key] = [pscustomobject]@{
            ControlType = [int]$parts[2].Trim()
            EntityId    = Parse-SqlIntOrNull $parts[3]
            Nbdecimal   = Parse-SqlIntOrNull $parts[4]
            ColumnOrder = if ($parts.Count -ge 6) { Parse-SqlIntOrNull $parts[5] } else { $null }
        }
    }
    return $map
}

function Get-PlmTabGridColumnVisibleMap([int[]]$TabIds) {
    # Layer for GRID columns: pdmTabGridMetaColumn (TabID + GridColumnID + Visible). Key: "tabId|gridColumnId".
    $map = @{}
    if (-not $TabIds -or $TabIds.Count -eq 0) { return $map }
    $inList = ($TabIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT tgc.TabID, tgc.GridColumnID, tgc.Visible, tgc.AliasName
FROM dbo.pdmTabGridMetaColumn tgc
WHERE tgc.TabID IN ($inList)
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 3) { continue }
        $tabId = [int]$parts[0].Trim()
        $gridColumnId = [int]$parts[1].Trim()
        $visibleRaw = $parts[2].Trim()
        $alias = if ($parts.Count -ge 4) { $parts[3].Trim() } else { '' }
        $visible = $false
        if ($visibleRaw -ne '' -and $visibleRaw -ne 'NULL') {
            try { $visible = ([int]$visibleRaw -eq 1) } catch { $visible = $false }
        }
        $map["$tabId|$gridColumnId"] = [pscustomobject]@{
            Visible   = $visible
            AliasName = if ([string]::IsNullOrWhiteSpace($alias)) { $null } else { $alias }
        }
    }
    return $map
}

function Get-PlmTabsWithGridSubItem([int[]]$TabIds) {
    # CHILD UNIT detection: a tab whose blocks contain a sub-item with ControlType = 6 (Grid)
    # is a 1:many detail unit. Its APP tab table is generated as a child unit (own identity PK).
    $set = [System.Collections.Generic.HashSet[int]]::new()
    if (-not $TabIds -or $TabIds.Count -eq 0) { return $set }
    $inList = ($TabIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT DISTINCT tb.TabID
FROM dbo.PdmTabBlock tb
INNER JOIN dbo.pdmBlockSubItem bsi ON bsi.BlockID = tb.BlockID
WHERE tb.TabID IN ($inList) AND bsi.ControlType = 6
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $t = if ($null -eq $line) { '' } else { $line.Trim() }
        if ($t -eq '' -or $t -eq 'NULL') { continue }
        $parsed = 0
        if ([int]::TryParse($t, [ref]$parsed)) { [void]$set.Add($parsed) }
    }
    return $set
}

# Resolve a tab's unit kind. RULE: a tab's WIDE table (regular sub-items) is ALWAYS a 'sibling'
# (PK = [ReferenceId], 1:1 with root). Grid sub-items become their own grid tables (RowId identity
# PK) via $config.grids - hosting a Grid sub-item does NOT make the tab wide table a child.
# 'unitType' in config is an OPTIONAL override: set 'child' to force an identity-PK child tab table.
function Resolve-TabUnitKind($tab, $childTabIds) {
    $ut = if ($tab.unitType) { ([string]$tab.unitType).Trim().ToLowerInvariant() } else { '' }
    if ($ut -eq 'child') { return 'child' }
    return 'sibling'
}

function Get-PlmTabLayoutSubItemSet([int[]]$TabIds) {
    # Layer 2 (Tab Design): sub-items actually placed on the tab layout. Set of "tabId|subItemId".
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    if (-not $TabIds -or $TabIds.Count -eq 0) { return $set }
    $inList = ($TabIds | Sort-Object -Unique | ForEach-Object { [string]$_ }) -join ','
    $q = @"
SELECT DISTINCT l.TabID, ls.SubItemID
FROM dbo.pdmTabLayout l
INNER JOIN dbo.pdmTabLayoutItem li ON li.LayoutID = l.LayoutID
INNER JOIN dbo.pdmTabLayoutSubitem ls ON ls.LayoutItemID = li.LayoutItemID
WHERE l.TabID IN ($inList)
"@
    foreach ($line in (Invoke-PlmQuery $q)) {
        $parts = $line -split '\|'
        if ($parts.Count -lt 2) { continue }
        $tabId = $parts[0].Trim()
        $subItemId = $parts[1].Trim()
        if ($tabId -eq '' -or $subItemId -eq '' -or $tabId -eq 'NULL' -or $subItemId -eq 'NULL') { continue }
        [void]$set.Add("$tabId|$subItemId")
    }
    return $set
}

function Apply-PlmFieldMetadata($fieldRow, $subItemMetaMap, $gridColMetaMap) {
    $gridBackedKind = $fieldRow.FieldKind -eq 'GridColumn' -or $fieldRow.FieldKind -eq 'GrandchildPivot' -or $fieldRow.FieldKind -eq 'BomColorwayDwSlot'
    if ($gridBackedKind -and $null -ne $fieldRow.PlmGridId) {
        $gridColumnId = $null
        if ($null -ne $fieldRow.PlmMetaColumnId) { $gridColumnId = [int]$fieldRow.PlmMetaColumnId }
        elseif ($null -ne $fieldRow.SubItemId) { $gridColumnId = [int]$fieldRow.SubItemId }
        if ($gridColumnId) {
            $key = "$([int]$fieldRow.PlmGridId)|$gridColumnId"
            if ($gridColMetaMap.ContainsKey($key)) {
                $m = $gridColMetaMap[$key]
                $fieldRow.PlmControlType = [int]$m.ControlType
                $fieldRow.PlmEntityId = $m.EntityId
                return
            }
        }
    }
    elseif ($fieldRow.PlmTabId -and $null -ne $fieldRow.SubItemId) {
        $key = "$([int]$fieldRow.PlmTabId)|$([int]$fieldRow.SubItemId)"
        if ($subItemMetaMap.ContainsKey($key)) {
            $m = $subItemMetaMap[$key]
            $fieldRow.PlmControlType = [int]$m.ControlType
            $fieldRow.PlmEntityId = $m.EntityId
        }
    }
}

function Resolve-FieldExtraInfo($fieldRow, $extraInfoMap, $subItemMetaMap, $gridColMetaMap, $tabGridVisibleMap, $layoutSubItemSet) {
    $tabId = if ($fieldRow.PlmTabId) { [int]$fieldRow.PlmTabId } else { $null }
    $subItemId = $null
    if ($null -ne $fieldRow.SubItemId) { $subItemId = [int]$fieldRow.SubItemId }
    elseif ($null -ne $fieldRow.PlmSubItemId) { $subItemId = [int]$fieldRow.PlmSubItemId }
    elseif ($null -ne $fieldRow.PlmMetaColumnId) { $subItemId = [int]$fieldRow.PlmMetaColumnId }

    $displayLabel = $fieldRow.AppColumn
    $isVisible = $false
    if ($fieldRow.FieldKind -eq 'GridColumn' -and $null -ne $fieldRow.PlmGridId -and $subItemId) {
        # GRID column visibility: pdmTabGridMetaColumn.Visible (TabID + GridColumnID). NOT pdmTabBlockSubItemExtraInfo.
        $gridKey = "$([int]$fieldRow.PlmGridId)|$subItemId"
        if ($gridColMetaMap.ContainsKey($gridKey) -and $tabId) {
            $tgKey = "$tabId|$subItemId"
            if ($tabGridVisibleMap.ContainsKey($tgKey)) {
                $tg = $tabGridVisibleMap[$tgKey]
                if ($tg.AliasName) { $displayLabel = $tg.AliasName }
                if ($tg.Visible) { $isVisible = $true }
            }
        }
    }
    elseif ($tabId -and $subItemId) {
        # TAB field visibility: Layer 1 = pdmTabBlockSubItemExtraInfo.Visible=1; Layer 2 = placed in Tab Design (pdmTabLayoutSubitem).
        $siKey = "$tabId|$subItemId"
        if ($subItemMetaMap.ContainsKey($siKey) -and $extraInfoMap.ContainsKey($siKey)) {
            $ei = $extraInfoMap[$siKey]
            if ($ei.AliasName) { $displayLabel = $ei.AliasName }
            if ($ei.Visible -and $layoutSubItemSet.Contains($siKey)) { $isVisible = $true }
        }
    }
    return [pscustomobject]@{ DisplayLabel = $displayLabel; IsVisible = $isVisible }
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

# PS 5.1 ConvertTo-Json unwraps single-element arrays; unary comma preserves JSON array.
function Get-JsonArrayForSerialize([array]$Items) {
    if (-not $Items -or $Items.Count -eq 0) { return @() }
    if ($Items.Count -eq 1) { return ,$Items[0] }
    return @($Items)
}

function Fix-BomColorwayBindingsJsonArray([string]$json) {
    $marker = '"bomColorwayPivotBindings"'
    $idx = $json.IndexOf($marker)
    if ($idx -lt 0) { return $json }
    $colonIdx = $json.IndexOf(':', $idx + $marker.Length)
    if ($colonIdx -lt 0) { return $json }
    $pos = $colonIdx + 1
    while ($pos -lt $json.Length -and [char]::IsWhiteSpace($json[$pos])) { $pos++ }
    if ($pos -ge $json.Length) { return $json }
    if ($json[$pos] -eq '[') { return $json }
    if ($json[$pos] -ne '{') { return $json }

    $json = $json.Insert($pos, '[')
    $depth = 0
    for ($i = $pos + 1; $i -lt $json.Length; $i++) {
        $ch = $json[$i]
        if ($ch -eq '{') { $depth++ }
        elseif ($ch -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $json.Insert($i + 1, ']')
            }
        }
    }
    return $json
}

function Build-BlueprintFromConfig($config, $allFieldRows, $extraInfoMap, $subItemMetaMap, $gridColMetaMap, $tabGridVisibleMap, $layoutSubItemSet, $childTabIds, $bomColorwayPivotBindings) {
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
        $childUnits = [System.Collections.Generic.List[object]]::new()
        $isChildTab = ((Resolve-TabUnitKind $tab $childTabIds) -eq 'child')

        if ($isChildTab) {
            # Child unit: 1:many under root. Own table has its own identity PK; it is NOT a sibling.
            $childUnits.Add([ordered]@{
                appTableName = $prefix + $tab.appTable
                attachToRoot = $true
            })
        }
        else {
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
        }

        $transactions += [ordered]@{
            plmTabId         = [int]$tab.tabId
            plmTabName       = $tabName
            integrationId    = "Tab_$($tab.tabId)"
            transactionName  = $tabName
            importStatus     = $importStatus
            plmTabSort       = if ($null -ne $tab.tabSort) { [int]$tab.tabSort } else { $null }
            isTemplateHeaderTab = if ($null -ne $tab.isTemplateHeaderTab) { [bool]$tab.isTemplateHeaderTab } else { $null }
            unitStructure    = [ordered]@{
                mode          = if ($isChildTab) { 'RootPlusChild' } else { 'RootPlusMasterSibling' }
                rootTableName = $prefix + $rootSuffix
                siblingUnits  = @($siblingUnits.ToArray())
                childUnits    = @($childUnits.ToArray())
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
        if ($r.FieldKind -eq 'BomColorwaySlot') { continue }
        if ($r.FieldKind -eq 'BomColorwayDwSlot') { continue }
        if ($r.FieldKind -eq 'GrandchildPivot') { continue }
        $appTableFull = if ($r.AppTable -eq $rootSuffix) { $prefix + $rootSuffix } else { $prefix + $r.AppTable }
        if (-not $orderByTable.ContainsKey($appTableFull)) { $orderByTable[$appTableFull] = 0 }
        $orderByTable[$appTableFull]++
        $plmCtrl = if ($null -ne $r.PlmControlType) { [int]$r.PlmControlType } else { Infer-PlmControlType ([pscustomobject]@{ FkTarget = $r.FkTarget }) $r.DwDataType }
        $entityId = if ($null -ne $r.PlmEntityId) { $r.PlmEntityId } else { Infer-PlmEntityId $r.FkTarget }
        $tabIds = @()
        if ($r.PlmTabId) { $tabIds = @([int]$r.PlmTabId) }
        $extra = Resolve-FieldExtraInfo $r $extraInfoMap $subItemMetaMap $gridColMetaMap $tabGridVisibleMap $layoutSubItemSet
        $fieldEntry = [ordered]@{
            appTableName   = $appTableFull
            appColumnName  = $r.AppColumn
            plmTabIds      = $tabIds
            plmControlType = $plmCtrl
            plmEntityId    = $entityId
            displayLabel   = $extra.DisplayLabel
            displayOrder   = $orderByTable[$appTableFull]
            includeInSearch = $false
            isVisible      = [bool]$extra.IsVisible
        }
        if ($entityId) {
            $fieldEntry.entityIntegrationId = [string]$entityId
        }
        $blueprintFields += $fieldEntry
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
            configFile    = "source/dwTabImportConfig.json"
            outputFolder  = "output/$templateId"
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
        bomColorwayPivotBindings = Get-JsonArrayForSerialize @(if ($bomColorwayPivotBindings) { $bomColorwayPivotBindings } else { @() })
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

function Get-DwStringLength($col) {
    if (-not $col.CharMaxLen -or $col.CharMaxLen -eq 'NULL') { return 255 }
    $parsed = 0
    if (-not [int]::TryParse($col.CharMaxLen, [ref]$parsed)) { return 255 }
    if ($parsed -lt 0) { return -1 }
    if ($parsed -eq 0) { return 255 }
    return $parsed
}

function Get-AppStringSqlType($col) {
    $len = Get-DwStringLength $col
    if ($len -lt 0 -or $len -gt 4000) { return '[nvarchar](max)' }
    return "[nvarchar]($len)"
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
        'nvarchar' { return Get-AppStringSqlType $col }
        'varchar' { return Get-AppStringSqlType $col }
        'nchar' { return Get-AppStringSqlType $col }
        'char' { return Get-AppStringSqlType $col }
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

function Build-FieldRows($dwCols, [string]$DwTable, $TabId, [string]$AppTable, [string]$FieldKind, $gridSubItemId, $gridId) {
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
            PlmTabId         = $TabId
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

function Build-CreateTableBlock([string]$LogicalTable, $fieldRows, [string]$UnitKind = 'sibling') {
    # $UnitKind:
    #   'sibling' -> 1:1 with root; PK = [ReferenceId] (NOT an identity, value comes from import).
    #   'child'   -> 1:many under root; PK = [{LogicalTable}Id] INT IDENTITY (DB-filled),
    #                [ReferenceId] is a plain FK column (value imported, links to parent).
    #   'grid'    -> 1:many under root; PK = [RowId] INT IDENTITY, plus [ReferenceId] FK + [Sort].
    $colDefs = New-Object System.Collections.Generic.List[string]
    $pk = $null
    switch ($UnitKind) {
        'grid' {
            [void]$colDefs.Add('[RowId] INT IDENTITY(1,1) NOT NULL')
            [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
            [void]$colDefs.Add('[Sort] INT NULL')
            $pk = 'RowId'
        }
        'child' {
            $pk = "${LogicalTable}Id"
            [void]$colDefs.Add("[$pk] INT IDENTITY(1,1) NOT NULL")
            [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
        }
        default {
            [void]$colDefs.Add('[ReferenceId] INT NOT NULL')
            $pk = 'ReferenceId'
        }
    }
    foreach ($r in $fieldRows) {
        [void]$colDefs.Add("[$($r.AppColumn)] $($r.SqlType) NULL")
    }
    [void]$colDefs.Add("CONSTRAINT [PK_$LogicalTable] PRIMARY KEY CLUSTERED ([$pk])")
    $innerCols = ($colDefs -join ', ')
    $alterLines = New-Object System.Collections.Generic.List[string]
    foreach ($r in $fieldRows) {
        # Shared physical tables (e.g. 3351 + 3360 APPEND): add missing columns, then widen nvarchar if needed.
        [void]$alterLines.Add(@"
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@TableName)) AND name = N'$($r.AppColumn)')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD [$($r.AppColumn)] $($r.SqlType) NULL;'; EXEC sp_executesql @sql; END
"@.TrimEnd())
        if ($r.SqlType -match '^\[nvarchar\]') {
            [void]$alterLines.Add("    SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN [$($r.AppColumn)] $($r.SqlType) NULL;';`r`n    EXEC sp_executesql @sql;")
        }
    }
    $alterBlock = if ($alterLines.Count -gt 0) {
        "ELSE`r`nBEGIN`r`n" + ($alterLines -join "`r`n") + "`r`nEND`r`n`r`n"
    } else { '' }
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
$alterBlock
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
--   1. 1_PlmDw_Tables.sql          (this file)
--   2. 2_PlmDw_FieldMapping.sql
--   3. 3_PlmDw_ImportFromDW.sql
--   4. 4_PlmDw_ImportBlueprint.json + Phase D Execute
--   5. 5_PlmDw_ImportBomColorwayGrandchild.sql  (when BOM colorway grids detected)
--   6. 6_PlmDw_CleanupBomColorwayStaging.sql
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
DECLARE @HostTable       NVARCHAR(128);
DECLARE @ParentFkName    NVARCHAR(128);
DECLARE @OldRefFkName    NVARCHAR(128);
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
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'ReferenceCode')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [ReferenceCode] NVARCHAR(255) NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'MasterReferenceId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [MasterReferenceId] INT NULL;'; EXEC sp_executesql @sql; END
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.' + @RootTable) AND name = N'FolderId')
    BEGIN SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@RootTable) + N' ADD [FolderId] INT NULL;'; EXEC sp_executesql @sql; END
END

"@)

# Tab wide tables are ALWAYS siblings (regular sub-items). Grid sub-items become separate grid
# tables (RowId identity PK) via $config.grids. The grid-sub-item probe below is INFORMATIONAL only
# (reports which tabs host grids); it no longer forces the tab table to a child unit. Use an explicit
# config "unitType": "child" override if you ever want an identity-PK child tab table.
$tabIdListForChild = @($config.tabs | ForEach-Object { [int]$_.tabId })
Write-Host "Probing tabs that host Grid sub-items (informational; tab tables stay sibling) from $PlmDatabase..."
$childTabIds = Get-PlmTabsWithGridSubItem $tabIdListForChild
Write-Host "  Tabs hosting grid sub-item(s): $((@($childTabIds) | Sort-Object) -join ', ')"

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

    $tabUnitKind = Resolve-TabUnitKind $tab $childTabIds
    $ddlParts.Add((Build-CreateTableBlock $tab.appTable $fieldRows $tabUnitKind))
    foreach ($r in $fieldRows) { [void]$allFieldRows.Add($r) }
}

Write-Host "Probing PLM for BOM ProductDesignColor colorway grids..."
$bomColorwayGrids = @(Get-BomColorwayGridsFromPlm $config.grids $config.tablePrefixDefault)
Write-Host "  BOM colorway grid(s): $($bomColorwayGrids.Count)"
$bomHostByAppTable = @{}
foreach ($bg in $bomColorwayGrids) {
    Write-Host "    Grid $($bg.plmGridId) tab $($bg.plmTabId) block $($bg.productGridBlockId) -> $($bg.grandchildAppTable)"
    $bomHostByAppTable[$bg.hostAppTable] = $bg
}

foreach ($grid in $config.grids) {
    [void]$scopeAppTables.Add($grid.appTable)
    $dwCols = Get-DwTableColumns $grid.dwTable
    $parentTabId = if ($grid.parentPlmTabId) { [int]$grid.parentPlmTabId } else { $null }
    $fieldRows = Build-FieldRows $dwCols $grid.dwTable $parentTabId $grid.appTable 'GridColumn' $grid.gridSubItemId $grid.gridId
    $ddlRows = $fieldRows
    if ($bomHostByAppTable.ContainsKey($grid.appTable)) {
        $bomBg = $bomHostByAppTable[$grid.appTable]
        $rgbEntityId = $bomBg.rgbColorPlmEntityId
        if (-not $rgbEntityId) { $rgbEntityId = Get-PlmEntityIdBySysTableName 'pdmRGBColor' }
        Complete-BomColorwayGridPivotSchema $bomBg $fieldRows $bomBg.gridMetaList $rgbEntityId $config

        $stagingMetaIds = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($slot in @($bomBg.slots)) {
            foreach ($sv in @($slot.slotValues)) {
                if ($sv.plmMetaColumnId) { [void]$stagingMetaIds.Add([int]$sv.plmMetaColumnId) }
            }
        }
        $slotRows = @($fieldRows | Where-Object { $_.PlmMetaColumnId -and $stagingMetaIds.Contains([int]$_.PlmMetaColumnId) })
        $ddlRows = @($fieldRows | Where-Object { -not $_.PlmMetaColumnId -or -not $stagingMetaIds.Contains([int]$_.PlmMetaColumnId) })
        foreach ($sr in $slotRows) {
            $sr.FieldKind = 'BomColorwayDwSlot'
            [void]$allFieldRows.Add($sr)
        }
    }
    $ddlParts.Add((Build-CreateTableBlock $grid.appTable $ddlRows 'grid'))
    foreach ($r in $ddlRows) { [void]$allFieldRows.Add($r) }
}

foreach ($bg in $bomColorwayGrids) {
    [void]$scopeAppTables.Add($bg.grandchildAppTable)
    if (-not $bg.pivotValueColumns -or $bg.pivotValueColumns.Count -eq 0) {
        Write-Host "  WARN: BOM grid $($bg.plmGridId) has no pivot value columns - skipping grandchild DDL/fields."
        continue
    }
    $ddlParts.Add((Build-GrandchildColorwayTableBlock $bg.grandchildAppTable $bg.hostAppTable $bg.pivotValueColumns))
    $pivotCol = if ($bg.sourcePivotKeyColumn) { $bg.sourcePivotKeyColumn } else { 'Color' }
    $srcRow = $allFieldRows | Where-Object {
        $_.AppTable -eq $bg.sourceAppTable -and $_.AppColumn -eq $pivotCol -and $_.FieldKind -eq 'GridColumn'
    } | Select-Object -First 1
    if ($srcRow) {
        $bg | Add-Member -NotePropertyName pivotKeyPlmMetaColumnId -NotePropertyValue $srcRow.PlmMetaColumnId -Force
        if (-not $bg.rgbColorPlmEntityId -and $srcRow.PlmEntityId) {
            $bg | Add-Member -NotePropertyName rgbColorPlmEntityId -NotePropertyValue $srcRow.PlmEntityId -Force
        }
    }
    if (-not $bg.rgbColorPlmEntityId) {
        $bg | Add-Member -NotePropertyName rgbColorPlmEntityId -NotePropertyValue (Get-PlmEntityIdBySysTableName 'pdmRGBColor') -Force
    }
    Add-GrandchildColorwayFieldRows $allFieldRows $bg
    Write-BomColorwayPivotColumnNameReport $bg
}

$bomColorwayPivotBindings = Build-BomColorwayPivotBindings $bomColorwayGrids $config.tablePrefixDefault

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

$tabIdsForPlm = @()
if ($config.importTabIds) {
    $tabIdsForPlm += @($config.importTabIds | ForEach-Object { [int]$_ })
}
elseif ($config.tabs) {
    $tabIdsForPlm += @($config.tabs | ForEach-Object { [int]$_.tabId })
}
$gridIdsForPlm = @()
if ($config.grids) {
    $gridIdsForPlm += @($config.grids | ForEach-Object { [int]$_.gridId })
}
Write-Host "Loading PLM pdmBlockSubItem metadata for $($tabIdsForPlm.Count) tab(s) from $PlmDatabase..."
$subItemMetaMap = Get-PlmSubItemMetadataMap $tabIdsForPlm
Write-Host "  Sub-item metadata rows: $($subItemMetaMap.Count)"
if ($gridIdsForPlm.Count -gt 0) {
    Write-Host "Loading PLM pdmGridMetaColumn metadata for $($gridIdsForPlm.Count) grid(s)..."
}
$gridColMetaMap = Get-PlmGridColumnMetadataMap $gridIdsForPlm
Write-Host "  Grid column metadata rows: $($gridColMetaMap.Count)"

# Re-order GRID columns by PLM pdmGridMetaColumn.ColumnOrder so the imported child-unit transaction
# fields follow the PLM grid design order — NOT the plmDW physical column (ORDINAL_POSITION) order.
# displayOrder is later assigned sequentially per table from $allFieldRows iteration order.
$gridAppTableToId = @{}
foreach ($g in $config.grids) { $gridAppTableToId[$g.appTable] = [int]$g.gridId }
if ($gridAppTableToId.Count -gt 0) {
    $tableGroups = [ordered]@{}
    foreach ($r in $allFieldRows) {
        if (-not $tableGroups.Contains($r.AppTable)) {
            $tableGroups[$r.AppTable] = New-Object System.Collections.Generic.List[object]
        }
        [void]$tableGroups[$r.AppTable].Add($r)
    }
    $reordered = New-Object System.Collections.Generic.List[object]
    foreach ($tblKey in $tableGroups.Keys) {
        $group = $tableGroups[$tblKey]
        if ($gridAppTableToId.ContainsKey($tblKey)) {
            $gid = $gridAppTableToId[$tblKey]
            $sorted = $group | Sort-Object `
                @{ Expression = {
                    $cid = if ($null -ne $_.PlmMetaColumnId) { [int]$_.PlmMetaColumnId } else { -1 }
                    $mk = "$gid|$cid"
                    if ($gridColMetaMap.ContainsKey($mk) -and $null -ne $gridColMetaMap[$mk].ColumnOrder) {
                        $gridColMetaMap[$mk].ColumnOrder
                    } else { [int]::MaxValue }
                } }, `
                @{ Expression = { if ($null -ne $_.PlmMetaColumnId) { [int]$_.PlmMetaColumnId } else { [int]::MaxValue } } }
            foreach ($x in $sorted) { [void]$reordered.Add($x) }
        }
        else {
            foreach ($x in $group) { [void]$reordered.Add($x) }
        }
    }
    $allFieldRows = $reordered
}

foreach ($r in $allFieldRows) {
    Apply-PlmFieldMetadata $r $subItemMetaMap $gridColMetaMap
}

$ddlParts.Add('GO')
$ddlParts.Add('')

$tablesPath = Join-Path $outDir '1_PlmDw_Tables.sql'
Set-Content -Path $tablesPath -Value ($ddlParts -join "`n") -Encoding UTF8

# SQL Server allows at most 1000 row-value expressions per INSERT … VALUES
$insertBatchSize = 500
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

$insertBatches = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $valuesLines.Count; $i += $insertBatchSize) {
    $take = [Math]::Min($insertBatchSize, $valuesLines.Count - $i)
    $chunk = $valuesLines.GetRange($i, $take)
    $valuesBlock = ($chunk -join ",`n        ")
    $batchNo = [int]($i / $insertBatchSize) + 1
    [void]$insertBatches.Add(@"
-- FieldMapping INSERT batch $batchNo ($take row(s))
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
"@)
}
$insertBatchesSql = $insertBatches -join "`n`n"

$deleteInList = ($scopeAppTables | Select-Object -Unique | ForEach-Object { "N''@P@$_''" }) -join ', '

$mappingSql = @"
-- =============================================================================
-- PLM DW → APP field mapping (generated — see ImportFromPLMDW/PROMPT.md)
-- EXECUTION ORDER:
--   1. 1_PlmDw_Tables.sql
--   2. 2_PlmDw_FieldMapping.sql    (this file)
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
        [FieldKind]         NVARCHAR(32)  NOT NULL,
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
    IF EXISTS (
        SELECT 1 FROM sys.columns AS c
        WHERE c.object_id = OBJECT_ID(N'dbo.' + QUOTENAME(@MappingTable))
          AND c.name = N'FieldKind'
          AND c.max_length < 64
    )
    BEGIN
        SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@MappingTable) + N' ALTER COLUMN [FieldKind] NVARCHAR(32) NOT NULL;';
        EXEC sp_executesql @sql;
    END
END

SET @sql = N'DELETE FROM dbo.' + QUOTENAME(@MappingTable)
    + N' WHERE [AppTableName] IN ($($deleteInList));';
SET @sql = REPLACE(@sql, N'@P@', @TablePrefix);
EXEC sp_executesql @sql;

$insertBatchesSql
GO

"@

$mappingPath = Join-Path $outDir '2_PlmDw_FieldMapping.sql'
Set-Content -Path $mappingPath -Value $mappingSql -Encoding UTF8

$importTemplate = Join-Path $PSScriptRoot 'PlmDw_ImportFromDW.sql'
$importPath = Join-Path $outDir '3_PlmDw_ImportFromDW.sql'
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
# Annotate execution-order comments once (do not replace bare PlmDw_*.sql — that doubles prefixes).
$importContent = $importContent -replace '(?m)^--   1\. PlmDw_Tables\.sql', '--   1. 1_PlmDw_Tables.sql'
$importContent = $importContent -replace '(?m)^--   2\. PlmDw_FieldMapping\.sql', '--   2. 2_PlmDw_FieldMapping.sql'
$importContent = $importContent -replace '(?m)^--   3\. PlmDw_ImportFromDW\.sql', '--   3. 3_PlmDw_ImportFromDW.sql'
Set-Content -Path $importPath -Value $importContent -Encoding UTF8

if (-not $config.blueprint) {
    $config | Add-Member -NotePropertyName blueprint -NotePropertyValue ([ordered]@{
        templateName = 'Fabric'
        transactionGroupName = 'Fabric'
        folderName = 'Fabric'
    }) -Force
}
$tabIdsForExtra = @()
if ($config.importTabIds) {
    $tabIdsForExtra += @($config.importTabIds | ForEach-Object { [int]$_ })
}
elseif ($config.tabs) {
    $tabIdsForExtra += @($config.tabs | ForEach-Object { [int]$_.tabId })
}
Write-Host "Loading PLM pdmTabBlockSubItemExtraInfo for $($tabIdsForExtra.Count) tab(s) from $PlmDatabase..."
$extraInfoMap = Get-PlmSubItemExtraInfoMap $tabIdsForExtra
Write-Host "  Extra info rows: $($extraInfoMap.Count)"
Write-Host "Loading PLM pdmTabGridMetaColumn (grid column visibility)..."
$tabGridVisibleMap = Get-PlmTabGridColumnVisibleMap $tabIdsForExtra
Write-Host "  Tab grid column rows: $($tabGridVisibleMap.Count)"
Write-Host "Loading PLM pdmTabLayoutSubitem (Tab Design layer)..."
$layoutSubItemSet = Get-PlmTabLayoutSubItemSet $tabIdsForExtra
Write-Host "  Tab layout sub-item placements: $($layoutSubItemSet.Count)"
$blueprintObj = Build-BlueprintFromConfig $config $allFieldRows $extraInfoMap $subItemMetaMap $gridColMetaMap $tabGridVisibleMap $layoutSubItemSet $childTabIds $bomColorwayPivotBindings
$blueprintPath = Join-Path $outDir '4_PlmDw_ImportBlueprint.json'
$blueprintJsonPretty = Fix-BomColorwayBindingsJsonArray ($blueprintObj | ConvertTo-Json -Depth 20)
$blueprintJsonPretty | Set-Content -Path $blueprintPath -Encoding UTF8

Generate-BomColorwaySqlFiles $bomColorwayGrids $config $templateId $outDir

# Remove legacy unnumbered deliverables from prior generator runs
@(
    'PlmDw_Tables.sql', 'PlmDw_FieldMapping.sql', 'PlmDw_ImportFromDW.sql',
    'PlmDw_ImportBlueprint.json', 'PlmDw_ImportBlueprint.sql',
    'PlmDw_ImportBomColorwayGrandchild.sql', 'PlmDw_CleanupBomColorwayStaging.sql',
    '4_PlmDw_ImportBomColorwayGrandchild.sql', '5_PlmDw_CleanupBomColorwayStaging.sql', '6_PlmDw_ImportBlueprint.json'
) | ForEach-Object {
    $legacy = Join-Path $outDir $_
    if (Test-Path $legacy) { Remove-Item $legacy -Force }
}

Write-Host "Output folder: $outDir"
Write-Host "Generated: $tablesPath"
Write-Host "Generated: $mappingPath ($($allFieldRows.Count) mappings)"
Write-Host "Generated: $blueprintPath"
Write-Host "Generated: $importPath"
if ($bomColorwayGrids.Count -gt 0) {
    Write-Host "Generated: $(Join-Path $outDir '5_PlmDw_ImportBomColorwayGrandchild.sql')"
    Write-Host "Generated: $(Join-Path $outDir '6_PlmDw_CleanupBomColorwayStaging.sql')"
}
foreach ($t in $config.tabs) {
    $n = @($allFieldRows | Where-Object { $_.AppTable -eq $t.appTable }).Count
    Write-Host "  $($t.appTable): $n columns"
}
foreach ($g in $config.grids) {
    $n = @($allFieldRows | Where-Object { $_.AppTable -eq $g.appTable }).Count
    Write-Host "  $($g.appTable): $n columns"
}
Write-Host "  $($config.rootTableSuffix): 1 mapping (ReferenceCode)"
