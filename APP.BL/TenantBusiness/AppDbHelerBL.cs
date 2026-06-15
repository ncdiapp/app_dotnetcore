using System.Collections.Generic;
using System.Data;
using APP.LBL.DatabaseSpecific;
using System.Linq;

using System;
using DatabaseSchemaMrg.DataSchema;
using System.Data.SqlClient;
using DatabaseSchemaMrg;
////using APP.Persistence.Common;
using APP.Components.EntityDto;
using System.Data.Common;
using APP.Components.Dto;



namespace App.BL
{

    public class AppSqlCmdDto
    {
        public string CmdText
        {
            get;
            set;
        }

        public List<DbParameter> ListParamters
        {
            get;
            set;
        }
    }

    public static class AppDbHelerBL
    {
        // dictPrimaryKeyValue 
        public static DataRow RetriveOneDataRowWtihPrimayKeyFromDataBaseTable(Dictionary<string, object> dictOneToOneWithPKValue, AppTransactionUnitExDto AppTransactionUnitExDto)
        {
            DataRow mainDataRow = null;

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(AppTransactionUnitExDto.DataSourceFrom.Value);

           

            List<DbParameter> sqlParamters = new List<DbParameter>();

            DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(AppTransactionUnitExDto);
            //  DatabaseTable rootDatabaseTable = null;
            SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

            //List<SqlParameter> listParamters = new List<SqlParameter>();

            string rootSelectall = sqlWriter.SelectByIdSql();

            foreach (var column in sqlWriter.PrimaryKeys)
            {

                DbParameter parameter = databaseFixtureInstance.CreateParameter(column.Replace(" ", ""));
                parameter.Value = dictOneToOneWithPKValue[column];
                sqlParamters.Add(parameter);
            }


            DataTable dtResult = databaseFixtureInstance.RetriveDataTable(rootSelectall, sqlParamters);
            if (dtResult != null && dtResult.Rows.Count > 0)
            {
                mainDataRow = dtResult.Rows[0];

                if (! AppTransactionUnitExDto.DictExtendDbField_Id.IsEmpty())
                {
                    PorcessExtendColumnValue(AppTransactionUnitExDto, mainDataRow, databaseFixtureInstance, sqlParamters);
                }



            }



            return mainDataRow;
        }

        public static DataRow RetriveOneDataRowWtihPrimayKeyFromSQLQury(Dictionary<string, object> dictOneToOneWithPKValue, AppTransactionUnitExDto transactionUnitExDto)
        {
            DataRow mainDataRow = null;

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(transactionUnitExDto.DataSourceFrom.Value);



            List<DbParameter> sqlParamters = new List<DbParameter>();

            //string rootSelectall = "";

            //    //TraceLevel: Enum: off-0 Error-1 Warning-2  Info-3  Verbose-4   : Output all debugging and tracing messages.
            //string aQuery = SqlQuery.SELECT + includingField + SqlQuery.FROM + " ( " + orgQuery + " )  as  DynTable " + whereClasue;


            string includingField = transactionUnitExDto.AppTransactionFieldList.Where(o => !string.IsNullOrEmpty(o.DataBaseFieldName))
                .Select(o => o.DataBaseFieldName).Aggregate((i, j) => i + "," + j);

            List<string> inputPara = transactionUnitExDto.AppTransactionFieldList.Where(o => o.IsPrimaryKey)
                                 .Select(o => o.DataBaseFieldName).ToList();

            string wheClause = "";
            foreach( string pkName in inputPara)
            {
               string kp= $@"{pkName}=@{pkName}";

                wheClause = wheClause + kp + " AND ";
            }

            wheClause = wheClause.Substring(0, wheClause.Length - " AND ".Length);

         
           List<SqlParameter> listParamters = new List<SqlParameter>();


            foreach (string column in inputPara)
            {

                DbParameter parameter = databaseFixtureInstance.CreateParameter(column.Replace(" ", ""));
                parameter.Value = dictOneToOneWithPKValue[column];
                sqlParamters.Add(parameter);
            }

            string aQuery = $@" SELECT {includingField} FROM ({transactionUnitExDto.DataSourceQuery}) as DynTable WHERE  {wheClause}";

            DataTable dtResult = databaseFixtureInstance.RetriveDataTable(aQuery, sqlParamters);
            if (dtResult != null && dtResult.Rows.Count > 0)
            {
                mainDataRow = dtResult.Rows[0];

                if (!transactionUnitExDto.DictExtendDbField_Id.IsEmpty())
                {
                    PorcessExtendColumnValue(transactionUnitExDto, mainDataRow, databaseFixtureInstance, sqlParamters);
                }



            }



            return mainDataRow;
        }

