using APP.Framework;
using APP.Persistence.Common;
using Com.Visual2000.SystemFramework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APP.Persistence.Common
{
    public enum EmValidationResult { AllOk = 0, DbMandatoryFiledHasNoValue = 1, InvalidData = 2 };
    public static class DataSoureHelp
	{
        public static readonly string TABLE_CATALOG = "TABLE_CATALOG";
        public static readonly string TABLE_SCHEMA = "TABLE_SCHEMA";
        public static readonly string TABLE_NAME = "TABLE_NAME";
        public static readonly string TABLE_TYPE = "TABLE_TYPE";
        public static readonly string StringSplitToken = "|";



        public static readonly Regex TableRegex = new Regex(@"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$");


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
		/// /!!!!!!!!!!!!! make sure all Data type keep the same( for decimal 2.0  to string will be different from  decimal 3) ...
		/// </summary>
		/// <param name="sourceDatabTable"></param>
		/// <param name="targetPkColumn"></param>
		/// <param name="mutipleOrignalSourceKeyColumnList"></param>
		/// <param name="TargetTableName"></param>
		/// <param name="SourceExCludeColumns"></param>
		/// <param name="otherOverideConnectionString"></param>
		public static void ProcessMutileKeyTable(DataTable sourceDatabTable, string targetPkColumn, List<string> mutipleOrignalSourceKeyColumnList, string TargetTableName, List<string> SourceExCludeColumns,
			string otherOverideConnectionString, string TargetTablewhereClause = "")
		{

			//  string localConn = otherOverideConnectionString;
			//GetExternalKeyPKMapping

			if (sourceDatabTable.Rows.Count > 0)
			{

				sourceDatabTable = sourceDatabTable.AsEnumerable().GroupBy(o =>
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
			GetNewRowsAndUpdateRows(sourceDatabTable, targetPkColumn, mutipleOrignalSourceKeyColumnList, TargetTableName, SourceExCludeColumns, otherOverideConnectionString, out newSourceRowList, out updateSourceRowList, TargetTablewhereClause);

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

		public static string ExcuteBatchSqlStatment(string strConnString, List<string> insertStamentList)
		{
			using (SqlConnection conn = new SqlConnection(strConnString))
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
						ApplicationLog.WriteError(ex.Message);

						return ex.Message;

					}

				}

			}

			return string.Empty;
		}

        public static void RemoveDataTableColumnNotInDatabaseTable(DataTable dtTable, string sTargetTableName, string connectionString)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {

                // DBInteractionBase.GetQuerySchemeColumnNameDataType(  

                string queryTableColumName = @"  SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + sTargetTableName + "'";
                DBInteractionBase aDBInteractionBase = new DBInteractionBase(connectionString);

                List<string> dbColumnName = aDBInteractionBase.RetriveDataTable(queryTableColumName).AsEnumerable().Select(o => o["COLUMN_NAME"].ToString().ToLower()).ToList();


                List<DataColumn> needToRemove = new List<DataColumn>();

                foreach (DataColumn dtColumn in dtTable.Columns)
                {
                    //StringComparison
                    //StringComparison.InvariantCultureIgnoreCase  

                    if (!dbColumnName.Contains(dtColumn.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                    {

                        needToRemove.Add(dtColumn);
                    }

                }


                foreach (var column in needToRemove)
                {

                    dtTable.Columns.Remove(column);
                }




            }
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
					ApplicationLog.WriteError("Error " + ex.Message + " at " + currentSQl);

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
					ApplicationLog.WriteError("Error " + ex.Message + " at " + currentSQl);

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
			KeyValuePair<EmValidationResult, string> validationresult = ValidateDataTableAgainstDbTable(sourceDatabTable, TargetTableName, localConn);
			//toReturnList.Add(validationresult);

			if (validationresult.Key != EmValidationResult.AllOk)
			{

				ApplicationLog.WriteError(validationresult.Value);

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
							ApplicationLog.WriteError(ex.Message);

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
							ApplicationLog.WriteError(ex.Message);

						}

					}

				}


				//new DBInteractionBase(localConne).ExecuteNonQuery(updateStament);

			}

		}



        public static KeyValuePair<EmValidationResult, string> ValidateDataTableAgainstDbTable(DataTable dtTable, string targetDataBaseTableName, string connectionString)
        {

            // 0: means all are ok !
            // int toReturn = 0;

            Dictionary<string, List<DataRow>> DictMandatoryFiledRow = new Dictionary<string, List<DataRow>>();
            Dictionary<string, List<DataRow>> DictColumnMaxLengthExceddRow = new Dictionary<string, List<DataRow>>();


            StringBuilder sbInvalidteToReturn = new StringBuilder();
            string queryidentFiled = @"SELECT sys.columns.name FROM sys.columns
                        WHERE   is_identity =1 and  object_id = object_id('" + targetDataBaseTableName + @"')
                        ";
            List<string> IdentityIntcolumn = new DBInteractionBase(connectionString).RetriveDataTable(queryidentFiled).AsEnumerable().Select(o => o["name"] as string).ToList();


            string queryTableColumn = @"    SELECT 
               COLUMN_NAME, IS_NULLABLE, COLUMN_DEFAULT, DATA_TYPE ,CHARACTER_MAXIMUM_LENGTH
            FROM 
               INFORMATION_SCHEMA.COLUMNS
            WHERE 
               TABLE_NAME = '" + targetDataBaseTableName + "' ";

            // string queryTableColumName = @"  SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + sTargetTableName + "'";
            DBInteractionBase aDBInteractionBase = new DBInteractionBase(connectionString);
            DataTable queryReqlst = aDBInteractionBase.RetriveDataTable(queryTableColumn);
            var result = queryReqlst.AsEnumerable();

            List<string> AllDbColumns = result.Select(o => o["COLUMN_NAME"].ToString()).ToList();

            //(newsequentialid())
            // need to remove "timestamp" ,not include identity id, and auto GuiID
            List<string> DbMandatoryColumns = result.Where(o => (o["IS_NULLABLE"].ToString() == "NO")
                && (o["DATA_TYPE"].ToString() != SqlServerDataType.timestamp_)
                && (!(o["COLUMN_DEFAULT"] != null && o["COLUMN_DEFAULT"].ToString().Contains("newsequentialid")))
                  && (!(IdentityIntcolumn.Contains(o["COLUMN_NAME"].ToString())))
                )
                                                .Select(o => o["COLUMN_NAME"].ToString()).ToList();

            Dictionary<string, int> DictColumnMaxLength = result.Where(
                    o => o["DATA_TYPE"].ToString().ToLowerInvariant() == SqlServerDataType.nvarchar_
                        || o["DATA_TYPE"].ToString().ToLowerInvariant() == SqlServerDataType.nchar_
                        || o["DATA_TYPE"].ToString().ToLowerInvariant() == SqlServerDataType.char_
                        || o["DATA_TYPE"].ToString().ToLowerInvariant() == SqlServerDataType.varchar_
                        || o["DATA_TYPE"].ToString().ToLowerInvariant() == SqlServerDataType.text_
                        )
                                                .ToDictionary(o => o["COLUMN_NAME"].ToString(),
                                                o => int.Parse(o["CHARACTER_MAXIMUM_LENGTH"].ToString()));

            Dictionary<string, string> DictFieldDaType = result
                                               .ToDictionary(o => o["COLUMN_NAME"].ToString(),
                                               o => o["DATA_TYPE"].ToString());


            Dictionary<string, string> DictColumnDefault = result.Where(o => !string.IsNullOrEmpty(o["COLUMN_DEFAULT"] as string))
                                           .ToDictionary(o => o["COLUMN_NAME"].ToString(),
                                           o => o["COLUMN_DEFAULT"].ToString().Replace("(", "").Replace(")", "").Replace("'", string.Empty));


            //1: remove no exsting column in database ????
            //ProcessOneKeyTable will process   DataSoureHelp.ProcessOneKeyTable(sourceDatabTable, sourceKeyColumn, TargetOriginalSourceKeycolumn, TargetTableName, SourceExCludeColumns, plmDbconneinfo);
            RemoveDataTableColumnNotInDbTableAndRenameToDbColumnName(dtTable, AllDbColumns);
            // need to remove timestampe column on the fly


            // 2:  check if mandoarory filed has value . not inlcude auto Generation number 1
            List<string> dataTableColumns = dtTable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();


            // Dictionary<string, List<DataRow>> DictMandatoryFiledRow = new Dictionary<string, List<DataRow>>();
            foreach (string dbMandatoryField in DbMandatoryColumns)
            {

                // dataTable Columns include  dbMandatoryField
                if (dataTableColumns.Contains(dbMandatoryField))
                {
                    List<DataRow> mantoryRowList = new List<DataRow>();

                    foreach (DataRow dr in dtTable.Rows)
                    {
                        //he class System.DbNull represents a nonexistent value in a database field
                        //if (rsData["usr.ursrdaystime"] != System.DBNull.Value))
                        if (dr[dbMandatoryField] == System.DBNull.Value)
                        {
                            mantoryRowList.Add(dr);


                            //!!!!! the end user must fill the mandoary file
                            // we can assign default value from 

                            //if (DictColumnDefault.ContainsKey(dbMandatoryField))
                            //{
                            //    if (DictFieldDaType[dbMandatoryField].ToLowerInvariant() == "datetime")
                            //    {
                            //        dr[dbMandatoryField] = System.DateTime.Now;     
                            //    }
                            //    else // need to convert to concorete type
                            //    {
                            //        dr[dbMandatoryField] = DictColumnDefault[dbMandatoryField];

                            //    }



                            //}
                            //else // cannot find the DB defautl value
                            //{

                            //    mantoryRowList.Add(dr);

                            //}

                        }

                    }

                    if (mantoryRowList.Count > 0)
                    {
                        DictMandatoryFiledRow.Add(dbMandatoryField, mantoryRowList);

                    }


                }
                //dataTable Columns  does not  include  dbMandatoryField , need to check if the dabase table has default value !!!!!
                else
                {
                    //  Cannot find the default valuye from database , cannot excute Sql statment !!1
                    if (!DictColumnDefault.ContainsKey(dbMandatoryField))
                    {

                        //dr[dbMandatoryField] = DictColumnDefault[dbMandatoryField];

                        DictMandatoryFiledRow.Add(dbMandatoryField, null);
                    }


                }

            }


            //3: check maxium lenght

            foreach (DataColumn col in dtTable.Columns)
            {
                List<DataRow> maxLengthRowList = new List<DataRow>();

                string columnName = col.ColumnName;
                if (DictColumnMaxLength.ContainsKey(columnName))
                {
                    foreach (DataRow dr in dtTable.Rows)
                    {
                        string valueStr = dr[col] as string;
                        if (!string.IsNullOrWhiteSpace(valueStr))
                        {
                            int length = valueStr.Length;
                            int dbbaselength = DictColumnMaxLength[columnName];
                            if (length > dbbaselength)
                            {

                                maxLengthRowList.Add(dr);

                            }
                        }
                    }

                }

                if (maxLengthRowList.Count > 0)
                {
                    DictColumnMaxLengthExceddRow.Add(columnName, maxLengthRowList);

                }

            }//

            //Step4: Validate Data Type ;; TODO !!!!

            List<DataRow> allInvalidateRows = new List<DataRow>();

            // step5:  need to process validation result !!
            if (DictMandatoryFiledRow.Count > 0)
            {
                foreach (string columnKey in DictMandatoryFiledRow.Keys)
                {
                    List<DataRow> rowList = DictMandatoryFiledRow[columnKey];

                    if (rowList == null)
                    {
                        string errorMessage = targetDataBaseTableName + ":" + columnKey + ":Mandatory Column Has no default value ";


                        KeyValuePair<EmValidationResult, string> toReturn = new KeyValuePair<EmValidationResult, string>(EmValidationResult.DbMandatoryFiledHasNoValue, errorMessage);
                        return toReturn;

                    }
                    else
                    {

                        foreach (DataRow row in rowList)
                        {

                            string rowString = string.Empty;
                            rowString = ConvertDataRowToString(dataTableColumns, row, rowString);

                            string errormsg = targetDataBaseTableName + "  : Mandatory " + columnKey + " has no value ! at  Row " + rowString;


                            sbInvalidteToReturn.Append(errormsg + System.Environment.NewLine);

                            allInvalidateRows.Add(row);
                        }

                    }

                }

            }

            // Step6: check maxium column length

            if (DictColumnMaxLengthExceddRow.Count > 0)
            {
                foreach (string columnKey in DictColumnMaxLengthExceddRow.Keys)
                {
                    List<DataRow> rowList = DictColumnMaxLengthExceddRow[columnKey];
                    foreach (DataRow row in rowList)
                    {
                        string rowString = string.Empty;
                        rowString = ConvertDataRowToString(dataTableColumns, row, rowString);



                        string errormsg = targetDataBaseTableName + ":" + columnKey + " exceed maxium length (" + DictColumnMaxLength[columnKey] + " )  in Row " + rowString;


                        sbInvalidteToReturn.Append(errormsg + System.Environment.NewLine);


                        allInvalidateRows.Add(row);


                    }
                }
            }

            var distinctInvalidateRows = allInvalidateRows.Distinct().ToList();



            if (distinctInvalidateRows.Count > 0)
            {
                KeyValuePair<EmValidationResult, string> toReturn = new KeyValuePair<EmValidationResult, string>(EmValidationResult.InvalidData, sbInvalidteToReturn.ToString());
                return toReturn;

            }
            else
            {
                KeyValuePair<EmValidationResult, string> toReturn = new KeyValuePair<EmValidationResult, string>(EmValidationResult.AllOk, "all ok");
                return toReturn;

            }



        }

        private static string ConvertDataRowToString(List<string> dataTableColumns, DataRow row, string rowString)
        {
            foreach (string columnName in dataTableColumns)
            {

                object value = row[columnName];
                if (value != null)
                {
                    rowString = rowString + value.ToString() + StringSplitToken;

                }
                else
                {
                    rowString = rowString + "" + StringSplitToken;
                }



            }
            return rowString;
        }

        private static void RemoveDataTableColumnNotInDbTableAndRenameToDbColumnName(DataTable dtTable, List<string> AllDbColumns)
        {
            List<DataColumn> needToRemove = new List<DataColumn>();

            foreach (DataColumn dtColumn in dtTable.Columns)
            {
                // not contain
                if (!AllDbColumns.Contains(dtColumn.ColumnName, StringComparer.InvariantCultureIgnoreCase))
                {

                    needToRemove.Add(dtColumn);
                }
                else // it is include in database table column
                {

                    dtColumn.ColumnName = AllDbColumns.First(o => string.Compare(o, dtColumn.ColumnName, true) == 0);
                }

            }

            foreach (var column in needToRemove)
            {

                dtTable.Columns.Remove(column);
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
						exterlaVale = exterlaVale + row[exKey].ToString() + StringSplitToken;
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
						exterlaVale = exterlaVale + row[exKey].ToString() + StringSplitToken;
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
        /// <summary>
        /// Returns true if the given object is a valid number, or false if it's not
        /// </summary>
        /// <param name="sDateTime"></param>
        /// <returns></returns>
        public static bool IsNumeric(Object objValue)
        {
            bool _Valid = false;

            try
            {
                double y = Convert.ToDouble(objValue);
                _Valid = true;
                return _Valid;
            }
            catch
            {
                _Valid = false;
            }

            try
            {
                int x = Convert.ToInt32(objValue);
                _Valid = true;
                return _Valid;
            }
            catch
            {
                _Valid = false;
            }

            return _Valid;
        }

        /// <summary>
        /// Returns true if the given string is a valid date string, or false if it's not
        /// </summary>
        /// <param name="sDateTime"></param>
        /// <returns></returns>
        public static bool IsDateTime(string sDateTime)
        {
            bool bIsDateTime = false;

            try
            {
                System.DateTime.Parse(sDateTime);
                bIsDateTime = true;
            }
            catch
            {
                bIsDateTime = false;
            }

            return bIsDateTime;
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

		//    string conBineKey = "+'" + DBInteractionBase.StringSplitToken + "'+";
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

		public static Dictionary<object, string> GetPkExternalKeyMapping(string pkColumn, List<string> externalkeycolumn, string tableName, string otherConnectionString)
		{
			Dictionary<object, string> toRetrun = new Dictionary<object, string>();

			Dictionary<string, object> dictExternalPk = GetExternalKeyPKMapping(pkColumn, externalkeycolumn, tableName, otherConnectionString);

			foreach (string key in dictExternalPk.Keys)
			{
				if (!toRetrun.ContainsKey(key))
				{
					toRetrun.Add(dictExternalPk[key], key);

				}


			}

			return toRetrun;

		}


		////https://dev.mysql.com/doc/refman/8.0/en/integer-types.html
		//public static string GetCreateTableSqlScript(string tableName, DataTable table)
		//{
		//	string sqlsc;
		//	sqlsc = "CREATE TABLE " + tableName + "(";
		//	for (int i = 0; i < table.Columns.Count; i++)
		//	{
		//		DataColumn dataColumn = table.Columns[i];

		//		sqlsc += "\n [" + dataColumn.ColumnName + "] ";
		//		string columnType = dataColumn.DataType.ToString();
		//		switch (columnType)
		//		{
		//			case "System.Int32":
		//				sqlsc += " int ";
		//				break;
		//			case "System.Int64":
		//				sqlsc += " bigint ";
		//				break;
		//			case "System.Int16":
		//				sqlsc += " smallint";
		//				break;
		//			case "System.Byte":
		//				sqlsc += " tinyint";
		//				break;
		//			case "System.Decimal":
		//				sqlsc += " decimal(18,4) ";
		//				break;
		//			case "System.DateTime":
		//				sqlsc += " datetime ";
		//				break;
		//			case "System.String": //65535 (nvarchr)
		//			default:
		//				sqlsc += string.Format(" varchar({0}) ", dataColumn.MaxLength == -1 ? "8000" : dataColumn.MaxLength.ToString());
		//				break;
		//		}
		//		if (dataColumn.AutoIncrement)
		//			sqlsc += " IDENTITY(" + dataColumn.AutoIncrementSeed.ToString() + "," + dataColumn.AutoIncrementStep.ToString() + ") ";
		//		if (!dataColumn.AllowDBNull)
		//			sqlsc += " NOT NULL ";
		//		sqlsc += ",";
		//	}

		//	//The MySQL version before 5.0. 3 was capable of storing 255 characters but from the version 5.0. 3 , it is capable of storing 65,535 characters
		//	return sqlsc.Substring(0, sqlsc.Length - 1) + "\n)";
		//}


		public static string GetPrimayKeyCreate(string tableName, DataTable table)
		{

			
			string primaryKeyName = "";

			for (int i = 0; i < table.Columns.Count; i++)
			{
				DataColumn dataColumn = table.Columns[i];
				if (dataColumn.Caption == "PrimaryKey")
				{
					primaryKeyName = dataColumn.ColumnName;

					break;
				}

			}

			if (!string.IsNullOrWhiteSpace(primaryKeyName))
			{

				return  string.Format(@"ALTER TABLE {0} add CONSTRAINT pk_{1}{2} primary key({3})", tableName, tableName, primaryKeyName, primaryKeyName);
				
			}
			else
			{
				return "";
			}



		}

	}
}
