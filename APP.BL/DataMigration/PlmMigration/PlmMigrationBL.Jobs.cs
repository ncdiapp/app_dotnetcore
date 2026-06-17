using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private static readonly ConcurrentDictionary<int, CancellationTokenSource> JobCancelTokens =
            new ConcurrentDictionary<int, CancellationTokenSource>();

        internal sealed class PlmJobRuntimeContext
        {
            public int SessionId { get; set; }
            public int JobId { get; set; }
            public int TenantDataSourceId { get; set; }
            public string PlmConnectionString { get; set; }
            public string TenantConnectionString { get; set; }
        }

        internal static PlmJobRuntimeContext BuildJobRuntimeContext(int sessionId, int jobId)
        {
            var fixture = GetTenantFixture();
            var session = LoadSessionById(fixture, sessionId, includeConnection: true);
            if (session == null)
                throw new InvalidOperationException("Import session not found.");

            if (string.IsNullOrWhiteSpace(session.PlmConnectionString))
                throw new InvalidOperationException("PLM connection is not available on this session.");

            int tenantDataSourceId = GetTenantDataSourceId();
            var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(tenantDataSourceId);
            if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
                throw new InvalidOperationException("Tenant database connection is not available.");

            string tenantConn = AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);

            return new PlmJobRuntimeContext
            {
                SessionId = sessionId,
                JobId = jobId,
                TenantDataSourceId = tenantDataSourceId,
                PlmConnectionString = session.PlmConnectionString.Trim(),
                TenantConnectionString = tenantConn
            };
        }

        internal static int CreateQueuedJob(DatabaseFixture fixture, int sessionId, string jobType, string progressMessage)
        {
            var now = DateTime.UtcNow;
            const string sql = @"
INSERT INTO dbo.AppPlmImportJob
    (SessionId, JobType, Status, ProgressPercent, ProgressMessage, CreatedAt, UpdatedAt, StartedAt)
VALUES
    (@SessionId, @JobType, @Status, 0, @ProgressMessage, @CreatedAt, @UpdatedAt, NULL);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var jobIdObj = fixture.RetriveScalar(sql, new List<DbParameter>
            {
                CreateParam(fixture, "@SessionId", sessionId),
                CreateParam(fixture, "@JobType", jobType),
                CreateParam(fixture, "@Status", JobStatusQueued),
                CreateParam(fixture, "@ProgressMessage", progressMessage),
                CreateParam(fixture, "@CreatedAt", now),
                CreateParam(fixture, "@UpdatedAt", now)
            });
            return Convert.ToInt32(jobIdObj);
        }

        internal static void UpdateJobProgress(
            DatabaseFixture fixture, int jobId, string status, int progressPercent,
            string progressMessage, string resultJson = null, string errorMessage = null, bool markCompleted = false)
        {
            var sb = new StringBuilder(@"
UPDATE dbo.AppPlmImportJob SET
    Status = @Status,
    ProgressPercent = @ProgressPercent,
    ProgressMessage = @ProgressMessage,
    UpdatedAt = @UpdatedAt");
            if (resultJson != null) sb.Append(", ResultJson = @ResultJson");
            if (errorMessage != null) sb.Append(", ErrorMessage = @ErrorMessage");
            if (status == JobStatusRunning) sb.Append(", StartedAt = COALESCE(StartedAt, @UpdatedAt)");
            if (markCompleted) sb.Append(", CompletedAt = @UpdatedAt");
            sb.Append(" WHERE JobId = @JobId");

            var parms = new List<DbParameter>
            {
                CreateParam(fixture, "@Status", status),
                CreateParam(fixture, "@ProgressPercent", progressPercent),
                CreateParam(fixture, "@ProgressMessage", (object)progressMessage ?? DBNull.Value),
                CreateParam(fixture, "@UpdatedAt", DateTime.UtcNow),
                CreateParam(fixture, "@JobId", jobId)
            };
            if (resultJson != null)
                parms.Add(CreateParam(fixture, "@ResultJson", resultJson));
            if (errorMessage != null)
                parms.Add(CreateParam(fixture, "@ErrorMessage", errorMessage));

            fixture.ExecuteNonQueryResult(sb.ToString(), parms);
        }

        internal static bool IsJobCancellationRequested(int jobId)
        {
            return JobCancelTokens.TryGetValue(jobId, out var cts) && cts.IsCancellationRequested;
        }

        internal static void RegisterJobCancellation(int jobId)
        {
            JobCancelTokens[jobId] = new CancellationTokenSource();
        }

        internal static void ClearJobCancellation(int jobId)
        {
            if (JobCancelTokens.TryRemove(jobId, out var cts))
                cts.Dispose();
        }

        internal static void RequestJobCancellation(int jobId)
        {
            if (JobCancelTokens.TryGetValue(jobId, out var cts))
                cts.Cancel();
        }

        internal static void RunJobInBackground(Action<PlmJobRuntimeContext> work, PlmJobRuntimeContext context)
        {
            RegisterJobCancellation(context.JobId);

            // Background Task.Run threads have no HttpContext; ServerContext falls back to
            // WindowsIdentityProvider which requires RegisterIdentity (same pattern as AppBuilderAgentBL).
            var requestIdentity = ServerContext.Instance?.CurrnetClientIdentity;
            if (requestIdentity is not AppClientIdentity appIdentity)
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
                const string msg = "PLM import job cannot start: user session identity is not available on the request thread.";
                UpdateJobProgress(fixture, context.JobId, JobStatusFailed, 100, "Job failed.", errorMessage: msg, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity, "RunJob", "Failed", null, null, null, null, msg);
                ClearJobCancellation(context.JobId);
                return;
            }

            AppClientIdentity capturedIdentity = appIdentity;

            Task.Run(() =>
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
                try
                {
                    if (ServerContext.Instance.WindowsIdentityProvider == null)
                        throw new InvalidOperationException("WindowsIdentityProvider is not initialized.");

                    ServerContext.Instance.WindowsIdentityProvider.RegisterIdentity(capturedIdentity);

                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, 0, "Job started.");
                    work(context);
                }
                catch (Exception ex)
                {
                    UpdateJobProgress(
                        fixture, context.JobId, JobStatusFailed, 100, "Job failed.",
                        errorMessage: ex.Message, markCompleted: true);
                    WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                        "RunJob", "Failed", null, null, null, null, ex.Message);
                }
                finally
                {
                    ClearJobCancellation(context.JobId);
                }
            });
        }

        public static OperationCallResult<PlmImportJobDto> StartPlmTableExportJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypePlmTableExport, "Queued PLM table import.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepEntity, "PlmTableExport", "Running", null, null, null, null, "PLM table import job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunPlmTableExportJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_TableExport_Start_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        private static void RunPlmTableExportJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var session = LoadSessionById(fixture, context.SessionId, includeConnection: false);
            string tablePrefix = ResolveImportPrefixes(session?.StepStateJson).TablePrefix;

            var exportResult = ExportPlmTablesToTenant(
                context.PlmConnectionString,
                context.TenantConnectionString,
                tablePrefix,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Export cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(exportResult);
            if (exportResult.Issues?.Count > 0)
            {
                WritePlmTableExportIssuesToLog(
                    fixture, context.SessionId, context.JobId, PlmExportActionExport, "Warning", exportResult.Issues);
            }

            if (!exportResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "Import completed with errors.",
                    resultJson: resultJson, errorMessage: exportResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                    PlmExportActionExport, "Failed", null, null, null, null, exportResult.ErrorMessage);
                return;
            }

            int? totalRows = exportResult.Tables?.Sum(t => t.RowsCopied);
            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "Import completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                "PlmTableExport", "Success", null, null, totalRows, null,
                "PLM tables imported to tenant database.");
        }

        public static OperationCallResult<PlmImportJobDto> StartSystemDefineEntityImportJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypeSystemDefineEntityImport,
                    "Queued System Define entity metadata import.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepEntity, PlmSysDefineActionImport, "Running",
                    null, null, null, null, "System Define entity metadata import job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunSystemDefineEntityImportJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SystemDefine_Execute_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<PlmImportJobDto> StartUserDefineEntityImportJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypeUserDefineEntityImport,
                    "Queued User Define entity import.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepEntity, PlmUserDefineActionImport, "Running",
                    null, null, null, null, "User Define entity import job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunUserDefineEntityImportJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_UserDefine_Execute_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        private static void RunUserDefineEntityImportJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var session = LoadSessionById(fixture, context.SessionId, includeConnection: false);

            var importResult = ImportUserDefineEntities(
                context.PlmConnectionString,
                context.TenantConnectionString,
                context.TenantDataSourceId,
                session?.SaasApplicationId,
                ResolveImportPrefixes(session?.StepStateJson).EntityWideTablePrefix,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Import cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(importResult);

            if (importResult.SkippedEntities?.Count > 0 || importResult.Blockers?.Count > 0)
            {
                WriteUserDefineEntityIssuesToLog(
                    fixture, context.SessionId, context.JobId, PlmUserDefineActionImport, "Warning",
                    importResult.SkippedEntities, importResult.Blockers);
            }

            if (!importResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "User Define entity import failed.",
                    resultJson: resultJson, errorMessage: importResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                    PlmUserDefineActionImport, "Failed", null, null, null, null, importResult.ErrorMessage);
                return;
            }

            int? totalAffected = importResult.InsertedCount + importResult.UpdatedCount;
            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "User Define entity import completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                PlmUserDefineActionImport, "Success", null, null, totalAffected, null,
                $"Inserted {importResult.InsertedCount}, updated {importResult.UpdatedCount}, skipped {importResult.SkippedCount}, rows {importResult.RowsImported}.");
        }

        private static void RunSystemDefineEntityImportJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var session = LoadSessionById(fixture, context.SessionId, includeConnection: false);

            var importResult = ImportSystemDefineEntities(
                context.PlmConnectionString,
                session?.DataSourceDiscoveryJson,
                context.TenantConnectionString,
                session?.SaasApplicationId,
                ResolveImportPrefixes(session?.StepStateJson).TablePrefix,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Import cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(importResult);

            if (importResult.SkippedEntities?.Count > 0 || importResult.Blockers?.Count > 0)
            {
                WriteSystemDefineEntityIssuesToLog(
                    fixture, context.SessionId, context.JobId, PlmSysDefineActionImport, "Warning",
                    importResult.SkippedEntities, importResult.Blockers);
            }

            if (!importResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "Entity metadata import failed.",
                    resultJson: resultJson, errorMessage: importResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                    PlmSysDefineActionImport, "Failed", null, null, null, null, importResult.ErrorMessage);
                return;
            }

            int? totalAffected = importResult.InsertedCount + importResult.UpdatedCount;
            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "Entity metadata import completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepEntity,
                PlmSysDefineActionImport, "Success", null, null, totalAffected, null,
                $"Inserted {importResult.InsertedCount}, updated {importResult.UpdatedCount}, skipped {importResult.SkippedCount}.");
        }

        public static OperationCallResult<PlmImportJobDto> StartTemplateImportJob(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                int jobId = CreateQueuedJob(fixture, sessionId.Value, JobTypeTemplateImport,
                    "Queued template structure import.");
                WriteImportLog(fixture, sessionId.Value, jobId, StepTemplate, PlmTemplateActionImport, "Running",
                    null, null, null, null, "Template structure import job queued.");

                var context = BuildJobRuntimeContext(sessionId.Value, jobId);
                RunJobInBackground(RunTemplateImportJob, context);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Execute_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        private static void RunTemplateImportJob(PlmJobRuntimeContext context)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(context.TenantDataSourceId);
            var session = LoadSessionById(fixture, context.SessionId, includeConnection: false);

            var importResult = ImportTemplateStructure(
                context.PlmConnectionString,
                context.TenantConnectionString,
                context.TenantDataSourceId,
                session?.SaasApplicationId,
                ResolveImportPrefixes(session?.StepStateJson).TablePrefix,
                (percent, message) =>
                {
                    if (IsJobCancellationRequested(context.JobId))
                        throw new OperationCanceledException("Import cancelled.");
                    UpdateJobProgress(fixture, context.JobId, JobStatusRunning, percent, message);
                });

            string resultJson = JsonConvert.SerializeObject(importResult);

            if (importResult.SkippedTabs?.Count > 0 || importResult.Blockers?.Count > 0 || importResult.Warnings?.Count > 0)
            {
                WriteTemplateIssuesToLog(
                    fixture, context.SessionId, context.JobId, PlmTemplateActionImport, "Warning",
                    importResult.SkippedTabs, importResult.Blockers, importResult.Warnings);
            }

            if (!importResult.IsSuccess)
            {
                UpdateJobProgress(
                    fixture, context.JobId, JobStatusFailed, 100, "Template import failed.",
                    resultJson: resultJson, errorMessage: importResult.ErrorMessage, markCompleted: true);
                WriteImportLog(fixture, context.SessionId, context.JobId, StepTemplate,
                    PlmTemplateActionImport, "Failed", null, null, null, null, importResult.ErrorMessage);
                return;
            }

            int? totalAffected = importResult.TabsInserted + importResult.TabsUpdated;
            UpdateJobProgress(
                fixture, context.JobId, JobStatusCompleted, 100, "Template import completed successfully.",
                resultJson: resultJson, markCompleted: true);
            WriteImportLog(fixture, context.SessionId, context.JobId, StepTemplate,
                PlmTemplateActionImport, "Success", null, null, totalAffected, null,
                $"Templates {importResult.TemplatesProcessed}, tabs inserted {importResult.TabsInserted}, updated {importResult.TabsUpdated}, skipped {importResult.TabsSkipped}.");
        }

        public static OperationCallResult<PlmTableExportPlanDto> PreviewPlmTableExportPlan(int? sessionId)
        {
            var result = new OperationCallResult<PlmTableExportPlanDto> { Object = new PlmTableExportPlanDto() };
            try
            {
                RequirePlmMigrationAdmin();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null || string.IsNullOrWhiteSpace(session.PlmConnectionString))
                    throw new InvalidOperationException("PLM connection is not available on this session.");

                result.Object = BuildPlmTableExportPlan(
                    session.PlmConnectionString.Trim(),
                    ResolveImportPrefixes(session.StepStateJson).TablePrefix);
                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_TableExport_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else
                {
                    if (result.Object.Issues?.Count > 0)
                    {
                        WritePlmTableExportIssuesToLog(
                            fixture, sessionId.Value, null, PlmExportActionPreview, "Warning", result.Object.Issues);
                        result.ValidationResult.Items.Add(new ValidationItem(
                            typeof(PlmMigrationBL), "Plm_TableExport_Preview_Warning", ValidationItemType.Warning,
                            result.Object.ErrorMessage));
                    }
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_TableExport_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }
    }
}
