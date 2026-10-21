using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.Memory;
using APP.Components.Dto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tools of library "platform-memory" (seeded by V055). All reads and writes are scoped to the
    /// authenticated user and agent taken from <see cref="AgentToolContext"/>, never from tool arguments.
    ///
    /// ToolConfig: {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentMemoryPlugin","MethodName":"RememberFact"}
    /// </summary>
    public class AgentMemoryPlugin
    {
        public string RememberFact(string key, string value, string category, AgentToolContext context)
        {
            if (context == null || context.UserId <= 0) return Error("No authenticated user; nothing was saved.");
            var result = AgentMemoryFactBL.Upsert(
                context.UserId, context.SkillKey, key, value, category,
                "confirmed", "agent", null, context.ChatSessionKey, context.DataSourceId);

            return result switch
            {
                FactWriteResult.Added     => Ok("saved", key),
                FactWriteResult.Updated   => Ok("updated", key),
                FactWriteResult.Unchanged => Ok("already known", key),
                _ => Error("Not saved. The key and value must be non-empty, the value at most "
                           + AgentMemoryFactBL.MaxValueLength + " characters, and it must not contain a password, key or token.")
            };
        }

        public string ForgetFact(string key, AgentToolContext context)
        {
            if (context == null || context.UserId <= 0) return Error("No authenticated user.");
            var deleted = AgentMemoryFactBL.DeleteByKey(context.UserId, context.SkillKey, key, context.DataSourceId);
            return deleted ? Ok("forgotten", key) : Error("No saved fact with that key.");
        }

        public async Task<string> RecallMemory(string query, AgentToolContext context, CancellationToken ct)
        {
            if (context == null || context.UserId <= 0) return Error("No authenticated user.");
            if (string.IsNullOrWhiteSpace(query)) return Error("A query is required.");

            var q = query.Trim().ToLowerInvariant();
            var facts = AgentMemoryFactBL.List(context.UserId, context.SkillKey, context.DataSourceId, includePending: false)
                .Where(f => f.FactKey.Contains(q.Replace(' ', '_')) || f.FactValue.ToLowerInvariant().Contains(q)
                            || q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(w => w.Length > 3 && f.FactValue.ToLowerInvariant().Contains(w)))
                .Select(f => new { f.FactKey, f.FactValue })
                .ToList();

            var identity = new AppClientIdentity
            {
                UserId                        = context.UserId,
                CurrentWorkingCompanyId       = context.CompanyId,
                CurrentUserDbConnectionString = context.ConnectionString,
                CurrentUserDataBaseName       = context.DatabaseName,
                SessionId                     = context.UserSessionId,
                DataSourceId                  = context.DataSourceId,
                CurrentLoginUserType          = context.LoginUserType
            };
            var embedding = await EmbeddingClient.EmbedAsync(query, identity, ct).ConfigureAwait(false);
            var episodes = embedding == null
                ? new System.Collections.Generic.List<AgentEpisodeDto>()
                : AgentEpisodeBL.Recall(context.UserId, context.SkillKey, context.DataSourceId, embedding, 3);

            return JsonConvert.SerializeObject(new
            {
                Facts    = facts,
                Episodes = episodes.Select(e => new { Date = e.CreatedAt.ToString("yyyy-MM-dd"), e.Summary }),
                Note     = facts.Count == 0 && episodes.Count == 0 ? "Nothing relevant is remembered." : null
            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        private static string Ok(string what, string key) => JsonConvert.SerializeObject(new { Result = what, Key = AgentMemoryFactBL.NormalizeKey(key) });
        private static string Error(string message)      => JsonConvert.SerializeObject(new { Error = message });
    }
}
