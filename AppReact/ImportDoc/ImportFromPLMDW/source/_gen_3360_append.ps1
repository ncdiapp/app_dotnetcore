$ErrorActionPreference = 'Stop'
$src = 'c:\DevAppAI\AppAIClean\AppReact\ImportDoc\ImportFromPLMDW\source'
$outRoot = 'c:\DevAppAI\AppAIClean\AppReact\ImportDoc\ImportFromPLMDW\output'
Copy-Item (Join-Path $src 'dwTabImportConfig.json') (Join-Path $src 'dwTabImportConfig.3351.json') -Force
Copy-Item (Join-Path $src 'dwTabImportConfig.3360.append.json') (Join-Path $src 'dwTabImportConfig.json') -Force
try {
    & (Join-Path $src '_gen_plmdw_import_sql.ps1')
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "3360 gen failed exit=$LASTEXITCODE" }
}
finally {
    Copy-Item (Join-Path $src 'dwTabImportConfig.3351.json') (Join-Path $src 'dwTabImportConfig.json') -Force
    Write-Host 'Restored dwTabImportConfig.json to 3351'
}
Get-ChildItem (Join-Path $outRoot '3360') | Select-Object Name, Length | Format-Table -AutoSize
