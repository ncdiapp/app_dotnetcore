using APP.LBL.DatabaseSpecific;

namespace App.BL
{
    internal static class AppMasterAdapterBL
    {
        internal static DataAccessAdapter GetMasterAdapter()
            => new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString);

        // Returns the connection string without creating an adapter — safe to capture on the
        // request thread and pass into Task.Run lambdas.
        internal static string GetMasterConnectionString()
            => AppCompanyBL.AppMasterDBConnectionString;
    }
}
