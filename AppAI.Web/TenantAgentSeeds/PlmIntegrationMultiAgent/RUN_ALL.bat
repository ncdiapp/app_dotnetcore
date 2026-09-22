@echo off
REM Apply PLM Migration Multi-Agent seeds to a NEW tenant DB (structure-migrated through V031+).
REM Usage: RUN_ALL.bat ServerName TenantDbName
REM Example: RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34
REM Uses Windows auth (-E). For SQL auth, edit to add -U/-P.
REM -f 65001 = UTF-8 input (avoids mojibake). Prefer ASCII-only in seed text when possible.
REM New-tenant only: INSERT agents/libraries; does not UPDATE existing agents.

if "%~1"=="" goto usage
if "%~2"=="" goto usage

set SERVER=%~1
set DB=%~2
set HERE=%~dp0

echo === Applying to [%SERVER%] / [%DB%] ===
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%01_Seed_IntegrationPlmImportLibrary.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%02_Seed_PlmIntegrationImportDw_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%03_Seed_PlmIntegrationOrchestrator_Root.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%99_Verify.sql" || goto fail
echo === DONE ===
goto end

:usage
echo Usage: RUN_ALL.bat ServerName TenantDbName
exit /b 1

:fail
echo FAILED. See messages above.
exit /b 1

:end
