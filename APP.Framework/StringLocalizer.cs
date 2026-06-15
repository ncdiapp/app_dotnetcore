using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
//using APP.Persistence.Common;
using APP.Framework;

#if SILVERLIGHT
#endif


namespace APP.Framework.Globalization
{
	/// <summary>
	///   Represents a string localizer
	///   
	///   AppLocalizeSystemLableBL
	///   
	///   
	/// </summary>
	public static class StringLocalizer
    {
     
		public static readonly string ScriptLangugeKeyPrefix = "script_";
		public static Dictionary<string, string> GetClientScriptLangKeyValue()
		{
            EnsureLanguageCacheLoaded();
			int languageId = (int)ServerContext.Instance.CurrnetClientIdentity.LanguageId;

			var list = _AllLanguageKeyValue.Where(o => o.LanguageId == languageId && o.ResourceKey.StartsWith(ScriptLangugeKeyPrefix));

			var distinctList = list.GroupBy(x => x.ResourceKey)
						  .Select(g => g.First())
						  .ToDictionary(x => x.ResourceKey, x => x.Value);

			return distinctList;


		}
		
		public static string Localize(string resourceKey, string defaultValue = "")
		{
            EnsureLanguageCacheLoaded();
			if (ServerContext.Instance.CurrnetClientIdentity != null && ServerContext.Instance.CurrnetClientIdentity.LanguageId != null)
			{
				string coneKey = resourceKey + "_" + ServerContext.Instance.CurrnetClientIdentity.LanguageId.ToString();
				if (_dictLanguangeKeyCache.ContainsKey(coneKey))
				{
					return _dictLanguangeKeyCache[coneKey];
				}

			}


			return defaultValue;

		}

		/// <summary>
		///   Localizes the enum value.
		/// </summary>
		/// <param name = "prefix">The prefix.</param>
		/// <param name = "value">The value.</param>
		/// <returns>Return the localized string if available; otherwise the default value.</returns>
		public static string LocalizeEnumValue(string prefix, Enum value)
        {
            string enumName = value.GetType().Name;

            string key = prefix + enumName + "_" + value;

            return Localize(key, value.ToString());
        }

        #region----------- Get Currentuser lanugatkey

        private static List<AppLanguageDto> _AllLanguageKeyValue = new List<AppLanguageDto>();
        private static Dictionary<string, string> _dictLanguangeKeyCache = new Dictionary<string, string>();
        private static bool _languageCacheLoaded = false;

        private static void EnsureLanguageCacheLoaded()
        {
            if (_languageCacheLoaded) return;
            RefreshAppLanguageKeyDictionary();
        }

        private static List<AppLanguageDto> LoadAllLanguageKeyValue()
        {
            var result = new List<AppLanguageDto>();

            try
            {
                string connStr = ServerContext.Instance.CurrentUserDbConnectionString;
                if (string.IsNullOrEmpty(connStr)) return result;

                string queryAllLanguage = @"SELECT [LanguageID], [ResourceKey], [Value] FROM [AppLanguageKey]";

                using (var conn = new SqlConnection(connStr))
                using (var cmd = new SqlCommand(queryAllLanguage, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var dto = new AppLanguageDto();

                            if (!reader.IsDBNull(0))
                                dto.LanguageId = reader.GetInt32(0);

                            if (!reader.IsDBNull(1))
                                dto.ResourceKey = reader.GetString(1);

                            if (!reader.IsDBNull(2))
                                dto.GetType().GetProperty("Value")?.SetValue(dto, reader.GetString(2));

                            result.Add(dto);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // No DB context available yet; cache stays empty, Localize() returns defaultValue.
            }

            return result;
        }

        private static Dictionary<string, string> BuildDictFromList(List<AppLanguageDto> list)
        {
            return list.GroupBy(x => x.LanguageId.ToString() + x.ResourceKey)
                       .Select(g => g.First())
                       .ToDictionary(x => x.ResourceKey + "_" + x.LanguageId.ToString(), x => x.Value);
        }

        public static void RefreshAppLanguageKeyDictionary()
        {
            _AllLanguageKeyValue = LoadAllLanguageKeyValue();
            _dictLanguangeKeyCache = BuildDictFromList(_AllLanguageKeyValue);
            _languageCacheLoaded = true;
        }


       



        #endregion

    }
}