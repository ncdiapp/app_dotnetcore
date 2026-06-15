using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

using APP.Framework;
namespace App.BL
{
    //aChildTransactionUnitExDto.IsUsedForLoadingAvailableSource.HasValue && aChildTransactionUnitExDto.IsUsedForLoadingAvailableSource.Value
    public static class AppMasterDetailFormDataSaveBL
    {
        public static OperationCallResult<AppMasterDetailDto> SaveTransactionData(AppMasterDetailDto rootClientAppformDataDto, bool isCallingFromWorkFlow = false)
        {
            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootClientAppformDataDto.TransactionId);

            if (aAppTransactionExDto.OtherOptions != null && aAppTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {
                if (aAppTransactionExDto.OtherOptions.ApiIntegrationTransactionCrudType.HasValue
                    && aAppTransactionExDto.OtherOptions.ApiIntegrationTransactionCrudType.Value == (int)EmAppTransactionCrudType.Read)
                {
                    if (aAppTransactionExDto.ConsumApiDataModelSaveSettingDto != null)
                    {
                       

                        return null;
                    }

                    return null;
                }
                else
                {
                    return AppMasterDetailApiFormDataSaveBL.SaveTransactionData(rootClientAppformDataDto, isCallingFromWorkFlow);
                }
            }


            OperationCallResult<AppMasterDetailDto> aOperationCallResult = new OperationCallResult<AppMasterDetailDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            if (!rootClientAppformDataDto.IsDirty && !rootClientAppformDataDto.IsNew)
            {
                return aOperationCallResult;
            }


            //   rootClientAppformDataDto.


            aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;


            AppTransactionStructureDto aAppMasterDetailStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(aAppTransactionExDto);

            //!!!!!
            VerifyTransDataAndResetDataType(rootClientAppformDataDto, aAppMasterDetailStructureDto);

            aAppTransactionExDto.AppMasterDetailStructureDtoInfo = aAppMasterDetailStructureDto;
            AppTransactionUnitExDto rootMasterUnit = aAppTransactionExDto.RootMasterUnit;
            rootClientAppformDataDto.RootUnitId = rootMasterUnit.Id;


            Dictionary<string, object> rootOneToOneFields = rootClientAppformDataDto.DictOneToOneFields;
            string tableName = rootMasterUnit.DataBaseTableName;

            //object orgFromDtoRootValue = null;
            AppMasterDetailDto orgOldAppMasterDetailDto = null;

            object rootPkValue = null;

            var calendarRepeatSettingField = rootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarRepeatSetting);

            if (calendarRepeatSettingField != null)
            {
                if (rootClientAppformDataDto.CalendarRepeatSetting != null)
                {
                    //JsonConvert.SerializeObject(aAppProjectWorkFlowActionDto.ActionAttribute);
                    //JsonConvert.DeserializeObject<AppActionAttributeDto>(aAppProjectWorkFlowActionEntity.FormulaExpression);

                    rootOneToOneFields[calendarRepeatSettingField.DataBaseFieldName] = JsonConvert.SerializeObject(rootClientAppformDataDto.CalendarRepeatSetting);
                }
            }



            if (!rootMasterUnit.IsPrimaryKeyIdentityInsert)
            {
                string validationNoneEmptyKeyMessage = ValidaNoneIdenityInsertPkValue(rootMasterUnit, rootOneToOneFields);

                if (!string.IsNullOrWhiteSpace(validationNoneEmptyKeyMessage))
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_PrimaryKey_Error", ValidationItemType.Error, validationNoneEmptyKeyMessage));

                    return aOperationCallResult;
                }
                else
                {

                    rootPkValue = GetUnitPkFiledValue(rootMasterUnit, rootOneToOneFields).First().Value;


                    DataRow rootData = null;

                    rootData = AppDbHelerBL.RetriveOneDataRowWtihPrimayKeyFromDataBaseTable(rootOneToOneFields, rootMasterUnit);

                    if (rootData == null)
                    {
                        rootClientAppformDataDto.IsNew = true;
                    }
                }
            }

            if (rootClientAppformDataDto.IsNew)
            {
                // need to validate if mannuly key exists in dabase  for none IsPrimaryKeyIdentityInsert) 
                if (!rootMasterUnit.IsPrimaryKeyIdentityInsert)
                {
                    DataRow rootData = null;

                    rootData = AppDbHelerBL.RetriveOneDataRowWtihPrimayKeyFromDataBaseTable(rootOneToOneFields, rootMasterUnit);

                    if (rootData != null)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_PrimaryKey_Error", ValidationItemType.Error, " Primary Key already exists "));

                        return aOperationCallResult;
                    }
                }


