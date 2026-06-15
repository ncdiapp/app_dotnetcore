using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;

namespace DatabaseSchemaMrg.ProviderSchemaReaders
{
    internal class DataTableHelp
    {


        public static DataTable GetOneTableQuery(DbProviderFactory factory, DbConnection connection, string aQuery, List<Tuple<string, object>> paramterList = null)
        {
            // public static DbProviderFactory GetFactory(DbConnection connection)
            // var factory = DbProviderFactories.GetFactory(Dbconnection);

          
          //  var factory = System.Data.Common.DbProviderFactories.GetFactory(connection);

            using (var command = factory.CreateCommand())
            {
                command.Connection = connection;
                command.CommandText = aQuery;
                command.CommandType = CommandType.Text;



                if (paramterList != null)
                {
                    foreach (var tuple in paramterList)
                    {
                        var pa = factory.CreateParameter();
                        pa.ParameterName = tuple.Item1;
                        pa.Value = tuple.Item2;
                        command.Parameters.Add(pa);
                    }


                }
                using (var adapter = factory.CreateDataAdapter())
                {
                    adapter.SelectCommand = command;

                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    //DataSet ds = new DataSet();
                    //da.Fill(ds, "Result");
                    //DataTable dt = ds.Tables["Result"];
                    return table;
                }


            }

        }
        public static void SavetoCSV(string tableName, DataTable dtv, string filePath)
        {

            string fileFullPath = Path.Combine(filePath , tableName.Trim() + ".csv");

            var sb = ConvertDataTableToCsvFile(dtv);
            using (StreamWriter objWriter = new StreamWriter(fileFullPath))
            {
                objWriter.WriteLine(sb);
            }
        }

        public static StringBuilder ConvertDataTableToCsvFile(DataTable dtData)
        {
            StringBuilder data = new StringBuilder();

            //Taking the column names.
            for (int column = 0; column < dtData.Columns.Count; column++)
            {
                //Making sure that end of the line, shoould not have comma delimiter.
                if (column == dtData.Columns.Count - 1)
                    data.Append(dtData.Columns[column].ColumnName.ToString().Replace(",", ";"));
                else
                    data.Append(dtData.Columns[column].ColumnName.ToString().Replace(",", ";") + ',');
            }

            data.Append(Environment.NewLine);//New line after appending columns.

            for (int row = 0; row < dtData.Rows.Count; row++)
            {
                for (int column = 0; column < dtData.Columns.Count; column++)
                {
                    ////Making sure that end of the line, shoould not have comma delimiter.
                    if (column == dtData.Columns.Count - 1)
                        data.Append(dtData.Rows[row][column].ToString().Replace(",", ";"));
                    else
                        data.Append(dtData.Rows[row][column].ToString().Replace(",", ";") + ',');
                }

                //Making sure that end of the file, should not have a new line.
                if (row != dtData.Rows.Count - 1)
                    data.Append(Environment.NewLine);
            }
            return data;
        }

    
    }
}