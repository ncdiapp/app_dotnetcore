using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
//
//using APP.Persistence.Common;
using APP.LBL.DatabaseSpecific;

using APP.Framework;
namespace App.BL
{

    //StringLocalizer
    public static class AppLocalizeSystemLableBL
    {


        private static Dictionary<int, List<DataRow>> _DictDataSourceRegIdAndAllLanguageKeyValue;
        private static Dictionary<String, string> _DictAllMenu;
        private static Dictionary<String, string> _DictAllTransactionFiled;
        private static Dictionary<String, string> _DictTransactionUnitLinkedSearch;
        private static Dictionary<String, string> _DictAllLinkTarget;

        private static Dictionary<String, string> _DictSearchViewField;
        private static Dictionary<String, string> _DictSearchField;
        private static Dictionary<String, string> _DictTransactionUnit;
        private static Dictionary<String, string> _DictSearchView;
        private static Dictionary<String, string> _DictSearch;



        static AppLocalizeSystemLableBL()
        {

            RefreshAppSystemLableLanguageKeyDictionaries();

        }


        #region ------------ Get Currentuser TransactionFieldLabel and menu

        public static void RefreshAppSystemLableLanguageKeyDictionaries()
        {

            _DictDataSourceRegIdAndAllLanguageKeyValue = new Dictionary<int, List<DataRow>>();

            _DictAllMenu = new Dictionary<string, string>();
            _DictAllTransactionFiled = new Dictionary<string, string>();
            _DictTransactionUnitLinkedSearch = new Dictionary<string, string>();
            _DictAllLinkTarget = new Dictionary<string, string>();
            _DictSearchViewField = new Dictionary<string, string>();
            _DictSearchField = new Dictionary<string, string>();
            _DictTransactionUnit = new Dictionary<string, string>();
            _DictSearchView = new Dictionary<string, string>();
            _DictSearch = new Dictionary<string, string>();

            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                _DictAllMenu = GetDictLabelWithFieldIdName("MenuID");
                _DictAllTransactionFiled = GetDictLabelWithFieldIdName("TransactionFieldID");
                _DictTransactionUnitLinkedSearch = GetDictLabelWithFieldIdName("TransactionUnitLinkedSearchId");
                _DictAllLinkTarget = GetDictLabelWithFieldIdName("LinkTargetID");

                _DictSearchViewField = GetDictLabelWithFieldIdName("SearchViewFieldID");
                _DictSearchField = GetDictLabelWithFieldIdName("SearchFieldID");
                _DictTransactionUnit = GetDictLabelWithFieldIdName("TransactionUnitID");
                _DictSearchView = GetDictLabelWithFieldIdName("SearchViewID");
                _DictSearch = GetDictLabelWithFieldIdName("SearchID");
            }
        }

        public static string GetSearchLabel(object SearchID, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + SearchID;
                if (_DictSearch.ContainsKey(coneKey))
                {
                    return _DictSearch[coneKey];
                }
            }

