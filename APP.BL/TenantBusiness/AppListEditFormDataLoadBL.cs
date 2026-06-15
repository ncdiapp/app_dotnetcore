using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

using APP.Framework;
namespace App.BL
{
    public static class AppListEditFormDataLoadBL
    {
        public static AppListDataDto GetListEditFormData(int transactionId, List<object> rootIdList = null, string transactionJsonData = null)
        {
            AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

            if (transactionExDto.OtherOptions != null && transactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {
                return AppListEditApiFormDataLoadBL.GetApiListEditFormData(transactionId, rootIdList, transactionJsonData);
            }
            else
            {
                AppChildDataDto cloneAppChildDataDto = GetListCloneEditRow(transactionExDto);

                AppListDataDto aAppformDataDto = GetOneListEditTransactionAllData(transactionExDto, rootIdList);

                aAppformDataDto.EditCloneAppChildDataDto = cloneAppChildDataDto;

                ProcessListEditFormFileIDCodeDictionary(transactionExDto, aAppformDataDto);

                return aAppformDataDto;
            }
        }

        public static AppChildDataDto GetListCloneEditRow(AppTransactionExDto AppTransactionExDto)
        {
            var rootMasterUnit = AppTransactionExDto.RootMasterUnit;

            Dictionary<string, List<AppChildDataDto>> DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            AppTransactionStructureLoadBL.SetChildDefaultValuePlaceholder(DictOneToManyFields, rootMasterUnit);

            AppChildDataDto firstAppChildDataDto = DictOneToManyFields[rootMasterUnit.Id.ToString()].FirstOrDefault();
            return firstAppChildDataDto;
        }


        public static void RemoveFolderRootUnit(AppTransactionExDto folderAndListTransactionExDto)
        {
            var rootMasterUnit = folderAndListTransactionExDto.AppTransactionUnitList.ElementAt(0);

            // need to remove the root
            if (!rootMasterUnit.ParentTransactionUnitId.HasValue && !rootMasterUnit.Children.IsEmpty() && rootMasterUnit.Children.Count > 0)
            {
                var childRootUnit = rootMasterUnit.Children.ElementAt(0);

                folderAndListTransactionExDto.DictAllTransactionUnitIdExDto.Remove(rootMasterUnit.Id.ToString());

                folderAndListTransactionExDto.AppTransactionUnitList = new ObservableSet<AppTransactionUnitExDto>();

                folderAndListTransactionExDto.AppTransactionUnitList.Add(rootMasterUnit.Children.First());
            }
            // return rootMasterUnit;
        }


        internal static AppMasterDetailDto ConvertAppListDataDtoToMasterDetailDto(AppListDataDto listDataDto)
        {
            if (listDataDto != null)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(listDataDto.TransactionId);

                AppMasterDetailDto masterDetailDto = new AppMasterDetailDto();
                masterDetailDto.DictOneToOneFields = new Dictionary<string, object>();
                masterDetailDto.DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>();
                masterDetailDto.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();

                masterDetailDto.OrgAppListDataDto = listDataDto;

                string rootUnitIdStr = transactionExDto.RootMasterUnit.Id.ToString();

                //masterDetailDto.DictOneToManyFields.Add(rootUnitIdStr, listDataDto.ListData);
                //masterDetailDto.EditCloneDictOneToManyFields.Add(rootUnitIdStr, new List<AppChildDataDto>() { listDataDto.EditCloneAppChildDataDto });

                masterDetailDto.TransactionId = listDataDto.TransactionId;
                masterDetailDto.IsShowSaveButton = transactionExDto.IsShowSaveButton;

                masterDetailDto.IsDirty = listDataDto.IsDirty;
                masterDetailDto.DictDocumentIdFileCode = listDataDto.DictDocumentIdFileCode;              
                masterDetailDto.DataTransferSettingId = listDataDto.DataTransferSettingId;
                masterDetailDto.TransactionCommandId = listDataDto.TransactionCommandId;

                return masterDetailDto;
            }

            return null;
        }


