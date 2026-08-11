# Dot-sourced by _gen_plmdw_import_sql.ps1 — TechPack Tchp* step 3b emitter (static SQL).

function Resolve-FitRoundInfoSemanticColumns($config, [string]$scriptRoot) {
    if (-not $config.techPack -or -not $config.techPack.fitRoundInfo) { return @() }
    $fri = $config.techPack.fitRoundInfo
    $inline = @($fri.semanticColumns) | Where-Object { $_ -and $_.appColumn }
    if ($inline.Count -gt 0) { return @($inline) }

    $fileName = $null
    if ($fri.semanticColumnsFile) { $fileName = [string]$fri.semanticColumnsFile }
    if (-not $fileName) {
        $tid = $null
        if ($null -ne $config.plmTemplateId -and [int]$config.plmTemplateId -gt 0) { $tid = [int]$config.plmTemplateId }
        elseif ($config.plmTemplate -and $null -ne $config.plmTemplate.templateId) { $tid = [int]$config.plmTemplate.templateId }
        if ($tid) { $fileName = "fitRoundInfo.semanticColumns.$tid.json" }
    }
    if (-not $fileName) { return @() }
    $path = Join-Path $scriptRoot $fileName
    if (-not (Test-Path $path)) { return @() }
    $doc = Get-Content $path -Raw | ConvertFrom-Json
    return @($doc.semanticColumns) | Where-Object { $_ -and $_.appColumn }
}

# Map SpecFit SampleN / Fit-family block → TchpFitRound.RoundType (Sample | PP | Top).
function Resolve-FitRoundTypeByRoundNumber($config) {
    $map = @{}
    if (-not $config.techPack) { return $map }

    # Explicit map wins (keys may be string or int).
    if ($config.techPack.fitRoundTypeByRoundNumber) {
        foreach ($p in $config.techPack.fitRoundTypeByRoundNumber.PSObject.Properties) {
            $n = 0
            if ([int]::TryParse([string]$p.Name, [ref]$n) -and $n -gt 0 -and $p.Value) {
                $map[$n] = [string]$p.Value
            }
        }
    }

    # Derive from techPack.bindings roles: FitN→Sample, PPn→PP, TOPn→Top.
    foreach ($b in @($config.techPack.bindings)) {
        if (-not $b -or -not $b.role) { continue }
        $role = [string]$b.role
        $n = $null
        $type = $null
        if ($role -match '^(?i)Fit(\d+)$') { $n = [int]$Matches[1]; $type = 'Sample' }
        elseif ($role -match '^(?i)PP(\d+)$') { $n = [int]$Matches[1]; $type = 'PP' }
        elseif ($role -match '^(?i)TOP(\d+)$') { $n = [int]$Matches[1]; $type = 'Top' }
        elseif ($role -match '^(?i)TOP$') { $n = 1; $type = 'Top' }
        if ($n -and $type -and -not $map.ContainsKey($n)) { $map[$n] = $type }
    }

    # Template with folded Fit1–4 (no FitN bindings): default Sample1–4 → Sample when map empty.
    if ($map.Count -eq 0) {
        for ($i = 1; $i -le 4; $i++) { $map[$i] = 'Sample' }
    }
    return $map
}

function Build-FitRoundTypeCaseSql($roundTypeMap, [string]$defaultType, [string]$expr) {
    if (-not $roundTypeMap -or $roundTypeMap.Count -eq 0) {
        return ("N'{0}'" -f ($defaultType -replace "'", "''"))
    }
    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add("CASE $expr")
    foreach ($k in ($roundTypeMap.Keys | Sort-Object)) {
        [void]$lines.Add(("  WHEN {0} THEN N'{1}'" -f $k, ($roundTypeMap[$k] -replace "'", "''")))
    }
    [void]$lines.Add(("  ELSE N'{0}' END" -f ($defaultType -replace "'", "''")))
    return ($lines -join "`r`n")
}

function Find-DwColumnByStem($dwCols, [string]$stem) {
    if (-not $dwCols) { return $null }
    $exact = @($dwCols | Where-Object {
        $m = Get-DwColumnMeta $_.DwColumn
        $m.Stem -eq $stem -or $_.DwColumn -eq $stem
    } | Select-Object -First 1)
    if ($exact) { return $exact[0] }
    $prefix = @($dwCols | Where-Object {
        $m = Get-DwColumnMeta $_.DwColumn
        ($m.Stem -and $m.Stem.StartsWith($stem)) -or ($_.DwColumn -and $_.DwColumn.StartsWith($stem))
    } | Select-Object -First 1)
    if ($prefix) { return $prefix[0] }
    return $null
}

