using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
#if NETFRAMEWORK
using System.Configuration;
#endif

namespace DatabaseSchemaMrg.DataSchema
{
    /// <summary>
    /// The parent of all schema objects.
    /// </summary>
    /// <remarks>
    /// When initially populated, many of the objects (tables, stored procedures) are not linked.
    /// Use <see cref="DatabaseSchemaFixer.UpdateReferences" /> to link things up
    /// </remarks>
    [Serializable]
    public partial class DatabaseSchema
    {
        #region Fields
        //backing fields
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabaseTable> _tables;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabaseView> _views;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DataType> _dataTypes;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabaseStoredProcedure> _storedProcedures;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabasePackage> _packages;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabaseSequence> _sequences;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabaseFunction> _functions;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<DatabaseUser> _users;
		#endregion


		private static readonly Dictionary<string, string> _DictServerTypePorviderName = new Dictionary<string, string>();


   //<add key = "SqlServer" value="System.Data.SqlClient" />
   // <add key = "Oracle" value="Oracle.ManagedDataAccess.Client" />
   // <add key = "MySql" value="MySql.Data.MySqlClient" />
		static DatabaseSchema()
		{
			// Initialize with default provider names
			_DictServerTypePorviderName.Add(EmSqlType.SqlServer.ToString(), GetProviderNameFromConfig(EmSqlType.SqlServer, "System.Data.SqlClient"));
			_DictServerTypePorviderName.Add(EmSqlType.Oracle.ToString(), GetProviderNameFromConfig(EmSqlType.Oracle, "Oracle.ManagedDataAccess.Client"));
			_DictServerTypePorviderName.Add(EmSqlType.MySql.ToString(), GetProviderNameFromConfig(EmSqlType.MySql, "MySql.Data.MySqlClient"));
			_DictServerTypePorviderName.Add(EmSqlType.PostgreSql.ToString(), GetProviderNameFromConfig(EmSqlType.PostgreSql, "Npgsql"));
			_DictServerTypePorviderName.Add(EmSqlType.Db2.ToString(), GetProviderNameFromConfig(EmSqlType.Db2, "IBM.Data.DB2"));
		}

		/// <summary>
		/// Gets the provider name from configuration if available, otherwise returns the default.
		/// </summary>
		private static string GetProviderNameFromConfig(EmSqlType sqlType, string defaultValue)
		{
#if NETFRAMEWORK
			return ConfigurationManager.AppSettings[sqlType.ToString()] ?? defaultValue;
#else
			return defaultValue;
#endif
		}

		/// <summary>
		/// Configures a provider name for a specific SQL type. Use this method in .NET Standard
		/// to override the default provider names, or when app.config is not available.
		/// </summary>
		/// <param name="sqlType">The SQL type to configure.</param>
		/// <param name="providerName">The provider name to use.</param>
		public static void ConfigureProvider(EmSqlType sqlType, string providerName)
		{
			if (string.IsNullOrEmpty(providerName))
				throw new ArgumentNullException(nameof(providerName));

			var key = sqlType.ToString();
			if (_DictServerTypePorviderName.ContainsKey(key))
			{
				_DictServerTypePorviderName[key] = providerName;
			}
			else
			{
				_DictServerTypePorviderName.Add(key, providerName);
			}
		}

		public static EmSqlType GetSqlTypeByProvideName(string providerName)
		{
			
			if(providerName.IndexOf ("Sqlserver", StringComparison.CurrentCultureIgnoreCase) != -1)
			{
				return EmSqlType.SqlServer;
			}

			if (providerName.IndexOf("Oracle", StringComparison.CurrentCultureIgnoreCase) != -1)
			{
				return EmSqlType.Oracle;
			}
			if (providerName.IndexOf("MySql", StringComparison.CurrentCultureIgnoreCase) != -1)
			{
				return EmSqlType.MySql;
			}
			if (providerName.IndexOf("PostgreSql", StringComparison.CurrentCultureIgnoreCase) != -1)
			{
				return EmSqlType.PostgreSql;
			}
			if (providerName.IndexOf("Db2", StringComparison.CurrentCultureIgnoreCase) != -1)
			{
				return EmSqlType.Db2;
			}

			return EmSqlType.SqlServer;
		}