        internal static AppMasterDetailDto ConvertBackMasterDetailDtoToAppListDataDto(AppMasterDetailDto masterDetailDto)
        {
            if (masterDetailDto != null)
            {
                AppTransactionExDto transactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(masterDetailDto.TransactionId);

                AppListDataDto listDataDto = masterDetailDto.OrgAppListDataDto;

                string rootUnitIdStr = transactionExDto.RootMasterUnit.Id.ToString();

                //listDataDto.ListData = masterDetailDto.DictOneToManyFields[rootUnitIdStr];
                //listDataDto.EditCloneAppChildDataDto = masterDetailDto.EditCloneDictOneToManyFields[rootUnitIdStr].FirstOrDefault();

                listDataDto.TransactionId = masterDetailDto.TransactionId;                
                listDataDto.IsDirty = masterDetailDto.IsDirty;
                listDataDto.DictDocumentIdFileCode = masterDetailDto.DictDocumentIdFileCode;
                listDataDto.DataTransferSettingId = masterDetailDto.DataTransferSettingId;
                listDataDto.TransactionCommandId = masterDetailDto.TransactionCommandId;

                return masterDetailDto;
            }

            return null;
        }



        private static AppListDataDto GetOneListEditTransactionAllData(AppTransactionExDto AppTransactionExDto, List<object> rootIdList)
        {
            int TransactionId = (int)AppTransactionExDto.Id;
            AppListDataDto rootAppformDataDto = new AppListDataDto();
            rootAppformDataDto.TransactionId = TransactionId;

            // only one root

            string rootIds = "";
            var rootMasterUnit = AppTransactionExDto.RootMasterUnit;

            string rootWhereClause = "";

            string rootPkDbFied = rootMasterUnit.PrimaryKeyDbfieldList[0];

            if (!rootIdList.IsEmpty())
            {


                var idStrings = rootIdList.Select(o => o.ToString()).ToList();

                rootIds = idStrings.Aggregate((i, j) => i.ToString() + "," + j.ToString());




                rootWhereClause = " WHERE " + rootPkDbFied + " IN ( " + rootIds + ")";
            }


            DataTable rootdataTble = GetOneUnitDataTable(rootMasterUnit, rootWhereClause);


            Dictionary<AppTransactionUnitExDto, DataTable> dictGrandChilddataTble = new Dictionary<AppTransactionUnitExDto, DataTable>();

            if (rootMasterUnit.Children != null && rootMasterUnit.Children.Count > 0)
            {
                foreach (var childUnit in rootMasterUnit.Children)
                {
                    string childWhereClause = "";
                    if (!string.IsNullOrWhiteSpace(rootIds))
                    {
                        string childFkDbFied = childUnit.DictLinkToParentKeyDbfield.Where(o => o.Value == rootPkDbFied).First().Key;
                        childWhereClause = " WHERE " + childFkDbFied + " IN ( " + rootIds + ")";
                    }

                    DataTable childDatatable = GetOneUnitDataTable(childUnit, childWhereClause);


                    dictGrandChilddataTble.Add(childUnit, childDatatable);
                }
            }

            List<AppChildDataDto> appChildDataDtoList =
                AppMasterDetailFormDataLoadBL.ConvertChildAndGradnChildTableToAppChildDataDtoList(rootMasterUnit, rootdataTble, dictGrandChilddataTble);

            rootAppformDataDto.ListData = appChildDataDtoList;

            return rootAppformDataDto;
        }


        private static DataTable GetOneUnitDataTable(AppTransactionUnitExDto unitExdto, string whereClause)
        {

            DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(unitExdto); ;

            var dataBaseFxiture = AppCacheManagerBL.GetOneDatabaseFixture(unitExdto.DataSourceFrom.Value);

            SqlWriter sqlWriter = new SqlWriter(databaseTable, dataBaseFxiture.SqlServerType.Value);

            string rootSelectall = sqlWriter.SelectAllSql() + whereClause;

            DataTable unitTable = dataBaseFxiture.RetriveDataTable(rootSelectall, new List<DbParameter>());






            return unitTable;
        }





        public static OperationCallResult<AppListDataDto> SaveListEditFormData(AppListDataDto appListDataDto)
        {
            AppTransactionExDto appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(appListDataDto.TransactionId);

            if (appTransactionExDto.OtherOptions != null && appTransactionExDto.OtherOptions.IsApiIntegrationTransaction)
            {
                return AppListEditApiFormDataLoadBL.SaveApiListEditFormData(appListDataDto);
            }

            OperationCallResult<AppListDataDto> aOperationCallResult = new OperationCallResult<AppListDataDto>();

            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            aValidationResult.Merge(AppTransactionFormulaBL.ValidateListEditTransactionData(appListDataDto).ValidationResult);

            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = appListDataDto;
                return aOperationCallResult;
            }



            


