using APP.LBL.DatabaseSpecific;

namespace App.BL
{
    internal static class AppMasterAdapterBL
    {
        internal static DataAccessAdapter GetMasterAdapter()
            => new DataAccessAdapter(AppCompanyBL.AppMasterDBConnectionString);
    }
}
