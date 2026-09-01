namespace App.BL.AppDataIntegrationAgent
{
    /// <summary>Resolved target for run_select / get_table_schema / propose_sql.</summary>
    public sealed class AppDataIntegrationAgentSqlTarget
    {
        public int? DataSourceRegisterId { get; set; }
        public string ConnectionString { get; set; }
        public bool UsesConnectionString => !string.IsNullOrWhiteSpace(ConnectionString);
    }
}
