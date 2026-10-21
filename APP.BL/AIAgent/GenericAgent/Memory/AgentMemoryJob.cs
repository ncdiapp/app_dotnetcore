using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL.GenericAgent;
using APP.Components.Dto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    /// <summary>Off: nothing runs. DryRun: list chats that would be extracted, no LLM. Preview: call the LLM and log
    /// proposals, write nothing. On: extract and store.</summary>
    public enum AgentMemoryMode { Off, DryRun, Preview, On }

    public sealed class AgentMemoryJobOptions
    {
        public AgentMemoryMode Mode           { get; set; } = AgentMemoryMode.Off;
        public int             IdleMinutes    { get; set; } = 30;
        public int             BatchPerTenant { get; set; } = 20;
        public int             MinNewTurns    { get; set; } = 4;
    }

    public sealed class AgentMemoryRunSummary
    {
        public int          Tenants   { get; set; }
        public int          Skipped   { get; set; }
        public int          Chats     { get; set; }
        public int          Extracted { get; set; }
        public int          Failed    { get; set; }
        public List<string> Lines     { get; } = new();
        public override string ToString() =>
            $"tenants={Tenants} skippedTenants={Skipped} chats={Chats} extracted={Extracted} failed={Failed}";
    }

    /// <summary>
    /// Background extraction of L2 facts and L3 episodes. Runs without an HTTP request: the tenant identity is
    /// built from the data-source row (company, decrypted connection string) and the chat's UserId, and only the
    /// identity-based overloads and dataSourceId-based SQL are used, so no ServerContext is needed.
    /// </summary>
    public static class AgentMemoryJob
    {
        private static volatile AgentMemoryJobOptions _current = new();
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        public static AgentMemoryJobOptions Current => _current;

        /// <summary>Applied at startup from configuration (AgentMemory section).</summary>
        public static void Configure(AgentMemoryJobOptions options) => _current = options ?? new AgentMemoryJobOptions();

        public static async Task<AgentMemoryRunSummary> RunOnceAsync(AgentMemoryJobOptions options, CancellationToken ct)
        {
            options ??= _current;
            var summary = new AgentMemoryRunSummary();
            if (options.Mode == AgentMemoryMode.Off) return summary;

            foreach (var ds in AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterEntity())
            {
                ct.ThrowIfCancellationRequested();
                // Rows without an owner company are unassigned reserve databases.
                if (ds.IsCompanyMasterDb != true || !ds.DataSourceOwnerCompanyId.HasValue || string.IsNullOrEmpty(ds.ConnectionString))
                    continue;
                try
                {
                    await RunTenantAsync(ds.DataSourceId, ds.DataSourceOwnerCompanyId.Value, ds.DatabaseName,
                        AppConnectionStringEncryptionBL.Decrypt(ds.ConnectionString), options, summary, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    summary.Failed++;
                    Log.Error(ex, "[MemoryJob] tenant {0} failed", ds.DataSourceName ?? ds.DataSourceId.ToString());
                }
            }
            Log.Info("[MemoryJob] mode={0} {1}", options.Mode, summary);
            return summary;
        }

        private static async Task RunTenantAsync(int dataSourceId, int companyId, string databaseName, string connectionString,
            AgentMemoryJobOptions options, AgentMemoryRunSummary summary, CancellationToken ct)
        {
            var identity = new AppClientIdentity
            {
                CurrentWorkingCompanyId       = companyId,
                CurrentUserDbConnectionString = connectionString,
                CurrentUserDataBaseName       = databaseName,
                DataSourceId                  = dataSourceId
            };

            var ready = AgentMemorySessionBL.FindReady(dataSourceId, options.IdleMinutes, options.BatchPerTenant);
            if (ready.Count == 0) return;

            if (options.Mode != AgentMemoryMode.DryRun && !AIConfigSettingBL.IsConfigured(identity))
            {
                summary.Skipped++;
                Log.Debug("[MemoryJob] company {0}: no LLM API key configured, skipped", companyId);
                return;
            }

            summary.Tenants++;
            foreach (var chat in ready)
            {
                ct.ThrowIfCancellationRequested();
                summary.Chats++;

                if (options.Mode == AgentMemoryMode.DryRun)
                {
                    var line = $"company={companyId} would extract skill={chat.SkillKey} session={chat.SessionKey} user={chat.UserId} processedTurns={chat.ExtractedTurnCount}";
                    summary.Lines.Add(line);
                    Log.Info("[MemoryJob] DRYRUN {0}", line);
                    continue;
                }

                if (!AgentMemorySessionBL.TryClaim(dataSourceId, chat.SessionKey)) continue;
                var chatIdentity = identity;
                chatIdentity.UserId = chat.UserId;
                try
                {
                    var outcome = await AgentMemoryExtractor.ExtractAsync(
                        chatIdentity, chat.SkillKey, chat.UserId, chat.SessionKey, ParseMessages(chat.MessagesJson),
                        chat.ExtractedTurnCount, options.MinNewTurns, dataSourceId,
                        dryRun: options.Mode == AgentMemoryMode.Preview, ct).ConfigureAwait(false);

                    var line = $"company={companyId} skill={chat.SkillKey} session={chat.SessionKey} {outcome}";
                    summary.Lines.Add(line);
                    Log.Info("[MemoryJob] {0} {1}", options.Mode, line);

                    if (options.Mode == AgentMemoryMode.Preview) AgentMemorySessionBL.Release(dataSourceId, chat.SessionKey);
                    else if (outcome.Ok) { AgentMemorySessionBL.MarkDone(dataSourceId, chat.SessionKey, outcome.TurnCount); summary.Extracted++; }
                    else { AgentMemorySessionBL.MarkFailed(dataSourceId, chat.SessionKey); summary.Failed++; }
                }
                catch (OperationCanceledException)
                {
                    AgentMemorySessionBL.Release(dataSourceId, chat.SessionKey);
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[MemoryJob] chat {0} failed", chat.SessionKey);
                    AgentMemorySessionBL.MarkFailed(dataSourceId, chat.SessionKey);
                    summary.Failed++;
                }
            }
        }

        /// <summary>
        /// "Clear conversation" is about to wipe the transcript: extract from the captured messages first, in the
        /// background. Writes facts and the episode only; the session row is reset by the clear itself.
        /// </summary>
        public static void QueueExtractOnClear(AppClientIdentity identity, string skillKey, int userId, string sessionKey,
            int dataSourceId, List<JObject> messages)
        {
            var options = _current;
            if (options.Mode != AgentMemoryMode.On || messages == null || messages.Count == 0 || userId <= 0) return;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!AgentMemorySessionBL.SkillUsesMemory(dataSourceId, skillKey)) return;
                    var state = AgentMemorySessionBL.GetState(dataSourceId, sessionKey, skillKey, userId);
                    if (state.OptOut || !AIConfigSettingBL.IsConfigured(identity)) return;

                    var outcome = await AgentMemoryExtractor.ExtractAsync(
                        identity, skillKey, userId, sessionKey, messages, state.ExtractedTurnCount,
                        minNewTurns: 2, dataSourceId, dryRun: false, CancellationToken.None).ConfigureAwait(false);
                    Log.Info("[MemoryJob] on-clear skill={0} session={1} {2}", skillKey, sessionKey, outcome);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[MemoryJob] on-clear extraction failed skill={0} session={1}", skillKey, sessionKey);
                }
            });
        }

        private static List<JObject> ParseMessages(string json)
        {
            try
            {
                return JArray.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json).OfType<JObject>().ToList();
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "[MemoryJob] MessagesJson could not be parsed");
                return new List<JObject>();
            }
        }
    }
}
