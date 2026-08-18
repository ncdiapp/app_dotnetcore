using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        private static void UpsertSimpleListEntities(
            AppConfigPackDto pack,
            int? saasApplicationId,
            AppConfigPackExecuteResultDto executeResult)
        {
            var entities = pack?.SimpleListEntities;
            if (entities == null || entities.Count == 0)
                return;

            int tenantDataSourceId = GetTenantDataSourceId();
            using (var conn = OpenTenantConnection())
            {
                foreach (var entity in entities)
                {
                    if (entity == null || string.IsNullOrWhiteSpace(entity.EntityCode))
                        continue;

                    string entityCode = entity.EntityCode.Trim();
                    string description = string.IsNullOrWhiteSpace(entity.Description)
                        ? entityCode
                        : entity.Description.Trim();

                    int entityInfoId;
                    bool inserted;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT TOP 1 EntityInfoID
FROM dbo.AppEntityInfo
WHERE EntityCode = @Code";
                        cmd.Parameters.AddWithValue("@Code", entityCode);
                        var existing = cmd.ExecuteScalar();
                        if (existing != null && existing != DBNull.Value)
                        {
                            entityInfoId = Convert.ToInt32(existing);
                            inserted = false;
                            using (var upd = conn.CreateCommand())
                            {
                                upd.CommandText = @"
UPDATE dbo.AppEntityInfo
SET Description = @Description,
    EntityType = @EntityType,
    DisplayFiled1 = N'Code',
    SaasApplicationID = COALESCE(@SaasApplicationId, SaasApplicationID),
    DataSourceFrom = COALESCE(DataSourceFrom, @DataSourceFrom),
    AppModifiedDate = GETDATE()
WHERE EntityInfoID = @Id";
                                upd.Parameters.AddWithValue("@Description", description);
                                upd.Parameters.AddWithValue("@EntityType", (int)EmAppEntityType.SimpleValueList);
                                upd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                                upd.Parameters.AddWithValue("@DataSourceFrom", tenantDataSourceId);
                                upd.Parameters.AddWithValue("@Id", entityInfoId);
                                upd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var ins = conn.CreateCommand())
                            {
                                ins.CommandText = @"
INSERT INTO dbo.AppEntityInfo (
    EntityCode, Description, EntityType, DisplayFiled1,
    DataSourceFrom, IsSystemDefine, SaasApplicationID, AppCreatedDate)
VALUES (
    @Code, @Description, @EntityType, N'Code',
    @DataSourceFrom, 0, @SaasApplicationId, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                                ins.Parameters.AddWithValue("@Code", TruncateName(entityCode, 100, entityCode));
                                ins.Parameters.AddWithValue("@Description", TruncateName(description, 500, description));
                                ins.Parameters.AddWithValue("@EntityType", (int)EmAppEntityType.SimpleValueList);
                                ins.Parameters.AddWithValue("@DataSourceFrom", tenantDataSourceId);
                                ins.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                                entityInfoId = Convert.ToInt32(ins.ExecuteScalar());
                            }
                            inserted = true;
                        }
                    }

                    ReplaceSimpleListValues(conn, entityInfoId, entity.Values ?? new List<AppConfigPackSimpleListValueDto>());
                    executeResult.Messages.Add(
                        inserted
                            ? $"Inserted simple list entity '{entityCode}' ({entityInfoId})."
                            : $"Updated simple list entity '{entityCode}' ({entityInfoId}).");
                }
            }
        }

        private static void ReplaceSimpleListValues(
            SqlConnection conn,
            int entityInfoId,
            List<AppConfigPackSimpleListValueDto> values)
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int sortFallback = 0;
            foreach (var value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.Code))
                    continue;
                string code = value.Code.Trim();
                if (!codes.Add(code))
                    continue;

                sortFallback += 10;
                int sort = value.Sort ?? sortFallback;
                int internalKey = value.InternalKey ?? sort;
                string description = string.IsNullOrWhiteSpace(value.Description)
                    ? code
                    : value.Description.Trim();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
IF EXISTS (
    SELECT 1 FROM dbo.AppEntitySimpleListValue
    WHERE EntityInfoID = @EntityId AND Code = @Code)
