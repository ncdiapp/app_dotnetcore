using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Linq; 

namespace APP.Persistence.Common
{
    public class SqlScriptGenerator
    {

        public static List<string> GenerateSqlUpdatesList(List<string> needUpdateColumnNames, List<string> whereColumns, DataTable dtTable, string targetTableName, string connectionString = null)
        {


            DataSoureHelp .RemoveDataTableColumnNotInDatabaseTable(dtTable, targetTableName, connectionString);


            List<string> allDtcolumnsAfterRemoveNotInDB = dtTable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();

            needUpdateColumnNames = needUpdateColumnNames.Intersect(allDtcolumnsAfterRemoveNotInDB).ToList();




            List<String> toReturn = new List<string>();


            string sSqlUpdates = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);
            StringBuilder sbColumns = new StringBuilder(string.Empty);

            // UPDATE table SET col1 = 3, col2 = 4 WHERE (select cols)
            // loop thru each record of the datatable
            foreach (DataRow drow in dtTable.Rows)
            {
                // VALUES clause:  loop thru each column, and include the value if the column is in the array
                string snewsql = GenerateOneUpateStement(needUpdateColumnNames, whereColumns, targetTableName, sbSqlStatements, drow);

                toReturn.Add(snewsql);


            }


            return toReturn;
        }

         public static List<string> GenerateSqlDeletesList(List<string> deleteWhereColumns, DataTable dtTable, string targetTableName, string connectionString = null)
        {


            DataSoureHelp.RemoveDataTableColumnNotInDatabaseTable(dtTable, targetTableName, connectionString);


            List<string> allDtcolumnsAfterRemoveNotInDB = dtTable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList();

            deleteWhereColumns = deleteWhereColumns.Intersect(allDtcolumnsAfterRemoveNotInDB).ToList();



            List<string> toReturn = new List<string>();

            string sSqlDeletes = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);

            // loop thru each record of the datatable
            foreach (DataRow drow in dtTable.Rows)
            {
                string snewsql = GenerateOneDeleteStatement(deleteWhereColumns, targetTableName, drow);

                toReturn.Add(snewsql);

            }

