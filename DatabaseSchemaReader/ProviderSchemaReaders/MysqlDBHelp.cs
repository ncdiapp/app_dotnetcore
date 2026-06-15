using DatabaseSchemaMrg.DataSchema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;

namespace DatabaseSchemaMrg.ProviderSchemaReaders
{
    public class MysqlDBHelp
    {

        // static  string sqlserverconn = System.Configuration.ConfigurationManager.ConnectionStrings["MysqlConnectionString"].ConnectionString;


        // myConnectionString1 = "server=127.0.0.1;uid=sa;pwd=plmsa;database=world";
        //   string myConnectionString2 = "server=127.0.0.1;uid=sa;pwd=plmsa;database=world_new";

        //static string myConnectionString1= "server=127.0.0.1;uid=sa;pwd=plmsa;database=world";
        //static string myConnectionString2 = "server=127.0.0.1;uid=sa;pwd=plmsa;database=world_new";

        public static void CompareTowDBFixture(string myConnectionString1, string myConnectionString2)
        {

           var fixture1=  new DatabaseFixture(myConnectionString1, DataSchema.EmSqlType.MySql);
            var fixture2 = new DatabaseFixture(myConnectionString1, DataSchema.EmSqlType.MySql);

            var t1 =  fixture1.AllTables();
            var t2 = fixture1.AllTables();

        }

            public   static void CompareTowDB(string myConnectionString1, string myConnectionString2)
        {





            DbProviderFactory factor = Utilities.FactoryTools.GetFactory("MySql.Data.MySqlClient");



            Dictionary<string, Dictionary<string, string>> dictTableNameFieldListOld = GetOneDatabaseSchemaInfo(myConnectionString1, factor);



            Dictionary<string, Dictionary<string, string>> dictTableNameFieldListNew = GetOneDatabaseSchemaInfo(myConnectionString2, factor);

            CompareAllDBTable(dictTableNameFieldListOld, dictTableNameFieldListNew);

        }

        private static void CompareAllDBTable(Dictionary<string, Dictionary<string, string>> dictTableNameFieldListOld, Dictionary<string, Dictionary<string, string>> dictTableNameFieldListNew)
        {
            // common table
            // 
            var commTable = dictTableNameFieldListOld.Keys.Intersect(dictTableNameFieldListNew.Keys).ToList();
            var addedTable = dictTableNameFieldListNew.Keys.Except(dictTableNameFieldListOld.Keys).ToList();
            var deleteTable = dictTableNameFieldListOld.Keys.Except(dictTableNameFieldListNew.Keys).ToList();

            StringBuilder sb = new StringBuilder();
           // sb.AppendFormat("Foo{0}Bar", Environment.NewLine);

            //  Write lines to a text file in C# (CSV)
            //using (var writer = new StreamWriter("C:\\temp\\test.txt"))
            //{
             if (addedTable.Count > 0)
                {
                    sb.AppendFormat($"--new added Table list--{Environment.NewLine}");

                    foreach (string tabNmae in addedTable)
                    {
                        sb.AppendFormat(tabNmae);

                    }
                }

                if (deleteTable.Count > 0)
                {
                    sb.AppendFormat($"--delete  Table list--{Environment.NewLine}");

                    foreach (string tabNmae in deleteTable)
                    {
                        sb.AppendFormat(tabNmae);

                    }
                }

                if (commTable.Count > 0)
                {
                    sb.AppendFormat($"--common Table diff--{Environment.NewLine}");

                    foreach (string tabNmae in commTable)
                    {
                        sb.AppendFormat($" table: {tabNmae}: {Environment.NewLine}");

                        var dictNewFiledList = dictTableNameFieldListNew[tabNmae];
                        var dictOldFiledList = dictTableNameFieldListOld[tabNmae];

                        var commTableColumn = dictOldFiledList.Keys.Intersect(dictNewFiledList.Keys).ToList();
                        var addedTablecolumn = dictNewFiledList.Keys.Except(dictOldFiledList.Keys).ToList();
                        var deleteTableColumn = dictOldFiledList.Keys.Except(dictNewFiledList.Keys).ToList();



                        if (addedTablecolumn.Count > 0)
                        {


                            foreach (string cNmae in addedTablecolumn)
                            {
                                sb.AppendFormat($"  added: {cNmae}->{dictNewFiledList[cNmae]} {Environment.NewLine}");



                            }
                        }
                        if (deleteTableColumn.Count > 0)
                        {


                            foreach (string cNmae in deleteTableColumn)
                            {
                                sb.AppendFormat($"  deleted: {cNmae} {Environment.NewLine}");

                            }
                        }

                        if (commTableColumn.Count > 0)
                        {


                            List<string> changeColList = new List<string>();

                            foreach (string cNmae in commTableColumn)
                            {
                                string oldColInfo = dictOldFiledList[cNmae];
                                string newColInfo = dictNewFiledList[cNmae];

                                if (!string.Equals(oldColInfo, newColInfo, StringComparison.OrdinalIgnoreCase))
                                {
                                    changeColList.Add($"  diff: {cNmae}: {oldColInfo} --> {newColInfo} {Environment.NewLine}");
                                }



                            }
                            if (changeColList.Count > 0)
                            {
                                foreach (string chgColInfo in changeColList)
                                {
                                    sb.AppendFormat(chgColInfo);
                                }
                            }


                        }




                    }

                }

           // }
        }

