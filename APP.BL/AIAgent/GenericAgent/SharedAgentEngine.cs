#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Canonical shared runtime facade for General and Specialist Agents.
    ///
    /// The implementation currently remains in GenericAgentEngine for
    /// compatibility with existing integrations. New code should depend on
    /// SharedAgentEngine; the legacy implementation can be migrated behind
    /// this facade in a later step.
    /// </summary>
    public static class SharedAgentEngine
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
            GenericAgentEngine.RunAsync(
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
            GenericAgentEngine.CompleteOnceAsync(identity, systemPrompt, userPrompt, ct);
    }
}