            sSqlDeletes = sbSqlStatements.ToString();
            return toReturn;
        }
        public static List<string> GenerateSqlInsertsList(List<string> needInsertColumnNames, DataTable dtTable, string targetTableName, string connectionString = null)
        {

            DataSoureHelp.RemoveDataTableColumnNotInDatabaseTable(dtTable, targetTableName, connectionString);

            List<string> allDtcolumnsAfterRemoveNotInDB =  dtTable.Columns.Cast<DataColumn>().Select(o => o.ColumnName).ToList ();

            needInsertColumnNames = needInsertColumnNames.Intersect(allDtcolumnsAfterRemoveNotInDB).ToList ();

            List<string> toReturnList = new List<string>();
            string sSqlInserts = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);
            StringBuilder sbColumns = new StringBuilder(string.Empty);

            // create the columns portion of the INSERT statement						            
            foreach (string colname in needInsertColumnNames)
            {
                if (sbColumns.ToString() != string.Empty)
                    sbColumns.Append(", ");

                sbColumns.Append("[" + colname + "]");
            }

            // loop thru each record of the datatable
            foreach (DataRow drow in dtTable.Rows)
            {
                string snewsql = GenerateOneLineInsertStatment(needInsertColumnNames, targetTableName, sbColumns, drow);

                toReturnList.Add(snewsql);
                //  sbSqlStatements.AppendLine();
                // sbSqlStatements.AppendLine();
            }

            sSqlInserts = sbSqlStatements.ToString();
            return toReturnList;
        }


        public static string GenerateSqlInserts(List<string> needInsertColumnNames, DataTable dtTable, string targetTableName)
        {
            string sSqlInserts = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);
            StringBuilder sbColumns = new StringBuilder(string.Empty);

            // create the columns portion of the INSERT statement						            
            foreach (string colname in needInsertColumnNames)
            {
                if (sbColumns.ToString() != string.Empty)
                    sbColumns.Append(", ");

                sbColumns.Append("[" + colname + "]");
            }

            // loop thru each record of the datatable
            foreach (DataRow drow in dtTable.Rows)
            {

                string snewsql = GenerateOneLineInsertStatment(needInsertColumnNames, targetTableName, sbColumns, drow);

               
                sbSqlStatements.Append(snewsql);
                sbSqlStatements.AppendLine();
               
            }

            sSqlInserts = sbSqlStatements.ToString();
            return sSqlInserts;
        }
     
        private static string GenerateOneLineInsertStatment(List<string> needInsertColumnNames, string targetTableName, StringBuilder sbColumns, DataRow drow)
        {
            // loop thru each column, and include the value if the column is in the array
            StringBuilder sbValues = new StringBuilder(string.Empty);
            foreach (string col in needInsertColumnNames)
            {
                if (sbValues.ToString() != string.Empty)
                    sbValues.Append(", ");

                // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                string sType = string.Empty;
                try
                {
                    sType = drow[col].GetType().ToString();
                    switch (sType.Trim().ToLower())
                    {
                        case "system.boolean":
                            sbValues.Append((Convert.ToBoolean(drow[col]) == true ? "1" : "0"));
                            break;

                        case "system.string":
                            string valueStr = drow[col] as string;
                            valueStr = CheckStringDefaultValue(valueStr);
                            sbValues.Append(string.Format("'{0}'", valueStr));
                            break;

                        case "system.datetime":
                            string sDateTime = QuoteSQLString(drow[col]);
                            if (DataSoureHelp.IsDateTime(sDateTime) == true)
                                sDateTime = System.DateTime.Parse(sDateTime).ToString("yyyy-MM-dd HH:mm:ss");
                            else
                                sDateTime = string.Empty;
                            sbValues.Append(string.Format("'{0}'", sDateTime));
                            break;

                        case "system.byte[]":
                            sbValues.Append(string.Format("'{0}'", Convert.ToBase64String((byte[])drow[col])));
                            break;

                        default:
                            if (drow[col] == System.DBNull.Value)
                                sbValues.Append("NULL");
                            else
                                sbValues.Append(Convert.ToString(drow[col]));
                            break;
                    }
                }
                catch
                {
                    sbValues.Append(string.Format("'{0}'", QuoteSQLString(drow[col])));
                }
            }

            //   INSERT INTO Tabs(Name) 
            //      VALUES('Referrals')
            // write the insert line out to the stringbuilder
            string snewsql = string.Format("INSERT INTO [{0}]({1}) ", targetTableName, sbColumns.ToString());

            snewsql =  snewsql + string.Format(" VALUES({0});", sbValues.ToString().Trim());

            return snewsql;
        }

        private static string CheckStringDefaultValue(string valueStr)
        {
            if (!string.IsNullOrEmpty(valueStr))
            {
                valueStr = valueStr.Trim();
                valueStr = QuoteSQLString(valueStr);
            }
            else
            {
                valueStr = "";
            }
            return valueStr;
        }

      
        private static string GenerateOneUpateStement(List<string> needUpdateColumnNames, List<string> whereColumns, string sTargetTableName, StringBuilder sbSqlStatements, DataRow drow)
        {
            StringBuilder sbValues = new StringBuilder(string.Empty);

            foreach (string col in needUpdateColumnNames)
            {
                StringBuilder sbNewValue = new StringBuilder("[" + col + "] = ");
                if (sbValues.ToString() != string.Empty)
                    sbValues.Append(", ");

                // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                string sType = string.Empty;
                try
                {
                    sType = drow[col].GetType().ToString();
                    switch (sType.Trim().ToLower())
                    {
                        case "system.boolean":
                            sbNewValue.Append((Convert.ToBoolean(drow[col]) == true ? "1" : "0"));
                            break;

                        case "system.string":
                             string valueStr = drow[col] as string;
                            valueStr = CheckStringDefaultValue(valueStr);

                            sbNewValue.Append(string.Format("'{0}'", valueStr));
                            break;

                        case "system.datetime":
                            string sDateTime = QuoteSQLString(drow[col]);
                            if (DataSoureHelp.IsDateTime(sDateTime) == true)
                                sDateTime = System.DateTime.Parse(sDateTime).ToString("yyyy-MM-dd HH:mm:ss");
                            else
                                sDateTime = string.Empty;
                            sbNewValue.Append(string.Format("'{0}'", sDateTime));
                            break;

                        case "system.byte[]":
                            sbNewValue.Append(string.Format("'{0}'", Convert.ToBase64String((byte[])drow[col])));
                            break;

                        default:
                            if (drow[col] == System.DBNull.Value)
                                sbNewValue.Append("NULL");
                            else
                                sbNewValue.Append(Convert.ToString(drow[col]));
                            break;
                    }
                }
                catch
                {
                    sbNewValue.Append(string.Format("'{0}'", QuoteSQLString(drow[col])));
                }

                sbValues.Append(sbNewValue.ToString());
            }

            // WHERE clause:  loop thru each column, and include the value if the column is in the array
            StringBuilder sbWhereValues = new StringBuilder(string.Empty);
            foreach (string col in whereColumns)
            {
                StringBuilder sbNewValue = new StringBuilder("[" + col + "] = ");
                if (sbWhereValues.ToString() != string.Empty)
                    sbWhereValues.Append(" AND ");

                // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                string sType = string.Empty;
                try
                {
                    sType = drow[col].GetType().ToString();
                    switch (sType.Trim().ToLower())
                    {
                        case "system.boolean":
                            sbNewValue.Append((Convert.ToBoolean(drow[col]) == true ? "1" : "0"));
                            break;

                        case "system.string":

                              string valueStr = drow[col] as string;
                            valueStr = CheckStringDefaultValue(valueStr);
                            sbNewValue.Append(string.Format("'{0}'", valueStr));
                            break;

                        case "system.datetime":
                            string sDateTime = QuoteSQLString(drow[col]);
                            if (DataSoureHelp.IsDateTime(sDateTime) == true)
                                sDateTime = System.DateTime.Parse(sDateTime).ToString("yyyy-MM-dd HH:mm:ss");
                            else
                                sDateTime = string.Empty;
                            sbNewValue.Append(string.Format("'{0}'", sDateTime));
                            break;

                        case "system.byte[]":
                            sbNewValue.Append(string.Format("'{0}'", Convert.ToBase64String((byte[])drow[col])));
                            break;

                        default:
                            if (drow[col] == System.DBNull.Value)
                                sbNewValue.Append("NULL");
                            else
                                sbNewValue.Append(Convert.ToString(drow[col]));
                            break;
                    }
                }
                catch
                {
                    sbNewValue.Append(string.Format("'{0}'", QuoteSQLString(drow[col])));
                }

                sbWhereValues.Append(sbNewValue.ToString());
            }

            // UPDATE table SET col1 = 3, col2 = 4 WHERE (select cols)
            // write the line out to the stringbuilder
            string snewsql = string.Format("UPDATE [{0}] SET {1} WHERE {2};", sTargetTableName, sbValues.ToString(), sbWhereValues.ToString());
            sbSqlStatements.Append(snewsql);
            return snewsql;
        }

        public static string GenerateSqlUpdates(List<string> needUpdateColumnNames, List<string> whereColumns, DataTable dtTable, string sTargetTableName)
        {
            string sSqlUpdates = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);
            StringBuilder sbColumns = new StringBuilder(string.Empty);

            // UPDATE table SET col1 = 3, col2 = 4 WHERE (select cols)
            // loop thru each record of the datatable
            foreach (DataRow drow in dtTable.Rows)
            {

                // VALUES clause:  loop thru each column, and include the value if the column is in the array
                string snewsql = GenerateOneUpateStement(needUpdateColumnNames, whereColumns, sTargetTableName, sbSqlStatements, drow);


                sbSqlStatements.Append(snewsql);
                sbSqlStatements.AppendLine();
               
            }

            sSqlUpdates = sbSqlStatements.ToString();
            return sSqlUpdates;
        }

   
        private static string GenerateOneDeleteStatement(List<string> deleteWhereColumns, string sTargetTableName, DataRow drow)
        {
            // loop thru each column, and include the value if the column is in the array
            StringBuilder sbValues = new StringBuilder(string.Empty);
            foreach (string col in deleteWhereColumns)
            {
                StringBuilder sbNewValue = new StringBuilder("[" + col + "] = ");

                if (sbValues.ToString() != string.Empty)
                    sbValues.Append(" AND ");

                // need to do a case to check the column-value types(quote strings(check for dups first), convert bools)
                string sType = string.Empty;
                try
                {
                    sType = drow[col].GetType().ToString();
                    switch (sType.Trim().ToLower())
                    {
                        case "system.boolean":
                            sbNewValue.Append((Convert.ToBoolean(drow[col]) == true ? "1" : "0"));
                            break;

                        case "system.string":
                            sbNewValue.Append(string.Format("'{0}'", QuoteSQLString(drow[col])));
                            break;

                        case "system.datetime":
                            string sDateTime = QuoteSQLString(drow[col]);
                            if (DataSoureHelp.IsDateTime(sDateTime) == true)
                                sDateTime = System.DateTime.Parse(sDateTime).ToString("yyyy-MM-dd HH:mm:ss");
                            else
                                sDateTime = string.Empty;
                            sbNewValue.Append(string.Format("'{0}'", sDateTime));
                            break;

                        default:
                            if (drow[col] == System.DBNull.Value)
                                sbNewValue.Append("NULL");
                            else
                                sbNewValue.Append(Convert.ToString(drow[col]));
                            break;
                    }
                }
                catch
                {
                    sbNewValue.Append(string.Format("'{0}'", QuoteSQLString(drow[col])));
                }

                sbValues.Append(sbNewValue.ToString());
            }

            // DELETE FROM table WHERE col1 = 3 AND col2 = '4'
            // write the line out to the stringbuilder
            string snewsql = string.Format("DELETE FROM [{0}] WHERE {1};", sTargetTableName, sbValues.ToString());
            return snewsql;
        }

        public static string GenerateSqlDeletes(List<string> deleteWhereColumns, DataTable dtTable, string sTargetTableName)
        {
            string sSqlDeletes = string.Empty;
            StringBuilder sbSqlStatements = new StringBuilder(string.Empty);

            // loop thru each record of the datatable
            foreach (DataRow drow in dtTable.Rows)
            {
                // loop thru each column, and include the value if the column is in the array

                string snewsql = GenerateOneDeleteStatement(deleteWhereColumns, sTargetTableName, drow);
             
                sbSqlStatements.Append(snewsql);
                sbSqlStatements.AppendLine();
               
            }

            sSqlDeletes = sbSqlStatements.ToString();
            return sSqlDeletes;
        }

        public static string QuoteSQLString(string str)
        {
            return str.Replace("'", "''");
        }

        public static string QuoteSQLString(object ostr)
        {
            return ostr.ToString().Replace("'", "''");
        }

        // Returns a string containing all the fields in the table

        public static string BuildAllFieldsSQL(DataTable table)
        {
            string sql = "";
            foreach (DataColumn column in table.Columns)
            {
                if (sql.Length > 0)
                    sql += ", ";
                sql += column.ColumnName;
            }
            return sql;
        }

        // Returns a SQL INSERT command. Assumes autoincrement is identity (optional)

        public static string BuildInsertSQL(DataTable table)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO " + table.TableName + " (");
            StringBuilder values = new StringBuilder("VALUES (");
            bool bFirst = true;
            bool bIdentity = false;
            string identityType = null;

            foreach (DataColumn column in table.Columns)
            {
                if (column.AutoIncrement)
                {
                    bIdentity = true;

                    switch (column.DataType.Name)
                    {
                        case "Int16":
                            identityType = "smallint";
                            break;
                        case "SByte":
                            identityType = "tinyint";
                            break;
                        case "Int64":
                            identityType = "bigint";
                            break;
                        case "Decimal":
                            identityType = "decimal";
                            break;
                        default:
                            identityType = "int";
                            break;
                    }
                }
                else
                {
                    if (bFirst)
                        bFirst = false;
                    else
                    {
                        sql.Append(", ");
                        values.Append(", ");
                    }

                    sql.Append(column.ColumnName);
                    values.Append("@");
                    values.Append(column.ColumnName);
                }
            }
            sql.Append(") ");
            sql.Append(values.ToString());
            sql.Append(")");

            if (bIdentity)
            {
                sql.Append("; SELECT CAST(scope_identity() AS ");
                sql.Append(identityType);
                sql.Append(")");
            }

            return sql.ToString(); ;
        }

        // Creates a SqlParameter and adds it to the command

        public static void InsertParameter(SqlCommand command,
                                             string parameterName,
                                              string sourceColumn,
                                              object value)
        {
            SqlParameter parameter = new SqlParameter(parameterName, value);

            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = parameterName;
            parameter.SourceColumn = sourceColumn;
            parameter.SourceVersion = DataRowVersion.Current;

            command.Parameters.Add(parameter);
        }

        // Creates a SqlCommand for inserting a DataRow
        public static SqlCommand CreateInsertCommand(DataRow row)
        {
            DataTable table = row.Table;
            string sql = BuildInsertSQL(table);
            SqlCommand command = new SqlCommand(sql);
            command.CommandType = System.Data.CommandType.Text;

            foreach (DataColumn column in table.Columns)
            {
                if (!column.AutoIncrement)
                {
                    string parameterName = "@" + column.ColumnName;
                    InsertParameter(command, parameterName, column.ColumnName, row[column.ColumnName]);
                }
            }
            return command;
        }

        // Inserts the DataRow for the connection, returning the identity
        public static object InsertDataRow(DataRow row, string connectionString)
        {
            SqlCommand command = CreateInsertCommand(row);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                command.Connection = connection;
                command.CommandType = System.Data.CommandType.Text;
                connection.Open();
                return command.ExecuteScalar();
            }
        }

    }
}
