using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;

namespace App.BL
{
    /// <summary>
    /// Reads from AppMasterDB.AppSystemSetting — installation-wide settings.
    /// Loaded once at startup; call Reload() to refresh without restarting.
    /// </summary>
    public static class AppSystemSettingBL
    {
        private static readonly object _lock = new object();
        private static Dictionary<string, string> _cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static AppSystemSettingBL()
        {
            try { LoadCache(); }
            catch { /* degrade gracefully; GetStringValue returns null, GetIntValue returns default */ }
        }

        private static void LoadCache()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                const string sql = "SELECT SetupCode, SetupValue FROM dbo.AppSystemSetting";
                var dt = adapter.ExecuteDataTableRetrievalQuery(sql, new List<SqlParameter>());
                foreach (DataRow row in dt.Rows)
                {
                    var key = row[0] as string;
                    if (!string.IsNullOrEmpty(key))
                        dict[key] = row[1] as string;
                }
            }
            lock (_lock) { _cache = dict; }
        }

        public static void Reload()
        {
            LoadCache();
        }

        public static string GetStringValue(EmSystemSettings key)
        {
            lock (_lock)
            {
                _cache.TryGetValue(key.ToString(), out var val);
                return val;
            }
        }

        public static int? GetIntValue(EmSystemSettings key)
        {
            var raw = GetStringValue(key);
            int result;
            return int.TryParse(raw, out result) ? (int?)result : null;
        }

        public static bool GetBoolValue(EmSystemSettings key)
        {
            var raw = GetStringValue(key) ?? string.Empty;
            return raw == "1" || string.Equals(raw.Trim(), bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        public static ObservableSet<AppSetupExDto> RetrieveAllAsDto()
        {
            var set = new ObservableSet<AppSetupExDto>();
            using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                const string sql = "SELECT SetupCode, SetupValue FROM dbo.AppSystemSetting ORDER BY SetupCode";
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

        public static int? CheckCacheStatus()
        {
#if NETFRAMEWORK
            try
            {
                var cfg = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
                int result;
                if (int.TryParse(cfg.AppSettings.Settings["EnableSystemCache"]?.Value, out result))
                    return result;
                return null;
            }
            catch { return null; }
#else
            return null;
#endif
        }

        public static bool EnableOrDisableCache(bool isEnableCache)
        {
#if NETFRAMEWORK
            try
            {
                var cfg = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");
                string filepath = cfg.FilePath;
                SetFileReadOnly(filepath, false);
                cfg.AppSettings.Settings["EnableSystemCache"].Value = isEnableCache ? "1" : "0";
                cfg.Save();
                SetFileReadOnly(filepath, true);
                return true;
            }
            catch { return false; }
#else
            return false;
#endif
        }

        public static APP.Components.Dto.ServerSettingDto CheckServerSetting()
        {
            return new APP.Components.Dto.ServerSettingDto
            {
                InstalledDbDriver = AppMetaDataBL.GetInstalledDbDriver()
            };
        }

        private static void SetFileReadOnly(string path, bool readOnly)
        {
            var attrs = System.IO.File.GetAttributes(path);
            if (readOnly)
                System.IO.File.SetAttributes(path, attrs | System.IO.FileAttributes.ReadOnly);
            else if ((attrs & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                System.IO.File.SetAttributes(path, attrs & ~System.IO.FileAttributes.ReadOnly);
        }

        public static OperationCallResult<AppSetupExDto> SaveAll(ObservableSet<AppSetupExDto> aSet)
        {
            var result = new OperationCallResult<AppSetupExDto>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            var modified = aSet
                .Where(o => o != null && !o.IsNew && o.IsModified)
                .ToList();
            if (!modified.Any())
                modified = aSet.FindModifiedItems().Where(o => !o.IsNew).ToList();

            if (!modified.Any())
            {
                result.ObjectList = RetrieveAllAsDto();
                return result;
            }

            using (var adapter = new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString))
            {
                adapter.StartTransaction(IsolationLevel.ReadCommitted, "SaveSystemSettings");
                try
                {
                    foreach (var dto in modified)
                    {
                        const string sql = "UPDATE dbo.AppSystemSetting SET SetupValue = @val WHERE SetupCode = @code";
                        adapter.ExecuteExecuteNonQuery(sql, new List<SqlParameter>
                        {
                            new SqlParameter("@val", (object)dto.SetupValue ?? DBNull.Value),
                            new SqlParameter("@code", dto.SetupCode)
                        });
                    }
                    adapter.Commit();
                    Reload();
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
