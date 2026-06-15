using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;
//using APP.Persistence.Common;
using System.Data.Common;


using APP.Framework;
namespace App.BL
{
    public static class AppTransactionDataDeleteBL
    {

        internal static void SetupDictAppTransactionUnitDeleteListByTransactionId(AppTransactionExDto aAppTransactionExDto)
        {

            Dictionary<string, List<AppTransactionUnitDeleteFlowExDto>> deleteList = new Dictionary<string, List<AppTransactionUnitDeleteFlowExDto>>();
            Dictionary<string, AppTransactionUnitDeleteFlowExDto> validationList = new Dictionary<string, AppTransactionUnitDeleteFlowExDto>();

            foreach (string unitId in aAppTransactionExDto.DictAllTransactionUnitIdExDto.Keys)
            {
                SetupOneUnitDeleteAndValidtion(deleteList, validationList, unitId);


            }

            aAppTransactionExDto.DictUnitValidationDeleteList = validationList;
            aAppTransactionExDto.DictUnitDeleteList = deleteList;



        }

        private static void SetupOneUnitDeleteAndValidtion(Dictionary<string, List<AppTransactionUnitDeleteFlowExDto>> deleteList, Dictionary<string, AppTransactionUnitDeleteFlowExDto> validationList, string unitId)
        {
            var listallDeleteFlow = AppTransactionUnitDeleteFlowBL.RetrieveDeleteFlowListByTransactionUnitId(unitId).OrderBy(o => o.DeleteFlowPriority);



            var listDeleteFlow = listallDeleteFlow.Where(o => string.IsNullOrWhiteSpace(o.DeleteValidationStoredProcedureName)).ToList();
            if (listDeleteFlow.Count > 0)
            {

                deleteList.Add(unitId, listDeleteFlow);
            }

            var DeleteValidationItem = listallDeleteFlow.FirstOrDefault(o => (!string.IsNullOrWhiteSpace(o.DeleteValidationStoredProcedureName)));

            if (DeleteValidationItem != null)
            {

                validationList.Add(unitId, DeleteValidationItem);

            }
        }


        public static OperationCallResult<object> ValidatDeleteChildUnit(object unitId, object rootPrimaryKeyValue, int dataSourceFromId)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            Dictionary<string, List<AppTransactionUnitDeleteFlowExDto>> deleteList = new Dictionary<string, List<AppTransactionUnitDeleteFlowExDto>>();
            Dictionary<string, AppTransactionUnitDeleteFlowExDto> validationList = new Dictionary<string, AppTransactionUnitDeleteFlowExDto>();

            SetupOneUnitDeleteAndValidtion(deleteList, validationList, unitId.ToString());

            if (validationList.ContainsKey(unitId.ToString()))
            {

                var AppTransactionUnitDeleteFlowExDto = validationList[unitId.ToString()];

                DeleteOrValidationTranscatiionByStorePorc(rootPrimaryKeyValue, aValidationResult, "Child Unit", AppTransactionUnitDeleteFlowExDto, true, dataSourceFromId);

            }

            return aOperationCallResult;
        }

        public static OperationCallResult<object> DeleteMultipleMasterDetailFormData(int transactionId, List<object> rootPrimaryKeyValueList)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            SetupDictAppTransactionUnitDeleteListByTransactionId(appTransactionExDto);
            var rootMasterUnit = appTransactionExDto.RootMasterUnit;
            string rootUnitId = rootMasterUnit.Id.ToString();
                 

