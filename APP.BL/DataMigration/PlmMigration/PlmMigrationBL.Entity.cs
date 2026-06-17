using System;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public static OperationCallResult<PlmSystemDefineEntityPreviewDto> PreviewSystemDefineEntityImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmSystemDefineEntityPreviewDto>
            {
                Object = new PlmSystemDefineEntityPreviewDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null || string.IsNullOrWhiteSpace(session.PlmConnectionString))
                    throw new InvalidOperationException("PLM connection is not available on this session.");

                var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(GetTenantDataSourceId());
                if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
                    throw new InvalidOperationException("Tenant database connection is not available.");

                string tenantConn = AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);
                result.Object = BuildSystemDefineEntityPreview(
                    session.PlmConnectionString.Trim(),
                    session.DataSourceDiscoveryJson,
                    tenantConn);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_SystemDefine_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else if (result.Object.BlockerCount > 0)
                {
                    WriteSystemDefineEntityIssuesToLog(
                        fixture, sessionId.Value, null, PlmSysDefineActionPreview, "Warning",
                        null, result.Object.Blockers);
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_SystemDefine_Preview_Blocker", ValidationItemType.Warning,
                        $"{result.Object.BlockerCount} entity blocker(s) found. Resolve EntityCode conflicts before import."));
                }
                else if (result.Object.SkippedCount > 0)
                {
                    WriteSystemDefineEntityIssuesToLog(
                        fixture, sessionId.Value, null, PlmSysDefineActionPreview, "Warning",
                        result.Object.Entities.Where(e => e.ImportStatus == "Skipped").ToList(), null);
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SystemDefine_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecuteSystemDefineEntityImport(int? sessionId)
        {
            return StartSystemDefineEntityImportJob(sessionId);
        }

        public static OperationCallResult<PlmUserDefineEntityPreviewDto> PreviewUserDefineEntityImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmUserDefineEntityPreviewDto>
            {
                Object = new PlmUserDefineEntityPreviewDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null || string.IsNullOrWhiteSpace(session.PlmConnectionString))
                    throw new InvalidOperationException("PLM connection is not available on this session.");

                var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(GetTenantDataSourceId());
                if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
                    throw new InvalidOperationException("Tenant database connection is not available.");

                string tenantConn = AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);
                var prefixes = ResolveImportPrefixes(session.StepStateJson);
                result.Object = BuildUserDefineEntityPreview(
                    session.PlmConnectionString.Trim(),
                    session.DataSourceDiscoveryJson,
                    tenantConn,
                    GetTenantDataSourceId(),
                    prefixes.EntityWideTablePrefix);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_UserDefine_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else if (result.Object.BlockerCount > 0)
                {
                    WriteUserDefineEntityIssuesToLog(
                        fixture, sessionId.Value, null, PlmUserDefineActionPreview, "Warning",
                        null, result.Object.Blockers);
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_UserDefine_Preview_Blocker", ValidationItemType.Warning,
                        $"{result.Object.BlockerCount} entity blocker(s) found. Resolve conflicts before import."));
                }
                else if (result.Object.SkippedCount > 0)
                {
                    WriteUserDefineEntityIssuesToLog(
                        fixture, sessionId.Value, null, PlmUserDefineActionPreview, "Warning",
                        result.Object.Entities.Where(e => e.ImportStatus == "Skipped").ToList(), null);
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_UserDefine_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecuteUserDefineEntityImport(int? sessionId)
        {
            return StartUserDefineEntityImportJob(sessionId);
        }

        private static OperationCallResult<PlmImportJobDto> ExecuteStubJob(int? sessionId, string jobType, string message)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                var fixture = GetTenantFixture();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                int jobId = CreateStubJob(fixture, sessionId.Value, jobType, message);
                WriteImportLog(fixture, sessionId.Value, jobId, StepEntity, jobType, "Failed", null, null, null, null, message);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Entity_Execute_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }
    }
}
