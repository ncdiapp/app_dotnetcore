using System;
using System.Data;
using System.Data.SqlClient;
using APP.Components.EntityDto;
using APP.Framework;
using APP.LBL.DatabaseSpecific;

namespace App.BL
{
    public static class AppReportTemplateBL
    {
        public static AppReportTemplateDto GetByReportId(int reportId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            using (var conn = new SqlConnection(adapter.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT ReportTemplateId, ReportId, TemplateHtml, DataSpName, DataApiPath,
                           PageSize, Orientation, MarginMm, ExtraParamConfig, CreatedDate, ModifiedDate
                    FROM   AppReportTemplate
                    WHERE  ReportId = @reportId", conn))
                {
                    cmd.Parameters.AddWithValue("@reportId", reportId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapReader(reader);
                    }
                }
            }
            return null;
        }

        public static void Save(AppReportTemplateDto dto)
        {
            var existing = GetByReportId(dto.ReportId);
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            using (var conn = new SqlConnection(adapter.ConnectionString))
            {
                conn.Open();
                if (existing == null)
                    Insert(conn, dto);
                else
                    Update(conn, dto);
            }
        }

        public static void DeleteByReportId(int reportId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            using (var conn = new SqlConnection(adapter.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("DELETE FROM AppReportTemplate WHERE ReportId = @reportId", conn))
                {
                    cmd.Parameters.AddWithValue("@reportId", reportId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Audit log ─────────────────────────────────────────────────────────

        public static void WriteLog(int reportId, int? requestId, int mainReferenceId, int requestedBy, int durationMs, int pageCount, string clientIp)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            using (var conn = new SqlConnection(adapter.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    INSERT INTO AppReportLog
                        (ReportId, RequestId, MainReferenceId, RequestedBy, RequestedAt, DurationMs, PdfPageCount, ClientIp)
                    VALUES
                        (@reportId, @requestId, @mainRef, @requestedBy, GETUTCDATE(), @durationMs, @pageCount, @clientIp)", conn))
                {
                    cmd.Parameters.AddWithValue("@reportId",    reportId);
                    cmd.Parameters.AddWithValue("@requestId",   (object)requestId   ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mainRef",     mainReferenceId);
                    cmd.Parameters.AddWithValue("@requestedBy", requestedBy);
                    cmd.Parameters.AddWithValue("@durationMs",  durationMs);
                    cmd.Parameters.AddWithValue("@pageCount",   pageCount);
                    cmd.Parameters.AddWithValue("@clientIp",    (object)clientIp    ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ── Print request store ───────────────────────────────────────────────

        public static int CreateRequest(int reportId, int mainReferenceId, int? masterReferenceId,
            string multipleReferenceIds, string parameterMapping, int requestedBy)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            using (var conn = new SqlConnection(adapter.ConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(@"
                    INSERT INTO AppReportRequest
                        (ReportId, MainReferenceId, MasterReferenceId, MultipleReferenceIds, ParameterMapping, RequestedBy, RequestedAt, Status)
                    OUTPUT INSERTED.RequestId
                    VALUES
                        (@reportId, @mainRef, @masterRef, @multiIds, @paramMap, @requestedBy, GETUTCDATE(), 0)", conn))
                {
                    cmd.Parameters.AddWithValue("@reportId",    reportId);
                    cmd.Parameters.AddWithValue("@mainRef",     mainReferenceId);
                    cmd.Parameters.AddWithValue("@masterRef",   (object)masterReferenceId       ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@multiIds",    (object)multipleReferenceIds    ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@paramMap",    (object)parameterMapping        ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@requestedBy", requestedBy);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void Insert(SqlConnection conn, AppReportTemplateDto dto)
        {
            using (var cmd = new SqlCommand(@"
                INSERT INTO AppReportTemplate
                    (ReportId, TemplateHtml, DataSpName, DataApiPath, PageSize, Orientation, MarginMm, ExtraParamConfig, CreatedDate, ModifiedDate)
                VALUES
                    (@reportId, @html, @spName, @apiPath, @pageSize, @orientation, @marginMm, @extraParams, GETUTCDATE(), GETUTCDATE())", conn))
            {
                AddTemplateParams(cmd, dto);
                cmd.ExecuteNonQuery();
            }
        }

        private static void Update(SqlConnection conn, AppReportTemplateDto dto)
        {
            using (var cmd = new SqlCommand(@"
                UPDATE AppReportTemplate SET
                    TemplateHtml     = @html,
                    DataSpName       = @spName,
                    DataApiPath      = @apiPath,
                    PageSize         = @pageSize,
                    Orientation      = @orientation,
                    MarginMm         = @marginMm,
                    ExtraParamConfig = @extraParams,
                    ModifiedDate     = GETUTCDATE()
                WHERE ReportId = @reportId", conn))
            {
                AddTemplateParams(cmd, dto);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddTemplateParams(SqlCommand cmd, AppReportTemplateDto dto)
        {
            cmd.Parameters.AddWithValue("@reportId",    dto.ReportId);
            cmd.Parameters.AddWithValue("@html",        (object)dto.TemplateHtml     ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@spName",      (object)dto.DataSpName        ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@apiPath",     (object)dto.DataApiPath       ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pageSize",    dto.PageSize     ?? "A4");
            cmd.Parameters.AddWithValue("@orientation", dto.Orientation  ?? "portrait");
            cmd.Parameters.AddWithValue("@marginMm",    dto.MarginMm);
            cmd.Parameters.AddWithValue("@extraParams", (object)dto.ExtraParamConfig  ?? DBNull.Value);
        }

        private static AppReportTemplateDto MapReader(IDataReader r) => new AppReportTemplateDto
        {
            ReportTemplateId  = r.GetInt32(r.GetOrdinal("ReportTemplateId")),
            ReportId          = r.GetInt32(r.GetOrdinal("ReportId")),
            TemplateHtml      = r.IsDBNull(r.GetOrdinal("TemplateHtml"))     ? null : r.GetString(r.GetOrdinal("TemplateHtml")),
            DataSpName        = r.IsDBNull(r.GetOrdinal("DataSpName"))       ? null : r.GetString(r.GetOrdinal("DataSpName")),
            DataApiPath       = r.IsDBNull(r.GetOrdinal("DataApiPath"))      ? null : r.GetString(r.GetOrdinal("DataApiPath")),
            PageSize          = r.GetString(r.GetOrdinal("PageSize")),
            Orientation       = r.GetString(r.GetOrdinal("Orientation")),
            MarginMm          = r.GetInt32(r.GetOrdinal("MarginMm")),
            ExtraParamConfig  = r.IsDBNull(r.GetOrdinal("ExtraParamConfig")) ? null : r.GetString(r.GetOrdinal("ExtraParamConfig")),
            CreatedDate       = r.IsDBNull(r.GetOrdinal("CreatedDate"))      ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("CreatedDate")),
            ModifiedDate      = r.IsDBNull(r.GetOrdinal("ModifiedDate"))     ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("ModifiedDate")),
        };
    }
}
