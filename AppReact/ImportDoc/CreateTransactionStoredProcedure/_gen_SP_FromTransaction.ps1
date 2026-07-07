# Generates a report stored procedure from AppTransaction metadata.
# Usage:
#   .\ _gen_SP_FromTransaction.ps1 -TransactionId 2256
#   .\ _gen_SP_FromTransaction.ps1 -TransactionId 2256 -Deploy

param(
    [Parameter(Mandatory = $true)]
    [int]$TransactionId,

    [string]$Server = "PC3B\MSSQLSERVER01",
    [string]$TenantDb = "TenantDB_PLM26",
    [string]$ErpDb = "SourceERP",
    [int]$TenantDataSourceId = 1070,
    [int]$ErpDataSourceId = 1071,
    [string]$User = "sa",
    [string]$Password = "appsa",
    [switch]$Deploy
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot
$outputDir = Join-Path $scriptRoot "OUTPUT"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$connStr = "Server=$Server;Database=$TenantDb;User ID=$User;Password=$Password;Encrypt=False;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Invoke-Query([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    return ,$table
}

function Get-SpName([string]$transactionName) {
    $clean = ($transactionName -replace '[^A-Za-z0-9]', '')
    if ([string]::IsNullOrWhiteSpace($clean)) { return "SP_Transaction_$TransactionId" }
    return "SP_$clean"
}

# Report Engine token names: only [A-Za-z0-9_] (see AppReportTemplateService.RenderScalars)
function Get-TokenAlias([string]$displayName, [string]$unitName, [hashtable]$used) {
    $token = ($displayName -replace '[^A-Za-z0-9]', '')
    if ([string]::IsNullOrWhiteSpace($token)) { $token = 'Field' }
    if ($token -match '^\d') { $token = "_$token" }
    if ($used.ContainsKey($token)) {
        $prefix = ($unitName -replace '[^A-Za-z0-9]', '')
        if (-not [string]::IsNullOrWhiteSpace($prefix)) {
            $token = "${prefix}_$token"
        }
    }
    $base = $token
    $n = 2
    while ($used.ContainsKey($token)) {
        $token = "${base}_$n"
        $n++
    }
    $used[$token] = $true
    return $token
}

$txnSql = "SELECT TransactionID, TransactionName, Description FROM AppTransaction WHERE TransactionID = $TransactionId"
$txn = Invoke-Query $txnSql
if ($txn.Rows.Count -eq 0) {
    throw "Transaction $TransactionId not found in $TenantDb"
}

$transactionName = $txn.Rows[0].TransactionName
$spName = Get-SpName $transactionName

$unitsSql = @"
SELECT
    tu.TransactionUnitID,
    tu.UnitDisplayName,
    tu.DataBaseTableName,
    tu.ParentTransactionUnitID,
    pk.DataBaseFieldName AS PKField
FROM AppTransactionUnit tu
OUTER APPLY (
    SELECT TOP 1 DataBaseFieldName
    FROM AppTransactionField tf
    WHERE tf.TransactionUnitID = tu.TransactionUnitID
      AND tf.IsPrimaryKey = 1
    ORDER BY tf.SortOrder
) pk
WHERE tu.TransactionID = $TransactionId
  AND tu.ParentTransactionUnitID IS NULL
ORDER BY tu.TransactionUnitID
"@

$units = Invoke-Query $unitsSql
if ($units.Rows.Count -eq 0) {
    throw "No level-1 units found for Transaction $TransactionId"
}

$unitAliases = @{}
$unitIdToAlias = @{}
$idx = 0
foreach ($unit in $units.Rows) {
    $idx++
    $alias = "u$idx"
    $unitIdToAlias[[int]$unit.TransactionUnitID] = $alias
    $unitAliases[$alias] = @{
        TableName = $unit.DataBaseTableName
        PKField   = $unit.PKField
        UnitName  = $unit.UnitDisplayName
    }
}

$fromParts = New-Object System.Collections.Generic.List[string]
$firstAlias = "u1"
$firstTable = $units.Rows[0].DataBaseTableName
$fromParts.Add("dbo.[$firstTable] AS [$firstAlias]")

for ($i = 1; $i -lt $units.Rows.Count; $i++) {
    $row = $units.Rows[$i]
    $alias = "u$($i + 1)"
    $table = $row.DataBaseTableName
    $pk = $row.PKField
    $basePk = $units.Rows[0].PKField
    if ([string]::IsNullOrWhiteSpace($pk)) { $pk = $basePk }
    if ([string]::IsNullOrWhiteSpace($basePk)) { $basePk = "ReferenceId" }
    $fromParts.Add("INNER JOIN dbo.[$table] AS [$alias] ON [$alias].[$pk] = [$firstAlias].[$basePk]")
}

$whereField = if ($unitAliases[$firstAlias].PKField) { $unitAliases[$firstAlias].PKField } else { "ReferenceId" }

$fieldsSql = @"
SELECT
    tu.TransactionUnitID,
    tu.UnitDisplayName,
    tu.DataBaseTableName,
    tf.DisplayName,
    tf.DataBaseFieldName,
    tf.ControlType,
    tf.EntityID,
    tf.SortOrder,
    ei.EntityType,
    ei.TableName       AS EntityTable,
    ei.IdentityField,
    ei.DisplayFiled1,
    ei.DataSourceFrom
FROM AppTransactionField tf
JOIN AppTransactionUnit tu ON tu.TransactionUnitID = tf.TransactionUnitID
LEFT JOIN AppEntityInfo ei ON ei.EntityInfoID = tf.EntityID
WHERE tu.TransactionID = $TransactionId
  AND tu.ParentTransactionUnitID IS NULL
