using System;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public static OperationCallResult<object> PreviewUserDefineEntityImport(int? sessionId)
        {
            var result = new OperationCallResult<object>();
            try
            {
                RequirePlmMigrationAdmin();
                GetTenantFixture();
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_UserDefine_Preview_NotImplemented", ValidationItemType.Warning,
                    "User Define entity preview is not implemented yet (Phase 4)."));
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_UserDefine_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<object> PreviewSystemDefineEntityImport(int? sessionId)
        {
            var result = new OperationCallResult<object>();
            try
            {
                RequirePlmMigrationAdmin();
                GetTenantFixture();
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SystemDefine_Preview_NotImplemented", ValidationItemType.Warning,
                    "System Define entity preview is not implemented yet (Phase 3)."));
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SystemDefine_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecuteUserDefineEntityImport(int? sessionId)
        {
            return ExecuteStubJob(sessionId, "UserDefineEntityImport", "User Define entity import is not implemented yet (Phase 4).");
        }

        public static OperationCallResult<PlmImportJobDto> ExecuteSystemDefineEntityImport(int? sessionId)
        {
            return ExecuteStubJob(sessionId, "SystemDefineEntityImport", "System Define entity import is not implemented yet (Phase 3).");
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
