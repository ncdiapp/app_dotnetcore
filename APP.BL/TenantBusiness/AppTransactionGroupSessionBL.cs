using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;
using DatabaseSchemaMrg;
using System.Data.Common;
using DatabaseSchemaMrg.DataSchema;

using APP.Framework;
namespace App.BL
{
    public static class AppTransactionGroupSessionBL
    {
        public static AppTransactionGroupSessionExDto RetrieveOneAppTransactionGroupSession(int groupSessionId)
        {
            AppTransactionGroupSessionEntity groupSessionEntity = RetrieveOneAppTransactionGroupSessionEntity(groupSessionId);
            AppTransactionGroupSessionExDto groupSessionExDto = AppTransactionGroupSessionConverter.ConvertEntityToExDto(groupSessionEntity);

            AppTransactionGroupExDto transactionGroupExDto = AppTransactionGroupBL.RetrieveOneAppTransactionGroupExDto(groupSessionEntity.TransactionGroupId);
            groupSessionExDto.ForeignAppTransactionGroupExDto = transactionGroupExDto;

            groupSessionExDto.HeaderGroupItemList = new List<AppTransactionGroupItemDto>();
            groupSessionExDto.RegularGroupItemList = new List<AppTransactionGroupItemDto>();


            foreach (var groupItem in transactionGroupExDto.AppTransactionGroupItemList.OrderBy(o => o.TransactionLayoutOrder))
            {
                int? transactionId = null;
                object rootPkValue = null;

                if (groupItem.ForeignAppTransactionItemExDto != null) // From Libarary Transaction Item
                {
                    transactionId = groupItem.ForeignAppTransactionItemExDto.TransactionId;
                    if (transactionId.HasValue)
                    {
                        groupItem.DisplayName = groupItem.ForeignAppTransactionItemExDto.TransactionItemName;
                        groupItem.TransId = transactionId.Value;
                        rootPkValue = GetRuntimeGroupItemRootPKValue(groupSessionId, (int)groupItem.Id, groupItem.TransId.Value);
                    }
                }
                else
                {
                    // Test Fake Data
                    if (!groupItem.TransId.HasValue)
                    {
                        groupItem.TransId = 7533;
                    }
                    

                    if (groupItem.TransId.HasValue)
                    {
                        groupItem.DisplayName = "";
                        rootPkValue = groupSessionId;
                    }

                }

                if (rootPkValue != null)
                {
                    groupItem.TransRid = rootPkValue;

                    if (groupItem.IsGroupSharedHeader.HasValue && groupItem.IsGroupSharedHeader.Value)
                    {
                        groupSessionExDto.HeaderGroupItemList.Add(groupItem);
                    }
                    else
                    {
                        groupSessionExDto.RegularGroupItemList.Add(groupItem);
                    }

                }


            }

            return groupSessionExDto;
        }


