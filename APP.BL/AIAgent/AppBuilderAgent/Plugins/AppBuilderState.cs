using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Shared session state and validation helpers for AppBuilder agent plugins.
    /// </summary>
    internal static class AppBuilderState
    {
        private static readonly ConcurrentDictionary<int, bool> _createdPackageIds = new();

        internal static void RegisterCreatedPackage(int packageId)
            => _createdPackageIds[packageId] = true;

        internal static bool IsKnownCreatedPackage(int packageId)
            => _createdPackageIds.ContainsKey(packageId);
    }

    /// <summary>
    /// Shared saasApplicationId validation used by all AppBuilder plugin tools.
    /// </summary>
    internal static class AppBuilderValidation
    {
        /// <summary>
        /// Returns null if saasApplicationId is a valid user-created package;
        /// returns an error JSON string to return directly from the calling tool otherwise.
        /// </summary>
        internal static string ValidateSaasApplicationId(int saasApplicationId)
        {
            if (saasApplicationId <= 0)
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    Error = "saasApplicationId is required. Call create_app_package first and pass the returned SaasApplicationId."
                });

            // Accept packages created in this server session (not yet in cache).
            if (AppBuilderState.IsKnownCreatedPackage(saasApplicationId))
                return null;

            var pkg = AppSaasUserApplicationPackageBL.RetrieveSelectedApplicationPackages()
                .FirstOrDefault(a => a.Id != null && (int)a.Id == saasApplicationId
                                     && a.AppCreatedByCompanyId.HasValue);
            if (pkg == null)
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    Error = $"saasApplicationId={saasApplicationId} is not a valid user-created application package. " +
                            "Call create_app_package to create one and use the returned SaasApplicationId."
                });

            return null;
        }

        // Nullable overload for TransactionBuilderPlugin which uses int?
        internal static string ValidateSaasApplicationId(int? saasApplicationId)
        {
            if (!saasApplicationId.HasValue)
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    Error = "saasApplicationId is required. Call create_app_package first and pass the returned SaasApplicationId."
                });
            return ValidateSaasApplicationId(saasApplicationId.Value);
        }
    }
}
