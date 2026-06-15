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

using APP.Framework;
namespace App.BL
{
    public static partial class AppApplicationImportBL
    {


        public static OperationCallResult<bool> UnistallApplicationFromCurrentUserDB(int targetApplicatoinId)
        {
            var result = UnistallApplicationFromTargetDB(UserDbName, targetApplicatoinId);

            if (result.IsSuccessful)
            {
                if (UserDbDataSourceId > 0)
                {
                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(UserDbDataSourceId);
                }
            }

            return result;
        }

        public static OperationCallResult<bool> UnistallApplicationFromTargetDB(string targetDbName, int targetApplicatoinId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            //AppApplicationImportSettingDto importSettingDto = RetriveOneImportSettingDtoFromTargetDB(targetDbName, targetApplicatoinId, validationResult);

            if (!validationResult.HasErrors)
            {



                string query_CreateDeleteIdTempTable = Unistall_CreateDeleteIdsTempTable(targetDbName);
                string query_PopulateDeleteIdsTempTable = "";


                string query_ExecuteDelete = Unistall_ReprepareDeleteConfigDataQuery(validationResult, targetDbName);
                string query_DeleteMenu = Unistall_PrepareDeleteMenuScript(targetDbName);
                string query_DropDeleteIdTempTable = Unistall_DropDeleteIdTempTable(targetDbName);
                string query_DropUserTables = "";



                try
                {

                    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adpater.ExecuteExecuteNonQuery(query_CreateDeleteIdTempTable, new List<System.Data.SqlClient.SqlParameter>());
                        }
                        catch (Exception ex)
                        {
                            adpater.Rollback();

                            string queryAll = query_CreateDeleteIdTempTable;

                            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                                "Create DeleteId TempTable Failed. \n" + ex.ToString() + "\n\nQuery:\n\n" + queryAll + "\n"));
                        }