            DbTransaction trans = null;
            try
            {
                DatabaseFixture dbFxiture = AppCacheManagerBL.GetOneDatabaseFixture(appTransactionExDto.DataSourceFrom.Value);
                using (trans = dbFxiture.CreateDbTransaction())
                {

                    foreach (object rootPrimaryKeyValue in rootPrimaryKeyValueList)
                    {
                        Dictionary<string, object> dictRootOneToOneValue = new Dictionary<string, object>();
                        var rootUnit = appTransactionExDto.RootMasterUnit;

                        dictRootOneToOneValue[rootUnit.PrimaryKeyDbfieldList[0]] = rootPrimaryKeyValue;


                        //List<AppChildDataDto> childeDetal =AppMasterDetailFormDataLoadBL.SetupChildAndGrandChildData(dictRootOneToOneValue,)
                        Dictionary<AppTransactionUnitExDto, List<Dictionary<string, object>>> dictUnitIdKeyChildDelteKeyList = new Dictionary<AppTransactionUnitExDto, List<Dictionary<string, object>>>();

                        foreach (AppTransactionUnitExDto childTransactionUnitExDto in rootUnit.Children)
                        {
                            DataTable childdataTble;
                            List<Dictionary<string, object>> childPkValueIDList = AppMasterDetailFormDataLoadBL.GetChildPkValueIDList(dictRootOneToOneValue, childTransactionUnitExDto, out childdataTble);

                            dictUnitIdKeyChildDelteKeyList[childTransactionUnitExDto] = childPkValueIDList;
                        }

                        foreach (var childTransactionUnitExDto in dictUnitIdKeyChildDelteKeyList.Keys)
                        {
                            List<Dictionary<string, object>> deleteKeyList = dictUnitIdKeyChildDelteKeyList[childTransactionUnitExDto];

                            if (deleteKeyList.Count > 0)
                            {
                                AppMasterDetailFormDataSaveBL.DeleteOneChildUnitAndGrandChildUnit(dbFxiture,trans, childTransactionUnitExDto, deleteKeyList);
                            }
                        }



                        foreach (AppTransactionUnitExDto sinlngUnitDto in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                        {
                            DelteSiblingRootUnit(rootPrimaryKeyValue, trans, sinlngUnitDto);
                        }



                        DelteRootUnit(rootPrimaryKeyValue, trans, rootUnit);

                    }
                    trans.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Delete_OK", ValidationItemType.Message, "Deelte Successfully"));

                }

            }
            catch (Exception ex)
            {
                trans.Rollback();
                aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
            }



            return aOperationCallResult;

        }

        public static OperationCallResult<object> DeleteTransactionData(int transactionId, object rootPrimaryKeyValue)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            SetupDictAppTransactionUnitDeleteListByTransactionId(appTransactionExDto);
            var rootMasterUnit = appTransactionExDto.RootMasterUnit;
            string rootUnitId = rootMasterUnit != null? rootMasterUnit.Id.ToString(): "";


            if (appTransactionExDto.TransactionOrganizedType == (int)EmTransactionOrganizedType.MasterDetail)
            {
                if (appTransactionExDto.OtherOptions != null && appTransactionExDto.OtherOptions.IsApiIntegrationTransaction
                    && (!appTransactionExDto.OtherOptions.ApiIntegrationTransactionCrudType.HasValue || appTransactionExDto.OtherOptions.ApiIntegrationTransactionCrudType.Value == (int)EmAppTransactionCrudType.Read)
                    && appTransactionExDto.OtherOptions.IsEnableDeleteByApi
                    && appTransactionExDto.ConsumApiDataModelDeleteSettingDto != null)
                {
                    AppMasterDetailDto formData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId, rootPrimaryKeyValue);

                    var deleteResult = AppMasterDetailApiFormDataSaveBL.DeleteTransactionDataByDataTransfer(formData);
                    aValidationResult.Merge(deleteResult.ValidationResult);
                }
                else if (appTransactionExDto.OtherOptions != null && appTransactionExDto.OtherOptions.IsApiIntegrationTransaction
                    && appTransactionExDto.OtherOptions.ApiIntegrationTransactionCrudType.HasValue && appTransactionExDto.OtherOptions.ApiIntegrationTransactionCrudType.Value == (int)EmAppTransactionCrudType.Delete)
                {
                    AppMasterDetailDto formData = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId);
                    formData.DictCascadingFiledDataSource = new Dictionary<string, List<LookupItemDto>>();
                    formData.DictCascadingFieldIdAndLabel = new Dictionary<string, string>();
                    formData.RootPrimaryKeyValue = rootPrimaryKeyValue;

