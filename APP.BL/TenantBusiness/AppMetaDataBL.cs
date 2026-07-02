using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.Conversion;
using DatabaseSchemaMrg.DataSchema;
using DatabaseSchemaMrg.SqlGen;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

using APP.Framework;
namespace App.BL
{
    public static class AppMetaDataBL
    {
        static AppMetaDataBL()
        {

        }

        public static string GetUnitQulifiedTableName(AppTransactionUnitExDto unitExdto)
        {
            var dbFxiture = AppCacheManagerBL.GetOneDatabaseFixture(unitExdto.DataSourceFrom.Value);

            string fullTableName = AppMetaDataBL.GetQulifiedTableName(unitExdto.SchemaOwner, unitExdto.DataBaseTableName, dbFxiture.SqlServerType.Value);

            return fullTableName;

        }

        public static string GetQulifiedTableName(string schemaOwner, string tableName, EmSqlType dataBaseServerType)
        {
            if (dataBaseServerType == EmSqlType.SqlServer)
            {
                return string.Format("[{0}].[{1}]", schemaOwner, tableName);
            }
            else if (dataBaseServerType == EmSqlType.MySql)
            {
                //`test_2`.`table_2`
                return string.Format("`{0}`.`{1}`", schemaOwner, tableName);

            }
            else if (dataBaseServerType == EmSqlType.Oracle)
            {
                return string.Format("[{0}].[{1}]", schemaOwner, tableName);

            }
            else
            {
                return string.Format("{0}.{1}", schemaOwner, tableName);

            }


        }

        public static string GetQulifiedTableFiledName(string dbFieldName, EmSqlType dataBaseServerType)
        {
            if (dataBaseServerType == EmSqlType.SqlServer)
            {
                return string.Format("[{0}]", dbFieldName);
            }
            else if (dataBaseServerType == EmSqlType.MySql)
            {
                //`test_2`.`table_2`
                return string.Format("`{0}`", dbFieldName);

            }
            else if (dataBaseServerType == EmSqlType.Oracle)
            {
                return string.Format("[{0}]", dbFieldName);

            }
            else
            {
                return dbFieldName;

            }


        }

        public static TableDataDto GetTableData(string tableName, int? dataSourceRegisterId, string schemaOwner, int? recordLimit = null)
        {
            //	string conenctInfo = GetConnectInfo(dataSourceRegisterId);

            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            TableDataDto aTableDataDto = new TableDataDto();

            if (string.IsNullOrWhiteSpace(schemaOwner))
            {
                schemaOwner = databaseFixture.CurrentOwner;

            }

            aTableDataDto.SchemaOwner = schemaOwner;
            string fullTaleName = GetQulifiedTableName(tableName, schemaOwner, databaseFixture.SqlServerType.Value);


            //DataTable dataTAble = new DBInteractionBase(conenctInfo)
            //	.RetriveDataTable(string.Format(@" select * from {0}", fullTaleName));





            //DataTable dataTAble = new DBInteractionBase(conenctInfo)
            string topSelection = "";
            //if (recordLimit.HasValue)
            //{
            //    topSelection = "top " + recordLimit.Value;
            //}

            string queryTable = $@" select {topSelection} * from {fullTaleName}";

            string ownerTableKey = GetOwnerTableKey(schemaOwner, tableName);

            Dictionary<string, DatabaseTable> dictOwnerTablenameDataTable = AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);
            DatabaseTable dbtBLE = dictOwnerTablenameDataTable[ownerTableKey];

            SqlWriter sqlWriter = new SqlWriter(dbtBLE, databaseFixture.SqlServerType.Value);

            var dataTAble = databaseFixture.RetriveDataTable(sqlWriter.SelectAllSql(recordLimit), new List<DbParameter>());


            aTableDataDto.DictColumnDataType = new Dictionary<string, string>();

            aTableDataDto.Columns = new List<string>();

            foreach (DataColumn column in dataTAble.Columns)
            {


                aTableDataDto.Columns.Add(column.ColumnName);
                aTableDataDto.DictColumnDataType.Add(column.ColumnName, column.DataType.ToString());

            }

            aTableDataDto.DataRowList = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTAble.Rows)
            {
                Dictionary<string, object> dictRow = new Dictionary<string, object>();
                foreach (DataColumn column in dataTAble.Columns)
                {
                    dictRow[column.ColumnName] = row[column.ColumnName];

                }

                aTableDataDto.DataRowList.Add(dictRow);

            }




