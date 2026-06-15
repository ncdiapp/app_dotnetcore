using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using APP.Components.EntityDto;

namespace App.BL.QueryAgent
{
    /// <summary>
    /// In-memory event queue for the polling-based Query AI Agent.
    ///
    /// Flow:
    ///   1. Controller calls CreateSession() → gets sessionId
    ///   2. Agent callbacks enqueue events via Enqueue()
    ///   3. React client polls GET /PollEvents?sessionId=... → DequeueAll()
    ///   4. Sessions auto-expire after 30 minutes
    /// </summary>
    public static class QueryAgentSessionStore
    {
        private static readonly ConcurrentDictionary<string, SessionData> Sessions
            = new ConcurrentDictionary<string, SessionData>();

        public class SessionData
        {
            public ConcurrentQueue<QueryAgentEventDto> Events = new ConcurrentQueue<QueryAgentEventDto>();
            public DateTime CreatedAt = DateTime.UtcNow;
        }

        public static string CreateSession()
        {
            var id = Guid.NewGuid().ToString("N");
            Sessions[id] = new SessionData();
            CleanExpired();
            return id;
        }

        public static void Enqueue(string sessionId, QueryAgentEventDto evt)
        {
            SessionData session;
            if (Sessions.TryGetValue(sessionId, out session))
                session.Events.Enqueue(evt);
        }

        public static QueryAgentPollResponseDto DequeueAll(string sessionId)
        {
            SessionData session;
            if (!Sessions.TryGetValue(sessionId, out session))
                return new QueryAgentPollResponseDto { SessionExists = false };

            var list = new List<QueryAgentEventDto>();
            QueryAgentEventDto evt;
            while (session.Events.TryDequeue(out evt))
                list.Add(evt);

            return new QueryAgentPollResponseDto { Events = list, SessionExists = true };
        }

        private static void CleanExpired()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            foreach (var kv in Sessions)
                if (kv.Value.CreatedAt < cutoff)
                {
                    SessionData removed;
                    Sessions.TryRemove(kv.Key, out removed);
                }
        }
    }
}
