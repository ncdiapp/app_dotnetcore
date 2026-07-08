$ErrorActionPreference = 'Stop'
$src = 'c:\DevAppAI\AppAIClean\AppReact\ImportDoc\ImportFromPLMDW\source'
Copy-Item (Join-Path $src 'dwTabImportConfig.json') (Join-Path $src 'dwTabImportConfig.3351.json') -Force
Copy-Item (Join-Path $src 'dwTabImportConfig.3361.json') (Join-Path $src 'dwTabImportConfig.json') -Force
try {
    & (Join-Path $src '_gen_plmdw_import_sql.ps1')
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "3361 gen failed: $LASTEXITCODE" }
}
finally {
    Copy-Item (Join-Path $src 'dwTabImportConfig.3351.json') (Join-Path $src 'dwTabImportConfig.json') -Force
    Write-Host 'Restored 3351 config'
}
Get-ChildItem 'c:\DevAppAI\AppAIClean\AppReact\ImportDoc\ImportFromPLMDW\output\3361' | Format-Table Name, Length -AutoSize
