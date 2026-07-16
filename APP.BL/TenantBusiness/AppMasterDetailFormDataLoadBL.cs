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
using ExpressionEval;

using APP.Framework;
namespace App.BL
{
    public static class AppMasterDetailFormDataLoadBL
    {
        public static readonly string TransactionId = "TransactionId";


        public static AppMasterDetailDto GetMasterDetailFormData(int transactionId, object rootPrimaryKeyValue, int? autoExecuteCommandId = null, StaticSearchResultRowJsonDto selectDataRow = null)
        {

            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            if (rootPrimaryKeyValue == null)
            {
                return AppMasterDetailFormDataLoadBL.GetNewFormData(transactionExDto);
            }

            // ReadOnly Master Detail Dto load
            if (transactionExDto.IsReadOnly.HasValue && transactionExDto.IsReadOnly.Value )
            {
                return AppMasterDetailFormReadOnlyDtoDataLoadBL.GetMasterDetailFormData(transactionId, rootPrimaryKeyValue, autoExecuteCommandId, selectDataRow);
            }

            // From  IsApiIntegrationTransaction
            if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {
                AppMasterDetailDto toReturn = AppMasterDetailApiFormDataLoadBL.GetApiMasterDetailFormData(transactionId, rootPrimaryKeyValue);

                if(selectDataRow != null)
                {
                    if(selectDataRow.SelectedRowLinkTargetId.HasValue )
                    {
                        var linkTargetDto = LinkTragetBL.RetrieveOneAppFormLinkTargetDto(selectDataRow.SelectedRowLinkTargetId);
                        if(linkTargetDto.OtherSettingsDto != null && linkTargetDto.OtherSettingsDto.DictTransDbFiledSearchFieldMappingAfterApiCall != null)
                        {
                            var dbFiledMapSearchFiled = linkTargetDto.OtherSettingsDto.DictTransDbFiledSearchFieldMappingAfterApiCall;
                            foreach(string dbFiled in dbFiledMapSearchFiled.Keys)
                            {
                                int searchFiledId = dbFiledMapSearchFiled[dbFiled];
                                toReturn.DictOneToOneFields[dbFiled] = selectDataRow[searchFiledId];
                            }
                            
                        }
                    }
                }

                return toReturn;
            }

            //  real trasction Master Detail Dto loading
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

                aAppformDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                aAppformDataDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
                //aAppformDataDto.DictAutoCompleteFieldDataSource = new Dictionary<string, List<LookupItemDto>>();

                AppTransactionTemplateDataLoadBL.LoadAutoExcutedTransactionTemplateData(aAppformDataDto, true);

                AppCascadingBL.SetupIntialCscadingFieldDataSource(transactionExDto, aAppformDataDto, false);

                foreach (var fieldDtoDto in transactionExDto.DictAllTransactionField.Values)
                {
                    if (!fieldDtoDto.AppConditionalAction__List.IsEmpty())
                    {
                        aAppformDataDto.IsChangedNeedToCascadingFiedIds.Add(fieldDtoDto.Id.ToString());
                    }
                }

                AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(transactionExDto, aAppformDataDto, false);

            }

            if (aAppformDataDto != null)
            {
                aAppformDataDto.DictDocumentIdFileCode = new Dictionary<int, string>();
                aAppformDataDto.DictFolderIdAndPath = new Dictionary<int, string>();


                aAppformDataDto.RootPrimaryKeyValue = rootPrimaryKeyValue;

                var titleDisplayField = transactionExDto.RootMasterUnit.AppTransactionFieldList.OrderBy(o => o.SortOrder).FirstOrDefault(o => o.ControlType == (int)EmAppControlType.TextBox);
                if (titleDisplayField != null)
                {
                    string filedDbName = titleDisplayField.DataBaseFieldName;
                    aAppformDataDto.FormTitleDisplay = aAppformDataDto.DictOneToOneFields[filedDbName];
                }


                //!!! need to change the code below to only retrieve the Files used by current form data.
                //aAppformDataDto.DictDocumentIdFileCode = AppFileBL.RetrieveAllAppFileEntity().ToDictionary(o => o.FileId, o => o.FileCode);
                // set up Specail column DDL
                //// it is like cascaing
                // ddl will be treaed as  ,keep it in  aAppformDataDto.DictCascadingFiledDataSource

                ProcessMasterDetailFormFileIDCodeDictionary(transactionExDto, aAppformDataDto);

                if (transactionExDto.MgtRootFolderId.HasValue)
                {
                    ProcessMasterDetailFormFolderIdAndPathDictionary(transactionExDto, aAppformDataDto);
                }

                SetupDdlQueryLookItem(transactionExDto, aAppformDataDto);

                SetupDdlForeignChildUnitLookItem(transactionExDto, aAppformDataDto);

                SetupAvailableSourceUnitSelectedRows(transactionExDto, aAppformDataDto);
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


        public static Dictionary<string, object> GetTransUnitNameDataModelData(AppMasterDetailDto aAppformDataDto)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppformDataDto.TransactionId);

            //aAppformDataDto = GetMasterDetailFormData(transactionId, rootPrimaryKeyValue);

            // dynamic expandObject = new ExpandoObject();
            // var dictRetrun = ((IDictionary<string, object>)expandObject);


            Dictionary<string, object> dictRetrun = new Dictionary<string, object>();



            //Process One to One
            foreach (string key in aAppformDataDto.DictOneToOneFields.Keys)
            {
                dictRetrun[key] = aAppformDataDto.DictOneToOneFields[key];
            }

            // Process Sibling
            foreach (var siblingUnitDto in transactionExDto.SibLineTransactionUnitIdExDtoList)
            {

                dictRetrun[siblingUnitDto.DataBaseTableName] = "";
                string siblingUnitId = siblingUnitDto.Id.ToString();
                if (aAppformDataDto.DictSiblingOneToOneFields.ContainsKey(siblingUnitId))
                {
                    dictRetrun[siblingUnitDto.DataBaseTableName] = aAppformDataDto.DictSiblingOneToOneFields[siblingUnitId];
                }

            }

            // Process Child and Grand Child
            var rootUnitDto = transactionExDto.AppTransactionUnitList.First();
            foreach (var childUnitDto in rootUnitDto.Children)
            {

                dictRetrun[childUnitDto.DataBaseTableName] = "";
                string childUnitId = childUnitDto.Id.ToString();
                if (aAppformDataDto.DictOneToManyFields.ContainsKey(childUnitId))
                {
                    List<AppChildDataDto> childDtoList = aAppformDataDto.DictOneToManyFields[childUnitId];
                    List<Dictionary<string, object>> childDcitList = new List<Dictionary<string, object>>();

                    foreach (AppChildDataDto aAppChildDataDto in childDtoList)
                    {
                        Dictionary<string, object> oneChildREcod = new Dictionary<string, object>();

                        childDcitList.Add(oneChildREcod);
                        // child property
                        foreach (string key in aAppChildDataDto.DictOneToOneFields.Keys)
                        {
                            oneChildREcod[key] = aAppChildDataDto.DictOneToOneFields[key];
                        }
                        // Child Sibling Proper


                        // Child Grand Child unit
                        foreach (var grandChild in childUnitDto.Children)
                        {
                            string grandchildUnitId = grandChild.Id.ToString();
                            var grandChildListDto = aAppChildDataDto.DictOneToManyFields[grandchildUnitId];

                            List<Dictionary<string, object>> grandChildDcitList = ConvertGrandDto(grandChildListDto, grandChild);

                            oneChildREcod[grandChild.DataBaseTableName] = grandChildDcitList;

                        }


                    }


                    dictRetrun[childUnitDto.DataBaseTableName] = childDcitList;


                }

            }

            dictRetrun[TransactionId] = aAppformDataDto.TransactionId;

            return dictRetrun;
        }
        //TODO
        public static Dictionary<string, object> SaveDataModelData(Dictionary<string, object> dictSrcData)
        {
            int? transactionId = ControlTypeValueConverter.ConvertValueToInt(dictSrcData[TransactionId]);



            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);



            AppMasterDetailDto aAppformDataDto = new AppMasterDetailDto();

            Dictionary<string, object> rootDictOneToOne = new Dictionary<string, object>();

            aAppformDataDto.DictOneToOneFields = rootDictOneToOne;

