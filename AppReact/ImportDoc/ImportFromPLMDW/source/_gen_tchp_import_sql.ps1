# Dot-sourced by _gen_plmdw_import_sql.ps1 — TechPack Tchp* step 3b emitter (static SQL).

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
        for ($rn = 1; $rn -le 6; $rn++) {
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
    & $add '-- L2: TchpStyleSpec.ProductReferenceId -> Root.ReferenceId (Blueprint Link-to-Parent; no DB FK).'
    & $add "-- S1: SizeRun/BaseSize/UOM from Grading tab $sourceTabId ($gradingDw)."
    & $add '-- UOM: PLM tblUnitOfMeasure (not on tenant) -> CM|INCH; unmatched defaults to CM.'
    & $add '-- SpecFit ActualValue = COALESCE(ReviseN, SampleN). Comments tabs do not host Fit grid.'
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
        & $add '-- 1. TchpStyleSpec'
        & $add ';WITH src AS ('
        & $add '  SELECT g.ProductReferenceID AS ProductReferenceId,'
        & $add "    TRY_CONVERT(INT, g.$sizeDwCol) AS SizeRunIdRaw,"
        & $add "    $baseExpr AS BaseSizeRaw,"
        & $add "    $uomExpr AS MeasureUnitRaw"
        & $add "  FROM $dwRef.$gradingDw g"
        & $add '  WHERE g.ProductReferenceID IS NOT NULL'
        & $add ')'
        & $add 'MERGE dbo.TchpStyleSpec AS t'
        & $add 'USING ('
        & $add '  SELECT s.ProductReferenceId,'
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
        & $add ') AS x ON x.ProductReferenceId = t.ProductReferenceId'
        & $add 'WHEN MATCHED THEN UPDATE SET SizeRunId = x.SizeRunId, BaseSizeDetailId = x.BaseSizeDetailId,'
        & $add '  UnitOfMeasure = x.UnitOfMeasure, AppModifiedDate = GETDATE()'
        & $add 'WHEN NOT MATCHED THEN INSERT (ProductReferenceId, SizeRunId, BaseSizeDetailId, UnitOfMeasure, AppCreatedDate)'
        & $add 'VALUES (x.ProductReferenceId, x.SizeRunId, x.BaseSizeDetailId, x.UnitOfMeasure, GETDATE());'
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
        & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = g.ProductReferenceID'
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
            & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = u.ProductReferenceID'
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
            $sampleExpr = if ($rp.SampleCol) { "g.$($rp.SampleCol)" } else { 'NULL' }
            $reviseExpr = if ($rp.ReviseCol) { "g.$($rp.ReviseCol)" } else { 'NULL' }
            [void]$fitUnions.Add(@"
SELECT g.ProductReferenceID, TRY_CONVERT(INT, g.$sfBodyPartCol) AS BodyPartRaw,
  $($rp.Round) AS RoundNumber,
  TRY_CONVERT(DECIMAL(10,3), COALESCE($reviseExpr, $sampleExpr)) AS ActualValue
FROM $dwRef.$sfDw g
WHERE TRY_CONVERT(INT, g.$sfBodyPartCol) IS NOT NULL
  AND TRY_CONVERT(DECIMAL(10,3), COALESCE($reviseExpr, $sampleExpr)) IS NOT NULL
"@.Trim())
        }
        $fitUnionSql = $fitUnions -join "`r`nUNION ALL`r`n"
        & $add '-- 4. TchpFitRound + TchpFitMeasurement'
        & $add 'INSERT INTO dbo.TchpFitRound (StyleSpecId, RoundNumber, RoundType, RoundStatus, AppCreatedDate)'
        & $add "SELECT DISTINCT ss.StyleSpecId, r.RoundNumber, N'INTERNAL', N'PENDING', GETDATE()"
        & $add 'FROM ('
        & $add '  SELECT DISTINCT ProductReferenceID, RoundNumber FROM ('
        & $add $fitUnionSql
        & $add '  ) x'
        & $add ') r'
        & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = r.ProductReferenceID'
        & $add 'WHERE NOT EXISTS ('
        & $add '  SELECT 1 FROM dbo.TchpFitRound fr'
        & $add '  WHERE fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = r.RoundNumber'
        & $add ');'
        & $add 'PRINT N''TchpFitRound insert done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''
        & $add ';WITH meas AS ('
        & $add $fitUnionSql
        & $add ')'
        & $add 'INSERT INTO dbo.TchpFitMeasurement (FitRoundId, PomSpecLineId, ActualValue, AppCreatedDate)'
        & $add 'SELECT fr.FitRoundId, pl.PomSpecLineId, m.ActualValue, GETDATE()'
        & $add 'FROM meas m'
        & $add 'INNER JOIN dbo.TchpStyleSpec ss ON ss.ProductReferenceId = m.ProductReferenceID'
        & $add 'INNER JOIN dbo.TchpFitRound fr ON fr.StyleSpecId = ss.StyleSpecId AND fr.RoundNumber = m.RoundNumber'
        & $add 'INNER JOIN dbo.TchpPomSpecLine pl ON pl.StyleSpecId = ss.StyleSpecId AND pl.BodyPartId = m.BodyPartRaw'
        & $add 'WHERE NOT EXISTS ('
        & $add '  SELECT 1 FROM dbo.TchpFitMeasurement fm'
        & $add '  WHERE fm.FitRoundId = fr.FitRoundId AND fm.PomSpecLineId = pl.PomSpecLineId'
        & $add ');'
        & $add 'PRINT N''TchpFitMeasurement insert done. Rows='' + CAST(@@ROWCOUNT AS NVARCHAR(20));'
        & $add ''
    }
    else {
        & $add 'PRINT N''WARN: FitRound/Measurement skipped — SpecFit not resolved.'';'
        & $add ''
    }

    & $add 'PRINT N''TechPack Tchp import batch finished.'';'
    & $add 'GO'

    $path = Join-Path $outDir '3b_Tchp_ImportFromDW.sql'
    Set-Content -Path $path -Value ($L -join "`r`n") -Encoding UTF8
    return $path
}
