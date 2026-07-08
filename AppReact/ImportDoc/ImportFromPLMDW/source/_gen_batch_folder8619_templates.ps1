# Multi-template Phase B config builder + generator
# Builds dwTabImportConfig JSON for each TemplateId and runs _gen_plmdw_import_sql.ps1
$ErrorActionPreference = 'Stop'
$src = $PSScriptRoot
$SqlServer = 'PC3B\MSSQLSERVER01'
$PlmDb = 'plm_live_20260602'
$DwDb = 'plmDW'

function Invoke-Sql([string]$Database, [string]$Query) {
    $tmp = [System.IO.Path]::GetTempFileName() + '.sql'
    $out = [System.IO.Path]::GetTempFileName() + '.txt'
    try {
        Set-Content -Path $tmp -Value $Query -Encoding UTF8
        $p = Start-Process -FilePath 'sqlcmd' -ArgumentList @(
            '-S', $SqlServer, '-d', $Database, '-E', '-i', $tmp, '-o', $out, '-W', '-s', '|', '-h', '-1'
        ) -Wait -PassThru -NoNewWindow
        if ($p.ExitCode -ne 0) { throw "sqlcmd failed on $Database exit=$($p.ExitCode)" }
        return @(Get-Content $out | Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' -and $_ -notmatch '^-+$' })
    }
    finally { Remove-Item $tmp, $out -ErrorAction SilentlyContinue }
}

function Sanitize-AppName([string]$name) {
    $n = ($name -replace '[^\w]', '_') -replace '_+', '_' -replace '^_|_$', ''
    if ([string]::IsNullOrWhiteSpace($n)) { $n = 'T' }
    if ($n.Length -gt 80) { $n = $n.Substring(0, 80) }
    return $n
}

function Get-DwTabTable([int]$tabId) {
    $q = @"
SELECT TOP 1 name FROM sys.tables
WHERE name LIKE N'PLM_DW_Tab[_]%'
  AND name LIKE N'%\_$tabId' ESCAPE N'\'
ORDER BY name
"@
    foreach ($line in (Invoke-Sql $DwDb $q)) {
        $t = $line.Trim()
        if ($t) { return $t }
    }
    return $null
}

function Get-DwGridTable([int]$gridId) {
    $q = @"
SELECT TOP 1 name FROM sys.tables
WHERE name LIKE N'PLM_DW_Grid[_]%'
  AND name LIKE N'%\_$gridId' ESCAPE N'\'
ORDER BY name
"@
    foreach ($line in (Invoke-Sql $DwDb $q)) {
        $t = $line.Trim()
        if ($t) { return $t }
    }
    return $null
}

function Get-AppTableFromDwTab([string]$dwTable, [int]$tabId) {
    # PLM_DW_Tab_{Segment}_{TabId}
    if ($dwTable -match '^PLM_DW_Tab_(.+)_\d+$') { return $Matches[1] }
    return "Tab_$tabId"
}

function Get-AppTableFromDwGrid([string]$dwTable, [int]$gridId) {
    if ($dwTable -match '^PLM_DW_Grid_(.+)_\d+$') { return $Matches[1] }
    return "Grid_$gridId"
}

function Integration-Id([string]$name) {
    $id = ($name -replace '[^a-zA-Z0-9]', '')
    if ($id.Length -gt 60) { $id = $id.Substring(0, 60) }
    return $id
}

function Build-Config([int]$templateId) {
    $tplQ = @"
SELECT CAST(t.TemplateID AS NVARCHAR(20)) + '|' + ISNULL(t.TemplateName,N'') 
FROM dbo.pdmTemplate t WHERE t.TemplateID = $templateId
"@
    $tplLine = (Invoke-Sql $PlmDb $tplQ | Select-Object -First 1)
    if (-not $tplLine) { throw "Template $templateId not found" }
    $parts = $tplLine -split '\|', 2
    $templateName = $parts[1].Trim()

    $tabQ = @"
SELECT CAST(tt.Sort AS NVARCHAR(20)) + '|' + CAST(tt.TabID AS NVARCHAR(20)) + '|' + ISNULL(tab.TabName,N'') + '|' + CAST(ISNULL(tab.IsTemplateHeaderTab,0) AS NVARCHAR(1))
FROM dbo.pdmTemplateTab tt
INNER JOIN dbo.pdmTab tab ON tab.TabID = tt.TabID
WHERE tt.TemplateID = $templateId
ORDER BY tt.Sort, tt.TabID
"@
    $tabRows = @()
    foreach ($line in (Invoke-Sql $PlmDb $tabQ)) {
        $p = $line -split '\|', 4
        if ($p.Count -lt 4) { continue }
        $tabRows += [pscustomobject]@{
            Sort = [int]$p[0].Trim()
            TabId = [int]$p[1].Trim()
            TabName = $p[2].Trim()
            IsHeader = ($p[3].Trim() -eq '1')
        }
    }

    $gridQ = @"
SELECT CAST(tt.TabID AS NVARCHAR(20)) + '|' + CAST(bsi.SubItemID AS NVARCHAR(20)) + '|' + CAST(bsi.GridID AS NVARCHAR(20)) + '|' + ISNULL(g.GridName,N'')
FROM dbo.pdmTemplateTab tt
INNER JOIN dbo.pdmTabBlock tb ON tb.TabID = tt.TabID
INNER JOIN dbo.pdmBlockSubItem bsi ON bsi.BlockID = tb.BlockID AND bsi.ControlType = 6 AND bsi.GridID IS NOT NULL
INNER JOIN dbo.pdmGrid g ON g.GridID = bsi.GridID
WHERE tt.TemplateID = $templateId
ORDER BY tt.Sort, tt.TabID, bsi.GridID, bsi.SubItemID
"@
    $gridSeen = @{}
    $gridRows = @()
    foreach ($line in (Invoke-Sql $PlmDb $gridQ)) {
        $p = $line -split '\|', 4
        if ($p.Count -lt 3) { continue }
        $gid = [int]$p[2].Trim()
        $tid = [int]$p[0].Trim()
        $key = "$tid|$gid"
        if ($gridSeen.ContainsKey($key)) { continue }
        $gridSeen[$key] = $true
        $gridRows += [pscustomobject]@{
            TabId = $tid
            SubItemId = [int]$p[1].Trim()
            GridId = $gid
            GridName = if ($p.Count -ge 4) { $p[3].Trim() } else { '' }
        }
    }

    $tabs = @()
    $importTabIds = @()
    $skippedNoDw = @()
    $headerDw = $null
    $headerTabId = $null

    foreach ($tr in $tabRows) {
        $dw = Get-DwTabTable $tr.TabId
        if (-not $dw) {
            $skippedNoDw += "Tab $($tr.TabId) $($tr.TabName) (no PLM_DW_Tab)"
            continue
        }
        $app = Get-AppTableFromDwTab $dw $tr.TabId
        $importTabIds += $tr.TabId
        $mode = 'all'
        $exclude = $null
        # Header/Info pair: Info often shares Article subitems with Header — exclude shared from Info when Header exists
        if (-not $tr.IsHeader -and $headerDw -and ($tr.TabName -match 'Info')) {
            $mode = 'excludeSubItemsFromDwTable'
            $exclude = $headerDw
        }
        $tabObj = [ordered]@{
            appTable = $app
            dwTable = $dw
            tabId = $tr.TabId
            plmTabName = $tr.TabName
            tabSort = $tr.Sort
            isTemplateHeaderTab = [bool]$tr.IsHeader
            importStatus = 'Ready'
            mode = $mode
        }
        if ($exclude) { $tabObj.excludeSubItemsFromDwTable = $exclude }
        $tabs += $tabObj
        if ($tr.IsHeader -and -not $headerDw) {
            $headerDw = $dw
            $headerTabId = $tr.TabId
        }
    }

    if ($tabs.Count -eq 0) { throw "Template $templateId has no DW tab tables" }

    # referenceScope: prefer Header + Article__22; else any tab with Article__22; else preferred name columns
    $refTab = $null
    $refDw = $null
    $refCol = $null
    $refSub = $null
    $candidates = @()
    if ($headerTabId -and $headerDw) {
        $candidates += [pscustomobject]@{ TabId = $headerTabId; Dw = $headerDw }
    }
    foreach ($t in $tabs) {
        $candidates += [pscustomobject]@{ TabId = [int]$t.tabId; Dw = [string]$t.dwTable }
    }
    foreach ($cand in $candidates) {
        $colQ = @"
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'$($cand.Dw.Replace("'","''"))'
  AND COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')
ORDER BY ORDINAL_POSITION
"@
        $cols = @(Invoke-Sql $DwDb $colQ | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        if ($cols -contains 'Article__22') {
            $refTab = $cand.TabId; $refDw = $cand.Dw; $refCol = 'Article__22'; $refSub = 22
            break
        }
    }
    if (-not $refCol) {
        $prefer = @('Request_Name_7154', 'Graphic_Req__6832', 'Name_7028', 'References_6861')
        foreach ($cand in $candidates) {
            $colQ = @"
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'$($cand.Dw.Replace("'","''"))'
  AND COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')
ORDER BY ORDINAL_POSITION
"@
            $cols = @(Invoke-Sql $DwDb $colQ | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            foreach ($c in $prefer) {
                if ($cols -contains $c) {
                    $refTab = $cand.TabId; $refDw = $cand.Dw; $refCol = $c
                    if ($c -match '_(\d+)(?:_FK_|$)') { $refSub = [int]$Matches[1] }
                    break
                }
            }
            if ($refCol) { break }
        }
    }
    if (-not $refCol) {
        $refTab = [int]$tabs[0].tabId
        $refDw = [string]$tabs[0].dwTable
        $colQ = @"
SELECT TOP 1 COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'$($refDw.Replace("'","''"))'
  AND COLUMN_NAME NOT IN (N'TabID', N'ProductReferenceID')
ORDER BY ORDINAL_POSITION
"@
        $refCol = (Invoke-Sql $DwDb $colQ | Select-Object -First 1)
        if ($refCol) { $refCol = $refCol.Trim() }
        if ($refCol -match '_(\d+)(?:_FK_|$)') { $refSub = [int]$Matches[1] }
        elseif ($refCol -match '__(\d+)$') { $refSub = [int]$Matches[1] }
    }

    # Second pass: if first non-header was before header in sort, headerDw set late — fix Fabric Info exclude
    $tabsFixed = @()
    foreach ($t in $tabs) {
        $to = [ordered]@{}
        foreach ($k in $t.Keys) { $to[$k] = $t[$k] }
        if ($headerDw -and -not $to.isTemplateHeaderTab -and $to.plmTabName -match 'Info' -and $to.mode -eq 'all') {
            $to.mode = 'excludeSubItemsFromDwTable'
            $to.excludeSubItemsFromDwTable = $headerDw
        }
        # Publish to ERP often shares header fields
        if ($headerDw -and $to.plmTabName -match 'Publish' -and $to.mode -eq 'all') {
            $to.mode = 'excludeSubItemsFromDwTable'
            $to.excludeSubItemsFromDwTable = $headerDw
        }
        $tabsFixed += $to
    }
    $tabs = $tabsFixed

    $grids = @()
    $gridIdDone = @{}
    foreach ($g in $gridRows) {
        # One APP table per GridId (shared SpecFitGrid / ProductDesignColorGrid across tabs)
        if ($gridIdDone.ContainsKey($g.GridId)) { continue }
        $gdw = Get-DwGridTable $g.GridId
        if (-not $gdw) {
            $skippedNoDw += "Grid $($g.GridId) $($g.GridName) on Tab $($g.TabId) (no PLM_DW_Grid)"
            continue
        }
        $parentInImport = ($importTabIds -contains $g.TabId)
        # Prefer a parent tab that is in the import set
        $parentTab = $g.TabId
        $subItem = $g.SubItemId
        if (-not $parentInImport) {
            $alt = $gridRows | Where-Object { $_.GridId -eq $g.GridId -and ($importTabIds -contains $_.TabId) } | Select-Object -First 1
            if ($alt) {
                $parentTab = $alt.TabId
                $subItem = $alt.SubItemId
                $parentInImport = $true
            }
        }
        $gapp = Get-AppTableFromDwGrid $gdw $g.GridId
        $txId = if ($parentInImport) { "Tab_$parentTab" } else { "Grid_$($g.GridId)" }
        $grids += [ordered]@{
            appTable = $gapp
            dwTable = $gdw
            gridSubItemId = $subItem
            gridId = $g.GridId
            parentPlmTabId = if ($parentInImport) { $parentTab } else { $null }
            transactionIntegrationId = $txId
        }
        $gridIdDone[$g.GridId] = $true
    }

    $tgId = 'TG_' + (Integration-Id $templateName)
    $searchId = 'Search_' + (Integration-Id $templateName)

    $config = [ordered]@{
        plmTemplateId = $templateId
        plmSqlServer = $SqlServer
        plmDatabase = $PlmDb
        sqlServer = $SqlServer
        dwDatabase = $DwDb
        importTabIds = @($importTabIds)
        tablePrefixDefault = 'Plm_'
        rootTableSuffix = 'ReferenceBasicInfo'
        plmTemplate = [ordered]@{
            templateId = $templateId
            templateName = $templateName
            templateHeaderTabIds = @($tabRows | Where-Object { $_.IsHeader } | ForEach-Object { $_.TabId })
        }
        referenceScope = [ordered]@{
            dwTable = $refDw
            dwColumn = $refCol
            plmTabId = $refTab
            plmSubItemId = $refSub
        }
        tabs = @($tabs)
        grids = @($grids)
        blueprint = [ordered]@{
            transactionGroupName = $templateName
            transactionGroupIntegrationId = $tgId
            searchName = "$templateName References"
            searchIntegrationId = $searchId
            folderName = $templateName
        }
    }

    return [pscustomobject]@{
        Config = $config
        Skipped = $skippedNoDw
        TemplateName = $templateName
    }
}

# Preserve current 3351 working config
$mainConfig = Join-Path $src 'dwTabImportConfig.json'
$bak3351 = Join-Path $src 'dwTabImportConfig.3351.json'
if (Test-Path $mainConfig) {
    Copy-Item $mainConfig $bak3351 -Force
}

$templateIds = @(3348, 3352, 3353, 3354, 3355, 3356, 3357, 3359, 3361)
$summary = [System.Collections.Generic.List[string]]::new()

try {
    foreach ($tid in $templateIds) {
        Write-Host "======== Building config for Template $tid ========"
        $built = Build-Config $tid
        $cfgPath = Join-Path $src ("dwTabImportConfig.$tid.json")
        ($built.Config | ConvertTo-Json -Depth 12) | Set-Content -Path $cfgPath -Encoding UTF8
        Write-Host "Wrote $cfgPath"
        if ($built.Skipped.Count -gt 0) {
            Write-Host "  Skipped (no DW):"
            $built.Skipped | ForEach-Object { Write-Host "    $_" }
        }
        Copy-Item $cfgPath $mainConfig -Force
        Write-Host "Generating output/$tid ..."
        & (Join-Path $src '_gen_plmdw_import_sql.ps1')
        if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "Generator failed for $tid" }
        $outDir = Join-Path (Split-Path $src -Parent) "output\$tid"
        $files = @(Get-ChildItem $outDir -File | Select-Object -ExpandProperty Name)
        [void]$summary.Add("$tid|$($built.TemplateName)|tabs=$($built.Config.tabs.Count)|grids=$($built.Config.grids.Count)|skipped=$($built.Skipped.Count)|files=$($files -join ',')")
    }
}
finally {
    if (Test-Path $bak3351) {
        Copy-Item $bak3351 $mainConfig -Force
        Write-Host 'Restored dwTabImportConfig.json to 3351'
    }
}

Write-Host ''
Write-Host '=== SUMMARY ==='
$summary | ForEach-Object { Write-Host $_ }
