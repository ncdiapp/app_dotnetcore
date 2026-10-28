#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Legacy compatibility facade for callers that still reference the old
    /// GenericAgentEngine type. New code should depend on SharedAgentEngine.
    /// </summary>
    [System.Obsolete("Use SharedAgentEngine.")]
    public static class GenericAgentEngine
    {
        public static Task RunAsync(
            string skillKey,
            string userMessage,
            List<JObject> chatHistory,
            GenericAgentCallbacks callbacks,
            AppClientIdentity? identity,
            CancellationToken ct,
            string workflowId = null,
            string chatSessionKey = null) =>
            SharedAgentEngine.RunAsync(
                skillKey,
                userMessage,
                chatHistory,
                callbacks,
                identity,
                ct,
                workflowId,
                chatSessionKey);

        public static Task<string> CompleteOnceAsync(
            AppClientIdentity? identity,
            string systemPrompt,
            string userPrompt,
            CancellationToken ct) =>
            SharedAgentEngine.CompleteOnceAsync(identity, systemPrompt, userPrompt, ct);
    }
}
