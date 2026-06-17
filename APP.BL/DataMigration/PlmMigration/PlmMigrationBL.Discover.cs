using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private sealed class PdmDataSourceRow
        {
            public int DataSourceFrom { get; set; }
            public string DataSourceName { get; set; }
            public string ConnectionString { get; set; }
        }

        private static string GetPlmDataSourceFromName(int dataSourceFrom)
        {
            switch (dataSourceFrom)
            {
                case 1: return "PLM";
                case 2: return "ERP";
                case 3: return "DataWS";
                case 4: return "OtherEx";
                default: return $"Source_{dataSourceFrom}";
            }
        }

        private static string GetExternalRegisterSuffix(int dataSourceFrom)
        {
            switch (dataSourceFrom)
            {
                case 2: return "_ERP";
                case 3: return "_DataWS";
                case 4: return "_OtherEx";
                default: return null;
            }
        }

        private static string NormalizeConnectionStringForCompare(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return string.Empty;

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString.Trim());
                return string.Join("|", new[]
                {
                    builder.DataSource?.Trim().ToLowerInvariant(),
                    builder.InitialCatalog?.Trim().ToLowerInvariant(),
                    builder.UserID?.Trim().ToLowerInvariant()
                });
            }
            catch
            {
                return connectionString.Trim().ToLowerInvariant();
            }
        }


        private static bool TryTestSqlConnection(string connectionString, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private static List<PdmDataSourceRow> ReadPdmDataSourceRows(string plmConnectionString)
        {
            var rows = new List<PdmDataSourceRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT DataSourceFrom, DataSourceName, ConnectionString
FROM dbo.pdmDataSource
WHERE DataSourceFrom IN (1, 2, 3, 4)
ORDER BY DataSourceFrom";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new PdmDataSourceRow
                            {
                                DataSourceFrom = reader.GetInt32(0),
                                DataSourceName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                ConnectionString = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            return rows;
        }

        private static AppDataSourceRegisterExDto FindRegisterByConnectionString(
            IEnumerable<AppDataSourceRegisterExDto> registers, string plainConnectionString)
        {
            string target = NormalizeConnectionStringForCompare(plainConnectionString);
            if (string.IsNullOrEmpty(target))
                return null;

            foreach (var reg in registers)
            {
                if (string.IsNullOrWhiteSpace(reg.ConnectionString))
                    continue;

                string plain = AppConnectionStringEncryptionBL.Decrypt(reg.ConnectionString);
                if (NormalizeConnectionStringForCompare(plain) == target)
                    return reg;
            }

            return null;
        }

        private static string GetTenantDatabaseName(int tenantDataSourceId)
        {
            var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(tenantDataSourceId);
            return tenantRegister?.DatabaseName;
        }

        internal static OperationCallResult<PlmDiscoverDataSourcesResultDto> DiscoverPlmDataSourcesCore(
            PlmDiscoverDataSourcesRequestDto request, int companyId)
        {
            var result = new OperationCallResult<PlmDiscoverDataSourcesResultDto>
            {
                Object = new PlmDiscoverDataSourcesResultDto()
            };

            if (string.IsNullOrWhiteSpace(request?.PlmConnectionString))
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = "PLM connection string is required.";
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_Connection_Empty", ValidationItemType.Error,
                    result.Object.ErrorMessage));
                return result;
            }

            string plmMain = request.PlmConnectionString.Trim();
            List<PdmDataSourceRow> pdmRows;
            try
            {
                pdmRows = ReadPdmDataSourceRows(plmMain);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = "Failed to read pdmDataSource: " + ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_Read_Error", ValidationItemType.Error,
                    result.Object.ErrorMessage));
                return result;
            }

            if (pdmRows.Count == 0)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = "No PLM data sources found in pdmDataSource (DataSourceFrom 1–4).";
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_Empty", ValidationItemType.Error,
                    result.Object.ErrorMessage));
                return result;
            }

            var masterRegister = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(companyId);
            if (masterRegister == null)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = "Company master database register was not found.";
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_MasterDb_Missing", ValidationItemType.Error,
                    result.Object.ErrorMessage));
                return result;
            }

            string tenantDbName = GetTenantDatabaseName(GetTenantDataSourceId());
            var allRegisters = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterExDto().ToList();
            var processedFrom = new HashSet<int>();

            foreach (var row in pdmRows)
            {
                if (!processedFrom.Add(row.DataSourceFrom))
                    continue;

                bool hasConnection = !string.IsNullOrWhiteSpace(row.ConnectionString);
                var item = new PlmDataSourceDiscoveryItemDto
                {
                    DataSourceFrom = row.DataSourceFrom,
                    DataSourceFromName = GetPlmDataSourceFromName(row.DataSourceFrom),
                    DataSourceName = !string.IsNullOrWhiteSpace(row.DataSourceName)
                        ? row.DataSourceName.Trim()
                        : GetPlmDataSourceFromName(row.DataSourceFrom),
                    ConnectionString = row.ConnectionString?.Trim() ?? string.Empty,
                    HasConnectionString = hasConnection
                };

                // PLM (DSF=1): no new register — always map to company master.
                if (row.DataSourceFrom == 1)
                {
                    item.RegisteredDataSourceId = masterRegister.DataSourceId;
                    item.RegisteredDataSourceName = masterRegister.DataSourceName;
                    item.IsReusedRegister = true;

                    if (hasConnection)
                    {
                        if (TryTestSqlConnection(row.ConnectionString.Trim(), out string plmTestError))
                        {
                            item.ConnectionTestSuccess = true;
                            item.ConnectionTestMessage = "OK";
                        }
                        else
                        {
                            item.ConnectionTestSuccess = false;
                            item.ConnectionTestMessage = plmTestError;
                        }
                    }

                    result.Object.DataSources.Add(item);
                    continue;
                }

                // B10: external sources — only pdmDataSource.ConnectionString; empty or failed test → skip.
                if (!hasConnection)
                {
                    result.Object.DataSources.Add(item);
                    continue;
                }

                string effectiveConn = row.ConnectionString.Trim();
                if (!TryTestSqlConnection(effectiveConn, out string externalTestError))
                {
                    item.ConnectionTestSuccess = false;
                    item.ConnectionTestMessage = externalTestError;
                    result.Object.DataSources.Add(item);
                    continue;
                }

                item.ConnectionTestSuccess = true;
                item.ConnectionTestMessage = "OK";

                string suffix = GetExternalRegisterSuffix(row.DataSourceFrom);
                if (suffix == null)
                {
                    result.Object.DataSources.Add(item);
                    continue;
                }

                var existingByConn = FindRegisterByConnectionString(allRegisters, effectiveConn);
                if (existingByConn != null)
                {
                    int? ownerCompanyId = existingByConn.DataSourceOwnerCompanyId;
                    if (ownerCompanyId.HasValue && ownerCompanyId.Value != companyId)
                    {
                        result.Object.IsSuccess = false;
                        result.Object.ErrorMessage =
                            $"Connection for {item.DataSourceFromName} is already registered to another company. Import blocked.";
                        result.ValidationResult.Items.Add(new ValidationItem(
                            typeof(PlmDiscoverDataSourcesRequestDto), "Plm_Discover_Company_Lock",
                            ValidationItemType.Error, result.Object.ErrorMessage));
                        result.Object.DataSources.Add(item);
                        return result;
                    }

                    item.RegisteredDataSourceId = EditableObject.ConvertValueToInt(existingByConn.Id);
                    item.RegisteredDataSourceName = existingByConn.DataSourceName;
                    item.IsReusedRegister = true;
                    result.Object.DataSources.Add(item);
                    continue;
                }

                string registerName = !string.IsNullOrWhiteSpace(tenantDbName)
                    ? tenantDbName + suffix
                    : (row.DataSourceName ?? item.DataSourceFromName) + suffix;

                var existingByName = allRegisters.FirstOrDefault(r =>
                    r.DataSourceOwnerCompanyId == companyId &&
                    string.Equals(r.DataSourceName, registerName, StringComparison.OrdinalIgnoreCase));

                if (existingByName != null)
                {
                    item.RegisteredDataSourceId = EditableObject.ConvertValueToInt(existingByName.Id);
                    item.RegisteredDataSourceName = existingByName.DataSourceName;
                    item.IsReusedRegister = true;
                    result.Object.DataSources.Add(item);
                    continue;
                }

                string databaseName;
                try
                {
                    databaseName = new SqlConnectionStringBuilder(effectiveConn).InitialCatalog;
                }
                catch
                {
                    databaseName = registerName;
                }

                var newRegister = new AppDataSourceRegisterExDto
                {
                    DataSourceName = registerName,
                    Description = $"PLM Import - {item.DataSourceFromName}",
                    DataSourceType = (int)EmAppDataServerType.SqlServer,
                    ConnectionString = effectiveConn,
                    DataSourceOwnerCompanyId = companyId,
                    DatabaseName = databaseName,
                    IsCompanyMasterDb = false,
                    AppCreatedById = AppSecurityUserBL.CurrentUserId,
                    AppCreatedDate = DateTime.UtcNow
                };

                var saveResult = AppDataSourceRegisterBL.SaveOneAppDataSourceRegisterExDto(newRegister);
                if (saveResult.ValidationResult.HasErrors)
                {
                    result.Object.IsSuccess = false;
                    result.Object.ErrorMessage = string.Join("; ",
                        saveResult.ValidationResult.Items.Select(i => i.LocalizedMessage));
                    result.ValidationResult.Merge(saveResult.ValidationResult);
                    result.Object.DataSources.Add(item);
                    return result;
                }

                int? newRegisterId = EditableObject.ConvertValueToInt(saveResult.Object?.Id);
                if (newRegisterId.HasValue)
                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(newRegisterId.Value);

                item.RegisteredDataSourceId = newRegisterId;
                item.RegisteredDataSourceName = saveResult.Object?.DataSourceName ?? registerName;
                item.IsReusedRegister = false;
                allRegisters.Add(saveResult.Object);
                result.Object.DataSources.Add(item);
            }

            result.Object.IsSuccess = true;
            return result;
        }
    }
}