function Generate-TchpImportSqlFile($config, [string]$outDir) {
    if (-not $config.techPack) { return $null }

    $dwDb = [string]$config.dwDatabase
    $dwRef = '[' + ($dwDb -replace ']', ']]') + '].dbo'
    $plmDb = if ($config.plmDatabase) { [string]$config.plmDatabase } else { 'PLM' }
    $plmRef = '[' + ($plmDb -replace ']', ']]') + '].dbo'
    $sourceTabId = if ($null -ne $config.techPack.styleSpecSourcePlmTabId) { [int]$config.techPack.styleSpecSourcePlmTabId } else { 4006 }
    $gradingTab = @($config.tabs) | Where-Object { [int]$_.tabId -eq $sourceTabId } | Select-Object -First 1
    $gradingDw = if ($gradingTab) { [string]$gradingTab.dwTable } else { $null }

    $specGrading = @($config.techPack.systemBlockGrids) | Where-Object { $_.role -eq 'SpecGrading' } | Select-Object -First 1
    $specFit = @($config.techPack.systemBlockGrids) | Where-Object { $_.role -eq 'SpecFit' } | Select-Object -First 1
    $sgDw = if ($specGrading) { [string]$specGrading.dwTable } else { $null }
    $sfDw = if ($specFit) { [string]$specFit.dwTable } else { $null }

    $sizeDwCol = $null; $baseDwCol = $null; $uomDwCol = $null
    if ($gradingDw) {
        $gCols = @(Get-DwTableColumns $gradingDw)
        $c = Find-DwColumnByStem $gCols 'Size_Run'; if ($c) { $sizeDwCol = $c.DwColumn }
        $c = Find-DwColumnByStem $gCols 'Base_Size'; if ($c) { $baseDwCol = $c.DwColumn }
        $c = Find-DwColumnByStem $gCols 'Measure_Unit'; if ($c) { $uomDwCol = $c.DwColumn }
        if (-not $uomDwCol) { $c = Find-DwColumnByStem $gCols 'Unit_Of_Measure'; if ($c) { $uomDwCol = $c.DwColumn } }
    }

    $sgBodyPartCol = $null; $sgBaseCol = $null; $sgTolCol = $null; $sgFixedCol = $null; $sgAliasCol = $null
    $gradeSizeCols = @()
    if ($sgDw) {
        $sgCols = @(Get-DwTableColumns $sgDw)
        $c = Find-DwColumnByStem $sgCols 'BodyPartDetailIDWDimDetailID'; if (-not $c) { $c = Find-DwColumnByStem $sgCols 'BodyPart' }
        if ($c) { $sgBodyPartCol = $c.DwColumn }
        $c = Find-DwColumnByStem $sgCols 'GradingBaseSize'; if ($c) { $sgBaseCol = $c.DwColumn }
        $c = Find-DwColumnByStem $sgCols 'Tolerance'; if ($c) { $sgTolCol = $c.DwColumn }
        $c = Find-DwColumnByStem $sgCols 'NeedToApplyGradingRule'; if ($c) { $sgFixedCol = $c.DwColumn }
        $c = Find-DwColumnByStem $sgCols 'BodyPartName'; if ($c) { $sgAliasCol = $c.DwColumn }
        $gradeSizeCols = @($sgCols | Where-Object { (Get-DwColumnMeta $_.DwColumn).Stem -match '^GradingSize\d+$' } |
            Sort-Object { [int](([regex]::Match((Get-DwColumnMeta $_.DwColumn).Stem, '\d+')).Value) })
    }

    $sfBodyPartCol = $null
    $sfRoundPairs = @()
    if ($sfDw) {
        $sfCols = @(Get-DwTableColumns $sfDw)
        $c = Find-DwColumnByStem $sfCols 'BodyPartDetailIDWDimDetailID'; if (-not $c) { $c = Find-DwColumnByStem $sfCols 'BodyPart' }
        if ($c) { $sfBodyPartCol = $c.DwColumn }
        for ($rn = 1; $rn -le 12; $rn++) {
            $sample = Find-DwColumnByStem $sfCols "Sample$rn"
            $revise = Find-DwColumnByStem $sfCols "Revise$rn"
            if ($sample -or $revise) {
                $sfRoundPairs += [pscustomobject]@{
                    Round = $rn
                    SampleCol = if ($sample) { $sample.DwColumn } else { $null }
                    ReviseCol = if ($revise) { $revise.DwColumn } else { $null }
                }
            }
        }
    }

    $L = New-Object System.Collections.Generic.List[string]
    $add = { param($s) [void]$L.Add($s) }

    & $add '-- ============================================================================='
    & $add '-- TechPack Tchp* import from plmDW (D1) — STATIC SQL (no dynamic @sql).'
    & $add '-- L2: TchpStyleSpec.StyleSpecId = Root.ReferenceId (no identity; sibling PK = parent PK).'
    & $add "-- S1: SizeRun/BaseSize/UOM from Grading tab $sourceTabId ($gradingDw)."
    & $add '-- UOM: PLM tblUnitOfMeasure (not on tenant) -> CM|INCH; unmatched defaults to CM.'
    & $add '-- SpecFit ActualValue = SampleN only (PLM Meas N). ReviseN is Rev.Spec — do not COALESCE into ActualValue.'
    & $add '-- Blank-safe NULLIF on Sample; Comments tabs do not host Fit grid.'
    & $add '-- Prerequisites: Tchp foundation (ImportPlmPomAndGrading); Plm_* steps 1-3.'
    & $add "-- Size_Run=$sizeDwCol Base_Size=$baseDwCol Measure_Unit=$uomDwCol"
    & $add "-- SpecGrading=$sgDw SpecFit=$sfDw PlmUom=$plmRef.tblUnitOfMeasure"
    & $add '-- ============================================================================='
    & $add 'SET NOCOUNT ON;'
    & $add 'SET XACT_ABORT ON;'
    & $add ''

    if ($gradingDw -and $sizeDwCol) {
        $baseExpr = if ($baseDwCol) { "TRY_CONVERT(INT, g.$baseDwCol)" } else { 'CAST(NULL AS INT)' }
        $uomExpr = if ($uomDwCol) { "CONVERT(NVARCHAR(50), g.$uomDwCol)" } else { 'CAST(NULL AS NVARCHAR(50))' }
        & $add '-- 1. TchpStyleSpec (StyleSpecId = ProductReferenceID = ReferenceId)'
        & $add ';WITH src AS ('
        & $add '  SELECT TRY_CONVERT(INT, g.ProductReferenceID) AS StyleSpecId,'
        & $add "    TRY_CONVERT(INT, g.$sizeDwCol) AS SizeRunIdRaw,"
        & $add "    $baseExpr AS BaseSizeRaw,"
        & $add "    $uomExpr AS MeasureUnitRaw"
        & $add "  FROM $dwRef.$gradingDw g"
        & $add '  WHERE TRY_CONVERT(INT, g.ProductReferenceID) IS NOT NULL'
        & $add ')'
        & $add 'MERGE dbo.TchpStyleSpec AS t'
        & $add 'USING ('
        & $add '  SELECT s.StyleSpecId,'
        & $add '    COALESCE(sr.SizeRunId, s.SizeRunIdRaw) AS SizeRunId,'
        & $add '    COALESCE(sz.SizeRunSizeId, s.BaseSizeRaw) AS BaseSizeDetailId,'
        & $add '    CASE'
        & $add "      WHEN UPPER(ISNULL(uom.Unit_Measure, N'')) LIKE N'%INCH%' THEN N'INCH'"
        & $add "      WHEN UPPER(ISNULL(uom.Unit_Measure, N'')) = N'IN' THEN N'INCH'"
        & $add "      WHEN UPPER(ISNULL(uom.Description, N'')) LIKE N'%INCH%' THEN N'INCH'"
        & $add "      ELSE N'CM'"
        & $add '    END AS UnitOfMeasure'
        & $add '  FROM src s'
        & $add '  LEFT JOIN dbo.TchpSizeRun sr ON sr.SizeRunId = s.SizeRunIdRaw'
        & $add '  LEFT JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunSizeId = s.BaseSizeRaw'
        & $add '    OR (sz.SizeRunId = COALESCE(sr.SizeRunId, s.SizeRunIdRaw) AND sz.SizeRunSizeId = s.BaseSizeRaw)'
        & $add "  LEFT JOIN $plmRef.tblUnitOfMeasure uom ON uom.Unit_Id = TRY_CONVERT(INT, s.MeasureUnitRaw)"
        & $add '  WHERE COALESCE(sr.SizeRunId, s.SizeRunIdRaw) IS NOT NULL'
        & $add '    AND COALESCE(sz.SizeRunSizeId, s.BaseSizeRaw) IS NOT NULL'
        & $add ') AS x ON x.StyleSpecId = t.StyleSpecId'
        & $add 'WHEN MATCHED THEN UPDATE SET SizeRunId = x.SizeRunId, BaseSizeDetailId = x.BaseSizeDetailId,'
        & $add '  UnitOfMeasure = x.UnitOfMeasure, AppModifiedDate = GETDATE()'
        & $add 'WHEN NOT MATCHED THEN INSERT (StyleSpecId, SizeRunId, BaseSizeDetailId, UnitOfMeasure, AppCreatedDate)'
        & $add 'VALUES (x.StyleSpecId, x.SizeRunId, x.BaseSizeDetailId, x.UnitOfMeasure, GETDATE());'
        & $add 'PRINT N''TchpStyleSpec MERGE done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''
    }
    else {
        & $add 'PRINT N''WARN: StyleSpec header skipped — Grading DW / Size_Run not resolved.'';'
        & $add ''
    }

    if ($sgDw -and $sgBodyPartCol) {
        $baseVal = if ($sgBaseCol) { "TRY_CONVERT(DECIMAL(10,3), g.$sgBaseCol)" } else { 'CAST(NULL AS DECIMAL(10,3))' }
        $tolVal = if ($sgTolCol) { "TRY_CONVERT(DECIMAL(10,3), g.$sgTolCol)" } else { 'CAST(NULL AS DECIMAL(10,3))' }
        $fixedVal = if ($sgFixedCol) {
            "CASE WHEN UPPER(ISNULL(CONVERT(NVARCHAR(20), g.$sgFixedCol), N'')) IN (N'0', N'N', N'NO', N'FALSE') THEN 1 ELSE 0 END"
        } else { '0' }
        $aliasVal = if ($sgAliasCol) { "CONVERT(NVARCHAR(50), g.$sgAliasCol)" } else { 'CAST(NULL AS NVARCHAR(50))' }
        & $add '-- 2. TchpPomSpecLine'
        & $add 'INSERT INTO dbo.TchpPomSpecLine (StyleSpecId, BodyPartId, BaseValue, Tolerance, IsFixed, Sort, BodypartAliasName, AppCreatedDate)'
        & $add "SELECT ss.StyleSpecId, COALESCE(bp.BodyPartId, TRY_CONVERT(INT, g.$sgBodyPartCol)),"
        & $add "  $baseVal, $tolVal, $fixedVal, g.Sort, $aliasVal, GETDATE()"
        & $add "FROM $dwRef.$sgDw g"
        & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, g.ProductReferenceID)'
        & $add "LEFT JOIN dbo.TchpBodyPart bp ON bp.BodyPartId = TRY_CONVERT(INT, g.$sgBodyPartCol)"
        & $add "WHERE TRY_CONVERT(INT, g.$sgBodyPartCol) IS NOT NULL"
        & $add '  AND NOT EXISTS ('
        & $add '    SELECT 1 FROM dbo.TchpPomSpecLine pl'
        & $add '    WHERE pl.StyleSpecId = ss.StyleSpecId'
        & $add "      AND pl.BodyPartId = COALESCE(bp.BodyPartId, TRY_CONVERT(INT, g.$sgBodyPartCol))"
        & $add '  );'
        & $add 'PRINT N''TchpPomSpecLine insert done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''

        $unions = New-Object System.Collections.Generic.List[string]
        foreach ($gc in $gradeSizeCols) {
            $ord = [int](([regex]::Match((Get-DwColumnMeta $gc.DwColumn).Stem, '\d+')).Value)
            if ($ord -le 0) { continue }
            [void]$unions.Add(@"
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.$sgBodyPartCol) AS BodyPartRaw,
  $ord AS SizeOrdinal, TRY_CONVERT(DECIMAL(10,3), g.$($gc.DwColumn)) AS DeltaVal
FROM $dwRef.$sgDw g
WHERE TRY_CONVERT(DECIMAL(10,3), g.$($gc.DwColumn)) IS NOT NULL
"@.Trim())
        }
        if ($unions.Count -gt 0) {
            & $add '-- 3. TchpGradeValue'
            & $add ';WITH unpvt AS ('
            & $add ($unions -join "`r`nUNION ALL`r`n")
            & $add ')'
            & $add 'INSERT INTO dbo.TchpGradeValue (PomSpecLineId, SizeRunSizeId, GradingDelta, AppCreatedDate)'
            & $add 'SELECT pl.PomSpecLineId, sz.SizeRunSizeId, u.DeltaVal, GETDATE()'
            & $add 'FROM unpvt u'
            & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, u.ProductReferenceID)'
            & $add 'INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = u.BodyPartRaw'
            & $add 'INNER JOIN dbo.TchpSizeRunSize sz ON sz.SizeRunId = ss.SizeRunId AND ISNULL(sz.SizeOrder, 0) = u.SizeOrdinal'
            & $add 'WHERE NOT EXISTS ('
            & $add '  SELECT 1 FROM dbo.TchpGradeValue gv'
            & $add '  WHERE gv.PomSpecLineId = pl.PomSpecLineId AND gv.SizeRunSizeId = sz.SizeRunSizeId'
            & $add ');'
            & $add 'PRINT N''TchpGradeValue insert done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
            & $add ''
        }
    }
    else {
        & $add 'PRINT N''WARN: PomSpecLine/GradeValue skipped — SpecGrading not resolved.'';'
        & $add ''
    }

    if ($sfDw -and $sfBodyPartCol -and $sfRoundPairs.Count -gt 0) {
        $fitUnions = New-Object System.Collections.Generic.List[string]
        foreach ($rp in $sfRoundPairs) {
            # PLM SpecFit: SampleN = Meas N (actual measurement); ReviseN = Rev.Spec N (revised target).
            # ActualValue must be SampleN only — never COALESCE(Revise, Sample).
            # DW nvarchar cells are often '' (not NULL) — NULLIF blank before TRY_CONVERT.
            if (-not $rp.SampleCol) { continue }
            $sampleExpr = "NULLIF(LTRIM(RTRIM(g.$($rp.SampleCol))), N'')"
            $actualExpr = "TRY_CONVERT(DECIMAL(10,3), $sampleExpr)"
            # Round discovery: also treat blank-safe Revise as evidence the round exists (header/spec only).
            $roundPresentParts = @($actualExpr)
            if ($rp.ReviseCol) {
                $revisePresent = "TRY_CONVERT(DECIMAL(10,3), NULLIF(LTRIM(RTRIM(g.$($rp.ReviseCol))), N''))"
                $roundPresentParts += $revisePresent
            }
            $roundPresentExpr = "(" + (($roundPresentParts | ForEach-Object { "$_ IS NOT NULL" }) -join ' OR ') + ")"
            [void]$fitUnions.Add(@"
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.$sfBodyPartCol) AS BodyPartRaw,
  $($rp.Round) AS RoundNumber,
  $actualExpr AS ActualValue
FROM $dwRef.$sfDw g
WHERE TRY_CONVERT(INT, g.$sfBodyPartCol) IS NOT NULL
  AND $roundPresentExpr
"@.Trim())
        }
        $fitUnionSql = $fitUnions -join "`r`nUNION ALL`r`n"
        # RoundType from PLM FIT BLOCK: Fit* → Sample, PP* → PP, TOP* → Top (config overrides).
        $roundTypeMap = Resolve-FitRoundTypeByRoundNumber $config
        $fitRoundTypeDefault = 'Sample'
        if ($config.techPack.fitDefaultRoundType) { $fitRoundTypeDefault = [string]$config.techPack.fitDefaultRoundType }
        $roundTypeCaseR = Build-FitRoundTypeCaseSql $roundTypeMap $fitRoundTypeDefault 'r.RoundNumber'
        $roundTypeCaseFr = Build-FitRoundTypeCaseSql $roundTypeMap $fitRoundTypeDefault 'fr.RoundNumber'

        & $add '-- 4. TchpFitRound + TchpFitMeasurement (R1: RoundNumber = N from SampleN/ReviseN)'
        & $add '-- RoundType: Sample | PP | Top from PLM Fit block (FitN->Sample, PPn->PP, TOPn->Top).'
        & $add 'INSERT INTO dbo.TchpFitRound (StyleSpecId, RoundNumber, RoundType, RoundStatus, AppCreatedDate)'
        & $add 'SELECT DISTINCT ss.StyleSpecId, r.RoundNumber,'
        & $add "  $roundTypeCaseR,"
        & $add '  N''PENDING'', GETDATE()'
        & $add 'FROM ('
        & $add '  SELECT DISTINCT ProductReferenceID, RoundNumber FROM ('
        & $add $fitUnionSql
        & $add '  ) x'
        & $add ') r'
        & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, r.ProductReferenceID)'
        & $add 'WHERE NOT EXISTS ('
        & $add '  SELECT 1 FROM dbo.TchpFitRound fr'
        & $add '  WHERE fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = r.RoundNumber'
        & $add ');'
        & $add 'PRINT N''TchpFitRound insert done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''
        & $add ';WITH roundSrc AS ('
        & $add '  SELECT DISTINCT TRY_CONVERT(INT, ProductReferenceID) AS StyleSpecId, RoundNumber FROM ('
        & $add $fitUnionSql
        & $add '  ) x WHERE TRY_CONVERT(INT, ProductReferenceID) IS NOT NULL'
        & $add ')'
        & $add 'UPDATE fr SET'
        & $add "  fr.RoundType = $roundTypeCaseFr,"
        & $add '  fr.AppModifiedDate = GETDATE()'
        & $add 'FROM dbo.TchpFitRound fr'
        & $add 'INNER JOIN roundSrc s ON s.StyleSpecId = fr.StyleSpecId AND s.RoundNumber = fr.RoundNumber'
        & $add "WHERE ISNULL(fr.RoundType, N'') <> ($roundTypeCaseFr);"
        & $add 'PRINT N''TchpFitRound RoundType sync done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''
        & $add ';WITH meas AS ('
        & $add $fitUnionSql
        & $add ')'
        & $add 'INSERT INTO dbo.TchpFitMeasurement (FitRoundId, PomSpecLineId, ActualValue, AppCreatedDate)'
        & $add 'SELECT fr.FitRoundId, pl.PomSpecLineId, m.ActualValue, GETDATE()'
        & $add 'FROM meas m'
        & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.StyleSpecId = TRY_CONVERT(INT, m.ProductReferenceID)'
        & $add 'INNER JOIN dbo.TchpFitRound fr ON fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = m.RoundNumber'
        & $add 'INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = m.BodyPartRaw'
        & $add 'WHERE m.ActualValue IS NOT NULL'
        & $add '  AND NOT EXISTS ('
        & $add '  SELECT 1 FROM dbo.TchpFitMeasurement fm'
        & $add '  WHERE fm.FitRoundId = fr.FitRoundId AND fm.PomSpecLineId = pl.PomSpecLineId'
        & $add ');'
        & $add 'PRINT N''TchpFitMeasurement insert done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''
        & $add ';WITH meas AS ('
        & $add $fitUnionSql
        & $add ')'
        & $add 'UPDATE fm'
        & $add 'SET fm.ActualValue = m.ActualValue'
        & $add 'FROM dbo.TchpFitMeasurement fm'
        & $add 'INNER JOIN dbo.TchpFitRound fr ON fr.FitRoundId = fm.FitRoundId'
        & $add 'INNER JOIN dbo.TchpPomSpecLine pl ON pl.PomSpecLineId = fm.PomSpecLineId'
        & $add 'INNER JOIN meas m ON TRY_CONVERT(INT, m.ProductReferenceID) = fr.StyleSpecId'
        & $add '  AND m.RoundNumber = fr.RoundNumber'
        & $add '  AND m.BodyPartRaw = pl.BodyPartId'
        & $add 'WHERE m.ActualValue IS NOT NULL'
        & $add '  AND (fm.ActualValue IS NULL OR fm.ActualValue <> m.ActualValue);'
        & $add 'PRINT N''TchpFitMeasurement update done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''

        # FX1: skeleton Plm_FitRoundInfo + semantic fill from Fit N / Comments tabs (not Fit Summary flatten).
        $friApp = 'FitRoundInfo'
        if ($config.techPack.fitRoundInfo -and $config.techPack.fitRoundInfo.appTable) {
            $friApp = [string]$config.techPack.fitRoundInfo.appTable
        }
        $friTable = 'Plm_' + $friApp
        if ($config.tablePrefixDefault) {
            $p = [string]$config.tablePrefixDefault
            if (-not $p.EndsWith('_')) { $p += '_' }
            $friTable = $p + $friApp
        }
        $friSemCols = @(Resolve-FitRoundInfoSemanticColumns $config $PSScriptRoot)
        & $add ("-- 4b. FX1 skeleton {0} (FitRoundId = TchpFitRound.FitRoundId)" -f $friTable)
        & $add ("IF OBJECT_ID(N'dbo.{0}', N'U') IS NOT NULL" -f $friTable)
        & $add 'BEGIN'
        & $add ("  INSERT INTO dbo.{0} (FitRoundId, StyleSpecId, AppCreatedDate)" -f $friTable)
        & $add '  SELECT fr.FitRoundId, fr.StyleSpecId, GETDATE()'
        & $add '  FROM dbo.TchpFitRound fr'
        & $add '  WHERE NOT EXISTS ('
        & $add ("    SELECT 1 FROM dbo.{0} i WHERE i.FitRoundId = fr.FitRoundId" -f $friTable)
        & $add '  );'
        & $add ("  PRINT N'{0} skeleton insert done. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));" -f $friTable)
        & $add 'END'
        & $add 'ELSE'
        & $add ("  PRINT N'WARN: {0} missing - run step 1_ tables before 3b.';" -f $friTable)
        & $add ''

        if ($friSemCols.Count -gt 0) {
            & $add ("-- 4c. FX1 {0} semantic columns from Fit N + Comments (per RoundNumber)" -f $friTable)
            & $add ("IF OBJECT_ID(N'dbo.{0}', N'U') IS NOT NULL" -f $friTable)
            & $add 'BEGIN'
            $roundNums = New-Object System.Collections.Generic.HashSet[int]
            foreach ($sc in $friSemCols) {
                foreach ($src in @($sc.roundSources) + @($sc.commentSources)) {
                    if ($src -and $null -ne $src.roundNumber) { [void]$roundNums.Add([int]$src.roundNumber) }
                }
            }
            foreach ($rn in ($roundNums | Sort-Object)) {
                $fitTbl = $null; $cmtTbl = $null
                foreach ($sc in $friSemCols) {
                    foreach ($rs in @($sc.roundSources)) {
                        if ($rs -and [int]$rs.roundNumber -eq $rn -and $rs.dwTable) { $fitTbl = [string]$rs.dwTable; break }
                    }
                    if ($fitTbl) { break }
                }
                foreach ($sc in $friSemCols) {
                    foreach ($cs in @($sc.commentSources)) {
                        if ($cs -and [int]$cs.roundNumber -eq $rn -and $cs.dwTable) { $cmtTbl = [string]$cs.dwTable; break }
                    }
                    if ($cmtTbl) { break }
                }
                if (-not $fitTbl -and -not $cmtTbl) { continue }

                $setParts = New-Object System.Collections.Generic.List[string]
                $applyParts = New-Object System.Collections.Generic.List[string]
                foreach ($sc in $friSemCols) {
                    $appCol = [string]$sc.appColumn
                    $exprs = New-Object System.Collections.Generic.List[string]
                    foreach ($rs in @($sc.roundSources)) {
                        if (-not $rs -or [int]$rs.roundNumber -ne $rn) { continue }
                        $alias = 'f'
                        foreach ($dc in @($rs.dwColumns)) {
                            if (-not $dc) { continue }
                            $exprs.Add(("$alias.{0}" -f $dc))
                        }
                    }
                    foreach ($cs in @($sc.commentSources)) {
                        if (-not $cs -or [int]$cs.roundNumber -ne $rn) { continue }
                        $alias = 'c'
                        foreach ($dc in @($cs.dwColumns)) {
                            if (-not $dc) { continue }
                            $exprs.Add(("$alias.{0}" -f $dc))
                        }
                    }
                    if ($exprs.Count -eq 0) { continue }
                    $coalesce = if ($exprs.Count -eq 1) { $exprs[0] } else { 'COALESCE(' + ($exprs -join ', ') + ')' }
                    [void]$applyParts.Add(("    {0} AS [{1}]" -f $coalesce, $appCol))
                    [void]$setParts.Add(("  i.[{0}] = src.[{0}]" -f $appCol))
                }
                if ($setParts.Count -eq 0) { continue }

                & $add ("  -- Round {0}" -f $rn)
                & $add '  UPDATE i SET'
                & $add (($setParts -join ",`r`n") + ',')
                & $add '  i.AppModifiedDate = GETDATE()'
                & $add ("  FROM dbo.{0} i" -f $friTable)
                & $add ("  INNER JOIN dbo.TchpFitRound fr ON fr.FitRoundId = i.FitRoundId AND fr.RoundNumber = {0}" -f $rn)
                if ($fitTbl) {
                    & $add ("  LEFT JOIN {0}.{1} f ON TRY_CONVERT(INT, f.ProductReferenceID) = fr.StyleSpecId" -f $dwRef, $fitTbl)
                } else {
                    & $add '  OUTER APPLY (SELECT CAST(NULL AS INT) AS _NoFit) f'
                }
                if ($cmtTbl) {
                    & $add ("  LEFT JOIN {0}.{1} c ON TRY_CONVERT(INT, c.ProductReferenceID) = fr.StyleSpecId" -f $dwRef, $cmtTbl)
                } else {
                    & $add '  OUTER APPLY (SELECT CAST(NULL AS INT) AS _NoCmt) c'
                }
                & $add '  CROSS APPLY (SELECT'
                & $add (($applyParts -join ",`r`n") )
                & $add '  ) src;'
                & $add ("  PRINT N'{0} semantic Round {1} update. Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));" -f $friTable, $rn)
                & $add ''
            }
            & $add 'END'
            & $add ''
        }
    }
    else {
        & $add 'PRINT N''WARN: FitRound/Measurement skipped — SpecFit not resolved.'';'
        & $add ''
    }

    & $add 'PRINT N''TechPack Tchp import batch finished.'';'
    & $add 'GO'
    & $add ''
    & $add '-- ============================================================================='
    & $add '-- V1: View_TchpStyleActiveSizeRunSizes (Grading ROOT read-only SizeRunSizes child)'
    & $add '-- IsVisible: Dimension filter AND VisibleSizes whitelist (pipe-delimited SizeRunSizeId).'
    & $add '-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql'
    & $add '-- Run this script BEFORE Phase D Blueprint Execute.'
    & $add '-- CREATE VIEW must be first statement in its batch (GO above required).'
    & $add '-- ============================================================================='
    & $add 'CREATE OR ALTER VIEW dbo.View_TchpStyleActiveSizeRunSizes'
    & $add 'AS'
    & $add 'SELECT'
    & $add '    ss.StyleSpecId,'
    & $add '    ss.SizeRunId,'
    & $add '    srs.SizeRunSizeId,'
    & $add '    srs.SizeLabel,'
    & $add '    srs.SizeOrder,'
    & $add '    srs.IsActive,'
    & $add '    CASE'
    & $add '        WHEN ('
    & $add '            CASE'
    & $add '                WHEN NOT EXISTS ('
    & $add '                    SELECT 1'
    & $add '                    FROM dbo.TchpStyleSpecDimension AS ssd'
    & $add '                    WHERE ssd.StyleSpecId = ss.StyleSpecId'
    & $add '                ) THEN 1'
    & $add '                WHEN EXISTS ('
    & $add '                    SELECT 1'
    & $add '                    FROM dbo.TchpSizeRunDimension AS srd'
    & $add '                    INNER JOIN dbo.TchpStyleSpecDimension AS ssd'
    & $add '                        ON ssd.StyleSpecId = ss.StyleSpecId'
    & $add '                       AND ssd.DimensionCode = srd.DimensionCode'
    & $add '                       AND ('
    & $add '                            ssd.IsActive = 1'
    & $add '                            OR NOT EXISTS ('
    & $add '                                SELECT 1'
    & $add '                                FROM dbo.TchpStyleSpecDimension AS x'
    & $add '                                WHERE x.StyleSpecId = ss.StyleSpecId'
    & $add '                                  AND x.IsActive = 1'
    & $add '                            )'
    & $add '                       )'
    & $add '                    WHERE srd.SizeRunSizeId = srs.SizeRunSizeId'
    & $add '                ) THEN 1'
    & $add '                ELSE 0'
    & $add '            END'
    & $add '        ) = 0 THEN 0'
    & $add '        WHEN NULLIF(LTRIM(RTRIM(ss.VisibleSizes)), N'''') IS NULL THEN 1'
    & $add '        WHEN EXISTS ('
    & $add '            SELECT 1'
    & $add '            FROM STRING_SPLIT(REPLACE(ss.VisibleSizes, N''|'', N'',''), N'','') AS tok'
    & $add '            WHERE TRY_CONVERT(INT, LTRIM(RTRIM(tok.[value]))) = srs.SizeRunSizeId'
    & $add '        ) THEN 1'
    & $add '        ELSE 0'
    & $add '    END AS IsVisible'
    & $add 'FROM dbo.TchpStyleSpec AS ss'
    & $add 'INNER JOIN dbo.TchpSizeRunSize AS srs'
    & $add '    ON srs.SizeRunId = ss.SizeRunId'
    & $add 'WHERE ISNULL(srs.IsActive, 1) = 1;'
    & $add 'GO'
    & $add 'PRINT N''View_TchpStyleActiveSizeRunSizes created/altered.'';'
    & $add 'GO'
    & $add ''
    & $add '-- ============================================================================='
    & $add '-- View_TchpSizeRunSize_DefaultDimension: size + first DimensionCode'
    & $add '-- One row per SizeRunSizeId; if multiple DimensionCodes, first by SortOrder.'
    & $add '-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql'
    & $add '-- ============================================================================='
    & $add 'IF OBJECT_ID(N''dbo.View_TchpSizeRunSize'', N''V'') IS NOT NULL'
    & $add '    DROP VIEW dbo.View_TchpSizeRunSize; -- rename: old short name'
    & $add 'GO'
    & $add 'CREATE OR ALTER VIEW dbo.View_TchpSizeRunSize_DefaultDimension'
    & $add 'AS'
    & $add 'SELECT'
    & $add '    srs.SizeRunSizeId,'
    & $add '    srs.SizeRunId,'
    & $add '    srs.SizeLabel,'
    & $add '    srs.SizeOrder,'
    & $add '    srs.IsActive,'
    & $add '    ISNULL(dim.DimensionCode, N'''') AS DimensionCode'
    & $add 'FROM dbo.TchpSizeRunSize AS srs'
    & $add 'OUTER APPLY ('
    & $add '    SELECT TOP (1)'
    & $add '        srd.DimensionCode'
    & $add '    FROM dbo.TchpSizeRunDimension AS srd'
    & $add '    WHERE srd.SizeRunSizeId = srs.SizeRunSizeId'
    & $add '    ORDER BY srd.SortOrder ASC, srd.SizeRunDimensionId ASC'
    & $add ') AS dim;'
    & $add 'GO'
    & $add 'PRINT N''View_TchpSizeRunSize_DefaultDimension created/altered.'';'
    & $add 'GO'
    & $add ''
    & $add '-- ============================================================================='
    & $add '-- F3: View_TchpFitMeasurementByPom (FIT SUMMARY POM × Round pivot, read-only)'
    & $add '-- ChildUnitPivotColumns: IsPivotColumn=RoundNumber, IsPivotValue=ActualValue.'
    & $add '-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql'
    & $add '-- ============================================================================='
    & $add 'CREATE OR ALTER VIEW dbo.View_TchpFitMeasurementByPom'
    & $add 'AS'
    & $add 'SELECT'
    & $add '    fm.FitMeasurementId,'
    & $add '    fm.PomSpecLineId,'
    & $add '    pl.StyleSpecId,'
    & $add '    fr.FitRoundId,'
    & $add '    fr.RoundNumber,'
    & $add '    fr.RoundType,'
    & $add '    CONCAT(N''Fit '', fr.RoundNumber) AS RoundLabel,'
    & $add '    fm.ActualValue'
    & $add 'FROM dbo.TchpFitMeasurement AS fm'
    & $add 'INNER JOIN dbo.TchpFitRound AS fr'
    & $add '    ON fr.FitRoundId = fm.FitRoundId'
    & $add 'INNER JOIN dbo.TchpPomSpecLine AS pl'
    & $add '    ON pl.PomSpecLineId = fm.PomSpecLineId;'
    & $add 'GO'
    & $add 'PRINT N''View_TchpFitMeasurementByPom created/altered.'';'
    & $add 'GO'
    & $add ''
    & $add '-- ============================================================================='
    & $add '-- F2: View_TchpPomSpecLine (FIT ROUND PomSpecLine DDL — BodyPartName + BaseValue/Tol)'
    & $add '-- Keep identical to Document/Design/POM_Grading_QC_NewSchema.sql'
    & $add '-- ============================================================================='
    & $add 'CREATE OR ALTER VIEW dbo.View_TchpPomSpecLine'
    & $add 'AS'
    & $add 'SELECT'
    & $add '    pl.PomSpecLineId,'
    & $add '    bp.BodyPartName,'
    & $add '    pl.StyleSpecId,'
    & $add '    pl.GradeRuleSetId,'
    & $add '    pl.BaseValue,'
    & $add '    pl.Tolerance,'
    & $add '    pl.IsFixed,'
    & $add '    pl.Sort,'
    & $add '    pl.BodypartAliasName'
    & $add 'FROM dbo.TchpPomSpecLine AS pl'
    & $add 'INNER JOIN dbo.TchpBodyPart AS bp'
    & $add '    ON bp.BodyPartId = pl.BodyPartId;'
    & $add 'GO'
    & $add 'PRINT N''View_TchpPomSpecLine created/altered.'';'
    & $add 'GO'

    $path = Join-Path $outDir '3b_Tchp_ImportFromDW.sql'
    Set-Content -Path $path -Value ($L -join "`r`n") -Encoding UTF8
    return $path
}
