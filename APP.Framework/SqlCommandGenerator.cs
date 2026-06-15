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
    public class SqlCommandGenerator
    {

        // Returns a SQL INSERT command. Assumes autoincrement is identity (optional)

//           SELECT 
//   COLUMN_NAME, IS_NULLABLE, COLUMN_DEFAULT, DATA_TYPE ,CHARACTER_MAXIMUM_LENGTH
//FROM 
//   INFORMATION_SCHEMA.COLUMNS
//WHERE 
//   TABLE_NAME = 'tblproduct' 
        
        //    string connes = @"Data Source=lab-vms;Initial Catalog=OOTB_ERP_BLANK;Integrated Security=False;User ID=sa;Password=sa; Connection Timeout=160";

        ////     With myDataSet.Tables("Orders")
        ////.Columns("Order_Date").DefaultValue = Today
        ////.Columns("Quantity").DefaultValue = 1

        //    using (SqlConnection conn = new SqlConnection(connes))
        //    {
        //        conn.Open();
        //        var schema = conn.GetSchema("Columns", new string[4] { conn.Database, null, "tblsizerun", null });
            
        //    }


        //public static List<SqlCommand> GenerateSqlUpdatesList(List<string> needUpdateColumnNames, List<string> whereColumns, DataTable dtTable, string targetTableName, string connectionString = null)
        //{

        //    List<SqlCommand> commandList = new List<SqlCommand>();



        //    return commandList;

        //}


        public static List<SqlCommand> GenerateSqlUpdatesList(List<string> needUpdateColumnNames, List<string> whereColumns, DataTable dtTable, string targetTableName, string connectionString = null)
        {

            List<SqlCommand> commandList = new List<SqlCommand>();



            return commandList;
        
        }
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