            return aTableDataDto;
        }



        public static string GetCurrentDbConnectionDefaultSchmeOner(int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            return databaseFixtureInstance.CurrentOwner;

            // should read from Cache
        }



        public static DatabaseTable GetOneDatabaseTableSchema(string tableName, int? dataSourceRegisterId, string schemaOwner)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            // should read from Cache

            if (string.IsNullOrWhiteSpace(schemaOwner))
            {
                schemaOwner = databaseFixtureInstance.CurrentOwner;
            }
            //AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);
            //Dictionary<string, DatabaseTable> dictOnerTable = AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);

            //string cacheKey = GetOwnerTableKey(schemaOwner, tableName);
            //DatabaseTable dbtable = dictOnerTable[cacheKey];

            DatabaseTable dbtable = databaseFixtureInstance.Table(tableName);

            if (dbtable != null)
            {
                // Need to update cache
                string cacheKey = GetOwnerTableKey(schemaOwner, tableName);
                Dictionary<string, DatabaseTable> dictOnerTable = AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);
                dictOnerTable[cacheKey] = dbtable;



                dbtable.DataSourceRegisterId = dataSourceRegisterId;

                foreach (var column in dbtable.Columns)
                {
                    //column.DbDataType
                    column.Tag = AppMetaDataSqlTypeConvertBL.ConvertSqlTypeToNetType(column, databaseFixtureInstance.SqlServerType);


                    column.NetName = ExtensionMethodhelper.RandomId();
                    // column.DataType.TypeName
                    // column.DataType.NetDataType
                }


                dbtable.Tag = ExtensionMethodhelper.RandomId();
            }

            //  dbtable.DataSourceRegisterId =

            return dbtable;

        }


        public static string GetDatabaseTableBuiltInQuery(string tableName, int? dataSourceRegisterId, string schemaOwner, EmAppBuiltInQueryType? emBuiltInQueryType)
        {
            string toReturn = "";


            if (emBuiltInQueryType.HasValue)
            {
                if (!dataSourceRegisterId.HasValue)
                {
                    dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                }

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

                //if (string.IsNullOrWhiteSpace(schemaOwner))
                //{
                //    schemaOwner = databaseFixtureInstance.CurrentOwner;
                //}

                DatabaseTable dbtable = databaseFixtureInstance.Table(tableName);

                var sqlWriter = new SqlWriter(dbtable, databaseFixtureInstance.SqlServerType.Value);


                if (emBuiltInQueryType.Value == EmAppBuiltInQueryType.Insert)
                {
                    toReturn = sqlWriter.InsertSql();
                }
                else if (emBuiltInQueryType.Value == EmAppBuiltInQueryType.Select)
                {
                    toReturn = sqlWriter.SelectAllSql();
                }
                else if (emBuiltInQueryType.Value == EmAppBuiltInQueryType.Update)
                {
                    toReturn = sqlWriter.UpdateWithConcurrencySql();
                }
                else if (emBuiltInQueryType.Value == EmAppBuiltInQueryType.Delete)
                {
                    toReturn = sqlWriter.DeleteSql();
                }
            }


            return toReturn;

        }

        public static List<string> GetDataBaseSchemaOwnerList(int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            var dictRegisterIdBaseTable = AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);

            return dictRegisterIdBaseTable.Values.Select(o => o.SchemaOwner).Distinct().ToList();




        }



        //internal static DatabaseFixture GetNewInstanceDatbaseFixture(int? dataSourceRegisterId,string owner)
        //{
        //	return new DatabaseFixture(GetConnectInfo(dataSourceRegisterId), SqlProvider, owner);
        //}


        /// <summary>
        ///  only to create new table .
        /// </summary>
        /// <param name="dataSourceRegisterId"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        internal static DatabaseFixture GetNewInstanceDatbaseFixtureWtihSchmaOwner(int? dataSourceRegisterId, string owner)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            if (databaseFixture.SqlServerType.Value == EmSqlType.MySql)
            {
                //Unlike other vendors, "db owner" is not a concept in MySQL.

                return AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            }
            else if (databaseFixture.SqlServerType.Value == EmSqlType.SqlServer)
            {
                return new DatabaseFixture(GetConnectInfo(dataSourceRegisterId), EmSqlType.SqlServer, owner);

            }
            //Need do Check oracle conenction
            else if (databaseFixture.SqlServerType.Value == EmSqlType.Oracle)
            {

            }

            return databaseFixture;



        }

        internal static string GetConnectInfo(int? dataSourceRegisterId)
        {
            //string conenctIfo = DBInteractionBase.APPConnectionString;

            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;
                //conenctIfo = AppCacheManagerBL.DictCustomerDatabaseFixtureInstance[dataSourceRegisterId.Value ].ConnectionString ; 
            }

            return AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value).ConnectionString;

        }

        public static string GetOwnerTableKey(string onwer, string tableName)
        {

            return string.Format("{0}_{1}", onwer.ToLower(), tableName.ToLower());

        }

        // key: onwer_tableName

        public static Dictionary<string, DatabaseTable> GetDatabaseTableBySchmaOnerTableNames(List<string> ownertableNameList, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }


            Dictionary<string, DatabaseTable> dictDatabaseTable = new Dictionary<string, DatabaseTable>();


            Dictionary<string, DatabaseTable> dictOwnerTablenameDataTable = AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);

            foreach (var onwetableName in ownertableNameList)
            {
                if (dictOwnerTablenameDataTable.ContainsKey(onwetableName))
                {
                    DatabaseTable dbtable = dictOwnerTablenameDataTable[onwetableName];

                    dictDatabaseTable[onwetableName] = dbtable;
                }

                //string keyFound = dictOwnerTablenameDataTable.Keys.FirstOrDefault(o => o.ToLower() == onwetableName.ToLower());

                //if (!string.IsNullOrEmpty(keyFound))
                //{
                //    DatabaseTable dbtable = dictOwnerTablenameDataTable[keyFound];
                //    dictDatabaseTable[onwetableName] = dbtable;
                //}

            }
            return dictDatabaseTable;

        }
        public static Dictionary<string, DatabaseTable> GetDatabaseTableSchemaDictionaryBySchemaOwnerTableNames(List<KeyValuePair<string, string>> ownertableNameList, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }


            Dictionary<string, DatabaseTable> dictDatabaseTable = new Dictionary<string, DatabaseTable>();


            Dictionary<string, DatabaseTable> dictOwnerTablenameDataTable = AppCacheManagerBL.GetDictOwnerTablenameDataTable(dataSourceRegisterId.Value);

            foreach (var dbotableName in ownertableNameList)
            {
                string owner = dbotableName.Key;
                string tableName = dbotableName.Value;

                if (!string.IsNullOrWhiteSpace(tableName))
                {

                    string key = GetOwnerTableKey(owner, tableName);


                    //DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, owner);


                    if (dictOwnerTablenameDataTable.ContainsKey(key))
                    {
                        DatabaseTable dbtable = dictOwnerTablenameDataTable[key];

                        if (!dictDatabaseTable.ContainsKey(key))
                        {
                            dictDatabaseTable.Add(key, dbtable);
                        }


                    }
                }
            }
            return dictDatabaseTable;

        }



        //private static DatabaseTable GetDatabaseTableByName(int? dataSourceRegisterId, DatabaseFixture databaseFixtureInstance, string tableName)
        //{
        //    DatabaseTable dbtable = databaseFixtureInstance.Table(tableName);
        //    dbtable.DataSourceRegisterId = dataSourceRegisterId;


        //    foreach (var column in dbtable.Columns)
        //    {
        //        column.Tag = ExtensionMethodhelper.RandomId();
        //        column.NetName = Guid.NewGuid().ToString();
        //    }

        //    dbtable.Tag = ExtensionMethodhelper.RandomId();
        //    return dbtable;
        //}



        public static void AddDatabaseColumn(DatabaseTable databaseTable, List<DatabaseColumn> listNewDatabaseColumn, DatabaseFixture databaseFixtureInstance)
        {




            var migration = GetMigration(databaseFixtureInstance);
            foreach (var newDatabaseColumn in listNewDatabaseColumn)
            {

                databaseTable.DatabaseSchema = databaseFixtureInstance.DatabaseSchema;
                string addNewColumn = migration.AddColumn(databaseTable, newDatabaseColumn);
                ExecSQlCommand(databaseFixtureInstance, addNewColumn);
            }

            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(databaseTable.DataSourceRegisterId.Value);


        }

        private static IMigrationGenerator GetMigration(DatabaseFixture databaseFixtureInstance)
        {
            var sqlType = FindSqlType(databaseFixtureInstance.DatabaseSchema);
            var ddlFactory = new DdlGeneratorFactory(sqlType.Value);
            var migration = ddlFactory.MigrationGenerator();
            return migration;
        }


        public static TableDataDto ExcuteQueryCheckSqlSynTaxResult(KeyValuePair<int?, string> dataSourceRegisterIdQuery, int? recordLimit = null)
        {

            return ExcuteQueryResult(dataSourceRegisterIdQuery, null, true);

        }

        public static TableDataDto ExcuteQueryResult(KeyValuePair<int?, string> dataSourceRegisterIdQuery, int? recordLimit = null, bool? isCheckSQlQuery = false, List<DbParameter> parameterList = null)
        {
            int? dataSourceRegisterId = dataSourceRegisterIdQuery.Key;
            string text = dataSourceRegisterIdQuery.Value;



            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            text = text.Trim();

            bool isSelestStatment = false;

            if (text.StartsWith("select", StringComparison.InvariantCultureIgnoreCase) &&

                Regex.IsMatch(text, "\\bfrom\\b", RegexOptions.IgnoreCase)

                )
            {
                //  SELECT TOP 50 * FROM(" + keyValue.Value + ") AS RESULT
                isSelestStatment = true;

                if (recordLimit.HasValue)
                {
                    DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

                    SqlWriter sqlWriter = new SqlWriter("", databaseFixtureInstance.SqlServerType.Value);

                    text = sqlWriter.SelectLimitedRecordFromQuery(text, recordLimit.Value);
                }
            }



            try
            {



                text = Regex.Replace(text, "\\bgo\\b", System.Environment.NewLine, RegexOptions.IgnoreCase);

                //where Id=[TF_32355_Id]

                // DataTable dataTAble = new DataTable();


                while (text.ContainsExt("[TF_", StringComparison.InvariantCultureIgnoreCase))
                {
                    string filedName = text.GetFirstStringBetweenStrings("[TF_", "]");
                    text = text.Replace($@"[TF_{filedName}]", "'-1'");
                }

                while (text.ContainsExt(AppTransactionCommandBL.GlobalTFPrefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    string filedName = text.GetFirstStringBetweenStrings(AppTransactionCommandBL.GlobalTFPrefix, "]");
                    text = text.Replace($@"[GlobalTF_{filedName}]", "'-1'");
                }

                if (isCheckSQlQuery.HasValue && isCheckSQlQuery.Value)
                {
                    if (text.ContainsExt("where", StringComparison.InvariantCultureIgnoreCase))
                    {
                        text.Replace("where", " WHERE 1=-1 AND ");
                    }
                    else
                    {
                        text = text + " WHERE 1=-1  ";
                    }
                }

                string returnErrorMsg = "";

                if (parameterList == null)
                {
                    parameterList = new List<DbParameter>();
                }

                var dataTAble = databaseFixture.RetriveDataTableWithErrorHandle(text, parameterList, out returnErrorMsg);

                if (dataTAble == null)
                {
                    if (!string.IsNullOrWhiteSpace(returnErrorMsg))
                    {
                        return PrepareQueryErrorAsTableDataDto(returnErrorMsg);
                    }
                    else
                    {
                        dataTAble = new DataTable();
                    }
                }


                TableDataDto aTableDataDto = null;


                // 
                //
                if (dataTAble.Rows.Count > 0)
                {
                    aTableDataDto = ConvertDataTableToDataTAbleDto(dataTAble);

                    bool isForJsonResult = text.ToLower().Contains("for json");

                    if (isForJsonResult)
                    {
                        ConvertTableDataDtoResultToJsonResult(aTableDataDto);
                    }
                }
                else
                {
                    aTableDataDto = new TableDataDto();
                    //aTableDataDto.ErrorMessage = "No Row Affect";
                }

                // need to refresh cache 

                // need refresh cashe
                if (!isSelestStatment)
                {
                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dataSourceRegisterId);
                }

                aTableDataDto.ErrorMessage = "Query completed successfully";


                return aTableDataDto;


                //if (isSelestStatment)
                //{
                //    var dataTAble = databaseFixture.RetriveDataTable(text, new List<DbParameter>());
                //    TableDataDto aTableDataDto = ConvertDataTableToDataTAbleDto(dataTAble);
                //    return aTableDataDto;

                //}
                //else // it is not select statment ,
                //{

                //    //SQL Server provides commands that are not Transact-SQL statements, but are recognized by the sqlcmd and osql utilities and SQL

                //    text = Regex.Replace(text, "\\bgo\\b", System.Environment.NewLine ,   RegexOptions.IgnoreCase);


                //    object result =  databaseFixture.RetriveScalar(text, new List<DbParameter>());

                //    TableDataDto aTableDataDto = new TableDataDto();
                //    aTableDataDto.ErrorMessage = result == null ? "No Recrod return" : result.ToString();
                //    return aTableDataDto;

                //}

            }
            catch (Exception ex)
            {
                string errorMsg = "System Error: " + ex.Message;

                return PrepareQueryErrorAsTableDataDto(errorMsg);
            }


        }

        private static TableDataDto PrepareQueryErrorAsTableDataDto(string errorMsg)
        {
            TableDataDto aTableDataDto = new TableDataDto();
            aTableDataDto.IsHaveErrors = true;
            aTableDataDto.ErrorMessage = errorMsg;
            return aTableDataDto;
        }






        private static void ConvertTableDataDtoResultToJsonResult(TableDataDto aTableDataDto)
        {
            string concatValue = "";

            foreach (var row in aTableDataDto.DataRowList)
            {
                foreach (string key in row.Keys)
                {
                    var aValue = row[key];
                    if (aValue != null)
                    {
                        concatValue += aValue.ToString();
                    }
                }

            }

            aTableDataDto.DataRowList = new List<Dictionary<string, object>>();

            var jsonRow = new Dictionary<string, object>();
            jsonRow.Add("JSON", concatValue);

            aTableDataDto.DataRowList.Add(jsonRow);
        }

        public static TableDataDto GetInstalledDbDriver()
        {


            DataTable toReturn = DatabaseFixture.GetServerInstallPorvider();

            return ConvertDataTableToDataTAbleDto(toReturn);



        }

        public static TableDataDto ConvertDataTableToDataTAbleDto(DataTable dataTAble)
        {
            TableDataDto aTableDataDto = new TableDataDto();
            aTableDataDto.Columns = new List<string>();

            foreach (DataColumn column in dataTAble.Columns)
            {


                aTableDataDto.Columns.Add(column.ColumnName);
            }

            aTableDataDto.DataRowList = new List<Dictionary<string, object>>();

            foreach (DataRow row in dataTAble.Rows)
            {
                Dictionary<string, object> dictRow = new Dictionary<string, object>();
                foreach (DataColumn column in dataTAble.Columns)
                {
                    object cellVAlue = row[column.ColumnName];
                    if (cellVAlue == DBNull.Value)
                    {
                        dictRow[column.ColumnName] = "";
                    }
                    else
                    {
                        dictRow[column.ColumnName] = cellVAlue;
                    }




                }

                aTableDataDto.DataRowList.Add(dictRow);

            }

            return aTableDataDto;
        }

        public static void AlterDatabaseColumn(DatabaseTable databaseTable, List<KeyValuePair<DatabaseColumn, DatabaseColumn>> listPairNewAsColumnKeyOrgColumnValue)
        {


            foreach (var keyPair in listPairNewAsColumnKeyOrgColumnValue)
            {

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(databaseTable.DataSourceRegisterId.Value);

                DatabaseColumn oldDataBaseColumn = keyPair.Key;
                AppMetaDataSqlTypeConvertBL.ConvertNetTypeToSqlType(oldDataBaseColumn, databaseFixtureInstance.SqlServerType);

                DatabaseColumn newDataColumn = keyPair.Value;
                int? newDataColumn_orgLength = newDataColumn.Length;
                AppMetaDataSqlTypeConvertBL.ConvertNetTypeToSqlType(newDataColumn, databaseFixtureInstance.SqlServerType);

                if (newDataColumn_orgLength.HasValue && newDataColumn_orgLength.Value == -1)
                {
                    newDataColumn.Length = -1;
                }

                ////GetNewInstanceDatbaseFixtureWtihSchmaOwner(databaseTable.DataSourceRegisterId, databaseTable.SchemaOwner);

                //if (!oldDataBaseColumn.IsPrimaryKey)
                //{
                //    oldDataBaseColumn.Nullable = true;
                //}

                //if (!newDataColumn.IsPrimaryKey)
                //{
                //    newDataColumn.Nullable = true;
                //}



                var migration = GetMigration(databaseFixtureInstance);


                // databaseTable.da

                databaseTable.DatabaseSchema = databaseFixtureInstance.DatabaseSchema;

                string alterColumn = migration.AlterColumn(databaseTable, newDataColumn, oldDataBaseColumn);


                ExecSQlCommand(databaseFixtureInstance, alterColumn);
            }

            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(databaseTable.DataSourceRegisterId.Value);

        }


        public static void RenameDatabaseColumn(DatabaseTable databaseTable, Dictionary<string, DatabaseColumn> dictOrgnameNewDataBasecolumn)
        {


            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(databaseTable.DataSourceRegisterId.Value);//GetNewInstanceDatbaseFixtureWtihSchmaOwner(databaseTable.DataSourceRegisterId, databaseTable.SchemaOwner);

            databaseTable.DatabaseSchema = databaseFixtureInstance.DatabaseSchema;

            foreach (string orgName in dictOrgnameNewDataBasecolumn.Keys)
            {
                DatabaseColumn newDataBaseColumn = dictOrgnameNewDataBasecolumn[orgName];
                var migration = GetMigration(databaseFixtureInstance);
                string renameColumn = migration.RenameColumn(databaseTable, newDataBaseColumn, orgName);
                ExecSQlCommand(databaseFixtureInstance, renameColumn);

            }

            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(databaseTable.DataSourceRegisterId.Value);

        }

        public static void DropDatabaseColumn(DatabaseTable databaseTable, List<DatabaseColumn> listDropDatabaseColumn)
        {


            //DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(databaseTable.DataSourceRegisterId, databaseTable.SchemaOwner);

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(databaseTable.DataSourceRegisterId.Value);

            var migration = GetMigration(databaseFixtureInstance);
            foreach (var databaseColumn in listDropDatabaseColumn)
            {
                string dropColumn = migration.DropColumn(databaseTable, databaseColumn);

                ExecSQlCommand(databaseFixtureInstance, dropColumn);

            }

            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(databaseTable.DataSourceRegisterId.Value);
        }

        public static string DropDatabaseTable(DatabaseTable databaseTable)
        {

            //DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(databaseTable.DataSourceRegisterId, databaseTable.SchemaOwner);

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(databaseTable.DataSourceRegisterId.Value);



            var migration = GetMigration(databaseFixtureInstance);


            string errorMsg = "";

            if (databaseTable.Description == "Views")
            {
                DatabaseView dbView = new DatabaseView();
                dbView.Name = databaseTable.Name;
                dbView.SchemaOwner = databaseTable.SchemaOwner;

                string dropView = migration.DropView(dbView).Replace("\r\nGO\r\n", "");
                errorMsg = ExecSQlCommand(databaseFixtureInstance, dropView);
            }
            else
            {
                string dropTable = migration.DropTable(databaseTable);
                errorMsg = ExecSQlCommand(databaseFixtureInstance, dropTable);
            }



            if (string.IsNullOrWhiteSpace(errorMsg))
            {
                Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();
                dictDbObjNameAndUsageType.Add(databaseTable.Name, (int)EmAppDataSetUsageType.DatabaseTable);

                AppDataSetBL.RemoveDatabaseObjectsFromAllApplication(dictDbObjNameAndUsageType);

                AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(databaseTable.DataSourceRegisterId.Value);
            }

            return errorMsg;

        }

        public static bool RenameTableName(string orgTableName, string newTableName, int? dataSourceRegisterId, string schemaOwner)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }


            // need to redo. use a new Dto to handle 2 parameters.

            //var AppMasterDatabaseFixtureInstance


            //DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, schemaOwner);

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);



            try
            {
                DatabaseTable orgTable = GetOneDatabaseTableSchema(orgTableName, dataSourceRegisterId, schemaOwner);
                orgTable.Name = newTableName;


                var migration = GetMigration(databaseFixtureInstance);

                string renameTable = migration.RenameTable(orgTable, orgTableName);
                ExecSQlCommand(databaseFixtureInstance, renameTable);


                AppDataSetBL.RenameDatabaseObjectsFromAllApplication(orgTableName, newTableName);

                AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dataSourceRegisterId);

                return true;

            }
            catch
            {
                return false;

            }



        }


        public static string FilterSQLDBInvalidChar(string tabGridColumnName)
        {
            return tabGridColumnName.
                Replace(' ', '_')
                .Replace('(', '_')
                .Replace(')', '_')
                .Replace('-', '_')
                .Replace('&', '_')
                .Replace("'", "")
                .Replace("#", "")
                .Replace("/", "_")
                .Replace('\\', '_')
                .Replace('.', '_')
                .Replace('$', '_')
                .Replace('*', '_')
                .Replace('%', '_')
                .Replace(',', '_')
                .Replace('+', '_')
                .Replace("delete", "DLT_")
                .Replace("FROM", "FRM")
                .Replace("SELECT", "SLCT"); ;
        }

        public static bool ExtractTableFromAppNewForm(object formId, int? dataSourceRegisterId)
        {
            ///int? dataSourceRegisterId = null;



            var appFormEntity = AppFormBL.RetrieveOneAppFormExDto(formId);

            // DODO!!1
            // step1: create trscatild from AppFormitem-->Transcation-->DatabaseTable-->update form item bind field
            // step2: craete table from trnascatio unit !!
            // step3: update appformitem bindibng filed ...
            // 1: root,
            var rootDataElements = appFormEntity.AppFormLayoutItemList.Where
            (
                o => o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value == 1
                 && o.DomAttribute.IsBindingToDataField
             );


            string tableMiddleName = FilterSQLDBInvalidChar(appFormEntity.Name);
            string rootTablename = "UDF_" + tableMiddleName + "_" + ServerContext.Instance.CurrentUid;



            bool result = CreateTablFromUIElments(rootDataElements, rootTablename, dataSourceRegisterId);

            // For the Child 

            var childDataUiElements = appFormEntity.AppFormLayoutItemList.Where
           (
               o => o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value == (int)EmAppTransactionUnitLevel.Child

            );

            string childTableFkColumnName = rootTablename + "ID";
            foreach (var childGrid in childDataUiElements)
            {
                List<AppFormLayoutItemExDto> childDataBidingElements = appFormEntity.AppFormLayoutItemList
                .Where(o => o.UigridLayoutParentId == childGrid.Id as int? && o.DomAttribute.IsBindingToDataField).ToList();

                string childTablename = FilterSQLDBInvalidChar(childGrid.DomAttribute.DisplayName);
                CreateTablFromUIElments(childDataBidingElements, childTablename, dataSourceRegisterId, childTableFkColumnName);


            }




            var grandChildDataElements = appFormEntity.AppFormLayoutItemList.Where
           (
                 o => (o.DomAttribute.TranscationUnitLevel.HasValue && o.DomAttribute.TranscationUnitLevel.Value == (int)EmAppTransactionUnitLevel.Grandchild)

            );


            return false;

        }

        private static bool CreateTablFromUIElments(IEnumerable<AppFormLayoutItemExDto> rootDataElements, string fullTablename, int? dataSourceRegisterId, string linkForeighKeyColumnName = "")
        {
            DatabaseTable databaseTableDto = new DatabaseTable();

            // make sure no deplciated table name
            databaseTableDto.Name = fullTablename;
            databaseTableDto.DataSourceRegisterId = dataSourceRegisterId;


            DatabaseColumn primaryColumn = new DatabaseColumn();
            primaryColumn.TableName = databaseTableDto.Name;
            DatabaseConstraint constraint = new DatabaseConstraint();
            constraint.TableName = databaseTableDto.Name;
            constraint.ConstraintType = ConstraintType.PrimaryKey;
            constraint.AddColumn(primaryColumn);
            constraint.Name = "PK_" + databaseTableDto.Name;
            databaseTableDto.PrimaryKey = constraint;
            primaryColumn.IsAutoNumber = true;

            primaryColumn.IdentityDefinition = new DatabaseColumnIdentity();
            databaseTableDto.Columns.Add(primaryColumn);


            // need to add Foreightkey
            if (!string.IsNullOrWhiteSpace(linkForeighKeyColumnName))
            {

                DatabaseColumn aDatabaseFKColumn = new DatabaseColumn();
                databaseTableDto.Columns.Add(aDatabaseFKColumn);
                aDatabaseFKColumn.Name = linkForeighKeyColumnName;
                aDatabaseFKColumn.Tag = EmAppDataType.Integer.ToString();

                databaseTableDto.Columns.Add(aDatabaseFKColumn);
            }


            foreach (AppFormLayoutItemExDto layItem in rootDataElements)
            {
                AppFormDomAttributeDto elementAtt = layItem.DomAttribute;
                DatabaseColumn aDatabaseColumn = new DatabaseColumn();
                databaseTableDto.Columns.Add(aDatabaseColumn);
                aDatabaseColumn.Name = FilterSQLDBInvalidChar(elementAtt.DisplayName);
                aDatabaseColumn.Tag = elementAtt.DataType.ToString();

                databaseTableDto.Columns.Add(aDatabaseColumn);
            }


            // int? dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;
            DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, databaseTableDto.SchemaOwner);
            foreach (DatabaseColumn column in databaseTableDto.Columns)
            {
                int? column_orgLength = column.Length;
                AppMetaDataSqlTypeConvertBL.ConvertNetTypeToSqlType(column, databaseFixtureInstance.SqlServerType);

                if (column_orgLength.HasValue && column_orgLength.Value == -1)
                {
                    column.Length = -1;
                }

                if (!column.IsPrimaryKey)
                {
                    column.Nullable = true;
                }
            }

            //
            var sqlType = databaseFixtureInstance.SqlServerType;

            var ddlFactory = new DdlGeneratorFactory(sqlType.Value);


            databaseTableDto.DatabaseSchema = databaseFixtureInstance.DatabaseSchema;


            var tg = ddlFactory.TableGenerator(databaseTableDto);
            tg.IncludeSchema = true;
            try
            {
                var txt = tg.Write();
                ExecSQlCommand(databaseFixtureInstance, txt);

                AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dataSourceRegisterId);
                return true;


            }
            catch (Exception exception)
            {

                return false;
                //Debug.WriteLine(exception.Message);
            }
        }

        public static bool CreateNewTable(DatabaseTable databaseTableDto, int? dataSourceRegisterId, int? applicationId, out string returnErrorMsg)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, databaseTableDto.SchemaOwner);

            try
            {
                databaseTableDto.Columns.ForAll(o =>
                {
                    if (!IsNumericColumnTagType(o.Tag))
                    {
                        o.IsAutoNumber = false;
                    }
                });

                var txt = PrepareCreateNewTableScript(databaseTableDto, dataSourceRegisterId);

                if (!string.IsNullOrWhiteSpace(txt))
                {
                    string error = ExecSQlCommand(databaseFixtureInstance, txt);

                    if (string.IsNullOrWhiteSpace(error))
                    {
                        AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(dataSourceRegisterId);

                        if (applicationId.HasValue)
                        {
                            Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();
                            dictDbObjNameAndUsageType.Add(databaseTableDto.Name, (int)EmAppDataSetUsageType.DatabaseTable);

                            AppDataSetBL.AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, applicationId.Value);
                        }

                        returnErrorMsg = "Table created successfully.";
                        return true;
                    }
                    else
                    {
                        returnErrorMsg = error;
                        return false;
                    }

                }
                else
                {
                    returnErrorMsg = "Invalid setting. Generate table creation script failed.";
                    return false;
                }
            }
            catch (Exception exception)
            {
                returnErrorMsg = exception.ToString();
                return false;
                //Debug.WriteLine(exception.Message);
            }
        }




        public static string PrepareCreateNewTableScript(DatabaseTable databaseTableDto, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, databaseTableDto.SchemaOwner);
            return PrepareCreateNewTableScript(databaseTableDto, databaseFixtureInstance);

        }

        public static string PrepareCreateNewTableScript(DatabaseTable databaseTableDto, DatabaseFixture databaseFixtureInstance)
        {
            if (databaseTableDto != null && databaseFixtureInstance != null)
            {
                var primaryColumn = databaseTableDto.Columns.FirstOrDefault(o => o.IsPrimaryKey);
                if (primaryColumn != null)
                {
                    primaryColumn.TableName = databaseTableDto.Name;
                    DatabaseConstraint constraint = new DatabaseConstraint();
                    constraint.TableName = databaseTableDto.Name;
                    constraint.ConstraintType = ConstraintType.PrimaryKey;
                    constraint.AddColumn(primaryColumn);
                    constraint.Name = "PK_" + databaseTableDto.Name;
                    databaseTableDto.PrimaryKey = constraint;
                    if (primaryColumn.IsAutoNumber)
                    {
                        primaryColumn.IsAutoNumber = true;

                        primaryColumn.IdentityDefinition = new DatabaseColumnIdentity();
                    }

                    // dbtable.Columns.Add(aPkDatabaseColumn);
                }



                foreach (DatabaseColumn column in databaseTableDto.Columns)
                {
                    int? column_orgLength = column.Length;
                    AppMetaDataSqlTypeConvertBL.ConvertNetTypeToSqlType(column, databaseFixtureInstance.SqlServerType);

                    if (column_orgLength.HasValue && column_orgLength.Value == -1)
                    {
                        column.Length = -1;
                    }

                }

                // only use for Add


                //
                var sqlType = databaseFixtureInstance.SqlServerType;

                var ddlFactory = new DdlGeneratorFactory(sqlType.Value);


                databaseTableDto.DatabaseSchema = databaseFixtureInstance.DatabaseSchema;


                var tg = ddlFactory.TableGenerator(databaseTableDto);
                tg.IncludeSchema = true;
                try
                {
                    var txt = tg.Write();
                    return txt;
                }
                catch (Exception exception)
                {

                    return "";
                    //Debug.WriteLine(exception.Message);
                }
            }

            return "";
        }

        public static string PrepareAlterTableScript(DatabaseTableInfoDto databaseTableDto, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }


            string query = "";

            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);
            string schemaOwner = dataBaseFixture.CurrentOwner;

            DatabaseTable existTable = AppCacheManagerBL.GetDatabaseTable(databaseTableDto.Name, dataSourceRegisterId, schemaOwner);

            if (existTable != null)
            {
                DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(dataSourceRegisterId, databaseTableDto.SchemaOwner);

                databaseTableDto.DictNewColumnNameAndDto = new Dictionary<string, DatabaseColumn>();

                foreach (DatabaseColumn column in databaseTableDto.Columns)
                {
                    if (!existTable.DictDataBaseColumn.ContainsKey(column.Name))
                    {

                        bool isColumnExist = dataBaseFixture.IsColumnExist(databaseTableDto.Name, column.Name);

                        if (!isColumnExist)
                        {
                            int? column_orgLength = column.Length;
                            AppMetaDataSqlTypeConvertBL.ConvertNetTypeToSqlType(column, databaseFixtureInstance.SqlServerType);

                            string columnType = column.DbDataType;

                            if (column_orgLength.HasValue && column_orgLength.Value == -1)
                            {
                                columnType = column.DbDataType + "(MAX)";
                            }
                            else if (column.Length.HasValue && column.Length.Value > 0)
                            {
                                columnType = column.DbDataType + "(" + column.Length.Value + ")";
                            }

                            query += "ALTER TABLE [" + databaseTableDto.Name + "] ADD [" + column.Name + "] " + columnType + " null \n ";


                            databaseTableDto.DictNewColumnNameAndDto.Add(column.Name, column);
                        }
                    }

                }
            }

            return query;

        }


        internal static string ExecSQlCommand(DatabaseFixture aDatabaseFixture, string queryText, bool splitQueryBySemicolon = true, List<DbParameter> parameterList = null, List<object> resultSet = null)
        {
                      
            queryText = aDatabaseFixture.ReformatQuery(queryText);

            DbProviderFactory factory = aDatabaseFixture.DbProviderFactory;
            string[] singleStateList = new string[1] { queryText };

            if (splitQueryBySemicolon)
            {
                singleStateList = queryText.Split(";".ToCharArray());
            }

            using (var connection = factory.CreateConnection())
            {
                connection.ConnectionString = aDatabaseFixture.ConnectionString;
                connection.Open();

                DbTransaction trans = connection.BeginTransaction();

                try
                {

                    // var connection = trans.Connection;


                    //	trans.ConnectionM

                    foreach (string query in singleStateList)
                    {
                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            DbCommand cmd = connection.CreateCommand();

                            if (parameterList != null)
                            {
                                cmd.Parameters.AddRange(parameterList.ToArray());
                            }

                            cmd.Transaction = trans;
                            cmd.CommandText = query;
                            cmd.CommandTimeout = 0; //180;
                            var result = cmd.ExecuteScalar();

                            if (resultSet != null)
                            {
                                resultSet.Add(result);
                            }
                        }
                    }


                    trans.Commit();

                    return "";

                }

                catch (Exception exception)
                {

                    trans.Rollback();
                    // for oracle then that does not support running more than one statement. 
                    //no oracle installed = System.Exception: System.Data.OracleClient requires Oracle client software version 8.1.7 or greater.
                    //Assert.Inconclusive("Cannot access database for provider " + providerName + " message= " + exception.Message);

                    return exception.ToString() + " \n\n Query: \n" + queryText;
                }

            }


        }

        private static EmSqlType? FindSqlType(DatabaseSchema databaseSchema)
        {
            var providerName = databaseSchema.Provider;
            return ProviderToSqlType.Convert(providerName);
        }

        static void CreateTablldd(DatabaseFixture databaseFixture, DatabaseTable databaseTable)
        {
            var sqlType = FindSqlType(databaseFixture.DatabaseSchema);
            var ddlFactory = new DdlGeneratorFactory(sqlType.Value);


            var tg = ddlFactory.TableGenerator(databaseTable);
            tg.IncludeSchema = true;
            try
            {
                var txt = tg.Write();
                //Clipboard.SetText(txt, TextDataFormat.UnicodeText);

                // aDatabaseReader.DatabaseSchema.

                ExecSQlCommand(databaseFixture, txt);


            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }
        }



        // need to add validation !!!
        public static OperationCallResult<DatabaseTable> SaveModifiedTableSchema(SchemaMetaDataDto schemaMetaDataDto, int? dataSourceRegisterId)
        {
            OperationCallResult<DatabaseTable> toReturn = new OperationCallResult<DatabaseTable>();

            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }


            try
            {
                DatabaseTable orgDatabaseTable = GetOneDatabaseTableSchema(schemaMetaDataDto.OriginalTableName, dataSourceRegisterId, schemaMetaDataDto.SchemaOwner); ;





                //  schemaMetaDataDto.DatabaseTable = datatableFromDB;


                AddDatabaseColumn(schemaMetaDataDto, orgDatabaseTable);
                AlterDatabaseColumn(schemaMetaDataDto, orgDatabaseTable);
                RenameDatabaseColumn(schemaMetaDataDto, orgDatabaseTable);
                DropDatabaseColumn(schemaMetaDataDto, orgDatabaseTable);

                //  string tableName = schemaMetaDataDto.DatabaseTable.Name;


                if (!string.IsNullOrWhiteSpace(schemaMetaDataDto.NewTableName) && schemaMetaDataDto.NewTableName != orgDatabaseTable.Name)
                {
                    bool isRenmaeSuccess = RenameTableName(orgDatabaseTable.Name, schemaMetaDataDto.NewTableName, orgDatabaseTable.DataSourceRegisterId, orgDatabaseTable.SchemaOwner);

                    if (isRenmaeSuccess)
                    {
                        // schemaMetaDataDto.DatabaseTable.Name = schemaMetaDataDto.NewTableName;
                        orgDatabaseTable = GetOneDatabaseTableSchema(schemaMetaDataDto.NewTableName, dataSourceRegisterId, schemaMetaDataDto.SchemaOwner); ;

                    }
                }

                DatabaseTable datatable = GetOneDatabaseTableSchema(orgDatabaseTable.Name, dataSourceRegisterId, schemaMetaDataDto.SchemaOwner); ;

                toReturn.Object = datatable;

                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_SaveModifiedTableSchema_Success", ValidationItemType.Message, "Table saved successfully."));
            }
            catch (Exception ex)
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionEntity), "App_SaveModifiedTableSchema_Error", ValidationItemType.Error, ex.ToString()));

            }


            return toReturn;

        }

        public static bool IsNumericColumnTagType(object tag)
        {
            string tagName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(tag);
            if (tagName == EmAppDataType.Decimal.ToString()
                || tagName == EmAppDataType.Integer.ToString()
                || tagName == EmAppDataType.Tinyint.ToString()
                || tagName == EmAppDataType.Smallint.ToString()
                || tagName == EmAppDataType.BigInt.ToString()
                || tagName == EmAppDataType.UInt8.ToString()
                || tagName == EmAppDataType.UInt16.ToString()
                || tagName == EmAppDataType.UInt32.ToString()
                || tagName == EmAppDataType.UInt64.ToString()

                )
            {
                return true;
            }

            return false;
        }

        public static bool AddDatabaseColumn(SchemaMetaDataDto schemaMetaDataDto, DatabaseTable databaseTable)
        {

            // need to redo. use a new Dto to handle 2 parameters.
            if (schemaMetaDataDto != null)
            {
                if (schemaMetaDataDto.ListNewDatabaseColumn != null && schemaMetaDataDto.ListNewDatabaseColumn.Count > 0)
                {

                    DatabaseFixture databaseFixtureInstance = GetNewInstanceDatbaseFixtureWtihSchmaOwner(databaseTable.DataSourceRegisterId, databaseTable.SchemaOwner);

                    foreach (var column in schemaMetaDataDto.ListNewDatabaseColumn)
                    {
                        if (column.IsAutoNumber)
                        {
                            if (IsNumericColumnTagType(column.Tag) == false)
                            {
                                column.IsAutoNumber = false;
                            }
                        }

                        int? column_orgLength = column.Length;
                        AppMetaDataSqlTypeConvertBL.ConvertNetTypeToSqlType(column, databaseFixtureInstance.SqlServerType);

                        if (column_orgLength.HasValue && column_orgLength.Value == -1)
                        {
                            column.Length = -1;
                        }
                    }

                    //  schemaMetaDataDto.DatabaseTable.DefaultConstraints 

                    AppMetaDataBL.AddDatabaseColumn(databaseTable, schemaMetaDataDto.ListNewDatabaseColumn, databaseFixtureInstance);
                    return true;
                }

            }

            return false;
        }



        public static bool AlterDatabaseColumn(SchemaMetaDataDto schemaMetaDataDto, DatabaseTable databaseTable)
        {
            // need to redo. use a new Dto to handle 2 parameters.
            if (schemaMetaDataDto != null)
            {
                if (!schemaMetaDataDto.ListPairAlterDatabaseColumn.IsEmpty())
                {

                    AppMetaDataBL.AlterDatabaseColumn(databaseTable, schemaMetaDataDto.ListPairAlterDatabaseColumn);
                    return true;
                }

            }

            return false;
        }


        public static bool RenameDatabaseColumn(SchemaMetaDataDto schemaMetaDataDto, DatabaseTable databaseTable)
        {
            // need to redo. use a new Dto to handle 2 parameters.
            if (schemaMetaDataDto != null)
            {
                if (schemaMetaDataDto.DictOrgnameNewDataBasecolumn != null && schemaMetaDataDto.DictOrgnameNewDataBasecolumn.Count > 0)
                {

                    AppMetaDataBL.RenameDatabaseColumn(databaseTable, schemaMetaDataDto.DictOrgnameNewDataBasecolumn);
                    return true;
                }

            }

            return false;
        }




        public static bool DropDatabaseColumn(SchemaMetaDataDto schemaMetaDataDto, DatabaseTable databaseTable)
        {
            // need to redo. use a new Dto to handle 2 parameters.
            if (schemaMetaDataDto != null)
            {
                if (schemaMetaDataDto.ListDropDatabaseColumn != null && schemaMetaDataDto.ListDropDatabaseColumn.Count > 0)
                {

                    AppMetaDataBL.DropDatabaseColumn(databaseTable, schemaMetaDataDto.ListDropDatabaseColumn);
                    return true;
                }


            }

            return false;
        }




        //  private static List<DatabaseTableDto> GetDataSourceTableAndViewList(int? dataSourceRegisterId)




        public static List<DatabaseTableDto> GetSaasDataSourceTableAndViewList(int? dataSourceRegisterId, int? saasFilterOption, int? filterByApplicationId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            string conenctInfor = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId);

            List<DatabaseTableDto> allTableDtoList = GetDataSourceTableAndViewList(dataSourceRegisterId);
            List<DatabaseTableDto> sysTableList = new List<DatabaseTableDto>();
            List<DatabaseTableDto> udTableList = new List<DatabaseTableDto>();
            List<DatabaseTableDto> viewList = new List<DatabaseTableDto>();

            foreach (var tableDto in allTableDtoList)
            {
                if (!tableDto.IsDbView)
                {
                    if (tableDto.Name.ToLower().StartsWith("app"))
                    {
                        sysTableList.Add(tableDto);
                    }
                    else
                    {
                        udTableList.Add(tableDto);
                    }
                }
                else
                {
                    viewList.Add(tableDto);
                }

            }

            sysTableList = sysTableList.Where(o => !SaasRestrictSystemTableNameList.Contains(o.Name.ToLower())).ToList();

            var allVisibleTableAndViews = sysTableList.Union(udTableList).Union(viewList).ToList();

            if (saasFilterOption.HasValue)
            {
                if (saasFilterOption.Value == (int)EmAppSaasTableFilterOption.AllTable)
                {
                    return allVisibleTableAndViews;
                }
                else if (saasFilterOption.Value == (int)EmAppSaasTableFilterOption.AllSystemTable)
                {
                    return sysTableList;
                }
                else if (saasFilterOption.Value == (int)EmAppSaasTableFilterOption.ByApplication)
                {
                    List<string> appTableNameList = GetApplicationTableNameList(filterByApplicationId);

                    var appTableDtoList = allVisibleTableAndViews.Where(o => appTableNameList.Contains(o.Name.ToLower())).ToList();

                    return appTableDtoList;
                }
                else if (saasFilterOption.Value == (int)EmAppSaasTableFilterOption.AllView)
                {
                    return viewList;
                }
            }

            return allVisibleTableAndViews;


        }

        private static List<string> GetApplicationTableNameList(int? filterByApplicationId)
        {
            List<string> appTableList = new List<string>();

            if (filterByApplicationId.HasValue)
            {
                string queryGetTableNames = @"                
                    DECLARE @SaasApplicationID int = {SaasApplicationID}
                    select distinct TableName 
                    FROM
                    (
                        select DataBaseTableName as TableName from [AppTransactionUnit] 
                        where DataBaseTableName is not null and DataBaseTableName <> '' and (	
		                    TransactionID in (
                                select TransactionID from [AppTransaction] where SaasApplicationID = @SaasApplicationID
                            )
		                    or TransactionID in (
                                select TransactionID from AppApplicationAssetsItem where TransactionID is not null and ApplicationID = @SaasApplicationID
                            )
	                    )    
                        UNION
                        select TableName from [AppEntityInfo] 
                        where SaasApplicationID = @SaasApplicationID
                            and EntityType = 1 and TableName is not null and TableName <> ''
                        UNION
                        select [Name] as TableName from AppDataSet where UsageTypeID in (11,12,13) and SaasApplicationID = @SaasApplicationID and [Name] is not null and [Name] <> ''
                    ) as TableNames;        
              
                ";

                queryGetTableNames = queryGetTableNames.Replace("{SaasApplicationID}", filterByApplicationId.Value.ToString());



                using (var adpater = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        DataTable dtUserTableName = adpater.ExecuteDataTableRetrievalQuery(queryGetTableNames, new List<System.Data.SqlClient.SqlParameter>());

                        foreach (DataRow dataRow in dtUserTableName.Rows)
                        {
                            string tableName = ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(dataRow["TableName"]).Trim().ToLower();

                            if (!string.IsNullOrWhiteSpace(tableName) && !appTableList.Contains(tableName))
                            {
                                appTableList.Add(tableName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            return appTableList;
        }

        private static void FindTransactionIdFromMenuTree(List<int> toReturnTransactionIdList, AppListMenuExDto parentMenu)
        {
            List<int> menuSearchIdList = new List<int>();

            foreach (var childMenu in parentMenu.AppListMenu_List)
            {
                if (!string.IsNullOrWhiteSpace(childMenu.RouteCode))
                {


                    if (childMenu.RouteCode.Trim().ToLower() == "FormListEdit".ToLower())
                    {
                        int? transactionId = ControlTypeValueConverter.ConvertValueToInt(childMenu.Link);
                        if (transactionId.HasValue)
                        {
                            if (!toReturnTransactionIdList.Contains(transactionId.Value))
                            {
                                toReturnTransactionIdList.Add(transactionId.Value);
                            }
                        }
                    }
                    else if (childMenu.RouteCode.Trim().ToLower() == "MasterDataManagement".ToLower())
                    {
                        int? searchId = ControlTypeValueConverter.ConvertValueToInt(childMenu.Link);

                        if (searchId.HasValue)
                        {
                            menuSearchIdList.Add(searchId.Value);
                        }
                    }

                    if (childMenu.AppListMenu_List != null && childMenu.AppListMenu_List.Count > 0)
                    {
                        FindTransactionIdFromMenuTree(toReturnTransactionIdList, childMenu);
                    }
                }
            }

            if (menuSearchIdList.Count > 0 && !parentMenu.ParentId.HasValue)
            {
                FindSearchRelatedLinkTargetTransactionIds(toReturnTransactionIdList, menuSearchIdList);
            }
        }

        private static void FindSearchRelatedLinkTargetTransactionIds(List<int> toReturnTransactionIdList, List<int> menuSearchIdList)
        {
            string searchIds = string.Empty;
            menuSearchIdList.Distinct().ForAll(o => searchIds += o.ToString() + ",");
            searchIds = searchIds.Substring(0, searchIds.Length - 1);

            string query = @" SELECT DISTINCT AppFormLinkTarget.LinkTargetTransactionID
                                     FROM AppDataSet INNER JOIN
                                        AppSearch ON AppDataSet.DataSetID = AppSearch.DataSetID INNER JOIN
                                        AppSearchView ON AppDataSet.DataSetID = AppSearchView.DataSetID INNER JOIN
                                        AppFormLinkTarget ON (AppSearchView.SearchViewID = AppFormLinkTarget.SearchViewID and AppFormLinkTarget.LinkTargetTransactionID IS NOT NULL)";

            query = query + " WHERE AppSearch.SearchID IN (" + searchIds + ")";

            try
            {

                // need to use DataAccessAdapter

                DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(ServerContext.Instance.CurrnetClientIdentity.DataSourceId);

                if (databaseFixtureInstance != null)
                {
                    DataTable transactioIdDataTable = databaseFixtureInstance.RetriveDataTable(query, new List<DbParameter>());
                    foreach (DataRow row in transactioIdDataTable.Rows)
                    {
                        int? transId = ControlTypeValueConverter.ConvertValueToInt(row["LinkTargetTransactionID"]);
                        if (transId.HasValue)
                        {
                            if (!toReturnTransactionIdList.Contains(transId.Value))
                            {
                                toReturnTransactionIdList.Add(transId.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        // will use to simple data transfer , need to fillter table with prefix App
        //public static List<DatabaseTableDto> GetDataSourceTableAndViewList(int? dataSourceRegisterId)
        //{


        //    string conenctInfor = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId);

        //    List<DatabaseTableDto> toReturn = GetDataBaseTableViewListDot(conenctInfor);

        //    return toReturn;


        //}


        internal static List<DatabaseTableDto> GetDataSourceTableAndViewList(int? dataSourceRegisterId)
        {


            if (!dataSourceRegisterId.HasValue)
            {
                // dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
                return null;
            }

            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            IList<DatabaseTable> allDatabasetale = databaseFixture.AllTables();
            IList<DatabaseView> allTableView = databaseFixture.AllViews();

            // databaseFixture.Table()
            List<DatabaseTableDto> toReturn = new List<DatabaseTableDto>();

            foreach (var dbtable in allDatabasetale)
            {

                // dbtable.ForeignKeyChildren = null; 
                DatabaseTableDto aDatabaseTableDto = new DatabaseTableDto();
                toReturn.Add(aDatabaseTableDto);

                aDatabaseTableDto.Name = dbtable.Name;
                aDatabaseTableDto.Description = dbtable.Description;
                aDatabaseTableDto.NetName = dbtable.NetName;

                //  aDatabaseTableDto.PrimaryKeyColumn = dbtable.PrimaryKeyColumn;
                aDatabaseTableDto.HasAutoNumberColumn = dbtable.HasAutoNumberColumn;
                aDatabaseTableDto.HasCompositeKey = dbtable.HasCompositeKey;
                aDatabaseTableDto.SchemaOwner = dbtable.SchemaOwner;
                aDatabaseTableDto.ObjType = "Tables";



                aDatabaseTableDto.EmDataSourceType = (int)databaseFixture.SqlServerType;

            }

            //dbtablev.SchemaOwner
            //
            foreach (var dbtablev in allTableView.Where(o => o.SchemaOwner == databaseFixture.CurrentOwner))
            {
                // dbtable.ForeignKeyChildren = null; 
                DatabaseTableDto aDatabaseTableDto = new DatabaseTableDto();
                toReturn.Add(aDatabaseTableDto);

                aDatabaseTableDto.Name = dbtablev.Name;
                aDatabaseTableDto.Description = dbtablev.Description;
                aDatabaseTableDto.NetName = dbtablev.NetName;

                //  aDatabaseTableDto.PrimaryKeyColumn = dbtable.PrimaryKeyColumn;
                aDatabaseTableDto.HasAutoNumberColumn = dbtablev.HasAutoNumberColumn;
                aDatabaseTableDto.HasCompositeKey = dbtablev.HasCompositeKey;
                aDatabaseTableDto.IsDbView = true;
                aDatabaseTableDto.ObjType = "Views";

                aDatabaseTableDto.SchemaOwner = dbtablev.SchemaOwner;
                aDatabaseTableDto.EmDataSourceType = (int)databaseFixture.SqlServerType;
            }

            return toReturn;
        }

        // will use to simple data transfer , need to fillter table with prefix App
        public static List<DatabaseTableDto> GetDatabaseSchemaTableDto(bool? isGetTableOnly, bool? isSystemBuitIn = null)
        {

            DatabaseFixture databaseFixture = new DatabaseFixture(ServerContext.Instance.CurrentUserDataBaseName, EmSqlType.SqlServer);
            // DatabaseSchema databaseSchema = databaseFixture.AllTables()

            IList<DatabaseTable> allDatabasetale = databaseFixture.AllTables();


            List<DatabaseTable> userDefinelAllTables = new List<DatabaseTable>();

            if (isSystemBuitIn.HasValue)
            {
                // onlt get sysdefin
                if (isSystemBuitIn.HasValue && isSystemBuitIn.Value)
                {
                    userDefinelAllTables = allDatabasetale
                        .Where(o => (o.Name.StartsWith("App", true, System.Globalization.CultureInfo.InvariantCulture))).ToList();
                }
                else
                {
                    userDefinelAllTables = allDatabasetale
                    .Where(o => (!o.Name.StartsWith("App", true, System.Globalization.CultureInfo.InvariantCulture))).ToList();
                }
            }
            else
            {
                userDefinelAllTables = allDatabasetale.ToList();
            }


            List<DatabaseTableDto> toReturn = new List<DatabaseTableDto>();

            foreach (var dbtable in userDefinelAllTables)
            {

                // dbtable.ForeignKeyChildren = null; 
                DatabaseTableDto aDatabaseTableDto = new DatabaseTableDto();
                toReturn.Add(aDatabaseTableDto);

                aDatabaseTableDto.Name = dbtable.Name;
                aDatabaseTableDto.Description = dbtable.Description;
                aDatabaseTableDto.NetName = dbtable.NetName;

                //  aDatabaseTableDto.PrimaryKeyColumn = dbtable.PrimaryKeyColumn;
                aDatabaseTableDto.HasAutoNumberColumn = dbtable.HasAutoNumberColumn;
                aDatabaseTableDto.HasCompositeKey = dbtable.HasCompositeKey;

            }

            if (!(isGetTableOnly.HasValue && isGetTableOnly.Value))
            {
                var allTableView = databaseFixture.AllViews();

                if (isSystemBuitIn.HasValue)
                {
                    // onlt get sysdefin
                    if (isSystemBuitIn.HasValue && isSystemBuitIn.Value)
                    {
                        allTableView = allTableView
                            .Where(o => (o.Name.StartsWith("App", true, System.Globalization.CultureInfo.InvariantCulture))).ToList();
                    }
                    else
                    {
                        allTableView = allTableView
                        .Where(o => (!o.Name.StartsWith("App", true, System.Globalization.CultureInfo.InvariantCulture))).ToList();
                    }
                }

                foreach (var dbtablev in allTableView)
                {
                    // dbtable.ForeignKeyChildren = null; 
                    DatabaseTableDto aDatabaseTableDto = new DatabaseTableDto();
                    toReturn.Add(aDatabaseTableDto);

                    aDatabaseTableDto.Name = dbtablev.Name;
                    aDatabaseTableDto.Description = dbtablev.Description;
                    aDatabaseTableDto.NetName = dbtablev.NetName;

                    //  aDatabaseTableDto.PrimaryKeyColumn = dbtable.PrimaryKeyColumn;
                    aDatabaseTableDto.HasAutoNumberColumn = dbtablev.HasAutoNumberColumn;
                    aDatabaseTableDto.HasCompositeKey = dbtablev.HasCompositeKey;
                    aDatabaseTableDto.IsDbView = true;
                }
            }


            return toReturn;


        }

        public static List<DatabaseTableDto> GetDatabaseTableDetail(bool isUserDefinedTableOnly = true)
        {

            List<DatabaseTableDto> roReturn = new List<DatabaseTableDto>();

            string queryTableDetail = @" select TableName, SchemaName, RowCounts, TotalSpaceKB, TotalSpaceMB, UsedSpaceKB, UsedSpaceMB, UnusedSpaceKB, UnusedSpaceMB from ViewAppTableDetail";
            if (isUserDefinedTableOnly)
            {
                queryTableDetail += " where TableName not like 'app%'";
            }


            //using (DBInteractionBase conn = new DBInteractionBase())
            //{

            //DataTable dt = conn.RetriveDataTable(queryTableDetail);

            //	foreach (DataRow row in dt.Rows)
            //	{
            //		DatabaseTableDto aDatabaseTableDto = new DatabaseTableDto();
            //		aDatabaseTableDto.Name = row["TableName"] as string;
            //		aDatabaseTableDto.NetName = row["TableName"] as string;
            //		aDatabaseTableDto.SchemaName = row["SchemaName"] as string;
            //		aDatabaseTableDto.RowCounts = row["RowCounts"] as int?;
            //		aDatabaseTableDto.TotalSpaceKB = row["TotalSpaceKB"] as decimal?;
            //		aDatabaseTableDto.TotalSpaceMB = row["TotalSpaceMB"] as decimal?;
            //		aDatabaseTableDto.UsedSpaceKB = row["UsedSpaceKB"] as decimal?;
            //		aDatabaseTableDto.UsedSpaceMB = row["UsedSpaceMB"] as decimal?;
            //		aDatabaseTableDto.UnusedSpaceKB = row["UnusedSpaceKB"] as decimal?;
            //		aDatabaseTableDto.UnusedSpaceMB = row["UnusedSpaceMB"] as decimal?;


            //		roReturn.Add(aDatabaseTableDto);

            //	}
            //}



            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                DataTable dt = adpater.ExecuteDataTableRetrievalQuery(queryTableDetail, new List<System.Data.SqlClient.SqlParameter>());

                foreach (DataRow row in dt.Rows)
                {
                    DatabaseTableDto aDatabaseTableDto = new DatabaseTableDto();
                    aDatabaseTableDto.Name = row["TableName"] as string;
                    aDatabaseTableDto.NetName = row["TableName"] as string;
                    aDatabaseTableDto.SchemaName = row["SchemaName"] as string;
                    aDatabaseTableDto.RowCounts = row["RowCounts"] as int?;
                    //aDatabaseTableDto.TotalSpaceKB = row["TotalSpaceKB"] as decimal?;
                    //aDatabaseTableDto.TotalSpaceMB = row["TotalSpaceMB"] as decimal?;
                    //aDatabaseTableDto.UsedSpaceKB = row["UsedSpaceKB"] as decimal?;
                    //aDatabaseTableDto.UsedSpaceMB = row["UsedSpaceMB"] as decimal?;
                    //aDatabaseTableDto.UnusedSpaceKB = row["UnusedSpaceKB"] as decimal?;
                    //aDatabaseTableDto.UnusedSpaceMB = row["UnusedSpaceMB"] as decimal?;


                    roReturn.Add(aDatabaseTableDto);

                }
            }

            return roReturn;
        }

        public static List<string> GetSysStoredProcedureNameList(string prefix, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            //  string connetInfo = PdmDataSourceBL.GetConnectionInfoWithCode(aEmDataSourceFrom);

            //         string queryStoredProcedureNameList = string.Empty;
            //         if (string.IsNullOrEmpty(prefix))
            //         {
            //             queryStoredProcedureNameList = @"select SPECIFIC_NAME
            //            from information_schema.routines  where routine_type = 'PROCEDURE'   order by SPECIFIC_NAME ";
            //         }
            //         else
            //         {
            //             queryStoredProcedureNameList = @"select SPECIFIC_NAME
            //            from information_schema.routines  where routine_type = 'PROCEDURE' and SPECIFIC_NAME like '" + prefix + "%'   order by SPECIFIC_NAME ";

            //         }

            //string connectInfo = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId); 
            //         using (DBInteractionBase conn = new DBInteractionBase(connectInfo))
            //         {

            //             return conn.RetriveMutilValues(queryStoredProcedureNameList).Cast<string>().ToList();
            //         }

            //  string connetInfo = PdmDataSourceBL.GetConnectionInfoWithCode(aEmDataSourceFrom);
            DatabaseFixture databaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);

            var listStoreProc = databaseFixture.AllStoredProcedures().Select(o => o.Name).ToList();
            if (!string.IsNullOrEmpty(prefix))
            {
                listStoreProc = listStoreProc.Where(o => o.StartsWith(prefix, true, null)).ToList();
            }





            return listStoreProc;

        }


        public static List<AppDataSetParameterDto> GetStoredProcedureParamterList(string storeProcName, int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            //string connetInfo = PdmDataSourceBL.GetConnectionInfoWithCode(aEmDataSourceFrom);

            // = string.Empty;

            List<AppDataSetParameterDto> roReturn = new List<AppDataSetParameterDto>();

            string queryStoredProcedureNameList = @" select PARAMETER_NAME as Name,  PARAMETER_MODE AS Mode, Data_Type as DataType from information_schema.parameters
                where specific_name='" + storeProcName + @"'";


            string conneInfo = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId);
            //using (DBInteractionBase conn = new DBInteractionBase(conneInfo))
            //{

            //	DataTable dt = conn.RetriveDataTable(queryStoredProcedureNameList);

            //	foreach (DataRow row in dt.Rows)
            //	{
            //		AppDataSetParameterDto aAppDataSetParameterDto = new AppDataSetParameterDto();
            //		aAppDataSetParameterDto.ParameterName = row["Name"] as string;
            //		aAppDataSetParameterDto.DirectionInOut = row["Mode"] as string;
            //		aAppDataSetParameterDto.DataType = row["DataType"] as string;
            //		// aAppDataSetParameterDto.DefautValue = 

            //		roReturn.Add(aAppDataSetParameterDto);

            //	}


            //}


            using (DataAccessAdapter adpater = new DataAccessAdapter(conneInfo))
            {
                DataTable dt = adpater.ExecuteDataTableRetrievalQuery(queryStoredProcedureNameList, new List<System.Data.SqlClient.SqlParameter>());
                foreach (DataRow row in dt.Rows)
                {
                    AppDataSetParameterDto aAppDataSetParameterDto = new AppDataSetParameterDto();
                    aAppDataSetParameterDto.ParameterName = row["Name"] as string;
                    aAppDataSetParameterDto.DirectionInOut = row["Mode"] as string;
                    aAppDataSetParameterDto.DataType = row["DataType"] as string;
                    // aAppDataSetParameterDto.DefautValue = 

                    roReturn.Add(aAppDataSetParameterDto);

                }
            }

            return roReturn;

        }

        public static List<LookupItemDto> GetStoredProcedureResultFields(string storeProcName, int? dataSourceREfisterId)
        {
            List<LookupItemDto> list = new List<LookupItemDto>();
            DataTable dt = new DataTable();

            string connetInfo = AppMetaDataBL.GetConnectInfo(dataSourceREfisterId);
            using (DataAccessAdapter aDataAccessAdapterWithDataSource = new DataAccessAdapter(connetInfo))
            {
                try
                {
                    List<SqlParameter> sqlParamterList = new List<SqlParameter>();
                    aDataAccessAdapterWithDataSource.CallRetrievalStoredProcedure(storeProcName, sqlParamterList.ToArray(), dt);

                    foreach (DataColumn pair in dt.Columns)
                    {
                        LookupItemDto aLookupItemDto = new LookupItemDto();
                        aLookupItemDto.Id = pair.ColumnName;
                        aLookupItemDto.Display = pair.DataType.Name;
                        list.Add(aLookupItemDto);
                    }

                }
                catch
                {

                }
            }

            return list;
        }


        public static string GetSelectStatement(string tableName, int? dataSourceRegisterId, string schemaOwner)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(tableName, dataSourceRegisterId, schemaOwner); ;
            SqlWriter sqlWriter = new SqlWriter(databaseTable, EmSqlType.SqlServer);

            return sqlWriter.SelectAllSql();

        }




        public static List<LookupItemDto> GetReadonlyTableTypeColumns(string tableTypeName, int? dataSourceRegisterId)
        {
            List<LookupItemDto> list = new List<LookupItemDto>();


            string querytableTypecolumns = string.Format(@"
                         SELECT name
                        FROM sys.columns
                        WHERE object_id IN (
                          SELECT type_table_object_id
                          FROM sys.table_types
                          WHERE name = '{0}'
                        )", tableTypeName);
            string conenctInfo = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId);
            using (DataAccessAdapter aDataAccessAdapterWithDataSource = new DataAccessAdapter(conenctInfo))
            {
                try
                {
                    List<SqlParameter> sqlParamterList = new List<SqlParameter>();
                    DataTable dt = aDataAccessAdapterWithDataSource.ExecuteDataTableRetrievalQuery(querytableTypecolumns, sqlParamterList);
                    foreach (DataRow dataRow in dt.Rows)
                    {
                        LookupItemDto aLookupItemDto = new LookupItemDto();
                        aLookupItemDto.Id = dataRow["name"].ToString();
                        aLookupItemDto.Display = dataRow["name"].ToString();
                        list.Add(aLookupItemDto);
                    }

                }
                catch
                {

                }
            }

            return list;
        }




        public static string GetSchemaOwnerByDataSourceRegId(int? dataSourceRegisterId)
        {
            if (!dataSourceRegisterId.HasValue)
            {
                dataSourceRegisterId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            }

            var dataBaseFixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceRegisterId.Value);
            string schemaOwner = dataBaseFixture.CurrentOwner;

            return schemaOwner;
        }





        public static List<string> SaasRestrictSystemTableNameList
        {
            get
            {
                List<string> toReturn = new List<string>();

                List<string> tableList = new List<string>() {

                    "ApErpSizeRange",
                    //"AppAccountGL",
                    //"AppAccountGLDetail",
                    //"AppAccountGLTxType",
                    //"AppAccountlChart",
                    //"AppAccountType",
                    //"AppAccountSubType",
                    "AppApplicationAssetsItem",
                    "AppApplicationImportSetting",
                    "AppBackupLog",
                    "AppBusienssAssormentNavigation",
                    "AppBusinessMgtScope",
                    //"AppBusinessPartner",
                    "AppBusinessPartnerInviteUser",
                    "AppBusinessPartnerInviteUserChildUser",
                    "AppBusinessScopeTag",
                    "AppCalendar",
                    "AppCalendarBaseDate",
                    "AppCalendarRecurringDay",
                    "AppCalendarSpecificDay",
                    "AppChartOfAccount",
                    "AppComOrganization",
                    "AppComOrgLevel",
                    "AppCompany",
                    "AppCompanyOrderModule",
                    "AppCompanyUserTypeRegister",
                    "AppConditionalAction",
                    //"AppCountry",
                    //"AppCurrency",
                    "AppCurrentUserFavouriteFolderOrFile",
                    "AppDatabaseDiagram",
                    "AppDatabaseDiagramItem",
                    "AppDataSet",
                    "AppDataSetParameter",
                    "AppDataSourceRegister",
                    "AppDateSetDataExtractView",
                    "AppDesktop",
                    "AppDesktopItem",
                    //"AppEmployee",
                    "AppEntityEnumValue",
                    "AppEntityInfo",
                    "AppEntitySimpleListValue",
                    "AppErpColor",
                    "AppErpSizeRangeDetail",
                    "AppEsite",
                    "AppEsiteCatalogue",
                    "AppESiteNavMenu",
                    "AppESitePages",
                    "AppEStore",
                    "AppExternalMethodRegister",
                    //"AppFile",
                    "AppFileOrFolderShareToOther",
                    "AppFileTypeView",
                    "AppForm",
                    "AppFormGridLayoutItemBindField",
                    "AppFormGroup",
                    "AppFormGroupItem",
                    "AppFormLayoutItem",
                    "AppFormLinkTarget",
                    "AppGeneralJournal",
                    "AppGeneralJournalDetail",
                    "AppIntergrationSetting",
                    "AppIntergrationSettingParameter",
                    "AppItemBase",
                    "AppLanguage",
                    "AppLanguageKey",
                    "AppListMenu",
                    "AppListMenuTemp",
                    "AppMessage",
                    "AppMessageDeleted",
                    "APPMessageNotificationSetting",
                    "AppMessageUserReceived",
                    "AppPorjectWorkFlowTaskTimeSheet",
                    //"AppProduct",
                    //"AppProductVariant",
                    //"AppProductWarehouseInventoryTrackLog",
                    //"AppProductWarehouseVariant",
                    "AppProjectOrTaskTranscation",
                    "AppProjectOrWorkFlow",
                    "AppProjectPerspectiveTask",
                    "AppProjectPerspectiveView",
                    "AppProjectPortfolio",
                    "AppProjectPortfolioBoard",
                    "AppProjectPrivacy",
                    "AppProjectPrivilegeLibrary",
                    "AppProjectRole",
                    "AppProjectRolePrivilege",
                    "AppProjectSnapshot",
                    "AppProjectTaskCheckList",
                    "AppProjectTaskExpense",
                    "AppProjectTaskPredecessor",
                    "AppProjectTaskResource",
                    "AppProjectTaskResourcePlannedHours",
                    "AppProjectTaskTag",
                    "AppProjectTaskTimeLog",
                    "AppProjectTeam",
                    "AppProjectTeamMember",
                    "AppProjectTeamMemberRole",
                    "AppProjectTemplateResource",
                    "AppProjectWorkFlowAction",
                    "AppProjectWorkFlowCondition",
                    "AppProjectWorkFlowTask",
                    "AppReport",
                    "AppRouteState",
                    "AppSearch",
                    "AppSearchField",
                    "AppSearchParameter",
                    "AppSearchSaved",
                    "AppSearchSavedValue",
                    "AppSearchView",
                    "AppSearchViewField",
                    "AppSearchViewReport",
                    "AppSearchViewReportParamterMapping",
                    "AppSecurityAuthticationInfo",
                    "AppSecurityEntityAction",
                    //"AppSecurityGroup",
                    "AppSecurityGroupMember",
                    "AppSecurityLoginAuditor",
                    "AppSecurityRegDomain",
                    "AppSecurityRegDomainListMenu",
                    "AppSecurityRegDomainORG",
                    "AppSecuritySysObjGroupUser",
                    "AppSecurityTransactionAction",
                    "AppSecurityTransactionActionResource",
                    //"AppSecurityUser",
                    "AppSecurityUserContact",
                    "AppSecurityUserInvitation",
                    "AppSecurityUserListMenu",
                    "AppSecurityUserRolePrevilege",
                    "AppSecurityUserSession",
                    "AppSEFolder",
                    "AppSEFolderResource",
                    "AppService",
                    "AppSetup",
                    "AppStorePages",
                    "AppSysLabelLanguage",
                    "AppTransaction",
                    "AppTransactionDataLoad",
                    "AppTransactionDataTransferSetting",
                    "AppTransactionDataTransferSettingDetail",
                    "AppTransactionField",
                    "AppTransactionFieldAggFunction",
                    //"AppTransactionGroup",
                    //"AppTransactionGroupItem",
                    "AppTransactionNavigation",
                    "AppTransactionPostProcess",
                    "AppTransactionSaveAsMapping",
                    "AppTransactionUnit",
                    "AppTransactionUnitDeleteFlow",
                    "AppTransactionUnitFormula",
                    "AppTransactionUnitLinkedSearch",
                    "AppTransactionUnitSearchFieldMapping",
                    "AppTransactionUnitSearchViewFieldMapping",
                    "AppTransAuditTrailLog",
                    "AppTranscationDataLoadFieldMapping",
                    "AppTranscationReport",
                    "AppTrasactionSnapShot",
                    "AppTrascationRecycleBin",
                    "AppUserAppointment",
                    "AppUserMessgeFollowup",
                    "AppUserSkill",
                    "AppUserSkillList",
                    "AppVersionEditionModule",
                    "AppViewFiledSearchFiledMapping",
                    //"AppWarehouse",
                    //"AppWarehouseInventory",
                    //"AppWarehouseLocation"

                    "AppModuleLibRegister",
                    "AppSecurityGroup",
                    "AppTimeZoneAbbreviation",
                    "AppTransactionGroup",
                    "AppTransactionGroupItem",
                    "AppTransactionGroupSession",
                    "AppTransactionItem",
                    "AppTransactionUnitJoin",
                    "AppTransactionUnitJoinSelectColumnMapping",
                    "AppViewLinkedSeaechOrUrl",
                    "AppWebApiConfig",
                    "AppWebAPIDataExchangeSetting",
                    "AppWebApiParamsHeaderSettig",
                    "AppWebApiProvider",
                    "AppWinScheduleSetting"
                };

                tableList.ForAll(o => toReturn.Add(o.ToLower()));

                return toReturn;
            }

        }

        public static string GetViewQueryText(string viewName)
        {
            int dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;

            string schemaOwner = "";

            string conneInfo = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId);
            if (viewName.IndexOf('.') == -1)
            {
                schemaOwner = AppMetaDataBL.GetSchemaOwnerByDataSourceRegId(dataSourceRegisterId);
                viewName = schemaOwner + "." + viewName;
            }

            string queryViewContexnt = $@" SELECT DEFINITION   FROM sys.sql_modules WHERE object_id = OBJECT_ID('{viewName}')";
            string queryResult;
            using (DataAccessAdapter adpater = new DataAccessAdapter(conneInfo))
            {
                queryResult = adpater.ExecuteScalarQuery(queryViewContexnt, new List<System.Data.SqlClient.SqlParameter>()) as string;

            }

            if (!string.IsNullOrWhiteSpace(queryResult))
            {
                //queryResult = queryResult.ToLowerInvariant();

                int indexSelect = queryResult.IndexOf("select", StringComparison.CurrentCultureIgnoreCase);
                queryResult = queryResult.Substring(indexSelect);
            }
            return queryResult;

        }


        public static TableDataDto SaveDatabaseViewFromDesignQuery(string viewName, string viewDesignQuery, bool isNewView, int? applicationId)
        {

            if (string.IsNullOrWhiteSpace(viewName))
            {
                TableDataDto errorMsgTableDataDto = new TableDataDto();
                errorMsgTableDataDto.ErrorMessage = "View name is empty";
                return errorMsgTableDataDto;
            }

            if (string.IsNullOrWhiteSpace(viewDesignQuery))
            {
                TableDataDto errorMsgTableDataDto = new TableDataDto();
                errorMsgTableDataDto.ErrorMessage = "View query is empty";
                return errorMsgTableDataDto;
            }

            int dataSourceRegisterId = ServerContext.Instance.CurrnetClientIdentity.DataSourceId;

            string schemaOwner = "";

            string conneInfo = AppMetaDataBL.GetConnectInfo(dataSourceRegisterId);
            string fullName = viewName;

            if (fullName.IndexOf('.') == -1)
            {
                schemaOwner = AppMetaDataBL.GetSchemaOwnerByDataSourceRegId(dataSourceRegisterId);
                fullName = schemaOwner + "." + viewName;
            }

            string queryToExecute = "";

            if (isNewView)
            {
                queryToExecute = $@"CREATE VIEW " + viewName + System.Environment.NewLine
                            + " AS " + System.Environment.NewLine
                            + viewDesignQuery;



            }
            else
            {
                queryToExecute = $@"ALTER VIEW " + fullName + System.Environment.NewLine
                            + " AS " + System.Environment.NewLine
                            + viewDesignQuery;
            }

            KeyValuePair<int?, string> dataSourceRegisterIdQuery = new KeyValuePair<int?, string>(dataSourceRegisterId, queryToExecute);

            var result = ExcuteQueryResult(dataSourceRegisterIdQuery);

            if (!result.IsHaveErrors)
            {
                if (applicationId.HasValue)
                {
                    Dictionary<string, int> dictDbObjNameAndUsageType = new Dictionary<string, int>();
                    dictDbObjNameAndUsageType.Add(viewName, (int)EmAppDataSetUsageType.DatabaseView);

                    AppDataSetBL.AddDatabaseObjectsToApplication(dictDbObjNameAndUsageType, applicationId.Value);
                }
            }


            return result;
        }


    }


}