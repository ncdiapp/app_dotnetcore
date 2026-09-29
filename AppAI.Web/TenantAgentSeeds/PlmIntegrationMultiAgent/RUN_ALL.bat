@echo off
setlocal EnableExtensions
REM Apply PLM Migration Multi-Agent seeds to a NEW tenant DB (structure through V034+).
REM Usage: RUN_ALL.bat ServerName TenantDbName [CompanyId] [FileRepositoryRoot]
REM Example: RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34 1
REM From PowerShell prefer: cmd /c RUN_ALL.bat "PC3B\MSSQLSERVER01" TenantDB_PLM34 1

if "%~1"=="" goto usage
if "%~2"=="" goto usage

set "SERVER=%~1"
set "DB=%~2"
set "HERE=%~dp0"

if "%~3"=="" echo NOTE: No CompanyId - SQL only. For Default Source descriptions add CompanyId as 3rd argument.

echo === Applying to [%SERVER%] / [%DB%] ===
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%01_Seed_IntegrationPlmImportLibrary.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%02_Seed_PlmIntegrationImportDw_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%03_Seed_PlmIntegrationOrchestrator_Root.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%04_Seed_PlmIntegrationEntity_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%05_Seed_PlmIntegrationFolder_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%06_Seed_PlmIntegrationImage_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%07_Seed_PlmIntegrationColor_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%08_Seed_PlmIntegrationPom_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%09_Seed_PlmIntegrationSearch_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%10_Seed_PlmIntegrationMassUpdate_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%11_Seed_ChildMappings.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%99_Verify.sql" || goto fail

if "%~3"=="" goto done
echo === Copy AgentStarter files ===
if "%~4"=="" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%HERE%CopyAgentStarter.ps1" -CompanyId %3
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%HERE%CopyAgentStarter.ps1" -CompanyId %3 -FileRepositoryRoot "%~4"
)
if errorlevel 1 goto fail

:done
echo === DONE ===
exit /b 0

:usage
echo Usage: RUN_ALL.bat ServerName TenantDbName [CompanyId] [FileRepositoryRoot]
echo Example: RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34 1
exit /b 1

:fail
echo FAILED. See messages above.
exit /b 1
