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
using Newtonsoft.Json;
using System.Data.Common;
  

using APP.Framework;
namespace App.BL
{
    public static partial class AppApplicationImportBL
    {
        private static readonly string AppApplicationImportSetting = "AppApplicationImportSetting";
        private static readonly string AppApplicationImportSetting_ImportDetails = "ImportDetails";

        private static readonly string AppListMenu = "AppListMenu";


        private static readonly string Org_TransactionId = "Org_TransactionId";
        private static readonly string Org_TransactionUnitId = "Org_TransactionUnitId";
        private static readonly string Org_TransactionFieldId = "Org_TransactionFieldId";

        private static readonly string New_TransactionId = "New_TransactionId";
        private static readonly string New_TransactionUnitId = "New_TransactionUnitId";
        private static readonly string New_TransactionFieldId = "New_TransactionFieldId";

        private static readonly string AppTransactionUnitFormula_TransactionUnitFormulaID = "TransactionUnitFormulaID";
        private static readonly string AppTransactionUnitFormula_FormulaExpression = "FormulaExpression";

        private static readonly string AppProjectWorkFlowAction_WorkFlowActionID = "WorkFlowActionID";
        private static readonly string AppProjectWorkFlowAction_Org_WorkFlowActionID = "Org_WorkFlowActionID";
        private static readonly string AppProjectWorkFlowAction_FormulaExpression = "FormulaExpression";
        private static readonly string AppProjectWorkFlowAction_NotificationMessage = "NotificationMessage";

        private static readonly string AppTransaction_TransactionId = "TransactionId";
        private static readonly string AppTransaction_PostProcessStoreProcedure = "PostProcessStoreProcedure";

        private static readonly string AppFormLayoutItem_FormLayoutItemId = "FormLayoutItemId";
        private static readonly string AppFormLayoutItem_ParameterKeyValue = "ParameterKeyValue";
        private static readonly int MaxWhileLoopCount = 1000;

        //select * from AppListMenu where ParentID is null and LinkType = 10 order by Sort
        public static readonly string AppMasterDBConnectionString = AppConfig.GetConnectionString("AppMasterDBConnectionString") ?? string.Empty;
        public static readonly string HostCompanyDbName = new SqlConnectionStringBuilder(AppMasterDBConnectionString).InitialCatalog;
        //public static readonly int UserDbDataSourceId = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(ServerContext.Instance.CompanySettings.CompanyId.Value).DataSourceId;
        //public static readonly string UserDbName = AppCacheManagerBL.GetCurrentCompanyMasterDataSource(ServerContext.Instance.CompanySettings.CompanyId.Value).DatabaseName;



        public static readonly int UserDbDataSourceId = ServerContext.Instance.DataSourceId;
        public static readonly string UserDbName = ServerContext.Instance.CurrentUserDataBaseName;

        //ServerContext.Instance.CurrentUserDbConnectionString = aAppDataSourceRegisterEntity.ConnectionString;
        //        ServerContext.Instance.CurrentUserDataBaseName 

        public static OperationCallResult<bool> ImportApplicationFromHostDBToCurrentUserDB(int hostDbApplicatoinId)
        {

            var result = ImportApplicationFromSourceDBToTargetDB(HostCompanyDbName, UserDbName, hostDbApplicatoinId);

            if (result.IsSuccessful)
            {
                if (UserDbDataSourceId > 0)
                {
                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(UserDbDataSourceId);
                }
            }

            return result;
        }


        public static OperationCallResult<bool> ImportApplicationFromSourceDBToTargetDB(string srcDbName, string targetDbName, int srcApplicatoinId)
        {

            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;



            AppApplicationImportSettingDto importSettingDto = null;

            string queryPrepareSourceTableGuid = PrepareQuerySourceTableGuid(srcDbName);

            string queryDropTempColumnAndTableIfExist = PrepareImportQuery_DropTempTable(targetDbName);
            queryDropTempColumnAndTableIfExist += PrepareImportQuery_Drop_OrgIdColumns(targetDbName);


            string queryCreateImportSettingTable = PrepareImportQuery_CreateImportSettingTable(targetDbName);
            string queryAddOrgIdColumns = PrepareImportQuery_Add_OrgIdColumns(targetDbName);

            string query = PrepareImportQuery_ImportSystemConfigTableData(srcDbName, targetDbName, srcApplicatoinId);

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {

                    adpater.ExecuteExecuteNonQuery(queryPrepareSourceTableGuid, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(queryDropTempColumnAndTableIfExist, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(queryCreateImportSettingTable, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(queryAddOrgIdColumns, new List<SqlParameter>());
                    adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Import Application Failed. \n" + ex.ToString() + "\nQuery:\n" + queryCreateImportSettingTable + "\n" + queryAddOrgIdColumns + "\n" + query));
                }
            }

            //string test = GetOneTableCreateScript(srcDbName, targetDbName, "a33_classroompc", validationResult);

            //validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Message, "\nQuery2:\n" + test));

            if (!validationResult.HasErrors)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, "Importing Configuration: \n" + "Query:\n" + queryAddOrgIdColumns + "\n" + query));

                importSettingDto = InitializeImportSettingDto(targetDbName, validationResult);
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



            if (!validationResult.HasErrors)
            {
                validationResult.Merge(ImportUserTables(srcDbName, targetDbName, importSettingDto));
            }
            

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
                aOperationCallResult.Object = true;
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, "Import Application Success. \n"));