		public static string GetProvideNameBySqlType(EmSqlType emSqlType)
		{


			return _DictServerTypePorviderName[emSqlType.ToString()];

		}

		/// <summary>
		///  need to set  ConnectionString and  Provider after default instance creation !
		/// </summary>
		public DatabaseSchema()
            //: this(null, null)
        {
			_packages = new List<DatabasePackage>();
			_views = new List<DatabaseView>();
			_users = new List<DatabaseUser>();
			_sequences = new List<DatabaseSequence>();
			_functions = new List<DatabaseFunction>();
			_tables = new List<DatabaseTable>();
			_storedProcedures = new List<DatabaseStoredProcedure>();
			_dataTypes = new List<DataType>();

			// 
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseSchema"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="sqlType">Type of the provider</param>
		public DatabaseSchema(string connectionString, EmSqlType sqlType)
		{

			Provider = _DictServerTypePorviderName[sqlType.ToString()];
			if(string.IsNullOrWhiteSpace (Provider))
			{
				throw new   ArgumentException  ("Cannot find Provider "+ sqlType.ToString () + " need to setup Db provider in App.config "); 
			}


			ConnectionString = connectionString;
			_packages = new List<DatabasePackage>();
			_views = new List<DatabaseView>();
			_users = new List<DatabaseUser>();
			_sequences = new List<DatabaseSequence>();
			_functions = new List<DatabaseFunction>();
			_tables = new List<DatabaseTable>();
			_storedProcedures = new List<DatabaseStoredProcedure>();
			_dataTypes = new List<DataType>();

		

		
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseSchema"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="providerName">Name of the provider.</param>
		//public DatabaseSchema(string connectionString, string providerName)
  //      {
          
  //          Provider = providerName;

		//	ConnectionString = connectionString;
		//	_packages = new List<DatabasePackage>();
  //          _views = new List<DatabaseView>();
  //          _users = new List<DatabaseUser>();
  //          _sequences = new List<DatabaseSequence>();
  //          _functions = new List<DatabaseFunction>();
  //          _tables = new List<DatabaseTable>();
  //          _storedProcedures = new List<DatabaseStoredProcedure>();
  //          _dataTypes = new List<DataType>();
  //      }

        /// <summary>
        /// Gets the data types.
        /// </summary>
        public List<DataType> DataTypes { get { return _dataTypes; } }

        /// <summary>
        /// Gets the stored procedures.
        /// </summary>
        public List<DatabaseStoredProcedure> StoredProcedures { get { return _storedProcedures; } }

        /// <summary>
        /// Gets the packages.
        /// </summary>
        public List<DatabasePackage> Packages { get { return _packages; } }

        /// <summary>
        /// Gets the tables.
        /// </summary>
        public List<DatabaseTable> Tables { get { return _tables; } }

        /// <summary>
        /// Gets the views.
        /// </summary>
        public List<DatabaseView> Views { get { return _views; } }

        /// <summary>
        /// Gets the users.
        /// </summary>
        public List<DatabaseUser> Users { get { return _users; } }

        /// <summary>
        /// Gets the functions.
        /// </summary>
        public List<DatabaseFunction> Functions { get { return _functions; } }

        /// <summary>
        /// Gets the sequences.
        /// </summary>
        public List<DatabaseSequence> Sequences { get { return _sequences; } }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        /// <value>
        /// The provider.
        /// </value>
        public string Provider { get; set; }
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        public string Owner { get; set; }

        /// <summary>
        /// Finds a table by name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public DatabaseTable FindTableByName(string name)
        {
            return Tables.Find(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }


        public Dictionary<string, DatabaseTable> _DictDatabaseTable;
        public Dictionary<string, DatabaseTable> DictDatabaseTable
        {
            get
            {
                if (_DictDatabaseTable == null)
                {
                    _DictDatabaseTable = Tables.ToDictionary(o => o.Name, o => o);
                    return _DictDatabaseTable;
                }
                else
                {
                    return _DictDatabaseTable;
                }




            }
        }


        /// <summary>
        /// Finds a table by name and schema
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        public DatabaseTable FindTableByName(string name, string schema)
        {
            return Tables.Find(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.SchemaOwner, schema, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Tables: {0}, Views: {1}, StoredProcedures: {2}", Tables.Count, Views.Count, StoredProcedures.Count);
        }
    }
}
