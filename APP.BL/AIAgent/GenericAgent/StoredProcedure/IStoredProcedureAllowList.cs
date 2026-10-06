namespace App.BL.AIAgent.GenericAgent.StoredProcedure
{
    /// <summary>
    /// Hook for later whitelist. P0 always allows every SP.
    /// </summary>
    public interface IStoredProcedureAllowList
    {
        bool IsAllowed(int dataSourceId, string schema, string procedureName);
    }
}
