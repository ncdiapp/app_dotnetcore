using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using APP.Components.EntityDto;
using APP.Framework;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace App.BL.TenantBusiness
{
    /// <summary>
    /// Tenant catalog helpers for agent tools: data sources, SaaS apps, connection smoke-test.
    /// Shared by platform BuiltIn tools and (legacy) PLM ExternalDll wrappers.
    /// Never returns connection strings to callers.
    /// </summary>
    public static class TenantCatalogBL
    {
        public static string ListTenantDataSourcesJson(int? targetCompanyId = null)
        {
            _ = targetCompanyId;
            try
            {
                var items = new List<object>();
                foreach (var reg in AppDataSourceRegisterBL.GetDataSourceRegisterList())
                {
                    if (reg?.Id == null) continue;
                    int id = Convert.ToInt32(reg.Id);
                    if (id <= 0) continue;
                    items.Add(new
                    {
                        dataSourceRegisterId = id,
                        dataSourceName = reg.DataSourceName,
                        databaseName = reg.DatabaseName
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    isSuccess = true,
                    dataSources = items,
                    count = items.Count
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, isSuccess = false, error = ex.Message });
            }
        }

        public static string ListTenantSaasApplicationsJson(int? targetCompanyId = null)
        {
            _ = targetCompanyId;
            try
            {
                var items = new List<object>();
                foreach (var app in AppSaasUserApplicationPackageBL.GetSaasApplicationList(excludeChildMenu: true))
                {
                    if (app?.Id == null) continue;
                    int id = Convert.ToInt32(app.Id);
                    if (id <= 0) continue;
                    items.Add(new
                    {
                        saasApplicationId = id,
                        applicationName = app.Name
                    });
                }

                if (items.Count == 0)
                {
                    try
                    {
                        var dsId = ServerContext.Instance?.DataSourceId as int? ?? 0;
                        if (dsId > 0)
                        {
                            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dsId);
                            if (fixture != null)
                            {
                                var dt = fixture.RetriveDataTable(@"
SELECT MenuID, Name
FROM dbo.AppListMenu
WHERE (ParentID IS NULL OR ParentID = 0)
  AND LinkType = 10
ORDER BY MenuID",
                                    new List<DbParameter>());
                                if (dt != null)
                                {
                                    foreach (DataRow row in dt.Rows)
                                    {
                                        int id = Convert.ToInt32(row["MenuID"]);
                                        if (id <= 0) continue;
                                        items.Add(new
                                        {
                                            saasApplicationId = id,
                                            applicationName = row["Name"]?.ToString()
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        /* fallback best-effort */
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    isSuccess = true,
                    applications = items,
                    count = items.Count
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, isSuccess = false, error = ex.Message });
            }
        }

        public static string TestDataSourceConnectionJson(int dataSourceRegisterId, int? targetCompanyId = null)
        {
            _ = targetCompanyId;
            try
            {
                if (dataSourceRegisterId <= 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        isSuccess = false,
                        error = "dataSourceRegisterId is required. Do not pass a connection string."
                    });
                }

                var reg = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterExDto(dataSourceRegisterId);
                if (reg == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        isSuccess = false,
                        dataSourceRegisterId,
                        error = $"DataSourceRegisterId {dataSourceRegisterId} was not found on this tenant."
                    });
                }

                string conn = ResolveConnectionString(dataSourceRegisterId);
                string serverVersion;
                string databaseName = reg.DatabaseName;
                using (var sqlConn = new SqlConnection(conn))
                {
                    sqlConn.Open();
                    serverVersion = sqlConn.ServerVersion;
                    if (string.IsNullOrWhiteSpace(databaseName))
                        databaseName = sqlConn.Database;
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    isSuccess = true,
                    dataSourceRegisterId,
                    dataSourceName = reg.DataSourceName,
                    databaseName,
                    serverVersion
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    isSuccess = false,
                    dataSourceRegisterId,
                    error = ex.Message
                });
            }
        }

        private static string ResolveConnectionString(int dataSourceRegisterId)
        {
            var reg = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(dataSourceRegisterId);
            if (reg == null)
                throw new InvalidOperationException($"DataSourceRegisterId {dataSourceRegisterId} was not found on this tenant.");
            if (string.IsNullOrWhiteSpace(reg.ConnectionString))
                throw new InvalidOperationException($"DataSourceRegisterId {dataSourceRegisterId} has no connection string configured.");
            return AppConnectionStringEncryptionBL.Decrypt(reg.ConnectionString);
        }
    }
}
