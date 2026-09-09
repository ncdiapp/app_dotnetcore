using APP.Components.Dto;
using App.BL;

namespace App.BL.GenericAgent
{
    /// <summary>
    /// Reads LLM / Cursor config exclusively from tenant settings (AppTenantSetting).
    /// No appsettings.json fallback for AIConfig* keys — each tenant must supply their own API keys.
    /// AIConfigCursorApiKey is tenant-only. AIConfigCursorModel / AIConfigCursorMcpPublicBaseUrl may fall back via CursorCloudAgentConfig.
    /// </summary>
    public static class AIConfigSettingBL
    {
        public static string GetDefaultProvider()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigDefaultProvider))
               ?? "Gemini";

        /// <summary>Backward-compatible alias for GetDefaultProvider(). </summary>
        public static string GetProvider() => GetDefaultProvider();

        public static string GetImageProcessProvider()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigImageProcessProvider))
               ?? GetDefaultProvider();

        /// <summary>
        /// Integration engine: Cursor | Cloud | OpenAI | Gemini | Anthropic.
        /// </summary>
        public static string GetIntegrationProvider()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigIntegrationProvider))
               ?? "Cursor";

        public static string GetApiKey() => GetApiKeyForProvider(GetDefaultProvider());

        public static string GetApiKeyForProvider(string provider)
        {
            switch ((provider ?? "").Trim().ToLowerInvariant())
            {
                case "openai": return GetOpenAIApiKey();
                case "anthropic": return GetAnthropicApiKey();
                case "gemini": return GetGeminiApiKey();
                default: return GetGeminiApiKey();
            }
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

        public static string GetModel() => GetModelForProvider(GetDefaultProvider());

        public static string GetModelForProvider(string provider)
        {
            switch ((provider ?? "").Trim().ToLowerInvariant())
            {
                case "openai": return GetOpenAIModel();
                case "anthropic": return GetAnthropicModel();
                case "gemini": return GetGeminiModel();
                default: return GetGeminiModel();
            }
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

        public static string GetImageProcessApiKey()
            => GetApiKeyForProvider(GetImageProcessProvider());

        public static string GetImageProcessModel()
            => GetModelForProvider(GetImageProcessProvider());

        public static string GetCursorApiKey()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorApiKey))
               ?? string.Empty;

        /// <summary> Empty tenant value → "auto". </summary>
        public static string GetCursorModel()
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorModel))
               ?? "auto";

        // ── Background-thread overloads (pass identity instead of using ServerContext) ──

        public static string GetDefaultProvider(AppClientIdentity identity)
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigDefaultProvider, identity))
               ?? "Gemini";

        public static string GetProvider(AppClientIdentity identity) => GetDefaultProvider(identity);

        public static string GetImageProcessProvider(AppClientIdentity identity)
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigImageProcessProvider, identity))
               ?? GetDefaultProvider(identity);

        public static string GetIntegrationProvider(AppClientIdentity identity)
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigIntegrationProvider, identity))
               ?? "Cursor";

        public static string GetApiKey(AppClientIdentity identity)
            => GetApiKeyForProvider(GetDefaultProvider(identity), identity);

        public static string GetApiKeyForProvider(string provider, AppClientIdentity identity)
        {
            switch ((provider ?? "").Trim().ToLowerInvariant())
            {
                case "openai":
                    return NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigOpenAIApiKey, identity)) ?? string.Empty;
                case "anthropic":
                    return NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigAnthropicApiKey, identity)) ?? string.Empty;
                default:
                    return NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigGeminiApiKey, identity)) ?? string.Empty;
            }
        }

        public static string GetModel(AppClientIdentity identity)
            => GetModelForProvider(GetDefaultProvider(identity), identity);

        public static string GetModelForProvider(string provider, AppClientIdentity identity)
        {
            switch ((provider ?? "").Trim().ToLowerInvariant())
            {
                case "openai":
                    return NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigOpenAIModel, identity)) ?? "gpt-4o";
                case "anthropic":
                    return NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigAnthropicModel, identity)) ?? "claude-3-5-sonnet-20241022";
                default:
                    return NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigGeminiModel, identity)) ?? "gemini-2.0-flash";
            }
        }

        public static string GetCursorApiKey(AppClientIdentity identity)
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorApiKey, identity))
               ?? string.Empty;

        public static string GetCursorModel(AppClientIdentity identity)
            => NonEmpty(AppTenantSettingBL.GetStringValue(EmTenantSettings.AIConfigCursorModel, identity))
               ?? "auto";

        private static string NonEmpty(string s)
            => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
