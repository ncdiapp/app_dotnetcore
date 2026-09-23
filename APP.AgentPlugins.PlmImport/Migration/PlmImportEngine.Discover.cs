using System.Collections.Generic;
using App.BL;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport
{
    /// <summary>
    /// PLM source-role helpers only. Wizard-era discover that read PLM pdmDataSource
    /// connection strings and created AppDataSourceRegister rows has been removed.
    /// Agents must use list_tenant_data_sources + ask_user-selected register ids.
    /// </summary>
    public static partial class PlmImportEngine
    {
        internal static string GetPlmDataSourceFromName(int dataSourceFrom)
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

        private static string GetTenantDatabaseName(int tenantDataSourceId)
        {
            var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(tenantDataSourceId);
            return tenantRegister?.DatabaseName;
        }

        /// <summary>
        /// Build Discover JSON from Gate-0 register ids (no connection strings).
        /// DataSourceFrom 1 = tenant company master (System Define target).
        /// 2 = ERP, 3 = DataWS / PLM DW, 4 = OtherEx / PLM ExDb.
        /// </summary>
        internal static string BuildDataSourceDiscoveryJson(PlmImportSessionDto session)
        {
            int tenantDs = GetTenantDataSourceId();
            var discovery = new PlmDiscoverDataSourcesResultDto
            {
                IsSuccess = true,
                DataSources = new List<PlmDataSourceDiscoveryItemDto>
                {
                    BuildDiscoveryItem(1, tenantDs),
                    BuildDiscoveryItem(2, session?.ErpDataSourceRegisterId),
                    BuildDiscoveryItem(3, session?.PlmDwDataSourceRegisterId),
                    BuildDiscoveryItem(4, session?.PlmExDbDataSourceRegisterId)
                }
            };
            return JsonConvert.SerializeObject(discovery);
        }

        internal static string EnsureDataSourceDiscoveryJson(PlmImportSessionDto session)
        {
            if (session == null)
                return null;
            if (!string.IsNullOrWhiteSpace(session.DataSourceDiscoveryJson))
                return session.DataSourceDiscoveryJson;
            return BuildDataSourceDiscoveryJson(session);
        }

        private static void MergeSessionRegisterIds(PlmImportSessionDto dto, PlmImportSessionDto existing)
        {
            if (dto == null || existing == null)
                return;
            if (!dto.PlmDataSourceRegisterId.HasValue)
                dto.PlmDataSourceRegisterId = existing.PlmDataSourceRegisterId;
            if (!dto.PlmDwDataSourceRegisterId.HasValue)
                dto.PlmDwDataSourceRegisterId = existing.PlmDwDataSourceRegisterId;
            if (!dto.ErpDataSourceRegisterId.HasValue)
                dto.ErpDataSourceRegisterId = existing.ErpDataSourceRegisterId;
            if (!dto.PlmExDbDataSourceRegisterId.HasValue)
                dto.PlmExDbDataSourceRegisterId = existing.PlmExDbDataSourceRegisterId;
        }

        private static PlmDataSourceDiscoveryItemDto BuildDiscoveryItem(int dataSourceFrom, int? registerId)
        {
            var item = new PlmDataSourceDiscoveryItemDto
            {
                DataSourceFrom = dataSourceFrom,
                DataSourceFromName = GetPlmDataSourceFromName(dataSourceFrom),
                HasConnectionString = false,
                ConnectionString = null
            };

            if (!registerId.HasValue || registerId.Value <= 0)
                return item;

            var register = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(registerId.Value);
            item.RegisteredDataSourceId = registerId.Value;
            item.IsReusedRegister = true;
            if (register != null)
            {
                item.RegisteredDataSourceName = register.DataSourceName;
                item.DataSourceName = register.DataSourceName;
                item.ConnectionTestSuccess = !string.IsNullOrWhiteSpace(register.DatabaseName);
            }

            return item;
        }
    }
}