            return defaultString;

        }


        public static string GetSearchViewLabel(object SearchViewID, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + SearchViewID;
                if (_DictSearchView.ContainsKey(coneKey))
                {
                    return _DictSearchView[coneKey];
                }
            }
            return defaultString;
        }


        public static string GetTransactionUnitLabel(object TransactionUnitID, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + TransactionUnitID;
                if (_DictTransactionUnit.ContainsKey(coneKey))
                {
                    return _DictTransactionUnit[coneKey];
                }
            }

            return defaultString;

        }

        public static string GeSearchFieldLabel(object SearchFieldId, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + SearchFieldId;
                if (_DictSearchField.ContainsKey(coneKey))
                {
                    return _DictSearchField[coneKey];
                }
            }

            return defaultString;

        }


        public static string GetSearchViewFieldLabel(object SearchViewFieldId, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + SearchViewFieldId;
                if (_DictSearchViewField.ContainsKey(coneKey))
                {
                    return _DictSearchViewField[coneKey];
                }

            }

            return defaultString;

        }



        public static string GeLinkTargetLabel(object LinkTargetId, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + LinkTargetId;
                if (_DictAllLinkTarget.ContainsKey(coneKey))
                {
                    return _DictAllLinkTarget[coneKey];
                }
            }
            return defaultString;

        }


        public static string GetMenuLabel(object menuID, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + menuID;
                if (_DictAllMenu.ContainsKey(coneKey))
                {
                    return _DictAllMenu[coneKey];
                }
            }
            return defaultString;

        }


        public static string GetDictTransactionFieldLabel(object filedId, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + filedId;
                if (_DictAllTransactionFiled.ContainsKey(coneKey))
                {
                    return _DictAllTransactionFiled[coneKey];
                }
            }
            return defaultString;

        }

        public static string GetDictTransactionUnitLinkedSearchLabel(object filedId, string defaultString)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {
                string dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId.ToString();
                string coneKey = dataSourceRegisterId + "_" + AppSecurityUserBL.CurrentUserEntity.LanguageId + "_" + filedId;
                if (_DictTransactionUnitLinkedSearch.ContainsKey(coneKey))
                {
                    return _DictTransactionUnitLinkedSearch[coneKey];
                }
            }
            return defaultString;

        }





        private static Dictionary<string, string> GetDictLabelWithFieldIdName(string fieldName)
        {
            if (ServerContext.Instance.CurrnetClientIdentity != null)
            {

                // return _AllLanguageKeyValue.Where(r => (r["TransactionFieldID"] as int?).HasValue).ToDictionary(o => o["TransactionFieldID"], o => o["LanguageText"].ToString());
                int dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;

                if (_DictDataSourceRegIdAndAllLanguageKeyValue == null)
                {
                    _DictDataSourceRegIdAndAllLanguageKeyValue = new Dictionary<int, List<DataRow>>();
                }

                if (!_DictDataSourceRegIdAndAllLanguageKeyValue.ContainsKey(dataSourceRegisterId))
                {
                    _DictDataSourceRegIdAndAllLanguageKeyValue.Add(dataSourceRegisterId, GetAllLanguageKeyValue());
                }



                List<DataRow> allLanguageKeyValue = _DictDataSourceRegIdAndAllLanguageKeyValue[dataSourceRegisterId];

                var toReturn = allLanguageKeyValue.Where(r => (r[fieldName] as int?).HasValue)
                               .GroupBy(x => x["LanguageID"].ToString() + "_" + x[fieldName])
                              .Select(g => g.First())
                              .ToDictionary(x => dataSourceRegisterId.ToString() + "_" + x["LanguageID"].ToString() + "_" + x[fieldName], x => x["LanguageText"].ToString());

                if (toReturn != null)
                {
                    return toReturn;

                }
            }


            return new Dictionary<string, string>();
        }











        private static List<DataRow> GetAllLanguageKeyValue()
        {

            Dictionary<string, string> toReturn = new Dictionary<string, string>();

            string queryAllLanguage = @" select 
              [SysLableLanguageID]
              ,[LanguageID]
              ,[MenuID]
              ,[TransactionFieldID]
              ,[FormID]
              ,[LanguageText]
              ,[TransactionUnitLinkedSearchId]
              ,[LinkTargetID]
              ,[SearchViewFieldID]
              ,[SearchFieldID]
              ,[TransactionUnitID]
              ,[SearchViewID]
              ,[SearchID]
               FROM [dbo].[AppSysLabelLanguage]";

            DataTable toreturn = new DataTable();
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                toreturn = adpater.ExecuteDataTableRetrievalQuery(queryAllLanguage, new List<System.Data.SqlClient.SqlParameter>());

            }
            return toreturn.AsEnumerable().ToList<DataRow>();



        }

        #endregion



    }
}