ORDER BY tu.TransactionUnitID, tf.SortOrder
"@

$fields = Invoke-Query $fieldsSql

$selectCols = New-Object System.Collections.Generic.List[string]
$fallbackCols = New-Object System.Collections.Generic.List[string]
$joins = New-Object System.Collections.Generic.List[string]
$usedTokenAliases = @{}
$aliasCounter = 0

function Get-SafeAlias([string]$baseName) {
    $script:aliasCounter++
    $clean = ($baseName -replace '[^A-Za-z0-9_]', '_')
    if ($clean -match '^\d') { $clean = "_$clean" }
    return "${clean}_$($script:aliasCounter)"
}

foreach ($row in $fields.Rows) {
    $col = $row.DataBaseFieldName
    $tokenAlias = Get-TokenAlias $row.DisplayName $row.UnitDisplayName $usedTokenAliases

    $unitId = [int]$row.TransactionUnitID
    $baseTableAlias = $unitIdToAlias[$unitId]
    $controlType = [int]$row.ControlType
    $entityId = if ($row.EntityID -is [DBNull]) { $null } else { [int]$row.EntityID }
    $entityType = if ($row.EntityType -is [DBNull]) { $null } else { [int]$row.EntityType }
    $entityTable = $row.EntityTable
    $identityField = $row.IdentityField
    $displayField1 = $row.DisplayFiled1
    $dataSourceFrom = if ($row.DataSourceFrom -is [DBNull]) { $null } else { [int]$row.DataSourceFrom }

    if ($controlType -eq 1 -and $entityId -and $entityType) {
        $alias = Get-SafeAlias $col
        if ($entityType -eq 4) {
            $joins.Add("LEFT JOIN dbo.AppEntitySimpleListValue AS [$alias] ON [$alias].EntityInfoID = $entityId AND [$alias].InternalKey = $baseTableAlias.[$col]")
            $selectCols.Add("[$alias].Code AS [$tokenAlias]")
        }
        elseif ($entityType -eq 1 -and $entityTable -and $identityField -and $displayField1) {
            $dbName = if ($dataSourceFrom -eq $ErpDataSourceId) { $ErpDb } else { $TenantDb }
            $joins.Add("LEFT JOIN [$dbName].dbo.[$entityTable] AS [$alias] ON [$alias].[$identityField] = $baseTableAlias.[$col]")
            $selectCols.Add("[$alias].[$displayField1] AS [$tokenAlias]")
        }
        else {
            $selectCols.Add("$baseTableAlias.[$col] AS [$tokenAlias]")
        }
    }
    elseif ($controlType -eq 13) {
        $selectCols.Add("CASE WHEN $baseTableAlias.[$col] IN ('1','Y','True','true') THEN 'Yes' WHEN $baseTableAlias.[$col] IN ('0','N','False','false') THEN 'No' ELSE CAST($baseTableAlias.[$col] AS NVARCHAR(MAX)) END AS [$tokenAlias]")
    }
    else {
        $selectCols.Add("$baseTableAlias.[$col] AS [$tokenAlias]")
    }

    # Token-discovery fallback: GetAvailableTokens calls SP with @MainReferenceId = 0
    $fallbackCols.Add("CAST(NULL AS NVARCHAR(MAX)) AS [$tokenAlias]")
}

$selectBlock = ($selectCols -join ",`n        ")
$fallbackBlock = ($fallbackCols -join ",`n        ")
$fromBlock = ($fromParts -join "`n    ")
$joinBlock = if ($joins.Count -gt 0) { "`n    " + ($joins -join "`n    ") } else { "" }

$sp = @"
-- ============================================================
-- $spName
-- Transaction $TransactionId ($transactionName) report data source
-- Generated from AppTransactionField / AppEntityInfo metadata
-- Tenant DB: $TenantDb
-- ERP lookup DB: $ErpDb (AppDataSourceRegister id $ErpDataSourceId)
-- Generator: AppReact/ImportDoc/CreateTransactionStoredProcedure/_gen_SP_FromTransaction.ps1
-- ============================================================
-- Report Engine contract: RS0 = header (1 row), tokens {{header.ColumnAlias}}
-- Parameters must match AppReportTemplateService.FetchData
-- When no row matches @MainReferenceId, returns one NULL row so the designer
-- can discover column tokens (GetAvailableTokens uses @MainReferenceId = 0).
-- ============================================================
IF OBJECT_ID('dbo.$spName', 'P') IS NOT NULL
    DROP PROCEDURE dbo.$spName;
GO

CREATE PROCEDURE dbo.$spName
    @MainReferenceId    INT,
    @MasterReferenceId  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM $fromBlock$joinBlock
        WHERE [$firstAlias].[$whereField] = @MainReferenceId
    )
    BEGIN
        -- Token-discovery fallback for Report Template Designer (Ref ID = 0)
        SELECT
            $fallbackBlock;
        RETURN;
    END

    -- Result set 1: header (exactly 1 row for {{header.*}} tokens)
    SELECT
        $selectBlock
    FROM $fromBlock$joinBlock
    WHERE [$firstAlias].[$whereField] = @MainReferenceId;
END
GO
"@

$outPath = Join-Path $outputDir "$spName.sql"
$sp | Set-Content -Path $outPath -Encoding UTF8
Write-Host "Generated: $outPath"

if ($Deploy) {
    sqlcmd -S $Server -U $User -P $Password -d $TenantDb -i $outPath | Out-Host
    Write-Host "Deployed to $TenantDb"
}

$conn.Close()
