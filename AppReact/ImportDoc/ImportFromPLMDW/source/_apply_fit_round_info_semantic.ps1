# Apply FitRoundInfo semantic columns to TenantDB: ALTER + DW fill + TX Entity/DDL + Form layout.
# Usage: .\_apply_fit_round_info_semantic.ps1 [-TenantDb TenantDB_PLM27]
param(
    [string]$SqlServer = 'PC3B\MSSQLSERVER01',
    [string]$TenantDb = 'TenantDB_PLM27',
    [string]$DwDatabase = 'plmDW',
    [string]$User = 'sa',
    [string]$Password = 'appsa',
    [string]$ConfigPath = ''
)
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if (-not $ConfigPath) { $ConfigPath = Join-Path $scriptRoot 'dwTabImportConfig.3283.json' }
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
. (Join-Path $scriptRoot '_gen_tchp_import_sql.ps1')

$semCols = @(Resolve-FitRoundInfoSemanticColumns $config $scriptRoot)
if ($semCols.Count -eq 0) { throw 'No FitRoundInfo semanticColumns resolved.' }

$friApp = 'FitRoundInfo'
if ($config.techPack.fitRoundInfo.appTable) { $friApp = [string]$config.techPack.fitRoundInfo.appTable }
$prefix = if ($config.tablePrefixDefault) { [string]$config.tablePrefixDefault } else { 'Plm_' }
if (-not $prefix.EndsWith('_')) { $prefix += '_' }
$friTable = $prefix + $friApp
$dwRef = '[' + ($DwDatabase -replace ']', ']]') + '].dbo'

function Invoke-Sql([string]$db, [string]$query) {
    $tmp = [System.IO.Path]::GetTempFileName() + '.sql'
    Set-Content -Path $tmp -Value $query -Encoding UTF8
    & sqlcmd -S $SqlServer -U $User -P $Password -C -d $db -b -i $tmp
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed exit=$LASTEXITCODE db=$db" }
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
}

Write-Host "1) ALTER $friTable add semantic columns ($($semCols.Count))..."
$alter = New-Object System.Collections.Generic.List[string]
[void]$alter.Add('SET NOCOUNT ON;')
[void]$alter.Add("IF OBJECT_ID(N'dbo.$friTable', N'U') IS NULL BEGIN RAISERROR(N'Missing $friTable', 16, 1); RETURN; END")
foreach ($sc in $semCols) {
    $col = [string]$sc.appColumn
    $sqlType = if ($sc.sqlType) { [string]$sc.sqlType } else { '[nvarchar](4000)' }
    [void]$alter.Add("IF COL_LENGTH(N'dbo.$friTable', N'$col') IS NULL ALTER TABLE dbo.[$friTable] ADD [$col] $sqlType NULL;")
}
Invoke-Sql $TenantDb ($alter -join "`r`n")

