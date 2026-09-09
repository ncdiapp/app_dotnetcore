using System;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Per-agent runtime / LLM provider for Agent Management.
    /// Direct LLM: OpenAI | Gemini | Anthropic (in-process GenericAgentEngine).
    /// CursorCloudAgents: Cursor Cloud Agents API + session MCP exposing local tools.
    /// </summary>
    public static class AppAgentRuntimeProvider
    {
        public const string UseDefault = "";
        public const string OpenAI = "OpenAI";
        public const string Gemini = "Gemini";
        public const string Anthropic = "Anthropic";
        public const string CursorCloudAgents = "CursorCloudAgents";

        public static bool IsCursorCloudAgents(string provider)
            => string.Equals(Normalize(provider), CursorCloudAgents, StringComparison.OrdinalIgnoreCase);

        public static bool IsDirectLlm(string provider)
            => !IsCursorCloudAgents(provider);

        /// <summary>Stored value; empty means follow tenant default at runtime.</summary>
        public static string NormalizeStored(string provider)
        {
            var p = (provider ?? "").Trim();
            if (p.Length == 0)
                return UseDefault;

            if (p.Equals(CursorCloudAgents, StringComparison.OrdinalIgnoreCase)
                || p.Equals("Cursor", StringComparison.OrdinalIgnoreCase)
                || p.Equals("CursorCloud", StringComparison.OrdinalIgnoreCase)
                || p.Equals("CursorCloudAgent", StringComparison.OrdinalIgnoreCase))
                return CursorCloudAgents;

            if (p.Equals(OpenAI, StringComparison.OrdinalIgnoreCase))
                return OpenAI;
            if (p.Equals(Anthropic, StringComparison.OrdinalIgnoreCase)
                || p.Equals("Claude", StringComparison.OrdinalIgnoreCase))
                return Anthropic;
            if (p.Equals(Gemini, StringComparison.OrdinalIgnoreCase)
                || p.Equals("Google", StringComparison.OrdinalIgnoreCase))
                return Gemini;

            return UseDefault;
        }

        /// <summary>Resolve stored/UI value to an effective provider; empty → tenantDefault (or Gemini).</summary>
        public static string Normalize(string provider, string tenantDefault = null)
        {
            var p = NormalizeStored(provider);
            if (p.Length == 0)
                p = (tenantDefault ?? "").Trim();
            if (p.Length == 0)
                return Gemini;
            return NormalizeStored(p) == UseDefault ? Gemini : NormalizeStored(p);
        }
    }
}
