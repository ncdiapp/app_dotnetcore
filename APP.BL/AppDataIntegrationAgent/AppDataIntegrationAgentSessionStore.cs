using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationAgentSessionStore
    {
        private static readonly ConcurrentDictionary<string, SessionData> Sessions
            = new ConcurrentDictionary<string, SessionData>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, string> McpTokenToSession
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public class SessionData
        {
            public string SessionId { get; set; }
            public string CloudAgentId { get; set; }
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
            public List<AppDataIntegrationAgentMessageDto> ConversationHistory { get; set; }
                = new List<AppDataIntegrationAgentMessageDto>();
            /// <summary>Pack paths touched during the in-flight assistant turn (cleared at run start).</summary>
            public List<string> CurrentTurnPackPaths { get; set; }
                = new List<string>();
            /// <summary>Open UI offers from navigate / table_preview tools this turn.</summary>
            public List<AppDataIntegrationAgentOpenUiOfferDto> CurrentTurnOpenOffers { get; set; }
                = new List<AppDataIntegrationAgentOpenUiOfferDto>();
            /// <summary>True when run_select / propose_sql used a user connection string this turn (no SQL Workbench Open).</summary>
            public bool CurrentTurnSqlViaConnectionString { get; set; }
            public ConcurrentQueue<AppDataIntegrationAgentEventDto> Events { get; set; }
                = new ConcurrentQueue<AppDataIntegrationAgentEventDto>();
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public SemaphoreSlim EventReady { get; set; } = new SemaphoreSlim(0, int.MaxValue);
            public AppDataIntegrationAgentGateEvent PendingGate { get; set; }
            public TaskCompletionSource<AppDataIntegrationAgentGateResult> PendingGateTcs { get; set; }
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
            if (string.IsNullOrWhiteSpace(token)) return null;
            var t = token.Trim();
            string sessionId;
            SessionData data;
            if (McpTokenToSession.TryGetValue(t, out sessionId)
                && Sessions.TryGetValue(sessionId, out data))
                return data;

            // After AppAI.Web restart, in-memory token map is empty — hydrate from DB so Cursor MCP writes still work.
            data = AppDataIntegrationAgentSessionBL.HydrateLiveByMcpToken(t);
            return data;
        }

        public static void Enqueue(string sessionId, AppDataIntegrationAgentEventDto evt)
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

        public static AppDataIntegrationAgentPollResponseDto DequeueAll(string sessionId)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session))
                return new AppDataIntegrationAgentPollResponseDto { SessionExists = false };

            var list = new List<AppDataIntegrationAgentEventDto>();
            AppDataIntegrationAgentEventDto evt;
            while (session.Events.TryDequeue(out evt))
                list.Add(evt);

            return new AppDataIntegrationAgentPollResponseDto { Events = list, SessionExists = true };
        }

        public static TaskCompletionSource<AppDataIntegrationAgentGateResult> RegisterGate(string sessionId, AppDataIntegrationAgentGateEvent gate)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId ?? "", out session))
                return null;

            var tcs = new TaskCompletionSource<AppDataIntegrationAgentGateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
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
            return tcs.TrySetResult(new AppDataIntegrationAgentGateResult
            {
                Confirmed = confirmed,
                Feedback = feedback
            });
        }

        public static void BeginAssistantTurn(SessionData session)
        {
            if (session == null) return;
            session.CurrentTurnPackPaths = new List<string>();
            session.CurrentTurnOpenOffers = new List<AppDataIntegrationAgentOpenUiOfferDto>();
            session.CurrentTurnSqlViaConnectionString = false;
        }

        public static void NotePackPath(SessionData session, string relativePath)
        {
            if (session == null || string.IsNullOrWhiteSpace(relativePath)) return;
            var path = relativePath.Trim().Replace('\\', '/');
            if (!path.EndsWith(".appConfigPack.json", StringComparison.OrdinalIgnoreCase))
                return;
            if (session.CurrentTurnPackPaths == null)
                session.CurrentTurnPackPaths = new List<string>();
            if (session.CurrentTurnPackPaths.Any(p =>
                string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
                return;
            session.CurrentTurnPackPaths.Add(path);
        }

        public static List<string> TakeTurnPackPaths(SessionData session)
        {
            if (session?.CurrentTurnPackPaths == null || session.CurrentTurnPackPaths.Count == 0)
                return null;
            var list = session.CurrentTurnPackPaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            session.CurrentTurnPackPaths = new List<string>();
            return list.Count == 0 ? null : list;
        }

        public static List<string> PeekTurnPackPaths(SessionData session)
        {
            if (session?.CurrentTurnPackPaths == null || session.CurrentTurnPackPaths.Count == 0)
                return null;
            return session.CurrentTurnPackPaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void NoteOpenUiOffer(SessionData session, AppDataIntegrationAgentOpenUiOfferDto offer)
        {
            if (session == null || offer == null) return;
            if (session.CurrentTurnOpenOffers == null)
                session.CurrentTurnOpenOffers = new List<AppDataIntegrationAgentOpenUiOfferDto>();
            session.CurrentTurnOpenOffers.Add(offer);
        }

        public static List<AppDataIntegrationAgentOpenUiOfferDto> TakeTurnOpenOffers(SessionData session)
        {
            if (session?.CurrentTurnOpenOffers == null || session.CurrentTurnOpenOffers.Count == 0)
                return null;
            var list = session.CurrentTurnOpenOffers.ToList();
            session.CurrentTurnOpenOffers = new List<AppDataIntegrationAgentOpenUiOfferDto>();
            return list.Count == 0 ? null : list;
        }

        public static List<AppDataIntegrationAgentOpenUiOfferDto> PeekTurnOpenOffers(SessionData session)
        {
            if (session?.CurrentTurnOpenOffers == null || session.CurrentTurnOpenOffers.Count == 0)
                return null;
            return session.CurrentTurnOpenOffers.ToList();
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
