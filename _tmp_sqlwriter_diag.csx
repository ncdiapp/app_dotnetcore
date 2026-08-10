using System;
using System.Data.SqlClient;
using System.Linq;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;

var cs = @""Server=PC3B\MSSQLSERVER01;Database=TenantDB_PLM27;User ID=sa;Password=appsa;Encrypt=False;TrustServerCertificate=True;"";
var fixture = new DatabaseFixture(cs, EmSqlType.SqlServer);
// Find how to load one table - check API
Console.WriteLine(""types ok"");
