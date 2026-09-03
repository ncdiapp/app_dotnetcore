using App.BL.DbGenie;
using App.BL.GenericAgent;
using APP.Components.EntityDto;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Resolves LLM provider + credentials for the generic agent.
    /// Uses tenant settings (AIConfigSettingBL) as the single source of truth.
    /// The generic agent uses direct-HTTP calls (same as AppBuilderAgentBL),
    /// not Semantic Kernel — this helper centralises provider/key resolution.
    /// </summary>
    public static class KernelProviderHelper
    {
        public static EmLLMProvider GetProvider()
            => LLMProviderHelper.GetConfiguredProvider();

        public static string GetApiKey()
            => LLMProviderHelper.GetConfiguredApiKey();

        public static string GetModel()
            => AIConfigSettingBL.GetModel();
    }
}
