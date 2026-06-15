using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema;

namespace ExchangeBL
{
    /// <summary>
    ///
    /// </summary>
    public class TableNameParameterDTO
    {
        /// <summary>
        /// table name
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// json instance
        /// </summary>
        public JsonSchema JsonData { get; set; }
        /// <summary>
        /// structure on database
        /// </summary>
        public TSqlTableModel TableSchema { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public enum EnumSqlScriptCRUDType
    {
        Select = 1,
        Insert = 2,
        //Delete = 3,
        UpdateKey =5,
    }

    /// <summary>
    ///
    /// </summary>
    public enum EnumDbBasedSqlScriptCRUDType
    {
        SqlServerSelect = 1,
        SqlServerInsert = 2,
        //SqlServerDelete = 3,
        SqlServerUpdateKey = 5,

        MySqlSelect = 11,
        MySqlInsert = 12,
        //MySqlDelete = 13,
        MySqlUpdateKey = 15,

        OracleSelect = 21,
        OracleInsert = 22,
        //OracleDelete = 23,
        OracleUpdateKey = 25,
    }


    /// <summary>
    ///
    /// </summary>
    public enum EnumDbBasedSqlTableCreationScriptType
    {
        SqlServerTable = 1,
        MySqlTable = 2,
        OracleTable = 3,

    }


    public enum EnumJsonSqlServerType
    {
        /// <summary>
        /// Microsoft SQL Server (2005, 2008, 2008 R2) including Express versions
        /// </summary>
        SqlServer = 1,
        /// <summary>
        /// Oracle platforms (Oracle 9- 11, including XE)
        /// </summary>
        Oracle = 2,
        /// <summary>
        /// MySQL (v5 onwards as we assume support for stored procedures)
        /// </summary>
        MySql = 3,

        /// <summary>
        /// PostgreSql
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Postgre")]
        PostgreSql = 4,
        /// <summary>
        /// IBM DB2
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Db")]
        Db2 = 5,


        /// <summary>
        /// SQLite
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lite")]

        SQLite = 6,
        ///<summary>
        /// Microsoft SQL Server CE 4
        ///</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ce")]
        SqlServerCe = 7,
    }
}
