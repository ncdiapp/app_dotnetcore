using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.EntityDto;

namespace App.BL.AppBuilderAgent
{
    /// <summary>
    /// In-memory event queue for the polling-based agent streaming.
    ///
    /// Flow:
    ///   1. Controller calls CreateSession() → gets sessionId
    ///   2. Agent callbacks enqueue events via Enqueue()
    ///   3. React client polls GET /PollEvents?sessionId=... → DequeueAll()
    ///   4. Sessions auto-expire after 30 minutes
    ///
    /// Plan-confirm flow (added):
    ///   a. propose_plan tool calls OnPlanReady callback in the controller
    ///   b. Controller enqueues a "plan" event AND calls RegisterPlanConfirmation(sessionId)
    ///      which returns a TaskCompletionSource{bool} that the callback awaits
    ///   c. React UI receives the "plan" event and prompts the user
    ///   d. User clicks Approve/Reject → React POSTs to /ConfirmPlan
    ///   e. Controller calls ConfirmPlan(sessionId, confirmed) which resolves the TCS
    ///   f. The awaited Task{bool} completes, propose_plan returns result to LLM
    /// </summary>
    public static class AppBuilderAgentSessionStore
    {
        private static readonly ConcurrentDictionary<string, SessionData> Sessions
            = new ConcurrentDictionary<string, SessionData>();

        // Pending plan confirmations: sessionId → TCS resolved by /ConfirmPlan
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> PendingConfirmations
            = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        // Pending schema confirmations: sessionId → TCS resolved by /ConfirmSchema
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<AgentSchemaResponse>> PendingSchemaConfirmations
            = new ConcurrentDictionary<string, TaskCompletionSource<AgentSchemaResponse>>();

        public class SessionData
        {
            public ConcurrentQueue<AgentEventDto> Events = new ConcurrentQueue<AgentEventDto>();
            public DateTime CreatedAt = DateTime.UtcNow;
        }

        public static string CreateSession()
        {
            var id = Guid.NewGuid().ToString("N");
            Sessions[id] = new SessionData();
            CleanExpired();
            return id;
        }

        public static void Enqueue(string sessionId, AgentEventDto evt)
        {
            SessionData session;
            if (Sessions.TryGetValue(sessionId, out session))
                session.Events.Enqueue(evt);
        }

        public static AgentPollResponseDto DequeueAll(string sessionId)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId, out session))
                return new AgentPollResponseDto { SessionExists = false };

            var list = new List<AgentEventDto>();
            AgentEventDto evt;
            while (session.Events.TryDequeue(out evt))
                list.Add(evt);

            return new AgentPollResponseDto { Events = list, SessionExists = true };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Plan-confirm support
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the controller's OnPlanReady callback.
        /// Registers a TCS the callback will await; returns it so the callback can block.
        /// A second call for the same session replaces the previous TCS.
        /// </summary>
        public static TaskCompletionSource<bool> RegisterPlanConfirmation(string sessionId)
        {
            var tcs = new TaskCompletionSource<bool>();
            PendingConfirmations[sessionId] = tcs;
            return tcs;
        }

        /// <summary>
        /// Called by POST /ConfirmPlan.
        /// Resolves the pending TCS so the blocked propose_plan tool can continue.
        /// Returns false if no pending confirmation exists for this session.
        /// </summary>
        public static bool ConfirmPlan(string sessionId, bool confirmed)
        {
            TaskCompletionSource<bool> tcs;
            if (PendingConfirmations.TryRemove(sessionId, out tcs))
            {
                tcs.TrySetResult(confirmed);
                return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Schema-confirm support
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the controller's OnSchemaReady callback.
        /// Registers a TCS the callback will await; the propose_schema tool blocks on it.
        /// </summary>
        public static TaskCompletionSource<AgentSchemaResponse> RegisterSchemaConfirmation(string sessionId)
        {
            var tcs = new TaskCompletionSource<AgentSchemaResponse>();
            PendingSchemaConfirmations[sessionId] = tcs;
            return tcs;
        }

        /// <summary>
        /// Called by POST /ConfirmSchema.
        /// Resolves the pending TCS so the blocked propose_schema tool can continue.
        /// Returns false if no pending schema confirmation exists for this session.
        /// </summary>
        public static bool ConfirmSchema(string sessionId, AgentSchemaResponse response)
        {
            TaskCompletionSource<AgentSchemaResponse> tcs;
            if (PendingSchemaConfirmations.TryRemove(sessionId, out tcs))
            {
                tcs.TrySetResult(response);
                return true;
            }
            return false;
        }

        private static void CleanExpired()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            foreach (var kv in Sessions)
            {
                if (kv.Value.CreatedAt < cutoff)
                {
                    SessionData removed;
                    Sessions.TryRemove(kv.Key, out removed);

                    // Cancel any pending plan confirmation for the expired session
                    TaskCompletionSource<bool> tcs;
                    PendingConfirmations.TryRemove(kv.Key, out tcs);
                    if (tcs != null) tcs.TrySetResult(false);

                    // Cancel any pending schema confirmation for the expired session
                    TaskCompletionSource<AgentSchemaResponse> schemaTcs;
                    PendingSchemaConfirmations.TryRemove(kv.Key, out schemaTcs);
                    if (schemaTcs != null)
                        schemaTcs.TrySetResult(new AgentSchemaResponse { Confirmed = false, Feedback = "Session expired." });
                }
            }
        }
    }
}