        private static Dictionary<string, Dictionary<string, string>> GetOneDatabaseSchemaInfo(string myConnectionString, DbProviderFactory factor)
        {
            Dictionary<string, Dictionary<string, string>> dictTableNameFieldList = new Dictionary<string, Dictionary<string, string>>();

            DataTable dt;
            using (DbConnection dbconnection = factor.CreateConnection())
            {
                dbconnection.ConnectionString = myConnectionString;

                string databasename = dbconnection.Database;
                // Get all 
                //string getAllDatabaesTableQuery = " SELECT table_name FROM information_schema.tables";

                //dt = DataTableHelp.GetOneTableQuery(dbconnection, getAllDatabaesTableQuery, null);


                string getOneDatabaesTableQuery = $@" 
                SELECT table_name FROM information_schema.tables
                WHERE table_schema = '{databasename}' ";


                dt = DataTableHelp.GetOneTableQuery(factor, dbconnection, getOneDatabaesTableQuery, null);

                foreach (DataRow row in dt.Rows)
                {
                    string tablename = row[0].ToString().Trim();

                    string getOneDatabaesSchema = $"  DESCRIBE {databasename}.{tablename} ";
                    dt = DataTableHelp.GetOneTableQuery(factor, dbconnection, getOneDatabaesSchema, null);
                    Dictionary<string, string> dictColumnInfoList = new Dictionary<string, string>();
                    foreach (DataRow colRow in dt.Rows)
                    {
                        string oneColInfo = "";

                        for (int i = 1; i < colRow.Table.Columns.Count; i++)
                        {
                            oneColInfo = oneColInfo + colRow[i].ToString().Trim() + ",";
                        }

                        dictColumnInfoList[colRow[0].ToString()] = oneColInfo;
                    }

                    dictTableNameFieldList[tablename] = dictColumnInfoList;

                }





                //  ;


            }

            return dictTableNameFieldList;
        }

        private static Dictionary<string, Dictionary<string,string>> GetOneDatabaseSchemaInfo(DatabaseFixture databaseFixture)
        {
            Dictionary<string, Dictionary<string, string>> dictTableNameFieldList = new Dictionary<string, Dictionary<string, string>>();

            var allTables = databaseFixture.AllTables();

            foreach (DatabaseTable databaseTable in allTables)
            {
                string tablename = databaseTable.Name;

             
                Dictionary<string, string> dictColumnInfoList = new Dictionary<string, string>();
                foreach (DatabaseColumn databaseColumn in databaseTable.Columns)
                {
                    string oneColInfo =  $@" {databaseColumn.DataType.TypeName}_{databaseColumn.IsPrimaryKey}_{databaseColumn.Nullable}_{databaseColumn.DefaultValue}_{databaseColumn.Length}";



                    dictColumnInfoList[databaseColumn.Name] = oneColInfo;
                }

                dictTableNameFieldList[tablename] = dictColumnInfoList;

            }

            return dictTableNameFieldList;
        }
    }






}
