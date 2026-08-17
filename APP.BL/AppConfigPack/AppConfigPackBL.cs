using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        public const string ActionInsert = "Insert";
        public const string ActionUpdate = "Update";
        public const string ActionSkip = "Skip";

        private const string EnsureIntegrationIdColumnsSql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppTransaction' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppTransaction ADD IntegrationId NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppTransactionUnit' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppTransactionUnit ADD IntegrationId NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppTransactionField' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppTransactionField ADD IntegrationId NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='AppSearch' AND COLUMN_NAME='IntegrationId')
    ALTER TABLE dbo.AppSearch ADD IntegrationId NVARCHAR(100) NULL;";

        public static OperationCallResult<AppConfigPackDto> Load(AppConfigPackLoadRequestDto request)
        {
            var result = new OperationCallResult<AppConfigPackDto>();
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.PackJson))
                    throw new ArgumentException("PackJson is required.");

                var pack = JsonConvert.DeserializeObject<AppConfigPackDto>(request.PackJson);
                if (pack == null)
                    throw new InvalidOperationException("App Config Pack JSON could not be deserialized.");

                if (pack.SchemaVersion <= 0)
                    pack.SchemaVersion = 1;

                pack.Tables = pack.Tables ?? new List<AppConfigPackTableDto>();
                pack.Views = pack.Views ?? new List<AppConfigPackViewDto>();
                pack.Transactions = pack.Transactions ?? new List<AppConfigPackTransactionDto>();
                pack.Searches = pack.Searches ?? new List<AppConfigPackSearchDto>();

                result.Object = pack;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AppConfigPackBL), "AppConfigPack_Load_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<AppConfigPackValidationDto> Validate(AppConfigPackDto pack)
        {
            var result = new OperationCallResult<AppConfigPackValidationDto>
            {
                Object = new AppConfigPackValidationDto()
            };
            try
            {
                EnsurePackSchema();
                ValidateInternal(pack, result.Object);
                result.Object.IsValid = result.Object.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Object.Errors.Add(ex.Message);
                result.Object.IsValid = false;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AppConfigPackBL), "AppConfigPack_Validate_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<AppConfigPackPreviewDto> Preview(AppConfigPackExecuteRequestDto request)
        {
            var result = new OperationCallResult<AppConfigPackPreviewDto>
            {
                Object = new AppConfigPackPreviewDto { IsSuccess = true }
            };
            try
            {
                EnsurePackSchema();
                var pack = request?.Pack;
                if (pack == null)
                    throw new ArgumentException("Pack is required.");

                var validation = new AppConfigPackValidationDto();
                ValidateInternal(pack, validation);
                if (validation.Errors.Count > 0)
                {
                    result.Object.IsSuccess = false;
                    result.Object.ErrorMessage = string.Join("; ", validation.Errors);
                    return result;
                }

                result.Object.Items = BuildPreviewItems(pack);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AppConfigPackBL), "AppConfigPack_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<AppConfigPackExecuteResultDto> Execute(AppConfigPackExecuteRequestDto request)
        {
            var result = new OperationCallResult<AppConfigPackExecuteResultDto>
            {
                Object = new AppConfigPackExecuteResultDto()
            };
            try
            {
                EnsurePackSchema();
                if (request?.Pack == null)
                    throw new ArgumentException("Pack is required.");

                var validation = new AppConfigPackValidationDto();
                ValidateInternal(request.Pack, validation);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                int tenantDataSourceId = GetTenantDataSourceId();
                int? saasApplicationId = request.SaasApplicationId
                    ?? request.Pack.Source?.SaasApplicationId;

                ApplyDdl(request.Pack, tenantDataSourceId, result.Object);
                AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(tenantDataSourceId);

                var txIdsByIntegration = UpsertTransactions(
                    request.Pack, tenantDataSourceId, saasApplicationId, result.Object);
                ApplyTransactionChildLinkTargets(request.Pack, txIdsByIntegration);

                int? groupId = UpsertTransactionGroup(request.Pack, txIdsByIntegration, saasApplicationId);
                if (groupId.HasValue && groupId.Value > 0)
                {
                    result.Object.TransactionGroupId = groupId;
                    result.Object.Messages.Add($"Transaction group {groupId.Value} ready.");
                }

                UpsertSearches(
                    request.Pack, tenantDataSourceId, saasApplicationId, txIdsByIntegration, groupId, result.Object);

                AttachApplicationAssets(saasApplicationId, txIdsByIntegration.Values.ToList(), result.Object);

                result.Object.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.Object.Messages.Add(ex.Message);
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AppConfigPackBL), "AppConfigPack_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<AppConfigPackExportResultDto> Export(AppConfigPackExportRequestDto request)
        {
            var result = new OperationCallResult<AppConfigPackExportResultDto>
            {
                Object = new AppConfigPackExportResultDto()
            };
            try
            {
                EnsurePackSchema();
                if (request == null || !request.SaasApplicationId.HasValue || request.SaasApplicationId.Value <= 0)
                    throw new ArgumentException("SaasApplicationId is required.");

                var pack = BuildExportPack(
                    request.SaasApplicationId.Value,
                    request.TransactionIds ?? new List<int>(),
                    request.SearchIds ?? new List<int>(),
                    request.ExportAll);

                result.Object.Pack = pack;
                result.Object.JsonText = JsonConvert.SerializeObject(pack, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                });
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(AppConfigPackBL), "AppConfigPack_Export_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void ValidateInternal(AppConfigPackDto pack, AppConfigPackValidationDto validation)
        {
            if (pack == null)
            {
                validation.Errors.Add("Pack is required.");
                return;
            }

            if (pack.SchemaVersion <= 0)
                validation.Warnings.Add("SchemaVersion missing — defaulting to 1.");

            pack.Tables = pack.Tables ?? new List<AppConfigPackTableDto>();
            pack.Views = pack.Views ?? new List<AppConfigPackViewDto>();
            pack.Transactions = pack.Transactions ?? new List<AppConfigPackTransactionDto>();
            pack.Searches = pack.Searches ?? new List<AppConfigPackSearchDto>();

            var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in pack.Tables)
            {
                if (string.IsNullOrWhiteSpace(table?.Name))
                {
                    validation.Errors.Add("tables[].name is required.");
                    continue;
                }

                if (!tableNames.Add(table.Name.Trim()))
                    validation.Errors.Add($"Duplicate table '{table.Name}'.");

                if (table.Columns == null || table.Columns.Count == 0)
                    validation.Errors.Add($"Table '{table.Name}' has no columns.");
                else if (!table.Columns.Any(c => c != null && c.IsPrimaryKey && !string.IsNullOrWhiteSpace(c.Name)))
                    validation.Warnings.Add($"Table '{table.Name}' has no primary key column.");
            }

            foreach (var view in pack.Views)
            {
                if (string.IsNullOrWhiteSpace(view?.Name))
                {
                    validation.Errors.Add("views[].name is required.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(view.CreateOrAlterSql))
                    validation.Errors.Add($"View '{view.Name}' is missing createOrAlterSql.");
            }

            var txIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tx in pack.Transactions)
            {
                if (string.IsNullOrWhiteSpace(tx?.IntegrationId))
                {
                    validation.Errors.Add("transactions[].integrationId is required.");
                    continue;
                }

                if (!txIds.Add(tx.IntegrationId.Trim()))
                    validation.Errors.Add($"Duplicate transaction integrationId '{tx.IntegrationId}'.");

                if (tx.UnitStructure == null || string.IsNullOrWhiteSpace(tx.UnitStructure.RootTableName))
                    validation.Errors.Add($"Transaction '{tx.IntegrationId}' is missing unitStructure.rootTableName.");
            }

            foreach (var tx in pack.Transactions)
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId))
                    continue;
                foreach (var child in tx.UnitStructure?.ChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                    ValidateChildUnit(tx.IntegrationId, child, txIds, validation);
            }

            var searchIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var search in pack.Searches)
            {
                if (string.IsNullOrWhiteSpace(search?.IntegrationId))
                {
                    validation.Errors.Add("searches[].integrationId is required.");
                    continue;
                }

                if (!searchIds.Add(search.IntegrationId.Trim()))
                    validation.Errors.Add($"Duplicate search integrationId '{search.IntegrationId}'.");

                if (string.IsNullOrWhiteSpace(search.DataSet?.QueryText))
                    validation.Errors.Add($"Search '{search.IntegrationId}' is missing dataSet.queryText.");

                if (search.SearchView?.Fields == null || search.SearchView.Fields.Count == 0)
                    validation.Errors.Add($"Search '{search.IntegrationId}' searchView.fields must contain at least one column.");
                else if (!search.SearchView.Fields.Any(f => f != null && f.IsTransRootId))
                    validation.Errors.Add($"Search '{search.IntegrationId}' must include one searchView field with isTransRootId=true.");

                foreach (var link in search.LinkTargets ?? Enumerable.Empty<AppConfigPackLinkTargetDto>())
                {
                    if (string.IsNullOrWhiteSpace(link?.TransactionIntegrationId))
                        continue;
                    if (!txIds.Contains(link.TransactionIntegrationId.Trim()))
                        validation.Warnings.Add(
                            $"Search '{search.IntegrationId}' link target '{link.TransactionIntegrationId}' is not in this pack — it must already exist in the tenant.");
                }
            }

            if (pack.Tables.Count == 0 && pack.Views.Count == 0 && pack.Transactions.Count == 0 && pack.Searches.Count == 0)
                validation.Errors.Add("Pack contains no tables, views, transactions, or searches.");
        }

        private static void ValidateChildUnit(
            string txIntegrationId,
            AppConfigPackChildUnitDto child,
            HashSet<string> txIds,
            AppConfigPackValidationDto validation)
        {
            if (child == null)
                return;
            if (string.IsNullOrWhiteSpace(child.TableName))
            {
                validation.Errors.Add($"Transaction '{txIntegrationId}' has a child unit without tableName.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(child.AvailableSourceTableName)
                && string.IsNullOrWhiteSpace(child.AvailableSelectSelectedColumn))
            {
                validation.Errors.Add(
                    $"Transaction '{txIntegrationId}' unit '{child.TableName}' has availableSourceTableName but no availableSelectSelectedColumn.");
            }

            foreach (var link in child.LinkTargets ?? Enumerable.Empty<AppConfigPackLinkTargetDto>())
            {
                if (string.IsNullOrWhiteSpace(link?.TransactionIntegrationId))
                    continue;
                if (string.IsNullOrWhiteSpace(link.SourceColumn))
                    validation.Errors.Add(
                        $"Transaction '{txIntegrationId}' unit '{child.TableName}' link target '{link.Name ?? link.ActionType}' is missing sourceColumn.");
                if (!txIds.Contains(link.TransactionIntegrationId.Trim()))
                {
                    string warn =
                        $"Transaction '{txIntegrationId}' unit '{child.TableName}' link target '{link.TransactionIntegrationId}' ({link.Name ?? link.ActionType ?? "Edit"}) is not in this pack — it must already exist in the tenant.";
                    if (!validation.Warnings.Contains(warn))
                        validation.Warnings.Add(warn);
                }
            }

            foreach (var grand in child.GrandChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                ValidateChildUnit(txIntegrationId, grand, txIds, validation);
        }

        private static List<AppConfigPackPreviewItemDto> BuildPreviewItems(AppConfigPackDto pack)
        {
            var items = new List<AppConfigPackPreviewItemDto>();
            using (var conn = OpenTenantConnection())
            {
                foreach (var table in pack.Tables ?? Enumerable.Empty<AppConfigPackTableDto>())
                {
                    if (string.IsNullOrWhiteSpace(table?.Name))
                        continue;

                    bool exists = ObjectExists(conn, table.Name, table.SchemaOwner);
                    int addCount = 0;
                    if (exists)
                    {
                        foreach (var col in table.Columns ?? Enumerable.Empty<AppConfigPackColumnDto>())
                        {
                            if (!string.IsNullOrWhiteSpace(col?.Name) && !ColumnExists(conn, table.Name, col.Name, table.SchemaOwner))
                                addCount++;
                        }
                    }

                    items.Add(new AppConfigPackPreviewItemDto
                    {
                        ObjectType = "Table",
                        Name = table.Name,
                        Action = exists ? (addCount > 0 ? ActionUpdate : ActionSkip) : ActionInsert,
                        Detail = exists
                            ? (addCount > 0 ? $"Add {addCount} column(s)" : "Exists")
                            : $"{table.Columns?.Count ?? 0} column(s)"
                    });
                }

                foreach (var view in pack.Views ?? Enumerable.Empty<AppConfigPackViewDto>())
                {
                    if (string.IsNullOrWhiteSpace(view?.Name))
                        continue;

                    bool exists = ObjectExists(conn, view.Name, view.SchemaOwner);
                    items.Add(new AppConfigPackPreviewItemDto
                    {
                        ObjectType = "View",
                        Name = view.Name,
                        Action = exists ? ActionUpdate : ActionInsert,
                        Detail = "CREATE OR ALTER VIEW"
                    });
                }

                foreach (var tx in pack.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
                {
                    if (string.IsNullOrWhiteSpace(tx?.IntegrationId))
                        continue;

                    int? existing = GetTransactionIdByIntegrationId(conn, tx.IntegrationId);
                    items.Add(new AppConfigPackPreviewItemDto
                    {
                        ObjectType = "Transaction",
                        Name = tx.Name ?? tx.IntegrationId,
                        IntegrationId = tx.IntegrationId,
                        Action = existing.HasValue ? ActionUpdate : ActionInsert,
                        ExistingId = existing,
                        Detail = $"Root {tx.UnitStructure?.RootTableName}; fields {tx.Fields?.Count ?? 0}"
                    });

                    foreach (var child in tx.UnitStructure?.ChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                    {
                        foreach (var link in child?.LinkTargets ?? Enumerable.Empty<AppConfigPackLinkTargetDto>())
                        {
                            if (string.IsNullOrWhiteSpace(link?.TransactionIntegrationId))
                                continue;
                            int? txId = GetTransactionIdByIntegrationId(conn, link.TransactionIntegrationId);
                            items.Add(new AppConfigPackPreviewItemDto
                            {
                                ObjectType = "LinkTarget",
                                Name = $"{child.TableName}: {link.Name ?? link.ActionType}",
                                IntegrationId = link.TransactionIntegrationId,
                                Action = ActionInsert,
                                ExistingId = txId,
                                Detail = link.ActionType
                            });
                        }
                    }
                }

                if (pack.TransactionGroup != null && !string.IsNullOrWhiteSpace(pack.TransactionGroup.Name))
                {
                    int? groupId = GetTransactionGroupIdByName(conn, pack.TransactionGroup.Name);
                    items.Add(new AppConfigPackPreviewItemDto
                    {
                        ObjectType = "TransactionGroup",
                        Name = pack.TransactionGroup.Name,
                        IntegrationId = pack.TransactionGroup.IntegrationId,
                        Action = groupId.HasValue ? ActionUpdate : ActionInsert,
                        ExistingId = groupId,
                        Detail = $"{pack.TransactionGroup.MemberTransactionIntegrationIds?.Count ?? 0} member(s)"
                    });
                }

                foreach (var search in pack.Searches ?? Enumerable.Empty<AppConfigPackSearchDto>())
                {
                    if (string.IsNullOrWhiteSpace(search?.IntegrationId))
                        continue;

                    int? existing = GetSearchIdByIntegrationId(conn, search.IntegrationId);
                    items.Add(new AppConfigPackPreviewItemDto
                    {
                        ObjectType = "Search",
                        Name = search.Name ?? search.IntegrationId,
                        IntegrationId = search.IntegrationId,
                        Action = existing.HasValue ? ActionUpdate : ActionInsert,
                        ExistingId = existing,
                        Detail = $"Criteria {search.CriteriaFields?.Count ?? 0}; view fields {search.SearchView?.Fields?.Count ?? 0}"
                    });

                    foreach (var link in search.LinkTargets ?? Enumerable.Empty<AppConfigPackLinkTargetDto>())
                    {
                        if (string.IsNullOrWhiteSpace(link?.TransactionIntegrationId))
                            continue;

                        int? txId = GetTransactionIdByIntegrationId(conn, link.TransactionIntegrationId);
                        items.Add(new AppConfigPackPreviewItemDto
                        {
                            ObjectType = "LinkTarget",
                            Name = link.Name ?? link.ActionType,
                            IntegrationId = link.TransactionIntegrationId,
                            Action = ActionInsert,
                            ExistingId = txId,
                            Detail = link.ActionType
                        });
                    }

                    if (search.Menu?.RegisterInMainMenu == true)
                    {
                        items.Add(new AppConfigPackPreviewItemDto
                        {
                            ObjectType = "Menu",
                            Name = search.Menu.MenuTitle ?? search.Name,
                            IntegrationId = search.IntegrationId,
                            Action = existing.HasValue ? ActionUpdate : ActionInsert,
                            ExistingId = existing,
                            Detail = "Register in application main menu"
                        });
                    }
                }
            }

            return items;
        }

        private static void EnsurePackSchema()
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(GetTenantDataSourceId());
            fixture.ExecuteNonQueryResult(EnsureIntegrationIdColumnsSql, new List<DbParameter>());
        }

        private static int GetTenantDataSourceId()
        {
            var dataSourceId = ServerContext.Instance?.DataSourceId as int?;
            if (!dataSourceId.HasValue || dataSourceId.Value <= 0)
                throw new InvalidOperationException("Tenant data source is not available.");
            return dataSourceId.Value;
        }

        private static string GetTenantConnectionString()
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(GetTenantDataSourceId());
            if (!string.IsNullOrWhiteSpace(fixture?.ConnectionString))
                return fixture.ConnectionString;

            var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(GetTenantDataSourceId());
            if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
                throw new InvalidOperationException("Tenant database connection is not available.");
            return AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);
        }

        private static SqlConnection OpenTenantConnection()
        {
            var conn = new SqlConnection(GetTenantConnectionString());
            conn.Open();
            return conn;
        }

        private static string QuoteIdent(string name)
        {
            return "[" + (name ?? string.Empty).Replace("]", "]]") + "]";
        }

        private static string SanitizeIdent(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            return Regex.Replace(name.Trim(), @"[^\w]", "");
        }

        private static string TruncateName(string name, int max, string fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;
            name = name.Trim();
            return name.Length <= max ? name : name.Substring(0, max);
        }

        private static string SlugIntegrationId(string prefix, string name)
        {
            string slug = Regex.Replace(name ?? string.Empty, @"[^\w]+", "");
            if (string.IsNullOrWhiteSpace(slug))
                slug = "Item";
            return TruncateName(prefix + slug, 100, prefix + "Item");
        }

        private static bool ObjectExists(SqlConnection conn, string name, string schemaOwner)
        {
            string schema = string.IsNullOrWhiteSpace(schemaOwner) ? "dbo" : schemaOwner.Trim();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 1 FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Name";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Name", name);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool IsView(SqlConnection conn, string name, string schemaOwner)
        {
            string schema = string.IsNullOrWhiteSpace(schemaOwner) ? "dbo" : schemaOwner.Trim();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 1 FROM INFORMATION_SCHEMA.VIEWS
WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Name";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Name", name);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool ColumnExists(SqlConnection conn, string tableName, string columnName, string schemaOwner)
        {
            string schema = string.IsNullOrWhiteSpace(schemaOwner) ? "dbo" : schemaOwner.Trim();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table AND COLUMN_NAME = @Column";
                cmd.Parameters.AddWithValue("@Schema", schema);
                cmd.Parameters.AddWithValue("@Table", tableName);
                cmd.Parameters.AddWithValue("@Column", columnName);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static int? GetTransactionIdByIntegrationId(SqlConnection conn, string integrationId)
        {
            if (string.IsNullOrWhiteSpace(integrationId))
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 TransactionID FROM dbo.AppTransaction WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", integrationId.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetSearchIdByIntegrationId(SqlConnection conn, string integrationId)
        {
            if (string.IsNullOrWhiteSpace(integrationId))
                return null;
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT TOP 1 SearchID FROM dbo.AppSearch WHERE IntegrationId = @IntegrationId";
                    cmd.Parameters.AddWithValue("@IntegrationId", integrationId.Trim());
                    var val = cmd.ExecuteScalar();
                    return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
                }
            }
            catch (SqlException)
            {
                return null;
            }
        }

        private static int? GetTransactionGroupIdByName(SqlConnection conn, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 TransactionGroupID FROM dbo.AppTransactionGroup WHERE GroupName = @Name";
                cmd.Parameters.AddWithValue("@Name", name.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void SetIntegrationId(SqlConnection conn, string table, string pkColumn, int id, string integrationId)
        {
            if (string.IsNullOrWhiteSpace(integrationId))
                return;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"UPDATE dbo.[{SanitizeIdent(table)}] SET IntegrationId = @IntegrationId WHERE [{SanitizeIdent(pkColumn)}] = @Id";
                cmd.Parameters.AddWithValue("@IntegrationId", TruncateName(integrationId.Trim(), 100, integrationId.Trim()));
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? ResolveEntityIdByCode(SqlConnection conn, string entityCode)
        {
            if (string.IsNullOrWhiteSpace(entityCode))
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 EntityInfoID
FROM dbo.AppEntityInfo
WHERE EntityCode = @Code OR Description = @Code
ORDER BY CASE WHEN EntityCode = @Code THEN 0 ELSE 1 END";
                cmd.Parameters.AddWithValue("@Code", entityCode.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetEntityCodeById(SqlConnection conn, int? entityId)
        {
            if (!entityId.HasValue || entityId.Value <= 0)
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 EntityCode FROM dbo.AppEntityInfo WHERE EntityInfoID = @Id";
                cmd.Parameters.AddWithValue("@Id", entityId.Value);
                return cmd.ExecuteScalar() as string;
            }
        }
    }
}