            // Root Unit Filed
            var rootUnit = transactionExDto.RootMasterUnit;
            foreach (string filed in rootUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName))
            {
                rootDictOneToOne[filed] = dictSrcData[filed];
            }
            // Sibling Field 

            // Process Sibling
            var dictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>();
            aAppformDataDto.DictSiblingOneToOneFields = dictSiblingOneToOneFields;

            foreach (var siblingUnitDto in transactionExDto.SibLineTransactionUnitIdExDtoList)
            {
                string siblingUnitId = siblingUnitDto.Id.ToString();

                JObject sibChild = dictSrcData[siblingUnitDto.DataBaseTableName] as JObject;
                //https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JTokenType.htm
                //if (sibChild.Type == JTokenType.Array || sibChild.Type == JTokenType.A JSON object.

                var values = sibChild.ToObject<Dictionary<string, object>>();
                //var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                dictSiblingOneToOneFields[siblingUnitId] = values;

            }

            //  // Process  child 
            Dictionary<string, List<AppChildDataDto>> dictChildAppDate = new Dictionary<string, List<AppChildDataDto>>();

            aAppformDataDto.DictOneToManyFields = dictChildAppDate;


            foreach (var childUnitDto in rootUnit.Children)
            {
                List<AppChildDataDto> oneChildDate = ExtractOneChildUnit(dictSrcData, childUnitDto);

                dictChildAppDate[childUnitDto.Id.ToString()] = oneChildDate;

            }



            return dictSrcData;

        }

               



        private static List<AppChildDataDto> ExtractOneChildUnit(Dictionary<string, object> dictSrcData, AppTransactionUnitExDto childUnitDto)
        {
            List<AppChildDataDto> childListValue = new List<AppChildDataDto>();
            string unitName = childUnitDto.DataBaseTableName;

            if (!dictSrcData.ContainsKey(unitName) || dictSrcData[unitName] == null)
                return childListValue;


            JObject childJobject = dictSrcData[unitName] as JObject;

            string chidUnitIdstr = childUnitDto.Id.ToString();




            if (childJobject != null && childJobject.Type == JTokenType.Array)
            {
                List<Dictionary<string, object>> childDictListVAlue = childJobject.ToObject<List<Dictionary<string, object>>>();

                foreach (Dictionary<string, object> onechildSrcRow in childDictListVAlue)
                {
                    AppChildDataDto aAppChildDataDto = ExtractOneChidDto(childUnitDto, onechildSrcRow);
                    childListValue.Add(aAppChildDataDto);


                    Dictionary<string, List<AppChildDataDto>> dictgrandChildAppDate = new Dictionary<string, List<AppChildDataDto>>();
                    aAppChildDataDto.DictOneToManyFields = dictgrandChildAppDate;

                    foreach (var grandchildUnitDto in childUnitDto.Children)
                    {
                        List<AppChildDataDto> oneChildDate = ExtractOneChildUnit(onechildSrcRow, grandchildUnitDto);

                        dictgrandChildAppDate[grandchildUnitDto.Id.ToString()] = oneChildDate;

                    }

                }

            }

            return childListValue;

        }

        private static AppChildDataDto ExtractOneChidDto(AppTransactionUnitExDto childUnit, Dictionary<string, object> onechildSrcRow)
        {
            AppChildDataDto aAppChildDataDto = new AppChildDataDto();
            Dictionary<string, object> dictChidOneTone = new Dictionary<string, object>();
            aAppChildDataDto.DictOneToOneFields = dictChidOneTone;
            foreach (string filed in childUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName))
            {
                dictChidOneTone[filed] = onechildSrcRow[filed];
            }

            return aAppChildDataDto;
        }

        private static List<Dictionary<string, object>> ConvertGrandDto(List<AppChildDataDto> grandChildListDto, AppTransactionUnitExDto grandChild)
        {
            List<Dictionary<string, object>> grandChildDcitList = new List<Dictionary<string, object>>();


            string grandchildUnitId = grandChild.Id.ToString();
            foreach (AppChildDataDto grandhildOneToOne in grandChildListDto)
            {
                Dictionary<string, object> oneGrandChildREcod = new Dictionary<string, object>();

                grandChildDcitList.Add(oneGrandChildREcod);
                // child property
                foreach (string key in grandhildOneToOne.DictOneToOneFields.Keys)
                {
                    oneGrandChildREcod[key] = grandhildOneToOne.DictOneToOneFields[key];
                }

            }

            return grandChildDcitList;
        }

        public static void SetupDdlQueryLookItem(AppTransactionExDto transactionExDto, AppMasterDetailDto aAppformDataDto)
        {
            var dictAllFieds = transactionExDto.DictAllTransactionField;
            var ddlQueryFuelds = transactionExDto.DictAllTransactionField.Values.Where(
                o => !String.IsNullOrWhiteSpace(o.DdlQueryText));

            if (transactionExDto.DataSourceFrom.HasValue)
            {
                foreach (var specailDdlFiedl in ddlQueryFuelds)
                {
                    SetupOneFiledDDLQuery(aAppformDataDto, dictAllFieds, specailDdlFiedl, transactionExDto.DataSourceFrom.Value);
                }
            }

        }

        public static void SetupDdlForeignChildUnitLookItem(AppTransactionExDto transactionExDto, AppMasterDetailDto aAppformDataDto)
        {

            var dictForeignUnitMasterFieldlIdLookupItem = new Dictionary<string, Dictionary<string, List<LookupItemDto>>>();


            foreach (string unitId in aAppformDataDto.DictOneToManyFields.Keys)
            {

                AppTransactionUnitExDto currentUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[unitId];
                Dictionary<string, List<LookupItemDto>> dictMasterEntityColumnValueList =
                 SetupOneUnitForeighUnitDataSource(transactionExDto, aAppformDataDto, currentUnitExDto);
                dictForeignUnitMasterFieldlIdLookupItem[currentUnitExDto.Id.ToString()] = dictMasterEntityColumnValueList;

            }


            aAppformDataDto.DictForeignUnitMasterFieldlIdLookupItem = dictForeignUnitMasterFieldlIdLookupItem;



        }


        private static void SetupAvailableSourceUnitSelectedRows(AppTransactionExDto transactionExDto, AppMasterDetailDto aAppformDataDto)
        {
            foreach (var aUnitExDto in transactionExDto.DictAllTransactionUnitIdExDto.Values)
            {
                if (aUnitExDto.AvailableSourceUnitId.HasValue)
                {
                    var mappingTransField = aUnitExDto.AppTransactionFieldList.FirstOrDefault(o => o.MappingToAvailableSourceUnitTransactionFieldExDto != null);
                    if (mappingTransField != null)
                    {
                        AppTransactionFieldExDto srcTransField = mappingTransField.MappingToAvailableSourceUnitTransactionFieldExDto;

                        string sourceUnitId = aUnitExDto.AvailableSourceUnitId.Value.ToString();

                        if (aAppformDataDto.DictOneToManyFields.ContainsKey(sourceUnitId) && aAppformDataDto.DictOneToManyFields.ContainsKey(aUnitExDto.Id.ToString()))
                        {
                            List<AppChildDataDto> srcUnitRows = aAppformDataDto.DictOneToManyFields[sourceUnitId];
                            List<AppChildDataDto> resultUnitRows = aAppformDataDto.DictOneToManyFields[aUnitExDto.Id.ToString()];

                            List<string> selectedIdList = resultUnitRows
                                .Select(o => ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(o.DictOneToOneFields[mappingTransField.DataBaseFieldName]))
                                .Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList();

                            srcUnitRows.Where(o => o.DictOneToOneFields[srcTransField.DataBaseFieldName] != null && selectedIdList.Contains(o.DictOneToOneFields[srcTransField.DataBaseFieldName].ToString()))
                                .ForAll(o => o.IsSelected = true);
                        }
                    }
                }

            }
        }


        private static Dictionary<string, List<LookupItemDto>> SetupOneUnitForeighUnitDataSource(AppTransactionExDto transactionExDto, AppMasterDetailDto aAppformDataDto, AppTransactionUnitExDto currentUnitExDto)
        {
            List<int> masterEntiyColumnIds = currentUnitExDto.AppTransactionFieldList.Where(o => o.MasterEntityFieldlId.HasValue).Select(o => o.MasterEntityFieldlId.Value).ToList();
            var aasterEntityColumnList = currentUnitExDto.AppTransactionFieldList.Where(
            o => masterEntiyColumnIds.Contains((int)o.Id)
            && (!o.EntityId.HasValue)
            && (o.DdlForeignUnitId.HasValue)).ToList();


            Dictionary<int, int> dictMasterColumnIdForeignUnitId = aasterEntityColumnList
               //.Where(o => o.GridColumnId == masterEntityColumnId)
               .Where(col => col.DdlForeignUnitId.HasValue)
               .ToDictionary(col => (int)col.Id, col => col.DdlForeignUnitId.Value);

            Dictionary<string, List<LookupItemDto>> dictMasterEntityColumnValueList = new Dictionary<string, List<LookupItemDto>>();


            foreach (int masterEntityColumnId in dictMasterColumnIdForeignUnitId.Keys)
            {

                List<LookupItemDto> lookItemList = new List<LookupItemDto>();

                dictMasterEntityColumnValueList[masterEntityColumnId.ToString()] = lookItemList;

                var masterFieldExDto = transactionExDto.DictAllTransactionField[masterEntityColumnId];

                int foreignUnitId = dictMasterColumnIdForeignUnitId[masterEntityColumnId];
                var foreignnitExdto = transactionExDto.DictAllTransactionUnitIdExDto[foreignUnitId.ToString()];

                //ignore ,DdlForeignUnitDisplayDbFieds 
                var displayFiedIds = foreignnitExdto.AppTransactionFieldList.Where(o => o.IsLogicalDisplay.HasValue && o.IsLogicalDisplay.Value).OrderBy(o => o.SortOrder).Select(o => o.DataBaseFieldName).ToList();
                string logicaKey = foreignnitExdto.AppTransactionFieldList.Where(o => o.IsUnique.HasValue && o.IsUnique.Value).Select(o => o.DataBaseFieldName).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(logicaKey))
                {
                    if (aAppformDataDto.DictOneToManyFields.ContainsKey(foreignUnitId.ToString()))
                    {

                        List<AppChildDataDto> foreighUnitDataRowList = aAppformDataDto.DictOneToManyFields[foreignUnitId.ToString()];



                        foreach (AppChildDataDto foreignDataRoe in foreighUnitDataRowList)
                        {
                            LookupItemDto aLookupItemDto = new LookupItemDto();

                            aLookupItemDto.Id = foreignDataRoe.DictOneToOneFields[logicaKey];

                            string display = "";

                            foreach (var displayFied in displayFiedIds)
                            {
                                string strValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(foreignDataRoe.DictOneToOneFields[displayFied]);

                                if (string.IsNullOrEmpty(display))
                                {
                                    display = strValue;
                                }
                                else
                                {
                                    display = display + ", " + strValue;
                                }


                            }

                            aLookupItemDto.Display = display;

                            lookItemList.Add(aLookupItemDto);

                        }
                    }
                }

            }

            return dictMasterEntityColumnValueList;
        }

        internal static void SetupStandAloneEntityDepedentFiled(List<AppChildDataDto> childAppformChildDataDto, AppTransactionUnitExDto unitExDto)
        {
            List<int> masterEntiyColumnIds = unitExDto.AppTransactionFieldList.Where(o => o.MasterEntityFieldlId.HasValue).Select(o => o.MasterEntityFieldlId.Value).ToList();
            var aasterEntityColumnList = unitExDto.AppTransactionFieldList.Where(
            o => masterEntiyColumnIds.Contains((int)o.Id)
            && o.EntityId.HasValue
            && (!o.DdlForeignUnitId.HasValue)).ToList();


            Dictionary<int, int> dictMasterEntityColumnIdEntityIds = aasterEntityColumnList
               //.Where(o => o.GridColumnId == masterEntityColumnId)
               .Where(col => col.EntityId.HasValue)
               .ToDictionary(col => (int)col.Id, col => col.EntityId.Value);

            Dictionary<int, List<object>> dictMasterEntityColumnValueList = new Dictionary<int, List<object>>();
            foreach (var GridColumnId in dictMasterEntityColumnIdEntityIds.Keys)
            {
                dictMasterEntityColumnValueList.Add(GridColumnId, new List<object>());
            }
            foreach (AppChildDataDto row in childAppformChildDataDto)
            {
                foreach (var masterEntityColumnId in dictMasterEntityColumnIdEntityIds.Keys)
                {

                    var dbFieldDto = unitExDto.DicFieldIdFieldExdto[masterEntityColumnId];
                    string dbFiledName = dbFieldDto.DataBaseFieldName;
                    object keyValueId = row.DictOneToOneFields[dbFiledName];

                    dictMasterEntityColumnValueList[masterEntityColumnId].Add(keyValueId);
                }
            }


            Dictionary<int, DataTable> dictMasterColumnValueListQueryResult = new Dictionary<int, DataTable>();

            foreach (var columnIdEntity in dictMasterEntityColumnIdEntityIds)
            {
                int masterEntityGridColumnId = columnIdEntity.Key;
                int entityId = columnIdEntity.Value;


                if (dictMasterEntityColumnValueList.ContainsKey(masterEntityGridColumnId))
                {
                    var valueIdList = dictMasterEntityColumnValueList[masterEntityGridColumnId].ToList();

                    List<string> dependentDbColumnNames = new List<string>();

                    dependentDbColumnNames = unitExDto.AppTransactionFieldList
                        .Where(o => o.MasterEntityFieldlId.HasValue && o.MasterEntityFieldlId.Value == masterEntityGridColumnId && !string.IsNullOrWhiteSpace(o.InnerEntitySubscribeFiled))

                        .Select(o => o.InnerEntitySubscribeFiled).ToList();


                    DataTable masterColumnForeighDataTable = AppEntityInfoBL.GetEntitySelectColumnRowValue(entityId, valueIdList, dependentDbColumnNames);
                    if (masterColumnForeighDataTable.Rows.Count > 0)
                    {
                        dictMasterColumnValueListQueryResult[masterEntityGridColumnId] = masterColumnForeighDataTable;
                    }




                }
            }

            //  process master entity dependent column

            foreach (var KeyValue in dictMasterColumnValueListQueryResult)
            {
                int masterColumnId = KeyValue.Key;
                var masterColumnForeighDataTable = KeyValue.Value;

                ProcessSystemDefineEntityColumn(unitExDto, childAppformChildDataDto, masterColumnForeighDataTable, masterColumnId);

            }
        }


        private static void ProcessSystemDefineEntityColumn(AppTransactionUnitExDto unitExDto,
            List<AppChildDataDto> childAppformChildDataDto, DataTable masterColumnDataTable, int masterColumnId)
        {
            // DataTable aDtResult = dictMasterColumnValueListQueryResult[masterColumnId] as DataTable;
            var dictRowIdforeignEntityDataRow = masterColumnDataTable.AsEnumerable().ToDictionary(dtRow => dtRow["Id"].ToString(), dtRow => dtRow);

            string masterColumnDBFiled = unitExDto.DicFieldIdFieldExdto[masterColumnId].DataBaseFieldName;
            var dependentColumn = unitExDto.AppTransactionFieldList.Where(o => o.MasterEntityFieldlId.HasValue && o.MasterEntityFieldlId.Value == masterColumnId).ToList();

            foreach (AppChildDataDto childDataDto in childAppformChildDataDto)
            {

                string masterDbFiledvalueId = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(childDataDto.DictOneToOneFields[masterColumnDBFiled]);

                if (dictRowIdforeignEntityDataRow.ContainsKey(masterDbFiledvalueId))
                {
                    var dtRow = dictRowIdforeignEntityDataRow[masterDbFiledvalueId];


                    foreach (var dependentEntityColumn in dependentColumn)
                    {
                        foreach (DataColumn aDtcolumn in masterColumnDataTable.Columns)
                        {
                            if (aDtcolumn.ColumnName == dependentEntityColumn.InnerEntitySubscribeFiled)
                            {

                                childDataDto.DictOneToOneFields[dependentEntityColumn.DataBaseFieldName] = dtRow[aDtcolumn];
                            }
                        }
                    }
                }

            }
        }
        private static void SetupOneFiledDDLQuery(AppMasterDetailDto aAppformDataDto, Dictionary<int, AppTransactionFieldExDto> dictAllFieds, AppTransactionFieldExDto specailDdlFiedl, int dataSourceRegistrationId)
        {
            string query = specailDdlFiedl.DdlQueryText;



            string whareClaseu = specailDdlFiedl.WhereClauseExpress ?? string.Empty;

            string[] fieldParaname = string.IsNullOrWhiteSpace(whareClaseu)
                ? Array.Empty<string>()
                : whareClaseu.Split("|".ToCharArray());

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegistrationId);


            List<DbParameter> sqlParamters = new List<DbParameter>();

            for (int index = 0; index < fieldParaname.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(fieldParaname[index]))
                {
                    continue;
                }

                int filedId = int.Parse(fieldParaname[index].Trim());

                if (dictAllFieds.ContainsKey(filedId))
                {
                    var fiedExdto = dictAllFieds[filedId];
                    int unitId = fiedExdto.TransactionUnitId;

                    if (aAppformDataDto.RootUnitId.ToString() == unitId.ToString())
                    {
                        object value = aAppformDataDto.DictOneToOneFields[fiedExdto.DataBaseFieldName];

                        string p = "p" + index;
                        DbParameter parameter = databaseFixtureInstance.CreateParameter(p);
                        parameter.Value = value ?? DBNull.Value;
                        sqlParamters.Add(parameter);

                        //?? null-coalescing operator
                        //sqlParamters.Add(new DbParameter("@p" + index, value ?? DBNull.Value));
                    }
                    //Sibling unit Field
                    else if (aAppformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                    {
                        Dictionary<string, object> siblingDictOneToOneFields = aAppformDataDto.DictSiblingOneToOneFields[unitId.ToString()];

                        //	aAppformDataDto.DictSiblingOneToOneFields[unitId.ToString()];

                        object value = siblingDictOneToOneFields[fiedExdto.DataBaseFieldName];
                        string p = "p" + index;
                        DbParameter parameter = databaseFixtureInstance.CreateParameter(p);
                        parameter.Value = value ?? DBNull.Value;
                        sqlParamters.Add(parameter);
                    }
                    // Child unit trigger, Conisder as cascading etc....
                    else if (aAppformDataDto.DictSiblingOneToOneFields.ContainsKey(unitId.ToString()))
                    {
                        //Dictionary<string, object> siblingDictOneToOneFields = aAppformDataDto.DictSiblingOneToOneFields[unitId.ToString()];

                        ////	aAppformDataDto.DictSiblingOneToOneFields[unitId.ToString()];

                        //object value = siblingDictOneToOneFields[fiedExdto.DataBaseFieldName];

                        ////?? null-coalescing operator
                        //sqlParamters.Add(new DbParameter("@p" + index, value ?? DBNull.Value));
                    }
                }
            }


            DataTable dt = databaseFixtureInstance.RetriveDataTable(query, sqlParamters);

            List<LookupItemDto> listDisplay = new List<LookupItemDto>();

            foreach (DataRow row in dt.Rows)
            {
                var itmeDto = new LookupItemDto();
                itmeDto.Id = row[0] == DBNull.Value ? null : row[0];
                itmeDto.Display = row[1] == DBNull.Value ? string.Empty : row[1].ToString();
                listDisplay.Add(itmeDto);
            }
            //var list = dt.AsEnumerable().Select(
            //	row => new LookupItemDto { Id = row[0], Display = row[1].ToString () }).ToList();

            if (!aAppformDataDto.DictCascadingFiledDataSource.ContainsKey(specailDdlFiedl.Id.ToString()))
            {

                aAppformDataDto.DictCascadingFiledDataSource.Add(specailDdlFiedl.Id.ToString(), listDisplay);
            }
            else
            {
                aAppformDataDto.DictCascadingFiledDataSource[specailDdlFiedl.Id.ToString()] = listDisplay;
            }

        }


        public static AppMasterDetailDto GetNewFormData(int transactionId, bool isConfigTestRun = false)
        {
            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId); //AppTransactionBL.GetOneTransactionExDtoWithFormId(transactionId);

            if(transactionExDto.IsReadOnly .HasValue && transactionExDto.IsReadOnly.Value )
            {
                return GetReadonlyNewFormData(transactionId, isConfigTestRun);
            }

           else if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {

                if (isConfigTestRun)
                {
                    var rootPkValue = new Dictionary<string, object>();
                    return AppMasterDetailApiFormDataLoadBL.GetApiMasterDetailFormData(transactionId, rootPkValue);
                }
                else
                {
                    return AppMasterDetailApiFormDataLoadBL.GetNewFormData(transactionExDto);
                }

            }
            else
            {
                AppMasterDetailDto rootAppformDataDto = GetNewFormData(transactionExDto);

                rootAppformDataDto.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                rootAppformDataDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
                //rootAppformDataDto.DictAutoCompleteFieldDataSource = new Dictionary<string, List<LookupItemDto>>();

                AppCascadingBL.SetupIntialCscadingFieldDataSource(transactionExDto, rootAppformDataDto, true);
                AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(transactionExDto, rootAppformDataDto, true);

                rootAppformDataDto.IsNew = true;

                SetupFormConditionLockingDictValue(transactionExDto, rootAppformDataDto);
                var rootMasterUnit = transactionExDto.AppTransactionUnitList.First();

                PrepareCalendarRepeatSettingValues(rootAppformDataDto, rootMasterUnit);



                return rootAppformDataDto;
            }
          
        }


        private static AppMasterDetailDto GetReadonlyNewFormData(int transactionId, bool isConfigTestRun )
        {
            var hierachyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId); //AppTransactionBL.GetOneTransactionExDtoWithFormId(transactionId);

 

            AppMasterDetailDto masterDetailDto = new AppMasterDetailDto();

            masterDetailDto.TransactionId = (int)hierachyTransactionExDto.Id;
            masterDetailDto.IsShowSaveButton = hierachyTransactionExDto.IsShowSaveButton;


            AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(hierachyTransactionExDto);

            Dictionary<string, List<AppChildDataDto>> DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            masterDetailDto.DictOneToManyFields = DictOneToManyFields;

            // only one root
            AppTransactionUnitExDto rootTransactionUnit = hierachyTransactionExDto.AppTransactionUnitList.ElementAt(0);
            masterDetailDto.RootUnitId = rootTransactionUnit.Id;

           // masterDetailDto.DictOneToOneFields = new Dictionary<string, object>();

            var dictOneToOneFields = new Dictionary<string, object>();

            masterDetailDto.DictOneToOneFields = dictOneToOneFields;

            //  masterDetailDto.RootTransactionUnit = rootTransactionUnit;

            SetupReadOnlyRootUnit(rootTransactionUnit,  dictOneToOneFields);

            //  need to add sibling default

            Dictionary<string, Dictionary<string, object>> dictSiblingValue = masterDetailDto.DictSiblingOneToOneFields;

            foreach (var sibLing in hierachyTransactionExDto.SibLineTransactionUnitIdExDtoList)
            {
                Dictionary<string, object> dictSilValue = new Dictionary<string, object>();

                dictSiblingValue.Add(sibLing.Id.ToString(), dictSilValue);
              
                SetupReadOnlyRootUnit(sibLing, dictSilValue);
            }

            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootTransactionUnit.Children)
            {
                AppTransactionStructureLoadBL.SetChildDefaultValuePlaceholder(DictOneToManyFields, aChildTransactionUnitExDto);
            }

            masterDetailDto.EditCloneDictOneToManyFields = masterDetailDto.DictOneToManyFields.DeepCopy();


            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootTransactionUnit.Children)
            {
                if (!aChildTransactionUnitExDto.IsVirtualUnit)
                {
                    masterDetailDto.DictOneToManyFields[aChildTransactionUnitExDto.Id.ToString()].Clear();
                }
            }

            // need to udpate
            return masterDetailDto;


         
        }

        // need to setup default value
        public static AppMasterDetailDto GetNewFormData(AppTransactionExDto hierachyTransactionExDto)
        {
            if (hierachyTransactionExDto.OtherOptions != null && hierachyTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {
                return AppMasterDetailApiFormDataLoadBL.GetNewFormData(hierachyTransactionExDto);
            }

            //AppTransactionExDto hierachyTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(TransactionId);
            // need to sort all talbe by transcaionflow !

            AppMasterDetailDto masterDetailDto = new AppMasterDetailDto();
            masterDetailDto.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
            masterDetailDto.TransactionId = (int)hierachyTransactionExDto.Id;
            masterDetailDto.IsShowSaveButton = hierachyTransactionExDto.IsShowSaveButton;


            AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(hierachyTransactionExDto);

            Dictionary<string, List<AppChildDataDto>> DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            masterDetailDto.DictOneToManyFields = DictOneToManyFields;

            // only one root
            AppTransactionUnitExDto rootTransactionUnit = hierachyTransactionExDto.AppTransactionUnitList.ElementAt(0);
            masterDetailDto.RootUnitId = rootTransactionUnit.Id;

            string rootTableName = rootTransactionUnit.DataBaseTableName;

            DatabaseTable rootDatabaseTable = AppCacheManagerBL.GetDatabaseTable(rootTableName, hierachyTransactionExDto.DataSourceFrom, rootTransactionUnit.SchemaOwner); ;
            // masterDetailDto.DictOneToOneFields = new Dictionary<string, object>();

            var dictOneToOneFields = new Dictionary<string, object>();

            masterDetailDto.DictOneToOneFields = dictOneToOneFields;

            //  masterDetailDto.RootTransactionUnit = rootTransactionUnit;

            SetupRootUnit(rootTransactionUnit, rootDatabaseTable, dictOneToOneFields);

            //  need to add sibling default

            Dictionary<string, Dictionary<string, object>> dictSiblingValue = masterDetailDto.DictSiblingOneToOneFields;

            foreach (var sibLing in hierachyTransactionExDto.SibLineTransactionUnitIdExDtoList)
            {
                Dictionary<string, object> dictSilValue = new Dictionary<string, object>();

                dictSiblingValue.Add(sibLing.Id.ToString(), dictSilValue);
                string sibTableName = sibLing.DataBaseTableName;

                DatabaseTable siblDatabaseTable = AppCacheManagerBL.GetDatabaseTable(sibTableName, sibLing.DataSourceFrom, sibLing.SchemaOwner); ;

                SetupRootUnit(sibLing, siblDatabaseTable, dictSilValue);
            }

            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootTransactionUnit.Children)
            {
                AppTransactionStructureLoadBL.SetChildDefaultValuePlaceholder(DictOneToManyFields, aChildTransactionUnitExDto);
            }

            masterDetailDto.EditCloneDictOneToManyFields = masterDetailDto.DictOneToManyFields.DeepCopy();


            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootTransactionUnit.Children)
            {
                if (!aChildTransactionUnitExDto.IsVirtualUnit) {
                    masterDetailDto.DictOneToManyFields[aChildTransactionUnitExDto.Id.ToString()].Clear();
                }
            }

            // need to udpate
            return masterDetailDto;
        }

        private static void SetupReadOnlyRootUnit(AppTransactionUnitExDto rootTransactionUnit,  Dictionary<string, object> dictOneToOneFields)
        {
            Dictionary<string, object> dictRootTransactionUnitSecurityDDLFieldValue = AppTransactionStructureLoadBL.GetTransactionUnitSecurityDDLFieldValue(rootTransactionUnit);
            var dbFiledNameList = rootTransactionUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName).Distinct().ToList();

            Dictionary<string, object> dictDbFieldDefaultValue = rootTransactionUnit.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue))
                .ToDictionary(o => o.DataBaseFieldName, o =>
                {
                    if (o.ControlType == (int)EmAppControlType.CheckBox)
                    {
                        return (object)ControlTypeValueConverter.ConvertValueToBoolean(o.DefaultValue);
                    }
                    else if (o.ControlType == (int)EmAppControlType.DDL && o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.Integer)
                    {
                        return (object)ControlTypeValueConverter.ConvertValueToInt(o.DefaultValue);
                    }
                    else if (o.ControlType == (int)EmAppControlType.Numeric)
                    {
                        if ((o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.Integer) || (o.Nbdecimal.HasValue && o.Nbdecimal.Value == 0))
                        {
                            return (object)ControlTypeValueConverter.ConvertValueToInt(o.DefaultValue);
                        }
                        else
                        {
                            return (object)ControlTypeValueConverter.ConvertValueToDecimal(o.DefaultValue);
                        }
                    }
                    else
                    {
                        return (object)o.DefaultValue;
                    }
                });


            rootTransactionUnit.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                .ForAll(o => dictDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));

           
        }
        private static void SetupRootUnit(AppTransactionUnitExDto rootTransactionUnit, DatabaseTable rootDatabaseTable, Dictionary<string, object> dictOneToOneFields)
        {
            Dictionary<string, object> dictRootTransactionUnitSecurityDDLFieldValue = AppTransactionStructureLoadBL.GetTransactionUnitSecurityDDLFieldValue(rootTransactionUnit);
            var dbFiledNameList = rootTransactionUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName).Distinct().ToList();

            Dictionary<string, object> dictDbFieldDefaultValue = rootTransactionUnit.AppTransactionFieldList.Where(o => !string.IsNullOrWhiteSpace(o.DefaultValue))
                .ToDictionary(o => o.DataBaseFieldName, o =>
                {
                    if (o.ControlType == (int)EmAppControlType.CheckBox)
                    {
                        return (object)ControlTypeValueConverter.ConvertValueToBoolean(o.DefaultValue);
                    }
                    else if (o.ControlType == (int)EmAppControlType.DDL && o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.Integer)
                    {
                        return (object)ControlTypeValueConverter.ConvertValueToInt(o.DefaultValue);
                    }
                    else if (o.ControlType == (int)EmAppControlType.Numeric)
                    {
                        if ((o.DataType.HasValue && o.DataType.Value == (int)EmAppDataType.Integer) || (o.Nbdecimal.HasValue && o.Nbdecimal.Value == 0))
                        {
                            return (object)ControlTypeValueConverter.ConvertValueToInt(o.DefaultValue);
                        }
                        else
                        {
                            return (object)ControlTypeValueConverter.ConvertValueToDecimal(o.DefaultValue);
                        }
                    }
                    else
                    {
                        return (object)o.DefaultValue;
                    }
                });



            rootTransactionUnit.AppTransactionFieldList.Where(o => o.ControlType == (int)EmAppControlType.Numeric && !dictDbFieldDefaultValue.ContainsKey(o.DataBaseFieldName) && !o.IsPrimaryKey && !o.IsLinkToParentPrimaryKey)
                .ForAll(o => dictDbFieldDefaultValue.Add(o.DataBaseFieldName, "0"));

            foreach (var column in rootDatabaseTable.Columns)
            {
                // need to get value from database column ?

                if (dictDbFieldDefaultValue.ContainsKey(column.Name))
                {
                    dictOneToOneFields.Add(column.Name, dictDbFieldDefaultValue[column.Name]);
                }
                else
                {
                    dictOneToOneFields.Add(column.Name, null);
                }

                if (dictRootTransactionUnitSecurityDDLFieldValue.ContainsKey(column.Name))
                {
                    dictOneToOneFields[column.Name] = dictRootTransactionUnitSecurityDDLFieldValue[column.Name];
                }
            }


            var notInDbFileds = dbFiledNameList.Except(dictOneToOneFields.Keys);

            foreach (string tempFiledname in notInDbFileds)
            {
                dictOneToOneFields[tempFiledname] = null;
            }

            if (rootTransactionUnit.DataBaseTableName == "AppBusinessPartner")
            {
                dictOneToOneFields["AppCompanyID"] = ServerContext.Instance.CurrentCompanyId;
            }

            AppTransDataSystemTokenBL.AssignAuntoGenerationCodeToUnitField(dictOneToOneFields, rootTransactionUnit, false);
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

            // to add new collection view
            AppMasterDetailDto newEmptyAppformDataDto = GetNewFormData(appTransactionExDto);
            masterDetailDto.EditCloneDictOneToManyFields = newEmptyAppformDataDto.EditCloneDictOneToManyFields;



            //string connectInfo = AppMetaDataBL.GetConnectInfo(appTransactionExDto.DataSourceFrom ); 

            //using (DataAccessAdapter adpater = new DataAccessAdapter(connectInfo))
            //{
            //	//adpater.ConnectionString = appTransactionExDto.ConnectionString;
            SetupRootUnitDictValue(rootPrimaryKeyValue, masterDetailDto, rootMasterUnit);



            foreach (var siblingUnitDto in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
            {
                SetupSiblingUnitDictValue(rootPrimaryKeyValue, masterDetailDto, siblingUnitDto);
            }
            SetupFormConditionLockingDictValue(appTransactionExDto, masterDetailDto);
            //}




            foreach (AppTransactionUnitExDto aChildTransactionUnitExDto in rootMasterUnit.Children)
            {
                List<AppChildDataDto> childAppformChildDataDto = null; ;

                if (!(aChildTransactionUnitExDto.IsUsedForLoadingAvailableSource.HasValue && aChildTransactionUnitExDto.IsUsedForLoadingAvailableSource.Value))
                {
                    childAppformChildDataDto = SetupOneChildAndGrandChildData(masterDetailDto.DictOneToOneFields, aChildTransactionUnitExDto);

                    if (childAppformChildDataDto.Count == 0 && aChildTransactionUnitExDto.IsVirtualUnit)
                    {
                        childAppformChildDataDto = newEmptyAppformDataDto.DictOneToManyFields[aChildTransactionUnitExDto.Id.ToString()].DeepCopy();
                    }
                }
                else
                {
                    childAppformChildDataDto = LoadChildAvialbeDataSource(masterDetailDto, rootMasterUnit, aChildTransactionUnitExDto);

                }

                SetupStandAloneEntityDepedentFiled(childAppformChildDataDto, aChildTransactionUnitExDto);


                masterDetailDto.DictOneToManyFields.Add(aChildTransactionUnitExDto.Id.ToString(), childAppformChildDataDto);
            }

            PrepareCalendarRepeatSettingValues(masterDetailDto, rootMasterUnit);

            return masterDetailDto;
        }


        internal static List<AppChildDataDto> LoadChildAvialbeDataSource(AppMasterDetailDto masterDetailDto,
            AppTransactionUnitExDto rootMasterUnit, AppTransactionUnitExDto aVaialbeChildTransactionUnitExDto)
        {
            List<AppChildDataDto> childAppformChildDataDto;
            Dictionary<int, object> dictRootUnitOneRowFiedIdValue = AppTransactionFormulaBL.ConvertUnitOneRowDbFileNameToFileId(rootMasterUnit, masterDetailDto.DictOneToOneFields);
            DataTable childdataTble = GetAvaialbeChildDataTable(
                dictRootUnitOneRowFiedIdValue,
                masterDetailDto.DictOneToOneFields,
                aVaialbeChildTransactionUnitExDto);

            childAppformChildDataDto = ConvertChildAndGradnChildTableToAppChildDataDtoList(aVaialbeChildTransactionUnitExDto, childdataTble, new Dictionary<AppTransactionUnitExDto, DataTable>());
            return childAppformChildDataDto;
        }

        private static Dictionary<AppTransactionUnitExDto, DataTable> GetGrandChildDataTable(AppTransactionUnitExDto aChildTransactionUnitExDto, List<Dictionary<string, object>> childPkValueIDList)
        {
            Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble = new Dictionary<AppTransactionUnitExDto, DataTable>();

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aChildTransactionUnitExDto.DataSourceFrom.Value);

            if (childPkValueIDList.Count > 0)
            {
                foreach (AppTransactionUnitExDto aGrandchildAppTransactionUnitExDto in aChildTransactionUnitExDto.Children)
                {
                    string grandChildtableName = aGrandchildAppTransactionUnitExDto.DataBaseTableName;
                    DatabaseTable grandChilddatabaseTable = AppCacheManagerBL.GetDatabaseTable(grandChildtableName, aGrandchildAppTransactionUnitExDto.DataSourceFrom, aGrandchildAppTransactionUnitExDto.SchemaOwner);
                    // AppMetaDataBL.DictDataBaseTable[grandChildtableName];
                    SqlWriter sqlWriter = new SqlWriter(grandChilddatabaseTable, databaseFixtureInstance.SqlServerType.Value);

                    string whereCaluse = string.Empty;

                    bool isUsedForLoadingAvailableSource = aGrandchildAppTransactionUnitExDto.IsUsedForLoadingAvailableSource.HasValue && aGrandchildAppTransactionUnitExDto.IsUsedForLoadingAvailableSource.Value;

                    string grandChildQuery = string.Empty;

                    List<DbParameter> paraList = new List<DbParameter>();

                    if (isUsedForLoadingAvailableSource)
                    {
                        var fieldCascadChildFied = aGrandchildAppTransactionUnitExDto.AppTransactionFieldList.Where(o => o.DdlparentLevelId.HasValue).FirstOrDefault();
                        if (fieldCascadChildFied == null)
                        {
                            // Get all standard alone source
                            grandChildQuery = sqlWriter.SelectAllSql();
                        }
                        else // need  to Cascagin init will load  SetupOneUnitCascadingDataSource

                        {
                            // var parentFiledDto = fieldCascadChildFied.DdlparentLevelId
                        }
                    }
                    else // normal trasction untit
                    {
                        paraList = SetupGrandChildWhereClasueWithChildPkValue(childPkValueIDList, aGrandchildAppTransactionUnitExDto, ref whereCaluse);

                        if (whereCaluse != string.Empty)
                        {


                            grandChildQuery = sqlWriter.SelectAllSql() + " WHERE " + whereCaluse;

                            //var grandChilddataTble = adpater.ExecuteDataTableRetrievalQuery(grandChildQuery, paraList);


                        }
                    }

                    if (grandChildQuery != string.Empty)
                    {
                        grandChildQuery = grandChildQuery + BuildChildUnitRowOrderByClause(aGrandchildAppTransactionUnitExDto, grandChilddatabaseTable);
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

        public static AppMasterDetailDto GetNewFormDataFromDataTransfer(int dataTransferSettingId, object srcTransactionRid)
        {

            var dataTransferSettingEntity = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingEntity(dataTransferSettingId);

            if (dataTransferSettingEntity != null)
            {
                AppMasterDetailDto srcFormData = GetMasterDetailFormData(dataTransferSettingEntity.TransactionId.Value, srcTransactionRid);

                if (srcFormData != null)
                {
                    AppMasterDetailDto newFormData = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromMasterDetailToMasterDetail(srcFormData, dataTransferSettingId);
                    return newFormData;
                }
            }

            return null;
        }

        public static AppMasterDetailDto GetNewFormDataFromDefaultDataTransfer(int srcTransactionId, object srcTransactionRid)
        {

            var defaultTransferSettingEntity = AppTransactionDataTransferSettingBL.RetrieveAllAppTransactionDataTransferSettingEntity(srcTransactionId, null)
                    .FirstOrDefault(o => o.DestinationTransactionId.HasValue && o.DestinationTransactionId.Value == srcTransactionId);

            if (defaultTransferSettingEntity != null)
            {
                AppMasterDetailDto srcFormData = GetMasterDetailFormData(srcTransactionId, srcTransactionRid);

                if (srcFormData != null)
                {
                    AppMasterDetailDto newFormData = AppTransactionDataTransferBL.PrepareDataTransferFormData_FromMasterDetailToMasterDetail(srcFormData, defaultTransferSettingEntity.DataTransferSettingId);
                    return newFormData;
                }
            }

            return null;
        }

        internal static List<DbParameter> SetupGrandChildWhereClasueWithChildPkValue(List<Dictionary<string, object>> childPkValueIDList, AppTransactionUnitExDto aGrandchildAppTransactionUnitExDto, ref string whereCaluse)
        {
            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aGrandchildAppTransactionUnitExDto.DataSourceFrom.Value);
            List<DbParameter> paraList = new List<DbParameter>();

            int rowCount = 0;
            foreach (Dictionary<string, object> childPk in childPkValueIDList)
            {
                string rowwhereCaluse = string.Empty;
                int colCount = 0;
                foreach (string linkParentPkDbfield in aGrandchildAppTransactionUnitExDto.DictLinkToParentKeyDbfield.Keys)
                {
                    string parentKeyFied = aGrandchildAppTransactionUnitExDto.DictLinkToParentKeyDbfield[linkParentPkDbfield];
                    if (childPk.ContainsKey(parentKeyFied))
                    {
                        //string parameName = string.Format("@para{0}{1}", rowCount, colCount);

                        //paraList.Add(new SqlParameter(parameName, childPk[parentKeyFied]));

                        //rowwhereCaluse = rowwhereCaluse + string.Format(" {0}={1}", linkParentPkDbfield, parameName) + " AND";

                        string parameName = string.Format("para{0}{1}", rowCount, colCount);
                        DbParameter parameter = databaseFixtureInstance.CreateParameter(parameName.Replace(" ", ""));
                        parameter.Value = childPk[parentKeyFied];
                        paraList.Add(parameter);

                        rowwhereCaluse = rowwhereCaluse + string.Format(" [{0}]={1}", linkParentPkDbfield, parameter.ParameterName) + " AND";
                    }

                    colCount++;
                }

                if (rowwhereCaluse != string.Empty)
                {
                    rowwhereCaluse = rowwhereCaluse.Substring(0, rowwhereCaluse.Length - 4);
                }

                whereCaluse = whereCaluse + "(" + rowwhereCaluse + ")" + " OR";
                rowCount++;
            }

            if (!string.IsNullOrWhiteSpace(whereCaluse))
            {
                whereCaluse = whereCaluse.Substring(0, whereCaluse.Length - 3);

            }

            return paraList;
        }

        internal static List<DbParameter> SetupOneSetKeyValueWhereClause(AppTransactionUnitExDto childTransactionUnitExDto,
            Dictionary<string, object> dictDeleteColumnValue, ref string whereCaluse)
        {
            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(childTransactionUnitExDto.DataSourceFrom.Value);

            List<DbParameter> paraList = new List<DbParameter>();
            foreach (string columnName in dictDeleteColumnValue.Keys)
            {
                //string parameName = string.Format("@{0}", childsinglePk);
                //paraList.Add(new SqlParameter(parameName, pkNameValueDict[childsinglePk]));
                //whereCaluse = whereCaluse + string.Format(" {0}={1}", childsinglePk, parameName) + " AND";

                string parameName = string.Format("{0}", columnName);
                DbParameter parameter = databaseFixtureInstance.CreateParameter(parameName.Replace(" ", ""));
                parameter.Value = dictDeleteColumnValue[columnName];
                paraList.Add(parameter);


                whereCaluse = whereCaluse + string.Format(" [{0}]={1}", columnName, parameter.ParameterName) + " AND";
            }

            if (!string.IsNullOrWhiteSpace(whereCaluse))
            {

                whereCaluse = whereCaluse.Substring(0, whereCaluse.Length - 4);
            }
            return paraList;
        }

        internal static List<DbParameter> SetupMutipleSetKeyValueWhereClause(List<Dictionary<string, object>> pkNameValueDictList, ref string whereCaluse, AppTransactionUnitExDto childAppTransactionUnitExDto)
        {
            List<DbParameter> paraList = new List<DbParameter>();

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(childAppTransactionUnitExDto.DataSourceFrom.Value);

            int rowCount = 0;
            foreach (Dictionary<string, object> dictPkValue in pkNameValueDictList)
            {
                string rowwhereCaluse = string.Empty;
                int colCount = 0;
                foreach (string childsinglePk in dictPkValue.Keys)
                {
                    //string parameName = string.Format("@para{0}{1}", rowCount, colCount);
                    //paraList.Add(new SqlParameter(parameName, dictPkValue[childsinglePk]));

                    //rowwhereCaluse = rowwhereCaluse + string.Format(" {0}={1}", childsinglePk, parameName) + " AND";

                    string parameName = string.Format("para{0}{1}", rowCount, colCount);
                    DbParameter parameter = databaseFixtureInstance.CreateParameter(parameName.Replace(" ", ""));
                    parameter.Value = dictPkValue[childsinglePk];
                    paraList.Add(parameter);


                    rowwhereCaluse = rowwhereCaluse + string.Format(" [{0}]={1}", childsinglePk, parameter.ParameterName) + " AND";


                    colCount++;
                }

                if (rowwhereCaluse != string.Empty)
                {
                    rowwhereCaluse = rowwhereCaluse.Substring(0, rowwhereCaluse.Length - 4);
                }

                whereCaluse = whereCaluse + "(" + rowwhereCaluse + ")" + " OR";
                rowCount++;
            }

            if (!string.IsNullOrWhiteSpace(whereCaluse))
            {
                whereCaluse = whereCaluse.Substring(0, whereCaluse.Length - 3);
                              

                whereCaluse = databaseFixtureInstance.ReformatQuery(whereCaluse);

            }

            return paraList;
        }

        // Test Code: Condition Locking
        internal static void SetupFormConditionLockingDictValue(AppTransactionExDto appTransactionExDto, AppMasterDetailDto rootAppformDataDto)
        {
            if (appTransactionExDto.Id == null)
            {
                return;
            }

            List<AppConditionalActionExDto> conditionActionList = AppTransactionConditionalActionBL.RetrieveAllAppConditionalActionListByTransactionId(appTransactionExDto.Id).ToList();

            rootAppformDataDto.DictOneToOneConditionHideFields = new Dictionary<string, object>();
            rootAppformDataDto.DictOneToOneLockFields = new Dictionary<string, object>();
            rootAppformDataDto.DictLockUnitIds = new Dictionary<string, object>();

            //TODO Control by Security 
            if (AppSecurityUserBL.IsAdminUser())
            {
                //return;
            }

            AppTransactionExDto transactionWithSecurity = AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable(appTransactionExDto.Id, rootAppformDataDto.RootPrimaryKeyValue);
            List<int> sepcialEditPermissionFieldIds = transactionWithSecurity.SepcialEditPermissionTransFieldIdList;
            Dictionary<int, object> dictRootLevelFieldIdAndValue = null;
            Dictionary<string, bool?> dictConditionFormulaAndIsTrue = new Dictionary<string, bool?>();


            foreach (var conditionActionDto in conditionActionList)
            {
                bool? isConditionTrue = false;

                bool isLockTransaction = conditionActionDto.IsLockingTransaction.HasValue && conditionActionDto.IsLockingTransaction.Value;
                int? conditionFieldId = conditionActionDto.BooleanConditionFieldId;
                string conditionFormula = conditionActionDto.BooleanConditionFormula;

                int? hideFieldId = conditionActionDto.NeedToHideTransactionFieldId;
                int? lockFieldId = conditionActionDto.LockingTransactionFieldId;
                int? lockChildUnitId = conditionActionDto.LockingTransactionUnitId;
                

                if (conditionFieldId.HasValue || !string.IsNullOrWhiteSpace(conditionFormula))
                {
                    if (conditionFieldId.HasValue)
                    {
                        if (appTransactionExDto.DictRootLevelUnitTransactionField != null
                            && appTransactionExDto.DictRootLevelUnitTransactionField.ContainsKey(conditionFieldId.Value))
                        {
                            var transactionFieldDto = appTransactionExDto.DictRootLevelUnitTransactionField[conditionFieldId.Value];

                            if (transactionFieldDto != null)
                            {
                                if (transactionFieldDto.IsSiblingField)
                                {
                                    if (rootAppformDataDto.DictSiblingOneToOneFields.ContainsKey(transactionFieldDto.TransactionUnitId.ToString()))
                                    {
                                        var dictSiblingField = rootAppformDataDto.DictSiblingOneToOneFields[transactionFieldDto.TransactionUnitId.ToString()];
                                        if (dictSiblingField != null && dictSiblingField.ContainsKey(transactionFieldDto.DataBaseFieldName))
                                        {
                                            isConditionTrue = ControlTypeValueConverter.ConvertValueToBoolean(dictSiblingField[transactionFieldDto.DataBaseFieldName]);
                                        }
                                    }
                                }
                                else
                                {
                                    if (rootAppformDataDto.DictOneToOneFields.ContainsKey(transactionFieldDto.DataBaseFieldName))
                                    {
                                        isConditionTrue = ControlTypeValueConverter.ConvertValueToBoolean(rootAppformDataDto.DictOneToOneFields[transactionFieldDto.DataBaseFieldName]);
                                    }
                                }
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(conditionFormula))
                    {
                        if (dictConditionFormulaAndIsTrue.ContainsKey(conditionFormula.Trim()))
                        {
                            isConditionTrue = dictConditionFormulaAndIsTrue[conditionFormula.Trim()];
                        }
                        else
                        {
                            if (dictRootLevelFieldIdAndValue == null)
                            {
                                dictRootLevelFieldIdAndValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(rootAppformDataDto, appTransactionExDto);
                            }

                            string rightSideEXpress = AppTransactionFormulaBL.RightSideAssignmentEXpressWithRealValue(appTransactionExDto.DictAllTransactionField, conditionFormula, dictRootLevelFieldIdAndValue);

                            if (rightSideEXpress.IndexOf(EmBLFiledMappingSystemTokenField.CurrentUserID.ToString(), StringComparison.InvariantCultureIgnoreCase) != -1)
                            {
                                string currentUserToken = string.Format("[{0}]", EmBLFiledMappingSystemTokenField.CurrentUserID.ToString());
                                rightSideEXpress = rightSideEXpress.Replace(currentUserToken, ServerContext.Instance.CurrentUid.ToString());
                            }

                            try
                            {

                                //object exResult = Evaluator.EvaluateToObject(rightSideEXpress);

                                object exResult = AppTransactionFormulaBL.ParseAndEvaluteExpress(rightSideEXpress);

                                isConditionTrue = ControlTypeValueConverter.ConvertValueToBoolean(exResult);


                                dictConditionFormulaAndIsTrue.Add(conditionFormula.Trim(), isConditionTrue);

                            }
                            catch (Exception ex)
                            {
                                string eexms = ex.ToString();
                            }
                        }                      

                    }                    

                    if (isLockTransaction)
                    {
                        if (isConditionTrue.HasValue && isConditionTrue.Value)
                        {
                            rootAppformDataDto.IsLockTransaction = true;
                        }
                    }

                    if (hideFieldId.HasValue)
                    {
                        if (!rootAppformDataDto.DictOneToOneConditionHideFields.ContainsKey(hideFieldId.Value.ToString()))
                        {
                            rootAppformDataDto.DictOneToOneConditionHideFields.Add(hideFieldId.Value.ToString(), false);
                        }

                        if (isConditionTrue.HasValue && isConditionTrue.Value)
                        {
                            if (sepcialEditPermissionFieldIds != null && sepcialEditPermissionFieldIds.Contains(hideFieldId.Value))
                            {
                                if (conditionActionDto.IsLockForSpecailEditPrivilege.HasValue && conditionActionDto.IsLockForSpecailEditPrivilege.Value)
                                {
                                    rootAppformDataDto.DictOneToOneConditionHideFields[hideFieldId.Value.ToString()] = true;
                                }
                            }
                            else
                            {
                                rootAppformDataDto.DictOneToOneConditionHideFields[hideFieldId.Value.ToString()] = true;
                            }
                        }
                    }
                    else if (lockFieldId.HasValue)
                    {
                        if (!rootAppformDataDto.DictOneToOneLockFields.ContainsKey(lockFieldId.Value.ToString()))
                        {
                            rootAppformDataDto.DictOneToOneLockFields.Add(lockFieldId.Value.ToString(), false);
                        }

                        if (isConditionTrue.HasValue && isConditionTrue.Value)
                        {
                            if (sepcialEditPermissionFieldIds != null && sepcialEditPermissionFieldIds.Contains(lockFieldId.Value))
                            {
                                if (conditionActionDto.IsLockForSpecailEditPrivilege.HasValue && conditionActionDto.IsLockForSpecailEditPrivilege.Value)
                                {
                                    rootAppformDataDto.DictOneToOneLockFields[lockFieldId.Value.ToString()] = true;
                                }
                            }
                            else
                            {
                                rootAppformDataDto.DictOneToOneLockFields[lockFieldId.Value.ToString()] = true;
                            }
                        }
                    }
                    else if (lockChildUnitId.HasValue)
                    {
                        if (!rootAppformDataDto.DictLockUnitIds.ContainsKey(lockChildUnitId.Value.ToString()))
                        {
                            rootAppformDataDto.DictLockUnitIds.Add(lockChildUnitId.Value.ToString(), false);
                        }

                        if (isConditionTrue.HasValue && isConditionTrue.Value)
                        {
                            rootAppformDataDto.DictLockUnitIds[lockChildUnitId.Value.ToString()] = true;
                        }

                        foreach (var transField in appTransactionExDto.DictAllTransactionField.Values)
                        {
                            if (transField.TransactionUnitId == lockChildUnitId.Value)
                            {
                                if (!rootAppformDataDto.DictOneToOneLockFields.ContainsKey(transField.Id.ToString()))
                                {
                                    rootAppformDataDto.DictOneToOneLockFields.Add(transField.Id.ToString(), false);
                                }

                                if (isConditionTrue.HasValue && isConditionTrue.Value)
                                {
                                    if (sepcialEditPermissionFieldIds != null && sepcialEditPermissionFieldIds.Contains((int)transField.Id))
                                    {
                                        if (conditionActionDto.IsLockForSpecailEditPrivilege.HasValue && conditionActionDto.IsLockForSpecailEditPrivilege.Value)
                                        {
                                            rootAppformDataDto.DictOneToOneLockFields[transField.Id.ToString()] = true;
                                        }
                                    }
                                    else
                                    {
                                        rootAppformDataDto.DictOneToOneLockFields[transField.Id.ToString()] = true;
                                    }
                                }
                            }
                        }


                    }
                }
            }
        }

        private static void SetupRootUnitDictValue(Dictionary<string, object> rootPrimaryKeyValue, AppMasterDetailDto rootAppformDataDto,
            AppTransactionUnitExDto rootMasterUnit)
        {
            if (!rootMasterUnit.IsVirtualUnit)
            {
                DataRow row = AppDbHelerBL.RetriveOneDataRowWtihPrimayKeyFromDataBaseTable(rootPrimaryKeyValue, rootMasterUnit);



                Dictionary<string, object> dictOneToOneFields = MappingDbFiledToTransField(rootMasterUnit, row);

                rootAppformDataDto.DictOneToOneFields = dictOneToOneFields;
            }
            else
            {
                rootAppformDataDto.DictOneToOneFields = new Dictionary<string, object>();
            }

        }

        internal static Dictionary<string, object> MappingDbFiledToTransField(AppTransactionUnitExDto aUnit, DataRow row)
        {
            var dbFiledNameList = aUnit.AppTransactionFieldList.Select(o => o.DataBaseFieldName).Distinct().ToList();



            var dictOneToOneFields = ConvertOneDataRowToDictRow(row);

            Dictionary<string, int> dictDbFieldNameAndDataType = aUnit.AppTransactionFieldList.Where(o => o.DataType.HasValue).ToDictionary(o => o.DataBaseFieldName, o => o.DataType.Value);
         

            // need to process special data type mapping
            foreach (string dbFiled in dictDbFieldNameAndDataType.Keys)
            {

                if (dictDbFieldNameAndDataType[dbFiled] == (int)EmAppDataType.Blob)
                {
                    if (dictOneToOneFields.ContainsKey(dbFiled))
                    {
                        dictOneToOneFields[dbFiled] = ControlTypeValueConverter.ConvertBlobToText(dictOneToOneFields[dbFiled] as byte[]);
                    }

                }

            }


            var notInDbFileds = dbFiledNameList.Except(dictOneToOneFields.Keys);

            foreach (string tempFiledname in notInDbFileds)
            {

                dictOneToOneFields[tempFiledname] = null;


            }

            return dictOneToOneFields;
        }

        private static void SetupSiblingUnitDictValue(Dictionary<string, object> rootPrimaryKeyValue, AppMasterDetailDto rootAppformDataDto,
            AppTransactionUnitExDto siblingUnitDto)
        {
            var siblingnewDict = new Dictionary<string, object>();

            if (!siblingUnitDto.IsVirtualUnit)
            {

                DataTable childdataTble = GetChildDataTable(rootPrimaryKeyValue, siblingUnitDto);

                Dictionary<string, int> dictDbFieldNameAndDataType = siblingUnitDto.AppTransactionFieldList.Where(o => o.DataType.HasValue).ToDictionary(o => o.DataBaseFieldName, o => o.DataType.Value);

                if (childdataTble.Rows.Count > 0)
                {
                    DataRow dataRow = childdataTble.Rows[0];
                    siblingnewDict = MappingDbFiledToTransField(siblingUnitDto, dataRow);
                    // siblingnewDict = AppMasterDetailDto.ConvertOneDataRowToDictRow(dataRow);


                }
            }


            rootAppformDataDto.DictSiblingOneToOneFields.Add(siblingUnitDto.Id.ToString(), siblingnewDict);

        }


        internal static List<AppChildDataDto> SetupOneChildAndGrandChildData(Dictionary<string, object> dictRootOneToOneValue, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            if (!aChildTransactionUnitExDto.IsVirtualUnit)
            {
                DataTable childdataTble;
                List<Dictionary<string, object>> childPkValueIDList = GetChildPkValueIDList(dictRootOneToOneValue, aChildTransactionUnitExDto, out childdataTble);

                Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble = GetGrandChildDataTable(aChildTransactionUnitExDto, childPkValueIDList);

                List<AppChildDataDto> childAppformChildDataDto = ConvertChildAndGradnChildTableToAppChildDataDtoList(aChildTransactionUnitExDto, childdataTble, dictGrandChilddataTble);

               

                return childAppformChildDataDto;
            }
            else
            {
                return new List<AppChildDataDto>();
            }
        }

        private static DataTable GetAvaialbeChildDataTable(
            Dictionary<int, object> dictOneRowFiedIdValue,
            Dictionary<string, object> dictParentOneToOneValue,
            AppTransactionUnitExDto avialbeDataSourceChildUnitExDto)
        {
            string childtableName = avialbeDataSourceChildUnitExDto.DataBaseTableName;
            DatabaseTable childdatabaseTable = AppCacheManagerBL.GetDatabaseTable(childtableName, avialbeDataSourceChildUnitExDto.DataSourceFrom, avialbeDataSourceChildUnitExDto.SchemaOwner);

            // Master primary key only has one key, for sibling unit alway take first one
            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(avialbeDataSourceChildUnitExDto.DataSourceFrom.Value);


            SqlWriter sqlWriter = new SqlWriter(childdatabaseTable, databaseFixtureInstance.SqlServerType.Value);

            string childQuery = sqlWriter.SelectAllSql();

            //
            //transactionfieldid_231< 100
            // diveisionI = transactionfieldid_231
            string linkToParentWhere = avialbeDataSourceChildUnitExDto.AvailableSourceFilterWhereClause;


            Dictionary<string, object> dictParatmerNameValue = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(linkToParentWhere))
            {
                foreach (int transactionFieldId in dictOneRowFiedIdValue.Keys)
                {
                    string tranActionFieldTokey = string.Format("{0}{1}", AppTransactionTemplateDataLoadSetupBL.TransactionFieldFormulaPrefix, transactionFieldId.ToString());
                    string matchSubItem = string.Format(@"\b{0}\b", tranActionFieldTokey);

                    if (Regex.IsMatch(linkToParentWhere, matchSubItem)) //; conditionWhereClause.Contains(matchSubItem))
                    {

                        //string sqlTextExpress = ControlTypeValueConverter.ConvertDataTableSelectFiledString(dictBlockSubitemValue[subItmeId], dictSubItemControType[subItmeId]);

                        string paraName = "@Paramter_" + transactionFieldId.ToString();
                        object value = dictOneRowFiedIdValue[transactionFieldId];
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {

                            dictParatmerNameValue[paraName] = value;

                            linkToParentWhere = linkToParentWhere.Replace(tranActionFieldTokey, string.Format("'{0}'", paraName));
                        }


                    }
                }

            }


            List<DbParameter> listParamters = new List<DbParameter>();
            foreach (var pair in dictParatmerNameValue)
            {
                DbParameter aDbParameter = new SqlParameter(pair.Key, pair.Value);
                listParamters.Add(aDbParameter);
            }



            if (avialbeDataSourceChildUnitExDto.AvailableSourceFilterByParentTransactionFieldId.HasValue
                && avialbeDataSourceChildUnitExDto.AvailableSourceMatchToParentUnitTransactionFieldId.HasValue)
            {
                int rootTRiggerFiedId = avialbeDataSourceChildUnitExDto.AvailableSourceFilterByParentTransactionFieldId.Value;

                int matchFiedId = avialbeDataSourceChildUnitExDto.AvailableSourceMatchToParentUnitTransactionFieldId.Value;
                var matchFieldExdto = avialbeDataSourceChildUnitExDto.AppTransactionFieldList.Where(o => (int)o.Id == matchFiedId).FirstOrDefault();
                if (matchFieldExdto != null && dictOneRowFiedIdValue[rootTRiggerFiedId] != null)
                {
                    string paraName = "@Paramter_" + matchFiedId.ToString();
                    object value = dictOneRowFiedIdValue[rootTRiggerFiedId];

                    DbParameter aDbParameter = new SqlParameter(paraName, value);
                    listParamters.Add(aDbParameter);

                    if (!string.IsNullOrWhiteSpace(linkToParentWhere))
                    {
                        linkToParentWhere = linkToParentWhere + " AND " + matchFieldExdto.DataBaseFieldName + "=" + paraName;


                    }
                    else
                    {
                        linkToParentWhere = "  " + matchFieldExdto.DataBaseFieldName + "=" + paraName;
                    }

                }




            }

            // Also honor Link To Parent Primary Key (DictLinkToParentKeyDbfield).
            // Available-source units previously ignored this and loaded all rows unless AvailableSourceFilter* was set.
            if (dictParentOneToOneValue != null
                && avialbeDataSourceChildUnitExDto.DictLinkToParentKeyDbfield != null
                && avialbeDataSourceChildUnitExDto.DictLinkToParentKeyDbfield.Count > 0)
            {
                string linkPkWhere = string.Empty;
                List<DbParameter> linkPkParams = GetChildWhereClauseParamters(
                    dictParentOneToOneValue,
                    avialbeDataSourceChildUnitExDto,
                    ref linkPkWhere);
                if (!string.IsNullOrWhiteSpace(linkPkWhere))
                {
                    if (!string.IsNullOrWhiteSpace(linkToParentWhere))
                    {
                        linkToParentWhere = linkToParentWhere + " AND (" + linkPkWhere + ")";
                    }
                    else
                    {
                        linkToParentWhere = linkPkWhere;
                    }
                    if (linkPkParams != null && linkPkParams.Count > 0)
                    {
                        listParamters.AddRange(linkPkParams);
                    }
                }
            }

            DataTable childdataTble = new DataTable();
            if (!string.IsNullOrWhiteSpace(linkToParentWhere))
            {
                childQuery = childQuery + " WHERE " + linkToParentWhere;

            }

            childQuery = childQuery + BuildChildUnitRowOrderByClause(avialbeDataSourceChildUnitExDto, childdatabaseTable);

            childdataTble = databaseFixtureInstance.RetriveDataTable(childQuery, listParamters); //adpater.ExecuteDataTableRetrievalQuery(childQuery, listParamters);


            return childdataTble;
        }

        internal static List<Dictionary<string, object>> GetChildPkValueIDList(Dictionary<string, object> dictRootOneToOneValue,
            AppTransactionUnitExDto aChildTransactionUnitExDto, out DataTable childdataTble)
        {
            var childPkValueIDList = new List<Dictionary<string, object>>();
            childdataTble = GetChildDataTable(dictRootOneToOneValue, aChildTransactionUnitExDto);

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

                //NEED TO CHECK GRAND CHILD
                // t
                //if (aChildTransactionUnitExDto.Children.Count > 0)
                //{

                //}
            }
            return childPkValueIDList;
        }

        private static string BuildChildUnitRowOrderByClause(AppTransactionUnitExDto unitExDto, DatabaseTable databaseTable)
        {
            if (unitExDto?.AppTransactionFieldList == null || databaseTable == null)
            {
                return string.Empty;
            }

            var orderFields = unitExDto.AppTransactionFieldList
                .Where(f => f.GroupByLevel.HasValue && !string.IsNullOrWhiteSpace(f.DataBaseFieldName))
                .OrderBy(f => f.GroupByLevel.Value)
                .ToList();

            if (orderFields.Count == 0)
            {
                return string.Empty;
            }

            var orderParts = new List<string>();
            foreach (var field in orderFields)
            {
                if (databaseTable.FindColumn(field.DataBaseFieldName) != null)
                {
                    orderParts.Add(string.Format("[{0}]", field.DataBaseFieldName));
                }
            }

            if (orderParts.Count == 0)
            {
                return string.Empty;
            }

            return " ORDER BY " + string.Join(", ", orderParts);
        }

        private static DataTable GetChildDataTable(Dictionary<string, object> dictRootOneToOneValue, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            string childtableName = aChildTransactionUnitExDto.DataBaseTableName;

            if (!string.IsNullOrWhiteSpace(childtableName))
            {
                DatabaseTable childdatabaseTable = AppCacheManagerBL.GetDatabaseTable(childtableName, aChildTransactionUnitExDto.DataSourceFrom, aChildTransactionUnitExDto.SchemaOwner);

                // Master primary key only has one key, for sibling unit alway take first one
                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aChildTransactionUnitExDto.DataSourceFrom.Value);


                SqlWriter sqlWriter = new SqlWriter(childdatabaseTable, databaseFixtureInstance.SqlServerType.Value);

                string childQuery = sqlWriter.SelectAllSql();


                string linkToParentWhere = "";

                List<DbParameter> listParamters = GetChildWhereClauseParamters(dictRootOneToOneValue, aChildTransactionUnitExDto, ref linkToParentWhere);

                DataTable childdataTble = new DataTable();
                if (!string.IsNullOrWhiteSpace(linkToParentWhere))
                {
                    childQuery = childQuery + "WHERE " + linkToParentWhere;
                    childQuery = childQuery + BuildChildUnitRowOrderByClause(aChildTransactionUnitExDto, childdatabaseTable);
                    childdataTble = databaseFixtureInstance.RetriveDataTable(childQuery, listParamters); //adpater.ExecuteDataTableRetrievalQuery(childQuery, listParamters);

                }

                return childdataTble;
            }

            return null;
        }

        internal static List<DbParameter> GetChildWhereClauseParamters(Dictionary<string, object> dictParentOneToOneValue, AppTransactionUnitExDto childTransactionUnitExDto, ref string whereCaluse)
        {

            Dictionary<string, AppTransactionFieldExDto> dictFileNameColumnDto = childTransactionUnitExDto.AppTransactionFieldList.ToDictionary(o => o.DataBaseFieldName, o => o);

            Dictionary<string, object> dictLinkToParentKeyValue = new Dictionary<string, object>();
            foreach (string linkPkfied in childTransactionUnitExDto.DictLinkToParentKeyDbfield.Keys)
            {
                AppTransactionFieldExDto aAppTransactionFieldExDto = dictFileNameColumnDto[linkPkfied];
                if (aAppTransactionFieldExDto.MappingEmSystemTokenField.HasValue
                    && aAppTransactionFieldExDto.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.TransactionID)
                {
                    dictLinkToParentKeyValue[linkPkfied] = childTransactionUnitExDto.TransactionId;
                }
                else
                {
                    string parentLinkKeyDbfiled = childTransactionUnitExDto.DictLinkToParentKeyDbfield[linkPkfied];
                    dictLinkToParentKeyValue[linkPkfied] = dictParentOneToOneValue[parentLinkKeyDbfiled];
                }


            }

            whereCaluse = "";
            List<DbParameter> listParamters = SetupOneSetKeyValueWhereClause(childTransactionUnitExDto, dictLinkToParentKeyValue, ref whereCaluse);

            return listParamters;


        }


        internal static List<AppChildDataDto> ConvertChildAndGradnChildTableToAppChildDataDtoList(AppTransactionUnitExDto aChildTransactionUnitExDto,
            DataTable childdataTble, Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble)
        {
            Dictionary<string, int> dictChildFieldNameAndDataType = aChildTransactionUnitExDto.AppTransactionFieldList.Where(o => o.DataType.HasValue).ToDictionary(o => o.DataBaseFieldName, o => o.DataType.Value);

            List<Dictionary<string, object>> childDataList = null;

            childDataList = ConvertOneDataTableToDataRowDict(childdataTble, aChildTransactionUnitExDto);

            List<AppChildDataDto> childAppformChildDataDto = new List<AppChildDataDto>();

            // Populate Chilld and Grandchild Dto
            foreach (Dictionary<string, object> childRow in childDataList)
            {
                AppChildDataDto aAppformChildDataDto = new AppChildDataDto();
                aAppformChildDataDto.ChildUnitId = (int)aChildTransactionUnitExDto.Id;

                childAppformChildDataDto.Add(aAppformChildDataDto);
                aAppformChildDataDto.DictOneToOneFields = childRow;


                if (!dictGrandChilddataTble.IsEmpty())
                {
                    aAppformChildDataDto.DictOneToManyFields = PopulateGrangChildUnit(dictGrandChilddataTble, childRow);
                }
                else
                {
                    aAppformChildDataDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
                }

            }
            return childAppformChildDataDto;
        }
        internal static Dictionary<string, List<AppChildDataDto>> PopulateGrangChildUnit(Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble, Dictionary<string, object> childRow)
        {

            if (dictGrandChilddataTble.IsEmpty())
                return null;


            Dictionary<string, List<AppChildDataDto>> GrandDictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();




            foreach (var grandChildUnit in dictGrandChilddataTble.Keys)
            {

                List<AppChildDataDto> listGridnChildRow = new List<AppChildDataDto>();
                var grandChilddataTble = dictGrandChilddataTble[grandChildUnit];
                Dictionary<string, int> dictGrandFieldNameAndControlType = grandChildUnit.AppTransactionFieldList.ToDictionary(o => o.DataBaseFieldName, o => o.ControlType);
                Dictionary<string, int> dictGrandFieldNameAndDataType = grandChildUnit.AppTransactionFieldList.Where(o => o.DataType.HasValue).ToDictionary(o => o.DataBaseFieldName, o => o.DataType.Value);


                // stadnalone data source without parent unit filed !
                if (grandChildUnit.IsUsedForLoadingAvailableSource.HasValue && grandChildUnit.IsUsedForLoadingAvailableSource.Value)
                {
                    var pareanrCascadigField = grandChildUnit.AppTransactionFieldList.Where(o => o.DdlparentLevelId.HasValue).FirstOrDefault();

                    if (pareanrCascadigField == null)
                    {
                        foreach (DataRow dataRow in grandChilddataTble.Rows)
                        {
                            var dictOneToOne = MappingDbFiledToTransField(grandChildUnit, dataRow);
                            AppChildDataDto aAppChildDataDto = new AppChildDataDto();
                            aAppChildDataDto.DictOneToOneFields = dictOneToOne;
                            listGridnChildRow.Add(aAppChildDataDto);
                        }


                    }

                }
                else
                {
                    string rowWhereCaluse = string.Empty;

                    foreach (string grandChildPkDbfield in grandChildUnit.DictLinkToParentKeyDbfield.Keys)
                    {
                        string childKeyFied = grandChildUnit.DictLinkToParentKeyDbfield[grandChildPkDbfield];
                        object cellValue = childRow[childKeyFied];


                        string dataTableSelectCellExress = ControlTypeValueConverter.ConvertDataTableSelectFiledString(cellValue, dictGrandFieldNameAndControlType[grandChildPkDbfield]);
                        rowWhereCaluse = rowWhereCaluse + string.Format(" [{0}]={1}", grandChildPkDbfield, dataTableSelectCellExress) + " AND";
                    }

                    if (rowWhereCaluse != string.Empty)
                    {
                        rowWhereCaluse = rowWhereCaluse.Substring(0, rowWhereCaluse.Length - 4);

                        foreach (DataRow dataRow in grandChilddataTble.Select(rowWhereCaluse))
                        {
                            var dictOneToOne = MappingDbFiledToTransField(grandChildUnit, dataRow);
                            AppChildDataDto aAppChildDataDto = new AppChildDataDto();
                            aAppChildDataDto.DictOneToOneFields = dictOneToOne;
                            listGridnChildRow.Add(aAppChildDataDto);
                        }
                    }
                }






                GrandDictOneToManyFields.Add(grandChildUnit.Id.ToString(), listGridnChildRow);
            }

            return GrandDictOneToManyFields;
        }

        //private static Dictionary<string, List<Dictionary<string, object>>> PopulateGrangChildUnit(Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble, Dictionary<string, object> childRow)
        //{
        //    Dictionary<string, List<Dictionary<string, object>>> GrandDictOneToManyFields = new Dictionary<string, List<Dictionary<string, object>>>();


        //    List<Dictionary<string, object>> listGridnChildRow = new List<Dictionary<string, object>>();

        //    foreach (var grandChildUnit in dictGrandChilddataTble.Keys)
        //    {
        //        var grandChilddataTble = dictGrandChilddataTble[grandChildUnit];

        //        Dictionary<string, int> dictGrandFieldNameAndControlType = grandChildUnit.AppTransactionFieldList.ToDictionary(o => o.DataBaseFieldName, o => o.ControlType);
        //        Dictionary<string, int> dictGrandFieldNameAndDataType = grandChildUnit.AppTransactionFieldList.Where(o => o.DataType.HasValue).ToDictionary(o => o.DataBaseFieldName, o => o.DataType.Value);

        //        string rowWhereCaluse = string.Empty;

        //        foreach (string grandChildPkDbfield in grandChildUnit.DictLinkToParentKeyDbfield.Keys)
        //        {
        //            string childKeyFied = grandChildUnit.DictLinkToParentKeyDbfield[grandChildPkDbfield];
        //            object cellValue = childRow[childKeyFied];


        //            string dataTableSelectCellExress = ControlTypeValueConverter.ConvertDataTableSelectFiledString(cellValue, dictGrandFieldNameAndControlType[grandChildPkDbfield]);
        //            rowWhereCaluse = rowWhereCaluse + string.Format(" {0}={1}", grandChildPkDbfield, dataTableSelectCellExress) + " AND";
        //        }

        //        if (rowWhereCaluse != string.Empty)
        //        {
        //            rowWhereCaluse = rowWhereCaluse.Substring(0, rowWhereCaluse.Length - 4);

        //            foreach (DataRow dataRow in grandChilddataTble.Select(rowWhereCaluse))
        //            {
        //                var dictOneToOne = MappingDbFiledToTransField(grandChildUnit, dataRow);
        //                listGridnChildRow.Add(dictOneToOne);
        //            }
        //        }

        //        GrandDictOneToManyFields.Add(grandChildUnit.Id.ToString(), listGridnChildRow);
        //    }

        //    return GrandDictOneToManyFields;
        //}

        // need focus on validation logical flow !!!

        internal static List<Dictionary<string, object>> ConvertOneDataTableToDataRowDict(DataTable dt, AppTransactionUnitExDto aChildTransactionUnitExDto)
        {
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (DataRow dr in dt.Rows)
            {
                var dictOneToone = MappingDbFiledToTransField(aChildTransactionUnitExDto, dr);
                rows.Add(dictOneToone);
            }

            return rows;

        }


        private static Dictionary<string, object> ConvertOneDataRowToDictRow(DataRow dr)
        {
            Dictionary<string, object> row = new Dictionary<string, object>();

            if(dr != null)
            {
                DataTable dt = dr.Table;


                foreach (DataColumn col in dt.Columns)
                {
                    object value = dr[col];
                    if (value == DBNull.Value)
                    {
                        value = null;
                    }

                    row.Add(col.ColumnName, value);
                }
            }
          


            return row;
        }

        public static AppListDataDto GetListEditFormData(int p)
        {
            throw new NotImplementedException();
        }

        private static void ProcessMasterDetailFormFileIDCodeDictionary(AppTransactionExDto appTransactionExDto, AppMasterDetailDto aAppformDataDto)
        {
            aAppformDataDto.DictDocumentIdFileCode = new Dictionary<int, string>();

            List<int> fileIdList = new List<int>();

            Dictionary<string, List<string>> DictUnitIdAndFileTransFieldDbNames =
                appTransactionExDto.DictAllTransactionField.Values.Where(o => o.ControlType == (int)EmAppControlType.File || o.ControlType == (int)EmAppControlType.Image)
                .GroupBy(o => o.TransactionUnitId).ToDictionary(g => g.Key.ToString(), g => g.Select(o => o.DataBaseFieldName).ToList());

            if (DictUnitIdAndFileTransFieldDbNames.Count > 0)
            {
                if (aAppformDataDto.RootUnitId != null && DictUnitIdAndFileTransFieldDbNames.ContainsKey(aAppformDataDto.RootUnitId.ToString()))
                {
                    var fileTypeTransFieldNames = DictUnitIdAndFileTransFieldDbNames[aAppformDataDto.RootUnitId.ToString()];

                    foreach (string rootfieldName in aAppformDataDto.DictOneToOneFields.Keys)
                    {
                        if (fileTypeTransFieldNames.Contains(rootfieldName))
                        {
                            int? fileId = ControlTypeValueConverter.ConvertValueToInt(aAppformDataDto.DictOneToOneFields[rootfieldName]);
                            if (fileId.HasValue && !fileIdList.Contains(fileId.Value))
                            {
                                fileIdList.Add(fileId.Value);
                            }
                        }
                    }
                }

                if (aAppformDataDto.DictSiblingOneToOneFields != null)
                {
                    foreach (string sibUnitKey in aAppformDataDto.DictSiblingOneToOneFields.Keys)
                    {
                        if (DictUnitIdAndFileTransFieldDbNames.ContainsKey(sibUnitKey))
                        {
                            var fileTypeRootTransFieldNames = DictUnitIdAndFileTransFieldDbNames[sibUnitKey];

                            foreach (string siblingFieldName in aAppformDataDto.DictSiblingOneToOneFields[sibUnitKey].Keys)
                            {
                                if (fileTypeRootTransFieldNames.Contains(siblingFieldName))
                                {
                                    int? fileId = ControlTypeValueConverter.ConvertValueToInt(aAppformDataDto.DictSiblingOneToOneFields[sibUnitKey][siblingFieldName]);
                                    if (fileId.HasValue && !fileIdList.Contains(fileId.Value))
                                    {
                                        fileIdList.Add(fileId.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (aAppformDataDto.DictOneToManyFields != null)
                {
                    foreach (string childUnitId in aAppformDataDto.DictOneToManyFields.Keys)
                    {
                        if (DictUnitIdAndFileTransFieldDbNames.ContainsKey(childUnitId))
                        {
                            var fileTypeChildTransFieldNames = DictUnitIdAndFileTransFieldDbNames[childUnitId];

                            foreach (var childRow in aAppformDataDto.DictOneToManyFields[childUnitId])
                            {
                                if (DictUnitIdAndFileTransFieldDbNames.ContainsKey(childUnitId))
                                {
                                    foreach (string childFieldName in childRow.DictOneToOneFields.Keys)
                                    {
                                        if (fileTypeChildTransFieldNames.Contains(childFieldName))
                                        {
                                            int? fileId = ControlTypeValueConverter.ConvertValueToInt(childRow.DictOneToOneFields[childFieldName]);
                                            if (fileId.HasValue && !fileIdList.Contains(fileId.Value))
                                            {
                                                fileIdList.Add(fileId.Value);
                                            }
                                        }
                                    }
                                }

                                if (childRow.DictOneToManyFields != null)
                                {
                                    foreach (string grandChildUnitId in childRow.DictOneToManyFields.Keys)
                                    {

                                        if (DictUnitIdAndFileTransFieldDbNames.ContainsKey(grandChildUnitId))
                                        {
                                            var fileTypeGrandChildTransFieldNames = DictUnitIdAndFileTransFieldDbNames[grandChildUnitId];

                                            foreach (AppChildDataDto grandChildRow in childRow.DictOneToManyFields[grandChildUnitId])
                                            {
                                                Dictionary<string, object> dictGrandChildKeyValue = grandChildRow.DictOneToOneFields;

                                                foreach (string grandChildFieldName in dictGrandChildKeyValue.Keys)
                                                {
                                                    if (fileTypeGrandChildTransFieldNames.Contains(grandChildFieldName))
                                                    {
                                                        int? fileId = ControlTypeValueConverter.ConvertValueToInt(dictGrandChildKeyValue[grandChildFieldName]);
                                                        if (fileId.HasValue && !fileIdList.Contains(fileId.Value))
                                                        {
                                                            fileIdList.Add(fileId.Value);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (fileIdList.Count > 0)
            {
                aAppformDataDto.DictDocumentIdFileCode = AppFileBL.RetrieveMultipleFileEntityByIds(fileIdList).ToDictionary(o => o.FileId, o => o.FileCode);
            }
        }


        private static void ProcessMasterDetailFormFolderIdAndPathDictionary(AppTransactionExDto appTransactionExDto, AppMasterDetailDto aAppformDataDto)
        {
            if (!appTransactionExDto.MgtRootFolderId.HasValue)
            {
                return;
            }
            if (aAppformDataDto.DictDocumentIdFileCode == null)
            {
                aAppformDataDto.DictDocumentIdFileCode = new Dictionary<int, string>();
            }

            List<int> folderIdList = new List<int>();

            Dictionary<string, List<string>> DictUnitIdAndFolderIdTransFieldDbNames =
                appTransactionExDto.DictAllTransactionField.Values.Where(o => o.ControlType == (int)EmAppControlType.FolderPathDisplay)
                .GroupBy(o => o.TransactionUnitId).ToDictionary(g => g.Key.ToString(), g => g.Select(o => o.DataBaseFieldName).ToList());

            if (DictUnitIdAndFolderIdTransFieldDbNames.Count > 0)
            {
                if (aAppformDataDto.RootUnitId != null && DictUnitIdAndFolderIdTransFieldDbNames.ContainsKey(aAppformDataDto.RootUnitId.ToString()))
                {
                    var fileTypeTransFieldNames = DictUnitIdAndFolderIdTransFieldDbNames[aAppformDataDto.RootUnitId.ToString()];

                    foreach (string rootfieldName in aAppformDataDto.DictOneToOneFields.Keys)
                    {
                        if (fileTypeTransFieldNames.Contains(rootfieldName))
                        {
                            int? fileId = ControlTypeValueConverter.ConvertValueToInt(aAppformDataDto.DictOneToOneFields[rootfieldName]);
                            if (fileId.HasValue && !folderIdList.Contains(fileId.Value))
                            {
                                folderIdList.Add(fileId.Value);
                            }
                        }
                    }
                }

                if (aAppformDataDto.DictSiblingOneToOneFields != null)
                {
                    foreach (string sibUnitKey in aAppformDataDto.DictSiblingOneToOneFields.Keys)
                    {
                        if (DictUnitIdAndFolderIdTransFieldDbNames.ContainsKey(sibUnitKey))
                        {
                            var fileTypeRootTransFieldNames = DictUnitIdAndFolderIdTransFieldDbNames[sibUnitKey];

                            foreach (string siblingFieldName in aAppformDataDto.DictSiblingOneToOneFields[sibUnitKey].Keys)
                            {
                                if (fileTypeRootTransFieldNames.Contains(siblingFieldName))
                                {
                                    int? fileId = ControlTypeValueConverter.ConvertValueToInt(aAppformDataDto.DictSiblingOneToOneFields[sibUnitKey][siblingFieldName]);
                                    if (fileId.HasValue && !folderIdList.Contains(fileId.Value))
                                    {
                                        folderIdList.Add(fileId.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                if (aAppformDataDto.DictOneToManyFields != null)
                {
                    foreach (string childUnitId in aAppformDataDto.DictOneToManyFields.Keys)
                    {
                        if (DictUnitIdAndFolderIdTransFieldDbNames.ContainsKey(childUnitId))
                        {
                            var fileTypeChildTransFieldNames = DictUnitIdAndFolderIdTransFieldDbNames[childUnitId];

                            foreach (var childRow in aAppformDataDto.DictOneToManyFields[childUnitId])
                            {
                                if (DictUnitIdAndFolderIdTransFieldDbNames.ContainsKey(childUnitId))
                                {
                                    foreach (string childFieldName in childRow.DictOneToOneFields.Keys)
                                    {
                                        if (fileTypeChildTransFieldNames.Contains(childFieldName))
                                        {
                                            int? fileId = ControlTypeValueConverter.ConvertValueToInt(childRow.DictOneToOneFields[childFieldName]);
                                            if (fileId.HasValue && !folderIdList.Contains(fileId.Value))
                                            {
                                                folderIdList.Add(fileId.Value);
                                            }
                                        }
                                    }
                                }

                                if (childRow.DictOneToManyFields != null)
                                {
                                    foreach (string grandChildUnitId in childRow.DictOneToManyFields.Keys)
                                    {

                                        if (DictUnitIdAndFolderIdTransFieldDbNames.ContainsKey(grandChildUnitId))
                                        {
                                            var fileTypeGrandChildTransFieldNames = DictUnitIdAndFolderIdTransFieldDbNames[grandChildUnitId];

                                            foreach (var grandChildRow in childRow.DictOneToManyFields[grandChildUnitId])
                                            {
                                                var dictGrandChildKeyValue = grandChildRow.DictOneToOneFields;

                                                foreach (string grandChildFieldName in dictGrandChildKeyValue.Keys)
                                                {
                                                    if (fileTypeGrandChildTransFieldNames.Contains(grandChildFieldName))
                                                    {
                                                        int? fileId = ControlTypeValueConverter.ConvertValueToInt(dictGrandChildKeyValue[grandChildFieldName]);
                                                        if (fileId.HasValue && !folderIdList.Contains(fileId.Value))
                                                        {
                                                            folderIdList.Add(fileId.Value);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (folderIdList.Count > 0)
            {
                AppSefolderDto[] folderHairarchy = AppSeFolderBL.RetrieveCurrentUserTranscationFolderHairarchyDto((int)appTransactionExDto.Id);

                List<AppSefolderDto> folderDtoList = AppSeFolderBL.ConvertFolderHairarchyToFlatList(folderHairarchy);

                aAppformDataDto.DictFolderIdAndPath = folderDtoList.Where(o => folderIdList.Contains((int)o.Id)).ToDictionary(o => (int)o.Id, o => o.FolderPath);
            }
        }



        public static readonly string apiDataTransfer_LevelToken = "->";
        public static readonly string apiDataTransfer_ArrayToken = "[0]";

        public static AppMasterDetailDto GetNewFormDataByApiToFormDataTransfer(int dataTransferId, JObject srcJsonObj)
        {

            AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferId);

            if (dataTransferDto != null && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.DataTransferMappingList != null)
            {
                AppMasterDetailDto tgtFormBlankData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                List<AppTransactionSaveAsMappingExDto> mappingList = ProcessApiToFormDataTransfer_PrepareMappingList(dataTransferDto, tgtTransactionExDto);

                Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitIdAndMappings = mappingList.GroupBy(o => o.TargetUnitId.Value).ToDictionary(o => o.Key, o => o.ToList());



                AppMasterDetailDto tgtFormData = AppMasterDetailFormDataLoadBL.GetNewFormData(dataTransferDto.DestinationTransactionId.Value);
                tgtFormData.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                tgtFormData.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
                tgtFormData.EditCloneDictOneToManyFields = tgtFormBlankData.DictOneToManyFields;


                // 1 Process Root Unit      
                var tgtRootMasterUnit = tgtTransactionExDto.RootMasterUnit;
                var rootUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)tgtRootMasterUnit.Id).ToList();

                if (rootUnitMappings.Count > 0)
                {
                    ProcessTransferOneJsonRowToForm(1, rootUnitMappings, srcJsonObj, tgtRootMasterUnit, tgtFormData.DictOneToOneFields);
                }



                // 2 Process Sibling Unit      
                foreach (var siblingUnitDto in tgtTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    var siblingUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)siblingUnitDto.Id).ToList();

                    if (siblingUnitMappings.Count > 0)
                    {
                        ProcessTransferOneJsonRowToForm(1, siblingUnitMappings, srcJsonObj, siblingUnitDto, tgtFormData.DictSiblingOneToOneFields[siblingUnitDto.Id.ToString()]);
                    }
                }


                // 3 Process Child Unit      
                foreach (var childUnitDto in tgtTransactionExDto.RootMasterUnit.Children)
                {
                    tgtFormData.DictOneToManyFields[childUnitDto.Id.ToString()].Clear();

                    var childUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)childUnitDto.Id && !string.IsNullOrWhiteSpace(o.ChildArrayPathName) && o.TargetUnitId.HasValue).ToList();

                    if (childUnitMappings.Count > 0)
                    {
                        string childArrayPathName = childUnitMappings.First().ChildArrayPathName;
                        string targetChildUnitId = childUnitMappings.First().TargetUnitId.Value.ToString();

                        foreach (var childArrayToken in srcJsonObj.SelectToken(childArrayPathName))
                        {
                            AppChildDataDto editCloneChildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetChildUnitId].FirstOrDefault();
                            if (editCloneChildDataDto != null)
                            {
                                AppChildDataDto aNewChildDataDto = editCloneChildDataDto.DeepCopy();
                                ProcessTransferOneJsonRowToForm(2, childUnitMappings, childArrayToken, childUnitDto, aNewChildDataDto.DictOneToOneFields);
                                tgtFormData.DictOneToManyFields[targetChildUnitId].Add(aNewChildDataDto);

                                // 4 Process Grandchild Unit      
                                foreach (var grandchildUnitDto in childUnitDto.Children)
                                {
                                    aNewChildDataDto.DictOneToManyFields[grandchildUnitDto.Id.ToString()].Clear();

                                    var grandchildUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)grandchildUnitDto.Id && !string.IsNullOrWhiteSpace(o.ChildArrayPathName) && !string.IsNullOrWhiteSpace(o.GrandChildArrayPathName) && o.TargetUnitId.HasValue).ToList();

                                    if (grandchildUnitMappings.Count > 0)
                                    {
                                        string grandchildArrayPathName = grandchildUnitMappings.First().GrandChildArrayPathName;
                                        string targetGrandchildUnitId = grandchildUnitMappings.First().TargetUnitId.Value.ToString();


                                        foreach (var grandchildArrayToken in childArrayToken.SelectToken(grandchildArrayPathName))
                                        {
                                            //AppChildDataDto editCloneGrandChildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetGrandchildUnitId].FirstOrDefault();
                                            AppChildDataDto editCloneGrandChildDataDto = editCloneChildDataDto.DictOneToManyFields[targetGrandchildUnitId].FirstOrDefault();
                                            if (editCloneGrandChildDataDto != null)
                                            {
                                                AppChildDataDto aNewGrandchildDataDto = editCloneGrandChildDataDto.DeepCopy();
                                                ProcessTransferOneJsonRowToForm(3, grandchildUnitMappings, grandchildArrayToken, grandchildUnitDto, aNewGrandchildDataDto.DictOneToOneFields);
                                                aNewChildDataDto.DictOneToManyFields[targetGrandchildUnitId].Add(aNewGrandchildDataDto);
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }
                }

                //AppCascadingBL.SetupIntialCscadingFieldDataSource(tgtTransactionExDto, tgtFormData, false);
                AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(tgtTransactionExDto, tgtFormData, false);

                tgtFormData.IsNew = true;
                tgtFormData.ValidationLimittedTransFieldIdList = new List<int>();
                var calculationResult = AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(tgtFormData);

                if (calculationResult.IsSuccessfulWithResult)
                {
                    return calculationResult.Object;
                }
            }

            return null;
        }



        public static void ModifyFormDataByApiToFormDataTransfer(int dataTransferId, JObject srcJsonObj, AppMasterDetailDto tgtFormData, List<string> needToUpdateMappingKeys = null)
        {

            AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferId);

            if (dataTransferDto != null && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.DataTransferMappingList != null && tgtFormData != null)
            {

                AppTransactionExDto tgtTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                List<AppTransactionSaveAsMappingExDto> mappingList = ProcessApiToFormDataTransfer_PrepareMappingList(dataTransferDto, tgtTransactionExDto);

                if (needToUpdateMappingKeys != null)
                {
                    mappingList = mappingList.Where(o => needToUpdateMappingKeys.Contains(o.Name)).ToList();
                }

                Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitIdAndMappings = mappingList.GroupBy(o => o.TargetUnitId.Value).ToDictionary(o => o.Key, o => o.ToList());


                // 1 Process Root Unit      
                var tgtRootMasterUnit = tgtTransactionExDto.RootMasterUnit;
                var rootUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)tgtRootMasterUnit.Id).ToList();

                if (rootUnitMappings.Count > 0)
                {
                    ProcessTransferOneJsonRowToForm(1, rootUnitMappings, srcJsonObj, tgtRootMasterUnit, tgtFormData.DictOneToOneFields);
                }


                // 2 Process Sibling Unit      
                foreach (var siblingUnitDto in tgtTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    var siblingUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)siblingUnitDto.Id).ToList();

                    if (siblingUnitMappings.Count > 0)
                    {
                        ProcessTransferOneJsonRowToForm(1, siblingUnitMappings, srcJsonObj, siblingUnitDto, tgtFormData.DictSiblingOneToOneFields[siblingUnitDto.Id.ToString()]);
                    }
                }

                // To do, need to process child and grand child grid rows

                //// 3 Process Child Unit      
                //foreach (var childUnitDto in tgtTransactionExDto.RootMasterUnit.Children)
                //{
                //    tgtFormData.DictOneToManyFields[childUnitDto.Id.ToString()].Clear();

                //    var childUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)childUnitDto.Id && !string.IsNullOrWhiteSpace(o.ChildArrayPathName) && o.TargetUnitId.HasValue).ToList();

                //    if (childUnitMappings.Count > 0)
                //    {
                //        string childArrayPathName = childUnitMappings.First().ChildArrayPathName;
                //        string targetChildUnitId = childUnitMappings.First().TargetUnitId.Value.ToString();

                //        foreach (var childArrayToken in srcJsonObj.SelectToken(childArrayPathName))
                //        {
                //            AppChildDataDto appChildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetChildUnitId].FirstOrDefault();
                //            if (appChildDataDto != null)
                //            {
                //                AppChildDataDto aNewChildDataDto = appChildDataDto.DeepCopy();
                //                ProcessTransferOneJsonRowToForm(2, childUnitMappings, childArrayToken, childUnitDto, aNewChildDataDto.DictOneToOneFields);
                //                tgtFormData.DictOneToManyFields[targetChildUnitId].Add(aNewChildDataDto);

                //                // 4 Process Grandchild Unit      
                //                foreach (var grandchildUnitDto in childUnitDto.Children)
                //                {
                //                    aNewChildDataDto.DictOneToManyFields[grandchildUnitDto.Id.ToString()].Clear();

                //                    var grandchildUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)grandchildUnitDto.Id && !string.IsNullOrWhiteSpace(o.ChildArrayPathName) && !string.IsNullOrWhiteSpace(o.GrandChildArrayPathName) && o.TargetUnitId.HasValue).ToList();

                //                    if (grandchildUnitMappings.Count > 0)
                //                    {
                //                        string grandchildArrayPathName = grandchildUnitMappings.First().GrandChildArrayPathName;
                //                        string targetGrandchildUnitId = grandchildUnitMappings.First().TargetUnitId.Value.ToString();

                //                        foreach (var grandchildArrayToken in childArrayToken.SelectToken(grandchildArrayPathName))
                //                        {
                //                            AppChildDataDto appGrandchildDataDto = tgtFormData.EditCloneDictOneToManyFields[targetGrandchildUnitId].FirstOrDefault();
                //                            if (appGrandchildDataDto != null)
                //                            {
                //                                AppChildDataDto aNewGrandchildDataDto = appGrandchildDataDto.DeepCopy();
                //                                ProcessTransferOneJsonRowToForm(3, grandchildUnitMappings, grandchildArrayToken, grandchildUnitDto, appGrandchildDataDto.DictOneToOneFields);
                //                                aNewChildDataDto.DictOneToManyFields[targetGrandchildUnitId].Add(aNewGrandchildDataDto);
                //                            }
                //                        }
                //                    }

                //                }
                //            }
                //        }
                //    }
                //}

                //AppCascadingBL.SetupIntialCscadingFieldDataSource(tgtTransactionExDto, tgtFormData, false);
                //AppCascadingBL.SetupIntialAutoCompleteFieldDataSource(tgtTransactionExDto, tgtFormData, false);

                tgtFormData.IsDirty = true;
            }
        }


        public static JObject ProcessFormToApiObjDataTransfer(int dataTransferId, object srcTranactionRId)
        {
            //!!!   To do
            // 1. the data transfer setting type should be "Form TO API".  For now, we use the transfer settig type of "API To Form", and only use the mapping list
            // 2. Need to process child and grandchild level data


            AppTransactionDataTransferSettingExDto dataTransferDto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferId);

            if (dataTransferDto != null && dataTransferDto.DestinationTransactionId.HasValue && dataTransferDto.DataTransferMappingList != null)
            {
                AppMasterDetailDto srcFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(dataTransferDto.DestinationTransactionId.Value, srcTranactionRId);
                DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(srcFormData);

                AppTransactionExDto srcTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dataTransferDto.DestinationTransactionId.Value);

                List<AppTransactionSaveAsMappingExDto> mappingList = ProcessApiToFormDataTransfer_PrepareMappingList(dataTransferDto, srcTransactionExDto);
                Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitIdAndMappings = mappingList.GroupBy(o => o.TargetUnitId.Value).ToDictionary(o => o.Key, o => o.ToList());

                JObject targetJsonObj = new JObject();


                // 1 Process Root Unit      
                var srcRootMasterUnit = srcTransactionExDto.RootMasterUnit;
                var rootUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)srcRootMasterUnit.Id).ToList();

                if (rootUnitMappings.Count > 0)
                {
                    ProcessTransferFormRowToJsonRow(1, rootUnitMappings, targetJsonObj, srcRootMasterUnit, srcFormData.DictOneToOneFields);
                }


                // 2 Process Sibling Unit      
                foreach (var siblingUnitDto in srcTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    var siblingUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)siblingUnitDto.Id).ToList();

                    if (siblingUnitMappings.Count > 0)
                    {
                        ProcessTransferFormRowToJsonRow(1, siblingUnitMappings, targetJsonObj, siblingUnitDto, srcFormData.DictSiblingOneToOneFields[siblingUnitDto.Id.ToString()]);
                    }
                }


                //// 3 Process Child Unit      
                //foreach (var childUnitDto in srcTransactionExDto.RootMasterUnit.Children)
                //{
                //    srcFormData.DictOneToManyFields[childUnitDto.Id.ToString()].Clear();

                //    var childUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)childUnitDto.Id && !string.IsNullOrWhiteSpace(o.ChildArrayPathName) && o.TargetUnitId.HasValue).ToList();

                //    if (childUnitMappings.Count > 0)
                //    {
                //        string childArrayPathName = childUnitMappings.First().ChildArrayPathName;
                //        string targetChildUnitId = childUnitMappings.First().TargetUnitId.Value.ToString();

                //        foreach (var childArrayToken in targetJsonObj.SelectToken(childArrayPathName))
                //        {
                //            AppChildDataDto appChildDataDto = srcFormData.EditCloneDictOneToManyFields[targetChildUnitId].FirstOrDefault();
                //            if (appChildDataDto != null)
                //            {
                //                AppChildDataDto aNewChildDataDto = appChildDataDto.DeepCopy();
                //                ProcessTransferFormRowToJsonRow(2, childUnitMappings, childArrayToken, childUnitDto, aNewChildDataDto.DictOneToOneFields);
                //                srcFormData.DictOneToManyFields[targetChildUnitId].Add(aNewChildDataDto);

                //                // 4 Process Grandchild Unit      
                //                foreach (var grandchildUnitDto in childUnitDto.Children)
                //                {
                //                    aNewChildDataDto.DictOneToManyFields[grandchildUnitDto.Id.ToString()].Clear();

                //                    var grandchildUnitMappings = mappingList.Where(o => o.TargetUnitId.Value == (int)grandchildUnitDto.Id && !string.IsNullOrWhiteSpace(o.ChildArrayPathName) && !string.IsNullOrWhiteSpace(o.GrandChildArrayPathName) && o.TargetUnitId.HasValue).ToList();

                //                    if (grandchildUnitMappings.Count > 0)
                //                    {
                //                        string grandchildArrayPathName = grandchildUnitMappings.First().GrandChildArrayPathName;
                //                        string targetGrandchildUnitId = grandchildUnitMappings.First().TargetUnitId.Value.ToString();

                //                        foreach (var grandchildArrayToken in childArrayToken.SelectToken(grandchildArrayPathName))
                //                        {
                //                            AppChildDataDto appGrandchildDataDto = srcFormData.EditCloneDictOneToManyFields[targetGrandchildUnitId].FirstOrDefault();
                //                            if (appGrandchildDataDto != null)
                //                            {
                //                                AppChildDataDto aNewGrandchildDataDto = appGrandchildDataDto.DeepCopy();
                //                                ProcessTransferFormRowToJsonRow(3, grandchildUnitMappings, grandchildArrayToken, grandchildUnitDto, appGrandchildDataDto.DictOneToOneFields);
                //                                aNewChildDataDto.DictOneToManyFields[targetGrandchildUnitId].Add(aNewGrandchildDataDto);
                //                            }
                //                        }
                //                    }

                //                }
                //            }
                //        }
                //    }
                //}


                return targetJsonObj;
            }

            return null;
        }

        internal static List<AppTransactionSaveAsMappingExDto> ProcessApiToFormDataTransfer_PrepareMappingList(AppTransactionDataTransferSettingExDto dataTransferDto, AppTransactionExDto tgtTransactionExDto)
        {
            List<AppTransactionSaveAsMappingExDto> mappingList = new List<AppTransactionSaveAsMappingExDto>();

            foreach (var aMappingDto in dataTransferDto.DataTransferMappingList)
            {
                if (aMappingDto.TargetFiledId.HasValue)
                {
                    if (tgtTransactionExDto.DictAllTransactionField.ContainsKey(aMappingDto.TargetFiledId.Value))
                    {
                        AppTransactionFieldExDto targetFieldExDto = tgtTransactionExDto.DictAllTransactionField[aMappingDto.TargetFiledId.Value];
                        aMappingDto.TargetTransactionFieldExDto = targetFieldExDto;
                        aMappingDto.TargetUnitId = targetFieldExDto.TransactionUnitId;

                        if (tgtTransactionExDto.DictAllTransactionUnitIdExDto.ContainsKey(targetFieldExDto.TransactionUnitId.ToString()))
                        {
                            aMappingDto.TargetUnitExDto = tgtTransactionExDto.DictAllTransactionUnitIdExDto[targetFieldExDto.TransactionUnitId.ToString()];
                        }

                        mappingList.Add(aMappingDto);
                    }
                }
            }

            PrepareChildAndGrandChildArrayMappingPath(mappingList);

            return mappingList;
        }


        private static void PrepareChildAndGrandChildArrayMappingPath(List<AppTransactionSaveAsMappingExDto> mappingList)
        {
            List<AppTransactionSaveAsMappingExDto> childLevelMappings = mappingList.Where(o => Regex.Matches(o.Name, apiDataTransfer_ArrayToken).Count == 1).ToList();
            List<AppTransactionSaveAsMappingExDto> grandchildLevelMappings = mappingList.Where(o => Regex.Matches(o.Name, apiDataTransfer_ArrayToken).Count == 2).ToList();

            // Json: Manufacturers[0].TotalPrice, 
            // ChildArrayPathName: Manufacturers;
            // ChildArraySubPropertyPathName: TotalPrice       
            foreach (AppTransactionSaveAsMappingExDto mappingDto in childLevelMappings)
            {
                int childArrayTokenIndex = mappingDto.Name.IndexOf(apiDataTransfer_ArrayToken);

                mappingDto.ChildArrayPathName = mappingDto.Name.Substring(0, childArrayTokenIndex);
                mappingDto.ChildArraySubPropertyPathName = mappingDto.Name.Substring(childArrayTokenIndex + 5);
            }

            // Json: Manufacturers[0].Products[0].Price, 
            // ChildArrayPathName: Manufacturers;
            // GrandChildArrayPathName: Products              
            // GrandChildArraySubPropertyPathName: Price  
            foreach (AppTransactionSaveAsMappingExDto mappingDto in grandchildLevelMappings)
            {
                int childArrayTokenIndex = mappingDto.Name.IndexOf(apiDataTransfer_ArrayToken);

                mappingDto.ChildArrayPathName = mappingDto.Name.Substring(0, childArrayTokenIndex);
                string childPropertyPath = mappingDto.Name.Substring(childArrayTokenIndex + 5);

                int grandchildArrayTokenIndex = childPropertyPath.IndexOf(apiDataTransfer_ArrayToken);

                mappingDto.GrandChildArrayPathName = childPropertyPath.Substring(0, grandchildArrayTokenIndex);
                mappingDto.GrandChildArraySubPropertyPathName = childPropertyPath.Substring(grandchildArrayTokenIndex + 5);
            }
        }

        private static void ProcessTransferOneJsonRowToForm(int level, List<AppTransactionSaveAsMappingExDto> mappingList, JToken srcJsonObj, AppTransactionUnitExDto tgtUnitExDto, Dictionary<string, object> tgtOneToOneFields)
        {
            foreach (var mappingDto in mappingList)
            {
                if (mappingDto.TargetFiledId.HasValue)
                {
                    AppTransactionFieldExDto targetTransFieldExDto = tgtUnitExDto.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == mappingDto.TargetFiledId.Value);
                    if (targetTransFieldExDto != null)
                    {
                        string jsonPropertyPath = string.Empty;

                        if (level == 1)
                        {
                            jsonPropertyPath = mappingDto.Name.Replace(apiDataTransfer_LevelToken, ".").Trim();
                        }
                        else if (level == 2)
                        {
                            jsonPropertyPath = mappingDto.ChildArraySubPropertyPathName.Replace(apiDataTransfer_LevelToken, ".").Trim();
                        }
                        else if (level == 3)
                        {
                            jsonPropertyPath = mappingDto.GrandChildArraySubPropertyPathName.Replace(apiDataTransfer_LevelToken, ".").Trim();
                        }

                        if (!string.IsNullOrWhiteSpace(jsonPropertyPath))
                        {
                            object jsonPropertyValue = srcJsonObj.SelectToken(jsonPropertyPath);
                            AssignOneFormFieldValueByType(tgtOneToOneFields, targetTransFieldExDto, jsonPropertyValue);
                        }
                    }
                }
            }
        }

        internal static Dictionary<string, object> AssignOneFormFieldValueByType(Dictionary<string, object> tgtOneToOneFields, AppTransactionFieldExDto targetTransFieldExDto, object jsonPropertyValue)
        {
            if (!tgtOneToOneFields.ContainsKey(targetTransFieldExDto.DataBaseFieldName))
            {
                tgtOneToOneFields.Add(targetTransFieldExDto.DataBaseFieldName, null);
            }

            if (targetTransFieldExDto.DataType.HasValue)
            {
                if (targetTransFieldExDto.DataType == (int)EmAppDataType.Boolean)
                {
                    tgtOneToOneFields[targetTransFieldExDto.DataBaseFieldName] = (object)ControlTypeValueConverter.ConvertValueToBoolean(jsonPropertyValue);
                }
                else if (targetTransFieldExDto.DataType.Value == (int)EmAppDataType.Integer)
                {
                    tgtOneToOneFields[targetTransFieldExDto.DataBaseFieldName] = (object)ControlTypeValueConverter.ConvertValueToInt(jsonPropertyValue);
                }
                else if (targetTransFieldExDto.DataType.Value == (int)EmAppDataType.Decimal)
                {
                    tgtOneToOneFields[targetTransFieldExDto.DataBaseFieldName] = (object)ControlTypeValueConverter.ConvertValueToDecimal(jsonPropertyValue);
                }
                else if (targetTransFieldExDto.DataType.Value == (int)EmAppDataType.Date || targetTransFieldExDto.DataType.Value == (int)EmAppDataType.DateTime)
                {
                    tgtOneToOneFields[targetTransFieldExDto.DataBaseFieldName] = (object)ControlTypeValueConverter.ConvertValueToDate(jsonPropertyValue);
                }
                else if (targetTransFieldExDto.DataType.Value == (int)EmAppDataType.Time)
                {
                    tgtOneToOneFields[targetTransFieldExDto.DataBaseFieldName] = (object)ControlTypeValueConverter.ConvertValueToTimeSpan(jsonPropertyValue);
                }
                else
                {
                    tgtOneToOneFields[targetTransFieldExDto.DataBaseFieldName] = (object)ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(jsonPropertyValue);
                }
            }

            return tgtOneToOneFields;
        }





        private static void ProcessTransferFormRowToJsonRow(int level, List<AppTransactionSaveAsMappingExDto> mappingList, JToken targetJsonObj, AppTransactionUnitExDto srcUnitExDto, Dictionary<string, object> srcOneToOneFields)
        {
            foreach (var mappingDto in mappingList)
            {
                if (mappingDto.TargetFiledId.HasValue)
                {
                    AppTransactionFieldExDto srcTransFieldExDto = srcUnitExDto.AppTransactionFieldList.FirstOrDefault(o => (int)o.Id == mappingDto.TargetFiledId.Value);
                    if (srcTransFieldExDto != null)
                    {
                        string jsonPropertyPath = string.Empty;

                        if (level == 1)
                        {
                            if (srcOneToOneFields.ContainsKey(srcTransFieldExDto.DataBaseFieldName))
                            {
                                string jsonPropertyValue = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(srcOneToOneFields[srcTransFieldExDto.DataBaseFieldName]);
                                jsonPropertyPath = mappingDto.Name.Replace(apiDataTransfer_LevelToken, ".").Trim();

                                var pathParts = jsonPropertyPath.Split('.');
                                JToken node = targetJsonObj;
                                foreach (var pathPart in pathParts)
                                {
                                    var partNode = node.SelectToken(pathPart);

                                    if (partNode == null)
                                    {
                                        if (pathPart != pathParts.Last())
                                        {
                                            ((JObject)node).Add(pathPart, new JObject());
                                            partNode = node.SelectToken(pathPart);
                                        }
                                        else
                                        {
                                            ((JObject)node).Add(pathPart, jsonPropertyValue);
                                            partNode = node.SelectToken(pathPart);
                                        }

                                    }

                                    node = partNode;
                                }
                            }

                        }
                        else if (level == 2)
                        {
                            jsonPropertyPath = mappingDto.ChildArraySubPropertyPathName.Replace(apiDataTransfer_LevelToken, ".").Trim();
                        }
                        else if (level == 3)
                        {
                            jsonPropertyPath = mappingDto.GrandChildArraySubPropertyPathName.Replace(apiDataTransfer_LevelToken, ".").Trim();
                        }

                        //if (!string.IsNullOrWhiteSpace(jsonPropertyPath))
                        //{
                        //    object jsonPropertyValue = srcOneToOneFields[srcTransFieldExDto.DataBaseFieldName];

                        //    targetJsonObj[]
                        //    AssignOneFormFieldValueByType(srcOneToOneFields, srcTransFieldExDto, jsonPropertyValue);
                        //}
                    }
                }
            }
        }


        private static void PrepareCalendarRepeatSettingValues(AppMasterDetailDto masterDetailDto, AppTransactionUnitExDto rootMasterUnit)
        {
            var calendarRepeatSettingField = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarRepeatSetting);

            if (calendarRepeatSettingField != null)
            {
                string repeatSettingValue = string.Empty;

                if (masterDetailDto.DictOneToOneFields[calendarRepeatSettingField.DataBaseFieldName] != null)
                {
                    repeatSettingValue = masterDetailDto.DictOneToOneFields[calendarRepeatSettingField.DataBaseFieldName].ToString();
                }

                masterDetailDto.CalendarRepeatSetting = JsonConvert.DeserializeObject<CalendarRepeatSettingDto>(repeatSettingValue);

                if (masterDetailDto.CalendarRepeatSetting == null)
                {
                    masterDetailDto.CalendarRepeatSetting = new CalendarRepeatSettingDto();
                }

                if (!masterDetailDto.CalendarRepeatSetting.RepeatSimpleSettingValue.HasValue)
                {
                    masterDetailDto.CalendarRepeatSetting.RepeatSimpleSettingValue = (int)EmAppCalendarRepeatSimpleSetting.DoesNotRepeat;
                }

                if (!masterDetailDto.CalendarRepeatSetting.RepeatTimeUnit.HasValue)
                {
                    masterDetailDto.CalendarRepeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Day;
                }

                if (masterDetailDto.CalendarRepeatSetting.RepeatWeeklySelectedWeekdays == null)
                {
                    masterDetailDto.CalendarRepeatSetting.RepeatWeeklySelectedWeekdays = new List<int>();
                }

                if (!masterDetailDto.CalendarRepeatSetting.NbOccurrencesPerTimeUnit.HasValue)
                {
                    masterDetailDto.CalendarRepeatSetting.NbOccurrencesPerTimeUnit = 1;
                }

                if (!masterDetailDto.CalendarRepeatSetting.RepeatEndType.HasValue)
                {
                    masterDetailDto.CalendarRepeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.NeverEnd;
                }

                if (!masterDetailDto.CalendarRepeatSetting.RepeatEndAfterNbOccurrences.HasValue)
                {
                    masterDetailDto.CalendarRepeatSetting.RepeatEndAfterNbOccurrences = 10;
                }

                //if (!masterDetailDto.CalendarRepeatSetting.RepeatEndDate.HasValue)
                //{
                //    masterDetailDto.CalendarRepeatSetting.RepeatEndDate = null;
                //}
            }

            var calendarRepeatTokenField = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarRepeatToken);

            if (calendarRepeatTokenField != null)
            {
                masterDetailDto.CalendarRepeatToken = string.Empty;

                if (masterDetailDto.DictOneToOneFields[calendarRepeatTokenField.DataBaseFieldName] != null)
                {
                    masterDetailDto.CalendarRepeatToken = masterDetailDto.DictOneToOneFields[calendarRepeatTokenField.DataBaseFieldName].ToString();
                }

            }
        }

    }
}