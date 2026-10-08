using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework;
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

        // Description stores EmAppApplicationSettingCategory codes. 7 is WeChat (not in the enum).
        private static readonly Dictionary<string, string> CategoryLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "Server Setting",
            ["2"] = "File System",
            ["3"] = "Email System",
            ["4"] = "UI Layout",
            ["5"] = "E Shop Page Setting",
            ["6"] = "File Folder Setting",
            ["7"] = "WeChat",
            ["8"] = "Mobile Setting",
            ["9"] = "Google Setting",
            ["10"] = "Calendar Setting By User Type",
            ["11"] = "User Profile Setting By User Type",
            ["12"] = "Security Filter Entity By User Type",
            ["13"] = "Default Login Page By User Type",
            ["14"] = "Partner User And Partner Mapping Relation",
            ["15"] = "Transfer Partner User Register Info To Partner Extend Table Mapping",
            ["16"] = "Figma",
            ["100"] = "General Setting",
        };

        private static readonly byte[] LegacySalt = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };

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
                        dict[key] = DecryptSetupValue(row[1] as string);
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
                const string sql = @"SELECT SetupID, SetupCode, SetupValue, Description, EntityID, UsageType
                                     FROM dbo.AppSystemSetting
                                     ORDER BY SetupCode";
                var dt = adapter.ExecuteDataTableRetrievalQuery(sql, new List<SqlParameter>());
                foreach (DataRow row in dt.Rows)
                {
                    var code = row["SetupCode"] as string;
                    if (string.IsNullOrEmpty(code)) continue;

                    var description = row["Description"] as string;
                    var dto = new AppSetupExDto
                    {
                        Id = row["SetupID"],
                        SetupCode = code,
                        SetupValue = DecryptSetupValue(row["SetupValue"] as string),
                        Description = string.IsNullOrWhiteSpace(description) ? code : description,
                        Category = ResolveCategory(description),
                    };

                    if (row["EntityID"] != DBNull.Value && int.TryParse(row["EntityID"]?.ToString(), out var entityId))
                        dto.EntityId = entityId;

                    if (row["UsageType"] != DBNull.Value && int.TryParse(row["UsageType"]?.ToString(), out var usageType))
                        dto.UsageType = usageType;

                    if (dto.UsageType.HasValue
                        && dto.UsageType.Value == (int)EmAppApplicationSettingValueType.List
                        && dto.EntityId.HasValue)
                    {
                        // Entity rows live in the tenant DB. SysAdmin is on AppMasterDB, so a miss
                        // must stay a text box — an empty combo would hide the decrypted value.
                        try
                        {
                            var lookup = AppEntityInfoBL.GetLookupItemList(dto.EntityId.Value, string.Empty, false);
                            if (lookup != null && lookup.Count > 0)
                                dto.EntityDataSource = lookup;
                            else
                                dto.UsageType = (int)EmAppApplicationSettingValueType.Text;
                        }
                        catch
                        {
                            dto.UsageType = (int)EmAppApplicationSettingValueType.Text;
                        }
                    }

                    dto.StopChangeTracking();
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
                        var plain = dto.SetupValue ?? string.Empty;
                        var stored = string.IsNullOrEmpty(plain) ? string.Empty : EncryptSetupValue(plain);
                        const string sql = "UPDATE dbo.AppSystemSetting SET SetupValue = @val WHERE SetupCode = @code";
                        adapter.ExecuteExecuteNonQuery(sql, new List<SqlParameter>
                        {
                            new SqlParameter("@val", stored),
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

        private static string ResolveCategory(string description)
        {
            var raw = (description ?? string.Empty).Trim();
            if (raw.Length == 0)
                return "General";
            string label;
            return CategoryLabels.TryGetValue(raw, out label) ? label : raw;
        }

        /// <summary>
        /// Existing AppSystemSetting rows were written with PasswordDeriveBytes (legacy AppSetup).
        /// APP.Framework.EnDeCrypt now uses Rfc2898 and cannot read those rows, so it is only a fallback.
        /// </summary>
        private static string DecryptSetupValue(string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return stored ?? string.Empty;

            var legacy = TryLegacyCrypt(stored, encrypt: false);
            if (legacy != null)
                return legacy;

            try
            {
                return EnDeCrypt.Decrypt(stored, AppSetupConverter.AdSetupValueSaltKey);
            }
            catch
            {
                return stored;
            }
        }

        private static string EncryptSetupValue(string plain)
        {
            if (string.IsNullOrEmpty(plain))
                return plain ?? string.Empty;
            return TryLegacyCrypt(plain, encrypt: true) ?? plain;
        }

        private static string TryLegacyCrypt(string text, bool encrypt)
        {
            try
            {
                byte[] key;
                byte[] iv;
#pragma warning disable SYSLIB0041
                using (var pdb = new PasswordDeriveBytes(AppSetupConverter.AdSetupValueSaltKey, LegacySalt))
                {
                    key = pdb.GetBytes(32);
                    iv = pdb.GetBytes(16);
                }
#pragma warning restore SYSLIB0041

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    if (encrypt)
                    {
                        var clear = Encoding.Unicode.GetBytes(text);
                        using (var enc = aes.CreateEncryptor())
                            return Convert.ToBase64String(enc.TransformFinalBlock(clear, 0, clear.Length));
                    }

                    var cipher = Convert.FromBase64String(text);
                    using (var dec = aes.CreateDecryptor())
                    {
                        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                        return Encoding.Unicode.GetString(plain);
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