        private static void PorcessExtendColumnValue(AppTransactionUnitExDto appTransactionUnitExDto, DataRow mainDataRow, DatabaseFixture databaseFixtureInstance, List<DbParameter> sqlParamters)
        {
            DataTable dtResult = mainDataRow.Table;
            foreach (string exdbFieldName in appTransactionUnitExDto.DictExtendDbField_Id.Keys)
            {
                dtResult.Columns.Add(exdbFieldName);
            }

            DataTable dtExtendResult = GetExtendFiledValue(appTransactionUnitExDto, databaseFixtureInstance, sqlParamters[0].Value);

            if (dtExtendResult != null && dtExtendResult.Rows.Count > 0)
            {
                foreach (DataRow exRow in dtExtendResult.Rows)
                {
                    int filedId = (int)exRow["UnitExtendFiledID"];
                    object value = exRow["ValueText"];

                    string fileName = appTransactionUnitExDto.DictExtenId_DbField[filedId];

                    int controlType = appTransactionUnitExDto.DictTransactionFieldIdControlType[filedId];

                    mainDataRow[fileName] = ControlTypeValueConverter.ConvertValueToObject(value, controlType);
                }


            }
        }

        private static DataTable GetExtendFiledValue(AppTransactionUnitExDto appTransactionUnitExDto, DatabaseFixture databaseFixtureInstance,object pkValue)
        {
           
            List<DbParameter> sqlExtendParamters = new List<DbParameter>();


            DbParameter unitIdParameter = databaseFixtureInstance.CreateParameter("TransactionUnitID");
            unitIdParameter.Value = appTransactionUnitExDto.Id;
            sqlExtendParamters.Add(unitIdParameter);

            // need to get first value 
            DbParameter unitPKValueParameter = databaseFixtureInstance.CreateParameter("UnitPKValue");
            unitPKValueParameter.Value = pkValue;
            sqlExtendParamters.Add(unitPKValueParameter);

            string rootExtendSelectall = $@"  select distinct  ValueText,UnitExtendFiledID  from AppTransactionUnitExtendFieldValue
            WHERE TransactionUnitID ={unitIdParameter.ParameterName} AND UnitPKValue={unitPKValueParameter.ParameterName} ";



            DataTable dtResult2 = databaseFixtureInstance.RetriveDataTable(rootExtendSelectall, sqlExtendParamters);
            return dtResult2;
        }



        public static Dictionary<string, object> RetriveOneToOneWithPrimaryKey(Dictionary<string, object> dictOneToOneWithPKValue, AppTransactionUnitExDto appTransactionUnitExDto)
        {
            Dictionary<string, object> toRetrun = new Dictionary<string, object>();
            DataRow dr = RetriveOneDataRowWtihPrimayKeyFromDataBaseTable(dictOneToOneWithPKValue, appTransactionUnitExDto);
            if (dr != null)
                toRetrun = ConvertOneDataRowToOneToOneDict(dr);
            return toRetrun;
        }

        public static Dictionary<string, object> ConvertOneDataRowToOneToOneDict(DataRow dr)
        {
            Dictionary<string, object> toRetrun = new Dictionary<string, object>();

            foreach (DataColumn col in dr.Table.Columns)
            {

                object value = dr[col];
                if (value == DBNull.Value)
                {
                    value = null;
                }


                //if (value != null && dictFiledControlname.ContainsKey(col.ColumnName))
                //{
                //	int controTyle = dictFiledControlname[col.ColumnName];
                //	if (controTyle == (int)EmAppControlType.Date || controTyle == (int)EmAppControlType.DateTimeDetail)
                //	{
                //		DateTime? dateTime = ControlTypeValueConverter.ConvertValueToDate(value);

                //		if (dateTime.HasValue)
                //		{
                //			value = ClientTimeZoneHelper.ConvertUTCToClientDateTime(dateTime);
                //		}

                //	}
                //}

                toRetrun.Add(col.ColumnName, value);

            }



            return toRetrun;
        }

        // 
        public static AppSqlCmdDto GetOneToOneInsertSqlCmdDto(Dictionary<string, object> dictOneToOneFields, string tableName, int? dataSourceId, string schemaOwner)
        {
            AppSqlCmdDto toReturn = new AppSqlCmdDto();

            DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(tableName, dataSourceId, schemaOwner);

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId.Value);


            //  DatabaseTable rootDatabaseTable = null;
            SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

            var insert = sqlWriter.InsertSqlWithoutOutputParameter();


            List<DbParameter> sqlParamters = new List<DbParameter>();


            string[] needToinsetColumns = databaseTable.GetColumnsExcludeAutoTimeStampAndComputed();
            foreach (var columnName in needToinsetColumns)
            {
                // need to get default value from transaction units
                object value = DBNull.Value;

                if (dictOneToOneFields.ContainsKey(columnName))
                {
                    value = (object)(dictOneToOneFields[columnName]) ?? DBNull.Value;

                }

                DbParameter parameter = databaseFixtureInstance.CreateParameter(columnName.Replace(" ", ""));

                SetDbParamterValueAndType(databaseTable, columnName, value, parameter, databaseFixtureInstance);

                sqlParamters.Add(parameter);


            }
            toReturn.CmdText = insert;
            toReturn.ListParamters = sqlParamters;

