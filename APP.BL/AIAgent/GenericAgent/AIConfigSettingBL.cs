using APP.Components.Dto;
using App.BL;

namespace App.BL.GenericAgent
{
    /// <summary>
    /// Reads LLM provider config exclusively from tenant settings (AppTenantSetting).
    /// No appsettings.json fallback — each tenant must supply their own API keys.
    /// </summary>
    public static class AIConfigSettingBL
    {
        public static string GetProvider()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigProvider))
               ?? "Gemini";

        // Returns the API key for the currently active provider.
        public static string GetApiKey()
        {
            var provider = GetProvider().ToLowerInvariant();
            return provider switch
            {
                "openai"    => GetOpenAIApiKey(),
                "anthropic" => GetAnthropicApiKey(),
                _           => GetGeminiApiKey(),
            };
        }

        public static string GetOpenAIApiKey()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigOpenAIApiKey))
               ?? string.Empty;

        public static string GetGeminiApiKey()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigGeminiApiKey))
               ?? string.Empty;

        public static string GetAnthropicApiKey()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigAnthropicApiKey))
               ?? string.Empty;

        // Returns the model name for the currently active provider.
        public static string GetModel()
        {
            var provider = GetProvider().ToLowerInvariant();
            return provider switch
            {
                "openai"    => GetOpenAIModel(),
                "anthropic" => GetAnthropicModel(),
                _           => GetGeminiModel(),
            };
        }

        public static string GetOpenAIModel()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigOpenAIModel))
               ?? "gpt-4o";

        public static string GetGeminiModel()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigGeminiModel))
               ?? "gemini-2.0-flash";

        public static string GetAnthropicModel()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigAnthropicModel))
               ?? "claude-3-5-sonnet-20241022";

        // ── Background-thread overloads (pass identity instead of using ServerContext) ──

        public static string GetProvider(AppClientIdentity identity)
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigProvider, identity))
               ?? "Gemini";

        public static string GetApiKey(AppClientIdentity identity)
        {
            var provider = GetProvider(identity).ToLowerInvariant();
            return provider switch
            {
                "openai"    => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigOpenAIApiKey, identity)) ?? string.Empty,
                "anthropic" => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigAnthropicApiKey, identity)) ?? string.Empty,
                _           => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigGeminiApiKey, identity)) ?? string.Empty,
            };
        }

        public static string GetModel(AppClientIdentity identity)
        {
            var provider = GetProvider(identity).ToLowerInvariant();
            return provider switch
            {
                "openai"    => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigOpenAIModel, identity)) ?? "gpt-4o",
                "anthropic" => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigAnthropicModel, identity)) ?? "claude-3-5-sonnet-20241022",
                _           => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigGeminiModel, identity)) ?? "gemini-2.0-flash",
            };
        }

        private static string NonEmpty(string s)
            => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