                        if (!validationResult.HasErrors)
                        {

                            try
                            {
                                query_PopulateDeleteIdsTempTable = Unistall_PopulateDeleteIdsTempTableQuery(validationResult, targetDbName, targetApplicatoinId);
                                adpater.ExecuteExecuteNonQuery(query_PopulateDeleteIdsTempTable, new List<System.Data.SqlClient.SqlParameter>());
                                string query_PopulateDeleteIdsTempTable_Entity = Unistall_PopulateDeleteIdsTempTable_AppEntityInfo(validationResult, targetDbName, targetApplicatoinId);


                                if (!string.IsNullOrWhiteSpace(query_PopulateDeleteIdsTempTable_Entity))
                                {
                                    query_PopulateDeleteIdsTempTable += "\n" + query_PopulateDeleteIdsTempTable_Entity;
                                    adpater.ExecuteExecuteNonQuery(query_PopulateDeleteIdsTempTable_Entity, new List<System.Data.SqlClient.SqlParameter>());
                                }


                                string query_PopulateDeleteIdsTempTable_Message = Unistall_PopulateDeleteIdsTempTable_AppMessage(validationResult, targetDbName);

                                if (!string.IsNullOrWhiteSpace(query_PopulateDeleteIdsTempTable_Message))
                                {
                                    query_PopulateDeleteIdsTempTable += "\n" + query_PopulateDeleteIdsTempTable_Message;
                                    adpater.ExecuteExecuteNonQuery(query_PopulateDeleteIdsTempTable_Message, new List<System.Data.SqlClient.SqlParameter>());
                                }

                            }
                            catch (Exception ex)
                            {
                                adpater.Rollback();
                                string queryAll = query_CreateDeleteIdTempTable
                                    + "\n\n" + query_PopulateDeleteIdsTempTable;

                                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                                    "Populate DeleteIds TempTable Failed. \n" + ex.ToString() + "\n\nQuery:\n\n" + queryAll + "\n"));
                            }
                        }


                        if (!validationResult.HasErrors)
                        {
                            query_DropUserTables = Unistall_PrepareDropUserTablesQuery(validationResult, targetDbName);

                            if (!string.IsNullOrWhiteSpace(query_DropUserTables))
                            {

                                try
                                {
                                    adpater.ExecuteExecuteNonQuery(query_DropUserTables, new List<System.Data.SqlClient.SqlParameter>());
                                }
                                catch (Exception ex)
                                {
                                    adpater.Rollback();

                                    string queryAll = query_CreateDeleteIdTempTable
                                        + "\n\n" + query_PopulateDeleteIdsTempTable
                                        + "\n\n" + query_DropUserTables;

                                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                                            "Execute Drop User Tables Failed. \n" + ex.ToString() + "\n\nQuery:\n\n" + queryAll + "\n"));
                                }

                            }
                        }

                        if (!validationResult.HasErrors)
                        {
                            if (!string.IsNullOrWhiteSpace(query_ExecuteDelete))
                            {

                                try
                                {
                                    adpater.ExecuteExecuteNonQuery(query_ExecuteDelete, new List<System.Data.SqlClient.SqlParameter>());
                                    adpater.ExecuteExecuteNonQuery(query_DeleteMenu, new List<System.Data.SqlClient.SqlParameter>());
                                    
                                }
                                catch (Exception ex)
                                {
                                    adpater.Rollback();

                                    string queryAll = query_CreateDeleteIdTempTable
                                        + "\n\n" + query_PopulateDeleteIdsTempTable
                                        + "\n\n" + query_DropUserTables
                                        + "\n\n" + query_ExecuteDelete
                                        + "\n\n" + query_DeleteMenu;

                                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                                        "Execute Delete Query Failed. \n" + ex.ToString() + "\n\nQuery:\n\n" + queryAll + "\n"));
                                }
                            }
                        }

                        if (!validationResult.HasErrors)
                        {
                            try
                            {
                                adpater.ExecuteExecuteNonQuery(query_DropDeleteIdTempTable, new List<System.Data.SqlClient.SqlParameter>());

                            }
                            catch (Exception ex)
                            {
                                adpater.Rollback();

                                string queryAll = query_CreateDeleteIdTempTable
                                    + "\n\n" + query_PopulateDeleteIdsTempTable
                                    + "\n\n" + query_DropUserTables
                                    + "\n\n" + query_ExecuteDelete
                                    + "\n\n" + query_DropDeleteIdTempTable;

                                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                                        "Drop DeleteId TempTable Failed. \n" + ex.ToString() + "\n\nQuery:\n\n" + queryAll + "\n"));
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                        "Unistall Application Failed. \n" + ex.ToString()));
                }

                //try
                //{
                //    string targetDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, targetDbName);
                //    DatabaseFixture databaseFixture = new DatabaseFixture(targetDbConnectionString, EmSqlType.SqlServer);


                //    foreach (string tableName in importSettingDto.UninstallTableNamesInOrder)
                //    {
                //        if (importSettingDto.DictSystemTableOrgIdAndNewId.ContainsKey(tableName))
                //        {

                //            Dictionary<int, int> dictOrgIdAndNewId = importSettingDto.DictSystemTableOrgIdAndNewId[tableName];

                //            if (dictOrgIdAndNewId.Keys.Count > 0)
                //            {
                //                DatabaseTable tableDto = databaseFixture.Table(tableName);

                //                DatabaseColumn pkDto = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                //                if (pkDto != null)
                //                {
                //                    string pkColumnName = pkDto.Name;

                //                    string deleteOneTableQuery = "";

                //                    string queryPrepareOneTalbeDeleteIdInTempTable = @"
                //                        DELETE FROM [" + targetDbName + @"].[dbo].[Temp_DeleteId];
                //                    ";

                //                    foreach (int pkId in dictOrgIdAndNewId.Values.Distinct())
                //                    {
                //                        queryPrepareOneTalbeDeleteIdInTempTable += @"
                //                                    INSERT INTO [" + targetDbName + @"].[dbo].[Temp_DeleteId] (DeleteId) 
                //                                    VALUES (" + pkId + @");
                //                            ";
                //                    }


                //                    string preCleanOneTableFkDependentQuery = "";

                //                    if (tableName.ToLower() == "AppListMenu".ToLower())
                //                    {
                //                        deleteOneTableQuery += @"
                //                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecurityUserListMenu] WHERE MenuID in (
                //                                        select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                                ";
                //                    }
                //                    else if (tableName.ToLower() == "AppForm".ToLower())
                //                    {
                //                        deleteOneTableQuery += @"
                //                                UPDATE [" + targetDbName + @"].[dbo].[AppTransaction] SET FormID = null WHERE FormID in (
                //                                        select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                                ";
                //                        deleteOneTableQuery += @"
                //                                UPDATE [" + targetDbName + @"].[dbo].[AppTransaction] SET PrintFormID = null WHERE PrintFormID in (
                //                                        select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                                ";
                //                    }
                //                    else if (tableName.ToLower() == "AppEntityInfo".ToLower())
                //                    {
                //                        deleteOneTableQuery += @"
                //                            DELETE FROM [" + targetDbName + @"].[dbo].[AppEntitySimpleListValue] WHERE EntityInfoID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppSearchView".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppSearchView] 
                //                                set WhereUsedDefaultViewId = null
                //                               ,CatalogueSearchID = null
                //                               ,TransactionID = null
                //                               ,ProductDetaiViewMapUnitID  = null
                //                               ,UpdateTransctionID = null
                //                               ,UpdateBaseTranscationUnitID =null
                //                               ,FilterSearchID = null
                //                               ,HierachyParentViewID = null
                //                            where SearchViewID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );

                //                            update [" + targetDbName + @"].[dbo].[AppFormLinkTarget] 
                //                            set SearchViewID = null,
                //                                SourceViewColumnID1 = null, 
                //                             SourceViewColumnID2 = null, 
                //                             SourceViewColumnID3 = null,
                //                             TargetSearchFieldID1 = null, 
                //                             TargetSearchFieldID2 = null, 
                //                             TargetSearchFieldID3 = null, 
                //                             RowDisplayViewColumnID = null, 
                //                             SourceConditionViewColumnID = null, 
                //                             DataTransferSettingID = null, 
                //                             LinkTargetSearchID = null, 
                //                             LinkTargetTransactionID = null
                //                            WHERE SearchViewID is not null and SearchViewID in (	                                               
                //                          select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]	                                                
                //                            )
                //                        ";
                //                    }

                //                    else if (tableName.ToLower() == "AppSearchViewField".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppSearchViewField] 
                //                                set MappingSearchFieldID = NULL
                //                           ,MassUpdateTransactionFieldID = NULL
                //                                    ,PullCriteriaAsDefaultValueSearchFieldID = NULL
                //                           ,JoinToParentViewFieldID = NULL		
                //                            where SearchViewFieldID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );

                //                            update [" + targetDbName + @"].[dbo].[AppFormLinkTarget] 
                //                            set SourceViewColumnID1 = null, 
                //                             SourceViewColumnID2 = null, 
                //                             SourceViewColumnID3 = null,
                //                             TargetSearchFieldID1 = null, 
                //                             TargetSearchFieldID2 = null, 
                //                             TargetSearchFieldID3 = null, 
                //                             RowDisplayViewColumnID = null, 
                //                             SourceConditionViewColumnID = null, 
                //                             DataTransferSettingID = null, 
                //                             LinkTargetSearchID = null, 
                //                             LinkTargetTransactionID = null
                //                            WHERE SearchViewID is not null and SearchViewID in (
                //                             select SearchViewID from [" + targetDbName + @"].[dbo].AppSearchViewField 
                //                             where SearchViewFieldID in (
                //                              select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                             )
                //                            )
                //                        ";
                //                    }

                //                    else if (tableName.ToLower() == "AppSearch".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppSearch] 
                //                                set WhereUsedSearchID = NULL
                //                            where SearchID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppSearchField".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppSearchField] 
                //                                set ParentFieldID = NULL
                //                           ,MasterEntityFieldlID = NULL	
                //                            where SearchFieldID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppTransaction".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppTransaction] 
                //                                set FolderTransactionID = NULL
                //                           ,MasterTransactionID = NULL		
                //                            where TransactionId in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );

                //                            update [" + targetDbName + @"].[dbo].AppTransactionDataTransferSetting 
                //                            set TransactionID = null	
                //                            WHERE TransactionID in (
                //                             select [DeleteId] FROM [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                            );

                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppTransactionUnit".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppTransactionUnit] 
                //                                set ParentTransactionUnitID = NULL
                //                           ,AvailableSourceUnitID = NULL
                //                            where TransactionUnitID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );

                //                            update [" + targetDbName + @"].[dbo].AppTransactionSaveAsMapping 
                //                            set TransactionID = null,
                //                             MappingUnitId = null,
                //                             DataTransferSettingID = null
                //                            WHERE MappingUnitId in (
                //                             select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                            )
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppTransactionField".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable +
                //                            @"
                //                            update [" + targetDbName + @"].[dbo].[AppTransactionField] 
                //                                set [DDLParentLevelID] = NULL,
                //                                   [MasterEntityFieldlID] = NULL,
                //                                   [ChildUnitSubscribeParentFieldID] = NULL,
                //                                   [ParentUnitSubscribeChildAggFunctionID]=NULL,
                //                                   [MatrixKeyTransactionFieldId]=NULL,
                //                                   [MatrixForeignKeyFieldID]=NULL,
                //                                   [DdlForeignUnitID] =NULL,
                //                                   [FileControlTypeFolderTransactionID]=NULL,
                //                                   [LinkToParentPrimaryKeyFieldID] =NULL,
                //                                   [SiblingUnitLogicalKeyFieldID]=NULL,
                //                                   [HostFormLayoutItemID] =NULL,
                //                                   [OnChangeTriggerToCommandID]=NULL
                //                            where TransactionFieldID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );

                //                            update [" + targetDbName + @"].[dbo].AppProjectWorkFlowAction
                //                             set UpdateActionTransactionFieldID = null,
                //                                NextWorkFlowID = null, 
                //                             NextTransactionID = null, 
                //                             NextProjectID = null,
                //                             MessageContentQueryDataSetID = null, 
                //                             TransactionID = null, 
                //                             TransactionFieldID = null, 
                //                             MessageTemplateID = null, 
                //                             DataLoadID = null, 
                //                             CommandTransactionID = null, 
                //                             CommandConditionTransactionFieldID = null, 
                //                             DataTransferSettingID = null, 
                //                             CommandSearchViewID = null	
                //                            WHERE TransactionID is not null and TransactionID in (
                //                             select TransactionID from [" + targetDbName + @"].[dbo].AppTransactionUnit where transactionUnitId in (
                //                              select TransactionUnitID from [" + targetDbName + @"].[dbo].AppTransactionField where transactionFieldId in 
                //                               (
                //                               select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                               )
                //                             )
                //                            );

                //                            update [" + targetDbName + @"].[dbo].AppProjectWorkFlowAction
                //                             set UpdateActionTransactionFieldID = null,
                //                                NextWorkFlowID = null, 
                //                             NextTransactionID = null, 
                //                             NextProjectID = null,
                //                             MessageContentQueryDataSetID = null, 
                //                             TransactionID = null, 
                //                             TransactionFieldID = null, 
                //                             MessageTemplateID = null, 
                //                             DataLoadID = null, 
                //                             CommandTransactionID = null, 
                //                             CommandConditionTransactionFieldID = null, 
                //                             DataTransferSettingID = null, 
                //                             CommandSearchViewID = null	
                //                            WHERE CommandConditionTransactionFieldID is not null and CommandConditionTransactionFieldID in (	                                               
                //                       select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]		                                                 
                //                            );
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppTransactionDataLoad".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppTransactionDataLoad] 
                //                                set DataSetID = NULL,
                //                                   TransactionID = NULL,
                //                                   TransactionUnitID = NULL
                //                            where DataLoadID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppTransactionFieldAggFunction".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"
                //                            update [" + targetDbName + @"].[dbo].[AppTransactionFieldAggFunction] 
                //                                set TransactionFieldID = NULL
                //                            where FieldAggFunctionID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                        ";
                //                    }
                //                    else if (tableName.ToLower() == "AppFormLayoutItem".ToLower())
                //                    {
                //                        preCleanOneTableFkDependentQuery = queryPrepareOneTalbeDeleteIdInTempTable + @"

                //                            update [" + targetDbName + @"].[dbo].[AppFormLayoutItem] 
                //                                set UIGridLayoutParentID = NULL, FormId = null
                //                            where UIGridLayoutParentID in (
                //                                        select[DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                        ";
                //                    }

                //                    deleteOneTableQuery += @"
                //                            DELETE FROM [" + targetDbName + @"].[dbo].[" + tableName + @"] WHERE [" + pkColumnName + @"] in (
                //                                        select [DeleteId] from [" + targetDbName + @"].[dbo].[Temp_DeleteId]
                //                                    );
                //                                ";

                //                    deleteOneTableQuery = queryPrepareOneTalbeDeleteIdInTempTable + deleteOneTableQuery;


                //                    queryDeleteAll = preCleanOneTableFkDependentQuery + queryDeleteAll + deleteOneTableQuery;
                //                }
                //            }
                //        }
                //    }



                //    foreach (string userTableName in importSettingDto.DictUserTableNameAndIsNeedToImport.Keys)
                //    {
                //        if (importSettingDto.DictUserTableNameAndIsNeedToImport[userTableName])
                //        {
                //            string queryDropOneUserTable = @"
                //                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[{userTableName}]') AND type in (N'U'))
                //                BEGIN	                   
                //                 DROP TABLE [{targetDbName}].[dbo].[{userTableName}]
                //                END;
                //            ".Replace("{targetDbName}", targetDbName).Replace("{userTableName}", userTableName);

                //            queryDeleteAll += queryDropOneUserTable;
                //        }
                //    }


                //    if (!string.IsNullOrWhiteSpace(queryDeleteAll))
                //    {
                //        using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                //        {
                //            try
                //            {
                //                adpater.ExecuteExecuteNonQuery(queryCreateDeleteIdTempTable, new List<System.Data.SqlClient.SqlParameter>());
                //                adpater.ExecuteExecuteNonQuery(queryDeleteAll, new List<System.Data.SqlClient.SqlParameter>());
                //            }
                //            catch (Exception ex)
                //            {
                //                adpater.Rollback();
                //                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                //                    "Unistall Application Failed. \n" + ex.ToString() + "\n\nDelete Query:\n\n" + queryDeleteAll + "\n"));
                //            }


                //        }
                //    }
                //}
                //catch (Exception ex)
                //{
                //    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                //        "Unistall Application Failed. \n" + ex.ToString() + "\n\nDelete Query:\n\n" + queryDeleteAll + "\n"));
                //}




            }


            if (!validationResult.HasErrors)
            {
                aOperationCallResult.Object = true;

                string msg = "Uninstall Application Success. \n\n" + (validationResult.LocalizedMessages).Replace("Message: ", "");
                validationResult.Items.Clear();

                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, msg));
            }

            return aOperationCallResult;
        }



        private static string Unistall_CreateDeleteIdsTempTable(string targetDbName)
        {
            string query = "";

            foreach (string tableName in AppApplicationImportSettingDto.UninstallTableNamesInOrder)
            {
                string tempDeleteIdTableName = "Temp_DeleteId_" + tableName;

                query += @"
                            IF NOT EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[{tempDeleteIdTableName}]') AND type in (N'U'))
                            BEGIN	                   
	                            CREATE TABLE [{targetDbName}].[dbo].[{tempDeleteIdTableName}] (DeleteId int NOT NULL)
                            END;

                            DELETE FROM [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}];

                        ";

                query = query.Replace("{targetDbName}", targetDbName).Replace("{tempDeleteIdTableName}", tempDeleteIdTableName); ;
            }


            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTableQuery(ValidationResult validationResult, string targetDbName, int targetApplicatoinId)
        {
            string query = @"
                declare @ApplicationId int = " + targetApplicatoinId.ToString() + @";
            ";

            query += Unistall_PopulateDeleteIdsTempTable_AppListMenu(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransaction(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionUnit(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionField(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionFieldAggFunction(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitFormula(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitLinkedSearch(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitSearchFieldMapping(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitSearchViewFieldMapping(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionDataTransferSetting(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionSaveAsMapping(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionDataLoad(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppTranscationDataLoadFieldMapping(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppTranscationReport(targetDbName);



            query += Unistall_PopulateDeleteIdsTempTable_AppTransactionNavigation(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppConditionalAction(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppForm(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppFormLayoutItem(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppSearch(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppSearchField(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppDataSet(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppDataSetParameter(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppSearchView(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppSearchViewField(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppFormLinkTarget(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppViewLinkedSeaechOrUrl(targetDbName);

            query += Unistall_PopulateDeleteIdsTempTable_AppProjectWorkFlowAction(targetDbName);

            //query += Unistall_PopulateDeleteIdsTempTable_AppMessage(targetDbName);
            //query += Unistall_PopulateDeleteIdsTempTable_AppEntityInfo(validationResult, targetDbName, targetApplicatoinId);


            //query += Unistall_PopulateDeleteIdsTempTable_AppDesktop(targetDbName);
            query += Unistall_PopulateDeleteIdsTempTable_AppReport(targetDbName);


            query += Unistall_PopulateDeleteIdsTempTable_AppApplicationAssetsItem(targetDbName);

            //query += Unistall_PopulateDeleteIdsTempTable_AppIntergrationSetting(targetDbName);
            //query += Unistall_PopulateDeleteIdsTempTable_AppIntergrationSettingParameter(targetDbName);

            //query += Unistall_PopulateDeleteIdsTempTable_AppWebAPIDataExchangeSetting(targetDbName);
            //query += Unistall_PopulateDeleteIdsTempTable_AppWinScheduleSetting(targetDbName);

            //query += Unistall_PopulateDeleteIdsTempTable_AppEsiteCatalogue(targetDbName);
            //query += Unistall_PopulateDeleteIdsTempTable_AppESiteNavMenu(targetDbName);
            //query += Unistall_PopulateDeleteIdsTempTable_AppESitePages(targetDbName);
            //query += Unistall_PopulateDeleteIdsTempTable_AppEsite(targetDbName);

















            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string Unistall_ReprepareDeleteConfigDataQuery(ValidationResult validationResult, string targetDbName)
        {
            string queryAll = "";
            string query_CleanTablesFKDependentData = "";
            string query_Delete = "";


            try
            {
                string targetDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, targetDbName);
                DatabaseFixture databaseFixture = new DatabaseFixture(targetDbConnectionString, EmSqlType.SqlServer);

                foreach (string tableName in AppApplicationImportSettingDto.UninstallTableNamesInOrder)
                {
                    DatabaseTable tableDto = databaseFixture.Table(tableName);

                    DatabaseColumn pkDto = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                    if (pkDto != null)
                    {
                        string pkColumnName = pkDto.Name;


                        string tempDeleteIdTableName = "Temp_DeleteId_" + tableName;

                        string query_CleanOneTableFKDependentData = Unistall_ExecuteDelete_PrepareCleanOneTableFKDependentDataQuery(targetDbName, tableName, tempDeleteIdTableName);

                        query_CleanTablesFKDependentData += query_CleanOneTableFKDependentData;

                        if (tableName.ToLower() != "AppListMenu".ToLower())
                        {

                            query_Delete += @"                           

                            DELETE FROM [{targetDbName}].[dbo].[" + tableName + @"] WHERE [" + pkColumnName + @"] in (
                                select [DeleteId] from [{targetDbName}].[dbo].[{tempDeleteIdTableName}]
                            );                                               

                        ";

                            query_Delete = query_Delete.Replace("{targetDbName}", targetDbName).Replace("{tempDeleteIdTableName}", tempDeleteIdTableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error,
                    "Generating Delete Query Failed. \n" + ex.ToString() + "\n"));
            }

            queryAll = query_CleanTablesFKDependentData + query_Delete;

            return queryAll;
        }

        private static string Unistall_ExecuteDelete_PrepareCleanOneTableFKDependentDataQuery(string targetDbName, string tableName, string tempDeleteIdTableName)
        {
            string query = "";

            if (tableName.ToLower() == "AppListMenu".ToLower())
            {
                query += @"
                                                            DELETE FROM [" + targetDbName + @"].[dbo].[AppSecurityUserListMenu] WHERE MenuID in (
                                                                select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                        ";

                query += @"
                                                            DELETE FROM [" + targetDbName + @"].[dbo].[AppSysLabelLanguage] WHERE MenuID in (
                                                                select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                        ";
            }
            else if (tableName.ToLower() == "AppForm".ToLower())
            {
                query += @"
                                                        UPDATE [" + targetDbName + @"].[dbo].[AppTransaction] SET FormID = null WHERE FormID in (
                                                                select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                        ";
                query += @"
                                                        UPDATE [" + targetDbName + @"].[dbo].[AppTransaction] SET PrintFormID = null WHERE PrintFormID in (
                                                                select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                        ";
            }
            else if (tableName.ToLower() == "AppEntityInfo".ToLower())
            {
                query += @"
                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppEntitySimpleListValue] WHERE EntityInfoID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppSearchView".ToLower())
            {
                query += @"

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where SearchViewID is not null and SearchViewID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppSearchView] 
                                                        set WhereUsedDefaultViewId = null
                                                       ,CatalogueSearchID = null
                                                       ,TransactionID = null
                                                       ,ProductDetaiViewMapUnitID  = null
                                                       ,UpdateTransctionID = null
                                                       ,UpdateBaseTranscationUnitID =null
                                                       ,FilterSearchID = null
                                                       ,HierachyParentViewID = null
                                                    where SearchViewID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppFormLinkTarget] 
                                                    set SearchViewID = null,
                                                        SourceViewColumnID1 = null, 
                                                     SourceViewColumnID2 = null, 
                                                     SourceViewColumnID3 = null,
                                                     TargetSearchFieldID1 = null, 
                                                     TargetSearchFieldID2 = null, 
                                                     TargetSearchFieldID3 = null, 
                                                     RowDisplayViewColumnID = null, 
                                                     SourceConditionViewColumnID = null, 
                                                     DataTransferSettingID = null, 
                                                     LinkTargetSearchID = null, 
                                                     LinkTargetTransactionID = null
                                                    WHERE SearchViewID is not null and SearchViewID in (	                                               
                                                  select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]	                                                
                                                    )
                                                ";
            }

            else if (tableName.ToLower() == "AppSearchViewField".ToLower())
            {
                query += @"
                                                    update [" + targetDbName + @"].[dbo].[AppSearchViewField] 
                                                        set MappingSearchFieldID = NULL
                                                   ,MassUpdateTransactionFieldID = NULL
                                                            ,PullCriteriaAsDefaultValueSearchFieldID = NULL
                                                   ,JoinToParentViewFieldID = NULL		
                                                    where SearchViewFieldID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppFormLinkTarget] 
                                                    set SourceViewColumnID1 = null, 
                                                     SourceViewColumnID2 = null, 
                                                     SourceViewColumnID3 = null,
                                                     TargetSearchFieldID1 = null, 
                                                     TargetSearchFieldID2 = null, 
                                                     TargetSearchFieldID3 = null, 
                                                     RowDisplayViewColumnID = null, 
                                                     SourceConditionViewColumnID = null, 
                                                     DataTransferSettingID = null, 
                                                     LinkTargetSearchID = null, 
                                                     LinkTargetTransactionID = null
                                                    WHERE SearchViewID is not null and SearchViewID in (
                                                     select SearchViewID from [" + targetDbName + @"].[dbo].AppSearchViewField 
                                                     where SearchViewFieldID in (
                                                      select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                     )
                                                    )
                                                ";
            }

            else if (tableName.ToLower() == "AppSearch".ToLower())
            {
                query += @"
                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [SearchID] is not null and [SearchID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppSearch] 
                                                        set WhereUsedSearchID = NULL
                                                    where SearchID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppSearchField".ToLower())
            {
                query += @"
                                                    update [" + targetDbName + @"].[dbo].[AppSearchField] 
                                                        set ParentFieldID = NULL
                                                   ,MasterEntityFieldlID = NULL	
                                                    where SearchFieldID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppTransaction".ToLower())
            {
                query += @"
                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [TransactionID] is not null and [TransactionID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [UserActionTransactionID] is not null and [UserActionTransactionID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppTransaction] 
                                                        set FolderTransactionID = NULL
                                                   ,MasterTransactionID = NULL		
                                                    where TransactionId in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].AppTransactionDataTransferSetting 
                                                    set TransactionID = null	
                                                    WHERE TransactionID in (
                                                     select [DeleteId] FROM [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                    );

                                                ";
            }
            else if (tableName.ToLower() == "AppTransactionUnit".ToLower())
            {
                query += @"
                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [TransactionUnitID] is not null and [TransactionUnitID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );


                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [UserActionTransactionUnitID] is not null and [UserActionTransactionUnitID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppTransactionUnit] 
                                                        set ParentTransactionUnitID = NULL
                                                   ,AvailableSourceUnitID = NULL
                                                    where TransactionUnitID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].AppTransactionSaveAsMapping 
                                                    set TransactionID = null,
                                                     MappingUnitId = null,
                                                     DataTransferSettingID = null
                                                    WHERE MappingUnitId in (
                                                     select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                    )
                                                ";
            }
            else if (tableName.ToLower() == "AppTransactionField".ToLower())
            {
                query += @"                         DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [TransactionFieldID] is not null and [TransactionFieldID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].[AppTransactionField] 
                                                        set [DDLParentLevelID] = NULL,
                                                           [MasterEntityFieldlID] = NULL,
                                                           [ChildUnitSubscribeParentFieldID] = NULL,
                                                           [ParentUnitSubscribeChildAggFunctionID]=NULL,
                                                           [MatrixKeyTransactionFieldId]=NULL,
                                                           [MatrixForeignKeyFieldID]=NULL,
                                                           [DdlForeignUnitID] =NULL,
                                                           [FileControlTypeFolderTransactionID]=NULL,
                                                           [LinkToParentPrimaryKeyFieldID] =NULL,
                                                           [SiblingUnitLogicalKeyFieldID]=NULL,
                                                           [HostFormLayoutItemID] =NULL,
                                                           [OnChangeTriggerToCommandID]=NULL
                                                    where TransactionFieldID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );

                                                    update [" + targetDbName + @"].[dbo].AppProjectWorkFlowAction
                                                     set UpdateActionTransactionFieldID = null,
                                                        NextWorkFlowID = null, 
                                                     NextTransactionID = null, 
                                                     NextProjectID = null,
                                                     MessageContentQueryDataSetID = null, 
                                                     TransactionID = null, 
                                                     TransactionFieldID = null, 
                                                     MessageTemplateID = null, 
                                                     DataLoadID = null, 
                                                     CommandTransactionID = null, 
                                                     CommandConditionTransactionFieldID = null, 
                                                     DataTransferSettingID = null, 
                                                     CommandSearchViewID = null	
                                                    WHERE TransactionID is not null and TransactionID in (
                                                     select TransactionID from [" + targetDbName + @"].[dbo].AppTransactionUnit where transactionUnitId in (
                                                      select TransactionUnitID from [" + targetDbName + @"].[dbo].AppTransactionField where transactionFieldId in 
                                                       (
                                                       select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                       )
                                                     )
                                                    );

                                                    update [" + targetDbName + @"].[dbo].AppProjectWorkFlowAction
                                                     set UpdateActionTransactionFieldID = null,
                                                        NextWorkFlowID = null, 
                                                     NextTransactionID = null, 
                                                     NextProjectID = null,
                                                     MessageContentQueryDataSetID = null, 
                                                     TransactionID = null, 
                                                     TransactionFieldID = null, 
                                                     MessageTemplateID = null, 
                                                     DataLoadID = null, 
                                                     CommandTransactionID = null, 
                                                     CommandConditionTransactionFieldID = null, 
                                                     DataTransferSettingID = null, 
                                                     CommandSearchViewID = null	
                                                    WHERE CommandConditionTransactionFieldID is not null and CommandConditionTransactionFieldID in (	                                               
                                               select [DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]		                                                 
                                                    );
                                                ";

                query += @"

                                                    update [" + targetDbName + @"].[dbo].[AppFormLayoutItem] 
                                                        set TransactionFieldID = NULL
                                                    where TransactionFieldID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";

            }
            else if (tableName.ToLower() == "AppTransactionDataLoad".ToLower())
            {
                query += @"
                                                    update [" + targetDbName + @"].[dbo].[AppTransactionDataLoad] 
                                                        set DataSetID = NULL,
                                                           TransactionID = NULL,
                                                           TransactionUnitID = NULL
                                                    where DataLoadID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppTransactionFieldAggFunction".ToLower())
            {
                query += @"
                                                    update [" + targetDbName + @"].[dbo].[AppTransactionFieldAggFunction] 
                                                        set TransactionFieldID = NULL
                                                    where FieldAggFunctionID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppFormLayoutItem".ToLower())
            {
                query += @"

                                                    update [" + targetDbName + @"].[dbo].[AppFormLayoutItem] 
                                                        set UIGridLayoutParentID = NULL, FormId = null
                                                    where UIGridLayoutParentID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";

                query += @"

                                                    update [" + targetDbName + @"].[dbo].[AppFormLayoutItem] 
                                                        set TransactionFieldID = NULL, GridTransactionUnitID=null, AutoExcuteSearchID=null
                                                    where FormLayoutItemID in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppProjectWorkFlowAction".ToLower())
            {
                query += @"

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [CommandId] is not null and [CommandId] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppReport".ToLower())
            {
                query += @"

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [ReportID] is not null and [ReportID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppDesktop".ToLower())
            {
                query += @"

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [DesktopID] is not null and [DesktopID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppFormLinkTarget".ToLower())
            {
                query += @"

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [FormLinkTargetID] is not null and [FormLinkTargetID] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            else if (tableName.ToLower() == "AppTransactionUnitLinkedSearch".ToLower())
            {
                query += @"

                                                    DELETE FROM [" + targetDbName + @"].[dbo].[AppSecuritySysObjGroupUser] 
                                                    where [TransactionUnitLinkedSearchId] is not null and [TransactionUnitLinkedSearchId] in (
                                                                select[DeleteId] from [" + targetDbName + @"].[dbo].[{tempDeleteIdTableName}]
                                                            );
                                                ";
            }
            query = query.Replace("{tempDeleteIdTableName}", tempDeleteIdTableName);
            return query;
        }

        private static string Unistall_PrepareDropUserTablesQuery(ValidationResult validationResult, string targetDbName)
        {
            string queryGetTableNames = @"                
                select distinct TableName 
                FROM
                (
                    select DataBaseTableName as TableName from [{targetDbName}].[dbo].[AppTransactionUnit] 
                    where TransactionUnitID in (
                            select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
                        )
                        and DataBaseTableName is not null and DataBaseTableName <> ''
                    UNION
                    select TableName from [{targetDbName}].[dbo].[AppEntityInfo] 
                    where EntityInfoID in (
                            select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppEntityInfo]
                        ) 
                        and EntityType = 1 and TableName is not null and TableName <> ''
                ) as TableNames
                where TableName not like 'app%' and TableName not like 'view_%';
                   
            ";
            queryGetTableNames = queryGetTableNames.Replace("{targetDbName}", targetDbName);

            string queryGetTableNames_ReferenceByOtherApplicatoin = @"                
                select distinct TableName, WhereUsedDisplay
                FROM
                (
                    select DataBaseTableName as TableName, ('Data Model: ' + AppTransaction.TransactionName + ' (' + CONVERT(nvarchar(100), transUnit.TransactionID) + ')') as WhereUsedDisplay
                    from [{targetDbName}].[dbo].[AppTransactionUnit] as transUnit
                        inner join [{targetDbName}].[dbo].[AppTransaction] as AppTransaction on (transUnit.TransactionId = AppTransaction.TransactionId)
                       
                    where transUnit.TransactionUnitID not in (
                            select DeleteId from [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionUnit]
                        ) 
                        and DataBaseTableName is not null and DataBaseTableName <> ''
                    UNION
                    select TableName, ('Entity: ' + EntityCode) as WhereUsedDisplay 
                    from [{targetDbName}].[dbo].[AppEntityInfo] 
                    where EntityInfoID not in (
                            select DeleteId from [{targetDbName}].[dbo].[Temp_DeleteId_AppEntityInfo]
                        ) 
                        and EntityType = 1 and TableName is not null and TableName <> ''
                ) as TableNames
                where TableName not like 'app%' and TableName not like 'view_%';
                    
            ";

            queryGetTableNames_ReferenceByOtherApplicatoin = queryGetTableNames_ReferenceByOtherApplicatoin.Replace("{targetDbName}", targetDbName);



            string query = "";

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtUserTableName = adpater.ExecuteDataTableRetrievalQuery(queryGetTableNames, new List<System.Data.SqlClient.SqlParameter>());
                    DataTable dtUserTableName_Referenced = adpater.ExecuteDataTableRetrievalQuery(queryGetTableNames_ReferenceByOtherApplicatoin, new List<System.Data.SqlClient.SqlParameter>());

                    List<string> userTableNameList_RefByCurrentApp = new List<string>();
                   // List<string> userTableNameList_ReferencedByOtherApp = new List<string>();
                    List<string> userTableNameList_NotToDelete = new List<string>();

                    Dictionary<string, List<string>> dictTableNameAndWhereUsedByOtherApp = new Dictionary<string, List<string>>();

                    foreach (DataRow dataRow in dtUserTableName_Referenced.Rows)
                    {
                        string tableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["TableName"]).Trim().ToLower();
                        string whereUsed = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["WhereUsedDisplay"]);

                        if (!string.IsNullOrWhiteSpace(tableName))
                        {
                            if (!tableName.StartsWith("app"))
                            {
                                if (!dictTableNameAndWhereUsedByOtherApp.ContainsKey(tableName))
                                {
                                    dictTableNameAndWhereUsedByOtherApp.Add(tableName, new List<string>() { whereUsed });
                                }
                                else
                                {
                                    dictTableNameAndWhereUsedByOtherApp[tableName].Add(whereUsed);
                                }                                
                            }
                        }
                    }

                    foreach (DataRow dataRow in dtUserTableName.Rows)
                    {
                        string tableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["TableName"]).Trim().ToLower();

                        if (!string.IsNullOrWhiteSpace(tableName) && !userTableNameList_RefByCurrentApp.Contains(tableName))
                        {
                            if (!tableName.StartsWith("app"))
                            {
                                if (!dictTableNameAndWhereUsedByOtherApp.ContainsKey(tableName))
                                {
                                    userTableNameList_RefByCurrentApp.Add(tableName);
                                }
                                else
                                {
                                    userTableNameList_NotToDelete.Add(tableName);
                                }
                            }
                        }
                    }


                    foreach (string tableName in userTableNameList_RefByCurrentApp)
                    {
                        string queryDropOneTable = @"

                            IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[{tableName}]') AND type in (N'U'))
                            BEGIN	                   
	                            DROP TABLE [{targetDbName}].[dbo].[{tableName}] 
                            END;                                                                       

                        ";

                        queryDropOneTable = queryDropOneTable.Replace("{targetDbName}", targetDbName)
                            .Replace("{tableName}", tableName);

                        query += queryDropOneTable;
                    }


                    if (userTableNameList_NotToDelete.Count > 0)
                    {
                        var tableStringParts = userTableNameList_NotToDelete.OrderBy(o => o).Select(o =>
                        {
                            string display = "[" + o + "]";

                            string usedByDataModel = string.Join(", ", dictTableNameAndWhereUsedByOtherApp[o].Where(p1 => p1.StartsWith("Data Model: ")).Select(p2 => p2.Replace("Data Model: ", "")).OrderBy(p3 => p3));
                            string usedByEntity = string.Join(", ", dictTableNameAndWhereUsedByOtherApp[o].Where(p1 => p1.StartsWith("Entity: ")).Select(p2 => p2.Replace("Entity: ", "")).OrderBy(p3 => p3));

                            if (!string.IsNullOrWhiteSpace(usedByDataModel))
                            {
                                display += "\n - used by data model: " + usedByDataModel;
                            }

                            if (!string.IsNullOrWhiteSpace(usedByEntity))
                            {
                                display += "\n - used by entity: " + usedByEntity;
                            }

                            return display;

                        });
                        string tableString = string.Join("\n\n", tableStringParts);

                        string msg = "The tables below serve as crucial data sources for other applications, and as such, they will be retained without deletion. \n\n" + tableString + "";
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_query_Warning", ValidationItemType.Message, msg));
                    }


                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Prepare Drop User Table Query Failed. \n" + ex.ToString()));
                }
            }




            return query;
        }

        private static string Unistall_DropDeleteIdTempTable(string targetDbName)
        {
            string query = "";

            foreach (string tableName in AppApplicationImportSettingDto.UninstallTableNamesInOrder)
            {
                string tempDeleteIdTableName = "Temp_DeleteId_" + tableName;

                query += @"

                            IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[{tempDeleteIdTableName}]') AND type in (N'U'))
                            BEGIN	                   
	                            DROP TABLE [{targetDbName}].[dbo].[{tempDeleteIdTableName}] 
                            END;                                                                       

                        ";
                query = query.Replace("{targetDbName}", targetDbName)
                        .Replace("{tempDeleteIdTableName}", tempDeleteIdTableName);

            }

            return query;
        }



















        private static string Unistall_PopulateDeleteIdsTempTable_AppListMenu(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppListMenu] (DeleteId)
                select [MenuID]  from
                (
                    select [MenuID] from [{targetDbName}].[dbo].[AppListMenu] where MenuID = @ApplicationId
                    union
                    select [MenuID] from [{targetDbName}].[dbo].[AppListMenu] where ParentID = @ApplicationId
                    union
                    select [MenuID] from [{targetDbName}].[dbo].[AppListMenu] where ParentID in (select MenuID from [{targetDbName}].[dbo].[AppListMenu] where ParentID = @ApplicationId)
                    union
                    select [MenuID] from [{targetDbName}].[dbo].[AppListMenu] where ParentID in (select MenuID from [{targetDbName}].[dbo].[AppListMenu] where ParentID in (select MenuID from [{targetDbName}].[dbo].[AppListMenu] where ParentID = @ApplicationId))
                ) as menus;

            ";

            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransaction(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransaction] (DeleteId)
                SELECT DISTINCT TransactionID FROM
                (
	                select TransactionID from {targetDbName}.[dbo].[AppTransaction] where SaasApplicationID = @ApplicationId
	                UNION
	                select TransactionID FROM {targetDbName}.[dbo].[AppApplicationAssetsItem] WHERE TransactionID is not null and ApplicationID = @ApplicationId
                ) as TransactionIds;
                ";

            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionUnit(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionUnit] (DeleteId)
                SELECT TransactionUnitID FROM {targetDbName}.[dbo].[AppTransactionUnit] where TransactionID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                )
                ";

            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionField(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionField] (DeleteId)
                SELECT TransactionFieldID FROM {targetDbName}.[dbo].[AppTransactionField] where TransactionUnitID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
                );
                ";

            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionFieldAggFunction(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionFieldAggFunction] (DeleteId)
                SELECT FieldAggFunctionID FROM {targetDbName}.[dbo].[AppTransactionFieldAggFunction] where TransactionFieldID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionField]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitFormula(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionUnitFormula] (DeleteId)
                SELECT TransactionUnitFormulaID FROM {targetDbName}.[dbo].[AppTransactionUnitFormula] where TransactionUnitID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }


        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitLinkedSearch(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionUnitLinkedSearch] (DeleteId)
                SELECT TransactionUnitLinkedSearchId FROM {targetDbName}.[dbo].[AppTransactionUnitLinkedSearch] where TransactionUnitID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitSearchFieldMapping(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionUnitSearchFieldMapping] (DeleteId)
                SELECT TransactionUnitSearchFieldMappingId FROM {targetDbName}.[dbo].[AppTransactionUnitSearchFieldMapping] where TransactionUnitLinkedSearchId in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnitLinkedSearch]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionUnitSearchViewFieldMapping(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionUnitSearchViewFieldMapping] (DeleteId)
                SELECT TransactionUnitSearchViewFieldMappingId FROM {targetDbName}.[dbo].[AppTransactionUnitSearchViewFieldMapping] where TransactionUnitLinkedSearchId in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnitLinkedSearch]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionDataTransferSetting(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionDataTransferSetting] (DeleteId)
                SELECT DataTransferSettingID FROM {targetDbName}.[dbo].[AppTransactionDataTransferSetting] where TransactionID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionSaveAsMapping(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionSaveAsMapping] (DeleteId)
                SELECT MappingID FROM {targetDbName}.[dbo].[AppTransactionSaveAsMapping] where DataTransferSettingID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionDataTransferSetting]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionDataLoad(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionDataLoad] (DeleteId)
                SELECT DataLoadID FROM {targetDbName}.[dbo].[AppTransactionDataLoad] where TransactionID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTranscationDataLoadFieldMapping(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTranscationDataLoadFieldMapping] (DeleteId)
                SELECT FieldMappingID FROM {targetDbName}.[dbo].[AppTranscationDataLoadFieldMapping] where DataLoadID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionDataLoad]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTranscationReport(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTranscationReport] (DeleteId)
                SELECT TransctionReportID FROM {targetDbName}.[dbo].[AppTranscationReport] where TranscationID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppTransactionNavigation(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppTransactionNavigation] (DeleteId)
                SELECT TransNavigationID FROM {targetDbName}.[dbo].[AppTransactionNavigation] where TransactionID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppConditionalAction(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppConditionalAction] (DeleteId)
                SELECT ActionID FROM {targetDbName}.[dbo].[AppConditionalAction] where TransactionID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppForm(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppForm] (DeleteId)
                SELECT FormID FROM {targetDbName}.[dbo].[AppForm] 
                where FormID in
                (
	                select FormID from {targetDbName}.[dbo].AppTransaction 
			        where FormID is not null and TransactionID in (
				        select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
			        )
                )
                or FormID in
                (
	                select PrintFormID from {targetDbName}.[dbo].AppTransaction 
			        where PrintFormID is not null and TransactionID in (
				        select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
			        )
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppFormLayoutItem(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppFormLayoutItem] (DeleteId)
                SELECT FormLayoutItemID FROM {targetDbName}.[dbo].[AppFormLayoutItem] where FormID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppForm]
                )
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppSearch(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppSearch] (DeleteId)
                SELECT DISTINCT SearchID FROM
                (
	                select SearchID from [{targetDbName}].[dbo].[AppSearch] where SaasApplicationID = @ApplicationId
	                UNION
	                select SearchID FROM [{targetDbName}].[dbo].[AppApplicationAssetsItem] WHERE SearchID is not null and ApplicationID = @ApplicationId
                ) as SearchIDs
                WHERE SearchID IS NOT NULL;

                
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }
        private static string Unistall_PopulateDeleteIdsTempTable_AppSearchField(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppSearchField] (DeleteId)
                SELECT SearchFieldID FROM {targetDbName}.[dbo].[AppSearchField] where SearchID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearch]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppDataSet(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppDataSet] (DeleteId)
                SELECT DISTINCT DataSetId FROM
                (
	                select DataSetId from [{targetDbName}].[dbo].[AppDataSet] where SaasApplicationID = @ApplicationId
	                UNION
	                select DISTINCT DataSetId FROM [{targetDbName}].[dbo].[AppSearch] WHERE SearchID in (
		                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearch]
	                )
	                UNION
	                select DISTINCT DataSetId FROM [{targetDbName}].[dbo].[AppTransactionDataLoad] WHERE TransactionUnitID in (
		                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
	                )
                ) as DataSetIds
                WHERE DataSetId is not null;
                
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppDataSetParameter(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppDataSetParameter] (DeleteId)
                SELECT ParameterID FROM {targetDbName}.[dbo].[AppDataSetParameter] where [DataSetID] in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppDataSet]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }
        private static string Unistall_PopulateDeleteIdsTempTable_AppSearchView(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppSearchView] (DeleteId)
                SELECT SearchViewID FROM {targetDbName}.[dbo].[AppSearchView] where DataSetID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppDataSet]
                )
                
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }
        private static string Unistall_PopulateDeleteIdsTempTable_AppSearchViewField(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppSearchViewField] (DeleteId)
                SELECT SearchViewFieldID FROM {targetDbName}.[dbo].[AppSearchViewField] where [SearchViewID] in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearchView]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppFormLinkTarget(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppFormLinkTarget] (DeleteId)
                SELECT LinkTargetID FROM {targetDbName}.[dbo].[AppFormLinkTarget] 
                where SearchViewID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearchView]
                ) 
                or 
                TransactionUnitID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }



        private static string Unistall_PopulateDeleteIdsTempTable_AppViewLinkedSeaechOrUrl(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppViewLinkedSeaechOrUrl] (DeleteId)
                SELECT SearchViewLinkSearchID FROM {targetDbName}.[dbo].[AppViewLinkedSeaechOrUrl] 
                where SearchViewID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearchView]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppProjectWorkFlowAction(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppProjectWorkFlowAction] (DeleteId)
                SELECT WorkFlowActionID FROM {targetDbName}.[dbo].[AppProjectWorkFlowAction] 
                where CommandTransactionID in
                (
	                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }



        private static string Unistall_PopulateDeleteIdsTempTable_AppMessage(ValidationResult validationResult, string targetDbName)
        {
            string query_MessageID = @"

                SELECT MessageID, Subject FROM {targetDbName}.[dbo].[AppMessage] 
                where IsPredefinedTemplate = 1 and 
                (
		            TransactionID in (
			            select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
		            )
		            or MessageID in (
			            select MessageTemplateID from [{targetDbName}].[dbo].AppProjectWorkFlowAction
			            where CommandTransactionID in (
				            select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
			            ) and MessageTemplateID is not null
		            )
	            );
            ";

            query_MessageID = query_MessageID.Replace("{targetDbName}", targetDbName);


            string query_MessageID_ReferenceByOtherApplicatoin = @"       

                SELECT MessageID, Subject FROM {targetDbName}.[dbo].[AppMessage] 
                where IsPredefinedTemplate = 1 and 
                (
		            TransactionID not in (
			            select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
		            )
		            or MessageID in (
			            select MessageTemplateID from [{targetDbName}].[dbo].AppProjectWorkFlowAction
			            where CommandTransactionID not in (
				            select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
			            ) and MessageTemplateID is not null
		            )
	            );                    
            ";

            query_MessageID_ReferenceByOtherApplicatoin = query_MessageID_ReferenceByOtherApplicatoin.Replace("{targetDbName}", targetDbName);



            string query = "";

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtMessageTemplate = adpater.ExecuteDataTableRetrievalQuery(query_MessageID, new List<System.Data.SqlClient.SqlParameter>());
                    DataTable dtMessageTemplate_Referenced = adpater.ExecuteDataTableRetrievalQuery(query_MessageID_ReferenceByOtherApplicatoin, new List<System.Data.SqlClient.SqlParameter>());

                    Dictionary<int, string> dictMessageIdAndCode_OnlyRefByCurrentApp = new Dictionary<int, string>();
                    Dictionary<int, string> dictAllMessageIdAndCode_ReferencedByOtherApp = new Dictionary<int, string>();
                    Dictionary<int, string> dictMessageIdAndCode_NotToDelete = new Dictionary<int, string>();

                    foreach (DataRow dataRow in dtMessageTemplate_Referenced.Rows)
                    {
                        int? messageId = ControlTypeValueConverter.ConvertValueToInt(dataRow["MessageID"]);
                        string subject = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["Subject"]).Trim().ToLower();

                        if (messageId.HasValue && !dictAllMessageIdAndCode_ReferencedByOtherApp.ContainsKey(messageId.Value))
                        {
                            dictAllMessageIdAndCode_ReferencedByOtherApp.Add(messageId.Value, subject);
                        }
                    }

                    foreach (DataRow dataRow in dtMessageTemplate.Rows)
                    {
                        int? messageId = ControlTypeValueConverter.ConvertValueToInt(dataRow["MessageID"]);
                        string subject = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["Subject"]).Trim().ToLower();

                        if (messageId.HasValue && !dictMessageIdAndCode_OnlyRefByCurrentApp.ContainsKey(messageId.Value))
                        {
                            if (!dictAllMessageIdAndCode_ReferencedByOtherApp.ContainsKey(messageId.Value))
                            {
                                dictMessageIdAndCode_OnlyRefByCurrentApp.Add(messageId.Value, subject);
                            }
                            else
                            {
                                dictMessageIdAndCode_NotToDelete.Add(messageId.Value, subject);
                            }
                        }

                    }

                    foreach (int messageId in dictMessageIdAndCode_OnlyRefByCurrentApp.Keys)
                    {
                        string queryInsertId = @"
                            INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppMessage] (DeleteId) VALUES({messageId});                           
                        ";

                        queryInsertId = queryInsertId.Replace("{targetDbName}", targetDbName)
                            .Replace("{messageId}", messageId.ToString());

                        query += queryInsertId;
                    }


                    if (dictMessageIdAndCode_NotToDelete.Count > 0)
                    {
                        string entityString = string.Join("\n", dictMessageIdAndCode_NotToDelete.OrderBy(o => o.Key).Select(o => o.Key.ToString() + " - " + o.Value));

                        string msg = "The message template below are used by other applications, and will not be deleted: \n\n" + entityString + "\n\n";
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_query_Warning", ValidationItemType.Message, msg));
                    }


                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Prepare Drop User Table Query Failed. \n" + ex.ToString()));
                }
            }


            return query;

        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppEntityInfo(ValidationResult validationResult, string targetDbName, int targetApplicatoinId)
        {
            string query_entityId = @"

                declare @ApplicationId int = " + targetApplicatoinId.ToString() + @";
                
                SELECT EntityInfoID, EntityCode FROM {targetDbName}.[dbo].[AppEntityInfo] 
                where (IsSystemDefine is null or IsSystemDefine = 0) and EntityInfoID in (
	                SELECT DISTINCT EntityInfoID FROM
	                (
		                select EntityInfoID from [{targetDbName}].[dbo].AppEntityInfo where SaasApplicationID = @ApplicationId
		                UNION
		                select LogicalDisplayEntityID as EntityInfoID from [{targetDbName}].[dbo].[AppTransaction] 
			                WHERE LogicalDisplayEntityID is not null and TransactionID in (
				                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
			                )
		                UNION
		                select EntityID as EntityInfoID from [{targetDbName}].[dbo].[AppTransactionField] 
			                WHERE EntityID is not null and TransactionUnitID in 
			                (
				                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
			                )
		                UNION
		                select EntityID as EntityInfoID from [{targetDbName}].[dbo].[AppSearchField] 
			                WHERE EntityID is not null and SearchID in 
			                (			
				                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearch]
			                )
		                UNION
		                select EntityID as EntityInfoID from [{targetDbName}].[dbo].[AppSearchViewField] 
			                WHERE EntityID is not null and SearchViewID in 
			                (			
				                select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearchView]
			                )
	                ) as EntityInfoIDs
                );
                ";

            query_entityId = query_entityId.Replace("{targetDbName}", targetDbName);



            string query_entityId_ReferenceByOtherApplicatoin = @"       
                declare @ApplicationId int = " + targetApplicatoinId.ToString() + @";

                SELECT DISTINCT AppEntityInfo.EntityInfoID, AppEntityInfo.EntityCode, whereUsedInfo.WhereUsedDisplay
                FROM {targetDbName}.[dbo].[AppEntityInfo] as AppEntityInfo
                    inner join (
                        SELECT DISTINCT EntityInfoID, WhereUsedDisplay FROM
	                    (
		                    select EntityInfoID, 'Application: ' + CONVERT(nvarchar(100), SaasApplicationID) as  WhereUsedDisplay 
                            from [{targetDbName}].[dbo].AppEntityInfo where SaasApplicationID <> @ApplicationId
		                    UNION
		                    select LogicalDisplayEntityID as EntityInfoID, ('Data Model: ' + AppTransaction.TransactionName + ' (' + CONVERT(nvarchar(100), TransactionID) + ')') as WhereUsedDisplay
                                from [{targetDbName}].[dbo].[AppTransaction] 
			                    WHERE LogicalDisplayEntityID is not null and TransactionID not in (
				                    select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransaction]
			                    )
		                    UNION
		                    select transField.EntityID as EntityInfoID, ('Data Model: ' + TransactionName + ' (' + CONVERT(nvarchar(100), transUnit.TransactionID) + ')') as WhereUsedDisplay 
                                from [{targetDbName}].[dbo].[AppTransactionField] as transField 
                                    inner join [{targetDbName}].[dbo].[AppTransactionUnit] as transUnit on (transField.TransactionUnitID = transUnit.TransactionUnitID)
                                    inner join [{targetDbName}].[dbo].[AppTransaction] as AppTransaction on (transUnit.TransactionId = AppTransaction.TransactionId)
			                    WHERE EntityID is not null and transUnit.TransactionUnitID not in 
			                    (
				                    select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppTransactionUnit]
			                    )
		                    UNION
		                    select EntityID as EntityInfoID, ('Search: ' + AppSearch.Name + ' (' + CONVERT(nvarchar(100), AppSearch.SearchID) + ')') as WhereUsedDisplay 
                                    from [{targetDbName}].[dbo].[AppSearchField]  as AppSearchField
                                    inner join [{targetDbName}].[dbo].[AppSearch] as AppSearch on (AppSearchField.SearchID = AppSearch.SearchID)
			                    WHERE EntityID is not null and AppSearchField.SearchID not in 
			                    (			
				                    select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearch]
			                    )
		                    UNION
		                    select EntityID as EntityInfoID, ('Search View: ' + AppSearchView.Name + ' (' + CONVERT(nvarchar(100), AppSearchView.SearchViewID) + ')') as WhereUsedDisplay 
                                from [{targetDbName}].[dbo].[AppSearchViewField] as AppSearchViewField
                                inner join [{targetDbName}].[dbo].[AppSearchView] as AppSearchView on (AppSearchViewField.SearchViewID = AppSearchView.SearchViewID)
			                    WHERE EntityID is not null and AppSearchViewField.SearchViewID not in 
			                    (			
				                    select DeleteId from {targetDbName}.[dbo].[Temp_DeleteId_AppSearchView]
			                    )
	                    ) as whereUsedIds
                     ) as whereUsedInfo     on (AppEntityInfo.EntityInfoID = whereUsedInfo.EntityInfoID)
	            where (AppEntityInfo.IsSystemDefine is null or AppEntityInfo.IsSystemDefine = 0) ;  
                    
                
                    
            ";

            query_entityId_ReferenceByOtherApplicatoin = query_entityId_ReferenceByOtherApplicatoin.Replace("{targetDbName}", targetDbName);



            string query = "";

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtEntityId = adpater.ExecuteDataTableRetrievalQuery(query_entityId, new List<System.Data.SqlClient.SqlParameter>());
                    DataTable dtEntityId_Referenced = adpater.ExecuteDataTableRetrievalQuery(query_entityId_ReferenceByOtherApplicatoin, new List<System.Data.SqlClient.SqlParameter>());

                    Dictionary<int, string> dictEntityIdAndCode_OnlyRefByCurrentApp = new Dictionary<int, string>();
                    Dictionary<int, string> dictAllEntityIdAndCode_ReferencedByOtherApp = new Dictionary<int, string>();
                    Dictionary<int, List<string>> dictAllEntityIdAndWhereUsed = new Dictionary<int, List<string>>();
                    Dictionary<int, string> dictEntityIdAndCode_NotToDelete = new Dictionary<int, string>();
                    

                    foreach (DataRow dataRow in dtEntityId_Referenced.Rows)
                    {
                        int? entityId = ControlTypeValueConverter.ConvertValueToInt(dataRow["EntityInfoID"]);
                        string entityCode = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["EntityCode"]).Trim().ToLower();
                        string whereUsed = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["WhereUsedDisplay"]);

                        if (entityId.HasValue)
                        {
                            if (!dictAllEntityIdAndCode_ReferencedByOtherApp.ContainsKey(entityId.Value))
                            {
                                dictAllEntityIdAndCode_ReferencedByOtherApp.Add(entityId.Value, entityCode);
                                dictAllEntityIdAndWhereUsed.Add(entityId.Value, new List<string>() { whereUsed });
                            }
                            else
                            {
                                dictAllEntityIdAndWhereUsed[entityId.Value].Add(whereUsed);
                            }                            
                        }
                    }

                    foreach (DataRow dataRow in dtEntityId.Rows)
                    {
                        int? entityId = ControlTypeValueConverter.ConvertValueToInt(dataRow["EntityInfoID"]);
                        string entityCode = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["EntityCode"]).Trim().ToLower();

                        if (entityId.HasValue && !dictEntityIdAndCode_OnlyRefByCurrentApp.ContainsKey(entityId.Value))
                        {
                            if (!dictAllEntityIdAndCode_ReferencedByOtherApp.ContainsKey(entityId.Value))
                            {
                                dictEntityIdAndCode_OnlyRefByCurrentApp.Add(entityId.Value, entityCode);
                            }
                            else
                            {
                                dictEntityIdAndCode_NotToDelete.Add(entityId.Value, entityCode);
                            }
                        }

                    }

                    foreach (int entityId in dictEntityIdAndCode_OnlyRefByCurrentApp.Keys)
                    {
                        string queryInsertEntityId = @"
                            INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppEntityInfo] (DeleteId) VALUES({entityId});                           
                        ";

                        queryInsertEntityId = queryInsertEntityId.Replace("{targetDbName}", targetDbName)
                            .Replace("{entityId}", entityId.ToString());

                        query += queryInsertEntityId;
                    }


                    if (dictEntityIdAndCode_NotToDelete.Count > 0)
                    {
                        string entityString = string.Join("\n\n", dictEntityIdAndCode_NotToDelete.OrderBy(o => o.Key)
                        .Select(o =>
                        {
                            string display = "[" + o.Key + " - " + o.Value + "]";

                            string usedByDataModel = string.Join(", ", dictAllEntityIdAndWhereUsed[o.Key].Where(p1 => p1.StartsWith("Data Model: ")).Select(p2 => p2.Replace("Data Model: ", "")).OrderBy(p3 => p3));
                            string usedBySearch = string.Join(", ", dictAllEntityIdAndWhereUsed[o.Key].Where(p1 => p1.StartsWith("Search: ")).Select(p2 => p2.Replace("Search: ", "")).OrderBy(p3 => p3));
                            string usedBySearchView = string.Join(", ", dictAllEntityIdAndWhereUsed[o.Key].Where(p1 => p1.StartsWith("Search View: ")).Select(p2 => p2.Replace("Search View: ", "")).OrderBy(p3 => p3));

                            if (!string.IsNullOrWhiteSpace(usedByDataModel))
                            {
                                display += "\n - used by data model: " + usedByDataModel;
                            }

                            if (!string.IsNullOrWhiteSpace(usedBySearch))
                            {
                                display += "\n - used by search: " + usedBySearch;
                            }

                            if (!string.IsNullOrWhiteSpace(usedBySearchView))
                            {
                                display += "\n - used by search view: " + usedBySearchView;
                            }

                            return display;

                        }));


                        string msg = "The entities listed below are integral to the functioning of other applications and will be maintained without deletion. \n\n" + entityString + "\n\n";
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_query_Warning", ValidationItemType.Message, msg));
                    }


                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Prepare Drop User Table Query Failed. \n" + ex.ToString()));
                }
            }


            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppDesktop(string targetDbName)
        {
            return "";
        }






        private static string Unistall_PopulateDeleteIdsTempTable_AppReport(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppReport] (DeleteId)                                                                  
                SELECT [ReportID]		                                                           
                FROM [{targetDbName}].[dbo].[AppReport]
                WHERE ReportID in (
	                SELECT DISTINCT ReportID FROM
	                (
		                select [ReportID] from [{targetDbName}].[dbo].[AppReport] WHERE SaasApplicationID = @ApplicationId
		                UNION
		                select [ReportID] from [{targetDbName}].[dbo].AppApplicationAssetsItem WHERE ReportID is not null and ApplicationID = @ApplicationId
		                UNION
		                select [ReportID] from [{targetDbName}].[dbo].[AppTranscationReport] 
			                WHERE TranscationID in (
				                select DeleteId from [{targetDbName}].[dbo].[Temp_DeleteId_AppTransaction]
			                )
	                ) as ReportIds
                );
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppApplicationAssetsItem(string targetDbName)
        {
            string query = @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_DeleteId_AppApplicationAssetsItem] (DeleteId)
                SELECT AssetsItemID FROM {targetDbName}.[dbo].[AppApplicationAssetsItem] 
                where ApplicationID = @ApplicationId;
                
                ";
            query = query.Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppWebAPIDataExchangeSetting(string targetDbName)
        {
            return "";
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppWinScheduleSetting(string targetDbName)
        {
            return "";
        }



        private static string Unistall_PopulateDeleteIdsTempTable_AppEsiteCatalogue(string targetDbName)
        {
            return "";
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppESiteNavMenu(string targetDbName)
        {
            return "";
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppESitePages(string targetDbName)
        {
            return "";
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppEsite(string targetDbName)
        {
            return "";
        }





        private static string Unistall_PopulateDeleteIdsTempTable_AppIntergrationSetting(string targetDbName)
        {
            return "";
        }

        private static string Unistall_PopulateDeleteIdsTempTable_AppIntergrationSettingParameter(string targetDbName)
        {
            return "";
        }

        private static string Unistall_PrepareDeleteMenuScript(string targetDbName)
        {
            string query_Delete = "";

            query_Delete += @"                           

                            DELETE FROM [{targetDbName}].[dbo].[AppListMenu] WHERE [MenuId] in (
                                select [DeleteId] from [{targetDbName}].[dbo].[Temp_DeleteId_AppListMenu]
                            );                                               

                        ";

            query_Delete = query_Delete.Replace("{targetDbName}", targetDbName);


            return query_Delete;
        }

    }
}
