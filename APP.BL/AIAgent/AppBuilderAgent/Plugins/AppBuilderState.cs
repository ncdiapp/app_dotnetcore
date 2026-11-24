using System;
using System.Collections.Concurrent;
using System.Linq;
using App.BL.AIAgent.GenericAgent;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    /// When My Application Editor (or any host) supplies HostContext.saasApplicationId,
    /// App Builder must reuse that package — never create_app_package a sibling app.
    /// </summary>
    internal static class AppBuilderHostApp
    {
        internal static bool TryGetHostSaasApplicationId(
            AgentToolContext context,
            out int saasApplicationId,
            out string applicationName)
        {
            saasApplicationId = 0;
            applicationName = null;
            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            if (string.IsNullOrWhiteSpace(hostJson))
                return false;

            try
            {
                var root = JObject.Parse(hostJson);
                var id = (root["saasApplicationId"] ?? root["SaasApplicationId"])?.Value<int?>();
                if (id == null || id.Value <= 0)
                    return false;

                saasApplicationId = id.Value;
                applicationName = (root["applicationName"] ?? root["ApplicationName"])?.Value<string>();
                AppBuilderState.RegisterCreatedPackage(saasApplicationId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prefer HostContext app id when present; otherwise keep the LLM-supplied id.
        /// </summary>
        internal static int? ResolveSaasApplicationId(int? requested, AgentToolContext context, out string note)
        {
            note = null;
            if (!TryGetHostSaasApplicationId(context, out var hostId, out var hostName))
                return requested;

            if (requested == null || requested.Value <= 0)
            {
                note = $"Using HostContext.saasApplicationId={hostId}"
                    + (string.IsNullOrWhiteSpace(hostName) ? "" : $" ({hostName})")
                    + " — do not create_app_package.";
                return hostId;
            }

            if (requested.Value != hostId)
            {
                note =
                    $"Ignored saasApplicationId={requested.Value}; forced HostContext.saasApplicationId={hostId}"
                    + (string.IsNullOrWhiteSpace(hostName) ? "" : $" ({hostName})")
                    + ". Do not create a new application package while editing an existing app.";
                return hostId;
            }

            return hostId;
        }
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
