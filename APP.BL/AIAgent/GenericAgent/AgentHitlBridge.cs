using System;
using System.Collections.Concurrent;
using System.Threading;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Bridge so BuiltIn ask_user / data_render (and other HITL/UI tools) can reach the
    /// run's <see cref="GenericAgentCallbacks"/> without putting DTO-typed Funcs on
    /// <c>AgentToolContext</c> in APP.Framework.
    /// Prefer <see cref="Current"/> (AsyncLocal). Also keep a session-keyed fallback because
    /// some tool dispatch paths lose AsyncLocal across threads — otherwise row-limit ask never fires.
    /// </summary>
    public static class AgentHitlBridge
    {
        private static readonly AsyncLocal<GenericAgentCallbacks> CurrentLocal = new();
        private static readonly ConcurrentDictionary<string, GenericAgentCallbacks> BySession =
            new ConcurrentDictionary<string, GenericAgentCallbacks>(StringComparer.OrdinalIgnoreCase);

        public static GenericAgentCallbacks Current
        {
            get => CurrentLocal.Value;
            set => CurrentLocal.Value = value;
        }

        public static void BindSession(string chatSessionKey, GenericAgentCallbacks callbacks)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || callbacks == null) return;
            BySession[chatSessionKey.Trim()] = callbacks;
        }

        public static void UnbindSession(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            BySession.TryRemove(chatSessionKey.Trim(), out _);
        }

        /// <summary>AsyncLocal first, then session fallback.</summary>
        public static GenericAgentCallbacks Resolve(string chatSessionKey = null)
        {
            if (CurrentLocal.Value != null) return CurrentLocal.Value;
            if (!string.IsNullOrWhiteSpace(chatSessionKey)
                && BySession.TryGetValue(chatSessionKey.Trim(), out var cb)
                && cb != null)
                return cb;
            return null;
        }
    }
}
