using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Framework;
using APP.Framework.Plugin;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// BuiltIn plugin that lets an orchestrator agent invoke a worker agent by SkillKey.
    /// Registered in AppAgentToolRegister under the 'platform-multi-agent' library.
    ///
    /// The WorkflowId from the orchestrator's AgentToolContext is propagated to the child
    /// so all agents in the same workflow share the same AppAgentSharedContext scope.
    ///
    /// The target agent MUST have ExecutionMode='Deterministic' — otherwise it will block
    /// waiting for user plan/schema confirmation that will never arrive.
    /// </summary>
    public static class AgentCallPlugin
    {
        public static async Task<string> CallAgent(
            string targetSkillKey,
            string message,
            AgentToolContext context,
            CancellationToken ct)
        {
            targetSkillKey = (targetSkillKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(targetSkillKey))
                return "[call_agent denied: targetSkillKey is required.]";

            // Whitelist: caller may only invoke Child-Agents registered in AppAgentChildMapping.
            var callerSkillKey = (context?.SkillKey ?? "").Trim();
            int dsId = context?.DataSourceId ?? 0;
            if (!string.IsNullOrWhiteSpace(callerSkillKey) && AppAgentChildMappingBL.TableExists(dsId))
            {
                if (!AppAgentChildMappingBL.IsRegisteredChild(dsId, callerSkillKey, targetSkillKey))
                {
                    var msg =
                        $"[call_agent denied: '{targetSkillKey}' is not a registered Child-Agent of '{callerSkillKey}'. " +
                        "Add it under Agent Management → Child-Agent tab.]";
                    NLog.LogManager.GetCurrentClassLogger().Warn("AgentCallPlugin: {0}", msg);
                    return msg;
                }
            }

            // Prefer AgentToolContext fields (includes DataSourceId). Falling back to
            // ServerContext alone is unsafe: older OverrideThreadIdentity omitted DataSourceId,
            // which made child GetFixture resolve register id 0 → ORMEntityOutOfSyncException.
            AppClientIdentity? identity = null;
            if (context != null &&
                (!string.IsNullOrEmpty(context.ConnectionString) || context.DataSourceId > 0))
            {
                identity = new AppClientIdentity
                {
                    UserId                        = context.UserId,
                    CurrentWorkingCompanyId       = context.CompanyId,
                    CurrentUserDbConnectionString = context.ConnectionString,
                    CurrentUserDataBaseName       = context.DatabaseName,
                    SessionId                     = context.UserSessionId,
                    DataSourceId                  = context.DataSourceId,
                    CurrentLoginUserType          = context.LoginUserType
                };
            }
            else if (ServerContext.Instance.CurrnetClientIdentity is AppClientIdentity ai)
            {
                identity = ai;
            }

            var result = "";
            var callbacks = new GenericAgentCallbacks
            {
                OnDone  = r => { result = r; return Task.CompletedTask; },
                OnError = e =>
                {
                    result = $"[{targetSkillKey} error: {e}]";
                    NLog.LogManager.GetCurrentClassLogger().Warn($"AgentCallPlugin: child agent {targetSkillKey} returned error: {e}");
                    return Task.CompletedTask;
                }
            };

            // Propagate WorkflowId so child agent shares the same blackboard scope.
            // Empty chatHistory — worker agents receive their full context via the message + shared context.
            await GenericAgentBL.RunAsync(
                targetSkillKey, message,
                new List<JObject>(),
                callbacks, identity, ct,
                workflowId: context.WorkflowId,
                chatSessionKey: context.ChatSessionKey).ConfigureAwait(false);

            return result;
        }
    }
}
