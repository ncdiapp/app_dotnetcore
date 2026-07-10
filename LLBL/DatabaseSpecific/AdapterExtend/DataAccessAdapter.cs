using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Transactions;

using SD.LLBLGen.Pro.ORMSupportClasses;
using System.IO;
using System.Security.Cryptography;


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
    /// must change ReadConnectionStringFromConfig()

    /// <summary>
		/// Reads the value of the setting with the key ConnectionStringKeyName from the *.config file and stores that value as the
		/// active connection string to use for this object.
		/// </summary>
		/// <returns>connection string read</returns>
        //private string ReadConnectionStringFromConfig()
        //{
        //    return DataAccessAdapter.APPConnectionString;
        //    //return ConfigFileHelper.ReadConnectionStringFromConfig( DataAccessAdapter.ConnectionStringKeyName);
        //}


    public partial class DataAccessAdapter
 {
	
      //  public static readonly string ConnectionStringFromConfig = ""; // System.Configuration.ConfigurationManager.ConnectionStrings[APPConnectionConfigName].ConnectionString;


        static DataAccessAdapter()
		{
 //#if, along with the #else, #elif, #endif, #define, and #undef directives, lets you include or exclude code based on the existence of one or more symbols. This can be useful when compiling code for a debug build or when compiling for a specific configuration.
//#if DEBUG
		
//            APPConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DebugConnectionString"].ConnectionString;
//#else
			


            //var enableDebugScript = System.Configuration.ConfigurationManager.AppSettings["EnableDebugScript"];
            //if (!string.IsNullOrWhiteSpace(enableDebugScript) && enableDebugScript.Trim() == "1")
            //{
            //    ConnectionStringFromConfig = System.Configuration.ConfigurationManager.ConnectionStrings["DebugConnectionString"].ConnectionString;
            //}
            //else
            //{
            //    string encry = System.Configuration.ConfigurationManager.ConnectionStrings[APPConnectionConfigName].ConnectionString;
            //    ConnectionStringFromConfig = EnDeCrypt.Decrypt(encry);
            //}


		
			//#endif
		}


        public IFieldPersistenceInfo GetEntityFieldPersistenceInfo(IEntityField2 entityField2)
        {
            return PersistenceInfoProviderSingleton.GetInstance().GetFieldPersistenceInfo(entityField2.ContainingObjectName, entityField2.Name);
        }

       


        public IDataReader ExcuteAqueryDataReader(string aQuery, List<SqlParameter> listParamters)
        {
            var command = new SqlCommand(aQuery);
            if (listParamters != null)
            {
                foreach (var parameter in listParamters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            IDataReader reader = this.FetchDataReader(new RetrievalQuery(command), CommandBehavior.SingleResult);

            return reader;
        }

        public object ExecuteScalarQuery(string aQuery, List<SqlParameter> listParamters)
        {
            var command = new SqlCommand(aQuery);
            if (listParamters != null)
            {
                foreach (var parameter in listParamters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            var retrievalQuery = new RetrievalQuery(command);

            return this.ExecuteScalarQuery(new RetrievalQuery(command));
        }

        public DataTable ExecuteDataTableRetrievalQuery(string aQuery, List<SqlParameter> listParamters)
        {
            IDbConnection connect = this.GetActiveConnection();
            if (connect.State != ConnectionState.Open)
            {
                connect.Open();
            }


            SqlCommand command  = new SqlCommand(aQuery, connect as SqlConnection);

            if (this.IsTransactionInProgress && this.PhysicalTransaction != null)
            {
                command.Transaction = this.PhysicalTransaction as SqlTransaction;
            }

            if (listParamters != null)
            {
                foreach (var parameter in listParamters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            command.CommandType = CommandType.Text;
            DataTable toReturn = new DataTable();

            SqlDataAdapter adapter = new SqlDataAdapter(command);
            command.CommandTimeout = 120;
            adapter.Fill(toReturn);

            return toReturn;
        }

        public int ExecuteExecuteNonQuery(string aQuery, List<SqlParameter> listParamters)
        {
            SqlConnection connect = this.GetActiveConnection() as SqlConnection;
            if (connect.State != ConnectionState.Open)
            {
                connect.Open();
            }

            SqlCommand command = new SqlCommand(aQuery, connect as SqlConnection);

            // Required when StartTransaction() is active — otherwise ExecuteNonQuery throws.
            if (this.IsTransactionInProgress && this.PhysicalTransaction != null)
            {
                command.Transaction = this.PhysicalTransaction as SqlTransaction;
            }

            if (listParamters != null)
            {
                foreach (var parameter in listParamters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            return command.ExecuteNonQuery();
        }

        //  cmdToExecute.Parameters.Add(new SqlParameter("@iErrorCode", SqlDbType.Int, 4, ParameterDirection.Output, true, 10, 0, "", DataRowVersion.Proposed, _errorCode));

        public int CallStoreProc(string storProcName, Dictionary<string, object> aParamterValue)
        {
            SqlConnection connect = this.GetActiveConnection() as SqlConnection;

            if (connect.State != ConnectionState.Open)
            {
                connect.Open();
            }

            SqlCommand cmdToExecute = new SqlCommand();
            cmdToExecute.CommandText = string.Format("dbo.{0}", storProcName);
            cmdToExecute.CommandType = CommandType.StoredProcedure;

            // Use base class' connection object
            cmdToExecute.Connection = connect as SqlConnection;
            if (this.IsTransactionInProgress && this.PhysicalTransaction != null)
            {
                cmdToExecute.Transaction = this.PhysicalTransaction as SqlTransaction;
            }
            foreach (string paName in aParamterValue.Keys)
            {
                cmdToExecute.Parameters.Add(new SqlParameter(paName, aParamterValue[paName]));
            }

            return cmdToExecute.ExecuteNonQuery();
        }

        public int CallStoreProc(string storProcName, List<SqlParameter> paramtersList)
        {
            IDbConnection connect = this.GetActiveConnection();

            SqlCommand cmdToExecute = new SqlCommand();
            cmdToExecute.CommandText = string.Format("dbo.{0}", storProcName);
            cmdToExecute.CommandType = CommandType.StoredProcedure;

            // Use base class' connection object
            cmdToExecute.Connection = connect as SqlConnection;
            if (this.IsTransactionInProgress && this.PhysicalTransaction != null)
            {
                cmdToExecute.Transaction = this.PhysicalTransaction as SqlTransaction;
            }

            cmdToExecute.Parameters.AddRange(paramtersList.ToArray());

            return cmdToExecute.ExecuteNonQuery();
        }
    }

     class EnDeCrypt
    {
        private const string password = "A8F26F78-1187-4544-8FC4-BE825F707160";

        public static string Encrypt(string clearText)
        {
            // First we need to turn the input string into a byte array.

            byte[] clearBytes = System.Text.Encoding.Unicode.GetBytes(clearText);
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password,
                new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

            byte[] encryptedData = Encrypt(clearBytes, pdb.GetBytes(32), pdb.GetBytes(16));

            return Convert.ToBase64String(encryptedData);
        }

        public static string Decrypt(string cipherText)
        {
            // First we need to turn the input string into a byte array.

            // We presume that Base64 encoding was used

            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            PasswordDeriveBytes pdb = new PasswordDeriveBytes(password,
                new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

            byte[] decryptedData = Decrypt(cipherBytes, pdb.GetBytes(32), pdb.GetBytes(16));
            return System.Text.Encoding.Unicode.GetString(decryptedData);
        }

        private static byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
        {
            MemoryStream ms = new MemoryStream();
            Rijndael alg = Rijndael.Create();
            alg.Key = Key;
            alg.IV = IV;
            CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);

            cs.Write(clearData, 0, clearData.Length);

            cs.Close();

            byte[] encryptedData = ms.ToArray();

            return encryptedData;
        }

        private static byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
        {
            MemoryStream ms = new MemoryStream();
            Rijndael alg = Rijndael.Create();
            alg.Key = Key;
            alg.IV = IV;
            CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherData, 0, cipherData.Length);

            cs.Close();

            byte[] decryptedData = ms.ToArray();

            return decryptedData;
        }

        private static byte[] Decrypt(byte[] cipherData, string Password)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,

                new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });

            return Decrypt(cipherData, pdb.GetBytes(32), pdb.GetBytes(16));
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}