            AppTransactionStructureDto transactionStructureDto = AppTransactionStructureLoadBL.GetGeneralStrcutureInfo(appTransactionExDto);

            int transcationId = int.Parse(appListDataDto.TransactionId.ToString());

            AppListDataDto orAppgListData = new AppListDataDto();

            if (appListDataDto.IsMassUpdate)
            {
                if (!appListDataDto.MassUpdateRootIdList.IsEmpty())
                {
                    orAppgListData = GetListEditFormData(transcationId, appListDataDto.MassUpdateRootIdList);
                }
            }
            else
            {
                orAppgListData = GetListEditFormData(transcationId);
            }






            var rootMasterUnit = appTransactionExDto.AppTransactionUnitList.ElementAt(0);

            string tableName = rootMasterUnit.DataBaseTableName;
            DatabaseTable rootDatabaseTable = AppCacheManagerBL.GetDatabaseTable(tableName, rootMasterUnit.DataSourceFrom, rootMasterUnit.SchemaOwner);

            //	List<string> rootUnitPkFields = transactionStructureDto.DictTransactionUnitPKFied[rootMasterUnit.Id.ToString()];

            if (appListDataDto.IsDirty)
            {


                List<AppChildDataDto> updateChildUnitDataList = appListDataDto.ListData;



                //string connectInfo = AppMetaDataBL.GetConnectInfo(rootMasterUnit.DataSourceFrom);

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(rootMasterUnit.DataSourceFrom.Value);

                DbProviderFactory factory = databaseFixtureInstance.DbProviderFactory;

                Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>> changeCollection = new Tuple<List<AppChildDataDto>, List<AppChildDataDto>, List<AppChildDataDto>>(
                                 new List<AppChildDataDto>(),
                                 new List<AppChildDataDto>(),
                                 new List<AppChildDataDto>()
                          );


                using (var connection = factory.CreateConnection())
                {
                    connection.ConnectionString = databaseFixtureInstance.ConnectionString;
                    connection.Open();

                    DbTransaction trans = connection.BeginTransaction();
                    try
                    {


                        string erromesgae = AppMasterDetailFormDataSaveBL.UpdateOneChildUnit(databaseFixtureInstance,appTransactionExDto, null, trans, rootMasterUnit, updateChildUnitDataList, orAppgListData.ListData, changeCollection);

                        if (string.IsNullOrWhiteSpace(erromesgae))
                        {
                            trans.Commit();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_ListEditFormData_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }
                        else
                        {
                            trans.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_ListEditFormData_Save_OK", ValidationItemType.Message, "Saved Failed: " + erromesgae));
                        }



                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppDataSetExDto), "App_ListEditFormData_Exception_Error", ValidationItemType.Error, ex.ToString()));
                    }

                }

            }

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = GetListEditFormData(appListDataDto.TransactionId);
            }

            return aOperationCallResult;
        }




        public static bool CheckIsFolderEmpty(int folderId)
        {
            AppSefolderEntity folderEntity = AppSeFolderBL.RetrieveOneAppSefolderEntity(folderId);
            bool isFolderEmpty = true;

            //if (folderEntity != null && folderEntity.FolderType.HasValue)
            //{
            //             if (folderEntity.FolderType.Value == (int)EmAppTransBusinessType.File)
            //             {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                //try
                //{
                List<SqlParameter> lsitparamter = new List<SqlParameter>();
                lsitparamter.Add(new SqlParameter("@folderId", folderId));

                string query = @"  SELECT [FileID] FROM AppFile where FolderID = @folderId ";

                DataTable dt = adapter.ExecuteDataTableRetrievalQuery(query, lsitparamter);

                if (dt.Rows.Count > 0)
                {
                    isFolderEmpty = false;
                }



                //}
                //catch(Exception e)
                //{
                //    isFolderEmpty = false;
                //}
            }
            //             }
            //             else
            //             {
            //                 // to do
            //                 isFolderEmpty = false;
            //             }				
            ////}

            return isFolderEmpty;
        }

        public static OperationCallResult<bool> UpdateAppFileFolder(List<int> fileIdList, int? targetFolderId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();

            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            if (fileIdList.Count > 0 && targetFolderId.HasValue)
            {


                AppFileEntity fileEntity = new AppFileEntity();
                fileEntity.FolderId = targetFolderId;

                var folderEntity = AppSeFolderBL.RetrieveOneAppSefolderEntity(targetFolderId.Value);

                if (folderEntity != null && folderEntity.AppCreatedById.HasValue)
                {
                    fileEntity.AppCreatedById = folderEntity.AppCreatedById.Value;
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                        RelationPredicateBucket filter = new RelationPredicateBucket();
                        filter.PredicateExpression.Add(AppFileFields.FileId == fileIdList.ToArray());

                        adapter.UpdateEntitiesDirectly(fileEntity, filter);
                        aOperationCallResult.ValidationResult.AddItem(null, "File_Folder_Update_OK", ValidationItemType.Message, "File folder updated successful.");
                        adapter.Commit();
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aOperationCallResult.ValidationResult.Items.Add(new ValidationItem(null, "File_Folder_Update_Failed", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }

            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }

        internal static void ProcessListEditFormFileIDCodeDictionary(AppTransactionExDto appTransactionExDto, AppListDataDto aAppformDataDto)
        {
            aAppformDataDto.DictDocumentIdFileCode = new Dictionary<int, string>();

            List<int> fileIdList = new List<int>();

            Dictionary<string, List<string>> DictUnitIdAndFileTransFieldDbNames = appTransactionExDto.DictAllTransactionField.Values.Where(o => o.ControlType == (int)EmAppControlType.File || o.ControlType == (int)EmAppControlType.Image)
                .GroupBy(o => o.TransactionUnitId).ToDictionary(g => g.Key.ToString(), g => g.Select(o => o.DataBaseFieldName).ToList());

            if (DictUnitIdAndFileTransFieldDbNames.Count > 0)
            {
                foreach (var childRow in aAppformDataDto.ListData)
                {
                    if (childRow.ChildUnitId != null && DictUnitIdAndFileTransFieldDbNames.ContainsKey(childRow.ChildUnitId.ToString()))
                    {
                        var fileTypeTransFieldNames = DictUnitIdAndFileTransFieldDbNames[childRow.ChildUnitId.ToString()];

                        foreach (string childFieldName in childRow.DictOneToOneFields.Keys)
                        {
                            if (fileTypeTransFieldNames.Contains(childFieldName))
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
                                var fileTypeTransFieldNames = DictUnitIdAndFileTransFieldDbNames[grandChildUnitId];

                                foreach (AppChildDataDto grandChildRow in childRow.DictOneToManyFields[grandChildUnitId])
                                {
                                    Dictionary<string, object> dictGrandChildKeyValue = grandChildRow.DictOneToOneFields;
                                    foreach (string grandChildFieldName in dictGrandChildKeyValue.Keys)
                                    {
                                        if (fileTypeTransFieldNames.Contains(grandChildFieldName))
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


                    //if (aChildRow.DictGrandUnitDataList != null)
                    //{
                    //    foreach (string grandChildUnitId in aChildRow.DictGrandUnitDataList.Keys)
                    //    {
                    //        if (DictUnitIdAndFileTransFieldDbNames.ContainsKey(grandChildUnitId))
                    //        {
                    //            var fileTypeTransFieldNames = DictUnitIdAndFileTransFieldDbNames[grandChildUnitId];

                    //            foreach (var grandChildRow in aChildRow.DictGrandUnitDataList[grandChildUnitId])
                    //            {
                    //                foreach (string grandChildFieldName in grandChildRow.Keys)
                    //                {
                    //                    if (fileTypeTransFieldNames.Contains(grandChildFieldName))
                    //                    {
                    //                        int? fileId = ControlTypeValueConverter.ConvertValueToInt(grandChildRow[grandChildFieldName]);
                    //                        if (fileId.HasValue && !fileIdList.Contains(fileId.Value))
                    //                        {
                    //                            fileIdList.Add(fileId.Value);
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}


                }
            }

            if (fileIdList.Count > 0)
            {
                aAppformDataDto.DictDocumentIdFileCode = AppFileBL.RetrieveMultipleFileEntityByIds(fileIdList).ToDictionary(o => o.FileId, o => o.FileCode);
            }
        }
    }
}