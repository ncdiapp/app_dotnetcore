using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Top-level entry point for the generic agent framework.
    /// Called by GenericAgentController with a fire-and-forget pattern.
    /// Multi-turn: callers pass chatHistory built from prior session messages.
    /// </summary>
    public static class GenericAgentBL
    {
        public static async Task RunAsync(
            string                skillKey,
            string                userMessage,
            List<JObject>         chatHistory,
            GenericAgentCallbacks callbacks,
            AppClientIdentity?    identity,
            CancellationToken     ct)
        {
            if (string.IsNullOrWhiteSpace(skillKey))
            {
                await SafeOnError(callbacks, "SkillKey is required.").ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                await SafeOnError(callbacks, "UserMessage is required.").ConfigureAwait(false);
                return;
            }

            try
            {
                await GenericAgentEngine.RunAsync(
                    skillKey, userMessage,
                    chatHistory ?? new List<JObject>(),
                    callbacks, identity, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await SafeOnError(callbacks, "Agent run was cancelled.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeOnError(callbacks, "Agent error: " + ex.Message).ConfigureAwait(false);
            }
        }

        private static async Task SafeOnError(GenericAgentCallbacks callbacks, string message)
        {
            if (callbacks?.OnError == null) return;
            try { await callbacks.OnError(message).ConfigureAwait(false); } catch { }
        }
    }
}
