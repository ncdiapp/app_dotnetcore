using System;
using APP.LBL.DatabaseSpecific;
using APP.Framework;
using APP.Components.Dto;

namespace App.BL
{
    internal static class AppTenantAdapterBL
    {
        private const string DefaultCatalogName = "AppMasterDB";

        // Returns a DataAccessAdapter connected to the current tenant DB.
        // Throws if called before the per-request identity is registered (e.g. during login).
        // Tenant BL must never fall back to the master DB — fail loudly on missing context.
        internal static DataAccessAdapter GetTenantAdapter()
        {
            var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
            var connStr  = identity?.CurrentUserDbConnectionString;
            var dbName   = identity?.CurrentUserDataBaseName;

            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(dbName))
                throw new InvalidOperationException(
                    "Tenant DB context is not available. GetTenantAdapter() must only be called after login and identity registration.");

            var adapter = new DataAccessAdapter(connStr);
            adapter.CatalogNameOverwrites.Add(DefaultCatalogName, dbName);
            return adapter;
        }

        // For tables that exist in BOTH AppMasterDB and every TenantDB (e.g. AppLanguage,
        // AppLanguageKey, AppSysLabelLanguage).  SysAdmin connects directly to AppMasterDB
        // so no catalog rewrite is needed.  Tenant users get the standard catalog overwrite.
        internal static DataAccessAdapter GetContextAdapter()
        {
            var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
            var connStr  = identity?.CurrentUserDbConnectionString;
            var dbName   = identity?.CurrentUserDataBaseName;

            if (string.IsNullOrEmpty(connStr) || string.IsNullOrEmpty(dbName))
                throw new InvalidOperationException(
                    "DB context is not available. GetContextAdapter() must only be called after login and identity registration.");

            var adapter = new DataAccessAdapter(connStr);

            // SysAdmin's dbName == DefaultCatalogName ("AppMasterDB") — queries already target
            // the right catalog, no rewrite needed.  For every other user, rewrite the
            // LLBLGen-generated "AppMasterDB" prefix to the actual tenant DB name.
            if (!string.Equals(dbName, DefaultCatalogName, StringComparison.OrdinalIgnoreCase))
                adapter.CatalogNameOverwrites.Add(DefaultCatalogName, dbName);

            return adapter;
        }
    }
}
