//using APP.Persistence.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using APP.Framework;

namespace APP.BL.DataMigration
{
    //---sp_help 'pdmProductGridValueView'

    //sp_help 'pdmProduct'


    //EXEC sp_pkeys 'pdmProduct'
    //EXEC sp_fkeys 'pdmProduct'

    //select* from PdmUserDefineEntityColumn

    //SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'pdmProduct'  and COLUMN_NAME = 'ProductReferenceID'

    //ALTER TABLE YourTable ADD Foo INT NULL

    //Update  pdmProduct set orgPkColumName = pkColumnNale

    //SELECT TABLE_NAME,COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS where COL_NAME like
    internal class DataCopyBL
    {

        const string ObjectTypeUserTable = "user table";
        const string ObjectTypeView = "view";
        const string OrgColPefix = "OrgCol_";
        const string DescConnInfo = "Data Source=appserver;Initial Catalog=TestDB;Integrated Security=False;User ID=sa;Password=sa;Connection Timeout=60";

        private static void Main(string[] args)
        {



            //ProductScaner.SetupCredential();

            //// 2140: not suppot ijep form
            ////2894
            //var FileIdUserId = new { FileId = 2894, UserId = 1 };
            //string toReturn = ImportExcelSheetToStyleBL.ImportExcelFileId(FileIdUserId);



            // DataCopy("AppFile");

            // trafer data:



            // DisableAllContrain(DescConnInfo);

            //DropAllOrgPrefixColumn();
            // EnbleAllContrain(DescConnInfo);
            string pkTableName = "AppFile";
            string getPrimaryKeys = string.Format(@"SELECT COLUMN_NAME  FROM INFORMATION_SCHEMA.COLUMNS  where  TABLE_NAME='{0}'  and Data_type<>'timestamp'
             and COLUMNPROPERTY(object_id(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') <> 1", pkTableName);
            var pkDt = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).RetriveDataTable(getPrimaryKeys);
           
             





        }



        public static void DataCopy(string tableName)
        {

            string getTableInfo = string.Format("sp_help '{0}'", tableName);


            // get the first DataRow
            var tableInfoDt = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).RetriveDataTable(getTableInfo);

            if (tableInfoDt.Rows.Count > 0)
            {
                DataRow oneRow = tableInfoDt.Rows[0];
                string owner = oneRow["Owner"].ToString();
                string objectType = oneRow["Type"].ToString();
                DateTime Created_datetime = (DateTime)oneRow["Created_datetime"];

                if (objectType == ObjectTypeUserTable)
                {
                    ProcessPk(tableName);

                }


            }


        }

        private static void ProcessPk(string pkTableName)
        {
            string getPrimaryKeys = string.Format("EXEC sp_pkeys '{0}'", pkTableName);
            var pkDt = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).RetriveDataTable(getPrimaryKeys);

            // Assume only have on e
            if (pkDt.Rows.Count > 0)
            {
                DataRow oneRow = pkDt.Rows[0];
                string owner = oneRow["TABLE_OWNER"].ToString();
                string pkColumnNale = oneRow["COLUMN_NAME"].ToString();
                string pkConstrain = oneRow["PK_NAME"].ToString();

                string dataType = CopyPkNameAndTrasnferPKtoOrgColumn(pkTableName, pkColumnNale);
                CopyFkNameAndTrasnferFktoOrgColumn(pkTableName, dataType);


            }

            // 

        }

        private static void CopyFkNameAndTrasnferFktoOrgColumn(string pkTableName, string pkDataType)
        {
            string getFkDefine = string.Format(@"EXEC sp_fkeys '{0}'", pkTableName);

            var fkdefineDt = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).RetriveDataTable(getFkDefine);

            foreach (DataRow fkRow in fkdefineDt.Rows)
            {
                string FKTABLE_NAME = fkRow["FKTABLE_NAME"].ToString();
                string FKCOLUMN_NAME = fkRow["FKCOLUMN_NAME"].ToString();

                CopyCoumnAndValue(FKTABLE_NAME, FKCOLUMN_NAME, pkDataType);
            }



        }
        private static string CopyPkNameAndTrasnferPKtoOrgColumn(string tableName, string pkColumnNale)
        {
            string getPkDefine = string.Format(@"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH    FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'{0}'  and COLUMN_NAME ='{1}'", tableName, pkColumnNale);

            var pkdefineDt = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).RetriveDataTable(getPkDefine);

            DataRow onePkRow = pkdefineDt.Rows[0];

            string dataType = onePkRow["DATA_TYPE"].ToString();

            string chraterLength = onePkRow["CHARACTER_MAXIMUM_LENGTH"].ToString();
            // string isNullable = onePkRow["IS_NULLABLE"].ToString();
            // no need to 
            if (dataType.ToLower() == "nvarchar" || dataType.ToLower() == "varchar")
            {
                dataType = string.Format("nvarchar({0})", chraterLength);
            }
            CopyCoumnAndValue(tableName, pkColumnNale, dataType);

            return dataType;

        }

        private static void DropAllOrgPrefixColumn()
        {
            string orgColumQuery = string.Format(@"
                 SELECT TABLE_NAME,COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH    FROM INFORMATION_SCHEMA.COLUMNS where COLUMN_NAME like '{0}_%'", OrgColPefix);

            var pkdefineDt = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).RetriveDataTable(orgColumQuery);

            foreach (DataRow fkRow in pkdefineDt.Rows)
            {
                string TABLE_NAME = fkRow["TABLE_NAME"].ToString();
                string COLUMN_NAME = fkRow["COLUMN_NAME"].ToString();

                DropTableCoumn(TABLE_NAME, COLUMN_NAME);
            }




        }

        private static void CopyCoumnAndValue(string tableName, string orgColumnName, string dataType)
        {
            string orgPkColumName = OrgColPefix + orgColumnName;
            string alterAddOrgPk = string.Format("ALTER TABLE [{0}]   ADD [{1}] {2} NULL ", tableName, orgPkColumName, dataType);

            new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).ExecuteNonQuery(alterAddOrgPk);

            //-----------
            string updateValue = string.Format("Update  {0} set {1} = {2}", tableName, orgPkColumName, orgColumnName) + System.Environment.NewLine;
            new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).ExecuteNonQuery(updateValue);
        }

        private static void DropTableCoumn(string tableName, string orgColumnName)
        {
            string dropCoumn = string.Format("ALTER TABLE [{0}] DROP COLUMN  [{1}] ", tableName, orgColumnName);

            new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString).ExecuteNonQuery(dropCoumn);


        }

        private static void DisableAllContrain(string conneInfo)
        {
            string disableAllConstgrain = @"EXEC sp_msforeachtable ""ALTER TABLE ? NOCHECK CONSTRAINT all"" ";

            new DBInteractionBase(conneInfo).ExecuteNonQuery(disableAllConstgrain);


        }

        private static void EnbleAllContrain(string conneInfo)
        {
            string disableAllConstgrain = "EXEC sp_msforeachtable \"ALTER TABLE ? CHECK CONSTRAINT all\" ";

            new DBInteractionBase(conneInfo).ExecuteNonQuery(disableAllConstgrain);


        }
    }
}
