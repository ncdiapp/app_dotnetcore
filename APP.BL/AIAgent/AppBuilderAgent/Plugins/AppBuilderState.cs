using System.Collections.Concurrent;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Shared session state for the AppBuilder agent plugins.
    /// Tracks package IDs created in this process lifetime so ValidateSaasApplicationId
    /// can accept them before the application package cache refreshes.
    /// </summary>
    internal static class AppBuilderState
    {
        // Package IDs created by create_app_package in this server session.
        // ConvertedSet thread-safe: multiple concurrent agent runs are fine.
        private static readonly ConcurrentDictionary<int, bool> _createdPackageIds = new();

        internal static void RegisterCreatedPackage(int packageId)
            => _createdPackageIds[packageId] = true;

        internal static bool IsKnownCreatedPackage(int packageId)
            => _createdPackageIds.ContainsKey(packageId);
    }
}
