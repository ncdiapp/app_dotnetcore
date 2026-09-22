using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
    {
        private const string TechPackNewSchemaResource =
            "APP.AgentPlugins.PlmImport.Sql.TechPack.POM_Grading_QC_NewSchema.sql";
        private const string TechPackInspectionAddonResource =
            "APP.AgentPlugins.PlmImport.Sql.TechPack.POM_Grading_QC_InspectionAddon.sql";

        /// <summary>
        /// Apply product TechPack DDL (full NewSchema; optional InspectionAddon).
        /// Scripts are embedded from APP.AgentPlugins.PlmImport/Sql/TechPack/ — not Document/Design.
        /// </summary>
        public static OperationCallResult<PlmEnsureTechPackSchemaResultDto> EnsureTechPackSchema(
            PlmEnsureTechPackSchemaRequestDto request)
        {
            var result = new OperationCallResult<PlmEnsureTechPackSchemaResultDto>
            {
                Object = new PlmEnsureTechPackSchemaResultDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                _ = ResolveCompanyId(request?.TargetCompanyId);

                string tenantConn = GetTenantConnectionString();
                using (var conn = new SqlConnection(tenantConn))
                {
                    conn.Open();

                    string newSchemaSql = LoadEmbeddedTechPackSql(TechPackNewSchemaResource);
                    int batches = ExecuteSqlBatches(conn, newSchemaSql);
                    result.Object.RanNewSchema = true;
                    result.Object.BatchesExecuted += batches;
                    result.Object.Messages.Add(
                        $"Applied POM_Grading_QC_NewSchema.sql ({batches} batch(es)).");

                    if (request != null && request.IncludeInspectionAddon)
                    {
                        string addonSql = LoadEmbeddedTechPackSql(TechPackInspectionAddonResource);
                        int addonBatches = ExecuteSqlBatches(conn, addonSql);
                        result.Object.RanInspectionAddon = true;
                        result.Object.BatchesExecuted += addonBatches;
                        result.Object.Messages.Add(
                            $"Applied POM_Grading_QC_InspectionAddon.sql ({addonBatches} batch(es)).");
                    }
                    else
                    {
                        result.Object.Messages.Add("InspectionAddon skipped (includeInspectionAddon=false).");
                    }
                }

                result.Object.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.Object.Messages.Add(ex.Message);
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmEnsureTechPackSchemaRequestDto), "Plm_EnsureTechPackSchema_Error",
                    ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static string LoadEmbeddedTechPackSql(string resourceName)
        {
            var asm = typeof(PlmImportEngine).Assembly;
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    string available = string.Join(", ", asm.GetManifestResourceNames()
                        .Where(n => n.IndexOf("TechPack", StringComparison.OrdinalIgnoreCase) >= 0)
                        .DefaultIfEmpty("(none)"));
                    throw new InvalidOperationException(
                        $"Embedded TechPack SQL not found: {resourceName}. Available: {available}");
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>Split on lines that are only GO (sqlcmd convention) and execute each batch.</summary>
        private static int ExecuteSqlBatches(SqlConnection conn, string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                return 0;

            var batches = Regex.Split(
                script,
                @"^\s*GO\s*(?:--.*)?\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            int executed = 0;
            foreach (string raw in batches)
            {
                string batch = raw.Trim();
                if (string.IsNullOrWhiteSpace(batch))
                    continue;
                // Skip pure SET NOCOUNT / comment-only noise is fine — still execute SET statements.
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = batch;
                    cmd.CommandTimeout = 300;
                    cmd.ExecuteNonQuery();
                }
                executed++;
            }

            return executed;
        }
    }
}
