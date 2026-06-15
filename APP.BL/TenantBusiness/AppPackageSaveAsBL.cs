using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Components.EntityConverter;
using System.Data;
using APP.Framework.Communication;

using System;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using System.Text.RegularExpressions;
    

using APP.Framework;
namespace App.BL
{
    public static partial class AppApplicationImportBL
    {
        public static OperationCallResult<AppTransactionExDto> SaveAsOneWorkflowAutomation(int workflowTransactionId, string newNameSurffix, string newWorkflowName, int? applicationId = null)
        {

            OperationCallResult<AppTransactionExDto> aOperationCallResult = new OperationCallResult<AppTransactionExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            AppApplicationImportSettingDto importSettingDto = null;

            AppTransactionExDto transactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(workflowTransactionId);

            AppTransactionCommandBL.SyncronizeWorkflowCommandNodeTreeFromActionList(transactionExDto);

            if (string.IsNullOrWhiteSpace(newNameSurffix))
            {
                newNameSurffix = "_Copy" + ExtensionMethodhelper.RandomId();
            }

            string newWorkflowTableName = Regex.Replace(newWorkflowName + ExtensionMethodhelper.RandomId(), @"[^0-9a-zA-Z]+", "_");


            string srcDbName = UserDbName;
            string targetDbName = UserDbName;

            string queryDropTempColumnAndTableIfExist = PrepareImportQuery_DropTempTable(targetDbName);
            queryDropTempColumnAndTableIfExist += PrepareImportQuery_Drop_OrgIdColumns(targetDbName);

            string queryCreateImportSettingTable = PrepareImportQuery_CreateImportSettingTable(targetDbName);
            string queryAddOrgIdColumns = PrepareImportQuery_Add_OrgIdColumns(targetDbName);

            string query = PrepareWorkflowSaveAsQuery_ImportSystemConfigTableData(transactionExDto, newNameSurffix);

            query += PrepareWorkflowSaveAsQuery_Update_WorkflowAndTransaction(workflowTransactionId, newNameSurffix, newWorkflowName, applicationId);

            string queryLog = queryDropTempColumnAndTableIfExist
                + queryCreateImportSettingTable
                + queryAddOrgIdColumns
                + query;

            //validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, "Query:\n" + queryLog + "\n"));

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adpater.ExecuteExecuteNonQuery(queryDropTempColumnAndTableIfExist, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(queryCreateImportSettingTable, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(queryAddOrgIdColumns, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Import Application Failed. \n" + ex.ToString()));
                }
            }



            if (!validationResult.HasErrors)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, "Importing Configuration: \n" + "Query:\n" + queryAddOrgIdColumns + "\n" + query));

                importSettingDto = InitializeSaveAsSettingDto(transactionExDto, newNameSurffix, targetDbName, validationResult);
            }

            if (!validationResult.HasErrors)
            {
                validationResult.Merge(PostImported_UpdateFormulaExpression(srcDbName, targetDbName));

                if (!validationResult.HasErrors)
                {
                    validationResult.Merge(PostImported_UpdateTransactionCommand(srcDbName, targetDbName, importSettingDto));

                    if (!validationResult.HasErrors)
                    {
                        validationResult.Merge(PostImported_UpdateTransaction(srcDbName, targetDbName, importSettingDto));

                        if (!validationResult.HasErrors)
                        {
                            validationResult.Merge(PostImported_UpdateFormLayoutItem(srcDbName, targetDbName, importSettingDto));
                        }
                    }
                }
            }



            //if (!validationResult.HasErrors)
            //{  
            //    Dictionary<int, Dictionary<string, string>> dictDataSourceRegIdOrgTableNameAndNewTableName = PrepareUdTableSaveAsDictionary(newNameSurffix, validationResult);

            //    if (!validationResult.HasErrors)
            //    {
            //        validationResult.Merge(SaveAsUDTablesWithNewPrefix(dictDataSourceRegIdOrgTableNameAndNewTableName));
            //    }
            //}

            //validationResult.Merge(SaveAsUDTablesWithNewPrefix(dictDataSourceRegIdOrgTableNameAndNewTableName));