        public static OperationCallResult<AppTransactionGroupSessionExDto> CreateOneAppTransactionGroupSession(AppTransactionGroupSessionExDto groupSessionExDto)
        {
            OperationCallResult<AppTransactionGroupSessionExDto> aOperationCallResult = new OperationCallResult<AppTransactionGroupSessionExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppTransactionGroupExDto appTransactionGroupExDto = AppTransactionGroupBL.RetrieveOneAppTransactionGroupExDto(groupSessionExDto.TransactionGroupId);

            if (appTransactionGroupExDto.AppTransactionGroupItemList.IsEmpty())
            {
                string errorMsg = "Cannot find data model list for this session.";
                aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupSessionExDto), "App_SecurityGroupSessionEntity_QueryExecution_Error", ValidationItemType.Error, errorMsg));
                return aOperationCallResult;
            }
            else
            {
                if (!groupSessionExDto.SaasApplicationId.HasValue)
                {
                    groupSessionExDto.SaasApplicationId = appTransactionGroupExDto.SaasApplicationId;
                }

                if (string.IsNullOrWhiteSpace(groupSessionExDto.SessionGroupName))
                {
                    groupSessionExDto.SessionGroupName = appTransactionGroupExDto.GroupName;
                }

                if (string.IsNullOrWhiteSpace(groupSessionExDto.Description))
                {
                    groupSessionExDto.Description = appTransactionGroupExDto.Description;
                }


                SaveNewGroupSessionEntity(groupSessionExDto, aValidationResult);

                if (!aValidationResult.HasErrors && groupSessionExDto.Id != null)
                {
                    foreach (var groupItem in appTransactionGroupExDto.AppTransactionGroupItemList)
                    {
                        if (groupItem.ForeignAppTransactionItemExDto != null)
                        {
                            var transactionItem = groupItem.ForeignAppTransactionItemExDto;
                            ValidationResult importResult = ImportOneTransactionGroupSessionFormDataFromLibrary((int)groupSessionExDto.Id, (int)groupItem.Id, transactionItem);

                            if (importResult.HasErrors)
                            {
                                aValidationResult.Merge(importResult);
                            }
                        }
                        else
                        {
                            // Test Fake Data
                            if (!groupItem.TransId.HasValue)
                            {
                                groupItem.TransId = 7533;
                            }

                            if (groupItem.TransId.HasValue)
                            {
                                AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(groupItem.TransId.Value, groupSessionExDto.Id);
                                if (aAppformDataDto != null)
                                {
                                    aAppformDataDto.IsDirty = true;
                                    ValidationResult saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(aAppformDataDto).ValidationResult;

                                    if (saveResult.HasErrors)
                                    {
                                        aValidationResult.Merge(saveResult);
                                    }
                                }
                            }
                        }
                    }

                    aOperationCallResult.Object = RetrieveOneAppTransactionGroupSession((int)groupSessionExDto.Id);
                }

                return aOperationCallResult;
            }
        }


        public static AppTransactionGroupSessionExDto LoadOneAppTransactionGroupPreviewSession(int transactionGroupId)
        {
            AppTransactionGroupSessionExDto groupSessionExDto = new AppTransactionGroupSessionExDto();
            groupSessionExDto.TransactionGroupId = transactionGroupId;


            AppTransactionGroupExDto transactionGroupExDto = AppTransactionGroupBL.RetrieveOneAppTransactionGroupExDto(transactionGroupId);
            groupSessionExDto.ForeignAppTransactionGroupExDto = transactionGroupExDto;

            groupSessionExDto.HeaderGroupItemList = new List<AppTransactionGroupItemDto>();
            groupSessionExDto.RegularGroupItemList = new List<AppTransactionGroupItemDto>();

            groupSessionExDto.SessionGroupName = transactionGroupExDto.GroupName;
            groupSessionExDto.Description = transactionGroupExDto.Description;

            foreach (var groupItem in transactionGroupExDto.AppTransactionGroupItemList.OrderBy(o => o.TransactionLayoutOrder))
            {
                int? transactionId = null;
                object rootPkValue = null;

                if (groupItem.ForeignAppTransactionItemExDto != null) // From Libarary Transaction Item
                {
                    transactionId = groupItem.ForeignAppTransactionItemExDto.TransactionId;
                    if (transactionId.HasValue)
                    {
                        groupItem.DisplayName = groupItem.ForeignAppTransactionItemExDto.TransactionItemName;
                        groupItem.TransId = transactionId.Value;
                        rootPkValue = GetLibraryGroupTransactionItemRootPKValue(groupItem.ForeignAppTransactionItemExDto);
                    }
                }
                else
                {
                    // Test Fake Data
                    groupItem.TransId = 7533;

                    if (groupItem.TransId.HasValue)
                    {
                        groupItem.DisplayName = "";
                    }

                }

                groupItem.TransRid = rootPkValue;

                if (groupItem.IsGroupSharedHeader.HasValue && groupItem.IsGroupSharedHeader.Value)
                {
                    groupSessionExDto.HeaderGroupItemList.Add(groupItem);
                }
                else
                {
                    groupSessionExDto.RegularGroupItemList.Add(groupItem);
                }
            }

            return groupSessionExDto;
        }


        private static AppTransactionGroupSessionEntity RetrieveOneAppTransactionGroupSessionEntity(object groupSessionId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppTransactionGroupSessionEntity groupSessionEntity = new AppTransactionGroupSessionEntity(int.Parse(groupSessionId.ToString()));

                adpater.FetchEntity(groupSessionEntity);
                return groupSessionEntity;
            }
        }

        private static void SaveNewGroupSessionEntity(AppTransactionGroupSessionExDto groupSessionExDto, ValidationResult aValidationResult)
        {
            AppTransactionGroupSessionEntity appTransactionGroupSessionEntity = new AppTransactionGroupSessionEntity();
            AppTransactionGroupSessionConverter.CopyDtoToEntity(appTransactionGroupSessionEntity, groupSessionExDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(appTransactionGroupSessionEntity);

                    adapter.Commit();

                    groupSessionExDto.Id = appTransactionGroupSessionEntity.TransactionGroupSessionId;
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupSessionExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupSessionExDto), "App_SecurityGroupSessionEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }

        private static ValidationResult ImportOneTransactionGroupSessionFormDataFromLibrary(int groupSessionId, int groupItemId, AppTransactionItemExDto transactionItem)
        {
            if (transactionItem != null && transactionItem.TransactionId.HasValue)
            {
                object libItemRid = GetLibraryGroupTransactionItemRootPKValue(transactionItem);

                if (libItemRid != null)
                {
                    AppMasterDetailDto newFormData = AppMasterDetailFormDataLoadBL.GetNewFormDataFromDefaultDataTransfer(transactionItem.TransactionId.Value, libItemRid);
                    if (newFormData != null)
                    {
                        newFormData.DictOneToOneFields["TransactionItemID"] = null;
                        newFormData.DictOneToOneFields["TransactionGroupSessionID"] = groupSessionId;
                        newFormData.DictOneToOneFields["TransactionGroupItemID"] = groupItemId;

                        OperationCallResult<AppMasterDetailDto> saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(newFormData);

                        return saveResult.ValidationResult;
                    }
                }
            }

            ValidationResult errorResult = new ValidationResult();
            errorResult.Items.Add(new ValidationItem(typeof(AppMasterDetailDto), "App_MasterDetailFormData_Error", ValidationItemType.Error, "Import failed on " + transactionItem.TransactionItemName));
            return errorResult;
        }

        private static object GetLibraryGroupTransactionItemRootPKValue(AppTransactionItemExDto transactionItem)
        {
            if (transactionItem != null && transactionItem.TransactionId.HasValue)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionItem.TransactionId);

                if (transactionExDto.RootMasterUnit != null && !transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.IsEmpty())
                {
                    DataRow aDataRow = null;

                    DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.RootMasterUnit.DataSourceFrom.Value);
                    List<DbParameter> sqlParamters = new List<DbParameter>();

                    DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(transactionExDto.RootMasterUnit);

                    SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

                    string rootSelectall = sqlWriter.SelectWhereSql("TransactionItemID");

                    DbParameter parameter = databaseFixtureInstance.CreateParameter("TransactionItemID");
                    parameter.Value = transactionItem.Id;
                    sqlParamters.Add(parameter);

                    DataTable dtResult = databaseFixtureInstance.RetriveDataTable(rootSelectall, sqlParamters);
                    if (dtResult.Rows.Count > 0)
                    {
                        aDataRow = dtResult.Rows[0];

                        string pkColumnName = transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.First();
                        if (aDataRow[pkColumnName] != null)
                        {
                            return aDataRow[pkColumnName];
                        }
                    }

                }
            }


            return null;
        }



        private static object GetRuntimeGroupItemRootPKValue(int groupSessionId, int groupItemId, int transactionId)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            if (transactionExDto.RootMasterUnit != null && !transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.IsEmpty())
            {
                DataRow aDataRow = null;

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.RootMasterUnit.DataSourceFrom.Value);
                List<DbParameter> sqlParamters = new List<DbParameter>();

                DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(transactionExDto.RootMasterUnit);

                SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

                string rootSelectall = sqlWriter.SelectWhereSql("TransactionGroupSessionID");
                rootSelectall += " and TransactionGroupItemID = @TransactionGroupItemID";

                DbParameter parameter_GroupSessionID = databaseFixtureInstance.CreateParameter("TransactionGroupSessionID");
                parameter_GroupSessionID.Value = groupSessionId;
                sqlParamters.Add(parameter_GroupSessionID);

                DbParameter parameter_GroupItemID = databaseFixtureInstance.CreateParameter("TransactionGroupItemID");
                parameter_GroupItemID.Value = groupItemId;
                sqlParamters.Add(parameter_GroupItemID);

                DataTable dtResult = databaseFixtureInstance.RetriveDataTable(rootSelectall, sqlParamters);
                if (dtResult.Rows.Count > 0)
                {
                    aDataRow = dtResult.Rows[0];

                    string pkColumnName = transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.First();
                    if (aDataRow[pkColumnName] != null)
                    {
                        return aDataRow[pkColumnName];
                    }
                }

            }



            return null;
        }


        //private static object GetPreviewSessionGroupItemRootPKValue(int transactionItemId, int transactionId)
        //{
        //    AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

        //    if (transactionExDto.RootMasterUnit != null && !transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.IsEmpty())
        //    {
        //        DataRow aDataRow = null;

        //        DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.RootMasterUnit.DataSourceFrom.Value);
        //        List<DbParameter> sqlParamters = new List<DbParameter>();

        //        DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(transactionExDto.RootMasterUnit);

        //        SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

        //        string rootSelectall = sqlWriter.SelectWhereSql("TransactionItemID");
              

        //        DbParameter parameter_TransactionItemID = databaseFixtureInstance.CreateParameter("TransactionItemID");
        //        parameter_TransactionItemID.Value = transactionItemId;
        //        sqlParamters.Add(parameter_TransactionItemID);

             
        //        DataTable dtResult = databaseFixtureInstance.RetriveDataTable(rootSelectall, sqlParamters);
        //        if (dtResult.Rows.Count > 0)
        //        {
        //            aDataRow = dtResult.Rows[0];

        //            string pkColumnName = transactionExDto.RootMasterUnit.PrimaryKeyDbfieldList.First();
        //            if (aDataRow[pkColumnName] != null)
        //            {
        //                return aDataRow[pkColumnName];
        //            }
        //        }

        //    }



        //    return null;
        //}
    }
}
