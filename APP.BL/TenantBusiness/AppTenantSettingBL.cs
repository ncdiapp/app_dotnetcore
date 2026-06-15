using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;

namespace App.BL
{
    /// <summary>
    /// Reads from the current tenant DB (AppTenantDB_{companyId}).AppTenantSetting.
    /// Each company's settings are cached independently; call InvalidateCache to refresh.
    /// </summary>
    public static class AppTenantSettingBL
    {
        private static readonly ConcurrentDictionary<int, Dictionary<string, string>> _companyCache
            = new ConcurrentDictionary<int, Dictionary<string, string>>();

        internal static Dictionary<string, string> GetOrLoadCache(int companyId)
        {
            return _companyCache.GetOrAdd(companyId, id =>
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
                var connStr = identity?.CurrentUserDbConnectionString;
                if (string.IsNullOrEmpty(connStr))
                    return dict;

                // AppTenantSetting is tenant-only — AppMasterDB (SysAdmin) does not have this table.
                if (string.Equals(connStr.Trim(), AppCompanyBL.AppMasterDBConnectionString?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    return dict;

                using (var adapter = new DataAccessAdapter(connStr))
                {
                    const string sql = "SELECT SetupCode, SetupValue FROM dbo.AppTenantSetting";
                    var dt = adapter.ExecuteDataTableRetrievalQuery(sql, new List<SqlParameter>());
                    foreach (DataRow row in dt.Rows)
                    {
                        var key = row[0] as string;
                        if (!string.IsNullOrEmpty(key))
                            dict[key] = row[1] as string;
                    }
                }
                return dict;
            });
        }

        public static void InvalidateCache(int companyId)
        {
            Dictionary<string, string> discarded;
            _companyCache.TryRemove(companyId, out discarded);
        }

        public static string GetStringValue(EmTenantSettings key)
        {
            var raw = ServerContext.Instance.CurrnetClientIdentity;
            if (!(raw is AppClientIdentity identity))
                return null;

            var companyId = identity.CurrentWorkingCompanyId is int id ? id : 0;
            if (companyId == 0)
                return null;

            var cache = GetOrLoadCache(companyId);
            cache.TryGetValue(key.ToString(), out var val);
            return val;
        }

        public static int? GetIntValue(EmTenantSettings key)
        {
            var raw = GetStringValue(key);
            int result;
            return int.TryParse(raw, out result) ? (int?)result : null;
        }

        public static bool GetBoolValue(EmTenantSettings key)
        {
            var raw = GetStringValue(key) ?? string.Empty;
            return raw == "1" || string.Equals(raw.Trim(), bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        public static ObservableSet<AppSetupExDto> RetrieveAllAsDto()
        {
            var set = new ObservableSet<AppSetupExDto>();
            var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
            var connStr = identity?.CurrentUserDbConnectionString;
            if (string.IsNullOrEmpty(connStr)) return set;

            using (var adapter = new DataAccessAdapter(connStr))
            {
                const string sql = "SELECT SetupCode, SetupValue FROM dbo.AppTenantSetting ORDER BY SetupCode";
                var dt = adapter.ExecuteDataTableRetrievalQuery(sql, new List<SqlParameter>());
                foreach (DataRow row in dt.Rows)
                {
                    var code = row[0] as string;
                    if (string.IsNullOrEmpty(code)) continue;
                    var dto = new AppSetupExDto();
                    dto.Id = code;
                    dto.SetupCode = code;
                    dto.SetupValue = row[1] as string;
                    dto.Description = code;
                    set.Add(dto);
                }
            }
            return set;
        }

        public static IEnumerable<string> RetrieveAllWebPageTemplateFileNameList()
        {
            var fileNameList = new List<string>();
            string path = GetStringValue(EmTenantSettings.WebPageTemplatePath);
            if (System.IO.Directory.Exists(path))
            {
                foreach (var file in System.IO.Directory.GetFiles(path))
                    fileNameList.Add(System.IO.Path.GetFileName(file));
            }
            return fileNameList;
        }

        public static OperationCallResult<AppSetupExDto> SaveAll(ObservableSet<AppSetupExDto> aSet)
        {
            var result = new OperationCallResult<AppSetupExDto>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            var identity = (AppClientIdentity?)ServerContext.Instance.CurrnetClientIdentity;
            var connStr = identity?.CurrentUserDbConnectionString;
            if (string.IsNullOrEmpty(connStr))
            {
                validation.Items.Add(new ValidationItem(typeof(AppSetupExDto), "no_conn", ValidationItemType.Error, "No tenant connection available."));
                return result;
            }

            var companyId = identity?.CurrentWorkingCompanyId is int id ? id : 0;
            var modified = aSet.FindModifiedItems().Where(o => !o.IsNew).ToList();
            if (!modified.Any())
            {
                result.ObjectList = RetrieveAllAsDto();
                return result;
            }

            using (var adapter = new DataAccessAdapter(connStr))
            {
                adapter.StartTransaction(IsolationLevel.ReadCommitted, "SaveTenantSettings");
                try
                {
                    foreach (var dto in modified)
                    {
                        const string sql = "UPDATE dbo.AppTenantSetting SET SetupValue = @val WHERE SetupCode = @code";
                        adapter.ExecuteExecuteNonQuery(sql, new List<SqlParameter>
                        {
                            new SqlParameter("@val", (object)dto.SetupValue ?? DBNull.Value),
                            new SqlParameter("@code", dto.SetupCode)
                        });
                    }
                    adapter.Commit();
                    if (companyId > 0) InvalidateCache(companyId);
                    result.ObjectList = RetrieveAllAsDto();
                }
                catch (Exception ex)
                {
                    adapter.Rollback();
                    validation.Items.Add(new ValidationItem(typeof(AppSetupExDto), "save_error", ValidationItemType.Error, ex.Message));
                }
            }
            return result;
        }
    }
}
