@echo off
REM Apply PLM Migration Multi-Agent seeds to a NEW / restored tenant DB (structure through V031+).
REM Usage: RUN_ALL.bat ServerName TenantDbName
REM Example: RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34
REM Windows auth (-E). SQL auth: add -U/-P.
REM -f 65001 = UTF-8. Scripts are INSERT (IF NOT EXISTS); 03 also UPDATEs ROOT prompt.
REM ROOT MaxIterations = 400 (long Image/Folder job polling without hitting tool-call cap as often).

if "%~1"=="" goto usage
if "%~2"=="" goto usage

set SERVER=%~1
set DB=%~2
set HERE=%~dp0

echo === Applying to [%SERVER%] / [%DB%] ===
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%01_Seed_IntegrationPlmImportLibrary.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%02_Seed_PlmIntegrationImportDw_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%03_Seed_PlmIntegrationOrchestrator_Root.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%04_Seed_PlmIntegrationEntity_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%05_Seed_PlmIntegrationFolder_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%06_Seed_PlmIntegrationImage_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%07_Seed_PlmIntegrationColor_Child.sql" || goto fail
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -f 65001 -i "%HERE%08_Seed_PlmIntegrationPom_Child.sql" || goto fail
echo === Ensure ROOT MaxIterations=400 ===
sqlcmd -S "%SERVER%" -d "%DB%" -E -b -Q "UPDATE dbo.AppAgentSkillSet SET MaxIterations = 400 WHERE SkillKey = N'plm-integration-orchestrator'; SELECT SkillKey, MaxIterations FROM dbo.AppAgentSkillSet WHERE SkillKey = N'plm-integration-orchestrator';" || goto fail
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