            return toReturn;

        }

        public static AppSqlCmdDto GetOneToOneUpdateWithPrimaryKeyValueSqlCmdDto(Dictionary<string, object> unitOneToOneFields, AppTransactionUnitExDto appTransactionUnitExDto)
        {


            //string tableName, int? dataBaseRegisterId

            AppSqlCmdDto toReturn = new AppSqlCmdDto();
            DatabaseTable databaseTable = AppCacheManagerBL.GetDatabaseTable(appTransactionUnitExDto);

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(appTransactionUnitExDto.DataSourceFrom.Value);
            List<DbParameter> sqlParamters = new List<DbParameter>();

            SqlWriter sqlWriter = new SqlWriter(databaseTable, databaseFixtureInstance.SqlServerType.Value);

            sqlWriter.ExcluedColumnUpdate.Add(EmSystemDbTrackField.AppCreatedDate.ToString());



            Dictionary<string, object> dictPkValue = new Dictionary<string, object>();
            foreach (var column in sqlWriter.PrimaryKeys)
            {
                if (unitOneToOneFields.ContainsKey(column))
                {
                    dictPkValue[column] = unitOneToOneFields[column];

                }

            }

            if (dictPkValue.IsEmpty())
            {
                return toReturn;
            }
            else
            {
                Dictionary<string, object> dictAllFileds = new Dictionary<string, object>();

                dictAllFileds = RetriveOneToOneWithPrimaryKey(dictPkValue, appTransactionUnitExDto);



                foreach (string columnName in unitOneToOneFields.Keys)
                {
                    if (dictAllFileds.ContainsKey(columnName))
                    {
                        dictAllFileds[columnName] = unitOneToOneFields[columnName];
                    }
                }

                unitOneToOneFields = dictAllFileds;
            }



            string updateStatment = sqlWriter.UpdateWithOutConcurrencySql();
            //  sqlWriter.u



            //   string[] needToinsetColumns = unitDatabaseTable.GetColumnsExcludeAutoTimeStampAndComputed();
            foreach (var column in sqlWriter.NonPrimaryKeyColumns)
            {
                if (unitOneToOneFields.ContainsKey(column))
                {
                    object value = (object)(unitOneToOneFields[column]) ?? DBNull.Value;

                    DbParameter parameter = databaseFixtureInstance.CreateParameter(column.Replace(" ", ""));
                    if (value is System.UInt16)
                    {
                        parameter.DbType = DbType.UInt16;

                    }
                    else if (value is System.UInt32)
                    {
                        parameter.DbType = DbType.UInt32;
                    }
                    else if (value is System.UInt64)
                    {
                        parameter.DbType = DbType.UInt64;
                    }

                    // need to test,if all strong type can convert to  string type
                    SetDbParamterValueAndType(databaseTable, column, value, parameter, databaseFixtureInstance);

                    sqlParamters.Add(parameter);
                }


            }

            foreach (var column in sqlWriter.PrimaryKeys)
            {
                object value = (object)(unitOneToOneFields[column]) ?? DBNull.Value;

                DbParameter parameter = databaseFixtureInstance.CreateParameter(column.Replace(" ", ""));
                SetDbParamterValueAndType(databaseTable, column, value, parameter, databaseFixtureInstance);

                sqlParamters.Add(parameter);
            }

            toReturn.CmdText = updateStatment;
            toReturn.ListParamters = sqlParamters;

            return toReturn;

        }