            CreateNewWorkflowRootUnitTable(validationResult, transactionExDto, newWorkflowTableName);

            if (!validationResult.HasErrors)
            {
                SaveImportSettingDto(targetDbName, importSettingDto, validationResult);
            }

            if (!validationResult.HasErrors)
            {
                DropTempTablesAndColumns(targetDbName, validationResult);
            }
            else
            {
                ClearnFailedImportConfigData(targetDbName, validationResult);
            }

            if (!validationResult.HasErrors)
            {
                SynchronizeDataBaseViews(srcDbName, targetDbName, validationResult);
            }

            if (!validationResult.HasErrors)
            {
                // aOperationCallResult.Object = true;
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, "Save As Successful. \n"));

            }


            return aOperationCallResult;
        }

        private static void CreateNewWorkflowRootUnitTable(ValidationResult validationResult, AppTransactionExDto transactionExDto, string newWorkflowTableName)
        {
            if (transactionExDto.RootMasterUnit != null && !string.IsNullOrWhiteSpace(transactionExDto.RootMasterUnit.DataBaseTableName)
                            && transactionExDto.RootMasterUnit.DataBaseTableName.ToLower() != "App_VirtualView".ToLower()
                            && !string.IsNullOrWhiteSpace(newWorkflowTableName))
            {
                string queryCreateUdTablesInOneDatasource = "";
                string orgworkflowTransactionUnitId = transactionExDto.RootMasterUnit.Id.ToString();

                var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(transactionExDto.DataSourceFrom.Value);


                DatabaseTable srcTableDto = databaseFixture.Table(transactionExDto.RootMasterUnit.DataBaseTableName);
                srcTableDto.Name = newWorkflowTableName;

                if (srcTableDto != null)
                {
                    foreach (var column in srcTableDto.Columns)
                    {
                        column.Tag = AppMetaDataSqlTypeConvertBL.ConvertSqlTypeToNetType(column, databaseFixture.SqlServerType);
                    }

                    queryCreateUdTablesInOneDatasource += AppMetaDataBL.PrepareCreateNewTableScript(srcTableDto, databaseFixture);


                    queryCreateUdTablesInOneDatasource += @"                
                        UPDATE targetTable set 
                            DataBaseTableName = '{newWorkflowTableName}'
                        FROM [AppTransactionUnit] as targetTable             
                        WHERE targetTable.Org_TransactionUnitID = {orgworkflowTransactionUnitId};";

                    queryCreateUdTablesInOneDatasource = queryCreateUdTablesInOneDatasource
                        .Replace("{orgworkflowTransactionUnitId}", orgworkflowTransactionUnitId)
                        .Replace("{newWorkflowTableName}", newWorkflowTableName);

                }


                if (!string.IsNullOrWhiteSpace(queryCreateUdTablesInOneDatasource))
                {
                    string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixture, queryCreateUdTablesInOneDatasource);

                    if (!string.IsNullOrWhiteSpace(errorMsg))
                    {
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Create table failed: " + errorMsg));
                    }
                    else
                    {
                        AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(transactionExDto.DataSourceFrom.Value);
                    }
                }
            }
        }

        private static AppApplicationImportSettingDto InitializeSaveAsSettingDto(AppTransactionExDto transactionExDto, string newNameSurffix, string targetDbName, ValidationResult validationResult)
        {

            AppApplicationImportSettingDto importSettingDto = new AppApplicationImportSettingDto();




            try
            {
                string targetDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, targetDbName);
                DatabaseFixture databaseFixture = new DatabaseFixture(targetDbConnectionString, EmSqlType.SqlServer);

                foreach (string systemTableName in importSettingDto.DictSystemTableOrgIdAndNewId.Keys)
                {
                    PrepareImportSettingDto_ProcessOneAppTable(targetDbName, systemTableName, validationResult, importSettingDto, databaseFixture);
                }



            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Prepare Import Log Failed. \n" + ex.ToString()));
            }

            return importSettingDto;
        }


        //private static Dictionary<int, Dictionary<string, string>> PrepareUdTableSaveAsDictionary(string newNameSurffix, ValidationResult validationResult)
        //{
        //    Dictionary<int, Dictionary<string, string>> dictDataSourceRegIdOrgTableNameAndNewTableName = new Dictionary<int, Dictionary<string, string>>();
        //    string queryGetTableNames = @"                
        //        select distinct TableName, DataSourceFrom 
        //        FROM
        //        (
        //        select AppTransactionUnit.DataBaseTableName as TableName, AppTransaction.DataSourceFrom from [AppTransactionUnit] inner join [AppTransaction] on [AppTransactionUnit].TransactionID = [AppTransaction].TransactionID
        //        where Org_TransactionUnitID is not null and DataBaseTableName is not null and DataBaseTableName <> ''
        //        UNION
        //        select TableName, DataSourceFrom from [AppEntityInfo] where Org_EntityInfoID is not null and EntityType = 1 and TableName is not null and TableName <> ''
        //        ) as TableNames

        //    ";

        //    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        try
        //        {
        //            DataTable dtUserTableName = adpater.ExecuteDataTableRetrievalQuery(queryGetTableNames, new List<SqlParameter>());

        //            //List<string> userTableNameList = new List<string>();

        //            foreach (DataRow dataRow in dtUserTableName.Rows)
        //            {
        //                string newTableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["TableName"]).Trim().ToLower();
        //                int? datasourceFrom = ControlTypeValueConverter.ConvertValueToInt(dataRow["DataSourceFrom"]);

        //                if (!string.IsNullOrWhiteSpace(newTableName) && datasourceFrom.HasValue)
        //                {
        //                    if (!newTableName.StartsWith("app"))
        //                    {
        //                        if (!dictDataSourceRegIdOrgTableNameAndNewTableName.ContainsKey(datasourceFrom.Value))
        //                        {
        //                            dictDataSourceRegIdOrgTableNameAndNewTableName.Add(datasourceFrom.Value, new Dictionary<string, string>());
        //                        }

        //                        Dictionary<string, string> dictOrgTableNameOrgNewTableName = dictDataSourceRegIdOrgTableNameAndNewTableName[datasourceFrom.Value];

        //                        if (!dictOrgTableNameOrgNewTableName.ContainsKey(newTableName))
        //                        {
        //                            if (newTableName.EndsWith(newNameSurffix))
        //                            {
        //                                string orgTableName = newTableName.Substring(0, newTableName.Length - newNameSurffix.Length);

        //                                dictOrgTableNameOrgNewTableName.Add(orgTableName, newTableName);
        //                            }


        //                        }
        //                    }
        //                }
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, ex.ToString()));
        //        }
        //    }

        //    return dictDataSourceRegIdOrgTableNameAndNewTableName;
        //}

        private static string PrepareWorkflowSaveAsQuery_ImportSystemConfigTableData(AppTransactionExDto transactionExDto, string newNameSurffix)
        {
            string query = "";

            string srcDbName = UserDbName;
            string targetDbName = UserDbName;


            List<int> transactionIdList = new List<int>();
            List<int> searchIdList = new List<int>();
            List<int> datasetIdList = new List<int>();
            List<int> entityIdList = new List<int>();


            transactionIdList.Add((int)transactionExDto.Id);
            if (transactionExDto.WorkflowCommandNodeTree != null)
            {
                foreach (var workflowNode in transactionExDto.WorkflowCommandNodeTree)
                {
                    FindWorkflowNodeConfigIds(workflowNode, transactionIdList, searchIdList, datasetIdList);
                }
            }

            transactionIdList = transactionIdList.Distinct().ToList();
            searchIdList = searchIdList.Distinct().ToList();
            datasetIdList = datasetIdList.Distinct().ToList();


            query += PrepareImportQuery_DropTempTable(targetDbName);

            query += PrepareImportQuery_CreateTempTable(targetDbName);

            query += PrepareWorkflowSaveAsQuery_PopulateTempTableData(transactionIdList, searchIdList, datasetIdList, entityIdList);

            //query += (PrepareImportQuery_AppEntityInfo(srcDbName, targetDbName).Replace("@NewApplicationId", "[SaasApplicationID]").Replace("@UserDbDataSourceId", "[DataSourceFrom]"));

            //query += PrepareImportQuery_AppEntitySimpleListValue(srcDbName, targetDbName);

            //query += (PrepareImportQuery_AppDataSet(srcDbName, targetDbName).Replace("@NewApplicationId", "[SaasApplicationID]").Replace("@UserDbDataSourceId", "[DataSourceFrom]"));

            //query += PrepareImportQuery_AppDataSetParameter(srcDbName, targetDbName);

            //query += (PrepareImportQuery_AppSearchView(srcDbName, targetDbName).Replace("@NewApplicationId", "[SaasApplicationID]"));

            //query += PrepareImportQuery_AppSearchViewField(srcDbName, targetDbName);

            //query += (PrepareImportQuery_AppSearch(srcDbName, targetDbName, newNameSurffix).Replace("@NewApplicationId", "[SaasApplicationID]"));

            //query += PrepareImportQuery_AppSearchField(srcDbName, targetDbName);

            query += (PrepareImportQuery_AppTransaction(srcDbName, targetDbName, newNameSurffix).Replace("@NewApplicationId", "[SaasApplicationID]").Replace("@UserDbDataSourceId", "[DataSourceFrom]"));

            query += PrepareImportQuery_AppTransactionUnit(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionField(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionFieldAggFunction(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionUnitFormula(srcDbName, targetDbName);

            query += PrepareImportQuery_AppConditionalAction(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionDataTransferSetting(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionSaveAsMapping(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionDataLoad(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTranscationDataLoadFieldMapping(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppMessage(srcDbName, targetDbName);

            query += PrepareImportQuery_AppProjectWorkFlowAction(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppFormLinkTarget(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppViewLinkedSeaechOrUrl(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppTransactionUnitLinkedSearch(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppTransactionUnitSearchFieldMapping(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppTransactionUnitSearchViewFieldMapping(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppTransactionNavigation(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppTranscationReport(srcDbName, targetDbName);

            query += (PrepareImportQuery_AppForm(srcDbName, targetDbName).Replace("@NewApplicationId", "[SaasApplicationID]"));

            query += PrepareImportQuery_AppFormLayoutItem(srcDbName, targetDbName);

            //query += (PrepareImportQuery_AppApplicationAssetsItem(srcDbName, targetDbName).Replace("@NewApplicationId", "[ApplicationID]")); ;




            //2. Post Import

            //query += PreparePostSaveAsQuery_Update_AppEntityInfo(srcDbName, targetDbName, newNameSurffix);
            //query += PreparePostImportQuery_Update_AppSearchView(srcDbName, targetDbName);
            //query += PreparePostImportQuery_Update_AppSearchViewField(srcDbName, targetDbName);
            //query += PreparePostImportQuery_Update_AppSearch(srcDbName, targetDbName);
            //query += PreparePostImportQuery_Update_AppSearchField(srcDbName, targetDbName);

            query += PreparePostImportQuery_Update_AppTransaction(srcDbName, targetDbName);
            query += PreparePostSaveAsQuery_Update_AppTransactionUnit(srcDbName, targetDbName, "");
            query += PreparePostImportQuery_Update_AppTransactionField(srcDbName, targetDbName);



            return query;
        }


        private static string PrepareWorkflowSaveAsQuery_Update_WorkflowAndTransaction(int workflowTransactionId, string newNameSurffix, string newWorkflowName, int? applicationId = null)
        {
            string query = "";

            if (!string.IsNullOrWhiteSpace(newWorkflowName))
            {
                query += @"                
                UPDATE targetTable set 
			                TransactionName = '{newWorkflowName}'			                        
                FROM [AppTransaction] as targetTable 
		                WHERE targetTable.Org_TransactionID = {workflowTransactionId};	

            ";
            }


            if (applicationId.HasValue)
            {
                query += @"                
                UPDATE targetTable set 
			                SaasApplicationID = '{applicationId}'			                		             
                FROM [AppTransaction] as targetTable 
		                WHERE targetTable.Org_TransactionID is not null;

            ";
            }


            query = query.Replace("{workflowTransactionId}", workflowTransactionId.ToString())
                .Replace("{newWorkflowName}", newWorkflowName)
                .Replace("{applicationId}", applicationId.Value.ToString());

            return query;
        }


        private static void FindWorkflowNodeConfigIds(AppProjectWorkFlowActionDto workflowNode, List<int> transactionIdList, List<int> searchIdList, List<int> datasetIdList)
        {
            if (workflowNode.CommandTransactionId.HasValue)
            {
                transactionIdList.Add(workflowNode.CommandTransactionId.Value);

                if (workflowNode.ActionAttribute != null)
                {
                    if (workflowNode.ActionAttribute.IsBatchCommand)
                    {
                        if (workflowNode.ActionAttribute.BatchCommandSourceFromType.HasValue
                            && workflowNode.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.Search
                            && workflowNode.ActionAttribute.BatchCommandSearchId.HasValue)
                        {
                            searchIdList.Add(workflowNode.ActionAttribute.BatchCommandSearchId.Value);
                        }
                        else if (workflowNode.ActionAttribute.BatchCommandSourceFromType.HasValue
                            && workflowNode.ActionAttribute.BatchCommandSourceFromType.Value == (int)EmAppBatchCommandSourceFrom.DataSet
                            && workflowNode.ActionAttribute.BatchCommandDataSetId.HasValue)
                        {
                            datasetIdList.Add(workflowNode.ActionAttribute.BatchCommandDataSetId.Value);
                        }
                    }
                }


                if (workflowNode.WorkflowChildTreeNodes != null)
                {
                    foreach (var childNode in workflowNode.WorkflowChildTreeNodes)
                    {
                        FindWorkflowNodeConfigIds(childNode, transactionIdList, searchIdList, datasetIdList);
                    }
                }
            }
        }

        private static string PreparePostSaveAsQuery_Update_AppEntityInfo(string srcDbName, string targetDbName, string newTableNamePrefix)
        {
            string query = @"               
               
                UPDATE targetTable set 
                            TableName = targetTable.TableName + '{newTableNamePrefix}'
                FROM [{targetDbName}].[dbo].[AppEntityInfo] as targetTable                         
		                inner join [{srcDbName}].[dbo].[AppEntityInfo] as srcTable  
			                on (targetTable.Org_EntityInfoID = srcTable.EntityInfoID)
                WHERE targetTable.TableName is not null and targetTable.TableName <> '' and (targetTable.EntityType = 1) and (targetTable.IsSystemDefine is null or targetTable.IsSystemDefine=0)
                    and targetTable.TableName not like '%App'
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName)
                .Replace("{newTableNamePrefix}", newTableNamePrefix);

            return query;
        }

        private static string PreparePostSaveAsQuery_Update_AppTransactionUnit(string srcDbName, string targetDbName, string newTableNamePrefix)
        {
            string query = @"                
                UPDATE targetTable set 
			                ParentTransactionUnitID = tempParentTransactionUnitID.New_TransactionUnitId,
			                AvailableSourceUnitID = tempAvailableSourceUnitID.New_TransactionUnitId
                            
                FROM [{targetDbName}].[dbo].[AppTransactionUnit] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppTransactionUnit] as srcTable  
			                on (targetTable.Org_TransactionUnitID = srcTable.TransactionUnitID)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempParentTransactionUnitID
			                on (srcTable.ParentTransactionUnitID = tempParentTransactionUnitID.Org_TransactionUnitId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempAvailableSourceUnitID
			                on (srcTable.AvailableSourceUnitID = tempAvailableSourceUnitID.Org_TransactionUnitId)		


                UPDATE targetTable set 
                            DataBaseTableName = targetTable.DataBaseTableName + '{newTableNamePrefix}'
                FROM [{targetDbName}].[dbo].[AppTransactionUnit] as targetTable 
                        inner join [{targetDbName}].[dbo].[AppTransaction] as appTransaction
                             on (targetTable.TransactionId = appTransaction.TransactionId)
		                inner join [{srcDbName}].[dbo].[AppTransactionUnit] as srcTable  
			                on (targetTable.Org_TransactionUnitID = srcTable.TransactionUnitID)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempParentTransactionUnitID
			                on (srcTable.ParentTransactionUnitID = tempParentTransactionUnitID.Org_TransactionUnitId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempAvailableSourceUnitID
			                on (srcTable.AvailableSourceUnitID = tempAvailableSourceUnitID.Org_TransactionUnitId)		
                WHERE targetTable.DataBaseTableName is not null and targetTable.DataBaseTableName <> '' and (appTransaction.FolderUsageType is null)
                        and targetTable.DataBaseTableName not like 'App%'
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName)
                 .Replace("{newTableNamePrefix}", newTableNamePrefix);

            return query;
        }

        private static string BuildSqlInClauseFromIdList(List<int> idList)
        {
            string toReturn = "";

            if (idList != null && idList.Count > 0)
            {
                toReturn = " in (" + string.Join(", ", idList) + ") ";
            }
            else
            {
                toReturn = " in (-1) ";
            }

            return toReturn;
        }

        private static string PrepareWorkflowSaveAsQuery_PopulateTempTableData(List<int> transactionIdList, List<int> searchIdList, List<int> datasetIdList, List<int> entityIdList)
        {
            string query = "";

            string srcDbName = UserDbName;
            string targetDbName = UserDbName;

            string transactionId_inclause = BuildSqlInClauseFromIdList(transactionIdList);
            string searchId_inclause = BuildSqlInClauseFromIdList(searchIdList);
            string datasetId_inclause = BuildSqlInClauseFromIdList(datasetIdList);
            string entityId_inclause = BuildSqlInClauseFromIdList(entityIdList);

            query += @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_Transaction](Org_TransactionId)
                SELECT DISTINCT TransactionID FROM
                (
	                select TransactionID from [{srcDbName}].[dbo].[AppTransaction] where TransactionID " + transactionId_inclause + @"
	                
                ) as TransactionIds

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_TransactionUnit](Org_TransactionUnitId, Org_TransactionId)
                SELECT TransactionUnitID, TransactionID FROM [{srcDbName}].[dbo].[AppTransactionUnit] where TransactionID in
                (
	                select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction]
                )

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_TransactionField](Org_TransactionFieldId, Org_TransactionUnitId, Org_TransactionId)
                SELECT TransactionFieldID, TransactionUnitID, Org_TransactionId
                FROM [{srcDbName}].[dbo].[AppTransactionField] as transField inner join  [{targetDbName}].[dbo].[Temp_Import_TransactionUnit] as temp_unit
	                on (transField.TransactionUnitID = temp_unit.Org_TransactionUnitId)
                where TransactionUnitID in
                (
	                select Org_TransactionUnitId from [{targetDbName}].[dbo].[Temp_Import_TransactionUnit]
                )


                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_Search](Org_SearchId)
                SELECT DISTINCT SearchID FROM
                (
	                select SearchID from [{srcDbName}].[dbo].[AppSearch] where SearchID " + searchId_inclause + @"                
                ) as SearchIDs

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_SearchField](Org_SearchFieldId, Org_SearchId)
                SELECT SearchFieldID, SearchID FROM [{srcDbName}].[dbo].[AppSearchField] where SearchID in
                (
	                select Org_SearchId from [{targetDbName}].[dbo].[Temp_Import_Search]
                )



                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_DataSet](Org_DataSetId)
                SELECT DISTINCT DataSetId FROM
                (
	                select DataSetId from [{srcDbName}].[dbo].[AppDataSet] where DataSetId " + datasetId_inclause + @"     
                    UNION
	                select DISTINCT DataSetId FROM [{srcDbName}].[dbo].[AppSearch] WHERE SearchID in (
		                select Org_SearchId from [{targetDbName}].[dbo].[Temp_Import_Search] 
	                )
	                UNION
	                select DISTINCT DataSetId FROM [{srcDbName}].[dbo].[AppTransactionDataLoad] WHERE TransactionUnitID in (
		                select Org_TransactionUnitId from [{targetDbName}].[dbo].[Temp_Import_TransactionUnit]
	                )
                ) as DataSetIds
                WHERE DataSetId IS NOT NULL

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_SearchView](Org_SearchViewId)
                select SearchViewID FROM [{srcDbName}].[dbo].[AppSearchView] WHERE DataSetID in (
	                select Org_DataSetId from [{targetDbName}].[dbo].[Temp_Import_DataSet] 
                )


                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_SearchViewField](Org_SearchViewFieldId, Org_SearchViewId)
                SELECT SearchViewFieldID, SearchViewID FROM [{srcDbName}].[dbo].[AppSearchViewField] where SearchViewID in
                (
	                select Org_SearchViewId from [{targetDbName}].[dbo].[Temp_Import_SearchView]
                )

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_Entity](Org_EntityInfoID, Org_EntityCode, New_EntityCode)
                SELECT EntityInfoID, EntityCode, EntityCode FROM [{srcDbName}].[dbo].AppEntityInfo WHERE EntityInfoID in (
	                SELECT DISTINCT EntityInfoID FROM
	                (
		                select EntityInfoID from [{srcDbName}].[dbo].AppEntityInfo where EntityInfoID " + entityId_inclause + @"   
                        UNION
		                select LogicalDisplayEntityID as EntityInfoID from [{srcDbName}].[dbo].[AppTransaction] 
			                WHERE LogicalDisplayEntityID is not null and TransactionID in (
				                select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction]
			                )
		                UNION
		                select EntityID as EntityInfoID from [{srcDbName}].[dbo].[AppTransactionField] 
			                WHERE EntityID is not null and TransactionUnitID in 
			                (
				                select Org_TransactionUnitId from [{targetDbName}].[dbo].[Temp_Import_TransactionUnit]
			                )
		                UNION
		                select EntityID as EntityInfoID from [{srcDbName}].[dbo].[AppSearchField] 
			                WHERE EntityID is not null and SearchID in 
			                (			
				                select Org_SearchId from [{targetDbName}].[dbo].[Temp_Import_Search]	
			                )
		                UNION
		                select EntityID as EntityInfoID from [{srcDbName}].[dbo].[AppSearchViewField] 
			                WHERE EntityID is not null and SearchViewID in 
			                (			
				                select Org_SearchViewId from [{targetDbName}].[dbo].[Temp_Import_SearchView]	
			                )
	                ) as EntityInfoIDs
                )                 
                

                UPDATE [{targetDbName}].[dbo].[Temp_Import_Entity] set New_EntityId = entityInfo.EntityInfoID
                FROM [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity inner join [{targetDbName}].[dbo].[AppEntityInfo] as entityInfo
                 on (tempEntity.New_EntityCode = entityInfo.EntityCode)
                WHERE entityInfo.IsSystemDefine = 1;
                
                UPDATE [{targetDbName}].[dbo].[Temp_Import_Entity] set New_EntityId = targetTable.EntityInfoID
                FROM [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity inner join [{srcDbName}].[dbo].[AppEntityInfo] as srcTable
                        on (tempEntity.Org_EntityInfoID = srcTable.EntityInfoID) 
                    inner join [{targetDbName}].[dbo].[AppEntityInfo] as targetTable
                        on (srcTable.GlobalGuid is not null and srcTable.GlobalGuid = targetTable.GlobalGuid)
                WHERE (srcTable.IsSystemDefine is null or srcTable.IsSystemDefine = 0);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_Entity] set New_EntityCode = Org_EntityCode + ' ' + (SELECT CONVERT(nvarchar(5), CONVERT(varchar(255), NEWID())))
                FROM [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity inner join [{targetDbName}].[dbo].[AppEntityInfo] as entityInfo
                    on (tempEntity.New_EntityCode = entityInfo.EntityCode)
                WHERE tempEntity.New_EntityId is null and (entityInfo.IsSystemDefine is null or entityInfo.IsSystemDefine = 0);


                

               

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_Command] (Org_CommandId)
                SELECT WorkFlowActionID FROM [{srcDbName}].[dbo].[AppProjectWorkFlowAction] where CommandTransactionID is not null and CommandTransactionID in
                (
	                select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction]
                );

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_Message] (Org_MessageId)
                SELECT MessageId FROM [{srcDbName}].[dbo].AppMessage 
                WHERE IsPredefinedTemplate = 1 
	                and 
	                (
		                TransactionID in (
			                select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction] 
		                )
		                or MessageID in (
			                select MessageTemplateID from [{srcDbName}].[dbo].AppProjectWorkFlowAction
			                where CommandTransactionID in (
				                select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction] 
			                ) and MessageTemplateID is not null
		                )
	                )
                
            ";

            query = query.Replace("{srcDbName}", srcDbName).Replace("{targetDbName}", targetDbName);

            return query;
        }



        private static ValidationResult SaveAsUDTablesWithNewPrefix(Dictionary<int, Dictionary<string, string>> dictDataSourceRegIdOrgTableNameAndNewTableName)
        {
            ValidationResult validationResult = new ValidationResult();


            if (dictDataSourceRegIdOrgTableNameAndNewTableName != null)
            {
                SaveAsUDTables_ValidateTableNames(dictDataSourceRegIdOrgTableNameAndNewTableName, validationResult);

                if (!validationResult.HasErrors)
                {
                    foreach (int dataSourceRegisterId in dictDataSourceRegIdOrgTableNameAndNewTableName.Keys)
                    {
                        Dictionary<string, string> dictOrgAndNewTableName = dictDataSourceRegIdOrgTableNameAndNewTableName[dataSourceRegisterId];

                        if (dictOrgAndNewTableName.Count > 0)
                        {
                            string queryCreateUdTablesInOneDatasource = "";

                            var databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

                            foreach (var kvPari in dictOrgAndNewTableName)
                            {
                                string orgTableName = kvPari.Key;
                                string newTableName = kvPari.Value;

                                if (!string.IsNullOrWhiteSpace(orgTableName) && !string.IsNullOrWhiteSpace(newTableName))
                                {
                                    DatabaseTable srcTableDto = databaseFixture.Table(orgTableName);

                                    if (srcTableDto != null)
                                    {
                                        foreach (var column in srcTableDto.Columns)
                                        {
                                            column.Tag = AppMetaDataSqlTypeConvertBL.ConvertSqlTypeToNetType(column, databaseFixture.SqlServerType);
                                        }

                                        queryCreateUdTablesInOneDatasource += AppMetaDataBL.PrepareCreateNewTableScript(srcTableDto, databaseFixture);
                                    }
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(queryCreateUdTablesInOneDatasource))
                            {
                                string errorMsg = AppMetaDataBL.ExecSQlCommand(databaseFixture, queryCreateUdTablesInOneDatasource);

                                if (!string.IsNullOrWhiteSpace(errorMsg))
                                {
                                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Create table failed: " + errorMsg));
                                }
                            }
                        }
                    }
                }
            }


            return validationResult;
        }

        private static void SaveAsUDTables_ValidateTableNames(Dictionary<int, Dictionary<string, string>> dictDataSourceRegIdOrgTableNameAndNewTableName, ValidationResult validationResult)
        {
            foreach (int dataSourceRegisterId in dictDataSourceRegIdOrgTableNameAndNewTableName.Keys)
            {
                Dictionary<string, string> dictOrgAndNewTableName = dictDataSourceRegIdOrgTableNameAndNewTableName[dataSourceRegisterId];

                if (dictOrgAndNewTableName.Count > 0)
                {
                    var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId);

                    foreach (var kvPari in dictOrgAndNewTableName)
                    {
                        string orgTableName = kvPari.Key;
                        string newTableName = kvPari.Value;

                        if (!string.IsNullOrWhiteSpace(orgTableName) && !string.IsNullOrWhiteSpace(newTableName))
                        {
                            if (dataBaseFixture.IsTableExist(orgTableName))
                            {
                                if (!dataBaseFixture.IsTableExist(newTableName))
                                {

                                }
                                else
                                {
                                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "New table name " + newTableName + " alraedy exists."));
                                }
                            }
                            else
                            {
                                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Cannot find table " + orgTableName));
                            }
                        }
                    }


                }
            }
        }
    }
}