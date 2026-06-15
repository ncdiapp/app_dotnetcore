
namespace DatabaseSchemaMrg.DataSchema
{
    /// <summary>
    /// Database platform types supported for generating SQL.
    /// </summary>
    public enum EmSqlType
    {
        /// <summary>
        /// Microsoft SQL Server (2005, 2008, 2008 R2) including Express versions
        /// </summary>
        SqlServer=1,
        /// <summary>
        /// Oracle platforms (Oracle 9- 11, including XE)
        /// </summary>
        Oracle=2,
        /// <summary>
        /// MySQL (v5 onwards as we assume support for stored procedures)
        /// </summary>
        MySql=3,
      
        /// <summary>
        /// PostgreSql
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Postgre")]
        PostgreSql=4,
        /// <summary>
        /// IBM DB2
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Db")]
        Db2=5,


		/// <summary>
		/// SQLite
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lite")]

		SQLite=6,
		///<summary>
		/// Microsoft SQL Server CE 4
		///</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ce")]
		SqlServerCe=7,
	}
}
