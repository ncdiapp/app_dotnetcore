using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.EntityDto;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// In-memory event queue for the GenericAgent SSE/polling streaming pattern.
    /// Mirrors AppBuilderAgentSessionStore but is generic across all skill keys.
    ///
    /// Session lifecycle: CreateSession → agent enqueues events → React client polls
    ///   → session expires after 30 minutes.
    /// Plan-confirm gate: RegisterPlanConfirmation → ConfirmPlan resolves the TCS.
    /// </summary>
    public static class GenericAgentSessionStore
    {
        private static readonly ConcurrentDictionary<string, SessionData> Sessions
            = new ConcurrentDictionary<string, SessionData>();

        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> PendingConfirmations
            = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        private static readonly ConcurrentDictionary<string, TaskCompletionSource<AgentSchemaResponse>> PendingSchema
            = new ConcurrentDictionary<string, TaskCompletionSource<AgentSchemaResponse>>();

        public sealed class SessionData
        {
            public ConcurrentQueue<AgentEventDto> Events    = new ConcurrentQueue<AgentEventDto>();
            public DateTime                       CreatedAt = DateTime.UtcNow;
            public SemaphoreSlim                  EventReady = new SemaphoreSlim(0, int.MaxValue);
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
            if (Sessions.TryGetValue(sessionId, out var session))
            {
                session.Events.Enqueue(evt);
                session.EventReady.Release();
            }
        }

        public static async Task<bool> WaitForEventAsync(
            string sessionId, TimeSpan timeout, CancellationToken ct = default)
        {
            if (!Sessions.TryGetValue(sessionId, out var session)) return false;
            try   { return await session.EventReady.WaitAsync(timeout, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return false; }
        }

        public static AgentPollResponseDto DequeueAll(string sessionId)
        {
            if (!Sessions.TryGetValue(sessionId, out var session))
                return new AgentPollResponseDto { SessionExists = false };

            var list = new List<AgentEventDto>();
            while (session.Events.TryDequeue(out var evt))
                list.Add(evt);

            return new AgentPollResponseDto { Events = list, SessionExists = true };
        }

        public static TaskCompletionSource<bool> RegisterPlanConfirmation(string sessionId)
        {
            var tcs = new TaskCompletionSource<bool>();
            PendingConfirmations[sessionId] = tcs;
            return tcs;
        }

        public static bool ConfirmPlan(string sessionId, bool confirmed)
        {
            if (PendingConfirmations.TryRemove(sessionId, out var tcs))
            {
                tcs.TrySetResult(confirmed);
                return true;
            }
            return false;
        }

        public static TaskCompletionSource<AgentSchemaResponse> RegisterSchemaConfirmation(string sessionId)
        {
            var tcs = new TaskCompletionSource<AgentSchemaResponse>();
            PendingSchema[sessionId] = tcs;
            return tcs;
        }

        public static bool ConfirmSchema(string sessionId, AgentSchemaResponse response)
        {
            if (PendingSchema.TryRemove(sessionId, out var tcs))
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
                    Sessions.TryRemove(kv.Key, out var removed);
                    removed?.EventReady.Dispose();

                    if (PendingConfirmations.TryRemove(kv.Key, out var tcs))
                        tcs.TrySetResult(false);

                    if (PendingSchema.TryRemove(kv.Key, out var stcs))
                        stcs.TrySetResult(new AgentSchemaResponse { Confirmed = false, Feedback = "Session expired." });
                }
            }
        }
    }
}