BEGIN
    UPDATE dbo.AppEntitySimpleListValue
    SET InternalKey = @InternalKey,
        Description = @Description,
        Sort = @Sort,
        AppModifiedDate = GETDATE()
    WHERE EntityInfoID = @EntityId AND Code = @Code;
END
ELSE
BEGIN
    INSERT INTO dbo.AppEntitySimpleListValue (
        EntityInfoID, InternalKey, Code, Description, Sort, AppCreatedDate)
    VALUES (
        @EntityId, @InternalKey, @Code, @Description, @Sort, GETDATE());
END";
                    cmd.Parameters.AddWithValue("@EntityId", entityInfoId);
                    cmd.Parameters.AddWithValue("@InternalKey", internalKey);
                    cmd.Parameters.AddWithValue("@Code", TruncateName(code, 100, code));
                    cmd.Parameters.AddWithValue("@Description", TruncateName(description, 500, description));
                    cmd.Parameters.AddWithValue("@Sort", sort);
                    cmd.ExecuteNonQuery();
                }
            }

            if (codes.Count == 0)
                return;

            using (var del = conn.CreateCommand())
            {
                var inParams = new List<string>();
                int i = 0;
                foreach (var code in codes)
                {
                    string p = "@c" + i;
                    inParams.Add(p);
                    del.Parameters.AddWithValue(p, code);
                    i++;
                }
                del.CommandText = $@"
DELETE FROM dbo.AppEntitySimpleListValue
WHERE EntityInfoID = @EntityId
  AND Code NOT IN ({string.Join(", ", inParams)})";
                del.Parameters.AddWithValue("@EntityId", entityInfoId);
                del.ExecuteNonQuery();
            }
        }

        private static void ExportSimpleListEntities(SqlConnection conn, AppConfigPackDto pack)
        {
            pack.SimpleListEntities = pack.SimpleListEntities ?? new List<AppConfigPackSimpleListEntityDto>();
            pack.SimpleListEntities.Clear();

            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tx in pack.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                foreach (var field in tx.Fields ?? Enumerable.Empty<AppConfigPackFieldDto>())
                {
                    if (!string.IsNullOrWhiteSpace(field?.EntityCode))
                        codes.Add(field.EntityCode.Trim());
                }
            }

            foreach (var search in pack.Searches ?? Enumerable.Empty<AppConfigPackSearchDto>())
            {
                foreach (var field in search.CriteriaFields ?? Enumerable.Empty<AppConfigPackCriteriaFieldDto>())
                {
                    if (!string.IsNullOrWhiteSpace(field?.EntityCode))
                        codes.Add(field.EntityCode.Trim());
                }
                foreach (var field in search.SearchView?.Fields ?? Enumerable.Empty<AppConfigPackSearchViewFieldDto>())
                {
                    if (!string.IsNullOrWhiteSpace(field?.EntityCode))
                        codes.Add(field.EntityCode.Trim());
                }
            }

            foreach (var code in codes.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
            {
                int entityInfoId;
                string description;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP 1 EntityInfoID, Description
FROM dbo.AppEntityInfo
WHERE EntityType = @EntityType
  AND (EntityCode = @Code OR Description = @Code)
ORDER BY CASE WHEN EntityCode = @Code THEN 0 ELSE 1 END";
                    cmd.Parameters.AddWithValue("@EntityType", (int)EmAppEntityType.SimpleValueList);
                    cmd.Parameters.AddWithValue("@Code", code);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            continue;
                        entityInfoId = reader.GetInt32(0);
                        description = reader.IsDBNull(1) ? code : reader.GetString(1);
                    }
                }

                var entity = new AppConfigPackSimpleListEntityDto
                {
                    EntityCode = code,
                    Description = description,
                    Values = new List<AppConfigPackSimpleListValueDto>()
                };

                using (var valCmd = conn.CreateCommand())
                {
                    valCmd.CommandText = @"
SELECT InternalKey, Code, Description, Sort
FROM dbo.AppEntitySimpleListValue
WHERE EntityInfoID = @Id
ORDER BY ISNULL(Sort, 9999), InternalKey";
                    valCmd.Parameters.AddWithValue("@Id", entityInfoId);
                    using (var reader = valCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entity.Values.Add(new AppConfigPackSimpleListValueDto
                            {
                                InternalKey = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0),
                                Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Sort = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
                            });
                        }
                    }
                }

                pack.SimpleListEntities.Add(entity);
            }
        }
    }
}