                    var deleteResult = AppMasterDetailApiFormDataSaveBL.DeleteComsumeApiTransactionData(formData);
                    aValidationResult.Merge(deleteResult.ValidationResult);
                }
                else if (rootUnitId != "" &&  appTransactionExDto.DictUnitDeleteList.ContainsKey(rootUnitId))
                {
                    DeleteTransactionRootUnitData(rootPrimaryKeyValue, aValidationResult, appTransactionExDto, rootUnitId);
                }
                else
                {
                    DeleteMasterTransactionDateByUnitStructure(rootPrimaryKeyValue, aValidationResult, appTransactionExDto);
                }

            }
            else // this is for master data source such as Type,color,size used by many tables, need to chekc all FK reference and set fk as null then delete from master table
            {
                //SetupDictAppTransactionUnitDeleteListByTransactionId(appTransactionExDto);

                //var rootMasterUnit = appTransactionExDto.RootMasterUnit;

                //string rootUnitId = rootMasterUnit.Id.ToString();

                if (rootUnitId != "" && appTransactionExDto.DictUnitDeleteList.ContainsKey(rootUnitId))
                {
                    DeleteTransactionRootUnitData(rootPrimaryKeyValue, aValidationResult, appTransactionExDto, rootUnitId);
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(null, appTransactionExDto.TransactionName, ValidationItemType.Message, " Cannot find Delete Method  "));
                }

            }



            return aOperationCallResult;

        }

        private static void DeleteMasterTransactionDateByUnitStructure(object rootPrimaryKeyValue, ValidationResult aValidationResult, AppTransactionExDto appTransactionExDto)
        {
            Dictionary<string, object> dictRootOneToOneValue = new Dictionary<string, object>();
            var rootUnit = appTransactionExDto.RootMasterUnit;

            dictRootOneToOneValue[rootUnit.PrimaryKeyDbfieldList[0]] = rootPrimaryKeyValue;


            //List<AppChildDataDto> childeDetal =AppMasterDetailFormDataLoadBL.SetupChildAndGrandChildData(dictRootOneToOneValue,)
            Dictionary<AppTransactionUnitExDto, List<Dictionary<string, object>>> dictUnitIdKeyChildDelteKeyList = new Dictionary<AppTransactionUnitExDto, List<Dictionary<string, object>>>();

            foreach (AppTransactionUnitExDto childTransactionUnitExDto in rootUnit.Children)
            {
                DataTable childdataTble;
                List<Dictionary<string, object>> childPkValueIDList = AppMasterDetailFormDataLoadBL.GetChildPkValueIDList(dictRootOneToOneValue, childTransactionUnitExDto, out childdataTble);

                dictUnitIdKeyChildDelteKeyList[childTransactionUnitExDto] = childPkValueIDList;

            }


            DatabaseFixture dbFxiture = AppCacheManagerBL.GetOneDatabaseFixture(appTransactionExDto.DataSourceFrom.Value);
            var factory = dbFxiture.DbProviderFactory;

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = dbFxiture.ConnectionString;
                connection.Open();
       
                DbTransaction trans = null;
                try
                {
                    using (trans = connection.BeginTransaction())
                    {



                        foreach (var childTransactionUnitExDto in dictUnitIdKeyChildDelteKeyList.Keys)
                        {
                            List<Dictionary<string, object>> deleteKeyList = dictUnitIdKeyChildDelteKeyList[childTransactionUnitExDto];

                            if (deleteKeyList.Count > 0)
                            {
                                AppMasterDetailFormDataSaveBL.DeleteOneChildUnitAndGrandChildUnit(dbFxiture,trans, childTransactionUnitExDto, deleteKeyList);
                            }
                        }



                        foreach (AppTransactionUnitExDto sinlngUnitDto in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                        {
                            DelteSiblingRootUnit(rootPrimaryKeyValue, trans, sinlngUnitDto);
                        }



                        DelteRootUnit(rootPrimaryKeyValue, trans, rootUnit);


                        trans.Commit();

                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Delete_OK", ValidationItemType.Message, "Deelte Successfully"));

                    }

                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            
        }

        private static void DelteRootUnit(object rootPrimaryKeyValue, DbTransaction trans, AppTransactionUnitExDto rootUnit)
        {
            DatabaseFixture dbFxiture = AppCacheManagerBL.GetOneDatabaseFixture(rootUnit.DataSourceFrom.Value);



            string fullTableName = AppMetaDataBL.GetQulifiedTableName(rootUnit.SchemaOwner, rootUnit.DataBaseTableName, dbFxiture.SqlServerType.Value);

            string rootPkField = rootUnit.PrimaryKeyDbfieldList[0];

            var dbpameter = dbFxiture.CreateParameter(rootPkField.Replace(" ", ""));
            dbpameter.Value = rootPrimaryKeyValue;

            List<DbParameter> rootParamterList = new List<DbParameter>();
            rootParamterList.Add(dbpameter);

            string deleteRootStatment = "  DELETE FROM " + fullTableName + " WHERE " + rootPkField + "=" + dbpameter.ParameterName;
            dbFxiture.ExecuteTransScalar(deleteRootStatment, rootParamterList, trans);





        }

        private static void DelteSiblingRootUnit(object rootPrimaryKeyValue, DbTransaction trans, AppTransactionUnitExDto sinlngUnitDto)
        {
            DatabaseFixture dbFxiture = AppCacheManagerBL.GetOneDatabaseFixture(sinlngUnitDto.DataSourceFrom.Value);



            string fullTableName = AppMetaDataBL.GetQulifiedTableName(sinlngUnitDto.SchemaOwner, sinlngUnitDto.DataBaseTableName, dbFxiture.SqlServerType.Value);

            string LinkToParentPrimaryKeyDbfield = sinlngUnitDto.DictLinkToParentKeyDbfield.First().Key;

            var dbpameter = dbFxiture.CreateParameter(LinkToParentPrimaryKeyDbfield.Replace(" ", ""));
            dbpameter.Value = rootPrimaryKeyValue;

            List<DbParameter> rootParamterList = new List<DbParameter>();
            rootParamterList.Add(dbpameter);

            string deleteRootStatment = "  DELETE FROM " + fullTableName + " WHERE " + LinkToParentPrimaryKeyDbfield + "=" + dbpameter.ParameterName;
            dbFxiture.ExecuteTransScalar(deleteRootStatment, rootParamterList, trans);





        }

        //public static OperationCallResult<object> DeleteTransactionDataOld(int transactionId, object rootPrimaryKeyValue)
        //      {
        //          OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
        //          ValidationResult aValidationResult = new ValidationResult();
        //          aOperationCallResult.ValidationResult = aValidationResult;

        //          AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);


        //	if(appTransactionExDto.TransactionOrganizedType == (int) EmTransactionOrganizedType.MasterDetail )
        //	{


        //		DeleteMasterDetialData(appTransactionExDto, rootPrimaryKeyValue, aValidationResult);


        //	}
        //	else // this is for master data source such as Type,color,size used by many tables, need to chekc all FK reference and set fk as null then delete from master table
        //	{

        //		SetupDictAppTransactionUnitDeleteListByTransactionId(appTransactionExDto);

        //		var rootMasterUnit = appTransactionExDto.RootMasterUnit;

        //		string rootUnitId = rootMasterUnit.Id.ToString();
        //		if (appTransactionExDto.DictUnitDeleteList.ContainsKey(rootUnitId))
        //		{
        //			DeleteTransactionRootUnitData(rootPrimaryKeyValue, aValidationResult, appTransactionExDto, rootUnitId);


        //		}
        //		else
        //		{

        //			aValidationResult.Items.Add(new ValidationItem(null, appTransactionExDto.TransactionName, ValidationItemType.Message, " Cannot find Delete Method  "));

        //		}

        //	}



        //	return aOperationCallResult;

        //      }


        private static void DeleteMasterDetialData(AppTransactionExDto appTransactionExDto, object rootPrimaryKeyValue, ValidationResult aValidationResult)
        {
            //delete grand child
            // delet child
            // delete sibling
            // delete root

            //need to use stack to stack all deelte stmt  from root to gradn child unit, the pop  eX HSTMT to delete
            Stack<string> masteDetailDeleteStatement = new Stack<string>();

            CollectRootDeleteStatement(appTransactionExDto, masteDetailDeleteStatement);
            CollectSiblingDeleteStatement(appTransactionExDto, masteDetailDeleteStatement);
            CollectChildAndGrandChildDeleteStatement(masteDetailDeleteStatement, appTransactionExDto.RootMasterUnit);

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "DeleteMaster");

                    while (masteDetailDeleteStatement.Count > 0)
                    {
                        string deleteStmt = masteDetailDeleteStatement.Pop();

                        List<SqlParameter> parametrList = new List<SqlParameter>();
                        parametrList.Add(new SqlParameter("@RootKey", rootPrimaryKeyValue));

                        adpater.ExecuteScalarQuery(deleteStmt, parametrList);



                    }

                    adpater.Commit();

                }

                catch (Exception ex)
                {
                    adpater.Rollback();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppFormEntity), "App_FormEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));

                }





            }
        }

        private static AppTransactionUnitExDto CollectRootDeleteStatement(AppTransactionExDto appTransactionExDto, Stack<string> masteDetailDeleteStatement)
        {
            var rootUnit = appTransactionExDto.RootMasterUnit;

            var dbFxiture = AppCacheManagerBL.GetOneDatabaseFixture(rootUnit.DataSourceFrom.Value);

            string fullTableName = AppMetaDataBL.GetQulifiedTableName(rootUnit.SchemaOwner, rootUnit.DataBaseTableName, dbFxiture.SqlServerType.Value);

            //Root unit only have ont Primaykey
            string keyFued = rootUnit.PrimaryKeyDbfieldList[0];
            var dbpameter = dbFxiture.CreateParameter(keyFued.Replace(" ", ""));

            string deleteRootStatment = "  DELETE FROM " + fullTableName + " WHERE " + keyFued + "=@RootKey";



            masteDetailDeleteStatement.Push(deleteRootStatment);
            return rootUnit;
        }

        private static void CollectSiblingDeleteStatement(AppTransactionExDto appTransactionExDto, Stack<string> masteDetailDeleteStatement)
        {
            var siblingUnitList = appTransactionExDto.SibLineTransactionUnitIdExDtoList;
            if (!siblingUnitList.IsEmpty())
            {

                foreach (AppTransactionUnitExDto sinlngUnitDto in appTransactionExDto.SibLineTransactionUnitIdExDtoList)
                {
                    string LinkToParentPrimaryKeyDbfield = sinlngUnitDto.DictLinkToParentKeyDbfield.First().Key;

                    string deletesiblingStatment = "  DELETE FROM [" + sinlngUnitDto.DataBaseTableName + "] WHERE " + LinkToParentPrimaryKeyDbfield + "=@RootKey";

                    if (!CheckIsObjectisVuew(sinlngUnitDto.DataBaseTableName))
                    {
                        masteDetailDeleteStatement.Push(deletesiblingStatment);

                    }



                }
            }
        }

        private static void CollectChildAndGrandChildDeleteStatement(Stack<string> masteDetailDeleteStatement, AppTransactionUnitExDto rootUnit)
        {
            if (!rootUnit.Children.IsEmpty())
            {

                foreach (AppTransactionUnitExDto childUnitDto in rootUnit.Children)
                {

                    string getChildPkStatment = ""; //"  SELECT " + childUnitDto.PrimaryKeyDbfield + "  FROM " + childUnitDto.DataBaseTableName + " WHERE " + childUnitDto.LinkToParentPrimaryKeyDbfield + "=@RootKey";
                    string deleteChildStatment = "";// "  DELETE FROM " + childUnitDto.DataBaseTableName + " WHERE " + childUnitDto.LinkToParentPrimaryKeyDbfield + "=@RootKey";


                    if (!CheckIsObjectisVuew(childUnitDto.DataBaseTableName))
                    {
                        masteDetailDeleteStatement.Push(deleteChildStatment);

                    }

                    if (!childUnitDto.Children.IsEmpty())
                    {

                        foreach (AppTransactionUnitExDto granchildUnitDto in childUnitDto.Children)
                        {

                            string deleteGrandChildStatment = "";// "  DELETE FROM " + granchildUnitDto.DataBaseTableName + " WHERE " + granchildUnitDto.LinkToParentPrimaryKeyDbfield + " IN ( " + getChildPkStatment + ")";


                            if (!CheckIsObjectisVuew(granchildUnitDto.DataBaseTableName))
                            {
                                masteDetailDeleteStatement.Push(deleteGrandChildStatment);

                            }





                        }

                    }


                }

            }
        }

        public static bool CheckIsObjectisVuew(string tableOrViewName)
        {

            string query = @"select OBJECT_ID FROM sys.views where name = @tableOrViewName";

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                List<SqlParameter> parametrList = new List<SqlParameter>();
                parametrList.Add(new SqlParameter("@tableOrViewName", tableOrViewName));
                object result = adpater.ExecuteScalarQuery(query, parametrList);

                if (result == null)
                {
                    return false;
                }
                else
                {
                    return true;

                }

            }




        }

        private static void DeleteTransactionRootUnitData(object rootPrimaryKeyValue, ValidationResult aValidationResult, AppTransactionExDto appTransactionExDto, string rootUnitId)
        {
            List<AppTransactionUnitDeleteFlowExDto> rootUnitIdDeleteList = appTransactionExDto.DictUnitDeleteList[rootUnitId];

            AppTransactionUnitDeleteFlowExDto storedProcedureDeelte = rootUnitIdDeleteList.FirstOrDefault(o => (!string.IsNullOrWhiteSpace(o.StoredProcedureName)));

            // no need to check child delete , the store proc will delete all children recursively 
            if (storedProcedureDeelte != null)
            {
                DeleteOrValidationTranscatiionByStorePorc(rootPrimaryKeyValue, aValidationResult, appTransactionExDto.TransactionName, storedProcedureDeelte, false, appTransactionExDto.DataSourceFrom.Value);

            }
            else // cannot find store proc --------- need to use  pure SQl
            {

                //   AppMasterDetailDto masterDetailData = AppMasterDetailFormDataLoadBL.GetMasterDetailTransactionData(appTransactionExDto, rootPrimaryKeyValue);



                // Root unit delete last time
                foreach (AppTransactionUnitDeleteFlowExDto aAppTransactionUnitDeleteFlowExDto in rootUnitIdDeleteList)
                {

                    string RelativeTableName = aAppTransactionUnitDeleteFlowExDto.RelativeTableName;
                    string fkName = aAppTransactionUnitDeleteFlowExDto.RelativeForeignKeyName;

                    string deleteFormat = string.Format("delete [{0}] where [{1}] = @{2}", RelativeTableName, fkName, fkName);
                    // Sql

                    // !!





                }

            }
        }



        private static void DeleteOrValidationTranscatiionByStorePorc(object rootPrimaryKeyValue, ValidationResult aValidationResult,
            string messageKeyName, AppTransactionUnitDeleteFlowExDto storedProcedureDeelteOrValidate, bool isValidation, int dataSourceFromId)
        {


            string ProcedureName = storedProcedureDeelteOrValidate.StoredProcedureName;

            if (isValidation)
            {
                ProcedureName = storedProcedureDeelteOrValidate.DeleteValidationStoredProcedureName;
            }

            //SqlParameter rootPrimaryPara = new SqlParameter(storedProcedureDeelteOrValidate.SpParameterOptions, rootPrimaryKeyValue);
            //List<SqlParameter> paramtersList = new List<SqlParameter>();
            //paramtersList.Add(rootPrimaryPara);

            //SqlParameter warningMsg = new SqlParameter(storedProcedureDeelteOrValidate.WarningMessage, SqlDbType.NVarChar, 4000);
            //warningMsg.Direction = ParameterDirection.Output;
            //paramtersList.Add(warningMsg);

            //SqlParameter userIdParam = new SqlParameter("@CurrentUserId", AppSecurityUserBL.CurrentUserEntity.UserId);
            //paramtersList.Add(userIdParam);


            //using (var conn = new SqlConnection(DBInteractionBase.APPConnectionString))
            //{
            //    conn.Open();
            //    SqlTransaction trans = conn.BeginTransaction();

            //    try
            //    {
            //        ExcuteNonQuery(ProcedureName, paramtersList, conn, trans, true);

            //        trans.Commit();
            //    }
            //    catch (Exception ex)
            //    {
            //        trans.Rollback();
            //        aValidationResult.Items.Add(new ValidationItem(null, messageKeyName, ValidationItemType.Error, ex.InnerException.Message));

            //    }

            //}



            var dbFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceFromId);

            string queryStorcname = ProcedureName;
            List<DbParameter> sqlParamterList = new List<DbParameter>();

            DbParameter rootPrimaryPara = dbFixture.CreateParameter(storedProcedureDeelteOrValidate.SpParameterOptions); new SqlParameter(storedProcedureDeelteOrValidate.SpParameterOptions, rootPrimaryKeyValue);
            rootPrimaryPara.Value = rootPrimaryKeyValue;
            sqlParamterList.Add(rootPrimaryPara);

            DbParameter warningMsg = new SqlParameter(storedProcedureDeelteOrValidate.WarningMessage, SqlDbType.NVarChar, 4000);
            warningMsg.Direction = ParameterDirection.Output;
            sqlParamterList.Add(warningMsg);

            DbParameter userIdParam = new SqlParameter("@CurrentUserId", AppSecurityUserBL.CurrentUserEntity.UserId);
            sqlParamterList.Add(userIdParam);

            try
            {
                DataTable fillDataTable = dbFixture.RetriveStorProcDataTable(queryStorcname, sqlParamterList);

            }
            catch (Exception ex)
            {
                aValidationResult.Items.Add(new ValidationItem(null, messageKeyName, ValidationItemType.Error, ex.InnerException.Message));

            }





            // no warmmesage 
            if (String.IsNullOrWhiteSpace(warningMsg.Value as string))
            {
                aValidationResult.Items.Add(new ValidationItem(null, messageKeyName, ValidationItemType.Message, "delete succsefully "));

            }
            else
            {

                aValidationResult.Items.Add(new ValidationItem(null, messageKeyName, ValidationItemType.Error, warningMsg.Value.ToString()));



            }
        }

        public static void ExcuteNonQuery(string ProcedureName, List<SqlParameter> paramtersList, SqlConnection conn, SqlTransaction trans, bool isStorProc = false)
        {
            SqlCommand cmdToExecute = new SqlCommand();
            cmdToExecute.Connection = conn;
            cmdToExecute.Transaction = trans;


            cmdToExecute.CommandText = string.Format("{0}", ProcedureName);

            if (isStorProc)
            {
                cmdToExecute.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                cmdToExecute.CommandType = CommandType.Text;
            }

            // Use base class' connection object

            cmdToExecute.Parameters.AddRange(paramtersList.ToArray());
            cmdToExecute.ExecuteNonQuery();
        }


    }
}