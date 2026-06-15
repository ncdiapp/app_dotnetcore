using System;
using System.Linq;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    public class SchemaBuilderPlugin
    {
        private readonly int?   _dataSourceId;
        private readonly string _schemaOwner;

        public SchemaBuilderPlugin(int? dataSourceId = null, string schemaOwner = "dbo")
        {
            _dataSourceId = dataSourceId;
            _schemaOwner  = schemaOwner ?? "dbo";
        }

        [AgentFunction("get_table_schema",
            "Get column definitions (name, data type, nullable, primary key) for a specific database table.")]
        public string GetTableSchema(
            [AgentParam("Table name to inspect", isRequired: true)] string tableName,
            [AgentParam("Schema owner, e.g. 'dbo'")] string schemaOwner = "dbo")
        {
            try
            {
                var owner = schemaOwner ?? _schemaOwner;
                var table = AppMetaDataBL.GetOneDatabaseTableSchema(tableName, _dataSourceId, owner);

                if (table == null)
                    return JsonConvert.SerializeObject(new { Error = $"Table '{tableName}' not found" });

                return JsonConvert.SerializeObject(new
                {
                    TableName   = table.Name,
                    SchemaOwner = owner,
                    Columns     = table.Columns?.Select(c => new
                    {
                        c.Name,
                        DataType        = c.DbDataType,
                        IsNullable      = c.Nullable,
                        c.IsPrimaryKey,
                        IsAutoIncrement = c.IsAutoNumber,
                        MaxLength       = c.Length
                    }).ToList()
                }, Formatting.Indented);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }

        [AgentFunction("create_database_table",
            "Execute a SQL CREATE TABLE statement to create a new table in the database. " +
            "WARNING: Do NOT use this after create_application — create_application already creates all tables. " +
            "Only use this when explicitly asked to create a specific table manually.")]
        public string CreateDatabaseTable(
            [AgentParam("The full CREATE TABLE SQL script", isRequired: true)] string createTableSql)
        {
            try
            {
                var result = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL                  = createTableSql,
                    DataSourceRegisterId = _dataSourceId,
                    RequireConfirmation  = false,
                    IsConfirmed          = true
                });

                return JsonConvert.SerializeObject(new
                {
                    result.IsSuccess, result.Error, result.RowsAffected
                }, Formatting.Indented);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message }); }
        }
    }
}
