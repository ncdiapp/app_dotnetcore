using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Dynamic;
using Newtonsoft.Json;
using PlmExpressionEval;

using APP.Framework;
namespace App.BL
{
    public static class AppMasterDetailFormReadOnlyDtoDataLoadBL
    {
        public static readonly string TransactionId = "TransactionId";


        public static AppMasterDetailDto GetMasterDetailFormData(int transactionId, object rootPrimaryKeyValue, int? autoExecuteCommandId = null, StaticSearchResultRowJsonDto selectDataRow = null)
        {

            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            if (rootPrimaryKeyValue == null)
            {
                return null;
            }



            AppMasterDetailDto aAppformDataDto = null;

            Dictionary<string, object> dictMasterPkValues = new Dictionary<string, object>();

            if (transactionExDto.TransactionOrganizedType.Value == (int)EmTransactionOrganizedType.MasterDetail)
            {

                // need to parse  rootPrimaryKeyValue as pk1=1|2|3
                //

                AppTransactionUnitExDto rootMasterUnit = transactionExDto.RootMasterUnit;
                foreach (string pk in rootMasterUnit.PrimaryKeyDbfieldList)
                {
                    dictMasterPkValues[pk] = rootPrimaryKeyValue;
                }

                aAppformDataDto = PopulateMasterDetailFormData(transactionExDto, dictMasterPkValues);

                aAppformDataDto.TransactionId = transactionId;
                aAppformDataDto.FormID = transactionExDto.FormId;



            }

            if (aAppformDataDto != null)
            {



                aAppformDataDto.RootPrimaryKeyValue = rootPrimaryKeyValue;

                var titleDisplayField = transactionExDto.RootMasterUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).FirstOrDefault(o => o.ControlType == (int)EmAppControlType.TextBox);
                if (titleDisplayField != null)
                {
                    string filedDbName = titleDisplayField.DataBaseFieldName;
                    aAppformDataDto.FormTitleDisplay = aAppformDataDto.DictOneToOneFields[filedDbName];
                }



            }


            AppTransactionTemplateDataLoadBL.LoadAutoExcutedTransactionTemplateData(aAppformDataDto, false);

            if (transactionExDto.CommandActionList != null)
            {
                var autoExecuteCommand = transactionExDto.CommandActionList.FirstOrDefault(o =>
                o.ActionAttribute != null
                && o.ActionAttribute.LinkToUI.HasValue && o.ActionAttribute.LinkToUI.Value
                && o.ActionAttribute.IsAutoExecuteOnFormOpen.HasValue && o.ActionAttribute.IsAutoExecuteOnFormOpen.Value);

                if (autoExecuteCommand != null)
                {
                    aAppformDataDto.TransactionCommandId = (int)autoExecuteCommand.Id;
                    var commandResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(aAppformDataDto);
                    if (commandResult.IsSuccessfulWithResult && commandResult.Object.FormData != null)
                    {
                        aAppformDataDto = commandResult.Object.FormData;
                    }
                }
            }

            if (autoExecuteCommandId.HasValue)
            {
                aAppformDataDto.TransactionCommandId = autoExecuteCommandId;
                var commandResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(aAppformDataDto);
                if (commandResult.IsSuccessfulWithResult && commandResult.Object.FormData != null)
                {
                    aAppformDataDto = commandResult.Object.FormData;
                }
            }

            aAppformDataDto.IsDirty = false;

            return aAppformDataDto;
        }


        private static AppMasterDetailDto PopulateMasterDetailFormData(AppTransactionExDto appTransactionExDto, Dictionary<string, object> rootPrimaryKeyValue)
        {
            int TransactionId = (int)appTransactionExDto.Id;
            AppMasterDetailDto masterDetailDto = new AppMasterDetailDto();
            masterDetailDto.TransactionId = TransactionId;
            masterDetailDto.IsShowSaveButton = appTransactionExDto.IsShowSaveButton;

            // only one root, and root only pass one root key value
            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);
            masterDetailDto.RootUnitId = rootMasterUnit.Id;

            masterDetailDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            masterDetailDto.DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>();
            masterDetailDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
            masterDetailDto.DictAutoCompleteFieldDataSource = new Dictionary<string, List<LookupItemDto>>();
            masterDetailDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();