        internal static void SetDbParamterValueAndType(DatabaseTable databaseTable, string column, object value, DbParameter sqlParameter, DatabaseFixture databseFixture)
        {
            //	var sqlParameter = new SqlParameter("@" + column, value);

            sqlParameter.Value = value;
            DatabaseColumn databaseColumn = databaseTable.DictDataBaseColumn[column];

            if (value == null || value == DBNull.Value)
            {
                ProcessNullDefaultValue(sqlParameter, databseFixture, databaseColumn);

            }

            else // VALUE IS NOT NULL
            {
                if (databaseColumn.DbDataType.ContainsExt("CHAR", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (databaseColumn.Length.HasValue && databaseColumn.Length.Value != -1)
                    {
                        string vlaueString = value.ToString();

                        if (vlaueString.Length >= databaseColumn.Length)
                        {
                            sqlParameter.Value = vlaueString.Substring(0, databaseColumn.Length.Value);
                        }

                    }

                }

                else if (databaseColumn.DbDataType.ToLower()=="date")
                {
                    
                    var dateValue = ControlTypeValueConverter.ConvertValueToDate(value);

                    if (dateValue.HasValue && dateValue.Value < new DateTime(1753, 1, 1))
                    {
                        sqlParameter.DbType = DbType.String;
                        sqlParameter.Value = dateValue.Value.Date.ToString("yyyy-MM-dd");
                    }                


                } //System.Data.SqlTypes.SqlTypeException: SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.
                else if (databaseColumn.DbDataType.ToLower() == "datetime")
                {
                    var dateValue = ControlTypeValueConverter.ConvertValueToDate(value);
                    sqlParameter.DbType = DbType.DateTime;

                    if (dateValue.HasValue && dateValue.Value < new DateTime(1753,1,1))
                    {
                        sqlParameter.Value = DBNull.Value;

                    }

                    


                }

            }


            // for all database type
            if (databaseColumn.DbDataType == "image")
            {
                sqlParameter.DbType = DbType.Binary;
            }


        }

        private static void ProcessNullDefaultValue(DbParameter sqlParameter, DatabaseFixture databseFixture, DatabaseColumn databaseColumn)
        {
            if (!string.IsNullOrEmpty(databaseColumn.DefaultValue))
            {

                string defaultValue = databaseColumn.DefaultValue;
                object finalDefaultValue = databaseColumn.DefaultValue;

                if (databseFixture.SqlServerType == EmSqlType.SqlServer)
                {


                    if (databaseColumn.DbDataType.ContainsExt("CHAR", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (defaultValue.StartsWith("(N'") && defaultValue.EndsWith("')"))
                        {
                            finalDefaultValue = defaultValue.Substring("(N'".Length, defaultValue.Length - "(N'".Length - "')".Length);
                        }

                    }
                    else
                    {
                        finalDefaultValue = defaultValue.Replace("(", "").Replace(")", "");

                    }


                }
                else if (databseFixture.SqlServerType == EmSqlType.MySql)
                {

                    if (databaseColumn.DbDataType.ContainsExt("bit", StringComparison.InvariantCultureIgnoreCase))
                    {

                        finalDefaultValue = defaultValue.Replace("b'", "").Replace("B'", "");
                        sqlParameter.DbType = DbType.SByte;

                    }
                    else if (databaseColumn.DbDataType.ContainsExt("date", StringComparison.InvariantCultureIgnoreCase))
                    {
                        DateTime outValue;

                        if (DateTime.TryParse(defaultValue, out outValue))
                        {
                            finalDefaultValue = outValue;
                        }

                    }
                }

                sqlParameter.Value = finalDefaultValue;
            }

            if (databaseColumn.SystemToken.HasValue)
            {
                EmBLFiledMappingSystemTokenField emToken = (EmBLFiledMappingSystemTokenField)databaseColumn.SystemToken.Value;

                object tokeValue = AppTransDataSystemTokenBL.GetSystemTokenValue(null, emToken);
                sqlParameter.Value = tokeValue;

            }
            else if (!string.IsNullOrEmpty(databaseColumn.OverrideDefaultValue))
            {
                sqlParameter.Value = databaseColumn.OverrideDefaultValue;

            }
        }

        public static void DeleteChildWithLinkToParentKeyFueld(AppTransactionUnitExDto transactionUnitExDto, string linkParentKeyFiledName, object linkParentKeyValue, DbTransaction trans)
        {
            //List<SqlParameter> listParamters = new List<SqlParameter>();
            //string paraName = "@" + linkParentKeyFiledName;
            //string deletecildstatment = "  DELETE FROM [" + tableName + "] WHERE " + linkParentKeyFiledName + "=" + paraName;

            //listParamters.Add(new SqlParameter(paraName, linkParentKeyValue));

            //passAdpater.ExecuteScalarQuery(deletecildstatment, listParamters);



            string connectionString = trans.Connection.ConnectionString;

            DatabaseFixture databaseFixtureInstance = AppCacheManagerBL.GetOneDatabaseFixture(transactionUnitExDto.DataSourceFrom.Value);

            DbParameter parameter = databaseFixtureInstance.CreateParameter(linkParentKeyFiledName.Replace(" ", ""));
            parameter.Value = linkParentKeyValue;

            string tableQulifiedName = AppMetaDataBL.GetQulifiedTableName(transactionUnitExDto.SchemaOwner, transactionUnitExDto.DataBaseTableName, databaseFixtureInstance.SqlServerType.Value);

            string deletecildstatment = "  DELETE FROM " + tableQulifiedName + " WHERE " + linkParentKeyFiledName + "=" + parameter.ParameterName;

            List<DbParameter> listParamters = new List<DbParameter>();

            listParamters.Add(parameter);


            databaseFixtureInstance.ExecuteTransScalar(deletecildstatment, listParamters, trans);




        }





    }
}