Write-Host '2) Skeleton rows + semantic UPDATE from Fit N / Comments...'
$fill = New-Object System.Collections.Generic.List[string]
[void]$fill.Add('SET NOCOUNT ON;')
[void]$fill.Add(@"
INSERT INTO dbo.[$friTable] (FitRoundId, StyleSpecId, AppCreatedDate)
SELECT fr.FitRoundId, fr.StyleSpecId, GETDATE()
FROM dbo.TchpFitRound fr
WHERE NOT EXISTS (SELECT 1 FROM dbo.[$friTable] i WHERE i.FitRoundId = fr.FitRoundId);
PRINT N'skeleton Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));
"@)

$roundNums = New-Object System.Collections.Generic.HashSet[int]
foreach ($sc in $semCols) {
    foreach ($src in @($sc.roundSources) + @($sc.commentSources)) {
        if ($src -and $null -ne $src.roundNumber) { [void]$roundNums.Add([int]$src.roundNumber) }
    }
}

foreach ($rn in ($roundNums | Sort-Object)) {
    $fitTbl = $null
    $cmtTbl = $null
    foreach ($sc in $semCols) {
        foreach ($rs in @($sc.roundSources)) {
            if ($rs -and [int]$rs.roundNumber -eq $rn -and $rs.dwTable) { $fitTbl = [string]$rs.dwTable; break }
        }
        if ($fitTbl) { break }
    }
    foreach ($sc in $semCols) {
        foreach ($cs in @($sc.commentSources)) {
            if ($cs -and [int]$cs.roundNumber -eq $rn -and $cs.dwTable) { $cmtTbl = [string]$cs.dwTable; break }
        }
        if ($cmtTbl) { break }
    }

    $setParts = New-Object System.Collections.Generic.List[string]
    $applyParts = New-Object System.Collections.Generic.List[string]
    foreach ($sc in $semCols) {
        $appCol = [string]$sc.appColumn
        $exprs = New-Object System.Collections.Generic.List[string]
        foreach ($rs in @($sc.roundSources)) {
            if (-not $rs -or [int]$rs.roundNumber -ne $rn) { continue }
            foreach ($dc in @($rs.dwColumns)) { if ($dc) { [void]$exprs.Add("f.$dc") } }
        }
        foreach ($cs in @($sc.commentSources)) {
            if (-not $cs -or [int]$cs.roundNumber -ne $rn) { continue }
            foreach ($dc in @($cs.dwColumns)) { if ($dc) { [void]$exprs.Add("c.$dc") } }
        }
        if ($exprs.Count -eq 0) { continue }
        $coalesce = if ($exprs.Count -eq 1) { $exprs[0] } else { 'COALESCE(' + ($exprs -join ', ') + ')' }
        [void]$applyParts.Add("    $coalesce AS [$appCol]")
        [void]$setParts.Add("  i.[$appCol] = src.[$appCol]")
    }
    if ($setParts.Count -eq 0) { continue }

    $joinF = if ($fitTbl) {
        "LEFT JOIN $dwRef.[$fitTbl] f ON TRY_CONVERT(INT, f.ProductReferenceID) = fr.StyleSpecId"
    } else {
        'OUTER APPLY (SELECT CAST(NULL AS INT) AS _NoFit) f'
    }
    $joinC = if ($cmtTbl) {
        "LEFT JOIN $dwRef.[$cmtTbl] c ON TRY_CONVERT(INT, c.ProductReferenceID) = fr.StyleSpecId"
    } else {
        'OUTER APPLY (SELECT CAST(NULL AS INT) AS _NoCmt) c'
    }

    [void]$fill.Add("-- Round $rn")
    [void]$fill.Add('UPDATE i SET')
    [void]$fill.Add((($setParts -join ",`r`n") + ','))
    [void]$fill.Add('  i.AppModifiedDate = GETDATE()')
    [void]$fill.Add("FROM dbo.[$friTable] i")
    [void]$fill.Add("INNER JOIN dbo.TchpFitRound fr ON fr.FitRoundId = i.FitRoundId AND fr.RoundNumber = $rn")
    [void]$fill.Add($joinF)
    [void]$fill.Add($joinC)
    [void]$fill.Add('CROSS APPLY (SELECT')
    [void]$fill.Add(($applyParts -join ",`r`n"))
    [void]$fill.Add(') src;')
    [void]$fill.Add("PRINT N'Round $rn Rows=' + CAST(@@ROWCOUNT AS NVARCHAR(20));")
}
Invoke-Sql $TenantDb ($fill -join "`r`n")

Write-Host '3) TX_FitRound Plm_FitRoundInfo fields + Form layout...'
$ctrlMap = @{ DDL = 1; TextBox = 2; Memo = 4; Date = 7; Numeric = 20 }
$entityCodes = @(
    $semCols | Where-Object { $_.entityCode } | ForEach-Object { [string]$_.entityCode } | Select-Object -Unique
)
$entitySql = ($entityCodes | ForEach-Object { "N'$_'" }) -join ','
if (-not $entitySql) { $entitySql = "N'__none__'" }

$metaSql = New-Object System.Collections.Generic.List[string]
[void]$metaSql.Add('SET NOCOUNT ON;')
[void]$metaSql.Add(@"
DECLARE @TxId INT = (SELECT TOP 1 TransactionID FROM dbo.AppTransaction WHERE IntegrationId = N'TX_FitRound');
IF @TxId IS NULL BEGIN RAISERROR(N'TX_FitRound not found', 16, 1); RETURN; END
DECLARE @FormId INT = (SELECT FormID FROM dbo.AppTransaction WHERE TransactionID = @TxId);
DECLARE @UnitId INT = (
  SELECT TOP 1 TransactionUnitID FROM dbo.AppTransactionUnit
  WHERE TransactionID = @TxId AND DataBaseTableName = N'$friTable');
IF @UnitId IS NULL BEGIN RAISERROR(N'FitRoundInfo unit missing', 16, 1); RETURN; END

DECLARE @LayoutJson NVARCHAR(MAX) = N'{"DefaultNbColumns":null,"ColSpanValue":24,"HeightValue":null,"IsUnlimitedHeight":false,"StyleClass":null,"StyleString":null,"IsHideLabel":false,"BackgroundColor":"#ffffff","TextColor":"#000000","LabelWidth":null,"EmUnitLabelPosition":null,"DisplayName":null}';
DECLARE @LayoutSort INT = ISNULL((SELECT MAX(FlowOrGridLayoutSortOrder) FROM dbo.AppFormLayoutItem WHERE FormID = @FormId), 0);

IF OBJECT_ID('tempdb..#Ent') IS NOT NULL DROP TABLE #Ent;
SELECT EntityInfoID, EntityCode INTO #Ent FROM dbo.AppEntityInfo WHERE EntityCode IN ($entitySql);
"@)

foreach ($sc in ($semCols | Sort-Object { [int]$_.sortOrder })) {
    $col = [string]$sc.appColumn
    $disp = if ($sc.displayName) { [string]$sc.displayName } else { $col }
    $ctName = if ($sc.controlType) { [string]$sc.controlType } else { 'TextBox' }
    if ($col -eq 'FitComment') { $ctName = 'Memo' }
    $ct = if ($ctrlMap.ContainsKey($ctName)) { $ctrlMap[$ctName] } else { 2 }
    $sort = if ($sc.sortOrder) { [int]$sc.sortOrder } else { 100 }
    $entCode = if ($sc.entityCode) { [string]$sc.entityCode } else { '' }
    $dispEsc = $disp.Replace("'", "''")
    $entExpr = if ($entCode) { "(SELECT TOP 1 EntityInfoID FROM #Ent WHERE EntityCode = N'$entCode')" } else { 'NULL' }

    [void]$metaSql.Add(@"
IF NOT EXISTS (SELECT 1 FROM dbo.AppTransactionField WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = N'$col')
BEGIN
  INSERT INTO dbo.AppTransactionField (
    TransactionUnitID, DisplayName, DataBaseFieldName, ControlType, SortOrder,
    IsPrimaryKey, IsVisible, IsReadonly, IsAllowEmpty, DisplayWidth, NBDecimal,
    IsLinkToParentPrimaryKey, RowIdentityGuid, AppCreatedDate, AppModifiedDate, EntityId)
  SELECT @UnitId, N'$dispEsc', N'$col', $ct, $sort,
    0, 1, 0, 1, N'150', 0, 0, NEWID(), GETDATE(), GETDATE(), $entExpr;
END
ELSE
BEGIN
  UPDATE tf SET
    DisplayName = N'$dispEsc',
    ControlType = $ct,
    SortOrder = $sort,
    IsVisible = 1,
    EntityId = COALESCE($entExpr, tf.EntityId),
    AppModifiedDate = GETDATE()
  FROM dbo.AppTransactionField tf
  WHERE tf.TransactionUnitID = @UnitId AND tf.DataBaseFieldName = N'$col';
END

IF @FormId IS NOT NULL AND NOT EXISTS (
  SELECT 1 FROM dbo.AppFormLayoutItem li
  INNER JOIN dbo.AppTransactionField tf ON tf.TransactionFieldID = li.TransactionFieldID
  WHERE li.FormID = @FormId AND tf.TransactionUnitID = @UnitId AND tf.DataBaseFieldName = N'$col')
BEGIN
  SET @LayoutSort = @LayoutSort + 1;
  INSERT INTO dbo.AppFormLayoutItem (
    FormID, FlowOrGridLayoutSortOrder, ParameterKeyValue, TransactionFieldID,
    AppCreatedDate, AppModifiedDate, AppCreatedByCompanyID, CurrentHostID, ParentHostID)
  SELECT @FormId, @LayoutSort, @LayoutJson, tf.TransactionFieldID,
    GETDATE(), GETDATE(),
    COALESCE(t.AppCreatedByCompanyID, sample.AppCreatedByCompanyID),
    sample.CurrentHostID, sample.ParentHostID
  FROM dbo.AppTransactionField tf
  CROSS JOIN dbo.AppTransaction t
  OUTER APPLY (
    SELECT TOP 1 li.AppCreatedByCompanyID, li.CurrentHostID, li.ParentHostID
    FROM dbo.AppFormLayoutItem li WHERE li.FormID = @FormId
  ) sample
  WHERE tf.TransactionUnitID = @UnitId AND tf.DataBaseFieldName = N'$col' AND t.TransactionID = @TxId;
END
"@)
}

[void]$metaSql.Add(@"
UPDATE tf SET IsVisible = 0, AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField tf
WHERE tf.TransactionUnitID = @UnitId AND tf.DataBaseFieldName = N'FitRoundId';

SELECT COUNT(*) AS VisibleFields FROM dbo.AppTransactionField WHERE TransactionUnitID = @UnitId AND IsVisible = 1;
SELECT COUNT(*) AS FriLayoutItems
FROM dbo.AppFormLayoutItem li
INNER JOIN dbo.AppTransactionField tf ON tf.TransactionFieldID = li.TransactionFieldID
WHERE li.FormID = @FormId AND tf.TransactionUnitID = @UnitId;
"@)
Invoke-Sql $TenantDb ($metaSql -join "`r`n")

Write-Host '4) Verify StyleSpec 31614:'
Invoke-Sql $TenantDb @"
SET NOCOUNT ON;
SELECT fr.RoundNumber, i.SampleType, i.SampleStatus, i.State, i.Factory, i.FitTechnician, i.Model,
       LEFT(i.FitComment, 40) AS FitComment
FROM dbo.Plm_FitRoundInfo i
INNER JOIN dbo.TchpFitRound fr ON fr.FitRoundId = i.FitRoundId
WHERE fr.StyleSpecId = 31614
ORDER BY fr.RoundNumber;
"@

Write-Host 'Done.'
