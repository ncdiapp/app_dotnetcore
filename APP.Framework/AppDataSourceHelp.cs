using APP.Persistence.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace APP.Framework
{
   


public static class AppDataSourceHelp
{

    public static readonly string QueryDatabaseTableMetaData = @"SELECT 
				c.name 'ColumnName',
				t.Name 'Datatype',
				c.max_length 'MaxLength',
				c.precision ,
				c.scale ,
				c.is_nullable,
				ISNULL(i.is_primary_key, 0) 'Primary Key'
			FROM    
				sys.columns c
			INNER JOIN 
				sys.types t ON c.user_type_id = t.user_type_id
			LEFT OUTER JOIN 
				sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
			LEFT OUTER JOIN 
				sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
			WHERE
				c.object_id = OBJECT_ID('[{0}]')";
    public static void UpdateFkTableValueKey(DataTable sourceDatabTable, string FkTablename, Dictionary<string, string> dictFkTable_CurrentTableKeyMapping, KeyValuePair<string, string> FkTablePK_CurrentTableFk, string ExChangeDbconneinfo)
    {
        Dictionary<string, object> dictExternalKeyPkValueList = GetExternalKeyPKMapping(FkTablePK_CurrentTableFk.Key, dictFkTable_CurrentTableKeyMapping.Keys.ToList(), FkTablename, ExChangeDbconneinfo);



        var targetDbColumn = sourceDatabTable.Columns.Cast<DataColumn>().FirstOrDefault(o => o.ColumnName.ToLower() == FkTablePK_CurrentTableFk.Value.ToLower());
        if (targetDbColumn == null)
        {
            sourceDatabTable.Columns.Add(FkTablePK_CurrentTableFk.Value);
        }
        else
        {

            targetDbColumn.ColumnName = FkTablePK_CurrentTableFk.Value;
        }


        foreach (DataRow row in sourceDatabTable.Rows)
        {
            string combValue = string.Empty;

            foreach (string fkCoumn in dictFkTable_CurrentTableKeyMapping.Values)
            {
                combValue = combValue + row[fkCoumn].ToString().Trim() + DataSoureHelp.StringSplitToken;
            }

            combValue = combValue.Substring(0, combValue.Length - 1);

            if (dictExternalKeyPkValueList.ContainsKey(combValue))
            {
                // need to add PK value ????
                row[FkTablePK_CurrentTableFk.Value] = dictExternalKeyPkValueList[combValue];

            }


        }
        // return FkTablePK_CurrentTableFk;
    }
    /// <summary>
    ///  make sure all Data type keep the same( for decimal 2.0  to string will be different from  decimal 3) ...
    /// For big data set update, must set  TargetTablewhereClause where clause 
    /// </summary>
    /// <param name="distinctSourceDatabTable"></param>
    /// <param name="targetPkColumn"></param>
    /// <param name="mutipleOrignalSourceKeyColumnList"></param>
    /// <param name="TargetTableName"></param>
    /// <param name="SourceExCludeColumns"></param>
    /// <param name="otherOverideConnectionString"></param>
    public static void ProcessMutileKeyTable(DataTable distinctSourceDatabTable, string targetPkColumn, List<string> mutipleOrignalSourceKeyColumnList, string TargetTableName, List<string> SourceExCludeColumns,
        string otherOverideConnectionString, string TargetTablewhereClause = "")
    {

        //  string localConn = otherOverideConnectionString;
        //GetExternalKeyPKMapping

        if (distinctSourceDatabTable.Rows.Count > 0)
        {

            distinctSourceDatabTable = distinctSourceDatabTable.AsEnumerable().GroupBy(o =>
            {

                string externalCombinekeyValue = string.Empty;
                foreach (string exKeyColumName in mutipleOrignalSourceKeyColumnList)
                {
                    //RTRIM(LTRIM(@String1))
                    externalCombinekeyValue = o[exKeyColumName].ToString() + "_" + externalCombinekeyValue;
                }
                if (externalCombinekeyValue != String.Empty)
                {
                    externalCombinekeyValue = externalCombinekeyValue.Substring(0, externalCombinekeyValue.Length - 1);
                }
                return externalCombinekeyValue;

            }

                )
                .Select(g => g.Last()).CopyToDataTable();

        }




        List<DataRow> newSourceRowList;
        List<DataRow> updateSourceRowList;
        GetNewRowsAndUpdateRows(distinctSourceDatabTable, targetPkColumn, mutipleOrignalSourceKeyColumnList, TargetTableName, SourceExCludeColumns, otherOverideConnectionString, out newSourceRowList, out updateSourceRowList, TargetTablewhereClause);


        // need to get the orgial schema



        if (newSourceRowList.Count > 0)
        {
            var insertStamentList = GenerateDataRowInsertStatement(TargetTableName, newSourceRowList);

            ExcuteBatchSqlStatment(otherOverideConnectionString, insertStamentList);

        }


        if (updateSourceRowList.Count > 0)
        {
            List<string> updateList = GenerateDataRowUpdateStatment(mutipleOrignalSourceKeyColumnList, TargetTableName, updateSourceRowList);

            ExcuteBatchSqlStatment(otherOverideConnectionString, updateList);
        }


    }

    private static void RemoveDataTableRedudantColumnBasedPhysicalDatabaseTable(DataTable datatable, string databaseTableName, string strConnString)
    {
        using (SqlConnection conn = new SqlConnection(strConnString))
        {
            conn.Open();


        }
    }

    public static List<string> GenerateDataRowInsertStatement(string TargetTableName, List<DataRow> newSourceRowList)
    {
        if (newSourceRowList == null || newSourceRowList.Count == 0)
        {
            return new List<string>();
        }

        DataTable newSourceDatatable = newSourceRowList.CopyToDataTable();

        var InsertColumnName = newSourceDatatable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();

        var insertStamentList = SqlScriptGenerator.GenerateSqlInsertsList(InsertColumnName, newSourceDatatable, TargetTableName);

        return insertStamentList;
    }

    public static List<string> GenerateDataRowUpdateStatment(List<string> mutipleOrignalSourceKeyColumnList, string TargetTableName, List<DataRow> updateSourceRowList)
    {
        if (updateSourceRowList == null || updateSourceRowList.Count == 0)
        {
            return new List<string>();
        }

        DataTable updateDatatable = updateSourceRowList.CopyToDataTable();

        var updateColumnName = updateDatatable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();
        List<string> updateWhereColumns = new List<string>();
        updateWhereColumns.AddRange(mutipleOrignalSourceKeyColumnList);

        List<string> updateList = SqlScriptGenerator.GenerateSqlUpdatesList(updateColumnName, updateWhereColumns, updateDatatable, TargetTableName);
        return updateList;
    }

    public static void GetNewRowsAndUpdateRows(DataTable sourceDatabTable, string targetPkColumn, List<string> mutipleOrignalSourceKeyColumnList, string TargetTableName, List<string> SourceExCludeColumns, string otherOverideConnectionString, out List<DataRow> newSourceRowList, out List<DataRow> updateSourceRowList, string TargetTablewhereClause = "")
    {
        foreach (string exColumn in SourceExCludeColumns)
        {
            var exDatColumn = sourceDatabTable.Columns.Cast<DataColumn>().FirstOrDefault(o => o.ColumnName == exColumn);
            if (exDatColumn != null)
            {
                sourceDatabTable.Columns.Remove(exColumn);
            }


        }


        Dictionary<string, object> dictExternalKeyPkValueList = GetExternalKeyPKMapping(targetPkColumn, mutipleOrignalSourceKeyColumnList, TargetTableName, otherOverideConnectionString, TargetTablewhereClause);

        newSourceRowList = new List<DataRow>();

        updateSourceRowList = new List<DataRow>();


        List<string> srcKeyValueList = new List<string>();

        foreach (DataRow row in sourceDatabTable.Rows)
        {
            string combValue = string.Empty;

            foreach (string fkCoumn in mutipleOrignalSourceKeyColumnList)
            {
                combValue = combValue + row[fkCoumn].ToString().Trim() + DataSoureHelp.StringSplitToken;
            }

            combValue = combValue.Substring(0, combValue.Length - 1);

            if (dictExternalKeyPkValueList.ContainsKey(combValue))
            {
                // need to add PK value ????
                updateSourceRowList.Add(row);

            }
            else
            {
                newSourceRowList.Add(row);

            }


        }
    }


    public static DataTable JoinDatable(DataTable dataTable1, DataTable dataTable2, string joinTable1ColName, string joinTable2ColName)
    {

        DataTable targetTable = dataTable1.Clone();
        var dt2Columns = dataTable2.Columns.OfType<DataColumn>().Select(dc =>
            new DataColumn(dc.ColumnName, dc.DataType, dc.Expression, dc.ColumnMapping));
        targetTable.Columns.AddRange(dt2Columns.ToArray());
        var rowData =
            from row1 in dataTable1.AsEnumerable()
            join row2 in dataTable2.AsEnumerable()
                on row1.Field<int>(joinTable1ColName) equals row2.Field<int>("joinTable2ColName")
            select row1.ItemArray.Concat(row2.ItemArray).ToArray();
        foreach (object[] values in rowData)
            targetTable.Rows.Add(values);

        return targetTable;

    }

    public static string ExcuteBatchSqlStatment(string strConnString, List<string> insertStamentList)
    {
        using (SqlConnection conn = new SqlConnection(strConnString))
        {
            conn.Open();


            foreach (string insertString in insertStamentList)
            {

                // need to write Error !!

                //try
                //{
                using (SqlCommand cmd = new SqlCommand(insertString, conn))
                {
                    cmd.ExecuteNonQuery();

                }
                //}
                //catch (Exception ex)
                //{
                // Appl
                //ApplicationLog.WriteError(ex.Message);

                //return ex.Message;

                //}

            }

        }

        return string.Empty;
    }

    public static string ExcuteBatchSqlStatment(string strConnString, string sqlStamentList, IsolationLevel transcationlevel)
    {

        List<string> insertStamentList = new List<string>();
        insertStamentList.Add(sqlStamentList);
        return ExcuteBatchSqlStatment(strConnString, insertStamentList, transcationlevel);
        //  return 0;
    }

    public static string ExcuteBatchSqlStatment(string strConnString, KeyValuePair<string, List<SqlParameter>> sqlStamentListWithParameters, IsolationLevel transcationlevel)
    {

        List<KeyValuePair<string, List<SqlParameter>>> listpair = new List<KeyValuePair<string, List<SqlParameter>>>();
        listpair.Add(sqlStamentListWithParameters);
        return ExcuteBatchSqlStatment(strConnString, listpair, transcationlevel);

    }

    public static string ExcuteBatchSqlStatment(string strConnString, List<KeyValuePair<string, List<SqlParameter>>> sqlStamentListWithParameters, IsolationLevel transcationlevel)
    {
        // SqlTransaction objTrans = null;
        using (SqlConnection conn = new SqlConnection(strConnString))
        {
            string currentSQl = string.Empty;

            conn.Open();
            SqlTransaction trans = conn.BeginTransaction(transcationlevel);

            try
            {
                foreach (KeyValuePair<string, List<SqlParameter>> pairSqlstatementString in sqlStamentListWithParameters)
                {
                    string sqlstatementString = pairSqlstatementString.Key;

                    currentSQl = sqlstatementString;
                    SqlCommand cmd = new SqlCommand(sqlstatementString, conn, trans);

                    cmd.Parameters.AddRange(pairSqlstatementString.Value.ToArray());

                    cmd.ExecuteNonQuery();

                }

                trans.Commit();
                return string.Empty;
            }
            catch (Exception ex)
            {

                trans.Rollback();
              

                return ex.Message;
            }

        }

        //  return 0;
    }



    // return 0 if transcation is succesfuly 
    public static string ExcuteBatchSqlStatment(string strConnString, List<string> sqlStamentList, IsolationLevel transcationlevel)
    {

        // SqlTransaction objTrans = null;
        using (SqlConnection conn = new SqlConnection(strConnString))
        {
            string currentSQl = string.Empty;

            conn.Open();
            SqlTransaction trans = conn.BeginTransaction(transcationlevel);

            try
            {
                foreach (string sqlstatementString in sqlStamentList)
                {
                    currentSQl = sqlstatementString;
                    SqlCommand cmd = new SqlCommand(sqlstatementString, conn, trans);
                    cmd.ExecuteNonQuery();

                }

                trans.Commit();
                return "";
            }
            catch (Exception ex)
            {

                trans.Rollback();
                //ApplicationLog.WriteError("Error " + ex.Message + " at " + currentSQl);

                return ex.Message;
            }

        }

        //  return 0;
    }

    //    public static void ProcessMutileKeyTable(DataTable sourceDatabTable, string pkColumn, List<string> mutipleOrignalSourceKeyColumnList, string TargetTableName, List<string> SourceExCludeColumns, string otherOverideConnectionString = null)

    public static void ProcessOneKeyTable(DataTable sourceDatabTable, string SourcePkColumn, string TargetExternalkeycolumn, string TargetTableName, List<string> SourceExCludeColumns, string otherOverideConnectionString)
    {


        string localConn = otherOverideConnectionString;





        foreach (string exColumn in SourceExCludeColumns)
        {
            var exDatColumn = sourceDatabTable.Columns.Cast<DataColumn>().FirstOrDefault(o => o.ColumnName == exColumn);
            if (exDatColumn != null)
            {
                sourceDatabTable.Columns.Remove(exColumn);
            }


        }

        // need to add TargetExternalkeycolumn if not exsting

        var exDatTargetExternalkeycolumn = sourceDatabTable.Columns.Cast<DataColumn>().FirstOrDefault(o => o.ColumnName == TargetExternalkeycolumn);
        if (exDatTargetExternalkeycolumn == null)
        {
            sourceDatabTable.Columns.Add(TargetExternalkeycolumn);
        }

        //
        if (TargetExternalkeycolumn != SourcePkColumn)
        {
            foreach (DataRow row in sourceDatabTable.Rows)
            {
                row[TargetExternalkeycolumn] = row[SourcePkColumn].ToString();
            }

            sourceDatabTable.Columns.Remove(SourcePkColumn);


        }




        // do database validation , null or mandatory 
        KeyValuePair<EmValidationResult, string> validationresult = DataSoureHelp.ValidateDataTableAgainstDbTable(sourceDatabTable, TargetTableName, localConn);
        //toReturnList.Add(validationresult);

        if (validationresult.Key != EmValidationResult.AllOk)
        {

           

            return;

        }



        var sourcePKeyValueList = sourceDatabTable.AsEnumerable().Select(o => o[TargetExternalkeycolumn].ToString()).ToList();
        // Target Table


        var targetExternalKeyValueList = new DBInteractionBase(localConn).RetriveDataTable("Select " + TargetExternalkeycolumn + " from  " + TargetTableName).AsEnumerable().Select(o => o[0].ToString()).ToList();
        // new rows

        var newRowsSourceKey = sourcePKeyValueList.Except(targetExternalKeyValueList).ToList();

        var updateRowsSourKey = sourcePKeyValueList.Intersect(targetExternalKeyValueList).ToList(); ;


        if (newRowsSourceKey.Count > 0)
        {
            DataTable newSourceDatatable = sourceDatabTable.AsEnumerable().Where(o => newRowsSourceKey.Contains(o[TargetExternalkeycolumn].ToString())).CopyToDataTable();
            var InsertColumnName = newSourceDatatable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();

            var insertStamentList = SqlScriptGenerator.GenerateSqlInsertsList(InsertColumnName, newSourceDatatable, TargetTableName, localConn);

            using (SqlConnection conn = new SqlConnection(localConn))
            {
                conn.Open();

                foreach (string insertString in insertStamentList)
                {

                    // need to write Error !!

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(insertString, conn))
                        {
                            cmd.ExecuteNonQuery();

                        }
                    }
                    catch (Exception ex)
                    {
                        // Appl
                       // ApplicationLog.WriteError(ex.Message);

                    }

                }


            }

            // new DBInteractionBase().ExecuteNonQuery(insertStament);
        }




        // update where clause
        if (updateRowsSourKey.Count > 0)
        {
            DataTable updateDatatable = sourceDatabTable.AsEnumerable().Where(o => updateRowsSourKey.Contains(o[TargetExternalkeycolumn].ToString())).CopyToDataTable();
            var updateColumnName = updateDatatable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();
            List<string> updateWhereColumns = new List<string>();
            updateWhereColumns.Add(TargetExternalkeycolumn);

            List<string> updateList = SqlScriptGenerator.GenerateSqlUpdatesList(updateColumnName, updateWhereColumns, updateDatatable, TargetTableName, localConn);


            using (SqlConnection conn = new SqlConnection(localConn))
            {
                conn.Open();

                foreach (string updateString in updateList)
                {

                    // need to write Error !!

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(updateString, conn))
                        {
                            cmd.ExecuteNonQuery();

                        }
                    }
                    catch (Exception ex)
                    {
                        // Appl
                        //ApplicationLog.WriteError(ex.Message);

                    }

                }

            }


            //new DBInteractionBase(localConne).ExecuteNonQuery(updateStament);

        }

    }


    public static void ConvertColumnDataType(DataTable sourceDtable, List<string> needToConvertDataColumn, Func<object, object> action)
    {
        Dictionary<string, string> dictOrgColumn = new Dictionary<string, string>();

        foreach (String orgDataCol in needToConvertDataColumn)
        {
            string tempColumn = orgDataCol + "Temp";
            dictOrgColumn[orgDataCol] = tempColumn;
            sourceDtable.Columns.Add(tempColumn);

        }

        foreach (DataRow row in sourceDtable.Rows)
        {
            foreach (String orgDataCol in needToConvertDataColumn)
            {
                // row[dictOrgColumn[julianDateCol]] = ToJulianDate(date);

                row[dictOrgColumn[orgDataCol]] = action(row[orgDataCol]);

            }
        }

        foreach (String orgDataCol in needToConvertDataColumn)
        {

            sourceDtable.Columns.Remove(orgDataCol);

            sourceDtable.Columns[dictOrgColumn[orgDataCol]].ColumnName = orgDataCol;

        }
    }

    public static Dictionary<string, object> GetExternalKeyPKMapping(string pkColumn, String oneOrignalSourceKeyColumn, string tableName, string otherConnectionString, string TargetTablewhereClause = "")
    {

        List<string> mutipleOrignalSourceKeyColumnList = new List<string>();
        mutipleOrignalSourceKeyColumnList.Add(oneOrignalSourceKeyColumn);

        return GetExternalKeyPKMapping(pkColumn, mutipleOrignalSourceKeyColumnList, tableName, otherConnectionString, TargetTablewhereClause);

    }

    public static Dictionary<string, object> GetExternalKeyPKMapping(string pkColumn, List<string> mutipleOrignalSourceKeyColumnList, string tableName, string otherConnectionString, string TargetTablewhereClause = "")
    {
        string localConne = otherConnectionString;

        string externalkeycolumn = GetOrginalSoureCombineKey(mutipleOrignalSourceKeyColumnList);


        //PK is not very use full at all !!!
        if (!string.IsNullOrEmpty(pkColumn))
        {
            string querymapping = " select " + externalkeycolumn + ", [" + pkColumn + "] from " + tableName;

            if (!string.IsNullOrWhiteSpace(TargetTablewhereClause))
            {
                querymapping = querymapping + " WHERE " + TargetTablewhereClause;
            }

            DataTable result = new DBInteractionBase(localConne).RetriveDataTable(querymapping);


            DataTable externalTable = new DataTable();
            externalTable.Columns.Add("externalKey");
            externalTable.Columns.Add("pkColumn");
            foreach (DataRow row in result.Rows)
            {
                string exterlaVale = string.Empty;

                foreach (string exKey in mutipleOrignalSourceKeyColumnList)
                {
                    exterlaVale = exterlaVale + row[exKey].ToString() + DataSoureHelp.StringSplitToken;
                }

                exterlaVale = exterlaVale.Substring(0, exterlaVale.Length - 1);

                DataRow newRow = externalTable.NewRow();

                externalTable.Rows.Add(newRow);

                newRow["externalKey"] = exterlaVale;
                newRow["pkColumn"] = row[pkColumn];




            }


            Dictionary<string, object> toRetrun = externalTable
                .AsEnumerable()
                .GroupBy(o => o["externalKey"].ToString(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(o => o.Key, o => o.First()["pkColumn"]);
            return toRetrun;
        }
        else // no PK but it is combine key
        {
            string querymapping = " select " + externalkeycolumn + "  from " + tableName;

            if (!string.IsNullOrWhiteSpace(TargetTablewhereClause))
            {
                querymapping = querymapping + " WHERE " + TargetTablewhereClause;
            }

            DataTable result = new DBInteractionBase(localConne).RetriveDataTable(querymapping);

            DataTable externalTable = new DataTable();
            externalTable.Columns.Add("externalKey");

            foreach (DataRow row in result.Rows)
            {
                string exterlaVale = string.Empty;

                foreach (string exKey in mutipleOrignalSourceKeyColumnList)
                {
                    // need to remove kye column space ( if dagtabase use nchar(20), the string length wll return 20, need to remove char space 
                    String pkColumnValue = row[exKey].ToString().Trim();


                    exterlaVale = exterlaVale + pkColumnValue + DataSoureHelp.StringSplitToken;
                }

                exterlaVale = exterlaVale.Substring(0, exterlaVale.Length - 1);

                DataRow newRow = externalTable.NewRow();

                externalTable.Rows.Add(newRow);

                newRow["externalKey"] = exterlaVale;





            }

            Dictionary<string, object> toRetrun = externalTable
                .AsEnumerable()
                .GroupBy(o => o["externalKey"].ToString(), StringComparer.OrdinalIgnoreCase)

                  .ToDictionary(o => o.Key, o => o.First()["externalKey"]);
            return toRetrun;

        }



    }

    private static string GetOrginalSoureCombineKey(List<string> mutipleOrignalSourceKeyColumnList)
    {
        string externalkeycolumn = string.Empty;


        foreach (string exKey in mutipleOrignalSourceKeyColumnList)
        {
            //RTRIM(LTRIM(@String1))
            externalkeycolumn = externalkeycolumn + exKey + ",";
        }

        if (externalkeycolumn != string.Empty)
        {

            externalkeycolumn = externalkeycolumn.Substring(0, externalkeycolumn.Length - 1);

        }

        return externalkeycolumn;
    }

    //public static Dictionary<string, object> GetExternalKeyPKMapping(string pkColumn, List<string> mutipleOrignalSourceKeyColumnList, string tableName, string otherConnectionString)
    //{
    //    string localConne = otherConnectionString;


    //    string externalkeycolumn = string.Empty;

    //    string conBineKey = "+'" + DataSoureHelp.StringSplitToken + "'+";
    //    foreach (string exKey in mutipleOrignalSourceKeyColumnList)
    //    {
    //        //RTRIM(LTRIM(@String1))
    //        externalkeycolumn = externalkeycolumn + "  RTRIM( LTRIM( CAST( [" + exKey + "] as varchar)))" + conBineKey;
    //    }

    //    if (externalkeycolumn != string.Empty)
    //    {

    //        externalkeycolumn = externalkeycolumn.Substring(0, externalkeycolumn.Length - conBineKey.Length);

    //    }

    //    externalkeycolumn = "(" + externalkeycolumn + ") as externalKey ";

    //    if (!string.IsNullOrEmpty(pkColumn))
    //    {
    //        string querymapping = " select " + externalkeycolumn + ", [" + pkColumn + "] from " + tableName;
    //        Dictionary<string, object> toRetrun = new DBInteractionBase(localConne).RetriveDataTable(querymapping)
    //            .AsEnumerable()
    //            .GroupBy(o => o["externalKey"])
    //            .Select(o => o.First())
    //            .ToDictionary(o => o["externalKey"].ToString().Trim(), o => o[pkColumn]);
    //        return toRetrun;
    //    }
    //    else // no PK but it is combine key
    //    {
    //        string querymapping = " select " + externalkeycolumn + "  from " + tableName;
    //        Dictionary<string, object> toRetrun = new DBInteractionBase(localConne).RetriveDataTable(querymapping)
    //            .AsEnumerable()
    //            .GroupBy(o => o["externalKey"])
    //            .Select(o => o.First())
    //              .ToDictionary(o => o["externalKey"].ToString().Trim(), o => o["externalKey"]);
    //        return toRetrun;

    //    }



    //}

    // reverse  ExternalKey- PK to Pk-external , bug fixed  on 2017 11-14
    public static Dictionary<string, string> GetPkExternalKeyMapping(string pkColumn, List<string> externalkeycolumn, string tableName, string otherConnectionString)
    {
        Dictionary<string, string> toRetrun = new Dictionary<string, string>();

        Dictionary<string, object> dictExternalPk = GetExternalKeyPKMapping(pkColumn, externalkeycolumn, tableName, otherConnectionString);

        foreach (string externalKey in dictExternalPk.Keys)
        {
            string pk = dictExternalPk[externalKey].ToString();
            toRetrun[pk] = externalKey;

        }

        //  toRetrun =  dictExternalPk.ToDictionary(o => o.Value.ToString (), o => o.Key); 
        return toRetrun;

    }



    }



}