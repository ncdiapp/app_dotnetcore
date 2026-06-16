using System;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public static OperationCallResult<object> PreviewTemplateMapping(int? sessionId)
        {
            var result = new OperationCallResult<object>();
            try
            {
                RequirePlmMigrationAdmin();
                GetTenantFixture();
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Preview_NotImplemented", ValidationItemType.Warning,
                    "Template preview is not implemented yet (Phase 5)."));
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecuteTemplateImport(int? sessionId)
        {
            var result = new OperationCallResult<PlmImportJobDto>();
            try
            {
                RequirePlmMigrationAdmin();
                var fixture = GetTenantFixture();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                const string message = "Template import is not implemented yet (Phase 5).";
                int jobId = CreateStubJob(fixture, sessionId.Value, "TemplateImport", message);
                WriteImportLog(fixture, sessionId.Value, jobId, StepTemplate, "TemplateImport", "Failed", null, null, null, null, message);
                result.Object = GetImportJob(jobId).Object;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Execute_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }
    }
}