                //validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_querysuccess_Message", ValidationItemType.Message, "Import Application Success. \n" + "\nImport Config Query:\n" + queryAddOrgIdColumns + "\n" + query));
            }


            return aOperationCallResult;
        }


        public static AppApplicationImportSettingDto RetriveOneImportSettingDtoFromTargetDB(string targetDbName, int targetApplicatoinId, ValidationResult validationResult)
        {
            AppApplicationImportSettingDto importSettingDto = null;

            string queryGetImportSetting = @"
                            select top 1 * from [{targetDbName}].[dbo].[AppApplicationImportSetting] where [ApplicationId] = {targetApplicatoinId};
                        ";

            queryGetImportSetting = queryGetImportSetting.Replace("{targetDbName}", targetDbName)
                .Replace("{targetApplicatoinId}", targetApplicatoinId.ToString());


            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    DataTable dtTransFieldTempTable = adpater.ExecuteDataTableRetrievalQuery(queryGetImportSetting, new List<System.Data.SqlClient.SqlParameter>());

                    Dictionary<int, int> dictTransFieldOrgIdAndNewId = new Dictionary<int, int>();

                    if (dtTransFieldTempTable.Rows.Count == 1)
                    {
                        DataRow dataRow = dtTransFieldTempTable.Rows[0];

                        string jsonString = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[AppApplicationImportSetting_ImportDetails]);

                        importSettingDto = JsonConvert.DeserializeObject<AppApplicationImportSettingDto>(jsonString);
                    }
                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Retrieve Import Log Failed. \n" + ex.ToString() + "\nQuery:\n" + queryGetImportSetting + "\n"));
                }
            }

            return importSettingDto;
        }


        public static void SaveImportSettingDto(string targetDbName, AppApplicationImportSettingDto importSettingDto, ValidationResult validationResult)
        {
            string query = "";

            try
            {

                using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    query += @"
                        declare @OrgApplicationId int;
                        declare @NewApplicationId int;
                        SELECT top 1 @NewApplicationId = MenuID, @OrgApplicationId = Org_MenuID from [{targetDbName}].[dbo].[AppListMenu] where Org_MenuID is not null and ParentID is NULL;

                        INSERT INTO [{targetDbName}].[dbo].[AppApplicationImportSetting]
                                   ([ApplicationId]
                                   ,[OrgApplicationId]
                                   ,[AppCreatedByID]
                                   ,[AppCreatedDate]
                                   ,[AppModifiedDate]
                                   ,[AppModifiedByID]
                                   ,[AppCompanyID]
                                   ,[ImportDetails])
                        VALUES
                               (@NewApplicationId
                               ,@OrgApplicationId
                               ,null
                               ,GETUTCDATE()
                               ,null
                               ,null
                               ,null
                               ,N'{ImportDetails}')                        
                    ";

                    string importDetails = JsonConvert.SerializeObject(importSettingDto).Replace("'", "''");

                    query = query.Replace("{targetDbName}", targetDbName).Replace("{ImportDetails}", importDetails);

                    adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                }

            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Save Import Log Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
            }
        }


        private static string PrepareImportQuery_ImportSystemConfigTableData(string srcDbName, string targetDbName, int srcApplicatoinId)
        {
            string query = "";

            query += @"
                declare @ApplicationId int = " + srcApplicatoinId.ToString() + @";
                declare @NewApplicationId int;
                declare @UserDbDataSourceId int = (select top 1 DataSourceID from [" + targetDbName + @"].[dbo].AppDataSourceRegister);
                declare @NewCompanyId int = (select top 1 DataSourceOwnerCompanyID from [" + targetDbName + @"].[dbo].AppDataSourceRegister);
            ";

            //1. Initial Import

            query += PrepareImportQuery_DropTempTable(targetDbName);
            query += PrepareImportQuery_CreateTempTable(targetDbName);
            query += PrepareImportQuery_PopulateTempTableData(srcDbName, targetDbName);

            query += PrepareImportQuery_AppListMenu(srcDbName, targetDbName);  // With GlobalGuid
            query += PrepareImportQuery_AppReport(srcDbName, targetDbName);  // With GlobalGuid
            query += PrepareImportQuery_AppEntityInfo(srcDbName, targetDbName);  // With GlobalGuid
            query += PrepareImportQuery_AppEntitySimpleListValue(srcDbName, targetDbName);
            query += PrepareImportQuery_AppDataSet(srcDbName, targetDbName);  // With GlobalGuid
            query += PrepareImportQuery_AppDataSetParameter(srcDbName, targetDbName);

            query += PrepareImportQuery_AppSearchView(srcDbName, targetDbName);  // With GlobalGuid
            query += PrepareImportQuery_AppSearchViewField(srcDbName, targetDbName);
            query += PrepareImportQuery_AppSearch(srcDbName, targetDbName, "");  // With GlobalGuid
            query += PrepareImportQuery_AppSearchField(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransaction(srcDbName, targetDbName, "");  // With GlobalGuid
            query += PrepareImportQuery_AppTransactionUnit(srcDbName, targetDbName);
            query += PrepareImportQuery_AppTransactionField(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionFieldAggFunction(srcDbName, targetDbName);
            query += PrepareImportQuery_AppTransactionUnitFormula(srcDbName, targetDbName);

            query += PrepareImportQuery_AppConditionalAction(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionDataTransferSetting(srcDbName, targetDbName);
            query += PrepareImportQuery_AppTransactionSaveAsMapping(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionDataLoad(srcDbName, targetDbName);
            query += PrepareImportQuery_AppTranscationDataLoadFieldMapping(srcDbName, targetDbName);

            query += PrepareImportQuery_AppMessage(srcDbName, targetDbName);  // With GlobalGuid

            query += PrepareImportQuery_AppProjectWorkFlowAction(srcDbName, targetDbName);


            query += PrepareImportQuery_AppFormLinkTarget(srcDbName, targetDbName);

            query += PrepareImportQuery_AppViewLinkedSeaechOrUrl(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionUnitLinkedSearch(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionUnitSearchFieldMapping(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionUnitSearchViewFieldMapping(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTransactionNavigation(srcDbName, targetDbName);

            query += PrepareImportQuery_AppTranscationReport(srcDbName, targetDbName);

            query += PrepareImportQuery_AppForm(srcDbName, targetDbName);
            query += PrepareImportQuery_AppFormLayoutItem(srcDbName, targetDbName);

            query += PrepareImportQuery_AppApplicationAssetsItem(srcDbName, targetDbName);

            //query += PrepareImportQuery_AppDesktop(srcDbName, targetDbName);  // With GlobalGuid

            //query += PrepareImportQuery_AppEsite(srcDbName, targetDbName);  // With GlobalGuid
            //query += PrepareImportQuery_AppESitePages(srcDbName, targetDbName);
            ////query += PrepareImportQuery_AppESiteNavMenu(srcDbName, targetDbName);
            //query += PrepareImportQuery_AppListMenu_For_Esite(srcDbName, targetDbName);







            //2. Post Import

            query += PreparePostImportQuery_Update_AppListMenu(srcDbName, targetDbName);

            query += PreparePostImportQuery_Update_AppSearchView(srcDbName, targetDbName);
            query += PreparePostImportQuery_Update_AppSearchViewField(srcDbName, targetDbName);
            query += PreparePostImportQuery_Update_AppSearch(srcDbName, targetDbName);
            query += PreparePostImportQuery_Update_AppSearchField(srcDbName, targetDbName);
            query += PreparePostImportQuery_Update_AppTransaction(srcDbName, targetDbName);
            query += PreparePostImportQuery_Update_AppTransactionUnit(srcDbName, targetDbName);
            query += PreparePostImportQuery_Update_AppTransactionField(srcDbName, targetDbName);



            return query;
        }

        private static string GetOneTableCreateScript(string srcDbName, string targetDbName, string tableName, ValidationResult validationResult)
        {
            string script = "";

            try
            {
                string srcDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, srcDbName);
                var databaseFixture = new DatabaseFixture(srcDbConnectionString, EmSqlType.SqlServer);
                DatabaseTable srcTableDto = databaseFixture.Table(tableName);

                if (srcTableDto != null && srcTableDto.PrimaryKey != null)
                {
                    foreach (var column in srcTableDto.Columns)
                    {
                        column.Tag = AppMetaDataSqlTypeConvertBL.ConvertSqlTypeToNetType(column, databaseFixture.SqlServerType);
                    }

                    script = AppMetaDataBL.PrepareCreateNewTableScript(srcTableDto, databaseFixture);
                }
            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_GenerateQuery_Error", ValidationItemType.Error
                    , "Generate Create Table Script Failed For Table " + srcDbName + ": \n" + ex.ToString()));
            }


            return script;
        }


        private static string PrepareImportQuery_DropTempTable(string targetDbName)
        {

            string query = @"
                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Transaction]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Transaction]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_TransactionUnit]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_TransactionUnit]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_TransactionField]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_TransactionField]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Search]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Search]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_SearchField]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_SearchField]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_SearchView]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_SearchView]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_SearchViewField]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_SearchViewField]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_DataSet]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_DataSet]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Entity]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Entity]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Message]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].Temp_Import_Message
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Command]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Command]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Desktop]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Desktop]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Report]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Report]
                END;

                IF EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[Temp_Import_Esite]') AND type in (N'U'))
                BEGIN	                   
	                DROP TABLE [{targetDbName}].[dbo].[Temp_Import_Esite]
                END;


            ";

            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_CreateImportSettingTable(string targetDbName)
        {
            string query = "";


            query += @"
                IF NOT EXISTS (SELECT * FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[AppApplicationImportSetting]') AND type in (N'U'))
                BEGIN	                   
	                CREATE TABLE [{targetDbName}].[dbo].[AppApplicationImportSetting](
		                [ImportLogId] [int] IDENTITY(1,1) NOT NULL,
		                [ApplicationId] int NULL,
		                [OrgApplicationId] int NULL,
		                [AppCreatedByID] [int] NULL,
		                [AppCreatedDate] [datetime] NULL,
		                [AppModifiedDate] [datetime] NULL,
		                [AppModifiedByID] [int] NULL,		
		                [AppCompanyID] [int] NULL,
		                [ImportDetails] nvarchar(MAX) null
	                 CONSTRAINT [PK_ImportLogId] PRIMARY KEY CLUSTERED 
	                (
		                [ImportLogId] ASC
	                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	                ) ON [PRIMARY]
                END;
            ";

            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_CreateTempTable(string targetDbName)
        {
            string query = "";


            query += @"
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Transaction] (Org_TransactionId int NOT NULL, New_TransactionId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_TransactionUnit] (Org_TransactionUnitId int NOT NULL, Org_TransactionId int NOT NULL, New_TransactionUnitId int NULL, New_TransactionId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_TransactionField] (Org_TransactionFieldId int NOT NULL, Org_TransactionUnitId int NOT NULL, Org_TransactionId int NOT NULL, New_TransactionFieldId int NULL, New_TransactionUnitId int NULL, New_TransactionId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Search] (Org_SearchId int NOT NULL, New_SearchId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_SearchField] (Org_SearchFieldId int NOT NULL, Org_SearchId int NOT NULL, New_SearchFieldId int NULL, New_SearchId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_SearchView] (Org_SearchViewId int NOT NULL, New_SearchViewId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_SearchViewField] (Org_SearchViewFieldId int NOT NULL, Org_SearchViewId int NOT NULL, New_SearchViewFieldId int NULL, New_SearchViewId int NULL)
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_DataSet] (Org_DataSetId int NOT NULL, New_DataSetId int NULL);
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Entity] (Org_EntityInfoID int NOT NULL, Org_EntityCode nvarchar(400) NOT NULL, New_EntityId int NULL, New_EntityCode nvarchar(400) NULL);
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Message] (Org_MessageId int NOT NULL, New_MessageId int NULL);
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Command] (Org_CommandId int NOT NULL, New_CommandId int NULL);
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Desktop] (Org_DesktopId int NOT NULL, New_DesktopId int NULL);
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Report] (Org_ReportId int NOT NULL, New_ReportId int NULL);
                CREATE TABLE [{targetDbName}].[dbo].[Temp_Import_Esite] (Org_EsiteId int NOT NULL, New_EsiteId int NULL);
                


            ";

            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_PopulateTempTableData(string srcDbName, string targetDbName)
        {
            string query = "";

            query += @"
                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_Transaction](Org_TransactionId)
                SELECT DISTINCT TransactionID FROM
                (
	                select TransactionID from [{srcDbName}].[dbo].[AppTransaction] where SaasApplicationID = @ApplicationId
	                UNION
	                select TransactionID FROM [{srcDbName}].[dbo].[AppApplicationAssetsItem] WHERE TransactionID is not null and ApplicationID = @ApplicationId
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
	                select SearchID from [{srcDbName}].[dbo].[AppSearch] where SaasApplicationID = @ApplicationId
	                UNION
	                select SearchID FROM [{srcDbName}].[dbo].[AppApplicationAssetsItem] WHERE SearchID is not null and ApplicationID = @ApplicationId
                ) as SearchIDs

                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_SearchField](Org_SearchFieldId, Org_SearchId)
                SELECT SearchFieldID, SearchID FROM [{srcDbName}].[dbo].[AppSearchField] where SearchID in
                (
	                select Org_SearchId from [{targetDbName}].[dbo].[Temp_Import_Search]
                )



                INSERT INTO [{targetDbName}].[dbo].[Temp_Import_DataSet](Org_DataSetId)
                SELECT DISTINCT DataSetId FROM
                (
	                select DataSetId from [{srcDbName}].[dbo].[AppDataSet] where SaasApplicationID = @ApplicationId
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
		                select EntityInfoID from [{srcDbName}].[dbo].AppEntityInfo where SaasApplicationID = @ApplicationId
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

        private static string PrepareImportQuery_Add_OrgIdColumns(string targetDbName)
        {

            string query = @"
                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppListMenu]', 'Org_MenuID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppListMenu] ADD Org_MenuID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppListMenu]', 'Org_ParentID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppListMenu] ADD Org_ParentID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppReport]', 'Org_ReportID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppReport] ADD Org_ReportID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppEntityInfo]', 'Org_EntityInfoID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppEntityInfo] ADD Org_EntityInfoID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppDataSet]', 'Org_DataSetID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppDataSet] ADD Org_DataSetID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppDataSetParameter]', 'Org_ParameterID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppDataSetParameter] ADD Org_ParameterID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearch]', 'Org_SearchID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearch ADD Org_SearchID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchField]', 'Org_SearchFieldID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchField ADD Org_SearchFieldID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchParameter]', 'Org_SearchparameterID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchParameter ADD Org_SearchparameterID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchSaved]', 'Org_SearchSavedID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchSaved ADD Org_SearchSavedID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchSavedValue]', 'Org_SearchSavedValueID')  IS NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchSavedValue ADD Org_SearchSavedValueID int null
                END;


                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchView]', 'Org_SearchViewID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchView  ADD Org_SearchViewID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchViewField]', 'Org_SearchViewFieldID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchViewField  ADD Org_SearchViewFieldID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppViewLinkedSeaechOrUrl]', 'Org_SearchViewLinkSearchID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppViewLinkedSeaechOrUrl  ADD Org_SearchViewLinkSearchID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransaction]', 'Org_TransactionID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransaction  ADD Org_TransactionID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnit]', 'Org_TransactionUnitID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnit  ADD Org_TransactionUnitID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionField]', 'Org_TransactionFieldID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionField  ADD Org_TransactionFieldID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionFieldAggFunction]', 'Org_FieldAggFunctionID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionFieldAggFunction  ADD Org_FieldAggFunctionID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitDeleteFlow]', 'Org_DeleteFlowID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitDeleteFlow  ADD Org_DeleteFlowID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitFormula]', 'Org_TransactionUnitFormulaID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitFormula  ADD Org_TransactionUnitFormulaID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppConditionalAction]', 'Org_ActionID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppConditionalAction  ADD Org_ActionID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppFormLinkTarget]', 'Org_LinkTargetID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppFormLinkTarget  ADD Org_LinkTargetID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitLinkedSearch]', 'Org_TransactionUnitLinkedSearchId')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch  ADD Org_TransactionUnitLinkedSearchId int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitSearchFieldMapping]', 'Org_TransactionUnitSearchFieldMappingId')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitSearchFieldMapping  ADD Org_TransactionUnitSearchFieldMappingId int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitSearchViewFieldMapping]', 'Org_TransactionUnitSearchViewFieldMappingId')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitSearchViewFieldMapping  ADD Org_TransactionUnitSearchViewFieldMappingId int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionDataTransferSetting]', 'Org_DataTransferSettingID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionDataTransferSetting  ADD Org_DataTransferSettingID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionSaveAsMapping]', 'Org_MappingID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionSaveAsMapping  ADD Org_MappingID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionNavigation]', 'Org_TransNavigationID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionNavigation  ADD Org_TransNavigationID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionDataLoad]', 'Org_DataLoadID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionDataLoad  ADD Org_DataLoadID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTranscationDataLoadFieldMapping]', 'Org_FieldMappingID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTranscationDataLoadFieldMapping  ADD Org_FieldMappingID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTranscationReport]', 'Org_TransctionReportID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTranscationReport  ADD Org_TransctionReportID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppMessage]', 'Org_MessageID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppMessage  ADD Org_MessageID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectOrWorkFlow]', 'Org_ProjectID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectOrWorkFlow  ADD Org_ProjectID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectTaskPredecessor]', 'Org_ProjectActivityPredecessorID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectTaskPredecessor  ADD Org_ProjectActivityPredecessorID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectWorkFlowAction]', 'Org_WorkFlowActionID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectWorkFlowAction  ADD Org_WorkFlowActionID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectWorkFlowCondition]', 'Org_WorkFlowConditionID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectWorkFlowCondition  ADD Org_WorkFlowConditionID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectWorkFlowTask]', 'Org_ProjectWorkFlowTaskID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectWorkFlowTask  ADD Org_ProjectWorkFlowTaskID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppForm]', 'Org_FormID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppForm  ADD Org_FormID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppFormLayoutItem]', 'Org_FormLayoutItemID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppFormLayoutItem  ADD Org_FormLayoutItemID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppIntergrationSetting]', 'Org_IntergrationSettingID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppIntergrationSetting  ADD Org_IntergrationSettingID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppIntergrationSettingParameter]', 'Org_SettingParameterID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppIntergrationSettingParameter  ADD Org_SettingParameterID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppWebAPIDataExchangeSetting]', 'Org_ActionID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppWebAPIDataExchangeSetting  ADD Org_ActionID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppWinScheduleSetting]', 'Org_WinScheduleSeetingID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppWinScheduleSetting  ADD Org_WinScheduleSeetingID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppApplicationAssetsItem]', 'Org_AssetsItemID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppApplicationAssetsItem  ADD Org_AssetsItemID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppEsite]', 'Org_EsiteID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppEsite  ADD Org_EsiteID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppEsiteCatalogue]', 'Org_EsiteCatalogueID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppEsiteCatalogue  ADD Org_EsiteCatalogueID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppESiteNavMenu]', 'Org_MenuID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppESiteNavMenu  ADD Org_MenuID int null
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppESitePages]', 'Org_PageID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppESitePages  ADD Org_PageID int null
                END;
                
                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppDesktop]', 'Org_DesktopID')  IS NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppDesktop  ADD Org_DesktopID int null
                END;
            ";

            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_Drop_OrgIdColumns(string targetDbName)
        {

            string query = @" 
                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppListMenu]', 'Org_MenuID')  IS NOT NULL 
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppListMenu] Drop COLUMN Org_MenuID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppListMenu]', 'Org_ParentID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppListMenu] Drop COLUMN Org_ParentID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppReport]', 'Org_ReportID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppReport] Drop COLUMN Org_ReportID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppEntityInfo]', 'Org_EntityInfoID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppEntityInfo] Drop COLUMN Org_EntityInfoID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppDataSet]', 'Org_DataSetID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppDataSet] Drop COLUMN Org_DataSetID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppDataSetParameter]', 'Org_ParameterID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].[AppDataSetParameter] Drop COLUMN Org_ParameterID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearch]', 'Org_SearchID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearch Drop COLUMN Org_SearchID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchField]', 'Org_SearchFieldID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchField Drop COLUMN Org_SearchFieldID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchParameter]', 'Org_SearchparameterID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchParameter Drop COLUMN Org_SearchparameterID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchSaved]', 'Org_SearchSavedID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchSaved Drop COLUMN Org_SearchSavedID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchSavedValue]', 'Org_SearchSavedValueID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchSavedValue Drop COLUMN Org_SearchSavedValueID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchView]', 'Org_SearchViewID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchView Drop COLUMN Org_SearchViewID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppSearchViewField]', 'Org_SearchViewFieldID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppSearchViewField Drop COLUMN Org_SearchViewFieldID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppViewLinkedSeaechOrUrl]', 'Org_SearchViewLinkSearchID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppViewLinkedSeaechOrUrl Drop COLUMN Org_SearchViewLinkSearchID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransaction]', 'Org_TransactionID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransaction Drop COLUMN Org_TransactionID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnit]', 'Org_TransactionUnitID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnit Drop COLUMN Org_TransactionUnitID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionField]', 'Org_TransactionFieldID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionField Drop COLUMN  Org_TransactionFieldID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionFieldAggFunction]', 'Org_FieldAggFunctionID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionFieldAggFunction  Drop COLUMN Org_FieldAggFunctionID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitDeleteFlow]', 'Org_DeleteFlowID')  IS NOT NULL  
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitDeleteFlow  Drop COLUMN Org_DeleteFlowID
                END;


                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitFormula]', 'Org_TransactionUnitFormulaID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitFormula  DROP COLUMN Org_TransactionUnitFormulaID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppConditionalAction]', 'Org_ActionID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppConditionalAction  DROP COLUMN Org_ActionID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppFormLinkTarget]', 'Org_LinkTargetID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppFormLinkTarget  DROP COLUMN Org_LinkTargetID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitLinkedSearch]', 'Org_TransactionUnitLinkedSearchId')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch  DROP COLUMN Org_TransactionUnitLinkedSearchId
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitSearchFieldMapping]', 'Org_TransactionUnitSearchFieldMappingId')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitSearchFieldMapping  DROP COLUMN Org_TransactionUnitSearchFieldMappingId
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionUnitSearchViewFieldMapping]', 'Org_TransactionUnitSearchViewFieldMappingId')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionUnitSearchViewFieldMapping  DROP COLUMN Org_TransactionUnitSearchViewFieldMappingId
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionDataTransferSetting]', 'Org_DataTransferSettingID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionDataTransferSetting  DROP COLUMN Org_DataTransferSettingID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionSaveAsMapping]', 'Org_MappingID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionSaveAsMapping  DROP COLUMN Org_MappingID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionNavigation]', 'Org_TransNavigationID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionNavigation  DROP COLUMN Org_TransNavigationID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTransactionDataLoad]', 'Org_DataLoadID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTransactionDataLoad  DROP COLUMN Org_DataLoadID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTranscationDataLoadFieldMapping]', 'Org_FieldMappingID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTranscationDataLoadFieldMapping  DROP COLUMN Org_FieldMappingID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppTranscationReport]', 'Org_TransctionReportID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppTranscationReport  DROP COLUMN Org_TransctionReportID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppMessage]', 'Org_MessageID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppMessage  DROP COLUMN Org_MessageID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectOrWorkFlow]', 'Org_ProjectID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectOrWorkFlow  DROP COLUMN Org_ProjectID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectTaskPredecessor]', 'Org_ProjectActivityPredecessorID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectTaskPredecessor  DROP COLUMN Org_ProjectActivityPredecessorID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectWorkFlowAction]', 'Org_WorkFlowActionID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectWorkFlowAction  DROP COLUMN Org_WorkFlowActionID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectWorkFlowCondition]', 'Org_WorkFlowConditionID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectWorkFlowCondition  DROP COLUMN Org_WorkFlowConditionID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppProjectWorkFlowTask]', 'Org_ProjectWorkFlowTaskID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppProjectWorkFlowTask  DROP COLUMN Org_ProjectWorkFlowTaskID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppForm]', 'Org_FormID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppForm  DROP COLUMN Org_FormID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppFormLayoutItem]', 'Org_FormLayoutItemID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppFormLayoutItem  DROP COLUMN Org_FormLayoutItemID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppIntergrationSetting]', 'Org_IntergrationSettingID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppIntergrationSetting  DROP COLUMN Org_IntergrationSettingID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppIntergrationSettingParameter]', 'Org_SettingParameterID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppIntergrationSettingParameter  DROP COLUMN Org_SettingParameterID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppWebAPIDataExchangeSetting]', 'Org_ActionID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppWebAPIDataExchangeSetting  DROP COLUMN Org_ActionID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppWinScheduleSetting]', 'Org_WinScheduleSeetingID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppWinScheduleSetting  DROP COLUMN Org_WinScheduleSeetingID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppApplicationAssetsItem]', 'Org_AssetsItemID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppApplicationAssetsItem  DROP COLUMN Org_AssetsItemID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppEsite]', 'Org_EsiteID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppEsite  DROP COLUMN Org_EsiteID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppEsiteCatalogue]', 'Org_EsiteCatalogueID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppEsiteCatalogue  DROP COLUMN Org_EsiteCatalogueID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppESiteNavMenu]', 'Org_MenuID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppESiteNavMenu  DROP COLUMN Org_MenuID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppESitePages]', 'Org_PageID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppESitePages  DROP COLUMN Org_PageID
                END;

                IF COL_LENGTH ('[{targetDbName}].[dbo].[AppDesktop]', 'Org_DesktopID')  IS NOT NULL
                BEGIN
	                ALTER Table [{targetDbName}].[dbo].AppDesktop  DROP COLUMN Org_DesktopID
                END;

            ";

            query = query.Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppListMenu(string srcDbName, string targetDbName)
        {

            string query = @"                          

                

                INSERT INTO [{targetDbName}].[dbo].[AppListMenu]
                           (Org_MenuID
		                   ,Org_ParentID
                           ,[Name]
                           ,[Description]
                           ,[IconName]
                           ,[RouteCode]
                           ,[Link]
                           ,[Sort]
                           ,[LinkType]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,[AppCreatedByCompanyID]
                           ,[IsSharedbyMutipleCompany]
                           ,[EmDeviceMenuShowMode]
                           ,[GlobalGuid]
                           ,[DisplayModeMenuOrTab]                          
                           ,[LinkParam1]
                           ,[LinkParam2]
                           ,[IconName2]
                           ,[ModuleRegisterID]
                           ,[EsiteID]
                           ,[EmAppMenuItemCategory])
                select [MenuID]
		                ,[ParentID]
                           ,[Name]
                           ,[Description]
                           ,[IconName]
                           ,[RouteCode]
                           ,[Link]
                           ,[Sort]
                           ,[LinkType]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,NULL
                           ,1
                           ,[EmDeviceMenuShowMode]
                           ,[GlobalGuid]
                           ,[DisplayModeMenuOrTab]                         
                           ,[LinkParam1]
                           ,[LinkParam2]
                           ,[IconName2]
                           ,[ModuleRegisterID]
                           ,[EsiteID]
                           ,[EmAppMenuItemCategory] from
                (
                select * from [{srcDbName}].[dbo].[AppListMenu] where MenuID = @ApplicationId
                union
                select * from [{srcDbName}].[dbo].[AppListMenu] where ParentID = @ApplicationId
                union
                select * from [{srcDbName}].[dbo].[AppListMenu] where ParentID in (select MenuID from [{srcDbName}].[dbo].[AppListMenu] where ParentID = @ApplicationId)
                union
                select * from [{srcDbName}].[dbo].[AppListMenu] where ParentID in (select MenuID from [{srcDbName}].[dbo].[AppListMenu] where ParentID in (select MenuID from [{srcDbName}].[dbo].[AppListMenu] where ParentID = @ApplicationId))
                ) as menus;
                
                

                UPDATE [{targetDbName}].[dbo].[AppListMenu] 
                SET ParentID = (select top 1 MenuID from [{targetDbName}].[dbo].[AppListMenu] where Org_MenuID = menuTable.Org_ParentID)
                FROM [{targetDbName}].[dbo].[AppListMenu] as menuTable
                WHERE Org_MenuID is not null;

                INSERT INTO [{targetDbName}].[dbo].[AppSecurityUserListMenu]
               ([MenuID]
               ,[GroupID])
                select [MenuID], 1
                from [{targetDbName}].[dbo].[AppListMenu] 
                where org_menuid is not null
                and MenuID not in (select MenuID from [{targetDbName}].[dbo].[AppSecurityUserListMenu]);
                
                SELECT top 1 @NewApplicationId = MenuID from [{targetDbName}].[dbo].[AppListMenu] where Org_MenuID is not null and ParentID is NULL;

               
            ";

            query = query.Replace("{srcDbName}", srcDbName).Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppReport(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppReport]
                   (Org_ReportID
		           ,[DataSourceID]
                   ,[ReportName]
                   ,[Description]
                   ,[ReportFileName]
                   ,[IsActive]
                   ,[ReportEngineType]
                   ,[AppCreatedByID]
                   ,[AppCreatedDate]
                   ,[AppModifiedDate]
                   ,[AppModifiedByID]
                   ,[AppCreatedByCompanyID]
                   ,[SaasApplicationID]
                   ,[GlobalGuid])
                SELECT 
			                [ReportID]
		                   ,@UserDbDataSourceId
                           ,[ReportName]
                           ,[Description]
                           ,[ReportFileName]
                           ,[IsActive]
                           ,[ReportEngineType]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,NULL
                           ,@NewApplicationId
                           ,[GlobalGuid]
                FROM [{srcDbName}].[dbo].[AppReport]
                WHERE ReportID in (
	                SELECT DISTINCT ReportID FROM
	                (
		                select [ReportID] from [{srcDbName}].[dbo].[AppReport] WHERE SaasApplicationID = @ApplicationId
		                UNION
		                select [ReportID] from [{srcDbName}].[dbo].AppApplicationAssetsItem WHERE ReportID is not null and ApplicationID = @ApplicationId
		                UNION
		                select [ReportID] from [{srcDbName}].[dbo].[AppTranscationReport] 
			                WHERE TranscationID in (
				                select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction]
			                )
	                ) as ReportIds
                );              
                
                
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }


        private static string PrepareImportQuery_AppEntityInfo(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppEntityInfo]
                           (Org_EntityInfoID
		                   ,[EntityCode]
                           ,[Description]
                           ,[EntityType]
                           ,[TableName]
                           ,[IdentityField]
                           ,[DisplayFiled1]
                           ,[DisplayFiled3]
                           ,[DisplayFiled2]
                           ,[QueryText]
                           ,[DataSourceFrom]
                           ,[IsSystemDefine]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,[SchemaOwner]
                           ,[AppCreatedByCompanyID]
                           ,[IsSharedbyMutipleCompany]
                           ,[ColorCodeField]
                           ,[SaasApplicationID]
                           ,[PartnerFilterFiled]
                           ,[ExternalKeyField]
                           ,[OtherSettings]
                           ,[IdentityCoumnDataType]
                            ,[GlobalGuid])
                SELECT [EntityInfoID]
                      ,tempEntiy.New_EntityCode
                      ,[Description]
                      ,[EntityType]
                      ,[TableName]
                      ,[IdentityField]
                      ,[DisplayFiled1]
                      ,[DisplayFiled3]
                      ,[DisplayFiled2]
                      ,[QueryText]
                      ,@UserDbDataSourceId
                      ,[IsSystemDefine]
                      ,[AppCreatedByID]
                      ,[AppCreatedDate]
                      ,[AppModifiedDate]
                      ,[AppModifiedByID]
                      ,[SchemaOwner]
                      ,NULL
                      ,[IsSharedbyMutipleCompany]
                      ,[ColorCodeField]
                      ,@NewApplicationId
                      ,[PartnerFilterFiled]
                      ,[ExternalKeyField]
                      ,[OtherSettings]
                      ,[IdentityCoumnDataType]
                        ,[GlobalGuid]
                  FROM [{srcDbName}].[dbo].[AppEntityInfo] as srcEntityInfo inner join [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntiy
	                on (srcEntityInfo.EntityInfoID = tempEntiy.Org_EntityInfoID)
                  WHERE tempEntiy.Org_EntityInfoID is not null and (srcEntityInfo.IsSystemDefine is null or srcEntityInfo.IsSystemDefine = 0) and tempEntiy.New_EntityID is null;

                UPDATE [{targetDbName}].[dbo].[Temp_Import_Entity] set New_EntityId = entityInfo.EntityInfoID
                FROM [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity inner join [{targetDbName}].[dbo].[AppEntityInfo] as entityInfo
	                on (tempEntity.Org_EntityInfoID = entityInfo.Org_EntityInfoID and entityInfo.Org_EntityInfoID is not null);
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppEntitySimpleListValue(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppEntitySimpleListValue]
                           ([EntityInfoID]
                           ,[Sort]
                           ,[Code]
                           ,[Description]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,[AppCreatedByCompanyID]
                           ,[InternalKey])
                SELECT targetEntityInfo.[EntityInfoID]
                           ,srcListValue.[Sort]
                           ,srcListValue.[Code]
                           ,srcListValue.[Description]
                           ,srcListValue.[AppCreatedByID]
                           ,srcListValue.[AppCreatedDate]
                           ,srcListValue.[AppModifiedDate]
                           ,srcListValue.[AppModifiedByID]
                           ,srcListValue.[AppCreatedByCompanyID]
                           ,srcListValue.[InternalKey]
                  FROM [{srcDbName}].[dbo].[AppEntitySimpleListValue] as srcListValue inner join [{targetDbName}].[dbo].[AppEntityInfo] as targetEntityInfo
	                on (srcListValue.EntityInfoID = targetEntityInfo.Org_EntityInfoID)
                  WHERE targetEntityInfo.Org_EntityInfoID is not null;
                
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppDataSet(string srcDbName, string targetDbName)
        {

            string query = @" 
                
                INSERT INTO [{targetDbName}].[dbo].[AppDataSet]
                           (Org_DataSetID
		                   ,[Name]
                           ,[Description]
                           ,[QueryText]
                           ,[QueryType]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,[DataSourceFrom]
                           ,[AppCreatedByCompanyID]
                           ,[BaseDataSetID]
                           ,[SaasApplicationID]
                           ,[UsageTypeID]
                           ,[BaseTableName]
                           ,[WebApiConfigID]
                           ,[RestApiHeaderKeyValue]
                           ,[RestApiQueryParameterKeyValue]
                           ,[HttpMethod]
                           ,[OtherSettings]
                            ,[GlobalGuid])
                SELECT DataSetID
		                   ,[Name]
                           ,[Description]
                           ,[QueryText]
                           ,[QueryType]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,@UserDbDataSourceId
                           ,null
                           ,[BaseDataSetID]
                           ,@NewApplicationId
                           ,[UsageTypeID]
                           ,[BaseTableName]
                           ,[WebApiConfigID]
                           ,[RestApiHeaderKeyValue]
                           ,[RestApiQueryParameterKeyValue]
                           ,[HttpMethod]
                           ,[OtherSettings]
                            ,[GlobalGuid]
                  FROM [{srcDbName}].[dbo].[AppDataSet] 
                  WHERE DataSetID in (select Org_DataSetID from [{targetDbName}].[dbo].[Temp_Import_DataSet]);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_DataSet] set New_DataSetId = dataset.DataSetID
                FROM [{targetDbName}].[dbo].[Temp_Import_DataSet] as tempDataSet inner join [{targetDbName}].[dbo].[AppDataSet] as dataset
	                on (tempDataSet.Org_DataSetID = dataset.Org_DataSetID);
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppDataSetParameter(string srcDbName, string targetDbName)
        {

            string query = @" 
                
                INSERT INTO [{targetDbName}].[dbo].[AppDataSetParameter]
                           (Org_ParameterID
		                   ,[DataSetID]
                           ,[ParameterName]
                           ,[DataType]
                           ,[DirectionInOut]
                           ,[DefautValue])
                SELECT	ParameterID	
		                ,targetDataSet.DataSetID
                           ,[ParameterName]
                           ,[DataType]
                           ,[DirectionInOut]
                           ,[DefautValue]          
                  FROM [{srcDbName}].[dbo].[AppDataSetParameter] as srcDataSetParameter inner join [{targetDbName}].[dbo].[AppDataSet] as targetDataSet
	                on (srcDataSetParameter.[DataSetID] = targetDataSet.Org_DataSetID)
                  WHERE targetDataSet.Org_DataSetID is not null;
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppSearchView(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppSearchView]
                           ([Org_SearchViewID]
		                   ,[Name]
                           ,[Description]
                           ,[NoSecurity]
                           ,[GridOutputMode]
                           ,[Options]
                           ,[ViewType]
                           ,[WhereUsedDefaultViewId]
                           ,[PivotOrChartSetting]
                           ,[ColumnCount]
                           ,[RowPerPage]
                           ,[IsFilterByCurrentUser]
                           ,[DataSetID]
                           ,[ChartInnerRadius]
                           ,[ChartType]
                           ,[CatalogueSearchID]
                           ,[EntityInternalCode]
                           ,[TransactionID]
                           ,[ProductDetaiViewMapUnitID]           
                           ,[IsMasterEditInSamePage]
                           ,[AppRestResourceUri]
                           ,[AppRestResourceUriDisplay]
                           ,[NbFrozenColumn]
                           ,[UpdateTransctionID]
                           ,[UpdateTransctionRootFieldName]
                           ,[UpdateChildParentFKFieldName]
                           ,[UpdateBaseTranscationUnitID]
                           ,[IsMassUpdateView]
                           ,[IsAllowAddRow]
                           ,[IsAllowDeleteRow]
                           ,[IsAllowUpdateRow]
                           ,[CanlendarDefaultViewMode]
                           ,[IsEnableCalendarMonthView]
                           ,[IsEnableCalendarWeekView]
                           ,[IsEnableCalendarDayView]
                           ,[IsEnableCalendarNavigator]
                           ,[IsDisableClientTimeConvert]
                           ,[SaasApplicationID]
                           ,[IsForPublicAcesss]
                           ,[IsFilterByUserTypeEntity]
                           ,[CalendarStartHour]
                           ,[CalendarEndHour]
                           ,[FilterSearchID]
                           ,[HierachyParentViewID]
                            ,[GlobalGuid]
		                   )
                SELECT SearchViewID
		                ,[Name]
                        ,[Description]
                        ,[NoSecurity]
                        ,[GridOutputMode]
                        ,[Options]
                        ,[ViewType]
                        ,null
                        ,[PivotOrChartSetting]
                        ,[ColumnCount]
                        ,[RowPerPage]
                        ,[IsFilterByCurrentUser]
                        ,tempDataSet.New_DataSetId
                        ,[ChartInnerRadius]
                        ,[ChartType]
                        ,null
                        ,[EntityInternalCode]
                        ,null
                        ,null        
                        ,[IsMasterEditInSamePage]
                        ,[AppRestResourceUri]
                        ,[AppRestResourceUriDisplay]
                        ,[NbFrozenColumn]
                        ,null
                        ,[UpdateTransctionRootFieldName]
                        ,[UpdateChildParentFKFieldName]
                        ,null
                        ,[IsMassUpdateView]
                        ,[IsAllowAddRow]
                        ,[IsAllowDeleteRow]
                        ,[IsAllowUpdateRow]
                        ,[CanlendarDefaultViewMode]
                        ,[IsEnableCalendarMonthView]
                        ,[IsEnableCalendarWeekView]
                        ,[IsEnableCalendarDayView]
                        ,[IsEnableCalendarNavigator]
                        ,[IsDisableClientTimeConvert]
                        ,@NewApplicationId
                        ,[IsForPublicAcesss]
                        ,[IsFilterByUserTypeEntity]
                        ,[CalendarStartHour]
                        ,[CalendarEndHour]
                        ,null
                        ,null
                        ,[GlobalGuid]
                  FROM [{srcDbName}].[dbo].[AppSearchView] as srcSearchView left join [{targetDbName}].[dbo].[Temp_Import_DataSet] as tempDataSet
	                on (srcSearchView.DataSetID = tempDataSet.Org_DataSetId)
                  WHERE SearchViewID in (select Org_SearchViewID from [{targetDbName}].[dbo].[Temp_Import_SearchView]);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_SearchView] set New_SearchViewId = targetTable.SearchViewID
                FROM [{targetDbName}].[dbo].[Temp_Import_SearchView] as tempTable inner join [{targetDbName}].[dbo].[AppSearchView] as targetTable
	                on (tempTable.Org_SearchViewId = targetTable.Org_SearchViewId);


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppSearchViewField(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppSearchViewField]
                           ([Org_SearchViewFieldID]
		                   ,[SearchViewID]
                           ,[IsVisible]
                           ,[DisplayText]
                           ,[Sort]
                           ,[SysTableFiledPath]
                           ,[ControlType]
                           ,[EntityID]
                           ,[DataType]
                           ,[IsGroupBy]
                           ,[GroupByLevel]
                           ,[AggregationFunctionType]
                           ,[IsFilterByCurrentUser]
                           ,[IsMapToChartX]
                           ,[IsMapToChartY]
                           ,[ChartYMappingOrder]
                           ,[TreeLevel]
                           ,[IsTreeNodeID]
                           ,[IsTreeNodeDisplay]
                           ,[MappingSearchFieldID]
                           ,[IsTreeNodeDesc]
                           ,[IsTreeNodeImageUrl]
                           ,[ProductDetaiMapTransFiledID]
                           ,[IsUserDefined1]
                           ,[IsUserDefined2]
                           ,[IsUserDefined3]
                           ,[IsUserDefined4]
                           ,[IsFileFoderID]
                           ,[IsTransRootID]           
                           ,[Width]
                           ,[RowNumber]
                           ,[ColumnNumber]
                           ,[OrderByLevel]
                           ,[IsDescOrder]
                           ,[MassUpdateTransactionFieldID]
                           ,[IsReadOnly]
                           ,[PullCriteriaAsDefaultValueSearchFieldID]
                           ,[EmInternalCodeRegistration]
                           ,[IsPartnerFilterFiled]
                           ,[JoinToParentViewFieldID]
                           ,[IsCalulationField]
                           ,[OtherSettings])
                SELECT SearchViewFieldID
		                   ,targetSearchViewTable.[SearchViewID]          
                           ,[IsVisible]
                           ,[DisplayText]
                           ,[Sort]
                           ,[SysTableFiledPath]
                           ,[ControlType]
                           ,tempEntity.New_EntityId
                           ,[DataType]
                           ,[IsGroupBy]
                           ,[GroupByLevel]
                           ,[AggregationFunctionType]
                           ,srcSearchViewFieldTable.[IsFilterByCurrentUser]
                           ,[IsMapToChartX]
                           ,[IsMapToChartY]
                           ,[ChartYMappingOrder]
                           ,[TreeLevel]
                           ,[IsTreeNodeID]
                           ,[IsTreeNodeDisplay]
                           ,null
                           ,[IsTreeNodeDesc]
                           ,[IsTreeNodeImageUrl]
                           ,[ProductDetaiMapTransFiledID]
                           ,[IsUserDefined1]
                           ,[IsUserDefined2]
                           ,[IsUserDefined3]
                           ,[IsUserDefined4]          
                           ,[IsFileFoderID]
                           ,[IsTransRootID]           
                           ,[Width]
                           ,[RowNumber]
                           ,[ColumnNumber]
                           ,[OrderByLevel]
                           ,[IsDescOrder]
                           ,null
                           ,[IsReadOnly]
                           ,null --[PullCriteriaAsDefaultValueSearchFieldID]
                           ,[EmInternalCodeRegistration]
                           ,[IsPartnerFilterFiled]
                           ,null
                           ,[IsCalulationField]
                           ,[OtherSettings]
                  FROM [{srcDbName}].[dbo].[AppSearchViewField] as srcSearchViewFieldTable inner join [{targetDbName}].[dbo].[AppSearchView] as targetSearchViewTable 
			                on (srcSearchViewFieldTable.SearchViewID = targetSearchViewTable.Org_SearchViewID)
		                left join [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity 
			                on (srcSearchViewFieldTable.EntityID = tempEntity.Org_EntityInfoID)
                  WHERE SearchViewFieldID in (select Org_SearchViewFieldID from [{targetDbName}].[dbo].[Temp_Import_SearchViewField]);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_SearchViewField] set New_SearchViewFieldId = targetTable.SearchViewFieldID, New_SearchViewId = targetTable.SearchViewID
                FROM [{targetDbName}].[dbo].[Temp_Import_SearchViewField] as tempTable inner join [{targetDbName}].[dbo].[AppSearchViewField] as targetTable
	                on (tempTable.Org_SearchViewFieldId = targetTable.Org_SearchViewFieldId);

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }



        private static string PrepareImportQuery_AppSearch(string srcDbName, string targetDbName, string newNameSurffix)
        {

            string query = @" 
               INSERT INTO [{targetDbName}].[dbo].[AppSearch]
                           ([Org_SearchID]
		                   ,[Name]
                           ,[Description]
                           ,[Type]
                           ,[IsBuiltIn]
                           ,[WhereUsedSearchID]
                           ,[SearchViewID]
                           ,[IsAutoExecute]
                           ,[DataSetID]
                           ,[FilterByCurrentUserMappingField]
                           ,[FilterByCurrentUserDomainTypeMappingField]
                           ,[FilterByCurrentUserRoleMappingField]
                           ,[BusinessScopeID]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,[FolderTransactionID]
                           ,[AppCreatedByCompanyID]
                           ,[IsHideAllToolsBar]
                           ,[SaasApplicationID]
                           ,[IsForPublicAcesss]
                           ,[IsFilterByUserTypeEntity]
                            ,[GlobalGuid])
                SELECT SearchID
		                   ,[Name] + '{newNameSurffix}'
                           ,[Description]
                           ,[Type]
                           ,[IsBuiltIn]
                           ,null
                           ,tempSearchView.New_SearchViewId
                           ,[IsAutoExecute]
                           ,tempDataSet.New_DataSetId
                           ,[FilterByCurrentUserMappingField]
                           ,[FilterByCurrentUserDomainTypeMappingField]
                           ,[FilterByCurrentUserRoleMappingField]
                           ,[BusinessScopeID]
                           ,[AppCreatedByID]
                           ,[AppCreatedDate]
                           ,[AppModifiedDate]
                           ,[AppModifiedByID]
                           ,null
                           ,null
                           ,[IsHideAllToolsBar]
                           ,@NewApplicationId
                           ,[IsForPublicAcesss]
                           ,[IsFilterByUserTypeEntity]
                            ,[GlobalGuid]
                  FROM [{srcDbName}].[dbo].[AppSearch] as srcSearch left join [{targetDbName}].[dbo].[Temp_Import_DataSet] as tempDataSet
			                on (srcSearch.DataSetID = tempDataSet.Org_DataSetId)		
		                left join [{targetDbName}].[dbo].[Temp_Import_SearchView] as tempSearchView
			                on (srcSearch.SearchViewID = tempSearchView.Org_SearchViewId)		
                  WHERE SearchID in (select Org_SearchID from [{targetDbName}].[dbo].[Temp_Import_Search]);


                UPDATE [{targetDbName}].[dbo].[Temp_Import_Search] set New_SearchId = targetTable.SearchID
                FROM [{targetDbName}].[dbo].[Temp_Import_Search] as tempTable inner join [{targetDbName}].[dbo].[AppSearch] as targetTable
	                on (tempTable.Org_SearchId = targetTable.Org_SearchId);


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName)
                .Replace("{newNameSurffix}", newNameSurffix);

            return query;
        }

        private static string PrepareImportQuery_AppSearchField(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppSearchField]
                           ([Org_SearchFieldID]
		                   ,[SearchID]
                           ,[Sort]
                           ,[PositionRow]
                           ,[PositionColumn]
                           ,[OperationID]
                           ,[DisplayText]
                           ,[IsVisible]
                           ,[DefaultValue]
                           ,[IsReadOnly]
                           ,[IsAutoPopulate]
                           ,[ParentFieldID]
                           ,[IsLoadOnDemand]
                           ,[SysTableFiledPath]
                           ,[ControlType]
                           ,[EntityID]
                           ,[DataType]
                           ,[IsFilterByCurrentUser]
                           ,[SysTableFiledFullPath]          
                           ,[AppCreatedByCompanyID]
                           ,[IsChangedAutoExecute]
                           ,[StartValueEntityField]
                           ,[EndValueEntityField]
                           ,[StartValueDataSetField]
                           ,[EndValueDataSetField]
                           ,[SubControlType]
                           ,[CascadingRelationTable]
                           ,[CascadingRelationTableParentKeyField]
                           ,[CascadingRelationTableChildKeyField]
                           ,[IsAllowMultipleSelect]
                           ,[MasterEntityFieldlID]
                           ,[InnerEntitySubscribeFiled]
                           ,[IsSkipSearch]
                           ,[DataRetrieveType]
                           ,[CascadingRelationTableSchemaOwner]
                           ,[AppExternalSourceFrom]
                           ,[DdlQueryText]
                           ,[WhereClauseExpress]
                           ,[EmInternalCodeRegistration])
                SELECT SearchFieldID
		                   ,targetSearchTable.[SearchID]
                           ,[Sort]
                           ,[PositionRow]
                           ,[PositionColumn]
                           ,[OperationID]
                           ,[DisplayText]
                           ,[IsVisible]
                           ,[DefaultValue]
                           ,[IsReadOnly]
                           ,[IsAutoPopulate]
                           ,NULL
                           ,[IsLoadOnDemand]
                           ,[SysTableFiledPath]
                           ,[ControlType]
                           ,tempEntity.New_EntityId
                           ,[DataType]
                           ,[IsFilterByCurrentUser]
                           ,[SysTableFiledFullPath]           
                           ,NULL
                           ,[IsChangedAutoExecute]
                           ,[StartValueEntityField]
                           ,[EndValueEntityField]
                           ,[StartValueDataSetField]
                           ,[EndValueDataSetField]
                           ,[SubControlType]
                           ,[CascadingRelationTable]
                           ,[CascadingRelationTableParentKeyField]
                           ,[CascadingRelationTableChildKeyField]
                           ,[IsAllowMultipleSelect]
                           ,[MasterEntityFieldlID]
                           ,[InnerEntitySubscribeFiled]
                           ,[IsSkipSearch]
                           ,[DataRetrieveType]
                           ,[CascadingRelationTableSchemaOwner]
                           ,[AppExternalSourceFrom]
                           ,[DdlQueryText]
                           ,[WhereClauseExpress]
                           ,[EmInternalCodeRegistration]
                  FROM [{srcDbName}].[dbo].[AppSearchField] as srcSearchFieldTable inner join [{targetDbName}].[dbo].[AppSearch] as targetSearchTable 
			                on (srcSearchFieldTable.SearchID = targetSearchTable.Org_SearchID)
		                left join [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity 
			                on (srcSearchFieldTable.EntityID = tempEntity.Org_EntityInfoID)
                  WHERE SearchFieldID in (select Org_SearchFieldID from [{targetDbName}].[dbo].[Temp_Import_SearchField]);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_SearchField] set New_SearchFieldId = targetTable.SearchFieldID, New_SearchId = targetTable.SearchID
                FROM [{targetDbName}].[dbo].[Temp_Import_SearchField] as tempTable inner join [{targetDbName}].[dbo].[AppSearchField] as targetTable
	                on (tempTable.Org_SearchFieldId = targetTable.Org_SearchFieldId);
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }



        private static string PrepareImportQuery_AppTransaction(string srcDbName, string targetDbName, string newNameSurffix)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppTransaction]
                           ([Org_TransactionID]
		                   ,[TransactionName]
                           ,[Description]
                           ,[NeedToCheckRowVersion]
                           ,[TransactionOrganizedType]
                           ,[PostProcessStoreProcedure]
                           ,[ListFilterWhereClause]
                           ,[IsReadOnly]
                           ,[FormID]
                           ,[BusinessScopeID]
                           ,[PrintFormID]           
                           ,[IsEnableFolderSecurity]
                           ,[IsSystemBuitIn]
                           ,[IsNeedToSetCriticalPathTrackFlow]
                           ,[IsNeedToSetComunication]
                           ,[FolderTransactionID]
                           ,[FolderUsageType]
                           ,[DataSourceFrom]
                           ,[EmAppTransBusinessType]
                           ,[MgtRootFolderID]
                           ,[LogicalDisplayEntityID]
                           ,[TransactionFileStorageRootFolderID]
                           ,[AppCreatedByCompanyID]
                           ,[IsExclusiveForOwner]
                           ,[MasterWorkflowID]
                           ,[MasterTransactionID]
                           ,[EmGrandChildEditMode]
                           ,[ConversationBoxDockPosition]
                           ,[PreSaveValidationMethod]
                           ,[IsPhysicalModelTableCreated]
                           ,[IsAllowSaveAs]
                           ,[FormTitleDisplayFieldID]
                           ,[IsShowSaveButton]
                           ,[IsShowCalculateButton]
                           ,[IsShowPrintButton]
                           ,[SaasApplicationID]
                           ,[IsForPublicAcesss]
                           ,[EmNotificaionMethod]
                           ,[NotificationSetting]
                           ,[WebApiConfigID]
                            ,[GlobalGuid]
		                   )
                SELECT TransactionID
		                ,[TransactionName] + '{newNameSurffix}'
                        ,[Description]
                        ,[NeedToCheckRowVersion]
                        ,[TransactionOrganizedType]
                        ,[PostProcessStoreProcedure]
                        ,[ListFilterWhereClause]
                        ,[IsReadOnly]
                        ,NULL -- [FormID]
                        ,[BusinessScopeID]
                        ,NULL -- [PrintFormID]       
                        ,[IsEnableFolderSecurity]
                        ,[IsSystemBuitIn]
                        ,[IsNeedToSetCriticalPathTrackFlow]
                        ,[IsNeedToSetComunication]
                        ,NULL --[FolderTransactionID]
                        ,[FolderUsageType]
                        ,@UserDbDataSourceId
                        ,[EmAppTransBusinessType]
                        ,[MgtRootFolderID]
                        ,NULL --[LogicalDisplayEntityID]
                        ,NULL --[TransactionFileStorageRootFolderID]
                        ,NULL --[AppCreatedByCompanyID]
                        ,[IsExclusiveForOwner]
                        ,NULL --[MasterWorkflowID]
                        ,NULL --[MasterTransactionID]
                        ,[EmGrandChildEditMode]
                        ,[ConversationBoxDockPosition]
                        ,[PreSaveValidationMethod]
                        ,[IsPhysicalModelTableCreated]
                        ,[IsAllowSaveAs]
                        ,NULL --[FormTitleDisplayFieldID]
                        ,[IsShowSaveButton]
                        ,[IsShowCalculateButton]
                        ,[IsShowPrintButton]
                        ,@NewApplicationId
                        ,[IsForPublicAcesss]
                        ,[EmNotificaionMethod]
                        ,[NotificationSetting]
                        ,NULL --[WebApiConfigID]
                        ,[GlobalGuid]
                  FROM [{srcDbName}].[dbo].[AppTransaction]
                  WHERE TransactionID in (select Org_TransactionId from [{targetDbName}].[dbo].[Temp_Import_Transaction]);


                UPDATE [{targetDbName}].[dbo].[Temp_Import_Transaction] set New_TransactionId = targetTable.TransactionID
                FROM [{targetDbName}].[dbo].[Temp_Import_Transaction] as tempTable inner join [{targetDbName}].[dbo].[AppTransaction] as targetTable
	                on (tempTable.Org_TransactionId = targetTable.Org_TransactionID);
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName)
                .Replace("{newNameSurffix}", newNameSurffix);

            return query;
        }


        private static string PrepareImportQuery_AppTransactionUnit(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionUnit]
                           ([Org_TransactionUnitID]		   
                           ,[TransactionID]
                           ,[UnitDisplayName]
                           ,[DataBaseTableName]
                           ,[TransactionFlow]
                           ,[ParentTransactionUnitID]
                           ,[IsReadOnly]
                           ,[IsMatrixUnit]
                           ,[IsSynchToDatabaseTable]
                           ,[IsMatrixPivotUnit]
                           ,[IsMasterSiblingUnit]         
                           ,[IsPrimaryKeyIdentityInsert]
                           ,[SchemaOwner]         
                           ,[IsExclusiveForOwner]
                           ,[IsDisableAddButton]
                           ,[IsDisableDeleteButton]
                           ,[BaseDataBaseTableName]
                           ,[TransactionUnitIentityGuid]
                           ,[TreeViewKeyField]
                           ,[TreeViewParentKeyField]
                           ,[EmGridViewDisplayType]
                           ,[ImageHeight]
                           ,[IsUsedForLoadingAvailableSource]
                           ,[AvailableSourceFilterWhereClause]
                           ,[AvailableSourceFilterByParentTransactionFieldID]
                           ,[AvailableSourceMatchToParentUnitTransactionFieldId]
                           ,[MinRowCount]
                           ,[MaxRowCount]
                           ,[DataSourceQuery]
                           ,[AvailableSourceUnitID])
                SELECT TransactionUnitID
		                   ,targetTransactionTable.TransactionID           
                           ,[UnitDisplayName]
                           ,[DataBaseTableName]
                           ,[TransactionFlow]
                           ,NULL --[ParentTransactionUnitID]
                           ,srcTransactionUnitTable.[IsReadOnly]
                           ,[IsMatrixUnit]
                           ,[IsSynchToDatabaseTable]
                           ,[IsMatrixPivotUnit]
                           ,[IsMasterSiblingUnit]          
                           ,[IsPrimaryKeyIdentityInsert]
                           ,[SchemaOwner]         
                           ,srcTransactionUnitTable.[IsExclusiveForOwner]
                           ,[IsDisableAddButton]
                           ,[IsDisableDeleteButton]
                           ,[BaseDataBaseTableName]
                           ,[TransactionUnitIentityGuid]
                           ,[TreeViewKeyField]
                           ,[TreeViewParentKeyField]
                           ,[EmGridViewDisplayType]
                           ,[ImageHeight]
                           ,[IsUsedForLoadingAvailableSource]
                           ,[AvailableSourceFilterWhereClause]
                           ,[AvailableSourceFilterByParentTransactionFieldID]
                           ,[AvailableSourceMatchToParentUnitTransactionFieldId]
                           ,[MinRowCount]
                           ,[MaxRowCount]
                           ,[DataSourceQuery]
                           ,[AvailableSourceUnitID]
                  FROM [{srcDbName}].[dbo].[AppTransactionUnit] as srcTransactionUnitTable inner join [{targetDbName}].[dbo].[AppTransaction] as targetTransactionTable 
			                on (srcTransactionUnitTable.TransactionID = targetTransactionTable.Org_TransactionID)	
                  WHERE TransactionUnitID in (select Org_TransactionUnitID from [{targetDbName}].[dbo].[Temp_Import_TransactionUnit]);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_TransactionUnit] set New_TransactionUnitId = targetTable.TransactionUnitID, New_TransactionId = targetTable.TransactionID
                FROM [{targetDbName}].[dbo].[Temp_Import_TransactionUnit] as tempTable inner join [{targetDbName}].[dbo].[AppTransactionUnit] as targetTable
	                on (tempTable.Org_TransactionUnitId = targetTable.Org_TransactionUnitId);

                update targetUnit set ParentTransactionUnitID = targetParentUnit.TransactionUnitID
                FROM [{targetDbName}].[dbo].AppTransactionUnit as targetUnit 
	                inner join [{srcDbName}].[dbo].[AppTransactionUnit] as srcUnit on targetUnit.Org_TransactionUnitID=srcUnit.TransactionUnitID
	                left join [{targetDbName}].[dbo].[AppTransactionUnit] as targetParentUnit on srcUnit.ParentTransactionUnitID=targetParentUnit.Org_TransactionUnitID
                WHERE targetUnit.Org_TransactionUnitID is not null and srcUnit.ParentTransactionUnitID is not null;


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }


        private static string PrepareImportQuery_AppTransactionField(string srcDbName, string targetDbName)
        {

            string query = @" 
               INSERT INTO [{targetDbName}].[dbo].[AppTransactionField]
                           ([Org_TransactionFieldID]
		                   ,[TransactionUnitID]
                           ,[DisplayName]
                           ,[DataBaseFieldName]
                           ,[ControlType]
                           ,[DataType]
                           ,[EntityID]
                           ,[InternalCode]
                           ,[NeedValidator]
                           ,[ValidatorType]
                           ,[NBDecimal]
                           ,[SortOrder]
                           ,[MaxCharLegnth]
                           ,[DDLParentLevelID]
                           ,[AutoIncrementSeed]
                           ,[AutoIncrementPrefix]
                           ,[AutoIncrementLastID]
                           ,[IsNeedLog]
                           ,[IsAllowEmpty]
                           ,[ToolTip]
                           ,[IsConvertToUpperCase]
                           ,[DefaultValue]
                           ,[CascadingRelationTable]
                           ,[CascadingRelationTableParentKeyField]
                           ,[CascadingRelationTableChildKeyField]
                           ,[MasterEntityFieldlID]
                           ,[InnerEntitySubscribeFiled]
                           ,[DisplayWidth]
                           ,[IsReadonly]
                           ,[ChildUnitSubscribeParentFieldID]
                           ,[ParentUnitSubscribeChildAggFunctionID]
                           ,[IsGridUseAvailableEntitySource]
                           ,[IsUnique]
                           ,[IsGroupBy]
                           ,[GroupByLevel]
                           ,[MatrixKeyTransactionFieldId]
                           ,[IsPrimaryKey]
                           ,[IsLinkToParentPrimaryKey]
                           ,[IsVisible]
                           ,[IsFilterByCurrentUser]
                           ,[DataRetrieveType]
                           ,[SystemVariableEnumCode]
                           ,[MatrixForeignKeyFieldID]
                           ,[IsPivotRow]
                           ,[IsPivotColumn]
                           ,[AppExternalSourceFrom]
                           ,[DdlQueryText]
                           ,[WhereClauseExpress]
                           ,[DdlForeignUnitID]
                           ,[DdlForeignUnitDisplayDbFieds]           
                           ,[FileControlTypeFolderTransactionID]
                           ,[LinkToParentPrimaryKeyFieldID]
                           ,[RowIdentityGuid]
                           ,[MaxNumber]
                           ,[CascadingRelationTableSchemaOwner]
                           ,[MappingEmSystemTokenField]          
                           ,[IsLogicalDisplay]
                           ,[IsChangeTrigerNotification]
                           ,[SiblingUnitLogicalKeyFieldID]
                           ,[IsFieldExclusiveForOwner]
                           ,[IsAllowEditOnMobileRowPopup]
                           ,[EmInternalCodeRegistration]
                           ,[HostFormLayoutItemID]
                           ,[IsPivotValue]
                           ,[PivotAggregationType]
                           ,[ControlTypeParam1]
                           ,[ControlTypeParam2]
                           ,[ControlTypeParam3]
                           ,[IsPrintVisible]
                           ,[OnChangeTriggerToCommandID]
                           ,[IsTempVariable]
                           ,[MappingToAvailableSourceUnitTransactionFieldID]
                           )
                SELECT TransactionFieldID
		                   ,targetUnitTable.[TransactionUnitID]  
                           ,[DisplayName]
                           ,[DataBaseFieldName]
                           ,[ControlType]
                           ,[DataType]
                           ,tempEntity.New_EntityId
                           ,[InternalCode]
                           ,[NeedValidator]
                           ,[ValidatorType]
                           ,[NBDecimal]
                           ,[SortOrder]
                           ,[MaxCharLegnth]
                           ,null --[DDLParentLevelID]
                           ,[AutoIncrementSeed]
                           ,[AutoIncrementPrefix]
                           ,[AutoIncrementLastID]
                           ,[IsNeedLog]
                           ,[IsAllowEmpty]
                           ,[ToolTip]
                           ,[IsConvertToUpperCase]
                           ,[DefaultValue]
                           ,[CascadingRelationTable]
                           ,[CascadingRelationTableParentKeyField]
                           ,[CascadingRelationTableChildKeyField]
                           ,null --[MasterEntityFieldlID]
                           ,[InnerEntitySubscribeFiled]
                           ,[DisplayWidth]
                           ,srcTransactionFieldTable.[IsReadonly]
                           ,null --[ChildUnitSubscribeParentFieldID]
                           ,null --[ParentUnitSubscribeChildAggFunctionID]
                           ,[IsGridUseAvailableEntitySource]
                           ,[IsUnique]
                           ,[IsGroupBy]
                           ,[GroupByLevel]
                           ,null --[MatrixKeyTransactionFieldId]
                           ,[IsPrimaryKey]
                           ,[IsLinkToParentPrimaryKey]
                           ,[IsVisible]
                           ,[IsFilterByCurrentUser]
                           ,[DataRetrieveType]
                           ,[SystemVariableEnumCode]
                           ,null --[MatrixForeignKeyFieldID]
                           ,[IsPivotRow]
                           ,[IsPivotColumn]
                           ,[AppExternalSourceFrom]
                           ,[DdlQueryText]
                           ,[WhereClauseExpress]
                           ,null --[DdlForeignUnitID]
                           ,[DdlForeignUnitDisplayDbFieds]           
                           ,null --[FileControlTypeFolderTransactionID]
                           ,null --[LinkToParentPrimaryKeyFieldID]
                           ,[RowIdentityGuid]
                           ,[MaxNumber]
                           ,[CascadingRelationTableSchemaOwner]
                           ,[MappingEmSystemTokenField]         
                           ,[IsLogicalDisplay]
                           ,[IsChangeTrigerNotification]
                           ,null --[SiblingUnitLogicalKeyFieldID]
                           ,[IsFieldExclusiveForOwner]
                           ,[IsAllowEditOnMobileRowPopup]
                           ,[EmInternalCodeRegistration]
                           ,null --[HostFormLayoutItemID]
                           ,[IsPivotValue]
                           ,[PivotAggregationType]
                           ,[ControlTypeParam1]
                           ,[ControlTypeParam2]
                           ,[ControlTypeParam3]
                           ,[IsPrintVisible]
                           ,null --[OnChangeTriggerToCommandID]
                           ,[IsTempVariable]
                           ,[MappingToAvailableSourceUnitTransactionFieldID]
                  FROM [{srcDbName}].[dbo].[AppTransactionField] as srcTransactionFieldTable inner join [{targetDbName}].[dbo].[AppTransactionUnit] as targetUnitTable 
			                on (srcTransactionFieldTable.TransactionUnitID = targetUnitTable.Org_TransactionUnitID)	
		                left join [{targetDbName}].[dbo].[Temp_Import_Entity] as tempEntity 
			                on (srcTransactionFieldTable.EntityID = tempEntity.Org_EntityInfoID)
                  WHERE TransactionFieldID in (select Org_TransactionFieldID from [{targetDbName}].[dbo].[Temp_Import_TransactionField]);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_TransactionField] set New_TransactionFieldId = targetTable.TransactionFieldID, New_TransactionUnitId = targetTable.TransactionUnitID
                FROM [{targetDbName}].[dbo].[Temp_Import_TransactionField] as tempTable inner join [{targetDbName}].[dbo].[AppTransactionField] as targetTable
	                on (tempTable.Org_TransactionFieldId = targetTable.Org_TransactionFieldId);
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PrepareImportQuery_AppTransactionFieldAggFunction(string srcDbName, string targetDbName)
        {

            string query = @" 
               INSERT INTO [{targetDbName}].[dbo].[AppTransactionFieldAggFunction]
                           ([Org_FieldAggFunctionID]
		                   ,[AggregationFunctionType]
                           ,[TransactionFieldID]           
                           )
                SELECT FieldAggFunctionID		   
		                   ,[AggregationFunctionType]
                           ,tempTransField.New_TransactionFieldId           
                  FROM [{srcDbName}].[dbo].[AppTransactionFieldAggFunction] as srcTransactionFieldTable inner join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempTransField 
			                on (srcTransactionFieldTable.TransactionFieldID = tempTransField.Org_TransactionFieldId)
                  WHERE TransactionFieldID in (select Org_TransactionFieldID from [{targetDbName}].[dbo].[Temp_Import_TransactionField]);


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }


        private static string PrepareImportQuery_AppTransactionUnitFormula(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionUnitFormula]
                           (
		                   [Org_TransactionUnitFormulaID]      
		                   ,[TransactionUnitID]
                           ,[CaculationFlowSort]
                           ,[FormulaExpression]
                           ,[WarningMessage]
                           ,[FunctionType]
                           ,[OperationType]
                           ,[ConditionFieldID]
                           ,[SwitchTrueFalseType]
                           ,[ChildTransactionUnitID]           
                           ,[WarningHighlightTransFieldID]
                           ,[WarningHighlightStyleID]
                           ,[FormulaName]
                           ,[ApplyToScope]
                           ,[SearchViewId]           
                           )
                SELECT TransactionUnitFormulaID		   
		                   ,tempTransUnit.New_TransactionUnitId
                           ,[CaculationFlowSort]
                           ,[FormulaExpression]
                           ,[WarningMessage]
                           ,[FunctionType]
                           ,[OperationType]
                           ,tempConditionField.New_TransactionFieldId
                           ,[SwitchTrueFalseType]
                           ,tempChildUnit.New_TransactionUnitId           
                           ,null --[WarningHighlightTransFieldID]
                           ,[WarningHighlightStyleID]
                           ,[FormulaName]
                           ,[ApplyToScope]
                           ,null --[SearchViewId]             
                  FROM [{srcDbName}].[dbo].[AppTransactionUnitFormula] as srcTable inner join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempTransUnit 
			                on (srcTable.TransactionUnitID = tempTransUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempChildUnit 
			                on (srcTable.ChildTransactionUnitID = tempChildUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempConditionField
			                on (srcTable.ConditionFieldID = tempConditionField.Org_TransactionFieldId)
                  WHERE TransactionUnitID in (select Org_TransactionUnitId from [{targetDbName}].[dbo].Temp_Import_TransactionUnit);
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppConditionalAction(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppConditionalAction]
                           ([Org_ActionID]
		                   ,[Name]
                           ,[TransactionID]
                           ,[ConditionUnitID]
                           ,[BooleanConditionFieldID]
                           ,[LockingTransactionFieldID]
                           ,[LockingFieldUnitID]
                           ,[IsLockingTransaction]
                           ,[LockingTransactionUnitID]         
                           ,[BooleanConditionFormula]
                           ,[NotificationTemplateMessgeID]        
                           ,[IsLockForSpecailEditPrivilege]
                           ,[UITriggerTransactionFieldID]
                           ,[NeedToHideTransactionFieldID]           
                           )

                SELECT     ActionID
		                   ,[Name]
                           ,tempTransaction.New_TransactionId
                           ,tempConditionUnit.New_TransactionUnitId
                           ,tempConditionField.New_TransactionFieldId
                           ,tempLockField.New_TransactionFieldId
                           ,tempLockingFieldUnit.New_TransactionUnitId
                           ,[IsLockingTransaction]
                           ,tempLockingTransactionUnit.New_TransactionUnitId           
                           ,'' --[BooleanConditionFormula]
                           ,NULL --[NotificationTemplateMessgeID]           
                           ,[IsLockForSpecailEditPrivilege]
                           ,tempUITriggerTransactionFieldID.New_TransactionFieldId
                           ,tempNeedToHideTransactionFieldID.New_TransactionFieldId
                  FROM [{srcDbName}].[dbo].[AppConditionalAction] as srcTable 
		                inner join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempConditionUnit 
			                on (srcTable.ConditionUnitID = tempConditionUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempConditionField
			                on (srcTable.BooleanConditionFieldID = tempConditionField.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempLockField
			                on (srcTable.LockingTransactionFieldID = tempLockField.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempLockingFieldUnit 
			                on (srcTable.LockingFieldUnitID = tempLockingFieldUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempLockingTransactionUnit
			                on (srcTable.LockingTransactionUnitID = tempLockingTransactionUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempUITriggerTransactionFieldID
			                on (srcTable.UITriggerTransactionFieldID = tempUITriggerTransactionFieldID.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempNeedToHideTransactionFieldID
			                on (srcTable.NeedToHideTransactionFieldID = tempNeedToHideTransactionFieldID.Org_TransactionFieldId)
                  WHERE [TransactionID] in (select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction);


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTransactionDataTransferSetting(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionDataTransferSetting]
                           (Org_DataTransferSettingID
		                   ,[TransactionID]
                           ,[InternalCode]
                           ,[Description]          
                           ,[TransferTypeID]
                           ,[DestinationTransactionID]
                           )
                SELECT     DataTransferSettingID
		                   ,tempTransaction.New_TransactionId
                           ,[InternalCode]
                           ,[Description]          
                           ,[TransferTypeID]
                           ,tempDestinationTransaction.New_TransactionId   
                  FROM [{srcDbName}].[dbo].[AppTransactionDataTransferSetting] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction 
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempDestinationTransaction
			                on (srcTable.[DestinationTransactionID] = tempTransaction.Org_TransactionId)
                  WHERE [TransactionID] in (select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction);



            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTransactionSaveAsMapping(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionSaveAsMapping]
                           (Org_MappingID
		                   ,[Name]
                           ,[TransactionID]
                           ,[MappingUnitId]
                           ,[SourceFiledID]
                           ,[TargetFiledID]
                           ,[IsBlankTargetField]           
                           ,[DataTransferSettingID]
                           ,[JsonPropertyPathName]
                           )
                SELECT     MappingID
		                   ,[Name]
                           ,tempTransaction.New_TransactionId
                           ,tempMappingUnitId.New_TransactionUnitId
                           ,tempSourceFiledID.New_TransactionFieldId
                           ,tempTargetFiledID.New_TransactionFieldId
                           ,[IsBlankTargetField]          
                           ,targetDataTransferSetting.DataTransferSettingID
                           ,[JsonPropertyPathName]           
                  FROM [{srcDbName}].[dbo].[AppTransactionSaveAsMapping] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction 
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempMappingUnitId
			                on (srcTable.MappingUnitId = tempMappingUnitId.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempSourceFiledID
			                on (srcTable.SourceFiledID = tempSourceFiledID.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempTargetFiledID
			                on (srcTable.TargetFiledID = tempTargetFiledID.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetDataTransferSetting
			                on (targetDataTransferSetting.Org_DataTransferSettingID is not null and srcTable.DataTransferSettingID = targetDataTransferSetting.Org_DataTransferSettingID)
                  WHERE srcTable.[TransactionID] in (select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction);





            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }


        private static string PrepareImportQuery_AppTransactionDataLoad(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionDataLoad]
                           (Org_DataLoadID
			                ,[DataSetID]
                           ,[TransactionID]
                           ,[LoadName]
                           ,[Description]
                           ,[LoadOrder]
                           ,[TransactionUnitID]          
                           ,[IsAutoExcutedWhenOpenEditForm]
                           )
                SELECT     DataLoadID
		                   ,tempDataSet.New_DataSetId
                           ,tempTransaction.New_TransactionId
                           ,[LoadName]
                           ,[Description]
                           ,[LoadOrder]
                           ,tempTransactionUnit.New_TransactionUnitId
                           ,[IsAutoExcutedWhenOpenEditForm]
                  FROM [{srcDbName}].[dbo].[AppTransactionDataLoad] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction 
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempTransactionUnit
			                on (srcTable.TransactionUnitID = tempTransactionUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_DataSet as tempDataSet
			                on (srcTable.DataSetID = tempDataSet.Org_DataSetID)
                  WHERE [TransactionID] in (select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction);

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTranscationDataLoadFieldMapping(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTranscationDataLoadFieldMapping]
                           (Org_FieldMappingID
			                ,[DataLoadID]
                           ,[TransactionFieldID]
                           ,[DBColumnName]
                           ,[IsConditionMapping]
                           ,[WhereClause]
           
                           )
                SELECT    FieldMappingID
                           ,targetDataLoad.DataLoadID
                           ,tempTransactionField.New_TransactionFieldId
                           ,[DBColumnName]
                           ,[IsConditionMapping]
                           ,[WhereClause]          
                  FROM [{srcDbName}].[dbo].[AppTranscationDataLoadFieldMapping] as srcTable 
		                left join [{targetDbName}].[dbo].AppTransactionDataLoad as targetDataLoad
			                on (srcTable.DataLoadID = targetDataLoad.Org_DataLoadID)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempTransactionField
			                on (srcTable.TransactionFieldID = tempTransactionField.Org_TransactionFieldId)
                  WHERE srcTable.[DataLoadID] in (select Org_DataLoadID from [{targetDbName}].[dbo].AppTransactionDataLoad where Org_DataLoadID is not null);



            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }


        private static string PrepareImportQuery_AppMessage(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppMessage]
                           (Org_MessageID
		                   ,[Subject]
                           ,[Message]
                           ,[ReplyMsgToID]
                           ,[FromEmail]
                           ,[ToList]
                           ,[CCList]
                           ,[BCCList]
                           ,[IsDraft]
                           ,[IsPredefinedTemplate]
                           ,[TransactionID]
                           ,[TransactionRootValueID]
                           ,[ProjectActivityID]
                           ,[ProjectTeamID]
                           ,[ProjectID]
                           ,[MsgUniqueID]          
                           ,[AttachmentFileToken]
                           ,[MessagePostType]
                           ,[MessgaeScopeType]
                           ,[ReminderMinutes]
                           ,[IsEnableReminder]
                           ,[ReminderTargetDate]           
                           ,[TransactionGroupID]
                            ,[GlobalGuid]
                           )
                SELECT     MessageID
		                   ,[Subject]
                           ,[Message]
                           ,[ReplyMsgToID]
                           ,[FromEmail]
                           ,''
                           ,''
                           ,''
                           ,[IsDraft]
                           ,[IsPredefinedTemplate]
                           ,tempTransaction.New_TransactionId
                           ,null
                           ,null
                           ,null
                           ,null
                           ,[MsgUniqueID]        
                           ,[AttachmentFileToken]
                           ,[MessagePostType]
                           ,[MessgaeScopeType]
                           ,[ReminderMinutes]
                           ,[IsEnableReminder]
                           ,[ReminderTargetDate]        
                           ,null
                            ,[GlobalGuid]
                  FROM [{srcDbName}].[dbo].[AppMessage] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction 
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)
                  WHERE IsPredefinedTemplate = 1 and 
	                MessageID in (select Org_MessageId from [{targetDbName}].[dbo].[Temp_Import_Message]);


                UPDATE [{targetDbName}].[dbo].[Temp_Import_Message] set New_MessageId = targetTable.MessageID
                FROM [{targetDbName}].[dbo].[Temp_Import_Message] as tempTable inner join [{targetDbName}].[dbo].[AppMessage] as targetTable
	                on (tempTable.Org_MessageId = targetTable.Org_MessageID);


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }


        private static string PrepareImportQuery_AppProjectWorkFlowAction(string srcDbName, string targetDbName)
        {

            string query = @" 
                INSERT INTO [{targetDbName}].[dbo].[AppProjectWorkFlowAction]
                           (Org_WorkFlowActionID
		                   ,[WorkFlowConditionID]
                           ,[Name]
                           ,[Description]
                           ,[ActionType]
                           ,[UpdateActionTransactionFieldID]
                           ,[FormulaExpression]
                           ,[NextWorkFlowID]
                           ,[RowIdentity]
                           ,[NextTransactionRID]
                           ,[NextTransactionID]
                           ,[NextProjectID]
                           ,[ExcutionDateTime]
                           ,[ExcutedByID]
                           ,[NotificationSubject]
                           ,[NotificationMessage]
                           ,[NotificationDestination]
                           ,[NotificationDestinationUserIDTransactionFiledID]
                           ,[PathUILayout]
                           ,[ActionFlowOrder]           
                           ,[NotificationDestinationRoleIDTransactionFiledID]
                           ,[MessageContentQueryDataSetID]
                           ,[DataSetQeuryString]
                           ,[TransactionID]
                           ,[TransactionUnitID]
                           ,[TransactionFieldID]
                           ,[ChangeTypeForTransactionUnitField]
                           ,[MessageTemplateID]
                           ,[IsNeedToAttachForm]
                           ,[IsNeedToAttachAllFormFiles]
                           ,[DataLoadID]
                           ,[ActionGUID]
                           ,[CommandTransactionID]
                           ,[CommandConditionTransactionFieldID]
                           ,[DataTransferSettingID]
                           ,[OtherOptions]
                           ,[CommandUIOption]
                           ,[CallBackCommandID]
                           ,[CommandSearchViewID]
                           ,[CommandConditionExpression]
                           )
                SELECT     WorkFlowActionID
			                ,[WorkFlowConditionID]
                           ,[Name]
                           ,srcTable.[Description]
                           ,[ActionType]
                           ,null --[UpdateActionTransactionFieldID]
                           ,[FormulaExpression]
                           ,null --[NextWorkFlowID]
                           ,[RowIdentity]
                           ,null --[NextTransactionRID]
                           ,null --[NextTransactionID]
                           ,null --[NextProjectID]
                           ,[ExcutionDateTime]
                           ,[ExcutedByID]
                           ,[NotificationSubject]
                           ,[NotificationMessage]
                           ,[NotificationDestination]
                           ,[NotificationDestinationUserIDTransactionFiledID]
                           ,[PathUILayout]
                           ,[ActionFlowOrder]          
                           ,[NotificationDestinationRoleIDTransactionFiledID]
                           ,[MessageContentQueryDataSetID]
                           ,[DataSetQeuryString]
                           ,null --[TransactionID]
                           ,null --[TransactionUnitID]
                           ,null --[TransactionFieldID]
                           ,null --[ChangeTypeForTransactionUnitField]
                           ,tempMessage.New_MessageId
                           ,[IsNeedToAttachForm]
                           ,[IsNeedToAttachAllFormFiles]
                           ,[DataLoadID]
                           ,[ActionGUID]
                           ,tempTransaction.New_TransactionId
                           ,tempCommandConditionTransactionFieldID.New_TransactionFieldId
                           ,targetDataTransferSetting.DataTransferSettingID
                           ,[OtherOptions]
                           ,[CommandUIOption]
                           ,null --[CallBackCommandID]
                           ,null --[CommandSearchViewID]
                           ,null --[CommandConditionExpression]
                  FROM [{srcDbName}].[dbo].[AppProjectWorkFlowAction] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction 
			                on (srcTable.CommandTransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_Message as tempMessage
			                on (srcTable.MessageTemplateID = tempMessage.Org_MessageId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempCommandConditionTransactionFieldID
			                on (srcTable.CommandConditionTransactionFieldID = tempCommandConditionTransactionFieldID.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetDataTransferSetting
			                on (srcTable.DataTransferSettingID = targetDataTransferSetting.Org_DataTransferSettingID)
                  WHERE CommandTransactionID in (select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction);

                UPDATE [{targetDbName}].[dbo].[Temp_Import_Command] set New_CommandId = targetTable.WorkFlowActionID
                FROM [{targetDbName}].[dbo].[Temp_Import_Command] as tempTable inner join [{targetDbName}].[dbo].[AppProjectWorkFlowAction] as targetTable
	                on (tempTable.Org_CommandId = targetTable.Org_WorkFlowActionID);

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }



        private static string PrepareImportQuery_AppFormLinkTarget(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppFormLinkTarget]
                           ([Org_LinkTargetID]
		                   ,[SearchViewID]
                           ,[SourceColumn1]
                           ,[SourceColumn2]
                           ,[SourceColumn3]
                           ,[TargetColumn1]
                           ,[TargetColumn2]
                           ,[TargetColumn3]
                           ,[NavigationActionName]
                           ,[ActionType]
                           ,[IsReadonly]
                           ,[TransactionUnitID]
                           ,[LinkTargetTransactionID]
                           ,[RowDisplayDbField]
                           ,[SourceConditionColumn]
                           ,[ConditionWarningMessage]
                           ,[GroupName]
                           ,[LinkTargetTransactionGroupID]          
                           ,[LinkTargetSearchID]
                           ,[LinkTargetUsageType]
                           ,[SourceColumnType]
                           ,[LinkTargetUrlOrRouteCode]
                           ,[LayoutDisplayMode]
                           ,[SourceViewColumnID1]
                           ,[SourceViewColumnID2]
                           ,[SourceViewColumnID3]
                           ,[TargetSearchFieldID1]
                           ,[TargetSearchFieldID2]
                           ,[TargetSearchFieldID3]
                           ,[RowDisplayViewColumnID]
                           ,[SourceConditionViewColumnID]
                           ,[ConditionTransFieldID]
                           ,[DataTransferSettingID]
                           ,[Sort]
                           ,[OpennedFormAutoExecuteCommandID]
                           ,[IsPopup]
                           ,[PopupWidth]
                           ,[PopupHeight]
                           ,[IconName]
                           ,[OtherSettings]           
                           )

                SELECT		[LinkTargetID]
		                   ,tempSearchView.New_SearchViewId
                           ,[SourceColumn1]
                           ,[SourceColumn2]
                           ,[SourceColumn3]
                           ,[TargetColumn1]
                           ,[TargetColumn2]
                           ,[TargetColumn3]
                           ,[NavigationActionName]
                           ,srcTable.[ActionType]
                           ,[IsReadonly]
                           ,tempUnit.New_TransactionUnitId
                           ,tempTransaction.New_TransactionId
                           ,[RowDisplayDbField]
                           ,''
                           ,[ConditionWarningMessage]
                           ,[GroupName]
                           ,null
                           ,null
                           ,[LinkTargetUsageType]
                           ,[SourceColumnType]
                           ,null
                           ,[LayoutDisplayMode]
                           ,tempSourceViewColumnID1.New_SearchViewFieldId
                           ,tempSourceViewColumnID2.New_SearchViewFieldId
                           ,tempSourceViewColumnID3.New_SearchViewFieldId
                           ,null
                           ,null
                           ,null
                           ,tempRowDisplayViewColumnId.New_SearchViewFieldId
                           ,tempSourceConditionViewColumnID.New_SearchViewFieldId
                           ,tempConditionTransFieldID.New_TransactionFieldId
                           ,targetDataTransferSetting.[DataTransferSettingID]
                           ,[Sort]
                           ,targetCommand.WorkFlowActionID
                           ,[IsPopup]
                           ,[PopupWidth]
                           ,[PopupHeight]
                           ,[IconName]
                           ,[OtherSettings] 
                  FROM [{srcDbName}].[dbo].[AppFormLinkTarget] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempSearchView
			                on (srcTable.SearchViewID = tempSearchView.Org_SearchViewId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempUnit
			                on (srcTable.TransactionUnitID = tempUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.LinkTargetTransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceViewColumnID1
			                on (srcTable.SourceViewColumnID1 = tempSourceViewColumnID1.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceViewColumnID2
			                on (srcTable.SourceViewColumnID2 = tempSourceViewColumnID2.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceViewColumnID3
			                on (srcTable.SourceViewColumnID3 = tempSourceViewColumnID3.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempRowDisplayViewColumnId
			                on (srcTable.RowDisplayViewColumnID = tempRowDisplayViewColumnId.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceConditionViewColumnID
			                on (srcTable.SourceConditionViewColumnID = tempSourceConditionViewColumnID.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempConditionTransFieldID
			                on (srcTable.ConditionTransFieldID = tempConditionTransFieldID.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetDataTransferSetting
			                on (targetDataTransferSetting.Org_DataTransferSettingID is not null and srcTable.DataTransferSettingID = targetDataTransferSetting.Org_DataTransferSettingID)
	                    left join [{targetDbName}].[dbo].AppProjectWorkFlowAction as targetCommand
			                on (targetCommand.Org_WorkFlowActionID is not null and srcTable.DataTransferSettingID = targetCommand.Org_WorkFlowActionID)
                  WHERE srcTable.SearchViewID in (select Org_SearchViewId from [{targetDbName}].[dbo].Temp_Import_SearchView)
		                or srcTable.TransactionUnitID in (select Org_TransactionUnitId from [{targetDbName}].[dbo].Temp_Import_TransactionUnit);

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppViewLinkedSeaechOrUrl(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppViewLinkedSeaechOrUrl]
                           (    
		                   Org_SearchViewLinkSearchID
		                   ,[SearchViewID]
                           ,[LinkTargetUrlOrRouteCode]         
                           ,[LayoutDisplayMode]
                           ,[SourceViewColumnID1]
                           ,[SourceViewColumnID2]
                           ,[SourceViewColumnID3]
                           ,[TargetSearchFieldID1]
                           ,[TargetSearchFieldID2]
                           ,[TargetSearchFieldID3]
                           ,[DisplayText]
                           ,[Sort]
                           ,[LinkTargetSearchID]
                           ,[IsPopup]
                           ,[PopupWidth]
                           ,[PopupHeight]
                           ,[IconName]
                           ,[RowDisplayViewColumnID]
                           ,[SourceConditionViewColumnID]
                           ,[OtherSettings]
                           )
                SELECT		SearchViewLinkSearchID
			                ,tempSearchView.New_SearchViewId
                           ,[LinkTargetUrlOrRouteCode]         
                           ,[LayoutDisplayMode]
		                   ,tempSourceViewColumnID1.New_SearchViewFieldId
                           ,tempSourceViewColumnID2.New_SearchViewFieldId
                           ,tempSourceViewColumnID3.New_SearchViewFieldId
                           ,tempTargetSearchFieldID1.New_SearchFieldId
                           ,tempTargetSearchFieldID2.New_SearchFieldId
                           ,tempTargetSearchFieldID3.New_SearchFieldId
                           ,[DisplayText]
                           ,[Sort]
                           ,tempLinkTargetSearchID.New_SearchId
                           ,[IsPopup]
                           ,[PopupWidth]
                           ,[PopupHeight]
                           ,[IconName]
                           ,tempRowDisplayViewColumnId.New_SearchViewFieldId
                           ,tempSourceConditionViewColumnID.New_SearchViewFieldId
                           ,[OtherSettings]
                  FROM [{srcDbName}].[dbo].[AppViewLinkedSeaechOrUrl] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempSearchView
			                on (srcTable.SearchViewID = tempSearchView.Org_SearchViewId)	
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceViewColumnID1
			                on (srcTable.SourceViewColumnID1 = tempSourceViewColumnID1.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceViewColumnID2
			                on (srcTable.SourceViewColumnID2 = tempSourceViewColumnID2.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceViewColumnID3
			                on (srcTable.SourceViewColumnID3 = tempSourceViewColumnID3.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempTargetSearchFieldID1
			                on (srcTable.TargetSearchFieldID1 = tempTargetSearchFieldID1.Org_SearchFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempTargetSearchFieldID2
			                on (srcTable.TargetSearchFieldID2 = tempTargetSearchFieldID2.Org_SearchFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempTargetSearchFieldID3
			                on (srcTable.TargetSearchFieldID3 = tempTargetSearchFieldID3.Org_SearchFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempLinkTargetSearchID
			                on (srcTable.LinkTargetSearchID = tempLinkTargetSearchID.Org_SearchId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempRowDisplayViewColumnId
			                on (srcTable.RowDisplayViewColumnID = tempRowDisplayViewColumnId.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSourceConditionViewColumnID
			                on (srcTable.SourceConditionViewColumnID = tempSourceConditionViewColumnID.Org_SearchViewFieldId)		
                  WHERE srcTable.SearchViewID in (select Org_SearchViewId from [{targetDbName}].[dbo].Temp_Import_SearchView);

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTransactionUnitLinkedSearch(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionUnitLinkedSearch]
                           (    
		                   Org_TransactionUnitLinkedSearchId
		                   ,[TransactionUnitId]
                           ,[SearchId]
                           ,[SearchSaveID]
                           ,[SearchViewId]
                           ,[Name]
                           ,[Action]
                           ,[IsSingleSelectedRow]
                           ,[Description]
                           ,[UsageType]
                           ,[GroupName]
                           ,[IsNeedPreValidation]
                           ,[IsNeedPostValidation]
                           ,[CallbackRestResourceUri]
                           ,[TargetTransactionID]
                           ,[ConditionTransFieldID]
                           ,[CallBackCommandID]
                           ,[Sort]
                           ,[IsPopup]
                           ,[PopupWidth]
                           ,[PopupHeight]
                           ,[IconName]
                           ,[OtherSettings]
                           )
                SELECT		[TransactionUnitLinkedSearchId]
			                ,tempUnit.New_TransactionUnitId
                           ,tempSearchID.New_SearchId
                           ,null --[SearchSaveID]
                           ,tempSearchView.New_SearchViewId
                           ,[Name]
                           ,[Action]
                           ,[IsSingleSelectedRow]
                           ,[Description]
                           ,[UsageType]
                           ,[GroupName]       
                           ,[IsNeedPreValidation]
                           ,[IsNeedPostValidation]
                           ,[CallbackRestResourceUri]
                           ,tempTransaction.New_TransactionId
                           ,tempConditionTransFieldID.New_TransactionFieldId
                           ,tempCommand.New_CommandId
                           ,[Sort]
                           ,[IsPopup]
                           ,[PopupWidth]
                           ,[PopupHeight]
                           ,[IconName]
                           ,[OtherSettings]
                  FROM [{srcDbName}].[dbo].[AppTransactionUnitLinkedSearch] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempUnit
			                on (srcTable.TransactionUnitId = tempUnit.Org_TransactionUnitId)	
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempSearchID
			                on (srcTable.SearchId = tempSearchID.Org_SearchId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempSearchView
			                on (srcTable.SearchViewID = tempSearchView.Org_SearchViewId)	
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.TargetTransactionID = tempTransaction.Org_TransactionId)	
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempConditionTransFieldID
			                on (srcTable.ConditionTransFieldID = tempConditionTransFieldID.Org_TransactionFieldId)	
		                left join [{targetDbName}].[dbo].Temp_Import_Command as tempCommand
			                on (srcTable.CallBackCommandID = tempCommand.Org_CommandId)	
                  WHERE srcTable.TransactionUnitId in (select Org_TransactionUnitId from [{targetDbName}].[dbo].Temp_Import_TransactionUnit)
                            and tempSearchView.New_SearchViewId is not null;
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTransactionUnitSearchFieldMapping(string srcDbName, string targetDbName)
        {

            string query = @"  
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionUnitSearchFieldMapping]
                           (    
			                Org_TransactionUnitSearchFieldMappingId
		                   ,[TransactionUnitLinkedSearchId]
                           ,[TransactionFieldId]
                           ,[SearchFieldId]         
                           ,[TargetUnitID]
                           ,[TargetTransactionFieldDBName]
                           )
                SELECT		TransactionUnitSearchFieldMappingId
		                   ,targetLinkedSearch.TransactionUnitLinkedSearchId
                           ,tempTransactionField.New_TransactionFieldId
                           ,tempSearchField.New_SearchFieldId         
                           ,tempUnit.New_TransactionUnitId
                           ,[TargetTransactionFieldDBName]
                  FROM [{srcDbName}].[dbo].[AppTransactionUnitSearchFieldMapping] as srcTable 
		                left join [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch as targetLinkedSearch
			                on (srcTable.TransactionUnitLinkedSearchId = targetLinkedSearch.Org_TransactionUnitLinkedSearchId)				
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempTransactionField
			                on (srcTable.TransactionFieldId = tempTransactionField.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempSearchField
			                on (srcTable.SearchFieldId = tempSearchField.Org_SearchFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempUnit
			                on (srcTable.TargetUnitID = tempUnit.Org_TransactionUnitId)	
                  WHERE srcTable.TransactionUnitLinkedSearchId in (
			                select Org_TransactionUnitLinkedSearchId from [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch
			                where Org_TransactionUnitLinkedSearchId is not null
		                );

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }



        private static string PrepareImportQuery_AppTransactionUnitSearchViewFieldMapping(string srcDbName, string targetDbName)
        {

            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionUnitSearchViewFieldMapping]
                           ( Org_TransactionUnitSearchViewFieldMappingId   
			                ,[TransactionUnitLinkedSearchId]
                           ,[TransactionFieldId]
                           ,[SearchViewFieldId]          
                           ,[ExternalAppFieldMappingCode]
                           ,[IsUnique]
                           ,[TargetUnitID]
                           ,[TargetTransactionFieldDBName]
                           )
                SELECT		TransactionUnitSearchViewFieldMappingId	
		                   ,targetLinkedSearch.TransactionUnitLinkedSearchId
                           ,tempTransactionField.New_TransactionFieldId
                           ,tempSearchViewField.New_SearchViewFieldId         
                           ,[ExternalAppFieldMappingCode]
                           ,[IsUnique]
                           ,tempUnit.New_TransactionUnitId
                           ,[TargetTransactionFieldDBName]
                  FROM [{srcDbName}].[dbo].[AppTransactionUnitSearchViewFieldMapping] as srcTable 
		                left join [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch as targetLinkedSearch
			                on (srcTable.TransactionUnitLinkedSearchId = targetLinkedSearch.Org_TransactionUnitLinkedSearchId)				
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempTransactionField
			                on (srcTable.TransactionFieldId = tempTransactionField.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSearchViewField
			                on (srcTable.SearchViewFieldId = tempSearchViewField.Org_SearchViewFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempUnit
			                on (srcTable.TargetUnitID = tempUnit.Org_TransactionUnitId)	
                  WHERE srcTable.TransactionUnitLinkedSearchId in (
			                select Org_TransactionUnitLinkedSearchId from [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch
			                where Org_TransactionUnitLinkedSearchId is not null
		                );

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTransactionNavigation(string srcDbName, string targetDbName)
        {
            string query = @"  
                INSERT INTO [{targetDbName}].[dbo].[AppTransactionNavigation]
                           (Org_TransNavigationID
		                   ,[TransactionID]
                           ,[QuickSearchID]
                           ,[FolderViewID]
                           ,[IsDefaultView]          
                           )
                SELECT		TransNavigationID
			                ,tempTransaction.New_TransactionId
                           ,tempSearch.New_SearchId
                           ,tempSearchView.New_SearchViewId
                           ,[IsDefaultView]          
                  FROM [{srcDbName}].[dbo].[AppTransactionNavigation] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempSearch
			                on (srcTable.QuickSearchID = tempSearch.Org_SearchId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempSearchView
			                on (srcTable.FolderViewID = tempSearchView.Org_SearchViewId)		
                  WHERE srcTable.TransactionID in (
			                select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction
		                );

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppTranscationReport(string srcDbName, string targetDbName)
        {
            string query = @"  
                INSERT INTO [{targetDbName}].[dbo].[AppTranscationReport]
                           (Org_TransctionReportID
		                   ,[TranscationID]
                           ,[ReportID]
                           ,[ReportDisplayName]          
                           )
                SELECT		TransctionReportID
			                ,tempTransaction.New_TransactionId
                           ,targetReport.ReportID
                           ,[ReportDisplayName]           
                  FROM [{srcDbName}].[dbo].[AppTranscationReport] as srcTable 
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.TranscationID = tempTransaction.Org_TransactionId)
		                left join [{targetDbName}].[dbo].AppReport as targetReport
			                on (srcTable.ReportID = targetReport.Org_ReportID)	
                  WHERE srcTable.TranscationID in (
			                select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction
		                );
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }


        private static string PrepareImportQuery_AppForm(string srcDbName, string targetDbName)
        {
            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppForm]
                           (Org_FormID
		                   ,[Name]
                           ,[Description]
                           ,[LayoutType]
                           ,[FormScope]
                           ,[SystemDefineRouteState]
                           ,[RouteParamter1]
                           ,[RouteParamter2]
                           ,[RouteParamter3]          
                           ,[SearchViewID]           
                           ,[DefaultWidth]
                           ,[DefaultHight]
                           ,[SaasApplicationID]        
                           )
                SELECT		FormID
		                   ,[Name]
                           ,[Description]
                           ,[LayoutType]
                           ,[FormScope]
                           ,[SystemDefineRouteState]
                           ,[RouteParamter1]
                           ,[RouteParamter2]
                           ,[RouteParamter3]          
                           ,null --[SearchViewID]          
                           ,[DefaultWidth]
                           ,[DefaultHight]
                           ,@NewApplicationId          
                  FROM [{srcDbName}].[dbo].[AppForm] as srcTable 			
                  WHERE srcTable.FormID in (
			                select FormID from [{srcDbName}].[dbo].AppTransaction 
			                where FormID is not null and TransactionID in (
				                select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction
			                )
		                ) or 
		                srcTable.FormID in (
			                select PrintFormID from [{srcDbName}].[dbo].AppTransaction 
			                where PrintFormID is not null and TransactionID in (
				                select Org_TransactionId from [{targetDbName}].[dbo].Temp_Import_Transaction
			                )
		                );

                UPDATE targetTransaction set FormID = targetForm.FormID, PrintFormID=targetPrintForm.FormID
                FROM [{targetDbName}].[dbo].[AppTransaction] as targetTransaction
	                inner join [{srcDbName}].[dbo].[AppTransaction] as srcTransaction
		                on targetTransaction.Org_TransactionID = srcTransaction.TransactionID
	                left join [{targetDbName}].[dbo].AppForm as targetForm
		                on (srcTransaction.FormID = targetForm.Org_FormID)
	                left join [{targetDbName}].[dbo].AppForm as targetPrintForm
		                on (srcTransaction.PrintFormID = targetPrintForm.Org_FormID)
                WHERE targetTransaction.Org_TransactionID is not null;

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppFormLayoutItem(string srcDbName, string targetDbName)
        {
            string query = @"                
                update child set FormID = parent1.FormID
                FROM [{srcDbName}].[dbo].[AppFormLayoutItem] as child inner join [{srcDbName}].[dbo].[AppFormLayoutItem] as parent1 on child.UIGridLayoutParentID = parent1.FormLayoutItemID
                where child.FormID is null and parent1.FormID is not null

                INSERT INTO [{targetDbName}].[dbo].[AppFormLayoutItem]
                           (Org_FormLayoutItemID
		                   ,[FormID]
                           ,[WidgetItemType]
                           ,[FlowOrGridLayoutSortOrder]
                           ,[StyleLayoutInfo]
                           ,[DomElementTag]
                           ,[ParameterKeyValue]
                           ,[DisplayTitle]
                           ,[RowIndex]
                           ,[ColumnIndex]
                           ,[RowSpan]
                           ,[ColumnSpan]
                           ,[UIGridLayoutParentID]
                           ,[TransactionFieldID]
                           ,[GridTransactionUnitID]
                           ,[AutoExcuteSearchID]         
                           ,[SearchViewFieldID]         
                           ,[CurrentHostID]
                           ,[ParentHostID]       
                           )
                SELECT		FormLayoutItemID
			                ,targetForm.FormID
                           ,[WidgetItemType]
                           ,[FlowOrGridLayoutSortOrder]
                           ,[StyleLayoutInfo]
                           ,[DomElementTag]
                           ,[ParameterKeyValue]
                           ,[DisplayTitle]
                           ,[RowIndex]
                           ,[ColumnIndex]
                           ,[RowSpan]
                           ,[ColumnSpan]
                           ,null --[UIGridLayoutParentID]
                           ,tempTransactionField.New_TransactionFieldId
                           ,tempTransactionUnit.New_TransactionUnitId
                           ,tempSearch.New_SearchId         
                           ,tempSearchViewField.New_SearchViewFieldId          
                           ,[CurrentHostID]
                           ,[ParentHostID]        
                  FROM [{srcDbName}].[dbo].[AppFormLayoutItem] as srcTable 	
		                inner join [{targetDbName}].[dbo].AppForm as targetForm
			                on (srcTable.FormID = targetForm.Org_FormID)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempTransactionUnit
			                on (srcTable.GridTransactionUnitID = tempTransactionUnit.Org_TransactionUnitId)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempTransactionField
			                on (srcTable.TransactionFieldID = tempTransactionField.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempSearch
			                on (srcTable.AutoExcuteSearchID = tempSearch.Org_SearchId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempSearchViewField
			                on (srcTable.SearchViewFieldID = tempSearchViewField.Org_SearchViewFieldId)		
                  WHERE srcTable.FormID in (
			                select Org_FormID from [{targetDbName}].[dbo].AppForm where Org_FormID is not null
		                ) ;

                UPDATE targetTable set UIGridLayoutParentID = targetParent.FormLayoutItemID
                FROM [{targetDbName}].[dbo].[AppFormLayoutItem] as targetTable 
	                inner join [{srcDbName}].[dbo].[AppFormLayoutItem] as srcTable
		                on targetTable.Org_FormLayoutItemID = srcTable.FormLayoutItemID
	                inner join [{targetDbName}].[dbo].[AppFormLayoutItem] as targetParent
		                on (srcTable.UIGridLayoutParentID = targetParent.Org_FormLayoutItemID)
                WHERE srcTable.UIGridLayoutParentID is not null;

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppApplicationAssetsItem(string srcDbName, string targetDbName)
        {
            string query = @"                
                INSERT INTO [{targetDbName}].[dbo].[AppApplicationAssetsItem]
                           (Org_AssetsItemID
		                   ,[ApplicationID]
                           ,[Name]
                           ,[Description]
                           ,[FormID]
                           ,[TransactionID]
                           ,[ProjectWorkflowID]
                           ,[SearchID]
                           ,[ReportID]
                           ,[DesktopID]      
                           )
                SELECT		AssetsItemID
			                ,@NewApplicationId   
                           ,srcTable.[Name]
                           ,srcTable.[Description]
                           ,targetForm.FormID
                           ,tempTransaction.New_TransactionId
                           ,null --[ProjectWorkflowID]
                           ,tempSearch.New_SearchId
                           ,targetReport.ReportID
                           ,null --[DesktopID]      
                  FROM [{srcDbName}].[dbo].[AppApplicationAssetsItem] as srcTable 
		                left join [{targetDbName}].[dbo].AppForm as targetForm
			                on (srcTable.FormID = targetForm.Org_FormID)
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)		
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempSearch
			                on (srcTable.SearchID = tempSearch.Org_SearchId)
		                left join [{targetDbName}].[dbo].AppReport as targetReport
			                on (srcTable.ReportID = targetReport.Org_ReportID)		
                  WHERE srcTable.[ApplicationID] =  @ApplicationId
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }


        
        private static string PrepareImportQuery_AppEsite(string srcDbName, string targetDbName)
        {
            string query = "";
            //query += @"                
                
                 
            //    INSERT INTO [{targetDbName}].[dbo].[AppEsite]
            //               (Org_EsiteID
		          //         ,[Sort]
            //               ,[Name]
            //               ,[Description]
            //               ,[EmAppEsiteTheme]
            //               ,[LogoImageID1]
            //               ,[LogoImageID2]
            //               ,[IsAllowGuestCheckout]
            //               ,[MyOrderListSearchID]
            //               ,[CustomerInfoDataModelID]
            //               ,[CustomerInfoDBTableName]
            //               ,[CustomerInfoCustomerIdDBFieldName]
            //               ,[CustomerInfoEmailDBFieldName]
            //               ,[CustomerInfoDataTransferID]
            //               ,[SaveCustomerInfoPostActionID]
            //               ,[CustomerShippingAddressSearchID]
            //               ,[CustomerShippingAddressDataModelID]
            //               ,[CustomerShippingAddressDataTransferID]
            //               ,[SaveCustomerShippingAddressPostActionID]
            //               ,[CustomerOrderListSearchID]
            //               ,[OrderDataModelID]
            //               ,[InvoiceDataModelId]
            //               ,[InvoiceReportId]
            //               ,[OrderDataTransferID]
            //               ,[SaveOrderPostActionID]
            //               ,[CustomerPaymentHistorySearchID]
            //               ,[CustomerPaymentHistoryDataModelID]
            //               ,[CustomerPaymentHistoryDataTransferID]
            //               ,[PaymentSuccessfulPostActionActionID]
            //               ,[PaymentFailedPostActionActionID]
            //               ,[EnablePaypalPayment]
            //               ,[PaypalPayment_ApiBaseURL]
            //               ,[PaypalPayment_SB_CLIENT_ID]
            //               ,[PaypalPayment_DefaultCurrencyCode]
            //               ,[EnableVisaPayment]
            //               ,[VisaPayment_ApiBaseURL]
            //               ,[VisaPayment_ApiParam1]
            //               ,[VisaPayment_ApiParam2]
            //               ,[VisaPayment_ApiParam3]
            //               ,[VisaPayment_ApiParam4]
            //               ,[VisaPayment_ApiParam5]
            //               ,[AppCreatedByCompanyID]          
            //               ,[MasteSiteHostLayoutHtmlContent]
            //               ,[EmApplicationType]
            //               ,[MasteSiteCustNavigationJavaScripControl]
            //               ,[SaasApplicationID]
            //               ,[SupplierInfoDataModelID]
            //               ,[SupplierInfoDBTableName]
            //               ,[SupplierInfoIdDBFieldName]
            //               ,[SupplierInfoEmailDBFieldName]
            //               ,[SupplierInfoDataTransferID]
            //               ,[SaveSupplierInfoPostActionID]
            //               ,[SitePublishedBaseUrl]
            //               ,[SitePublishedLoginUrl]
            //               ,[StartPage]
            //               ,[SiteNavMenuSearchID]
            //               ,[DesignPreviewCustomerPartnerId]
            //               ,[DesignPreviewSupplierPartnerId]      
            //               )
            //    SELECT		EsiteID
			         //       ,[Sort]
            //               ,[Name]
            //               ,srcTable.[Description]
            //               ,[EmAppEsiteTheme]
            //               ,[LogoImageID1]
            //               ,[LogoImageID2]
            //               ,[IsAllowGuestCheckout]
            //               ,tempMyOrderListSearchID.New_SearchId
            //               ,tempCustomerInfoDataModelID.New_TransactionId
            //               ,[CustomerInfoDBTableName]
            //               ,[CustomerInfoCustomerIdDBFieldName]
            //               ,[CustomerInfoEmailDBFieldName]
            //               ,targetCustomerInfoDataTransferID.DataTransferSettingID
            //               ,tempSaveCustomerInfoPostActionID.New_CommandId
            //               ,tempCustomerShippingAddressSearchID.New_SearchId
            //               ,tempCustomerShippingAddressDataModelID.New_TransactionId
            //               ,targetCustomerShippingAddressDataTransferID.DataTransferSettingID
            //               ,tempSaveCustomerShippingAddressPostActionID.New_CommandId
            //               ,tempCustomerOrderListSearchID.New_SearchId
            //               ,tempOrderDataModelID.New_TransactionId
            //               ,tempInvoiceDataModelId.New_TransactionId
            //               ,targetAppReport.ReportID
            //               ,targetOrderDataTransferID.DataTransferSettingID
            //               ,tempSaveOrderPostActionID.New_CommandId
            //               ,tempCustomerPaymentHistorySearchID.New_SearchId
            //               ,tempCustomerPaymentHistoryDataModelID.New_TransactionId
            //               ,targetCustomerPaymentHistoryDataTransferID.DataTransferSettingID
            //               ,tempPaymentSuccessfulPostActionActionID.New_CommandId
            //               ,tempPaymentFailedPostActionActionID.New_CommandId
            //               ,[EnablePaypalPayment]
            //               ,[PaypalPayment_ApiBaseURL]
            //               ,[PaypalPayment_SB_CLIENT_ID]
            //               ,[PaypalPayment_DefaultCurrencyCode]
            //               ,[EnableVisaPayment]
            //               ,[VisaPayment_ApiBaseURL]
            //               ,[VisaPayment_ApiParam1]
            //               ,[VisaPayment_ApiParam2]
            //               ,[VisaPayment_ApiParam3]
            //               ,[VisaPayment_ApiParam4]
            //               ,[VisaPayment_ApiParam5]
            //               ,@NewCompanyId  
            //               ,[MasteSiteHostLayoutHtmlContent]
            //               ,[EmApplicationType]
            //               ,[MasteSiteCustNavigationJavaScripControl]
            //               ,@NewApplicationId
            //               ,tempSupplierInfoDataModelID.New_TransactionId
            //               ,[SupplierInfoDBTableName]
            //               ,[SupplierInfoIdDBFieldName]
            //               ,[SupplierInfoEmailDBFieldName]
            //               ,targetSupplierInfoDataTransferID.DataTransferSettingID
            //               ,tempSaveSupplierInfoPostActionID.New_CommandId
            //               ,[SitePublishedBaseUrl]
            //               ,[SitePublishedLoginUrl]
            //               ,[StartPage]
            //               ,tempSiteNavMenuSearchID.New_SearchId
            //               ,null --[DesignPreviewCustomerPartnerId]
            //               ,null --[DesignPreviewSupplierPartnerId]   
            //      FROM [{srcDbName}].[dbo].[AppEsite] as srcTable 	
		          //      left join [{targetDbName}].[dbo].Temp_Import_Search as tempMyOrderListSearchID
			         //       on (srcTable.MyOrderListSearchID = tempMyOrderListSearchID.Org_SearchId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Search as tempCustomerShippingAddressSearchID
			         //       on (srcTable.CustomerShippingAddressSearchID = tempCustomerShippingAddressSearchID.Org_SearchId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Search as tempCustomerOrderListSearchID
			         //       on (srcTable.CustomerOrderListSearchID = tempCustomerOrderListSearchID.Org_SearchId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Search as tempCustomerPaymentHistorySearchID
			         //       on (srcTable.CustomerPaymentHistorySearchID = tempCustomerPaymentHistorySearchID.Org_SearchId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Search as tempSiteNavMenuSearchID
			         //       on (srcTable.SiteNavMenuSearchID = tempSiteNavMenuSearchID.Org_SearchId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempCustomerInfoDataModelID
			         //       on (srcTable.CustomerInfoDataModelID = tempCustomerInfoDataModelID.Org_TransactionId)		
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempCustomerShippingAddressDataModelID
			         //       on (srcTable.CustomerShippingAddressDataModelID = tempCustomerShippingAddressDataModelID.Org_TransactionId)		
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempOrderDataModelID
			         //       on (srcTable.OrderDataModelID = tempOrderDataModelID.Org_TransactionId)	
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempInvoiceDataModelId
			         //       on (srcTable.InvoiceDataModelId = tempInvoiceDataModelId.Org_TransactionId)	
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempCustomerPaymentHistoryDataModelID
			         //       on (srcTable.CustomerPaymentHistoryDataModelID = tempCustomerPaymentHistoryDataModelID.Org_TransactionId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempSupplierInfoDataModelID
			         //       on (srcTable.SupplierInfoDataModelID = tempSupplierInfoDataModelID.Org_TransactionId)	
		          //      left join [{targetDbName}].[dbo].Temp_Import_Command as tempSaveCustomerInfoPostActionID
			         //       on (srcTable.SaveCustomerInfoPostActionID = tempSaveCustomerInfoPostActionID.Org_CommandId)	
		          //      left join [{targetDbName}].[dbo].Temp_Import_Command as tempSaveCustomerShippingAddressPostActionID
			         //       on (srcTable.SaveCustomerShippingAddressPostActionID = tempSaveCustomerShippingAddressPostActionID.Org_CommandId)		
		          //      left join [{targetDbName}].[dbo].Temp_Import_Command as tempSaveOrderPostActionID
			         //       on (srcTable.SaveOrderPostActionID = tempSaveOrderPostActionID.Org_CommandId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Command as tempPaymentSuccessfulPostActionActionID
			         //       on (srcTable.PaymentSuccessfulPostActionActionID = tempPaymentSuccessfulPostActionActionID.Org_CommandId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Command as tempPaymentFailedPostActionActionID
			         //       on (srcTable.PaymentFailedPostActionActionID = tempPaymentFailedPostActionActionID.Org_CommandId)
		          //      left join [{targetDbName}].[dbo].Temp_Import_Command as tempSaveSupplierInfoPostActionID
			         //       on (srcTable.SaveSupplierInfoPostActionID = tempSaveSupplierInfoPostActionID.Org_CommandId)
		          //      left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetCustomerInfoDataTransferID
			         //       on (srcTable.CustomerInfoDataTransferID is not null and srcTable.CustomerInfoDataTransferID = targetCustomerInfoDataTransferID.Org_DataTransferSettingID)	
		
		          //      left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetCustomerShippingAddressDataTransferID
			         //       on (srcTable.CustomerShippingAddressDataTransferID is not null and srcTable.CustomerShippingAddressDataTransferID = targetCustomerShippingAddressDataTransferID.Org_DataTransferSettingID)	
		          //      left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetOrderDataTransferID
			         //       on (srcTable.OrderDataTransferID is not null and srcTable.OrderDataTransferID = targetOrderDataTransferID.Org_DataTransferSettingID)	
		          //      left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetCustomerPaymentHistoryDataTransferID
			         //       on (srcTable.CustomerPaymentHistoryDataTransferID is not null and srcTable.CustomerPaymentHistoryDataTransferID = targetCustomerPaymentHistoryDataTransferID.Org_DataTransferSettingID)	
		          //      left join [{targetDbName}].[dbo].AppTransactionDataTransferSetting as targetSupplierInfoDataTransferID
			         //       on (srcTable.SupplierInfoDataTransferID is not null and srcTable.SupplierInfoDataTransferID = targetSupplierInfoDataTransferID.Org_DataTransferSettingID)
			
		          //      left join [{targetDbName}].[dbo].AppReport as targetAppReport
			         //       on (srcTable.InvoiceReportId is not null and srcTable.InvoiceReportId = targetAppReport.Org_ReportID)	

            //      WHERE srcTable.SaasApplicationID =  @ApplicationId;

            //";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppESitePages(string srcDbName, string targetDbName)
        {
            string query = "";
            //query += @"                
            //    INSERT INTO [{targetDbName}].[dbo].[AppESitePages]
            //               (Org_PageID
		          //         ,[Title]
            //               ,[EMResourceContentType]
            //               ,[ResourceContent]
            //               ,[LoadOrder]
            //               ,[IsActive]
            //               ,[MetaDesciption]
            //               ,[UrlAndHandle]
            //               ,[EsiteID]
            //               ,[TransactionID]          
            //               ,[IsDefault]
            //               ,[ControllerName]
            //               ,[SearchID]
            //               ,[SearchViewID]
            //               ,[IsMasterLayoutPage]
            //               ,[PageJsMethod]
            //               ,[PageCssStyle]
            //               ,[NavigationCtrlJavascript]
            //               ,[FileFullPath]
            //               ,[DesignLayout]      
            //               )
            //    SELECT		PageID
			         //       ,[Title]
            //               ,[EMResourceContentType]
            //               ,[ResourceContent]
            //               ,[LoadOrder]
            //               ,[IsActive]
            //               ,[MetaDesciption]
            //               ,[UrlAndHandle]
            //               ,targetEsite.EsiteID
            //               ,tempTransaction.New_TransactionId           
            //               ,[IsDefault]
            //               ,[ControllerName]
            //               ,tempSearch.New_SearchId
            //               ,tempSearchView.New_SearchViewId
            //               ,[IsMasterLayoutPage]
            //               ,[PageJsMethod]
            //               ,[PageCssStyle]
            //               ,[NavigationCtrlJavascript]
            //               ,[FileFullPath]
            //               ,[DesignLayout]    
            //      FROM [{srcDbName}].[dbo].[AppESitePages] as srcTable 	
		          //      inner join [{targetDbName}].[dbo].AppEsite as targetEsite
			         //       on (srcTable.EsiteID = targetEsite.Org_EsiteID)		
		          //      left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			         //       on (srcTable.TransactionID = tempTransaction.Org_TransactionId)		
		          //      left join [{targetDbName}].[dbo].Temp_Import_Search as tempSearch
			         //       on (srcTable.SearchID = tempSearch.Org_SearchId)	
		          //      left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempSearchView
			         //       on (srcTable.SearchViewID = tempSearchView.Org_SearchViewId)	
            //      WHERE srcTable.EsiteID in (
			         //       select Org_EsiteID from [{targetDbName}].[dbo].AppEsite where Org_EsiteID is not null
		          //      ) ;
            //";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppESiteNavMenu(string srcDbName, string targetDbName)
        {
            string query = @"                
                
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }

        private static string PrepareImportQuery_AppListMenu_For_Esite(string srcDbName, string targetDbName)
        {
            string query = @"                
                
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);
            return query;
        }


        private static ValidationResult PostImported_UpdateFormulaExpression(string srcDbName, string targetDbName)
        {
            ValidationResult validationResult = new ValidationResult();

            string queryGetTransFieldTempTable = @"                
                select * from [{targetDbName}].[dbo].[Temp_Import_TransactionField] 
            ";
            queryGetTransFieldTempTable = queryGetTransFieldTempTable.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);


            string queryGetFormula = @"                
                select * from [{targetDbName}].[dbo].[AppTransactionUnitFormula] 
                where Org_TransactionUnitFormulaID is not null;
            ";
            queryGetFormula = queryGetFormula.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            string queryUpdateFormula = "";



            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtTransFieldTempTable = adpater.ExecuteDataTableRetrievalQuery(queryGetTransFieldTempTable, new List<System.Data.SqlClient.SqlParameter>());

                    Dictionary<int, int> dictTransFieldOrgIdAndNewId = new Dictionary<int, int>();

                    foreach (DataRow dataRow in dtTransFieldTempTable.Rows)
                    {
                        int? orgTransFieldId = ControlTypeValueConverter.ConvertValueToInt(dataRow[Org_TransactionFieldId]);
                        int? newTransFieldId = ControlTypeValueConverter.ConvertValueToInt(dataRow[New_TransactionFieldId]);

                        if (orgTransFieldId.HasValue && newTransFieldId.HasValue)
                        {
                            if (!dictTransFieldOrgIdAndNewId.ContainsKey(orgTransFieldId.Value))
                            {
                                dictTransFieldOrgIdAndNewId.Add(orgTransFieldId.Value, newTransFieldId.Value);
                            }
                        }
                    }


                    DataTable dtOrgImportedFormula = adpater.ExecuteDataTableRetrievalQuery(queryGetFormula, new List<System.Data.SqlClient.SqlParameter>());

                    foreach (DataRow dataRow in dtOrgImportedFormula.Rows)
                    {
                        int? formulaId = ControlTypeValueConverter.ConvertValueToInt(dataRow[AppTransactionUnitFormula_TransactionUnitFormulaID]);
                        string expression = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[AppTransactionUnitFormula_FormulaExpression]);

                        if (formulaId.HasValue && !string.IsNullOrWhiteSpace(expression))
                        {
                            string queryUpdateOneFormula = PreparePostImportQuery_UpdateOneFormulaExpression(dictTransFieldOrgIdAndNewId, expression, formulaId.Value);

                            queryUpdateFormula += queryUpdateOneFormula;
                        }
                    }

                    queryUpdateFormula = queryUpdateFormula.Replace("{srcDbName}", srcDbName)
                        .Replace("{targetDbName}", targetDbName);

                    if (!string.IsNullOrWhiteSpace(queryUpdateFormula))
                    {
                        adpater.ExecuteExecuteNonQuery(queryUpdateFormula, new List<SqlParameter>());
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_query_success", ValidationItemType.Message, "Update Formula Query: \n" + "\nQuery:\n" + queryUpdateFormula + "\n"));
                    }

                }
                catch (Exception ex)
                {
                    string allQuery = queryGetTransFieldTempTable + "\n\n" + queryGetFormula + "\n\n" + queryUpdateFormula + "\n\n";
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Formula Failed. \n" + ex.ToString() + "\nQuery:\n" + allQuery + "\n"));
                }
            }

            return validationResult;
        }


        private static string PreparePostImportQuery_UpdateOneFormulaExpression(Dictionary<int, int> dictTransFieldOrgIdAndNewId, string expression, int formulaId)
        {
            var members = expression.Split(AppTransactionFormulaSetupBL.FormulaConstString, StringSplitOptions.RemoveEmptyEntries);
            foreach (string info in members)
            {
                if (info.Trim().StartsWith(AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix))
                {
                    int? orgId = ControlTypeValueConverter.ConvertValueToInt(info.Replace(AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix, "").Trim());

                    if (orgId.HasValue && dictTransFieldOrgIdAndNewId.ContainsKey(orgId.Value))
                    {
                        int newId = dictTransFieldOrgIdAndNewId[orgId.Value];
                        expression = expression.Replace(info, AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix + newId.ToString());
                    }
                }
                //else if (info.Trim().StartsWith(TaskFieldPrefix))
                //{
                //    string taskFieldDispay = "[" + info.Trim() + "]";
                //    expression = expression.Replace(info, taskFieldDispay);
                //}
            }

            string query = @"
                            UPDATE [{targetDbName}].[dbo].[AppTransactionUnitFormula] SET [FormulaExpression] = N'" + expression
                            + @"' WHERE TransactionUnitFormulaID = " + formulaId.ToString() + @";";


            return query;
        }


        private static string PreparePostImportQuery_Update_AppListMenu(string srcDbName, string targetDbName)
        {
            string query = @"                
                update AppListMenu set link = tempSearch.New_SearchId
                from [{targetDbName}].[dbo].[AppListMenu] as AppListMenu
	                inner join [{targetDbName}].[dbo].Temp_Import_Search as tempSearch 
		                on (AppListMenu.RouteCode = 'MasterDataManagement' and  AppListMenu.Link = tempSearch.Org_SearchId)
                where AppListMenu.Org_MenuID is not null and AppListMenu.RouteCode = 'MasterDataManagement'

                update AppListMenu set link = tempTransaction.New_TransactionId
                from [{targetDbName}].[dbo].[AppListMenu] as AppListMenu
	                inner join [{targetDbName}].[dbo].[Temp_Import_Transaction] as tempTransaction 
		                on ((AppListMenu.RouteCode = 'FormListEdit' or AppListMenu.RouteCode =  'FormMasterDetail') and  AppListMenu.Link =tempTransaction.Org_TransactionId)
                where AppListMenu.Org_MenuID is not null and (AppListMenu.RouteCode = 'FormListEdit' or AppListMenu.RouteCode =  'FormMasterDetail')

                --update AppListMenu set link = AppDesktop.DesktopID
                --from [{targetDbName}].[dbo].[AppListMenu] as AppListMenu
                --	inner join [{targetDbName}].[dbo].AppDesktop as AppDesktop 
                --		on (AppListMenu.RouteCode = 'Dashboard' and  AppListMenu.Link =AppDesktop.org)
                --where AppListMenu.Org_MenuID is not null and AppListMenu.RouteCode = 'Dashboard'

                update AppListMenu set EmDeviceMenuShowMode = 1
                from [{targetDbName}].[dbo].[AppListMenu] as AppListMenu
                where AppListMenu.Org_MenuID is not null and AppListMenu.RouteCode = 'Dashboard';

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppSearchView(string srcDbName, string targetDbName)
        {
            string query = @"                
                UPDATE targetTable set 
			                WhereUsedDefaultViewId = tempWhereUsedDefaultViewId.New_SearchViewId
			                ,CatalogueSearchID = tempCatalogueSearchID.New_SearchId
			                ,TransactionID = tempTransaction.New_TransactionId
			                ,ProductDetaiViewMapUnitID  = tempProductDetaiViewMapUnitID.New_TransactionUnitId
			                ,UpdateTransctionID = tempUpdateTransctionID.New_TransactionId
			                ,UpdateBaseTranscationUnitID = tempUpdateBaseTranscationUnitID.New_TransactionUnitId
			                ,FilterSearchID = tempFilterSearchID.New_SearchId
			                ,HierachyParentViewID = tempHierachyParentViewID.New_SearchViewId
                FROM [{targetDbName}].[dbo].[AppSearchView] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppSearchView] as srcTable  
			                on (targetTable.Org_SearchViewID = srcTable.SearchViewID)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempWhereUsedDefaultViewId
			                on (srcTable.WhereUsedDefaultViewId = tempWhereUsedDefaultViewId.Org_SearchViewId)
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempCatalogueSearchID
			                on (srcTable.CatalogueSearchID = tempCatalogueSearchID.Org_SearchId)
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempTransaction
			                on (srcTable.TransactionID = tempTransaction.Org_TransactionId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempProductDetaiViewMapUnitID
			                on (srcTable.ProductDetaiViewMapUnitID = tempProductDetaiViewMapUnitID.Org_TransactionUnitId)	
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempUpdateTransctionID
			                on (srcTable.UpdateTransctionID = tempUpdateTransctionID.Org_TransactionId)	
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempUpdateBaseTranscationUnitID
			                on (srcTable.UpdateBaseTranscationUnitID = tempUpdateBaseTranscationUnitID.Org_TransactionUnitId)	
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempFilterSearchID
			                on (srcTable.FilterSearchID = tempFilterSearchID.Org_SearchId)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchView as tempHierachyParentViewID
			                on (srcTable.HierachyParentViewID = tempHierachyParentViewID.Org_SearchViewId)	

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppSearchViewField(string srcDbName, string targetDbName)
        {
            string query = @"                
                UPDATE targetTable set 
			                MappingSearchFieldID = tempMappingSearchFieldID.New_SearchFieldId
			                ,MassUpdateTransactionFieldID = tempMassUpdateTransactionFieldID.New_TransactionFieldId
                            ,PullCriteriaAsDefaultValueSearchFieldID = tempPullCriteriaAsDefaultValueSearchFieldID.New_SearchFieldId
			                ,JoinToParentViewFieldID = tempJoinToParentViewFieldID.New_SearchViewFieldId		
                FROM [{targetDbName}].[dbo].[AppSearchViewField] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppSearchViewField] as srcTable  
			                on (targetTable.Org_SearchViewFieldID = srcTable.SearchViewFieldID)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempMappingSearchFieldID
			                on (srcTable.MappingSearchFieldID = tempMappingSearchFieldID.Org_SearchFieldId)		
                        left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempPullCriteriaAsDefaultValueSearchFieldID
			                on (srcTable.PullCriteriaAsDefaultValueSearchFieldID = tempPullCriteriaAsDefaultValueSearchFieldID.Org_SearchFieldId)	
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempMassUpdateTransactionFieldID
			                on (srcTable.MassUpdateTransactionFieldID = tempMassUpdateTransactionFieldID.Org_TransactionFieldId)		
		                left join [{targetDbName}].[dbo].Temp_Import_SearchViewField as tempJoinToParentViewFieldID
			                on (srcTable.JoinToParentViewFieldID = tempJoinToParentViewFieldID.Org_SearchViewFieldId)	

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppSearch(string srcDbName, string targetDbName)
        {
            string query = @"                
                UPDATE targetTable set 
			                WhereUsedSearchID = tempWhereUsedSearchID.New_SearchId		
                FROM [{targetDbName}].[dbo].[AppSearch] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppSearch] as srcTable  
			                on (targetTable.Org_SearchID = srcTable.SearchID)
		                left join [{targetDbName}].[dbo].Temp_Import_Search as tempWhereUsedSearchID
			                on (srcTable.WhereUsedSearchID = tempWhereUsedSearchID.Org_SearchId)		
	

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppSearchField(string srcDbName, string targetDbName)
        {
            string query = @"                
                UPDATE targetTable set 
			                ParentFieldID = tempParentFieldID.New_SearchFieldId,
			                MasterEntityFieldlID = tempMasterEntityFieldlID.New_SearchFieldId		
                FROM [{targetDbName}].[dbo].[AppSearchField] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppSearchField] as srcTable  
			                on (targetTable.Org_SearchFieldID = srcTable.SearchFieldID)
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempParentFieldID
			                on (srcTable.ParentFieldID = tempParentFieldID.Org_SearchFieldId)		
		                left join [{targetDbName}].[dbo].Temp_Import_SearchField as tempMasterEntityFieldlID
			                on (srcTable.MasterEntityFieldlID = tempMasterEntityFieldlID.Org_SearchFieldId)		
            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppTransaction(string srcDbName, string targetDbName)
        {
            string query = @"                
                UPDATE targetTable set 
			                FolderTransactionID = tempFolderTransactionID.New_TransactionId,
			                MasterTransactionID = tempMasterTransactionID.New_TransactionId
			                --LogicalDisplayEntityID = null,
			                --TransactionFileStorageRootFolderID = null,
			                --MasterWorkflowID = null,			
			                --FormTitleDisplayFieldID = null,
			                --WebApiConfigID = null			
                FROM [{targetDbName}].[dbo].[AppTransaction] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppTransaction] as srcTable  
			                on (targetTable.Org_TransactionID = srcTable.TransactionID)
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempFolderTransactionID
			                on (srcTable.FolderTransactionID = tempFolderTransactionID.Org_TransactionId)		
			                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempMasterTransactionID
			                on (srcTable.MasterTransactionID = tempMasterTransactionID.Org_TransactionId);	

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppTransactionUnit(string srcDbName, string targetDbName)
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


            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static string PreparePostImportQuery_Update_AppTransactionField(string srcDbName, string targetDbName)
        {
            string query = @"                
                UPDATE targetTable set 
		                   [DDLParentLevelID] = tempDDLParentLevelID.New_TransactionFieldId,   
                           [MasterEntityFieldlID] = tempMasterEntityFieldlID.New_TransactionFieldId,
                           [ChildUnitSubscribeParentFieldID]=tempChildUnitSubscribeParentFieldID.New_TransactionFieldId,
                           [MappingToAvailableSourceUnitTransactionFieldId] = tempMappingToAvailableSourceUnitTransactionFieldId.New_TransactionFieldId,
                           [ParentUnitSubscribeChildAggFunctionID]=targetAggregationFunction.FieldAggFunctionID,
                           [MatrixKeyTransactionFieldId]=tempMatrixKeyTransactionFieldId.New_TransactionFieldId,
                           [MatrixForeignKeyFieldID]=tempMatrixForeignKeyFieldID.New_TransactionFieldId,
                           [DdlForeignUnitID] =tempDdlForeignUnitID.New_TransactionUnitId,
                           [FileControlTypeFolderTransactionID]=tempFileControlTypeFolderTransactionID.New_TransactionId,
                           [LinkToParentPrimaryKeyFieldID] =tempLinkToParentPrimaryKeyFieldID.New_TransactionFieldId,         
                           [SiblingUnitLogicalKeyFieldID]=tempSiblingUnitLogicalKeyFieldID.New_TransactionFieldId,         
                           [HostFormLayoutItemID] =targetAppFormLayoutItem.FormLayoutItemID,         
                           [OnChangeTriggerToCommandID]=tempOnChangeTriggerToCommandID.New_CommandId
                FROM [{targetDbName}].[dbo].[AppTransactionField] as targetTable 
		                inner join [{srcDbName}].[dbo].[AppTransactionField] as srcTable  
			                on (targetTable.Org_TransactionFieldID = srcTable.TransactionFieldID)
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempDDLParentLevelID
			                on (srcTable.DDLParentLevelID = tempDDLParentLevelID.Org_TransactionFieldId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempMasterEntityFieldlID
			                on (srcTable.MasterEntityFieldlID = tempMasterEntityFieldlID.Org_TransactionFieldId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempChildUnitSubscribeParentFieldID
			                on (srcTable.ChildUnitSubscribeParentFieldID = tempChildUnitSubscribeParentFieldID.Org_TransactionFieldId)	
                        left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempMappingToAvailableSourceUnitTransactionFieldId
			                on (srcTable.[MappingToAvailableSourceUnitTransactionFieldId] = tempMappingToAvailableSourceUnitTransactionFieldId.Org_TransactionFieldId)
		                left join [{targetDbName}].[dbo].AppTransactionFieldAggFunction as targetAggregationFunction
			                on (srcTable.ParentUnitSubscribeChildAggFunctionID = targetAggregationFunction.Org_FieldAggFunctionID)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempMatrixKeyTransactionFieldId
			                on (srcTable.MatrixKeyTransactionFieldId = tempMatrixKeyTransactionFieldId.Org_TransactionFieldId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempMatrixForeignKeyFieldID
			                on (srcTable.MatrixForeignKeyFieldID = tempMatrixForeignKeyFieldID.Org_TransactionFieldId)	
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionUnit as tempDdlForeignUnitID
			                on (srcTable.DdlForeignUnitID = tempDdlForeignUnitID.Org_TransactionUnitId)		
		                left join [{targetDbName}].[dbo].Temp_Import_Transaction as tempFileControlTypeFolderTransactionID
			                on (srcTable.FileControlTypeFolderTransactionID = tempFileControlTypeFolderTransactionID.Org_TransactionId)		
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempLinkToParentPrimaryKeyFieldID
			                on (srcTable.LinkToParentPrimaryKeyFieldID = tempLinkToParentPrimaryKeyFieldID.Org_TransactionFieldId)	
		                left join [{targetDbName}].[dbo].Temp_Import_TransactionField as tempSiblingUnitLogicalKeyFieldID
			                on (srcTable.SiblingUnitLogicalKeyFieldID = tempSiblingUnitLogicalKeyFieldID.Org_TransactionFieldId)	
		                left join [{targetDbName}].[dbo].AppFormLayoutItem as targetAppFormLayoutItem
			                on (srcTable.HostFormLayoutItemID = targetAppFormLayoutItem.Org_FormLayoutItemID)	
		                left join [{targetDbName}].[dbo].Temp_Import_Command as tempOnChangeTriggerToCommandID
			                on (srcTable.OnChangeTriggerToCommandID = tempOnChangeTriggerToCommandID.Org_CommandId)	

            ";

            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);

            return query;
        }

        private static AppApplicationImportSettingDto InitializeImportSettingDto(string targetDbName, ValidationResult validationResult)
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

        private static void PrepareImportSettingDto_ProcessOneAppTable(string targetDbName, string tableName, ValidationResult validationResult, AppApplicationImportSettingDto importSettingDto, DatabaseFixture databaseFixture)
        {
            string query = "";
            try
            {
                DatabaseTable tableDto = databaseFixture.Table(tableName);

                DatabaseColumn pkDto = tableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);

                if (pkDto != null)
                {

                    string orgIdColumnName = "Org_" + pkDto.Name;
                    string IdColumnName = pkDto.Name;

                    query += @"
                                SELECT [{orgIdColumnName}], [{IdColumnName}] FROM [{targetDbName}].[dbo].[{tableName}] WHERE [{orgIdColumnName}] is not null;
                            ";

                    query = query.Replace("{targetDbName}", targetDbName)
                        .Replace("{tableName}", tableName)
                        .Replace("{orgIdColumnName}", orgIdColumnName)
                        .Replace("{IdColumnName}", IdColumnName);

                    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        DataTable dataTable = adpater.ExecuteDataTableRetrievalQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                        if (dataTable.Columns.Count == 2)
                        {

                            foreach (DataRow row in dataTable.Rows)
                            {
                                int? orgId = ControlTypeValueConverter.ConvertValueToInt(row[dataTable.Columns[0].ColumnName]);
                                int? newId = ControlTypeValueConverter.ConvertValueToInt(row[dataTable.Columns[1].ColumnName]);

                                if (orgId.HasValue && newId.HasValue)
                                {
                                    if (!importSettingDto.DictSystemTableOrgIdAndNewId[tableName].ContainsKey(orgId.Value))
                                    {
                                        importSettingDto.DictSystemTableOrgIdAndNewId[tableName].Add(orgId.Value, newId.Value);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_PrepareImportSettingDto_ProcessOneAppTable_Error", ValidationItemType.Error, "Initialize Import Log Failed on Table " + tableName + ". \n" + ex.ToString() + "\n\nInitialize Import Log Query:\n\n" + query + "\n"));
            }
        }


        

        private static void DropTempTablesAndColumns(string targetDbName, ValidationResult validationResult)
        {
            string query = PrepareImportQuery_DropTempTable(targetDbName);
            query += PrepareImportQuery_Drop_OrgIdColumns(targetDbName);

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    query = query.Replace("{targetDbName}", targetDbName);

                    adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());
                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Drop Temp Tables And Columns Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                }

            }
        }

        private static void ClearnFailedImportConfigData(string targetDbName, ValidationResult validationResult)
        {
            string query = @"

                ---Delete Import Failed or Test Temp Data

                update [{targetDbName}].[dbo].[AppTransactionDataLoad]
	                set		DataSetID = NULL,
                           TransactionID = NULL,
                           TransactionUnitID = NULL
                where  Org_DataLoadID is not null

                update [{targetDbName}].[dbo].[AppTransactionField]
	                set		[DDLParentLevelID] = NULL,
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
                where  Org_TransactionFieldID is not null

                update [{targetDbName}].[dbo].[AppTransactionUnit]
	                set		ParentTransactionUnitID = NULL
			                ,AvailableSourceUnitID = NULL		
                where  Org_TransactionUnitID is not null


                update [{targetDbName}].[dbo].[AppTransaction]
	                set		FolderTransactionID = NULL
			                ,MasterTransactionID = NULL		
                where  Org_TransactionId is not null



                update [{targetDbName}].[dbo].[AppSearchField] 
	                set		ParentFieldID = NULL
			                ,MasterEntityFieldlID = NULL		
                where Org_SearchFieldID is not null



                update [{targetDbName}].[dbo].[AppSearch] 
	                set WhereUsedSearchID = NULL		
                where Org_SearchID is not null


                update [{targetDbName}].[dbo].[AppSearchViewField] 
	                set MappingSearchFieldID = NULL
			                ,MassUpdateTransactionFieldID = NULL
                            ,PullCriteriaAsDefaultValueSearchFieldID = NULL
			                ,JoinToParentViewFieldID = NULL		
                where Org_SearchViewFieldID is not null


                update [{targetDbName}].[dbo].[AppSearchView] 
	                set WhereUsedDefaultViewId = null
			                ,CatalogueSearchID = null
			                ,TransactionID = null
			                ,ProductDetaiViewMapUnitID  = null
			                ,UpdateTransctionID = null
			                ,UpdateBaseTranscationUnitID =null
			                ,FilterSearchID = null
			                ,HierachyParentViewID = null
                where Org_SearchViewID is not null





                
                

                delete from [{targetDbName}].[dbo].AppIntergrationSettingParameter where Org_SettingParameterID is not null
                delete from [{targetDbName}].[dbo].AppIntergrationSetting where Org_IntergrationSettingID is not null
                delete from [{targetDbName}].[dbo].AppWebAPIDataExchangeSetting where Org_ActionID is not null
                delete from [{targetDbName}].[dbo].AppWinScheduleSetting where Org_WinScheduleSeetingID is not null

                delete from [{targetDbName}].[dbo].AppApplicationAssetsItem where Org_AssetsItemID is not null


                delete from [{targetDbName}].[dbo].AppEsiteCatalogue where Org_EsiteCatalogueID is not null
                delete from [{targetDbName}].[dbo].AppESiteNavMenu where Org_MenuID is not null
                delete from [{targetDbName}].[dbo].AppESitePages where Org_PageID is not null
                delete from [{targetDbName}].[dbo].AppEsite where Org_EsiteID is not null


                delete from [{targetDbName}].[dbo].AppTransactionUnitSearchFieldMapping where Org_TransactionUnitSearchFieldMappingId is not null
                delete from [{targetDbName}].[dbo].AppTransactionUnitSearchViewFieldMapping where Org_TransactionUnitSearchViewFieldMappingId is not null
                delete from [{targetDbName}].[dbo].AppTransactionUnitLinkedSearch where Org_TransactionUnitLinkedSearchId is not null

                delete from [{targetDbName}].[dbo].AppFormLinkTarget where Org_LinkTargetID is not null
                delete from [{targetDbName}].[dbo].AppViewLinkedSeaechOrUrl where Org_SearchViewLinkSearchID is not null


                delete from [{targetDbName}].[dbo].AppTransactionNavigation where Org_TransNavigationID is not null
                delete from [{targetDbName}].[dbo].AppTranscationReport where Org_TransctionReportID is not null
                delete from [{targetDbName}].[dbo].[AppReport] where Org_ReportID is not null

                delete from [{targetDbName}].[dbo].AppProjectWorkFlowAction where Org_WorkFlowActionID is not null




                delete from [{targetDbName}].[dbo].[AppSearchField] where Org_SearchFieldID is not null

                delete from [{targetDbName}].[dbo].[AppSearch] where Org_SearchID is not null

                delete from [{targetDbName}].[dbo].[AppSearchViewField] where Org_SearchViewFieldID is not null
                delete from [{targetDbName}].[dbo].[AppSearchView] where Org_SearchViewID is not null

                delete from [{targetDbName}].[dbo].AppTransactionSaveAsMapping where Org_MappingID is not null
                delete from [{targetDbName}].[dbo].AppTransactionDataTransferSetting where Org_DataTransferSettingID is not null


                delete from [{targetDbName}].[dbo].AppMessage where Org_MessageID is not null

                delete from [{targetDbName}].[dbo].AppTranscationDataLoadFieldMapping where Org_FieldMappingID is not null
                delete from [{targetDbName}].[dbo].AppTransactionDataLoad where Org_DataLoadID is not null

                delete from [{targetDbName}].[dbo].[AppDataSetParameter] where Org_ParameterID is not null
                delete from [{targetDbName}].[dbo].[AppDataSet] where Org_DataSetID is not null

                delete from [{targetDbName}].[dbo].[AppTransactionUnitFormula] where Org_TransactionUnitFormulaID is not null
                delete from [{targetDbName}].[dbo].[AppTransactionFieldAggFunction] where Org_FieldAggFunctionID is not null
                delete from [{targetDbName}].[dbo].[AppConditionalAction] where Org_ActionID is not null

                update [{targetDbName}].[dbo].[AppTransaction] set FormID = null, PrintFormID = null where  Org_TransactionID is not null

                delete from [{targetDbName}].[dbo].AppFormLayoutItem where Org_FormLayoutItemID is not null
                delete from [{targetDbName}].[dbo].AppForm where Org_FormID is not null

                delete from [{targetDbName}].[dbo].[AppTransactionField] where Org_TransactionFieldID is not null
                delete from [{targetDbName}].[dbo].[AppTransactionUnit] where Org_TransactionUnitID is not null
                delete from [{targetDbName}].[dbo].[AppTransaction] where Org_TransactionID is not null



                delete from [{targetDbName}].[dbo].[AppEntitySimpleListValue] where EntityInfoID in (select EntityInfoID from [{targetDbName}].[dbo].[AppEntityInfo] where Org_EntityInfoID is not null)
                delete from [{targetDbName}].[dbo].[AppEntityInfo] where Org_EntityInfoID is not null

                delete from [{targetDbName}].[dbo].[AppDesktop] where Org_DesktopID is not null

                delete from [{targetDbName}].[dbo].[AppSecurityUserListMenu] where MenuID in (
	                select [MenuID] from [{targetDbName}].[dbo].[AppListMenu] where org_menuid is not null
                )

                delete from [{targetDbName}].[dbo].[AppListMenu] where org_menuid is not null

            ".Replace("{targetDbName}", targetDbName);





            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {                   

                    adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());
                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Clean failed config data Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                }

            }


        }

        private static ValidationResult PostImported_UpdateTransactionCommand(string srcDbName, string targetDbName, AppApplicationImportSettingDto importSettingDto)
        {
            ValidationResult validationResult = new ValidationResult();

            List<AppProjectWorkFlowActionExDto> commandDtoList = PostImported_UpdateTransactionCommand_RetrieveImportedCommand(srcDbName, targetDbName, validationResult);

            if (validationResult.HasErrors)
            {
                return validationResult;
            }

            if (importSettingDto != null)
            {
                string query = "";

                Dictionary<int, int> dictOrgIdAndNewId_Command = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppProjectWorkFlowAction.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_Trans = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppTransaction.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_TransUnit = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppTransactionUnit.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_Transfield = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppTransactionField.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_LinkedSearch = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppTransactionUnitLinkedSearch.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_LinkTarget = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppFormLinkTarget.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_Search = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppSearch.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_DataSet = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppDataSet.ToString()];

                if (commandDtoList != null && commandDtoList.Count > 0)
                {
                    foreach (var commandDto in commandDtoList)
                    {
                        if (commandDto.Id != null)
                        {
                            if (commandDto.ActionAttribute != null)
                            {
                                query += PrepareOneCommandExpressoinUpdateQuery(dictOrgIdAndNewId_Command, dictOrgIdAndNewId_Trans, dictOrgIdAndNewId_TransUnit, dictOrgIdAndNewId_Transfield, dictOrgIdAndNewId_LinkedSearch, dictOrgIdAndNewId_LinkTarget, dictOrgIdAndNewId_Search, dictOrgIdAndNewId_DataSet, commandDto);
                            }

                            if (!string.IsNullOrWhiteSpace(commandDto.NotificationMessage))
                            {
                                query += PrepareOneCommandNotificationMessageUpdateQuery(dictOrgIdAndNewId_Transfield, commandDto);
                            }
                        }

                    }
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    query = query.Replace("{srcDbName}", srcDbName)
                        .Replace("{targetDbName}", targetDbName);

                    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                        }
                        catch (Exception ex)
                        {

                            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Transaction Command Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                        }
                    }
                }

            }



            return validationResult;
        }

        private static string PrepareOneCommandNotificationMessageUpdateQuery(Dictionary<int, int> dictOrgIdAndNewId_Transfield, AppProjectWorkFlowActionExDto commandDto)
        {
            //[TF_25461_PoItemId]
            string query = "";

            int index = commandDto.NotificationMessage.IndexOf("[TF_");

            Dictionary<string, string> dictOrgTokenAndNewToken = new Dictionary<string, string>();
            int loopCount = 0;

            while (index >= 0)
            {
                int token_EndIndex = commandDto.NotificationMessage.IndexOf("]", index);
                string orgToken = commandDto.NotificationMessage.Substring(index, token_EndIndex - index + 1);

                int transFieldId_StartIndex = index + "[TF_".Length;
                int transFieldId_EndIndex = commandDto.NotificationMessage.IndexOf("_", transFieldId_StartIndex);

                if (transFieldId_EndIndex < 0)
                {
                    transFieldId_EndIndex = commandDto.NotificationMessage.IndexOf("]", transFieldId_StartIndex);
                }

                if (transFieldId_EndIndex > transFieldId_StartIndex)
                {
                    if (!dictOrgTokenAndNewToken.ContainsKey(orgToken))
                    {
                        string transFieldIdString = commandDto.NotificationMessage.Substring(transFieldId_StartIndex, transFieldId_EndIndex - transFieldId_StartIndex);

                        int? transFieldId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdString);

                        if (transFieldId.HasValue && dictOrgIdAndNewId_Transfield.ContainsKey(transFieldId.Value))
                        {
                            int newTrasnfieldId = dictOrgIdAndNewId_Transfield[transFieldId.Value];

                            string newToken = orgToken.Replace(transFieldId.Value.ToString(), newTrasnfieldId.ToString());
                            dictOrgTokenAndNewToken.Add(orgToken, newToken);
                        }
                    }
                }

                index = commandDto.NotificationMessage.IndexOf("[TF_", token_EndIndex);

                loopCount++;

                if (loopCount >= MaxWhileLoopCount)
                {
                    throw new Exception("Infinit loop while processing Command Notification Message Transaction Field Token Update.\n" 
                        + "Command: " + commandDto.Id + "_" + commandDto.Name 
                        + "\nMessageText: " + commandDto.NotificationMessage);
                }
            }


            int indexGlobalTF = commandDto.NotificationMessage.IndexOf(AppTransactionCommandBL.GlobalTFPrefix);
            loopCount = 0;
            while (indexGlobalTF >= 0)
            {
                int token_EndIndex = commandDto.NotificationMessage.IndexOf("]", indexGlobalTF);
                string orgToken = commandDto.NotificationMessage.Substring(indexGlobalTF, token_EndIndex - indexGlobalTF + 1);

                int transFieldId_StartIndex = indexGlobalTF + AppTransactionCommandBL.GlobalTFPrefix.Length;
                int transFieldId_EndIndex = commandDto.NotificationMessage.IndexOf("_", transFieldId_StartIndex);

                if (transFieldId_EndIndex < 0)
                {
                    transFieldId_EndIndex = commandDto.NotificationMessage.IndexOf("]", transFieldId_StartIndex);
                }

                if (transFieldId_EndIndex > transFieldId_StartIndex)
                {
                    if (!dictOrgTokenAndNewToken.ContainsKey(orgToken))
                    {
                        string transFieldIdString = commandDto.NotificationMessage.Substring(transFieldId_StartIndex, transFieldId_EndIndex - transFieldId_StartIndex);

                        int? transFieldId = ControlTypeValueConverter.ConvertValueToInt(transFieldIdString);

                        if (transFieldId.HasValue && dictOrgIdAndNewId_Transfield.ContainsKey(transFieldId.Value))
                        {
                            int newTrasnfieldId = dictOrgIdAndNewId_Transfield[transFieldId.Value];

                            string newToken = orgToken.Replace(transFieldId.Value.ToString(), newTrasnfieldId.ToString());
                            dictOrgTokenAndNewToken.Add(orgToken, newToken);
                        }
                    }
                }

                indexGlobalTF = commandDto.NotificationMessage.IndexOf(AppTransactionCommandBL.GlobalTFPrefix, token_EndIndex);
                
                loopCount++;

                if (loopCount >= MaxWhileLoopCount)
                {
                    throw new Exception("Infinit loop while processing Command Notification Message Global Transaction Field  Token Update.\n"
                        + "Command: " + commandDto.Id + "_" + commandDto.Name
                        + "\nMessageText: " + commandDto.NotificationMessage);
                }
            }

            if (dictOrgTokenAndNewToken.Keys.Count > 0)
            {
                foreach (string orgToken in dictOrgTokenAndNewToken.Keys)
                {
                    string newToken = dictOrgTokenAndNewToken[orgToken];

                    commandDto.NotificationMessage = commandDto.NotificationMessage.Replace(orgToken, newToken);
                }

                commandDto.NotificationMessage = commandDto.NotificationMessage.Replace(@"'", @"''");

                query += @"
                                            UPDATE [{targetDbName}].[dbo].[AppProjectWorkFlowAction] SET [NotificationMessage] = '" + commandDto.NotificationMessage
                            + @"' WHERE WorkFlowActionID = " + commandDto.Id.ToString() + @";
                                             ";
            }

            return query;
        }

        private static string PrepareOneCommandExpressoinUpdateQuery(Dictionary<int, int> dictOrgIdAndNewId_Command, Dictionary<int, int> dictOrgIdAndNewId_Trans, Dictionary<int, int> dictOrgIdAndNewId_TransUnit, Dictionary<int, int> dictOrgIdAndNewId_Transfield, Dictionary<int, int> dictOrgIdAndNewId_LinkedSearch
            , Dictionary<int, int> dictOrgIdAndNewId_LinkTarget, Dictionary<int, int> dictOrgIdAndNewId_Search, Dictionary<int, int> dictOrgIdAndNewId_DataSet, AppProjectWorkFlowActionExDto commandDto)
        {
            string query = "";
            if (commandDto.ActionAttribute.ChildActionList != null)
            {
                foreach (ChildTransactionCommandDto childActionDto in commandDto.ActionAttribute.ChildActionList)
                {
                    if (childActionDto.CommandId.HasValue
                        && dictOrgIdAndNewId_Command.ContainsKey(childActionDto.CommandId.Value))
                    {
                        childActionDto.CommandId = dictOrgIdAndNewId_Command[childActionDto.CommandId.Value];
                    }

                    if (childActionDto.ChangeTriggerRootLevelFieldId.HasValue
                        && dictOrgIdAndNewId_Transfield.ContainsKey(childActionDto.ChangeTriggerRootLevelFieldId.Value))
                    {
                        childActionDto.ChangeTriggerRootLevelFieldId = dictOrgIdAndNewId_Transfield[childActionDto.ChangeTriggerRootLevelFieldId.Value];
                    }

                    if (childActionDto.ChangeTriggerChildGridUnitId.HasValue
                        && dictOrgIdAndNewId_TransUnit.ContainsKey(childActionDto.ChangeTriggerChildGridUnitId.Value))
                    {
                        childActionDto.ChangeTriggerChildGridUnitId = dictOrgIdAndNewId_TransUnit[childActionDto.ChangeTriggerChildGridUnitId.Value];
                    }

                    if (childActionDto.ExternalTransactionId.HasValue
                         && dictOrgIdAndNewId_Trans.ContainsKey(childActionDto.ExternalTransactionId.Value))
                    {
                        childActionDto.ExternalTransactionId = dictOrgIdAndNewId_Trans[childActionDto.ExternalTransactionId.Value];
                    }
                }

            }
            if (commandDto.ActionAttribute.CallBackCommandID.HasValue
                && dictOrgIdAndNewId_Command.ContainsKey(commandDto.ActionAttribute.CallBackCommandID.Value))
            {
                commandDto.ActionAttribute.CallBackCommandID = dictOrgIdAndNewId_Command[commandDto.ActionAttribute.CallBackCommandID.Value];
            }

            if (commandDto.ActionAttribute.TargetTransactionCommandId.HasValue
               && dictOrgIdAndNewId_Command.ContainsKey(commandDto.ActionAttribute.TargetTransactionCommandId.Value))
            {
                commandDto.ActionAttribute.TargetTransactionCommandId = dictOrgIdAndNewId_Command[commandDto.ActionAttribute.TargetTransactionCommandId.Value];
            }

            if (commandDto.ActionAttribute.LinkedSearchId.HasValue
               && dictOrgIdAndNewId_LinkedSearch.ContainsKey(commandDto.ActionAttribute.LinkedSearchId.Value))
            {
                commandDto.ActionAttribute.LinkedSearchId = dictOrgIdAndNewId_LinkedSearch[commandDto.ActionAttribute.LinkedSearchId.Value];
            }

            if (commandDto.ActionAttribute.NotificationDestinationEmailAddressTransactionFiledId.HasValue
               && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.NotificationDestinationEmailAddressTransactionFiledId.Value))
            {
                commandDto.ActionAttribute.NotificationDestinationEmailAddressTransactionFiledId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.NotificationDestinationEmailAddressTransactionFiledId.Value];
            }

            if (commandDto.ActionAttribute.SmsMessageToPhoneNumberFiledId.HasValue
               && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.SmsMessageToPhoneNumberFiledId.Value))
            {
                commandDto.ActionAttribute.SmsMessageToPhoneNumberFiledId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.SmsMessageToPhoneNumberFiledId.Value];
            }

            if (commandDto.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.HasValue
               && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.Value))
            {
                commandDto.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.NotificationDestinationPartnerIdTransactionFiledId.Value];
            }

            if (commandDto.ActionAttribute.AssignSqlResultToFiledId.HasValue
               && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.AssignSqlResultToFiledId.Value))
            {
                commandDto.ActionAttribute.AssignSqlResultToFiledId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.AssignSqlResultToFiledId.Value];
            }

            if (commandDto.ActionAttribute.LinkTargetId.HasValue
               && dictOrgIdAndNewId_LinkTarget.ContainsKey(commandDto.ActionAttribute.LinkTargetId.Value))
            {
                commandDto.ActionAttribute.LinkTargetId = dictOrgIdAndNewId_LinkTarget[commandDto.ActionAttribute.LinkTargetId.Value];
            }

            if (commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId.HasValue
                && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId.Value))
            {
                commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.ChildCommandsSwitchConditionFieldId.Value];
            }

            if (commandDto.ActionAttribute.CommandsChangedTrigerFieldId.HasValue
                && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.CommandsChangedTrigerFieldId.Value))
            {
                commandDto.ActionAttribute.CommandsChangedTrigerFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.CommandsChangedTrigerFieldId.Value];
            }

            if (commandDto.ActionAttribute.ForeachLoopSourceUnitId.HasValue
                && dictOrgIdAndNewId_TransUnit.ContainsKey(commandDto.ActionAttribute.ForeachLoopSourceUnitId.Value))
            {
                commandDto.ActionAttribute.ForeachLoopSourceUnitId = dictOrgIdAndNewId_TransUnit[commandDto.ActionAttribute.ForeachLoopSourceUnitId.Value];
            }

            if (!string.IsNullOrWhiteSpace(commandDto.ActionAttribute.ExecutionFormula))
            {
                string expression = commandDto.ActionAttribute.ExecutionFormula;
                expression = UpdateImportedFurmulaExpressionTransFieldIds(dictOrgIdAndNewId_Transfield, expression);

                commandDto.ActionAttribute.ExecutionFormula = expression;
            }

            if (commandDto.ActionAttribute.UserTypeTransFieldId.HasValue
                && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.UserTypeTransFieldId.Value))
            {
                commandDto.ActionAttribute.UserTypeTransFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.UserTypeTransFieldId.Value];
            }

            if (commandDto.ActionAttribute.UserNameTransFieldId.HasValue
                && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.UserNameTransFieldId.Value))
            {
                commandDto.ActionAttribute.UserNameTransFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.UserNameTransFieldId.Value];
            }

            if (commandDto.ActionAttribute.UserPasswordTransFieldId.HasValue
                && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.UserPasswordTransFieldId.Value))
            {
                commandDto.ActionAttribute.UserPasswordTransFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.UserPasswordTransFieldId.Value];
            }

            if (commandDto.ActionAttribute.UserEmailTransFieldId.HasValue
                && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.UserEmailTransFieldId.Value))
            {
                commandDto.ActionAttribute.UserEmailTransFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.UserEmailTransFieldId.Value];
            }

            if (commandDto.ActionAttribute.UserPartnerIdTransFieldId.HasValue
               && dictOrgIdAndNewId_Transfield.ContainsKey(commandDto.ActionAttribute.UserPartnerIdTransFieldId.Value))
            {
                commandDto.ActionAttribute.UserPartnerIdTransFieldId = dictOrgIdAndNewId_Transfield[commandDto.ActionAttribute.UserPartnerIdTransFieldId.Value];
            }

            if (commandDto.ActionAttribute.BatchCommandDataSetId.HasValue
                && dictOrgIdAndNewId_DataSet.ContainsKey(commandDto.ActionAttribute.BatchCommandDataSetId.Value))
            {
                commandDto.ActionAttribute.BatchCommandDataSetId = dictOrgIdAndNewId_DataSet[commandDto.ActionAttribute.BatchCommandDataSetId.Value];
            }

            if (commandDto.ActionAttribute.BatchCommandSearchId.HasValue
                && dictOrgIdAndNewId_Search.ContainsKey(commandDto.ActionAttribute.BatchCommandSearchId.Value))
            {
                commandDto.ActionAttribute.BatchCommandSearchId = dictOrgIdAndNewId_Search[commandDto.ActionAttribute.BatchCommandSearchId.Value];
            }

            string updatedFormulaExpression = JsonConvert.SerializeObject(commandDto.ActionAttribute).Replace("'", "''"); 

            query += @"
                                UPDATE [{targetDbName}].[dbo].[AppProjectWorkFlowAction] SET [FormulaExpression] = N'" + updatedFormulaExpression
                + @"' WHERE WorkFlowActionID = " + commandDto.Id.ToString() + @";
                                 ";



            return query;
        }


        private static ValidationResult PostImported_UpdateTransaction(string srcDbName, string targetDbName, AppApplicationImportSettingDto importSettingDto)
        {
            ValidationResult validationResult = new ValidationResult();

            List<AppTransactionExDto> transactionDtoList = PostImported_UpdateTransaction_RetrieveImportedTransaction(srcDbName, targetDbName, validationResult);

            if (validationResult.HasErrors)
            {
                return validationResult;
            }

            if (importSettingDto != null)
            {
                string query = "";

                
                Dictionary<int, int> dictOrgIdAndNewId_DataSet = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppDataSet.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_Transfield = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppTransactionField.ToString()];
               

                if (transactionDtoList != null && transactionDtoList.Count > 0)
                {
                    foreach (var transactionDto in transactionDtoList)
                    {
                        if (transactionDto.Id != null)
                        {
                            if (transactionDto.OtherOptions != null)
                            {
                                query += PrepareOneTransactionOtherOptionsUpdateQuery(transactionDto, dictOrgIdAndNewId_DataSet, dictOrgIdAndNewId_Transfield);
                            }                          
                        }

                    }
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    query = query.Replace("{srcDbName}", srcDbName)
                        .Replace("{targetDbName}", targetDbName);

                    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                        }
                        catch (Exception ex)
                        {

                            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Transaction Command Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                        }
                    }
                }

            }



            return validationResult;
        }

        private static List<AppTransactionExDto> PostImported_UpdateTransaction_RetrieveImportedTransaction(string srcDbName, string targetDbName, ValidationResult validationResult)
        {

            List<AppTransactionExDto> toReturn = new List<AppTransactionExDto>();

            string query = @"                
                select * from [{targetDbName}].[dbo].[AppTransaction] 
                where Org_TransactionID is not null;
            ";


            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);


            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtTransaction = adpater.ExecuteDataTableRetrievalQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                    foreach (DataRow dataRow in dtTransaction.Rows)
                    {
                        int? transactionId = ControlTypeValueConverter.ConvertValueToInt(dataRow[AppTransaction_TransactionId]);
                        string expression = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[AppTransaction_PostProcessStoreProcedure]);

                        if (transactionId.HasValue && !string.IsNullOrWhiteSpace(expression))
                        {
                            AppTransactionExDto transactionDto = new AppTransactionExDto();
                            transactionDto.Id = transactionId;
                            transactionDto.PostProcessStoreProcedure = expression;

                            if (!string.IsNullOrWhiteSpace(transactionDto.PostProcessStoreProcedure))
                            {
                                try
                                {
                                    transactionDto.OtherOptions = JsonConvert.DeserializeObject<TransactionOptionDto>(transactionDto.PostProcessStoreProcedure);
                                }
                                catch
                                {
                                    transactionDto.OtherOptions = null;
                                }

                            }
                            else
                            {
                                transactionDto.OtherOptions = null;
                            }

                            toReturn.Add(transactionDto);
                        }
                    }

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Transaction Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                }
            }

            return toReturn;
        }

        private static string PrepareOneTransactionOtherOptionsUpdateQuery(AppTransactionExDto transactionDto, Dictionary<int, int> dictOrgIdAndNewId_DataSet, Dictionary<int, int> dictOrgIdAndNewId_Transfield)
        {
            string query = "";
            if (transactionDto.OtherOptions != null)
            {
                if (transactionDto.OtherOptions.ErDiagramId.HasValue
                    && dictOrgIdAndNewId_DataSet.ContainsKey(transactionDto.OtherOptions.ErDiagramId.Value))
                {
                    transactionDto.OtherOptions.ErDiagramId = dictOrgIdAndNewId_DataSet[transactionDto.OtherOptions.ErDiagramId.Value];
                }

                if (transactionDto.OtherOptions.ImportSettingId.HasValue)
                {
                    transactionDto.OtherOptions.ImportSettingId = null;
                }

                if (transactionDto.OtherOptions.TransactionDataUpdateImportSettingId.HasValue)
                {
                    transactionDto.OtherOptions.TransactionDataUpdateImportSettingId = null;
                }

                if (transactionDto.OtherOptions.CommunicationToUserIdTransField.HasValue
                    && dictOrgIdAndNewId_Transfield.ContainsKey(transactionDto.OtherOptions.CommunicationToUserIdTransField.Value))
                {
                    transactionDto.OtherOptions.CommunicationToUserIdTransField = dictOrgIdAndNewId_Transfield[transactionDto.OtherOptions.CommunicationToUserIdTransField.Value];
                }
            }           

            string updatedFormulaExpression = JsonConvert.SerializeObject(transactionDto.OtherOptions).Replace("'", "''");

            query += @"
                                UPDATE [{targetDbName}].[dbo].[AppTransaction] SET [PostProcessStoreProcedure] = N'" + updatedFormulaExpression
                + @"' WHERE TransactionID = " + transactionDto.Id.ToString() + @";
                                 ";



            return query;
        }



        private static ValidationResult PostImported_UpdateFormLayoutItem(string srcDbName, string targetDbName, AppApplicationImportSettingDto importSettingDto)
        {
            ValidationResult validationResult = new ValidationResult();

            List<AppFormLayoutItemExDto> layoutItemDtoList = PostImported_UpdateFormLayoutItem_RetrieveImportedFormLayoutItem(srcDbName, targetDbName, validationResult);

            if (validationResult.HasErrors)
            {
                return validationResult;
            }

            if (importSettingDto != null)
            {
                string query = "";


                Dictionary<int, int> dictOrgIdAndNewId_UnitLinkedSearch = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppTransactionUnitLinkedSearch.ToString()];
                Dictionary<int, int> dictOrgIdAndNewId_Command = importSettingDto.DictSystemTableOrgIdAndNewId[EmAppModuleConfigTable.AppProjectWorkFlowAction.ToString()];


                if (layoutItemDtoList != null && layoutItemDtoList.Count > 0)
                {
                    foreach (var layoutItemDto in layoutItemDtoList)
                    {
                        if (layoutItemDto.Id != null)
                        {
                            if (layoutItemDto.DomAttribute != null)
                            {
                                query += PrepareOneFormLayoutItemUpdateQuery(layoutItemDto, dictOrgIdAndNewId_UnitLinkedSearch, dictOrgIdAndNewId_Command);
                            }
                        }

                    }
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    query = query.Replace("{srcDbName}", srcDbName)
                        .Replace("{targetDbName}", targetDbName);

                    using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());

                        }
                        catch (Exception ex)
                        {

                            validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Form Layout Item Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                        }
                    }
                }

            }



            return validationResult;
        }



        private static List<AppFormLayoutItemExDto> PostImported_UpdateFormLayoutItem_RetrieveImportedFormLayoutItem(string srcDbName, string targetDbName, ValidationResult validationResult)
        {

            List<AppFormLayoutItemExDto> toReturn = new List<AppFormLayoutItemExDto>();

            string query = @"                
                select * from [{targetDbName}].[dbo].[AppFormLayoutItem] 
                where Org_FormLayoutItemID is not null;
            ";


            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);


            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtTransaction = adpater.ExecuteDataTableRetrievalQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                    foreach (DataRow dataRow in dtTransaction.Rows)
                    {
                        int? layoutItemId = ControlTypeValueConverter.ConvertValueToInt(dataRow[AppFormLayoutItem_FormLayoutItemId]);
                        string expression = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[AppFormLayoutItem_ParameterKeyValue]);

                        if (layoutItemId.HasValue && !string.IsNullOrWhiteSpace(expression))
                        {
                            AppFormLayoutItemExDto layoutItemDto = new AppFormLayoutItemExDto();
                            layoutItemDto.Id = layoutItemId;
                            layoutItemDto.ParameterKeyValue = expression;

                            if (!string.IsNullOrWhiteSpace(layoutItemDto.ParameterKeyValue))
                            {
                                try
                                {
                                    layoutItemDto.DomAttribute = JsonConvert.DeserializeObject<AppFormDomAttributeDto>(layoutItemDto.ParameterKeyValue);
                                }
                                catch
                                {
                                    layoutItemDto.DomAttribute = null;
                                }

                            }
                            else
                            {
                                layoutItemDto.DomAttribute = null;
                            }

                            toReturn.Add(layoutItemDto);
                        }
                    }

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Form Layout Item Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                }
            }

            return toReturn;
        }

        private static string PrepareOneFormLayoutItemUpdateQuery(AppFormLayoutItemExDto layoutItemDto, Dictionary<int, int> dictOrgIdAndNewId_UnitLinkedSearch, Dictionary<int, int> dictOrgIdAndNewId_Command)
        {
            string query = "";
            if (layoutItemDto.DomAttribute != null)
            {
                if (layoutItemDto.DomAttribute.LinkedSearchId.HasValue
                    && dictOrgIdAndNewId_UnitLinkedSearch.ContainsKey(layoutItemDto.DomAttribute.LinkedSearchId.Value))
                {
                    layoutItemDto.DomAttribute.LinkedSearchId = dictOrgIdAndNewId_UnitLinkedSearch[layoutItemDto.DomAttribute.LinkedSearchId.Value];
                }

                if (layoutItemDto.DomAttribute.CommandActionId.HasValue
                    && dictOrgIdAndNewId_Command.ContainsKey(layoutItemDto.DomAttribute.CommandActionId.Value))
                {
                    layoutItemDto.DomAttribute.CommandActionId = dictOrgIdAndNewId_Command[layoutItemDto.DomAttribute.CommandActionId.Value];
                }                
            }

            string updatedFormulaExpression = JsonConvert.SerializeObject(layoutItemDto.DomAttribute).Replace("'", "''");

            query += @"
                                UPDATE [{targetDbName}].[dbo].[AppFormLayoutItem] SET [ParameterKeyValue] = N'" + updatedFormulaExpression
                + @"' WHERE FormLayoutItemID = " + layoutItemDto.Id.ToString() + @";
                                 ";



            return query;
        }





      

        private static List<AppProjectWorkFlowActionExDto> PostImported_UpdateTransactionCommand_RetrieveImportedCommand(string srcDbName, string targetDbName, ValidationResult validationResult)
        {

            List<AppProjectWorkFlowActionExDto> toReturn = new List<AppProjectWorkFlowActionExDto>();

            string query = @"                
                select * from [{targetDbName}].[dbo].[AppProjectWorkFlowAction] 
                where Org_WorkFlowActionID is not null;
            ";


            query = query.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);


            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtCommand = adpater.ExecuteDataTableRetrievalQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                    foreach (DataRow dataRow in dtCommand.Rows)
                    {
                        int? commandId = ControlTypeValueConverter.ConvertValueToInt(dataRow[AppProjectWorkFlowAction_WorkFlowActionID]);
                        string expression = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[AppProjectWorkFlowAction_FormulaExpression]);
                        string notificationMessage = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow[AppProjectWorkFlowAction_NotificationMessage]);

                        if (commandId.HasValue && (!string.IsNullOrWhiteSpace(expression) || !string.IsNullOrWhiteSpace(notificationMessage)))
                        {
                            AppProjectWorkFlowActionExDto commandDto = new AppProjectWorkFlowActionExDto();
                            commandDto.Id = commandId;
                            commandDto.FormulaExpression = expression;
                            commandDto.NotificationMessage = notificationMessage;

                            if (!string.IsNullOrWhiteSpace(commandDto.FormulaExpression))
                            {
                                try
                                {
                                    commandDto.ActionAttribute = JsonConvert.DeserializeObject<AppActionAttributeDto>(commandDto.FormulaExpression);
                                }
                                catch
                                {
                                    commandDto.ActionAttribute = null;
                                }

                            }
                            else
                            {
                                commandDto.ActionAttribute = null;
                            }

                            toReturn.Add(commandDto);
                        }
                    }

                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Update Transaction Command Failed. \n" + ex.ToString() + "\nQuery:\n" + query + "\n"));
                }
            }

            return toReturn;
        }


        private static string UpdateImportedFurmulaExpressionTransFieldIds(Dictionary<int, int> dictOrgIdAndNewId_Transfield, string expression)
        {
            var members = expression.Split(AppTransactionFormulaSetupBL.FormulaConstString, StringSplitOptions.RemoveEmptyEntries);
            foreach (string info in members)
            {
                if (info.Trim().StartsWith(AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix))
                {
                    int? orgId = ControlTypeValueConverter.ConvertValueToInt(info.Replace(AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix, "").Trim());

                    if (orgId.HasValue && dictOrgIdAndNewId_Transfield.ContainsKey(orgId.Value))
                    {
                        int newId = dictOrgIdAndNewId_Transfield[orgId.Value];
                        expression = expression.Replace(info, AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix + newId.ToString());
                    }
                }
            }

            return expression;
        }

        private static ValidationResult ImportUserTables(string srcDbName, string targetDbName, AppApplicationImportSettingDto importSettingDto)
        {
            ValidationResult validationResult = new ValidationResult();


            string queryGetTableNames = @"                
                select distinct TableName 
                FROM
                (
                select DataBaseTableName as TableName from [{targetDbName}].[dbo].[AppTransactionUnit] where Org_TransactionUnitID is not null and DataBaseTableName is not null and DataBaseTableName <> ''
                UNION
                select TableName from [{targetDbName}].[dbo].[AppEntityInfo] where Org_EntityInfoID is not null and EntityType = 1 and TableName is not null and TableName <> ''
                ) as TableNames

            ";

            queryGetTableNames = queryGetTableNames.Replace("{srcDbName}", srcDbName)
                .Replace("{targetDbName}", targetDbName);


            string query = "";

            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    DataTable dtUserTableName = adpater.ExecuteDataTableRetrievalQuery(queryGetTableNames, new List<System.Data.SqlClient.SqlParameter>());

                    List<string> userTableNameList = new List<string>();

                    foreach (DataRow dataRow in dtUserTableName.Rows)
                    {
                        string tableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["TableName"]).Trim().ToLower();

                        if (!string.IsNullOrWhiteSpace(tableName) && !userTableNameList.Contains(tableName))
                        {
                            if (!tableName.StartsWith("app"))
                            {
                                userTableNameList.Add(tableName);
                            }
                        }
                    }

                    List<string> existTableNameList = new List<string>();

                    foreach (string tableName in userTableNameList)
                    {
                        importSettingDto.DictUserTableNameAndIsNeedToImport.Add(tableName, false);

                        bool isTableExist = CheckIfTableNameExist(targetDbName, tableName, validationResult);

                        if (validationResult.HasErrors)
                        {
                            break;
                        }
                        else
                        {
                            if (isTableExist)
                            {
                                existTableNameList.Add(tableName);
                                //validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Warning,
                                //    "Table " + tableName + " already exists. This table will not be imported. "));
                            }
                            else
                            {
                                string queryCreateOneTable = GetOneTableCreateScript(srcDbName, targetDbName, tableName, validationResult);

                                if (validationResult.HasErrors)
                                {
                                    break;
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(queryCreateOneTable))
                                    {
                                        importSettingDto.DictUserTableNameAndIsNeedToImport[tableName] = true;

                                        queryCreateOneTable = queryCreateOneTable.Replace(" [dbo]", " [" + targetDbName + "].[dbo]");
                                        queryCreateOneTable = queryCreateOneTable.Replace("[" + srcDbName + "].[dbo]", "[" + targetDbName + "].[dbo]");
                                        query += queryCreateOneTable + @";

                                    ";

                                        string queryImportOneTableData = GetOneTableDataImportScript(srcDbName, targetDbName, tableName, validationResult);

                                        if (validationResult.HasErrors)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            query += queryImportOneTableData + @";

                    


                                        ";
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (existTableNameList.Count > 0)
                    {
                        string existTableNamesString = string.Join(",\n", existTableNameList);

                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Message,
                            "Warning: The following tables alraedy exist and will not be imported: \n\n" + existTableNamesString));
                    }

                    if (!string.IsNullOrWhiteSpace(query))
                    {


                        adpater.ExecuteExecuteNonQuery(query, new List<SqlParameter>());
                        validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_query_success", ValidationItemType.Message, "\n\nImport User Table Query: \n\n" + query + "\n"));
                    }
                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Import User Table Failed. \n" + ex.ToString() + "\n\nImport User Table Query:\n\n" + query + "\n"));
                }
            }

            return validationResult;
        }

        private static string GetOneTableDataImportScript(string srcDbName, string targetDbName, string tableName, ValidationResult validationResult)
        {
            string script = "";

            try
            {
                string srcDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, srcDbName);
                var databaseFixture = new DatabaseFixture(srcDbConnectionString, EmSqlType.SqlServer);
                DatabaseTable srcTableDto = databaseFixture.Table(tableName);


                var identyPkColumn = srcTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey && o.IsAutoNumber);
                if (identyPkColumn != null)
                {
                    string columnNames = string.Join(", ", srcTableDto.Columns.Where(o=> o.IsPrimaryKey || (o.DbDataType != "timestamp")).Select(o => o.Name).Select(o => "[" + o + "]").ToList());

                    

                    script += @"
                        SET IDENTITY_INSERT [{targetDbName}].[dbo].[{tableName}] ON;
                    ";

                    script += @"INSERT [{targetDbName}].[dbo].[{tableName}] (";
                    script += columnNames + ")";

                    script += @"
                        SELECT 
                    ";

                    script += columnNames;

                    script += @"
                        FROM [{srcDbName}].[dbo].[{tableName}];
                    ";

                    script += @"
                        SET IDENTITY_INSERT [{targetDbName}].[dbo].[{tableName}] OFF;
                    ";
                }
                else
                {
                    string columnNames = string.Join(", ", srcTableDto.Columns.Where(o => !(o.IsPrimaryKey && o.IsAutoNumber) && (o.DbDataType != "timestamp")).Select(o => o.Name).Select(o => "[" + o + "]").ToList());
                    
                    script += @"INSERT [{targetDbName}].[dbo].[{tableName}] (";
                    script += columnNames + ")";

                    script += @"
                        SELECT 
                    ";

                    script += columnNames;

                    script += @"
                        FROM [{srcDbName}].[dbo].[{tableName}];
                    "
                    ;
                }
                script = script.Replace("{srcDbName}", srcDbName)
                              .Replace("{targetDbName}", targetDbName)
                               .Replace("{tableName}", tableName);

            }
            catch (Exception ex)
            {
                validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_GenerateQuery_Error", ValidationItemType.Error
                    , "Generate Import Table Data Script Failed For Table " + srcDbName + ": \n" + ex.ToString()));
            }


            return script;
        }


        private static void SynchronizeDataBaseViews(string srcDbName, string targetDbName, ValidationResult validationResult)
        {
            try
            {
                string srcDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, srcDbName);
                DatabaseFixture srcDatabaseFixture = new DatabaseFixture(srcDbConnectionString, EmSqlType.SqlServer);

                string targetDbConnectionString = AppMasterDBConnectionString.Replace(HostCompanyDbName, targetDbName);
                DatabaseFixture targetDatabaseFixture = new DatabaseFixture(targetDbConnectionString, EmSqlType.SqlServer);

                List<string> srcViews = srcDatabaseFixture.AllViews().Select(o => o.Name.ToLower()).ToList();
                List<string> targetViews = targetDatabaseFixture.AllViews().Select(o => o.Name.ToLower()).ToList();

                List<string> needToImportViews = new List<string>();

                foreach (string viewName in srcViews)
                {
                    if (!targetViews.Contains(viewName))
                    {
                        needToImportViews.Add(viewName);
                    }
                }

                if (needToImportViews.Count > 0)
                {
                    //List<string> viewCreationQueryList = new List<string>();

                    foreach (string viewName in needToImportViews)
                    {
                        try
                        {
                            // 
                            string getViewCreationTextQuery = @"
                                sp_helptext '[" + viewName + @"]';
                            ";

                            DataTable dtViewCreationText = srcDatabaseFixture.RetriveDataTable(getViewCreationTextQuery, new List<DbParameter>());

                            if (dtViewCreationText.Rows.Count > 0)
                            {
                                string query = "";

                                for (int i = 0; i < dtViewCreationText.Rows.Count; i++)
                                {
                                    query += ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dtViewCreationText.Rows[i][0]);
                                }

                                targetDatabaseFixture.ExecuteNonQueryResult(query, new List<DbParameter>());
                            }

                        }
                        catch (Exception ex)
                        {

                        }


                    }
                }

            }
            catch (Exception ex)
            {

            }

        }




        private static bool CheckIfTableNameExist(string targetDbName, string tableName, ValidationResult validationResult)
        {
            bool isTableExist = false;

            string query = "";

            query += @"
                SELECT count (*) as objCount FROM [{targetDbName}].sys.objects WHERE object_id = OBJECT_ID(N'[{targetDbName}].[dbo].[{tableName}]') AND type in (N'U');
            ";

            query = query.Replace("{targetDbName}", targetDbName)
                .Replace("{tableName}", tableName);



            using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    object quryResult = adpater.ExecuteScalarQuery(query, new List<System.Data.SqlClient.SqlParameter>());

                    int? objCount = ControlTypeValueConverter.ConvertValueToInt(quryResult);

                    if (objCount.HasValue && objCount.Value > 0)
                    {
                        isTableExist = true;
                    }
                }
                catch (Exception ex)
                {
                    validationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserExDto), "app_queryfailed_Error", ValidationItemType.Error, "Check Table Exist Query Error. \n" + ex.ToString() + "\nQuery:\n" + query + "\n" + query));
                }
            }

            return isTableExist;
        }


        private static string PrepareQuerySourceTableGuid(string srcDbName)
        {
            string query = "";

            foreach (string tableName in AppApplicationImportSettingDto.TablesWithGlobalGuid)
            {              
                query += @"
                           update [{srcDbName}].dbo.[{tableName}] set GlobalGuid = NEWID() where GlobalGuid is null;
                        ";

                query = query.Replace("{srcDbName}", srcDbName).Replace("{tableName}", tableName); ;
            }

            return query;
        }

            //string test = GetTableScript("a33_classroompc", AppMasterDBConnectionString);

            //public static string GetTableScript(string TableName, string ConnectionString)
            //{
            //    string Script = "";

            //    string Sql = "declare @table varchar(100)" + Environment.NewLine +
            //    "set @table = '" + TableName + "' " + Environment.NewLine +
            //    //"-- set table name here" +
            //    "declare @sql table(s varchar(1000), id int identity)" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    //"-- create statement" +
            //    "insert into  @sql(s) values ('create table [' + @table + '] (')" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    //"-- column list" +
            //    "insert into @sql(s)" + Environment.NewLine +
            //    "select " + Environment.NewLine +
            //    "    '  ['+column_name+'] ' + " + Environment.NewLine +
            //    "    data_type + coalesce('('+cast(character_maximum_length as varchar)+')','') + ' ' + " + Environment.NewLine +
            //    "    case when exists ( " + Environment.NewLine +
            //    "        select id from syscolumns" + Environment.NewLine +
            //    "        where object_name(id)=@table" + Environment.NewLine +
            //    "        and name=column_name" + Environment.NewLine +
            //    "        and columnproperty(id,name,'IsIdentity') = 1 " + Environment.NewLine +
            //    "    ) then" + Environment.NewLine +
            //    "        'IDENTITY(' + " + Environment.NewLine +
            //    "        cast(ident_seed(@table) as varchar) + ',' + " + Environment.NewLine +
            //    "        cast(ident_incr(@table) as varchar) + ')'" + Environment.NewLine +
            //    "    else ''" + Environment.NewLine +
            //    "   end + ' ' +" + Environment.NewLine +
            //    "    ( case when IS_NULLABLE = 'No' then 'NOT ' else '' end ) + 'NULL ' + " + Environment.NewLine +
            //    "    coalesce('DEFAULT '+COLUMN_DEFAULT,'') + ','" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    " from information_schema.columns where table_name = @table" + Environment.NewLine +
            //    " order by ordinal_position" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    //"-- primary key" +
            //    "declare @pkname varchar(100)" + Environment.NewLine +
            //    "select @pkname = constraint_name from information_schema.table_constraints" + Environment.NewLine +
            //    "where table_name = @table and constraint_type='PRIMARY KEY'" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    "if ( @pkname is not null ) begin" + Environment.NewLine +
            //    "    insert into @sql(s) values('  PRIMARY KEY (')" + Environment.NewLine +
            //    "    insert into @sql(s)" + Environment.NewLine +
            //    "        select '   ['+COLUMN_NAME+'],' from information_schema.key_column_usage" + Environment.NewLine +
            //    "        where constraint_name = @pkname" + Environment.NewLine +
            //    "        order by ordinal_position" + Environment.NewLine +
            //    //"    -- remove trailing comma" +
            //    "    update @sql set s=left(s,len(s)-1) where id=@@identity" + Environment.NewLine +
            //    "    insert into @sql(s) values ('  )')" + Environment.NewLine +
            //    "end" + Environment.NewLine +
            //    "else begin" + Environment.NewLine +
            //    //"    -- remove trailing comma" +
            //    "    update @sql set s=left(s,len(s)-1) where id=@@identity" + Environment.NewLine +
            //    "end" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    "-- closing bracket" + Environment.NewLine +
            //    "insert into @sql(s) values( ')' )" + Environment.NewLine +
            //    " " + Environment.NewLine +
            //    //"-- result!" +
            //    "select s from @sql order by id";

            //    DataTable dt = GetTableData(Sql, ConnectionString);
            //    foreach (DataRow row in dt.Rows)
            //    {
            //        Script += row[0].ToString() + Environment.NewLine;
            //    }

            //    return Script;
            //}

            //public static DataTable GetTableData(string Sql, string ConnectionString)
            //{
            //    SqlConnection con = new SqlConnection(ConnectionString);
            //    try
            //    {
            //        con.Open();
            //        SqlCommand selectCommand = new SqlCommand(Sql, con);
            //        DataSet dataSet = new DataSet();
            //        new SqlDataAdapter(selectCommand).Fill(dataSet);
            //        DataTable table = dataSet.Tables[0];
            //        return table;
            //    }
            //    catch (Exception)
            //    {
            //        return new DataTable();
            //    }
            //    finally
            //    {
            //        con.Close();
            //    }
            //}



        }
}