            SetupReaOnlyRootUnitDictValue(rootPrimaryKeyValue, masterDetailDto, rootMasterUnit);



            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootMasterUnit.Children)
            {
                List<AppChildDataDto> childAppformChildDataDto = SetupReadOnlyOneChildAndGrandChildData(masterDetailDto.DictOneToOneFields, aChildTransactionUnitExDto);
                masterDetailDto.DictOneToManyFields.Add(aChildTransactionUnitExDto.Id.ToString(), childAppformChildDataDto);
            }



            return masterDetailDto;
        }

        private static List<AppChildDataDto> SetupReadOnlyOneChildAndGrandChildData(Dictionary<string, object> dictRootOneToOneValue, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            if (!aChildTransactionUnitExDto.IsVirtualUnit)
            {
                DataTable childdataTble;
                List<Dictionary<string, object>> childPkValueIDList = GetReadOnlyChildPkValueIDList(dictRootOneToOneValue, aChildTransactionUnitExDto, out childdataTble);

                Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble = GetGrandChildDataTable(aChildTransactionUnitExDto, childPkValueIDList);

                List<AppChildDataDto> childAppformChildDataDto = AppMasterDetailFormDataLoadBL.ConvertChildAndGradnChildTableToAppChildDataDtoList(aChildTransactionUnitExDto, childdataTble, dictGrandChilddataTble);

                return childAppformChildDataDto;
            }
            else
            {
                return new List<AppChildDataDto>();
            }
        }
        private static void SetupReaOnlyRootUnitDictValue(Dictionary<string, object> rootPrimaryKeyValue, AppMasterDetailDto rootAppformDataDto,
           AppTransactionUnitExDto rootMasterUnit)
        {
            if (!rootMasterUnit.IsVirtualUnit)
            {
                DataRow row = AppDbHelerBL.RetriveOneDataRowWtihPrimayKeyFromSQLQury(rootPrimaryKeyValue, rootMasterUnit);

                Dictionary<string, object> dictOneToOneFields = AppMasterDetailFormDataLoadBL.MappingDbFiledToTransField(rootMasterUnit, row);

                rootAppformDataDto.DictOneToOneFields = dictOneToOneFields;
            }
            else
            {
                rootAppformDataDto.DictOneToOneFields = new Dictionary<string, object>();
            }

        }


        private static List<Dictionary<string, object>> GetReadOnlyChildPkValueIDList(Dictionary<string, object> dictRootOneToOneValue,
           AppTransactionUnitExDto aChildTransactionUnitExDto, out DataTable childdataTble)
        {
            var childPkValueIDList = new List<Dictionary<string, object>>();
            childdataTble = GetReadOnlyChildDataTable(dictRootOneToOneValue, aChildTransactionUnitExDto);

            if (childdataTble != null)
            {

                foreach (DataRow row in childdataTble.Rows)
                {
                    Dictionary<string, object> dictPkValue = new Dictionary<string, object>();

                    foreach (string PKfILE in aChildTransactionUnitExDto.PrimaryKeyDbfieldList)
                    {
                        dictPkValue[PKfILE] = row[PKfILE];
                    }


                    //AppDbHelerBL.ConvertOneDataRowToOneToOneDict(row);
                    childPkValueIDList.Add(dictPkValue);
                }

            }
            return childPkValueIDList;
        }



        private static Dictionary<AppTransactionUnitExDto, DataTable> GetGrandChildDataTable(AppTransactionUnitExDto aChildTransactionUnitExDto, List<Dictionary<string, object>> childPkValueIDList)
        {
            Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble = new Dictionary<AppTransactionUnitExDto, DataTable>();

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aChildTransactionUnitExDto.DataSourceFrom.Value);

            if (childPkValueIDList.Count > 0)
            {
                foreach (AppTransactionUnitExDto aGrandchildAppTransactionUnitExDto in aChildTransactionUnitExDto.Children)
                {

                    string whereCaluse = string.Empty;


                    string grandChildQuery = aGrandchildAppTransactionUnitExDto.DataSourceQuery;
                    if (grandChildQuery != string.Empty)
                    {
                        string includingField = aGrandchildAppTransactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrEmpty(o.DataBaseFieldName))
                         .Select(o => o.DataBaseFieldName).Aggregate((i, j) => i + "," + j);

                        List<DbParameter> paraList = new List<DbParameter>();
                        paraList = AppMasterDetailFormDataLoadBL.SetupGrandChildWhereClasueWithChildPkValue(childPkValueIDList, aGrandchildAppTransactionUnitExDto, ref whereCaluse);

                        string aQuery = $@" SELECT {includingField} FROM ({aGrandchildAppTransactionUnitExDto.DataSourceQuery}) as DynTable WHERE  {whereCaluse}";

                        var grandChilddataTble = databaseFixtureInstance.RetriveDataTable(grandChildQuery, paraList);

                        dictGrandChilddataTble[aGrandchildAppTransactionUnitExDto] = grandChilddataTble;
                    }
                    else
                    {
                        dictGrandChilddataTble[aGrandchildAppTransactionUnitExDto] = null;
                    }


                }
            }

            return dictGrandChilddataTble;
        }



        private static DataTable GetReadOnlyChildDataTable(Dictionary<string, object> dictRootOneToOneValue, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            //string childtableName = aChildTransactionUnitExDto.DataBaseTableName;

            if (!string.IsNullOrWhiteSpace(aChildTransactionUnitExDto.DataSourceQuery))
            {


                // Master primary key only has one key, for sibling unit alway take first one
                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aChildTransactionUnitExDto.DataSourceFrom.Value);




                string includingField = aChildTransactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrEmpty(o.DataBaseFieldName))
                 .Select(o => o.DataBaseFieldName).Aggregate((i, j) => i + "," + j);


                string linkToParentWhere = "";

                List<DbParameter> listParamters = AppMasterDetailFormDataLoadBL.GetChildWhereClauseParamters(dictRootOneToOneValue, aChildTransactionUnitExDto, ref linkToParentWhere);

                DataTable childdataTble = new DataTable();
                if (!string.IsNullOrWhiteSpace(linkToParentWhere))
                {


                    string childQuery = $@" SELECT {includingField} FROM ({aChildTransactionUnitExDto.DataSourceQuery}) as DynTable WHERE  {linkToParentWhere}";
                    childdataTble = databaseFixtureInstance.RetriveDataTable(childQuery, listParamters); //adpater.ExecuteDataTableRetrievalQuery(childQuery, listParamters);

                }

                return childdataTble;
            }

            return null;
        }













    }
}