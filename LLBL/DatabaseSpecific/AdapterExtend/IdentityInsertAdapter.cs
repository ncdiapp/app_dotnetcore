using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using SD.LLBLGen.Pro.DQE.SqlServer;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace APP.LBL.DatabaseSpecific
{
    /// <summary>
    /// Data access adapter class, which controls the complete database interaction with the database for all objects.
    /// SqlServer specific.
    /// </summary>
    /// <remarks>
    /// Use a DataAccessAdapter object solely per thread, and per connection. A DataAccessAdapter object contains 1 active connection
    /// and no thread-access scheduling code. This means that you need to create a new DataAccessAdapter object if you want to utilize
    /// in another thread a new connection and a new transaction or want to open a new connection.
    /// </remarks>
    public class IdentityInsertAdapter : DataAccessAdapter
    {
         public IdentityInsertAdapter() { }







        private IFieldPersistenceInfo ResetIdentity(IFieldPersistenceInfo info)
        {


            IFieldPersistenceInfo i = new FieldPersistenceInfo(info.SourceSchemaName,
                info.SourceObjectName,
                info.SourceColumnName,
                info.SourceColumnIsNullable,
                info.SourceColumnDbType,
                info.SourceColumnMaxLength, 
                info.SourceColumnScale, 
                info.SourceColumnPrecision, 
               // info.IsIdentity,  
               false,
                info.IdentityValueSequenceName, 
                info.TypeConverterToUse, 
                info.ActualDotNetType);

            return i;
        }


        public static void SetIdentityInserts(string tableName, bool status)
        {
            SetIdentityInserts(tableName, status, false);
        }
        private static void SetIdentityInserts(string tableName, bool status, bool reseed)
        {


            using (DataAccessAdapter aDataAccessAdapter = new DataAccessAdapter())
            { 

                string queryTExt = string.Empty ;
                 if (reseed)
                {
                    queryTExt = String.Format("dbcc checkident ({0}, reseed, 1)", tableName);
                    //cmd.ExecuteNonQuery();
                }
                queryTExt = String.Format("set identity_insert {0} {1}", tableName, (status) ? "ON" : "OFF");



                aDataAccessAdapter.ExecuteExecuteNonQuery(queryTExt,null);
            
            }



            //SqlConnection conn = new SqlConnection(ConfigurationSettings.AppSettings["Main.ConnectionString"]);
            //SqlCommand cmd = conn.CreateCommand();

            //try
            //{
            //    conn.Open();

            //    if (reseed)
            //    {
            //        cmd.CommandText = String.Format("dbcc checkident ({0}, reseed, 1)", tableName);
            //        cmd.ExecuteNonQuery();
            //    }
            //    cmd.CommandText = String.Format("set identity_insert {0} {1}", tableName, (status) ? "ON" : "OFF");
            //    cmd.ExecuteNonQuery();
            //}
            //catch (Exception ex)
            //{
            //    //log.Error(ex.Message);
            //}
            //finally
            //{
            //    if (conn.State != ConnectionState.Closed)
            //        conn.Close();
            //}
        }

        
    }
}

//SetIdentityInserts("SomeTableName", true, true); // last value reseeds and is optional
//IdentityInsertAdapter iiAdapter = new IdentityInsertAdapter();
//iiAdapter.SaveEntity(someEntity);
//SetIdentityInserts("SomeTableName", false);

   //info.SourceSchemaName,
                            
   //             info.SourceObjectName,
   //             info.SourceColumnName,
   //             info.SourceColumnIsNullable,
   //             info.SourceColumnDbType,
   //             info.SourceColumnMaxLength,
   //             info.SourceColumnScale,
   //             info.SourceColumnPrecision,
   //             false,
   //             info.IdentityValueSequenceName,
   //             info.TypeConverterToUse