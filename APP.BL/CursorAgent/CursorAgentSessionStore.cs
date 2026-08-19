using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace App.BL.CursorAgent
{
    public static class CursorAgentSessionStore
    {
        private static readonly ConcurrentDictionary<string, SessionData> Sessions
            = new ConcurrentDictionary<string, SessionData>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, string> McpTokenToSession
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public class SessionData
        {
            public string SessionId { get; set; }
            public string CursorAgentId { get; set; }
            public string LatestRunId { get; set; }
            public string McpToken { get; set; }
            public string AppSessionId { get; set; }
            public AppClientIdentity? Identity { get; set; }
            public string IdentityJson { get; set; }
            public int? CreatedById { get; set; }
            public int? CompanyId { get; set; }
            public int? SaasApplicationId { get; set; }
            public int? DataSourceRegisterId { get; set; }
            public string SkillKey { get; set; }
            public bool AllowProposeImport { get; set; }
            public string WorkspaceRelativePath { get; set; }
            public List<CursorAgentMessageDto> ConversationHistory { get; set; }
                = new List<CursorAgentMessageDto>();
            public ConcurrentQueue<CursorAgentEventDto> Events { get; set; }
                = new ConcurrentQueue<CursorAgentEventDto>();
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public SemaphoreSlim EventReady { get; set; } = new SemaphoreSlim(0, int.MaxValue);
            public CursorAgentGateEvent PendingGate { get; set; }
            public TaskCompletionSource<CursorAgentGateResult> PendingGateTcs { get; set; }
            public CancellationTokenSource RunCts { get; set; }
        }

        public static SessionData CreateSession()
        {
            var id = Guid.NewGuid().ToString("N");
            var mcp = Guid.NewGuid().ToString("N");
            var data = new SessionData
            {
                SessionId = id,
                McpToken = mcp
            };
            Sessions[id] = data;
            McpTokenToSession[mcp] = id;
            CleanExpired();
            return data;
        }

        public static bool TryGet(string sessionId, out SessionData data)
        {
            return Sessions.TryGetValue(sessionId ?? "", out data);
        }

        public static SessionData GetByMcpToken(string token)
        {
            string sessionId;
            SessionData data;
            if (string.IsNullOrWhiteSpace(token)) return null;
            if (!McpTokenToSession.TryGetValue(token.Trim(), out sessionId)) return null;
            return Sessions.TryGetValue(sessionId, out data) ? data : null;
        }

        public static void Enqueue(string sessionId, CursorAgentEventDto evt)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session)) return;
            session.Events.Enqueue(evt);
            try { session.EventReady.Release(); } catch (SemaphoreFullException) { }
        }

        public static async Task<bool> WaitForEventAsync(string sessionId, TimeSpan timeout, CancellationToken ct)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session)) return false;
            try { return await session.EventReady.WaitAsync(timeout, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return false; }
        }

        public static CursorAgentPollResponseDto DequeueAll(string sessionId)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session))
                return new CursorAgentPollResponseDto { SessionExists = false };

            var list = new List<CursorAgentEventDto>();
            CursorAgentEventDto evt;
            while (session.Events.TryDequeue(out evt))
                list.Add(evt);

            return new CursorAgentPollResponseDto { Events = list, SessionExists = true };
        }

        public static TaskCompletionSource<CursorAgentGateResult> RegisterGate(string sessionId, CursorAgentGateEvent gate)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session))
                return null;

            var tcs = new TaskCompletionSource<CursorAgentGateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            session.PendingGate = gate;
            session.PendingGateTcs = tcs;
            return tcs;
        }

        public static bool ConfirmGate(string sessionId, string gateId, bool confirmed, string feedback)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session)) return false;
            if (session.PendingGate == null || session.PendingGateTcs == null) return false;
            if (!string.IsNullOrWhiteSpace(gateId) &&
                !string.Equals(session.PendingGate.GateId, gateId, StringComparison.OrdinalIgnoreCase))
                return false;

            var tcs = session.PendingGateTcs;
            session.PendingGateTcs = null;
            session.PendingGate = null;
            return tcs.TrySetResult(new CursorAgentGateResult
            {
                Confirmed = confirmed,
                Feedback = feedback
            });
        }

        public static void AttachLive(SessionData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.SessionId)) return;
            Sessions[data.SessionId] = data;
            if (!string.IsNullOrWhiteSpace(data.McpToken))
                McpTokenToSession[data.McpToken] = data.SessionId;
        }

        public static void Remove(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;
            SessionData removed;
            if (!Sessions.TryRemove(sessionId, out removed) || removed == null) return;
            try { removed.RunCts?.Cancel(); } catch { }
            if (!string.IsNullOrWhiteSpace(removed.McpToken))
            {
                string ignored;
                McpTokenToSession.TryRemove(removed.McpToken, out ignored);
            }
        }

        private static void CleanExpired()
        {
            var cutoff = DateTime.UtcNow.AddHours(-6);
            foreach (var kv in Sessions)
            {
                if (kv.Value.CreatedAt >= cutoff) continue;
                SessionData removed;
                if (Sessions.TryRemove(kv.Key, out removed) && !string.IsNullOrWhiteSpace(removed.McpToken))
                {
                    string ignored;
                    McpTokenToSession.TryRemove(removed.McpToken, out ignored);
                }
            }
        }
    }
}
