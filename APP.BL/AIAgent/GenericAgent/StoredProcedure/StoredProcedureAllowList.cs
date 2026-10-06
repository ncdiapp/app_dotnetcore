namespace App.BL.AIAgent.GenericAgent.StoredProcedure
{
    /// <summary>P0: allow all. Replace or wrap when catalog whitelist ships.</summary>
    public sealed class AllowAllStoredProcedureAllowList : IStoredProcedureAllowList
    {
        public static readonly AllowAllStoredProcedureAllowList Instance = new AllowAllStoredProcedureAllowList();

        public bool IsAllowed(int dataSourceId, string schema, string procedureName) => true;
    }
}