                rootPkValue = InsertMasterDetail(rootClientAppformDataDto, aAppTransactionExDto, aValidationResult);

            }//// need to update !!
            else if (rootClientAppformDataDto.IsDirty)
            {

                orgOldAppMasterDetailDto = UpdateMasterDetailAndGetOrgDetailDto(rootClientAppformDataDto, aAppTransactionExDto, aValidationResult);

                rootPkValue = GetUnitPkFiledValue(rootMasterUnit, rootOneToOneFields).First().Value;


            }



            if (!aValidationResult.HasErrors)
            {

                var freshDBAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData((int)aAppTransactionExDto.Id, rootPkValue);
                aOperationCallResult.Object = freshDBAppformDataDto;

                // it is new Form dataValue 
                DetectRootSiblingFiledChanged(aAppTransactionExDto, orgOldAppMasterDetailDto, freshDBAppformDataDto);

                foreach (int sourChandgeUnit in rootClientAppformDataDto.SourceChildChangedUnitIds)
                {
                    freshDBAppformDataDto.ChildChangedUnitIds.Add(sourChandgeUnit);
                }



                //  // need to use command to replace  post process, need to remove !!
                //if(aAppTransactionExDto.
                AppTransactionPostPorcessDispatchBL.PostPorcessFormDataAfterSave(
                    aAppMasterDetailStructureDto,
                    rootPkValue,
                    orgOldAppMasterDetailDto,
                    freshDBAppformDataDto,
                    isCallingFromWorkFlow,
                     aOperationCallResult

                );


                // // need to use command to replace  post process need to remove !!

                if (rootClientAppformDataDto.CalendarRepeatSetting != null && !rootClientAppformDataDto.IsIgnoreCalendarRepeat)
                {
                    ApplyCalendarFormRepeatSetting(aAppMasterDetailStructureDto.TransactionId, rootPkValue, rootClientAppformDataDto);
                }



            }




            if (aOperationCallResult.Object != null)
            {
                aOperationCallResult.Object.RootUnitId = rootMasterUnit.Id;
            }

            return aOperationCallResult;
        }

        private static void DetectRootSiblingFiledChanged(AppTransactionExDto aAppTransactionExDto, AppMasterDetailDto orgOldAppMasterDetailDto, AppMasterDetailDto freshDBAppformDataDto)
        {
            if (orgOldAppMasterDetailDto != null)
            {
                freshDBAppformDataDto.DictRootAndSiblingChangedField = new Dictionary<int, KeyValuePair<object, object>>();

                var dictOldFieldIdAndValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(orgOldAppMasterDetailDto, aAppTransactionExDto);
                var dictFreshdbFieldIdAndValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(freshDBAppformDataDto, aAppTransactionExDto);

                foreach (int filedId in dictOldFieldIdAndValue.Keys)
                {
                    if (dictFreshdbFieldIdAndValue.ContainsKey(filedId))
                    {
                        object oldValue = dictOldFieldIdAndValue[filedId];
                        object freshValue = dictFreshdbFieldIdAndValue[filedId];
                        string oldValueString = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(oldValue);
                        string freshValueString = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(freshValue);
                        if (oldValueString != freshValueString)
                        {
                            freshDBAppformDataDto.DictRootAndSiblingChangedField[filedId] = new KeyValuePair<object, object>(oldValue, freshValue);
                        }

                    }


                }

            }
        }


        private static AppMasterDetailDto UpdateMasterDetailAndGetOrgDetailDto(AppMasterDetailDto rootClientAppformDataDto, AppTransactionExDto aAppTransactionExDto,
        ValidationResult aValidationResult)
        {
            Dictionary<string, object> rootOneToOneFields = rootClientAppformDataDto.DictOneToOneFields;
            string tableName = aAppTransactionExDto.RootMasterUnit.DataBaseTableName;

            object rootPkValue = GetUnitPkFiledValue(aAppTransactionExDto.RootMasterUnit, rootOneToOneFields).First().Value;

            //ROOT key nerver been changed for UnitPrimaryKeyIdentity
            AppMasterDetailDto orgAppMasterDetailDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(rootClientAppformDataDto.TransactionId, rootPkValue);
            AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(rootOneToOneFields, aAppTransactionExDto.RootMasterUnit);

            var sqlCmdDto = AppDbHelerBL.GetOneToOneUpdateWithPrimaryKeyValueSqlCmdDto(rootOneToOneFields, aAppTransactionExDto.RootMasterUnit);

            string updateRootUnitCmd = sqlCmdDto.CmdText;
            List<DbParameter> listRootParamters = sqlCmdDto.ListParamters;

            //	string connectInfo = AppMetaDataBL.GetConnectInfo(aAppTransactionExDto.DataSourceFrom ); 

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(aAppTransactionExDto.DataSourceFrom.Value);
            DbProviderFactory factory = databaseFixtureInstance.DbProviderFactory;

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = databaseFixtureInstance.ConnectionString;
                connection.Open();
                DbTransaction trans = null;
                try
                {
                    using (trans = connection.BeginTransaction())
                    {
                        databaseFixtureInstance.ExecuteTransScalar(updateRootUnitCmd, listRootParamters, trans);

                        ProcessRootUnitExtendField(rootOneToOneFields, aAppTransactionExDto.RootMasterUnit, databaseFixtureInstance, rootPkValue, trans);

                        string errormessageSibling;
                        errormessageSibling = UpdateAllSiblingUnit(databaseFixtureInstance,rootClientAppformDataDto, aAppTransactionExDto, rootPkValue, trans);

                        // update child
                        string errormessageChild = UpdateAllChildUnit(databaseFixtureInstance,rootClientAppformDataDto, aAppTransactionExDto, rootPkValue, orgAppMasterDetailDto, trans);

                        if (!string.IsNullOrWhiteSpace(errormessageSibling) || !string.IsNullOrWhiteSpace(errormessageChild))
                        {
                            trans.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, errormessageChild + errormessageChild));
                        }
                        else
                        {
                            trans.Commit();

                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }
                    }

                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
                }

            }

            return orgAppMasterDetailDto;
        }

        internal static void VerifyTransDataAndResetDataType(AppMasterDetailDto aformData, AppTransactionStructureDto formStructure)
        {
            object rootUnnitID = formStructure.HierarchyTransactionExdto.RootMasterUnit.Id;

            var rootUnitImageBinaryFieldNames = formStructure.DictUnitIdUnitImageBinaryFieldNames[rootUnnitID.ToString()];


            var dictOnToOne = aformData.DictOneToOneFields;

            ValidaUnitValue(rootUnitImageBinaryFieldNames, dictOnToOne);

            var dictChildDateSet = aformData.DictOneToManyFields;

            foreach (string childUnitId in dictChildDateSet.Keys)
            {

                List<AppChildDataDto> listChid = dictChildDateSet[childUnitId];

                foreach (AppChildDataDto appChildDataDto in listChid)
                {
                    var childUnitImageBinaryFieldNames = formStructure.DictUnitIdUnitImageBinaryFieldNames[childUnitId];
                    ValidaUnitValue(childUnitImageBinaryFieldNames, appChildDataDto.DictOneToOneFields);

                    if (!appChildDataDto.DictOneToManyFields.IsEmpty())
                    {
                        foreach (string grandUnit in appChildDataDto.DictOneToManyFields.Keys)
                        {

                            List<AppChildDataDto> glist = appChildDataDto.DictOneToManyFields[grandUnit];

                            var grandchildUnitImageBinaryFieldNames = formStructure.DictUnitIdUnitImageBinaryFieldNames[grandUnit];

                            foreach (AppChildDataDto aAppChildDataDto in glist)
                            {

                                var gd = aAppChildDataDto.DictOneToOneFields;

                                ValidaUnitValue(grandchildUnitImageBinaryFieldNames, gd);
                            }




                        }
                    }
                }




            }


        }

        private static void ValidaUnitValue(List<string> rootUnitImageBinaryFieldNames, Dictionary<string, object> dictOnToOne)
        {
            foreach (string fieldName in rootUnitImageBinaryFieldNames)
            {
                if (dictOnToOne.ContainsKey(fieldName) && dictOnToOne[fieldName] != null)
                {
                    string base64String = dictOnToOne[fieldName] as string;

                    dictOnToOne[fieldName] = Convert.FromBase64String(base64String);
                }
            }
        }


        public static OperationCallResult<AppMasterDetailDto> SaveAsMasterDetailTransactionData(int transactionId, object rootPrimaryKeyValue)
        {
            var rootClientAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, rootPrimaryKeyValue);

            ValidationResult aValidationResult = new ValidationResult();

            List<AppTransactionSaveAsMappingExDto> mappingList = AppTransactionSaveAsMappingBL.RetrieveAllAppTransactionSaveAsMappingListByTransactionId(transactionId).ToList();

            Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitSaveAsMapping = mappingList.GroupBy(o => o.MappingUnitId).ToDictionary(o => o.Key.Value, o => o.ToList());

            AppTransactionExDto AppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(rootClientAppformDataDto.TransactionId);

            var rootMasterUnit = AppTransactionExDto.RootMasterUnit;

            Dictionary<string, object> RootOneToOneFields = rootClientAppformDataDto.DictOneToOneFields;

            // need to keep old valie for porject or woflow flow trigger
            Dictionary<string, object> orgRootObejctValue = CloneRootObejctValue(RootOneToOneFields);

            // Set root Pk as null as new root object
            //RootOneToOneFields[rootMasterUnit.PrimaryKeyDbfield] = null;
            SaveAsOneUnitOneRow(dictUnitSaveAsMapping, rootMasterUnit, RootOneToOneFields);

            foreach (string childUnitid in rootClientAppformDataDto.DictOneToManyFields.Keys)
            {
                var childUnit = AppTransactionExDto.DictAllTransactionUnitIdExDto[childUnitid];

                List<AppChildDataDto> childUnitData = rootClientAppformDataDto.DictOneToManyFields[childUnitid];

                foreach (AppChildDataDto appChildDataDto in childUnitData)
                {
                    Dictionary<string, object> childOneToOneFields = appChildDataDto.DictOneToOneFields;
                    SaveAsOneUnitOneRow(dictUnitSaveAsMapping, childUnit, childOneToOneFields);

                    //grand child
                    foreach (string grandUnitId in appChildDataDto.DictOneToManyFields.Keys)
                    {
                        var gradnchildUnit = AppTransactionExDto.DictAllTransactionUnitIdExDto[grandUnitId];

                        List<AppChildDataDto> grandchildOneToOneFieldsList = appChildDataDto.DictOneToManyFields[grandUnitId];

                        foreach (var aAppChildDataDto in grandchildOneToOneFieldsList)
                        {
                            Dictionary<string, object> gradnOneToOneFields = aAppChildDataDto.DictOneToOneFields;
                            SaveAsOneUnitOneRow(dictUnitSaveAsMapping, gradnchildUnit, gradnOneToOneFields);
                        }
                    } // end of grand child
                } // end of child
            }
            rootClientAppformDataDto.IsNew = true;

            OperationCallResult<AppMasterDetailDto> toReturn = SaveTransactionData(rootClientAppformDataDto);

            // need to
            if (!toReturn.ValidationResult.HasErrors)
            {
            }

            return toReturn;
        }




        //// RootKey nerver been changed !!!
        private static string UpdateAllChildUnit(DatabaseFixture databaseFixtureInstance,AppMasterDetailDto rootClientAppformDataDto, AppTransactionExDto aAppTransactionExDto, object rootPkValue, AppMasterDetailDto orgAppMasterDetailDto, DbTransaction trans)
        {
            string errormessage = "";

            rootClientAppformDataDto.DictChildUnitIdChangedCollection = new Dictionary<int, Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>>>();

            foreach (string childUnitId in rootClientAppformDataDto.DictOneToManyFields.Keys)
            {

                AppTransactionUnitExDto childTransactionUnitExDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto[childUnitId];
                if (childTransactionUnitExDto.IsEditableUnit)
                {
                    List<AppChildDataDto> updateChildUnitDataList = rootClientAppformDataDto.DictOneToManyFields[childUnitId];



                    foreach (AppChildDataDto appChildDataDto in updateChildUnitDataList)
                    {

                        PassRootKeyValueToSiblingOrChildUnit(appChildDataDto.DictOneToOneFields, rootPkValue, childTransactionUnitExDto);
                    }

                    //need to transfer parakyeLink key value to child
                    List<AppChildDataDto> orgChildUnitDataList = orgAppMasterDetailDto.DictOneToManyFields[childUnitId];


                    Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>> changeCollection = new Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>>(
                               new List<AppChildDataDto>(),
                               new List<AppChildDataDto>(),
                               new List<AppChildDataDto>()
                        );


                    rootClientAppformDataDto.DictChildUnitIdChangedCollection[int.Parse(childUnitId)] = changeCollection;

                    var result = UpdateOneChildUnit(databaseFixtureInstance,aAppTransactionExDto, rootPkValue, trans, childTransactionUnitExDto, updateChildUnitDataList, orgChildUnitDataList, changeCollection);


                    errormessage = errormessage + System.Environment.NewLine + result;


                }


            }

            return errormessage;
        }

        //	// for list Edit  rootPkValue  is always empty
        internal static string UpdateOneChildUnit(DatabaseFixture databaseFixtureInstance ,AppTransactionExDto appTransactionExDto, object rootPkValue, DbTransaction trans, AppTransactionUnitExDto childTransactionUnitExDto, List<AppChildDataDto> clientUiChildUnitDataList, List<AppChildDataDto> orgChildUnitDataList, Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>> changeCollection)
        {
            string ErrorEmssageRetrun = "";

            if (!childTransactionUnitExDto.IsEditableUnit)
            {
                return "";
            }


            // Get new childList

            List<AppChildDataDto> newAddedListOut = changeCollection.Item1;
            List<AppChildDataDto> updateListOut = changeCollection.Item2;
            List<AppChildDataDto> deleteListOut = changeCollection.Item3;

            // all are insert 
            if (orgChildUnitDataList.IsEmpty())
            {

                newAddedListOut.AddRange(clientUiChildUnitDataList);
                foreach (AppChildDataDto newAppChildDataDto in clientUiChildUnitDataList)
                {
                    InsertAppChildDataDto(databaseFixtureInstance,appTransactionExDto, rootPkValue, trans, childTransactionUnitExDto, newAppChildDataDto);
                }

            }
            else
            {


                Dictionary<string, AppChildDataDto> dictOrgKeyValue = ClassifyChildUnitDataSet(childTransactionUnitExDto,
                 clientUiChildUnitDataList,
                 orgChildUnitDataList,
                 newAddedListOut,
                 updateListOut,
                  deleteListOut,
                  ref ErrorEmssageRetrun);

                foreach (AppChildDataDto newAppChildDataDto in newAddedListOut)
                {
                    InsertAppChildDataDto(databaseFixtureInstance,appTransactionExDto, rootPkValue, trans, childTransactionUnitExDto, newAppChildDataDto);
                }


                foreach (AppChildDataDto updatechildDataDto in updateListOut)
                {
                    string conmKey = GetOneUnitCombineKey(childTransactionUnitExDto, updatechildDataDto.DictOneToOneFields);
                    var origichildDataDto = dictOrgKeyValue[conmKey];
                    string updateOneChildRowGrandChildResult = UpdateOneChildUnitOneRow(databaseFixtureInstance,appTransactionExDto, childTransactionUnitExDto, updatechildDataDto, origichildDataDto, trans);

                    //if(updatechildDataDto.IsGrandChildChangedCollection )
                    //{

                    //}

                    ErrorEmssageRetrun = ErrorEmssageRetrun + System.Environment.NewLine + updateOneChildRowGrandChildResult;



                }

                //

                List<Dictionary<string, object>> deleteKeyList = new List<Dictionary<string, object>>();

                foreach (AppChildDataDto appChildDataDto in deleteListOut)
                {
                    Dictionary<string, object> deleteKye = new Dictionary<string, object>();

                    var dictOneToOne = appChildDataDto.DictOneToOneFields;

                    foreach (string pk in childTransactionUnitExDto.PrimaryKeyDbfieldList)
                    {
                        deleteKye[pk] = dictOneToOne[pk];

                    }

                    deleteKeyList.Add(deleteKye);
                }
                if (deleteKeyList.Count > 0)
                {
                    DeleteOneChildUnitAndGrandChildUnit(databaseFixtureInstance,trans, childTransactionUnitExDto, deleteKeyList);
                }

            }






            return ErrorEmssageRetrun;
        }

        internal static void DeleteOneChildUnitAndGrandChildUnit(DatabaseFixture databaseFixtureInstance ,DbTransaction trans, AppTransactionUnitExDto childTransactionUnitExDto,
                List<Dictionary<string, object>> childPkValueIDList)
        {

            if (!childTransactionUnitExDto.IsEditableUnit)
            {
                return;
            }

            if (childPkValueIDList.Count > 0)
            {
                foreach (var grandChildUnitDto in childTransactionUnitExDto.Children)
                {

                    if (!grandChildUnitDto.IsEditableUnit)
                    {
                        continue;
                    }

                    string qulifiedGrandChildTableName = AppMetaDataBL.GetUnitQulifiedTableName(grandChildUnitDto);

                    string whereCaluse = string.Empty;


                    List<DbParameter> grandChildParaList = AppMasterDetailFormDataLoadBL.SetupGrandChildWhereClasueWithChildPkValue(childPkValueIDList, grandChildUnitDto, ref whereCaluse);

                    if (whereCaluse != string.Empty)
                    {
                        string deleteGrandChildstatment = "   DELETE FROM " + qulifiedGrandChildTableName + "  WHERE " + whereCaluse;

                        databaseFixtureInstance.ExecuteTransScalar(deleteGrandChildstatment, grandChildParaList, trans);
                        //adpater.ExecuteScalarQuery(deleteGrandChildstatment, grandChildParaList);
                    }
                }

                string childDeleteWhere = string.Empty;

                List<DbParameter> childparaList = AppMasterDetailFormDataLoadBL.SetupMutipleSetKeyValueWhereClause(childPkValueIDList, ref childDeleteWhere, childTransactionUnitExDto);

                if (childDeleteWhere != string.Empty)

                {
                    string qulifiedChildTableName = AppMetaDataBL.GetUnitQulifiedTableName(childTransactionUnitExDto);

                    string deleteChildStatment = "  DELETE FROM " + qulifiedChildTableName + " WHERE " + childDeleteWhere;

                    //adpater.ExecuteScalarQuery(deleteChildStatment, childparaList);

                    databaseFixtureInstance.ExecuteTransScalar(deleteChildStatment, childparaList, trans);
                }
            }
        }


        internal static string UpdateOneChildUnitOneRow(DatabaseFixture databaseFixtureInstance, AppTransactionExDto AppTransactionExDto, AppTransactionUnitExDto childTransactionUnitExDto, AppChildDataDto updatechildDataDto,
            AppChildDataDto origichildDataDto, DbTransaction trans)
        {

            string ErrorEmssageRetrun = "";



            if (!childTransactionUnitExDto.IsEditableUnit)
            {

                return "";
            }


            UpdateOneUnitRecord(databaseFixtureInstance,updatechildDataDto.DictOneToOneFields, childTransactionUnitExDto, trans);


            if (updatechildDataDto.DictOneToManyFields.IsEmpty())
                return ""; ;


            var dictGrandChildChangedCollection = updatechildDataDto.DictGrandChildChangedCollection;
            // process  AppformChildDataDto.Dictionary<String, List<Dictionary<string, object>>> DictOneToManyFields
            foreach (var grandChildUnitId in updatechildDataDto.DictOneToManyFields.Keys)
            {
                AppTransactionUnitExDto grandchildAppTransactionUnitExDto = AppTransactionExDto.DictAllTransactionUnitIdExDto[grandChildUnitId];


                if (!grandchildAppTransactionUnitExDto.IsEditableUnit)
                {
                    continue;
                }


                if (!dictGrandChildChangedCollection.IsEmpty() && dictGrandChildChangedCollection.ContainsKey(grandChildUnitId))
                {
                    var changeCollection = dictGrandChildChangedCollection[grandChildUnitId];

                    List<Dictionary<string, object>> newAddedList = changeCollection.Item1;
                    List<Dictionary<string, object>> updateList = changeCollection.Item2;
                    List<Dictionary<string, object>> deleteList = changeCollection.Item3;


                    //!!!
                    foreach (var dictGradnChildData in newAddedList)
                    {
                        // var dictGradnChildData = aGrandChildDataDto.DictOneToOneFields;
                        PassChildLinkKeyValueToGrandChild(grandchildAppTransactionUnitExDto, dictGradnChildData, updatechildDataDto.DictOneToOneFields);
                    }

                    foreach (var dictGradnChildData in updateList)
                    {
                        // var dictGradnChildData = aGrandChildDataDto.DictOneToOneFields;
                        PassChildLinkKeyValueToGrandChild(grandchildAppTransactionUnitExDto, dictGradnChildData, updatechildDataDto.DictOneToOneFields);
                    }


                    //insert
                    InsertGrandChild(databaseFixtureInstance,trans, updatechildDataDto, grandchildAppTransactionUnitExDto, newAddedList);

                    //Update
                    foreach (Dictionary<string, object> updateGrandCildDataDto in updateList)
                    {


                        UpdateOneUnitRecord(databaseFixtureInstance,updateGrandCildDataDto, grandchildAppTransactionUnitExDto, trans);

                    }

                    //Deelte 
                    foreach (Dictionary<string, object> udeletCildDataDto in deleteList)
                    {

                        DeleteOneUnit(databaseFixtureInstance,trans, grandchildAppTransactionUnitExDto, deleteList);

                    }
                }




            }

            return ErrorEmssageRetrun;



        }

        private static string CheckGrandChildUnitChange(AppChildDataDto updatechildDataDto, AppChildDataDto origichildDataDto, string grandChildUnitId, AppTransactionUnitExDto grandchildAppTransactionUnitExDto, List<Dictionary<string, object>> newAddedList, List<Dictionary<string, object>> updateList, List<Dictionary<string, object>> deleteList)
        {
            List<AppChildDataDto> orgGrandChildDataSet = origichildDataDto.DictOneToManyFields[grandChildUnitId];
            List<AppChildDataDto> updateGrandChildDataList = updatechildDataDto.DictOneToManyFields[grandChildUnitId];


            string errorMesageREsult = ClassifyGrandChildUnitDataSet(grandchildAppTransactionUnitExDto, updateGrandChildDataList, orgGrandChildDataSet, newAddedList, updateList, deleteList);

            return errorMesageREsult;
        }

        internal static void DeleteOneUnit(DatabaseFixture databaseFixtureInstance , DbTransaction trans, AppTransactionUnitExDto grandchildAppTransactionUnitExDto,
                List<Dictionary<string, object>> deleteList)
        {
            if (deleteList.Count > 0)
            {
                List<Dictionary<string, object>> deleteKeyList = GetUnitPkValueList(grandchildAppTransactionUnitExDto, deleteList);

                string deleteWhere = string.Empty;

                string qulifiedGrandChildTableName = AppMetaDataBL.GetUnitQulifiedTableName(grandchildAppTransactionUnitExDto);
                List<DbParameter> childparaList = AppMasterDetailFormDataLoadBL.SetupMutipleSetKeyValueWhereClause(deleteKeyList, ref deleteWhere, grandchildAppTransactionUnitExDto);

                if (deleteWhere != string.Empty)
                {
                    string deleteChildStatment = "  DELETE FROM " + qulifiedGrandChildTableName + " WHERE " + deleteWhere;

                    databaseFixtureInstance.ExecuteTransScalar(deleteChildStatment, childparaList, trans);


                    //adpater.ExecuteScalarQuery(deleteChildStatment, childparaList);
                }
            }
        }

        private static List<Dictionary<string, object>> GetUnitPkValueList(AppTransactionUnitExDto grandchildAppTransactionUnitExDto, List<Dictionary<string, object>> deleteList)
        {
            List<Dictionary<string, object>> deleteKeyList = new List<Dictionary<string, object>>();

            foreach (var dictOneToOne in deleteList)
            {
                Dictionary<string, object> deleteKye = new Dictionary<string, object>();



                foreach (string pk in grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList)
                {
                    deleteKye[pk] = dictOneToOne[pk];

                }

                deleteKeyList.Add(deleteKye);
            }

            return deleteKeyList;
        }
        private static string ClassifyGrandChildUnitDataSet(
                 AppTransactionUnitExDto grandchildAppTransactionUnitExDto,
                 List<AppChildDataDto> updateGrandChildDataSet,
                 List<AppChildDataDto> orgGrandChildDataSet,
                 List<Dictionary<string, object>> newAddedList,
                 List<Dictionary<string, object>> updateList,
                 List<Dictionary<string, object>> deleteList
            )
        {
            string ErrorEmssageRetrun = "";
            List<string> unitPkFields = grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList;

            Dictionary<string, Dictionary<string, object>> dictOrgKeyValue = new Dictionary<string, Dictionary<string, object>>();

            foreach (Dictionary<string, object> dictOneToOneFields in
                orgGrandChildDataSet.Select(o => o.DictOneToOneFields))
            {

                string conmKey = "";
                foreach (string pk in grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList)
                {
                    conmKey = conmKey + string.Format("{0}_", dictOneToOneFields[pk]);
                }

                dictOrgKeyValue[conmKey] = dictOneToOneFields;
            }




            List<string> updateExistKeyValueList = new List<string>();
            foreach (Dictionary<string, object> dictOneToOneFields in
                    updateGrandChildDataSet.Select(o => o.DictOneToOneFields)

                )
            {

                string conmKey = "";
                foreach (string pk in grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList)
                {
                    conmKey = conmKey + string.Format("{0}_", dictOneToOneFields[pk]);
                }

                updateExistKeyValueList.Add(conmKey);

                if (grandchildAppTransactionUnitExDto.IsPrimaryKeyIdentityInsert)
                {
                    object childPkValue = dictOneToOneFields[unitPkFields.First()];

                    if (childPkValue == null)
                    {

                        newAddedList.Add(dictOneToOneFields);

                    }
                    else
                    {
                        if (dictOrgKeyValue.ContainsKey(conmKey))
                        {
                            updateList.Add(dictOneToOneFields);
                        }

                    }
                }
                else// it none primay key update 
                {
                    if (dictOrgKeyValue.ContainsKey(conmKey))
                    {
                        updateList.Add(dictOneToOneFields);
                    }
                    else
                    {
                        newAddedList.Add(dictOneToOneFields);
                    }


                }


            }

            RemoveGrandChildNotModifiedRecord(grandchildAppTransactionUnitExDto, updateList, dictOrgKeyValue);

            if (!grandchildAppTransactionUnitExDto.IsPrimaryKeyIdentityInsert)
            {
                var duplciatKeys = updateExistKeyValueList.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

                if (duplciatKeys.Count > 0)
                {
                    ErrorEmssageRetrun = " Deplciated Key at :" + grandchildAppTransactionUnitExDto.UnitDisplayName;


                }


            }



            List<string> needToDeleteKey = dictOrgKeyValue.Keys.Except(updateExistKeyValueList).ToList();

            deleteList.AddRange(
                             dictOrgKeyValue.Where(o => needToDeleteKey.Contains(o.Key))
                             .Select(o => o.Value)
                );


            return ErrorEmssageRetrun;
        }

        private static void RemoveGrandChildNotModifiedRecord(AppTransactionUnitExDto oneChildUnitExDto, List<Dictionary<string, object>> grandChildUpdateList, Dictionary<string, Dictionary<string, object>> dictOrgKeyValue)
        {
            var compareFiledList = oneChildUnitExDto.ListExcludeSystemTokenDbFileName;

            List<Dictionary<string, object>> needRemoveTheSameValueRecord = new List<Dictionary<string, object>>();
            foreach (Dictionary<string, object> dictGrandChildKeyValues in grandChildUpdateList)
            {
                string conmKey = "";
                foreach (string pk in oneChildUnitExDto.PrimaryKeyDbfieldList)
                {
                    conmKey = conmKey + string.Format("{0}_", dictGrandChildKeyValues[pk]);
                }



                string currentValue = GetDictOneToOneStringValue(compareFiledList, dictGrandChildKeyValues);



                string oldtValue = GetDictOneToOneStringValue(compareFiledList, dictOrgKeyValue[conmKey]);

                if (currentValue == oldtValue)
                {
                    needRemoveTheSameValueRecord.Add(dictGrandChildKeyValues);
                }

            }

            foreach (var removeItem in needRemoveTheSameValueRecord)
            {
                grandChildUpdateList.Remove(removeItem);
            }
        }

        //private static string ClassifyGrandChildUnitDataSet(
        //    AppTransactionUnitExDto grandchildAppTransactionUnitExDto, List<AppChildDataDto> updateGrandChildDataSet, List<AppChildDataDto> orgGrandChildDataSet,
        //   List<Dictionary<string, object>> newAddedList, List<Dictionary<string, object>> updateList, List<Dictionary<string, object>> deleteList)
        //{
        //    string ErrorEmssageRetrun = "";
        //    List<string> unitPkFields = grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList;

        //    Dictionary<string, Dictionary<string, object>> dictOrgKeyValue = new Dictionary<string, Dictionary<string, object>>();

        //    foreach (AppChildDataDto aAppChildDataDto in orgGrandChildDataSet)
        //    {
        //        Dictionary<string, object> dictOneToOneFields = aAppChildDataDto.DictOneToOneFields;
        //        string conmKey = "";
        //        foreach (string pk in grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList)
        //        {
        //            conmKey = conmKey + string.Format("{0}_", dictOneToOneFields[pk]);
        //        }

        //        dictOrgKeyValue[conmKey] = dictOneToOneFields;
        //    }

        //    List<string> updateExistKeyValueList = new List<string>();

        //    foreach (AppChildDataDto aAppChildDataDto in orgGrandChildDataSet)
        //    {
        //        Dictionary<string, object> dictOneToOneFields = aAppChildDataDto.DictOneToOneFields;

        //        string conmKey = "";
        //        foreach (string pk in grandchildAppTransactionUnitExDto.PrimaryKeyDbfieldList)
        //        {
        //            conmKey = conmKey + string.Format("{0}_", dictOneToOneFields[pk]);
        //        }

        //        updateExistKeyValueList.Add(conmKey);

        //        if (grandchildAppTransactionUnitExDto.IsPrimaryKeyIdentityInsert)
        //        {
        //            object childPkValue = dictOneToOneFields[unitPkFields.First()];

        //            if (childPkValue == null)
        //            {

        //                newAddedList.Add(dictOneToOneFields);

        //            }
        //            else
        //            {
        //                updateList.Add(dictOneToOneFields);

        //            }
        //        }
        //        else// it none primay key update 
        //        {
        //            if (dictOrgKeyValue.ContainsKey(conmKey))
        //            {
        //                updateList.Add(dictOneToOneFields);
        //            }
        //            else
        //            {
        //                newAddedList.Add(dictOneToOneFields);
        //            }


        //        }


        //    }

        //    if (!grandchildAppTransactionUnitExDto.IsPrimaryKeyIdentityInsert)
        //    {
        //        var duplciatKeys = updateExistKeyValueList.GroupBy(x => x)
        //        .Where(g => g.Count() > 1)
        //        .Select(y => y.Key)
        //        .ToList();

        //        if (duplciatKeys.Count > 0)
        //        {
        //            ErrorEmssageRetrun = " Deplciated Key at :" + grandchildAppTransactionUnitExDto.UnitDisplayName;


        //        }


        //    }



        //    List<string> needToDeleteKey = dictOrgKeyValue.Keys.Except(updateExistKeyValueList).ToList();

        //    deleteList.AddRange(
        //                     dictOrgKeyValue.Where(o => needToDeleteKey.Contains(o.Key))
        //                     .Select(o => o.Value)
        //        );


        //    return ErrorEmssageRetrun;
        //}


        //updateChildUnitDataList, 
        //			 orgChildUnitDataList, 
        //			 newAddedListOut, 
        //			 updateListOut,
        //			  deleteListOut,
        //			  ref ErrorEmssageRetrun

        internal static Dictionary<string, AppChildDataDto> ClassifyChildUnitDataSet(AppTransactionUnitExDto oneChildUnitExDto,
        List<AppChildDataDto> clientUiChildUnitDataList,
        List<AppChildDataDto> orgChildUnitDataList,
            List<AppChildDataDto> newAddedList,
             List<AppChildDataDto> updatedChildDataDtoList, List<AppChildDataDto> deleteList, ref string ErrorEmssageRetrun)
        {

            List<string> unitPkFields = oneChildUnitExDto.PrimaryKeyDbfieldList;

            Dictionary<string, AppChildDataDto> dictOrgKeyValue = new Dictionary<string, AppChildDataDto>();

            foreach (AppChildDataDto appChildDataDto in orgChildUnitDataList)
            {
                string conmKey = GetOneUnitCombineKey(oneChildUnitExDto, appChildDataDto.DictOneToOneFields);  //GetChildUnitOneRowComkey(oneChildUnitExDto, appChildDataDto);
                dictOrgKeyValue[conmKey] = appChildDataDto;
            }

            Dictionary<string, AppChildDataDto> dictClientUiKeyValue = new Dictionary<string, AppChildDataDto>();

            List<string> clientUiExistKeyValueList = new List<string>();
            foreach (AppChildDataDto appChildDataDto in clientUiChildUnitDataList)
            {

                //string conmKey = "";
                //foreach (string pk in oneChildUnitExDto.PrimaryKeyDbfieldList)
                //{
                //    conmKey = conmKey + string.Format("{0}_", appChildDataDto.DictOneToOneFields[pk]);
                //}

                string conmKey = "";
                foreach (string pk in oneChildUnitExDto.PrimaryKeyDbfieldList)
                {
                    object pkVal = null;
                    appChildDataDto.DictOneToOneFields?.TryGetValue(pk, out pkVal);

                    if(pkVal != null)
                    {
                        conmKey = conmKey + string.Format("{0}_", pkVal);

                    }
                    
                }
                if (!string.IsNullOrEmpty(conmKey))
                {
                    clientUiExistKeyValueList.Add(conmKey);
                }
                


                if (oneChildUnitExDto.IsPrimaryKeyIdentityInsert)
                {
                    object childPkValue = null;
                    appChildDataDto.DictOneToOneFields?.TryGetValue(unitPkFields.First(), out childPkValue);

                    if (childPkValue == null)
                    {

                        newAddedList.Add(appChildDataDto);

                    }
                    else
                    {
                        if (dictOrgKeyValue.ContainsKey(conmKey))
                        {
                            updatedChildDataDtoList.Add(appChildDataDto);
                        }

                    }
                }
                else// it none primay key update 
                {
                    if (dictOrgKeyValue.ContainsKey(conmKey))
                    {
                        updatedChildDataDtoList.Add(appChildDataDto);
                    }
                    else
                    {
                        newAddedList.Add(appChildDataDto);
                    }


                }


            }

            // need to check duplciation Key
            // need to checl if 
            CheckIftheChildRowIsChangedAndSetupGranChildRowChange(oneChildUnitExDto, updatedChildDataDtoList, dictOrgKeyValue);

            if (!oneChildUnitExDto.IsPrimaryKeyIdentityInsert)
            {
                var duplciatKeys = clientUiExistKeyValueList.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();

                if (duplciatKeys.Count > 0)
                {
                    ErrorEmssageRetrun = " Deplciated Key at :" + oneChildUnitExDto.UnitDisplayName;


                }


            }


            List<string> needToDeleteKey = dictOrgKeyValue.Keys.Except(clientUiExistKeyValueList).ToList();

            deleteList.AddRange(
                             dictOrgKeyValue.Where(o => needToDeleteKey.Contains(o.Key))
                             .Select(o => o.Value)
                );

            return dictOrgKeyValue;
        }

        private static string CheckIftheChildRowIsChangedAndSetupGranChildRowChange(AppTransactionUnitExDto oneChildUnitExDto,
            List<AppChildDataDto> potentialChildDataDtoList, Dictionary<string, AppChildDataDto> dictOrgKeyValue)
        {
            string errorMesage = "";

            AppTransactionExDto aAppTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(oneChildUnitExDto.TransactionId);

            var compareFiledList = oneChildUnitExDto.ListExcludeSystemTokenDbFileName;

            List<AppChildDataDto> notRealModifiedChildUniotRowList = new List<AppChildDataDto>();
            foreach (AppChildDataDto updatechildDataDto in potentialChildDataDtoList)
            {
                string conmKey = "";
                foreach (string pk in oneChildUnitExDto.PrimaryKeyDbfieldList)
                {
                    conmKey = conmKey + string.Format("{0}_", updatechildDataDto.DictOneToOneFields[pk]);
                }

                var dictCurrentOntowOne = updatechildDataDto.DictOneToOneFields;
                string currentValue = GetDictOneToOneStringValue(compareFiledList, dictCurrentOntowOne);

                AppChildDataDto OrgAppChildDataDto = dictOrgKeyValue[conmKey];

                string oldtValue = GetDictOneToOneStringValue(compareFiledList, OrgAppChildDataDto.DictOneToOneFields);



                updatechildDataDto.DictGrandChildChangedCollection = new Dictionary<string, Tuple<List<Dictionary<string, object>>, List<Dictionary<string, object>>, List<Dictionary<string, object>>>>();

                if (!oneChildUnitExDto.Children.IsEmpty())
                {

                    foreach (var grandChildUnitId in updatechildDataDto.DictOneToManyFields.Keys)
                    {

                        AppTransactionUnitExDto grandchildAppTransactionUnitExDto = aAppTransactionExDto.DictAllTransactionUnitIdExDto[grandChildUnitId];
                        List<AppChildDataDto> orgGrandChildDataSet = updatechildDataDto.DictOneToManyFields[grandChildUnitId];

                        var newGranChildAddedList = new List<Dictionary<string, object>>();
                        var updateGranChildList = new List<Dictionary<string, object>>();
                        var deleteGrandList = new List<Dictionary<string, object>>();





                        errorMesage = CheckGrandChildUnitChange(updatechildDataDto, OrgAppChildDataDto, grandChildUnitId, grandchildAppTransactionUnitExDto, newGranChildAddedList, updateGranChildList, deleteGrandList);
                        if (!(newGranChildAddedList.IsEmpty() && updateGranChildList.IsEmpty() && deleteGrandList.IsEmpty()))
                        {
                            updatechildDataDto.DictGrandChildChangedCollection[grandChildUnitId] = new Tuple<List<Dictionary<string, object>>, List<Dictionary<string, object>>, List<Dictionary<string, object>>>
                                             (newGranChildAddedList, updateGranChildList, deleteGrandList);

                        }





                    }

                }



                // Child row is changed, no need to check griandChhild
                if (currentValue == oldtValue && updatechildDataDto.DictGrandChildChangedCollection.IsEmpty())
                {
                    notRealModifiedChildUniotRowList.Add(updatechildDataDto);
                }




            }

            foreach (var removeItem in notRealModifiedChildUniotRowList)
            {
                potentialChildDataDtoList.Remove(removeItem);
            }

            return errorMesage; ;
        }

        private static string GetDictOneToOneStringValue(List<string> compareFiledList, Dictionary<string, object> dictOntowOne)
        {
            string currentValue = "";

            List<string> valueString = dictOntowOne
               .Where(o => compareFiledList.Contains(o.Key)).
               Select(o => o.Value == null ? "_" : o.Value.ToString())
               .ToList();
            if (valueString.Count > 0)
            {
                currentValue = valueString.Aggregate((i, j) => i + "_" + j);

            }

            return currentValue;
        }

        internal static string GetOneUnitCombineKey(AppTransactionUnitExDto oneChildUnitExDto, Dictionary<string, object> dictOneToOneFields)
        {
            string conmKey = "";
            foreach (string pk in oneChildUnitExDto.PrimaryKeyDbfieldList)
            {
                conmKey = conmKey + string.Format("{0}_", dictOneToOneFields[pk]);
            }

            return conmKey;
        }

        // RootKey nerver been changed !!!
        internal static string UpdateAllSiblingUnit(DatabaseFixture databaseFixtureInstance,AppMasterDetailDto rootAppformDataDto, AppTransactionExDto transactionExDto, object rootPkValue, DbTransaction trans)
        {
            string errormessge = "";
            foreach (string siblingUnitid in rootAppformDataDto.DictSiblingOneToOneFields.Keys)
            {
                AppTransactionUnitExDto siblingTransactionUnitExDto = transactionExDto.DictAllTransactionUnitIdExDto[siblingUnitid];

                bool isReadonlyUnit = (siblingTransactionUnitExDto.IsReadOnly.HasValue && siblingTransactionUnitExDto.IsReadOnly.Value);

                if (!isReadonlyUnit)
                {
                    Dictionary<string, object> siblingUnitOneToOneFields = rootAppformDataDto.DictSiblingOneToOneFields[siblingUnitid];

                    DatabaseTable siblingDatabaseTable = AppCacheManagerBL.GetDatabaseTable(siblingTransactionUnitExDto);

                    string sbPkDbFied = siblingTransactionUnitExDto.PrimaryKeyDbfieldList[0];

                    object sbPkValue = null;
                    if (siblingUnitOneToOneFields.ContainsKey(sbPkDbFied))
                    {
                        sbPkValue = siblingUnitOneToOneFields[sbPkDbFied];
                    }


                    // exsintg sibling
                    if (sbPkValue != null)
                    {

                        PassRootKeyValueToSiblingOrChildUnit(siblingUnitOneToOneFields, rootPkValue, siblingTransactionUnitExDto);

                        UpdateOneUnitRecord(databaseFixtureInstance,siblingUnitOneToOneFields, siblingTransactionUnitExDto, trans);
                    }
                    else // it is a new add sibling value
                    {

                        InsertOneSiblingUnit(databaseFixtureInstance,rootPkValue, trans, errormessge, siblingTransactionUnitExDto, siblingUnitOneToOneFields);
                    }


                    // delete by Foreigh Key...

                    // string updateRootUnitCmd = sqlCmdDto.CmdText;
                    //   List<DbParameter> listRootParamters = sqlCmdDto.ListParamters;



                    //
                    //  AppDbHelerBL.DeleteChildWithLinkToParentKeyFueld(siblingTransactionUnitExDto, siblingTransactionUnitExDto.DictLinkToParentKeyDbfield.First().Key, rootPkValue, trans);

                    // InsertOneSiblingUnit(rootPkValue, trans, errormessge, siblingTransactionUnitExDto, siblingUnitOneToOneFields);

                    //AppSqlCmdDto aAppSqlCmdDto = AppDbHelerBL.GetOneToOneUpdateWithPrimaryKeyValueSqlCmdDto(siblingUnitOneToOneFields, siblingTransactionUnitExDto);

                }




            }

            return errormessge;
        }

        private static object InsertMasterDetail(AppMasterDetailDto rootAppformDataDto,
            AppTransactionExDto appTransactionExDto,
            ValidationResult aValidationResult)
        {
            Dictionary<string, object> rootOneToOneFields = rootAppformDataDto.DictOneToOneFields;

            var rootMasterUnit = appTransactionExDto.RootMasterUnit;
            //string tableName = rootMasterUnit.DataBaseTableName;

            AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(rootOneToOneFields, rootMasterUnit);

            AppTransDataSystemTokenBL.AssignAuntoGenerationCodeToUnitField(rootOneToOneFields, rootMasterUnit, true);




            string dataBaseTableName = rootMasterUnit.DataBaseTableName;


            if (!string.IsNullOrWhiteSpace(rootMasterUnit.BaseDataBaseTableName))
            {
                dataBaseTableName = rootMasterUnit.BaseDataBaseTableName;

            }




            int? dataSourceFrom = rootMasterUnit.DataSourceFrom;


            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceFrom.Value);

            AppSqlCmdDto sqlCmDto = AppDbHelerBL.GetOneToOneInsertSqlCmdDto(rootOneToOneFields, dataBaseTableName, dataSourceFrom, rootMasterUnit.SchemaOwner);

            object rootPkValue = null;

            if (!rootMasterUnit.IsPrimaryKeyIdentityInsert)
            {
                rootPkValue = GetUnitPkFiledValue(rootMasterUnit, rootOneToOneFields).First().Value;
            }

            //	string connectInfo = AppMetaDataBL.GetConnectInfo(appTransactionExDto.DataSourceFrom );
            // AppCacheManagerBL.GetOneDatabaseFixture(unitExdto.DataSourceFrom.Value);

            bool IsAppSecurityRootUnit = (rootMasterUnit.DataBaseTableName.ToLowerInvariant() == "AppSecurityUser".ToLowerInvariant())
                && rootMasterUnit.DataSourceFrom != AppCacheManagerBL.HostCompayDataBaseID;

            if (IsAppSecurityRootUnit)
            {
                rootPkValue = PorcessHostMasterDB(sqlCmDto, aValidationResult);
                if (rootPkValue == null)
                {
                    return null;
                }
            }
            DbProviderFactory factory = databaseFixtureInstance.DbProviderFactory;

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = databaseFixtureInstance.ConnectionString;
                connection.Open();

                //  DbTransaction trans = connection.BeginTransaction();


                DbTransaction trans = null;
                try
                {
                    using (trans = connection.BeginTransaction())
                    {
                        if (rootMasterUnit.IsPrimaryKeyIdentityInsert)
                        {
                            //rootPkValue = adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);
                            //databaseFixtureInstance
                            if (!IsAppSecurityRootUnit)
                            {
                                rootPkValue = databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
                            }

                        }
                        else // for none sPrimaryKeyIdentity , no need to update the rootPkValue;
                        {
                            //	adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);

                            databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);


                        }
                        ProcessRootUnitExtendField(rootOneToOneFields, rootMasterUnit, databaseFixtureInstance, rootPkValue, trans);


                        // Check Second
                        string silblngError = InsertAllSiblingUnits(databaseFixtureInstance,rootAppformDataDto, appTransactionExDto, rootPkValue, trans);
                        if (!string.IsNullOrWhiteSpace(silblngError))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, silblngError));

                            trans.Rollback();
                        }

                        string childError = InsertChildAndGrandUnits(databaseFixtureInstance,rootAppformDataDto, appTransactionExDto, rootPkValue, trans);

                        if (!string.IsNullOrWhiteSpace(childError))
                        {
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, childError));

                            trans.Rollback();
                        }

                        if (string.IsNullOrWhiteSpace(childError) && string.IsNullOrWhiteSpace(silblngError))
                        {
                            trans.Commit();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_OK", ValidationItemType.Message, "Saved Successfully"));


                        }
                    }



                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, ex.Message));

                    trans.Rollback();
                }
            }

            return rootPkValue;
        }

        private static void ProcessRootUnitExtendField(Dictionary<string, object> rootOneToOneFields, AppTransactionUnitExDto rootMasterUnit, DatabaseFixture databaseFixtureInstance, object rootPkValue, DbTransaction trans)
        {
            if (!rootMasterUnit.DictExtendDbField_Id.IsEmpty())
            {
                // if(rootPkValue != null && rootPkValue.t)
                string deleteOldVaue = @" delete  AppTransactionUnitExtendFieldValue  FROM  AppTransactionUnitExtendFieldValue  where  TransactionUnitID= @TransactionUnitID And UnitPKValue=@UnitPKValue ";
                List<DbParameter> sqlExtendParamters = new List<DbParameter>();



                DbParameter unitIdParameter = databaseFixtureInstance.CreateParameter("TransactionUnitID");
                unitIdParameter.Value = rootMasterUnit.Id;
                sqlExtendParamters.Add(unitIdParameter);

                // need to get first value 
                DbParameter unitPKValueParameter = databaseFixtureInstance.CreateParameter("UnitPKValue");
                unitPKValueParameter.Value = rootPkValue;
                sqlExtendParamters.Add(unitPKValueParameter);
                databaseFixtureInstance.ExecuteTransScalar(deleteOldVaue, sqlExtendParamters, trans);

               
                foreach (string extendFied in rootMasterUnit.DictExtendDbField_Id.Keys)
                {
                    if (rootOneToOneFields.ContainsKey(extendFied))
                    {

                        List<DbParameter> sqlInsertExtendParamters = new List<DbParameter>();

                        unitIdParameter = databaseFixtureInstance.CreateParameter("TransactionUnitID");
                        unitIdParameter.Value = rootMasterUnit.Id;
                        sqlInsertExtendParamters.Add(unitIdParameter);




                        var UnitExtendFiledID = databaseFixtureInstance.CreateParameter("UnitExtendFiledID");
                        UnitExtendFiledID.Value = rootMasterUnit.DictExtendDbField_Id[extendFied];
                        sqlInsertExtendParamters.Add(UnitExtendFiledID);


                        unitPKValueParameter = databaseFixtureInstance.CreateParameter("UnitPKValue");
                        unitPKValueParameter.Value = rootPkValue;
                        sqlInsertExtendParamters.Add(unitPKValueParameter);


                        var ValueText = databaseFixtureInstance.CreateParameter("ValueText");
                        ValueText.Value = rootOneToOneFields[extendFied];
                        sqlInsertExtendParamters.Add(ValueText);

                        string insertToExtendTable = $@"
                                INSERT INTO [AppTransactionUnitExtendFieldValue]
                                           ([TransactionUnitID]
                                           ,[UnitExtendFiledID]
                                           ,[UnitPKValue]
                                           ,[ValueText])
                                     VALUES
                                           ( {unitIdParameter.ParameterName}
                                           ,{UnitExtendFiledID.ParameterName}
                                           ,{unitPKValueParameter.ParameterName}
                                           ,{ValueText.ParameterName}
                                          ) ";

                        databaseFixtureInstance.ExecuteTransScalar(insertToExtendTable, sqlInsertExtendParamters, trans);

                    }
                }





            }
        }

        private static bool InsertMultipleMasterDetailData(List<AppMasterDetailDto> formDataList,
              AppTransactionExDto appTransactionExDto,
              ValidationResult aValidationResult)
        {
            DbTransaction trans = null;


            try
            {
                var rootMasterUnit = appTransactionExDto.RootMasterUnit;
                int? dataSourceFrom = rootMasterUnit.DataSourceFrom;

                string dataBaseTableName = rootMasterUnit.DataBaseTableName;


                if (!string.IsNullOrWhiteSpace(rootMasterUnit.BaseDataBaseTableName))
                {
                    dataBaseTableName = rootMasterUnit.BaseDataBaseTableName;

                }


                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceFrom.Value);

                DbProviderFactory factory = databaseFixtureInstance.DbProviderFactory;

                using (var connection = factory.CreateConnection())
                {
                    connection.ConnectionString = databaseFixtureInstance.ConnectionString;
                    connection.Open();

                    //  DbTransaction trans = connection.BeginTransaction();


                    using (trans = connection.BeginTransaction())
                    {
                        foreach (AppMasterDetailDto rootAppformDataDto in formDataList)
                        {

                            Dictionary<string, object> rootOneToOneFields = rootAppformDataDto.DictOneToOneFields;


                            //string tableName = rootMasterUnit.DataBaseTableName;

                            AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(rootOneToOneFields, rootMasterUnit);

                            AppTransDataSystemTokenBL.AssignAuntoGenerationCodeToUnitField(rootOneToOneFields, rootMasterUnit, true);



                            AppSqlCmdDto sqlCmDto = AppDbHelerBL.GetOneToOneInsertSqlCmdDto(rootOneToOneFields, dataBaseTableName, dataSourceFrom, rootMasterUnit.SchemaOwner);

                            object rootPkValue = null;

                            if (!rootMasterUnit.IsPrimaryKeyIdentityInsert)
                            {
                                rootPkValue = GetUnitPkFiledValue(rootMasterUnit, rootOneToOneFields).First().Value;
                            }

                            //	string connectInfo = AppMetaDataBL.GetConnectInfo(appTransactionExDto.DataSourceFrom );
                            // AppCacheManagerBL.GetOneDatabaseFixture(unitExdto.DataSourceFrom.Value);

                            bool IsAppSecurityRootUnit = (rootMasterUnit.DataBaseTableName.ToLowerInvariant() == "AppSecurityUser".ToLowerInvariant())
                                && rootMasterUnit.DataSourceFrom != AppCacheManagerBL.HostCompayDataBaseID;

                            if (IsAppSecurityRootUnit)
                            {
                                rootPkValue = PorcessHostMasterDB(sqlCmDto, aValidationResult);
                                if (rootPkValue == null)
                                {
                                    return false;
                                }
                            }



                            if (rootMasterUnit.IsPrimaryKeyIdentityInsert)
                            {
                                //rootPkValue = adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);
                                //databaseFixtureInstance
                                if (!IsAppSecurityRootUnit)
                                {
                                    rootPkValue = databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
                                }

                            }
                            else // for none sPrimaryKeyIdentity , no need to update the rootPkValue;
                            {
                                //	adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);

                                databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);


                            }

                            // Check Second
                            string silblngError = InsertAllSiblingUnits(databaseFixtureInstance,rootAppformDataDto, appTransactionExDto, rootPkValue, trans);
                            if (!string.IsNullOrWhiteSpace(silblngError))
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, silblngError));

                                trans.Rollback();
                                break;
                            }

                            string childError = InsertChildAndGrandUnits(databaseFixtureInstance,rootAppformDataDto, appTransactionExDto, rootPkValue, trans);

                            if (!string.IsNullOrWhiteSpace(childError))
                            {
                                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, childError));

                                trans.Rollback();
                                break;
                            }

                            //if (string.IsNullOrWhiteSpace(childError) && string.IsNullOrWhiteSpace(silblngError))
                            //{

                            //    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_OK", ValidationItemType.Message, "Saved Successfully"));


                            //}

                        }

                        trans.Commit();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, ex.Message));

                trans.Rollback();

                return false;
            }
        }

        private static object PorcessHostMasterDB(AppSqlCmdDto sqlCmDto, ValidationResult aValidationResult)
        {
            object rootPkValue;

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(AppCacheManagerBL.HostCompayDataBaseID);

            DbProviderFactory factory = databaseFixtureInstance.DbProviderFactory;

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = databaseFixtureInstance.ConnectionString;
                connection.Open();


                DbTransaction trans = null;
                try
                {
                    using (trans = connection.BeginTransaction())
                    {

                        rootPkValue = databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
                        trans.Commit();

                    }

                }
                catch (Exception ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Save_Failed", ValidationItemType.Error, ex.Message));

                    trans.Rollback();
                    rootPkValue = null;
                }

                if (rootPkValue != null)
                {
                    AppSecurityUserEntity userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserSimpleEntity(rootPkValue);
                    AppSecurityUserExDto aAppSecurityUserExDto = AppSecurityUserConverter.ConvertEntityToExDto(userEntity);

                    aAppSecurityUserExDto.GlobalGuid = System.Guid.NewGuid();
                    AppDataSourceRegisterEntity aAppDataSourceRegisterEntity =
                        AppCacheManagerBL.GetCurrentCompanyMasterDataSource(int.Parse(ServerContext.Instance.CurrentCompanyId.ToString()));


                    AppSaasAccountUserBL.CopyMasterDBUserToUserCompanyDatabaseForNoneExistUser
                        (aAppSecurityUserExDto, aAppDataSourceRegisterEntity);

                }
            }
            return rootPkValue;

        }

        internal static string InsertAllSiblingUnits(DatabaseFixture databaseFixtureInstance,AppMasterDetailDto rootAppformDataDto, AppTransactionExDto AppTransactionExDto, object rootPkValue, DbTransaction trans)
        {
            string errormessge = "";

            foreach (string siblingUnitid in rootAppformDataDto.DictSiblingOneToOneFields.Keys)
            {
                var siblingTransactionUnitExDto = AppTransactionExDto.DictAllTransactionUnitIdExDto[siblingUnitid];


                bool isReadonlyUnit = (siblingTransactionUnitExDto.IsReadOnly.HasValue && siblingTransactionUnitExDto.IsReadOnly.Value);

                if (!isReadonlyUnit)
                {
                    Dictionary<string, object> siblingUnitOneToOneFields = rootAppformDataDto.DictSiblingOneToOneFields[siblingUnitid];
                    errormessge = InsertOneSiblingUnit(databaseFixtureInstance,rootPkValue, trans, errormessge, siblingTransactionUnitExDto, siblingUnitOneToOneFields);
                }

            }

            return errormessge;
        }

        private static string InsertOneSiblingUnit(DatabaseFixture databaseFixtureInstance , object rootPkValue, DbTransaction trans, string errormessge, AppTransactionUnitExDto siblingTransactionUnitExDto, Dictionary<string, object> siblingUnitOneToOneFields)
        {


            if (!siblingTransactionUnitExDto.IsEditableUnit)
            {
                return "";
            }

            AppTransDataSystemTokenBL.AssignAuntoGenerationCodeToUnitField(siblingUnitOneToOneFields, siblingTransactionUnitExDto, true);

            PassRootKeyValueToSiblingOrChildUnit(siblingUnitOneToOneFields, rootPkValue, siblingTransactionUnitExDto);


            string dataBaseTableName = siblingTransactionUnitExDto.DataBaseTableName;


            if (!string.IsNullOrWhiteSpace(siblingTransactionUnitExDto.BaseDataBaseTableName))
            {
                dataBaseTableName = siblingTransactionUnitExDto.BaseDataBaseTableName;

            }

            var sqlDto = AppDbHelerBL.GetOneToOneInsertSqlCmdDto(siblingUnitOneToOneFields, dataBaseTableName, siblingTransactionUnitExDto.DataSourceFrom, siblingTransactionUnitExDto.SchemaOwner);

            string InsertChildUnit = sqlDto.CmdText;
            List<DbParameter> listChildtParamters = sqlDto.ListParamters;



            try
            {

                //adpater.ExecuteScalarQuery (InsertChildUnit, listChildtParamters);

                databaseFixtureInstance.ExecuteTransScalar(InsertChildUnit, listChildtParamters, trans);


            }
            catch (Exception ex)
            {
                errormessge = errormessge + System.Environment.NewLine + ex.ToString();
            }

            return errormessge;
        }

        internal static void PassRootKeyValueToSiblingOrChildUnit(Dictionary<string, object> unitOneToOneFields, object rootPkValue, AppTransactionUnitExDto childOrsiblingUnitExDto)
        {
            if (childOrsiblingUnitExDto.IsEditableUnit)
            {
                AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(unitOneToOneFields, childOrsiblingUnitExDto);
                // Assing parent key to sibling foreign key
                string linkToParentPrimaryKeyDbfield = childOrsiblingUnitExDto.DictLinkToParentKeyDbfield.First().Key;
                unitOneToOneFields[linkToParentPrimaryKeyDbfield] = rootPkValue;
            }


        }

        internal static void UpdateOneUnitRecord(DatabaseFixture databaseFixtureInstance,Dictionary<string, object> dictOneToOneFields, AppTransactionUnitExDto childTransactionUnitExDto, DbTransaction trans)
        {
            DatabaseTable childDatabaseTable = AppCacheManagerBL.GetDatabaseTable(childTransactionUnitExDto);

            AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(dictOneToOneFields, childTransactionUnitExDto);

            var sqlDto = AppDbHelerBL.GetOneToOneUpdateWithPrimaryKeyValueSqlCmdDto(dictOneToOneFields, childTransactionUnitExDto);
            string updateChildUnit = sqlDto.CmdText;
            List<DbParameter> listChildtParamters = sqlDto.ListParamters;

            databaseFixtureInstance.ExecuteTransScalar(updateChildUnit, listChildtParamters, trans);
            //adpater.ExecuteScalarQuery(updateChildUnit, listChildtParamters);
        }

        internal static string InsertOneUnitChild(DatabaseFixture databaseFixtureInstance, AppTransactionUnitExDto childTransactionUnitExDto, Dictionary<string, object> childOneToOneFields, DbTransaction trans)
        {
            AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(childOneToOneFields, childTransactionUnitExDto);


            string dataBaseTableName = childTransactionUnitExDto.DataBaseTableName;


            if (!string.IsNullOrWhiteSpace(childTransactionUnitExDto.BaseDataBaseTableName))
            {
                dataBaseTableName = childTransactionUnitExDto.BaseDataBaseTableName;

            }

            var sqlCmDto = AppDbHelerBL.GetOneToOneInsertSqlCmdDto(childOneToOneFields, dataBaseTableName, childTransactionUnitExDto.DataSourceFrom, childTransactionUnitExDto.SchemaOwner);
            try
            {
                //var childPkValue = adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);
                var childPkValue = databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        //private static string InsertGrandChild(AppTransactionExDto AppTransactionExDto, DataAccessAdapter adpater, AppChildDataDto appformChildDataDto, string grandchildUnitid)
        //InsertGrandChild(adpater, appformChildDataDto, grandChildTransactionUnitExDto, grandchildAppformChildDataDtoList);
        private static string InsertGrandChild(DatabaseFixture databaseFixtureInstance, DbTransaction trans, AppChildDataDto appformChildDataDto, AppTransactionUnitExDto grandChildTransactionUnitExDto, List<Dictionary<string, object>> grandchildAppformChildDataDtoList)
        {


            if (!grandChildTransactionUnitExDto.IsEditableUnit)
            {
                return "";
            }

            string erroinsert = "";
            foreach (Dictionary<string, object> gradnChildDataDto in grandchildAppformChildDataDtoList)
            {
                var childDictOneToOneFields = appformChildDataDto.DictOneToOneFields;

                PassChildLinkKeyValueToGrandChild(grandChildTransactionUnitExDto, gradnChildDataDto, childDictOneToOneFields);

                erroinsert = erroinsert + InsertOneUnitChild(databaseFixtureInstance,grandChildTransactionUnitExDto, gradnChildDataDto, trans);
            }

            return erroinsert;
        }

        private static void PassChildLinkKeyValueToGrandChild(AppTransactionUnitExDto grandChildTransactionUnitExDto, Dictionary<string, object> gradnChildDataDto, Dictionary<string, object> childDictOneToOneFields)
        {
            foreach (string fkFiekd in grandChildTransactionUnitExDto.DictLinkToParentKeyDbfield.Keys)
            {
                string parentPrimary = grandChildTransactionUnitExDto.DictLinkToParentKeyDbfield[fkFiekd];
                gradnChildDataDto[fkFiekd] = childDictOneToOneFields[parentPrimary];
            }
        }

        private static string InsertChildAndGrandUnits(DatabaseFixture databaseFixtureInstance,AppMasterDetailDto rootAppformDataDto, AppTransactionExDto appTransactionExDto, object rootPkValue, DbTransaction trans)
        {
            string errorMessage = "";
            foreach (string childUnitid in rootAppformDataDto.DictOneToManyFields.Keys)
            {
                errorMessage = errorMessage +
                    InsertOneChildUnit(databaseFixtureInstance,rootAppformDataDto, appTransactionExDto, rootPkValue, childUnitid, trans);
            }

            return errorMessage;
        }

        private static string InsertOneChildUnit(DatabaseFixture databaseFixtureInstance,AppMasterDetailDto rootAppformDataDto, AppTransactionExDto AppTransactionExDto, object rootPkValue, string childUnitid, DbTransaction trans)
        {
            string errorMessage = "";
            List<AppChildDataDto> childAppformChildDataDtoList = rootAppformDataDto.DictOneToManyFields[childUnitid];
            var childTransactionUnitExDto = AppTransactionExDto.DictAllTransactionUnitIdExDto[childUnitid];

            foreach (AppChildDataDto appformChildDataDto in childAppformChildDataDtoList)
            {
                errorMessage = errorMessage +
                    InsertAppChildDataDto(databaseFixtureInstance,AppTransactionExDto, rootPkValue, trans, childTransactionUnitExDto, appformChildDataDto);
            }

            return errorMessage;
        }

        internal static string InsertAppChildDataDto(DatabaseFixture databaseFixtureInstance, AppTransactionExDto AppTransactionExDto, object rootPkValue, DbTransaction trans, AppTransactionUnitExDto childTransactionUnitExDto, AppChildDataDto appformChildDataDto)
        {
            string KeyFieldErrormessge = "";


            if (!childTransactionUnitExDto.IsEditableUnit)
            {
                return "";
            }

            Dictionary<string, object> childOneToOneFields = appformChildDataDto.DictOneToOneFields;

            AppTransDataSystemTokenBL.AssignSystemTokenValueToUnitField(childOneToOneFields, childTransactionUnitExDto);

            // for list Edit  rootPkValue always is empty
            if (rootPkValue != null)
            {
                PassRootKeyValueToSiblingOrChildUnit(childOneToOneFields, rootPkValue, childTransactionUnitExDto);
            }

            string dataBaseTableName = childTransactionUnitExDto.DataBaseTableName;


            if (!string.IsNullOrWhiteSpace(childTransactionUnitExDto.BaseDataBaseTableName))
            {
                dataBaseTableName = childTransactionUnitExDto.BaseDataBaseTableName;

            }

            var sqlCmDto = AppDbHelerBL.GetOneToOneInsertSqlCmdDto(childOneToOneFields, dataBaseTableName, childTransactionUnitExDto.DataSourceFrom, childTransactionUnitExDto.SchemaOwner);

            // need to update childUnits PK fiked, it will pass to grand child for new insert
            if (childTransactionUnitExDto.IsPrimaryKeyIdentityInsert)
            {
                string childPrimaryKeyDbfielname = childTransactionUnitExDto.PrimaryKeyDbfieldList.First();
                //object autoInsertPkChildPkValue = adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);
                object autoInsertPkChildPkValue = databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
                // need to update auot
                childOneToOneFields[childPrimaryKeyDbfielname] = autoInsertPkChildPkValue;
            }
            else
            {
                //adpater.ExecuteScalarQuery(sqlCmDto.CmdText, sqlCmDto.ListParamters);

                databaseFixtureInstance.ExecuteTransScalar(sqlCmDto.CmdText, sqlCmDto.ListParamters, trans);
            }

            if (!appformChildDataDto.DictOneToManyFields.IsEmpty())
            {
                foreach (string grandchildUnitid in appformChildDataDto.DictOneToManyFields.Keys)
                {
                    var grandChildTransactionUnitExDto = AppTransactionExDto.DictAllTransactionUnitIdExDto[grandchildUnitid];

                    List<AppChildDataDto> listGridnChildRow = appformChildDataDto.DictOneToManyFields[grandchildUnitid];
                    List<Dictionary<string, object>> grandchildAppformChildDataDtoList = listGridnChildRow.Select(o => o.DictOneToOneFields).ToList();

                    InsertGrandChild(databaseFixtureInstance,trans, appformChildDataDto, grandChildTransactionUnitExDto, grandchildAppformChildDataDtoList);
                }
            }

            return KeyFieldErrormessge;
        }

        private static Dictionary<string, object> CloneRootObejctValue(Dictionary<string, object> RootOneToOneFields)
        {
            Dictionary<string, object> OrginalRootOneToOneFields = new Dictionary<string, object>();
            foreach (string key in RootOneToOneFields.Keys)
            {
                OrginalRootOneToOneFields.Add(key, RootOneToOneFields[key]);
            }

            return OrginalRootOneToOneFields;
        }

        internal static Dictionary<string, object> GetUnitPkFiledValue(AppTransactionUnitExDto transactionUnitExDto, Dictionary<string, object> rootOneToOneFields)
        {
            Dictionary<string, object> dictRootUnitPkValue = new Dictionary<string, object>();

            if (transactionUnitExDto != null)
            {
                foreach (string pkFied in transactionUnitExDto.PrimaryKeyDbfieldList)
                {
                    dictRootUnitPkValue[pkFied] = rootOneToOneFields[pkFied];
                }
            }

            return dictRootUnitPkValue;
        }

        internal static string ValidaNoneIdenityInsertPkValue(AppTransactionUnitExDto oneUnit, Dictionary<string, object> rootOneToOneFields)
        {
            string keyFieldErrormessge = "";
            foreach (string pkFied in oneUnit.PrimaryKeyDbfieldList)
            {
                object pkValue = rootOneToOneFields[pkFied];
                if (pkValue == null || string.IsNullOrWhiteSpace(pkValue.ToString()))
                {
                    keyFieldErrormessge = keyFieldErrormessge + string.Format("{0} is empty", pkFied);
                }
            }

            return keyFieldErrormessge;
        }

        private static void SaveAsOneUnitOneRow(Dictionary<int, List<AppTransactionSaveAsMappingExDto>> dictUnitMapping, AppTransactionUnitExDto rootMasterUnit, Dictionary<string, object> RootOneToOneFields)
        {
            Dictionary<int, string> dictIDbfieldname = rootMasterUnit.AppTransactionFieldList.ToDictionary(o => (int)o.Id, o => o.DataBaseFieldName);

            if (dictUnitMapping.ContainsKey((int)rootMasterUnit.Id))
            {
                List<AppTransactionSaveAsMappingExDto> unitListmapping = dictUnitMapping[(int)rootMasterUnit.Id];

                foreach (var mapping in unitListmapping)
                {
                    if (mapping.SourceFiledId.HasValue)
                    {
                        // needto blank this field
                        if (mapping.IsBlankTargetField.HasValue && mapping.IsBlankTargetField.Value)
                        {
                            string blankFieldName = dictIDbfieldname[mapping.SourceFiledId.Value];

                            RootOneToOneFields[blankFieldName] = null;
                        }
                        else if (mapping.TargetFiledId.HasValue)
                        {
                            string sourceFieldName = dictIDbfieldname[mapping.SourceFiledId.Value];
                            string targetFieldName = dictIDbfieldname[mapping.TargetFiledId.Value];

                            RootOneToOneFields[targetFieldName] = RootOneToOneFields[sourceFieldName];
                        }
                    }
                }
            }
        }


        private static void ApplyCalendarFormRepeatSetting(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto)
        {
            var repeatSetting = rootClientAppformDataDto.CalendarRepeatSetting;

            if (repeatSetting != null)
            {
                if (repeatSetting.RepeatSimpleSettingValue.HasValue)
                {
                    if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.DoesNotRepeat)
                    {
                        repeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Day;
                        repeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.EndAfterNumberOfOccurrences;
                        repeatSetting.RepeatEndAfterNbOccurrences = 1;
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                    else if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.Daily)
                    {
                        repeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Day;
                        repeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.NeverEnd;
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                    else if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.WeeklyOnThisWeekday)
                    {
                        repeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Week;
                        repeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.NeverEnd;
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                    else if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.MonthlyOnThisWeekAndWeekDay)
                    {
                        repeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Month;
                        repeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.NeverEnd;
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                    else if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.AnnuallyOnThisDate)
                    {
                        repeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Year;
                        repeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.NeverEnd;
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                    else if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.EveryWeekdayMondayToFriday)
                    {
                        repeatSetting.RepeatTimeUnit = (int)EmAppCalendarRepeatTimeUnit.Day;
                        repeatSetting.IsWeekdayOnly = true;
                        repeatSetting.RepeatEndType = (int)EmAppCalendarRepeatEndType.NeverEnd;
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                    else if (repeatSetting.RepeatSimpleSettingValue.Value == (int)EmAppCalendarRepeatSimpleSetting.Custom)
                    {
                        ApplyCalendarFormRepeatCustomSetting(transactionId, rootPkValue, rootClientAppformDataDto, repeatSetting);
                    }
                }
            }
        }

        private static void ApplyCalendarFormRepeatCustomSetting(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, CalendarRepeatSettingDto repeatSetting)
        {
            int? timeUnit = repeatSetting.RepeatTimeUnit;
            int? endType = repeatSetting.RepeatEndType;
            DateTime? endDate = repeatSetting.RepeatEndDate;
            int? nbOccurrencesPerTimeUnit = repeatSetting.NbOccurrencesPerTimeUnit;
            int? nbOccurrences = repeatSetting.RepeatEndAfterNbOccurrences;
            List<int> selectedWeekdays = repeatSetting.RepeatWeeklySelectedWeekdays;
            int? repeatMonthlySettingValue = repeatSetting.RepeatMonthlySettingValue;
            bool isWeekdayOnly = repeatSetting.IsWeekdayOnly;

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            var repeatTokenField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarRepeatToken);
            var startDateTimeField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarEventStartDateTime);
            var endDateTimeField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarEventEndDateTime);



            DateTime? eventStartDateTime = null;
            DateTime? eventEndDateTime = null;
            string repeatToken = string.Empty;

            if (!timeUnit.HasValue)
            {
                return;
            }

            else if (timeUnit.Value == (int)EmAppCalendarRepeatTimeUnit.Month && !repeatMonthlySettingValue.HasValue)
            {
                repeatMonthlySettingValue = 1;
            }

            if (!endType.HasValue)
            {
                return;
            }
            else if (endType.Value == (int)EmAppCalendarRepeatEndType.EndOnDate && !endDate.HasValue)
            {
                if (endDate.HasValue)
                {
                    endDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(endDate.Value);
                }
                else
                {
                    return;
                }

            }
            else if (endType.Value == (int)EmAppCalendarRepeatEndType.EndAfterNumberOfOccurrences && !nbOccurrences.HasValue)
            {
                return;
            }

            if (!nbOccurrencesPerTimeUnit.HasValue || nbOccurrencesPerTimeUnit.Value == 0)
            {
                return;
            }

            if (repeatTokenField == null)
            {
                return;
            }
            else
            {
                repeatToken = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(rootClientAppformDataDto.DictOneToOneFields[repeatTokenField.DataBaseFieldName]);
            }

            if (startDateTimeField == null || endDateTimeField == null)
            {
                return;
            }
            else
            {
                eventStartDateTime = ControlTypeValueConverter.ConvertValueToDate(rootClientAppformDataDto.DictOneToOneFields[startDateTimeField.DataBaseFieldName]);
                eventEndDateTime = ControlTypeValueConverter.ConvertValueToDate(rootClientAppformDataDto.DictOneToOneFields[endDateTimeField.DataBaseFieldName]);

                if (eventStartDateTime.HasValue && eventEndDateTime.HasValue)
                {
                    eventStartDateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(eventStartDateTime.Value);
                    eventEndDateTime = ClientTimeZoneHelper.ConvertUTCToClientDateTime(eventEndDateTime.Value);
                }
                else
                {
                    return;
                }
            }



            if (timeUnit.Value == (int)EmAppCalendarRepeatTimeUnit.Week && selectedWeekdays.IsEmpty())
            {
                selectedWeekdays = new List<int>();
                selectedWeekdays.Add((int)eventStartDateTime.Value.DayOfWeek);
            }

            string newTokenValue = Guid.NewGuid().ToString();

            if (endType.Value == (int)EmAppCalendarRepeatEndType.NeverEnd)
            {
                endType = (int)EmAppCalendarRepeatEndType.EndOnDate;
                endDate = eventStartDateTime.Value.Date.AddYears(1);
                //endDate = eventStartDateTime.Value.Date.AddDays(5);
            }

            if (endType.Value == (int)EmAppCalendarRepeatEndType.EndOnDate)
            {
                if (endDate.HasValue)
                {
                    if (rootClientAppformDataDto.IsNew)
                    {
                        ApplyCalendarFormRepeatCustomSettingByEndDate_NewEvent(transactionId, rootPkValue, rootClientAppformDataDto, timeUnit, nbOccurrencesPerTimeUnit.Value, endDate, selectedWeekdays, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, isWeekdayOnly);
                    }
                    else
                    {
                        EmAppCalendarRepeatSettingApplyToRange applyToRange = (EmAppCalendarRepeatSettingApplyToRange)rootClientAppformDataDto.CalendarRepeatSettingApplyToRange.Value;
                        if (applyToRange == EmAppCalendarRepeatSettingApplyToRange.ThisEvent)
                        {
                            return;
                        }
                        else if (applyToRange == EmAppCalendarRepeatSettingApplyToRange.AllEvents)
                        {
                            ApplyCalendarFormRepeatCustomSettingByEndDate_AllExistEvent(transactionId, rootPkValue, rootClientAppformDataDto, timeUnit, nbOccurrencesPerTimeUnit.Value, endDate, selectedWeekdays, transactionExDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, repeatToken, newTokenValue, isWeekdayOnly);
                        }
                        else if (applyToRange == EmAppCalendarRepeatSettingApplyToRange.ThisAndFollowingEvents)
                        {
                            ApplyCalendarFormRepeatCustomSettingByEndDate_ExistFollowingEvent(transactionId, rootPkValue, rootClientAppformDataDto, timeUnit, nbOccurrencesPerTimeUnit.Value, endDate, selectedWeekdays, transactionExDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, repeatToken, newTokenValue, isWeekdayOnly);
                        }
                    }
                }
            }
            else if (endType.Value == (int)EmAppCalendarRepeatEndType.EndAfterNumberOfOccurrences)
            {
                if (!nbOccurrences.HasValue)
                {
                    return;
                }
                else
                {
                    if (rootClientAppformDataDto.IsNew)
                    {
                        ApplyCalendarFormRepeatCustomSettingByNbRepeat_NewEvent(transactionId, rootPkValue, rootClientAppformDataDto, timeUnit, nbOccurrencesPerTimeUnit.Value, endDate, selectedWeekdays, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, nbOccurrences.Value, isWeekdayOnly);
                    }
                    else
                    {
                        EmAppCalendarRepeatSettingApplyToRange applyToRange = (EmAppCalendarRepeatSettingApplyToRange)rootClientAppformDataDto.CalendarRepeatSettingApplyToRange.Value;
                        if (applyToRange == EmAppCalendarRepeatSettingApplyToRange.ThisEvent)
                        {
                            return;
                        }
                        else if (applyToRange == EmAppCalendarRepeatSettingApplyToRange.AllEvents)
                        {
                            ApplyCalendarFormRepeatCustomSettingByNbRepeat_AllExistEvent(transactionId, rootPkValue, rootClientAppformDataDto, timeUnit, nbOccurrencesPerTimeUnit.Value, endDate, selectedWeekdays, transactionExDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, repeatToken, newTokenValue, nbOccurrences.Value, isWeekdayOnly);
                        }
                        else if (applyToRange == EmAppCalendarRepeatSettingApplyToRange.ThisAndFollowingEvents)
                        {
                            ApplyCalendarFormRepeatCustomSettingByNbRepeat_ExistFollowingEvent(transactionId, rootPkValue, rootClientAppformDataDto, timeUnit, nbOccurrencesPerTimeUnit.Value, endDate, selectedWeekdays, transactionExDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, repeatToken, newTokenValue, nbOccurrences.Value, isWeekdayOnly);
                        }
                    }
                }
            }
        }

        private static void ApplyCalendarFormRepeatCustomSettingByEndDate_ExistFollowingEvent(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, int? timeUnit, int nbOccurrencesPerTimeUnit, DateTime? endDate, List<int> selectedWeekdays, AppTransactionExDto transactionExDto, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string repeatToken, string newTokenValue, bool isWeekdayOnly)
        {
            Dictionary<string, DateTime?> dictPKValueAndEventDate = GetFormPKValueAndEventStartDateByCalendarRepeatToken(transactionExDto, repeatToken);

            if (dictPKValueAndEventDate.Count == 0)
            {
                DateTime? currentEventStartDateTime = ControlTypeValueConverter.ConvertValueToDate(rootClientAppformDataDto.DictOneToOneFields[startDateTimeField.DataBaseFieldName]);
                dictPKValueAndEventDate.Add(rootPkValue.ToString(), currentEventStartDateTime);
            }

            //foreach (string pkValue in dictPKValueAndEventDate.Keys)
            //{
            //    DateTime? eventDate = dictPKValueAndEventDate[pkValue];
            //    if (eventDate.HasValue && eventDate.Value >= eventStartDateTime.Value.Date)
            //    {
            //        AppTransactionDataDeleteBL.DeleteTransactionData(transactionId, pkValue);
            //    }
            //}

            List<object> pkValueList = new List<object>();

            foreach (string pkValue in dictPKValueAndEventDate.Keys)
            {
                DateTime? eventDate = dictPKValueAndEventDate[pkValue];
                if (eventDate.HasValue && eventDate.Value >= eventStartDateTime.Value.Date)
                {
                    pkValueList.Add(pkValue);
                }

            }

            AppTransactionDataDeleteBL.DeleteMultipleMasterDetailFormData(transactionId, pkValueList);

            List<DateTime> repeatDateList = new List<DateTime>();
            DateTime cursorDate = eventStartDateTime.Value.Date;

            while (cursorDate <= endDate.Value.Date)
            {
                cursorDate = ProcessCalendarRepeatEventDateList(timeUnit, nbOccurrencesPerTimeUnit, selectedWeekdays, repeatDateList, cursorDate, isWeekdayOnly);
            }

            SaveCalendarRepeatEventFromDateList(transactionId, rootPkValue, rootClientAppformDataDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, repeatDateList, false);
        }

        private static void ApplyCalendarFormRepeatCustomSettingByEndDate_AllExistEvent(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, int? timeUnit, int nbOccurrencesPerTimeUnit, DateTime? endDate, List<int> selectedWeekdays, AppTransactionExDto transactionExDto, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string repeatToken, string newTokenValue, bool isWeekdayOnly)
        {
            Dictionary<string, DateTime?> dictPKValueAndEventDate = GetFormPKValueAndEventStartDateByCalendarRepeatToken(transactionExDto, repeatToken);

            if (dictPKValueAndEventDate.Count == 0)
            {
                DateTime? currentEventStartDateTime = ControlTypeValueConverter.ConvertValueToDate(rootClientAppformDataDto.DictOneToOneFields[startDateTimeField.DataBaseFieldName]);
                dictPKValueAndEventDate.Add(rootPkValue.ToString(), currentEventStartDateTime);
            }

            //foreach (string pkValue in dictPKValueAndEventDate.Keys)
            //{
            //    AppTransactionDataDeleteBL.DeleteTransactionData(transactionId, pkValue);
            //}

            List<object> pkValueList = new List<object>();

            foreach (string pkValue in dictPKValueAndEventDate.Keys)
            {

                pkValueList.Add(pkValue);


            }

            AppTransactionDataDeleteBL.DeleteMultipleMasterDetailFormData(transactionId, pkValueList);


            List<DateTime> repeatDateList = new List<DateTime>();
            DateTime cursorDate = dictPKValueAndEventDate.Values.Where(o => o.HasValue).Min(o => o.Value);
            while (cursorDate <= endDate.Value.Date)
            {
                cursorDate = ProcessCalendarRepeatEventDateList(timeUnit, nbOccurrencesPerTimeUnit, selectedWeekdays, repeatDateList, cursorDate, isWeekdayOnly);
            }
            SaveCalendarRepeatEventFromDateList(transactionId, rootPkValue, rootClientAppformDataDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, repeatDateList, false);
        }

        private static void ApplyCalendarFormRepeatCustomSettingByEndDate_NewEvent(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, int? timeUnit, int nbOccurrencesPerTimeUnit, DateTime? endDate, List<int> selectedWeekdays, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string newTokenValue, bool isWeekdayOnly)
        {
            List<DateTime> repeatDateList = new List<DateTime>();
            DateTime cursorDate = eventStartDateTime.Value.Date;
            while (cursorDate <= endDate.Value.Date)
            {
                cursorDate = ProcessCalendarRepeatEventDateList(timeUnit, nbOccurrencesPerTimeUnit, selectedWeekdays, repeatDateList, cursorDate, isWeekdayOnly);
            }
            UpdateCurrentEventToken(transactionId, rootPkValue, repeatTokenField, newTokenValue);
            SaveCalendarRepeatEventFromDateList(transactionId, rootPkValue, rootClientAppformDataDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, repeatDateList, true);
        }






        private static void ApplyCalendarFormRepeatCustomSettingByNbRepeat_ExistFollowingEvent(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, int? timeUnit, int nbOccurrencesPerTimeUnit, DateTime? endDate, List<int> selectedWeekdays, AppTransactionExDto transactionExDto, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string repeatToken, string newTokenValue, int nbOccurrences, bool isWeekdayOnly)
        {
            Dictionary<string, DateTime?> dictPKValueAndEventDate = GetFormPKValueAndEventStartDateByCalendarRepeatToken(transactionExDto, repeatToken);

            if (dictPKValueAndEventDate.Count == 0)
            {
                DateTime? currentEventStartDateTime = ControlTypeValueConverter.ConvertValueToDate(rootClientAppformDataDto.DictOneToOneFields[startDateTimeField.DataBaseFieldName]);
                dictPKValueAndEventDate.Add(rootPkValue.ToString(), currentEventStartDateTime);
            }

            //foreach (string pkValue in dictPKValueAndEventDate.Keys)
            //{
            //    DateTime? eventDate = dictPKValueAndEventDate[pkValue];
            //    if (eventDate.HasValue && eventDate.Value >= eventStartDateTime.Value.Date)
            //    {
            //        AppTransactionDataDeleteBL.DeleteTransactionData(transactionId, pkValue);
            //    }
            //}

            List<object> pkValueList = new List<object>();

            foreach (string pkValue in dictPKValueAndEventDate.Keys)
            {
                DateTime? eventDate = dictPKValueAndEventDate[pkValue];
                if (eventDate.HasValue && eventDate.Value >= eventStartDateTime.Value.Date)
                {
                    pkValueList.Add(pkValue);
                }

            }

            AppTransactionDataDeleteBL.DeleteMultipleMasterDetailFormData(transactionId, pkValueList);



            List<DateTime> repeatDateList = new List<DateTime>();
            DateTime cursorDate = eventStartDateTime.Value.Date;

            int repeatCount = 0;
            while (repeatDateList.Count < nbOccurrences)
            {
                cursorDate = ProcessCalendarRepeatEventDateList(timeUnit, nbOccurrencesPerTimeUnit, selectedWeekdays, repeatDateList, cursorDate, isWeekdayOnly);
                repeatCount++;
            }

            SaveCalendarRepeatEventFromDateList(transactionId, rootPkValue, rootClientAppformDataDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, repeatDateList, false);
        }

        private static void ApplyCalendarFormRepeatCustomSettingByNbRepeat_AllExistEvent(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, int? timeUnit, int nbOccurrencesPerTimeUnit, DateTime? endDate, List<int> selectedWeekdays, AppTransactionExDto transactionExDto, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string repeatToken, string newTokenValue, int nbOccurrences, bool isWeekdayOnly)
        {
            Dictionary<string, DateTime?> dictPKValueAndEventDate = GetFormPKValueAndEventStartDateByCalendarRepeatToken(transactionExDto, repeatToken);

            if (dictPKValueAndEventDate.Count == 0)
            {
                DateTime? currentEventStartDate = null;

                DateTime? currentEventStartDateTime = ControlTypeValueConverter.ConvertValueToDate(rootClientAppformDataDto.DictOneToOneFields[startDateTimeField.DataBaseFieldName]);
                if (currentEventStartDateTime.HasValue)
                {

                    currentEventStartDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(currentEventStartDateTime.Value).Date;
                }

                dictPKValueAndEventDate.Add(rootPkValue.ToString(), currentEventStartDate);
            }

            //foreach (string pkValue in dictPKValueAndEventDate.Keys)
            //{
            //    AppTransactionDataDeleteBL.DeleteTransactionData(transactionId, pkValue);
            //}


            List<object> pkValueList = new List<object>();

            foreach (string pkValue in dictPKValueAndEventDate.Keys)
            {
                pkValueList.Add(pkValue);
            }

            AppTransactionDataDeleteBL.DeleteMultipleMasterDetailFormData(transactionId, pkValueList);



            List<DateTime> repeatDateList = new List<DateTime>();
            DateTime cursorDate = dictPKValueAndEventDate.Values.Where(o => o.HasValue).Min(o => o.Value);

            int repeatCount = 0;
            while (repeatDateList.Count < nbOccurrences)
            {
                cursorDate = ProcessCalendarRepeatEventDateList(timeUnit, nbOccurrencesPerTimeUnit, selectedWeekdays, repeatDateList, cursorDate, isWeekdayOnly);
                repeatCount++;
            }

            SaveCalendarRepeatEventFromDateList(transactionId, rootPkValue, rootClientAppformDataDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, repeatDateList, false);
        }

        private static void ApplyCalendarFormRepeatCustomSettingByNbRepeat_NewEvent(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, int? timeUnit, int nbOccurrencesPerTimeUnit, DateTime? endDate, List<int> selectedWeekdays, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string newTokenValue, int nbOccurrences, bool isWeekdayOnly)
        {
            List<DateTime> repeatDateList = new List<DateTime>();
            DateTime cursorDate = eventStartDateTime.Value.Date;

            int repeatCount = 0;
            while (repeatDateList.Count < nbOccurrences)
            {
                cursorDate = ProcessCalendarRepeatEventDateList(timeUnit, nbOccurrencesPerTimeUnit, selectedWeekdays, repeatDateList, cursorDate, isWeekdayOnly);
                repeatCount++;
            }
            UpdateCurrentEventToken(transactionId, rootPkValue, repeatTokenField, newTokenValue);
            SaveCalendarRepeatEventFromDateList(transactionId, rootPkValue, rootClientAppformDataDto, repeatTokenField, startDateTimeField, endDateTimeField, eventStartDateTime, eventEndDateTime, newTokenValue, repeatDateList, true);
        }











        private static void SaveCalendarRepeatEventFromDateList(int transactionId, object rootPkValue, AppMasterDetailDto rootClientAppformDataDto, AppTransactionFieldExDto repeatTokenField, AppTransactionFieldExDto startDateTimeField, AppTransactionFieldExDto endDateTimeField, DateTime? eventStartDateTime, DateTime? eventEndDateTime, string newTokenValue, List<DateTime> repeatDateList, bool isIgnoreCurrentEventDate)
        {

            var transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            ValidationResult aValidationResult = new ValidationResult();

            List<AppMasterDetailDto> needToInsertFormData = new List<AppMasterDetailDto>();

            foreach (DateTime eventDate in repeatDateList)
            {
                if (isIgnoreCurrentEventDate && eventDate == eventStartDateTime.Value.Date)
                {
                    continue;
                }
                else
                {
                    var needToSaveFormData = rootClientAppformDataDto.DeepCopy();

                    needToSaveFormData.IsDirty = true;
                    needToSaveFormData.IsNew = true;
                    double dayDiff = (eventDate.Date - eventStartDateTime.Value.Date).TotalDays;

                    needToSaveFormData.DictOneToOneFields[startDateTimeField.DataBaseFieldName] = ClientTimeZoneHelper.ConvertClientToUTCDateTime(eventStartDateTime.Value.AddDays(dayDiff));
                    needToSaveFormData.DictOneToOneFields[endDateTimeField.DataBaseFieldName] = ClientTimeZoneHelper.ConvertClientToUTCDateTime(eventEndDateTime.Value.AddDays(dayDiff));

                    needToSaveFormData.DictOneToOneFields[repeatTokenField.DataBaseFieldName] = newTokenValue;
                    needToSaveFormData.IsIgnoreCalendarRepeat = true;

                    needToInsertFormData.Add(needToSaveFormData);
                }
            }

            InsertMultipleMasterDetailData(needToInsertFormData, transactionExDto, aValidationResult);
        }

        private static void UpdateCurrentEventToken(int transactionId, object rootPkValue, AppTransactionFieldExDto repeatTokenField, string newTokenValue)
        {
            AppMasterDetailDto orgFormData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, rootPkValue);
            orgFormData.DictOneToOneFields[repeatTokenField.DataBaseFieldName] = newTokenValue;
            orgFormData.IsDirty = true;
            orgFormData.IsIgnoreCalendarRepeat = true;
            SaveTransactionData(orgFormData);
        }

        private static DateTime ProcessCalendarRepeatEventDateList(int? timeUnit, int nbOccurrencesPerTimeUnit, List<int> selectedWeekdays, List<DateTime> repeatDateList, DateTime cursorDate, bool isWeekdayOnly)
        {
            if (timeUnit.Value == (int)EmAppCalendarRepeatTimeUnit.Day)
            {
                repeatDateList.Add(cursorDate);

                cursorDate = cursorDate.AddDays(1 * nbOccurrencesPerTimeUnit);
            }
            else if (timeUnit.Value == (int)EmAppCalendarRepeatTimeUnit.Week)
            {
                if (selectedWeekdays.Contains((int)cursorDate.DayOfWeek))
                {
                    repeatDateList.Add(cursorDate);
                }

                cursorDate = cursorDate.AddDays(1 * nbOccurrencesPerTimeUnit);
            }
            else if (timeUnit.Value == (int)EmAppCalendarRepeatTimeUnit.Month)
            {
                repeatDateList.Add(cursorDate);

                cursorDate = cursorDate.AddMonths(1 * nbOccurrencesPerTimeUnit);
            }
            else if (timeUnit.Value == (int)EmAppCalendarRepeatTimeUnit.Year)
            {
                repeatDateList.Add(cursorDate);

                cursorDate = cursorDate.AddYears(1 * nbOccurrencesPerTimeUnit);
            }

            return cursorDate;
        }

        private static Dictionary<string, DateTime?> GetFormPKValueAndEventStartDateByCalendarRepeatToken(AppTransactionExDto transactionExDto, string repeatToken)
        {
            Dictionary<string, DateTime?> dictPKValueAndEventDate = new Dictionary<string, DateTime?>();


            var repeatTokenField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarRepeatToken);
            var startDateTimeField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarEventStartDateTime);
            //var endDateTimeField = transactionExDto.RootMasterUnit.AppTransactionFieldList.FirstOrDefault(o => o.MappingEmSystemTokenField.HasValue && o.MappingEmSystemTokenField.Value == (int)EmBLFiledMappingSystemTokenField.CalendarEventEndDateTime);


            if (repeatTokenField != null && startDateTimeField != null)
            {
                string tableName = transactionExDto.RootMasterUnit.DataBaseTableName;

                string pkFieldName = transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.First();
                string startDateTimeFieldName = startDateTimeField.DataBaseFieldName;
                string tokenFieldName = repeatTokenField.DataBaseFieldName;

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.DataSourceFrom.Value);

                List<DbParameter> sqlParamters = new List<DbParameter>();


                string query = string.Format("SELECT {0}, {1} FROM {2} WHERE {3} = '{4}'", pkFieldName, startDateTimeFieldName, tableName, tokenFieldName, repeatToken);

                DataTable dt = databaseFixtureInstance.RetriveDataTable(query, sqlParamters);


                foreach (DataRow dataRow in dt.Rows)
                {
                    string pkValue = dataRow[pkFieldName].ToString();
                    DateTime? eventDate = ControlTypeValueConverter.ConvertValueToDate(dataRow[startDateTimeFieldName]);

                    if (eventDate.HasValue)
                    {
                        eventDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(eventDate.Value).Date;
                    }

                    if (!dictPKValueAndEventDate.ContainsKey(pkValue))
                    {
                        dictPKValueAndEventDate.Add(pkValue, eventDate);
                    }

                }
            }

            return dictPKValueAndEventDate;
        }

    }
}