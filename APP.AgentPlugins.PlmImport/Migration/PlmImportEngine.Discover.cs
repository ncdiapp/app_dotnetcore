using System;

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
            var tenantRegister = App.BL.AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(tenantDataSourceId);
            return tenantRegister?.DatabaseName;
        }
    }
}
