using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// ask_user — structured HITL question gate for Interactive GenericAgent runs.
    ///
    /// Reads <see cref="GenericAgentCallbacks.OnAskUser"/> via <see cref="AgentHitlBridge"/>
    /// (set by GenericAgentEngine). Waits until the UI POSTs ConfirmAskUser.
    /// Deterministic mode returns an error JSON (no blocking).
    /// </summary>
    public class AgentAskUserPlugin
    {
        public async Task<string> AskUser(
            string prompt,
            AgentToolContext context,
            string mode = "text",
            string fieldsJson = null,
            string optionsJson = null,
            string contextKey = null)
        {
            if (context != null && context.IsDeterministic)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ask_user is not available in Deterministic mode. Collect required inputs before calling the agent, or use Interactive ExecutionMode."
                });
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return JsonConvert.SerializeObject(new { ok = false, error = "prompt is required." });
            }

            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "text" : mode.Trim().ToLowerInvariant();
            if (normalizedMode != "text" && normalizedMode != "single_choice" && normalizedMode != "multi_choice")
                normalizedMode = "text";

            var askEvent = new AgentAskUserEvent
            {
                Prompt = prompt.Trim(),
                Mode = normalizedMode,
                Fields = ParseFields(fieldsJson),
                Options = ParseOptions(optionsJson),
                ContextKey = string.IsNullOrWhiteSpace(contextKey) ? null : contextKey.Trim()
            };

            var onAsk = AgentHitlBridge.Current?.OnAskUser;
            if (onAsk == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ask_user callback is not wired. Use GenericAgent Interactive chat (SSE) so the UI can answer."
                });
            }

            AgentAskUserResponse response;
            try
            {
                response = await onAsk(askEvent).ConfigureAwait(false)
                    ?? new AgentAskUserResponse { Cancelled = true };
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    cancelled = true,
                    error = "ask_user callback error: " + ex.Message
                });
            }

            if (!response.Cancelled
                && !string.IsNullOrWhiteSpace(askEvent.ContextKey)
                && context != null
                && HasAnswers(response))
            {
                try
                {
                    MergeAnswersToSharedContext(context, askEvent.ContextKey, response);
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex,
                        "ask_user contextKey merge failed for key={0}", askEvent.ContextKey);
                }
            }

            return JsonConvert.SerializeObject(new
            {
                ok = !response.Cancelled,
                cancelled = response.Cancelled,
                answers = response.Answers ?? new Dictionary<string, string>(),
                selectedIds = response.SelectedIds ?? new List<string>(),
                freeText = response.FreeText ?? ""
            });
        }

        private static bool HasAnswers(AgentAskUserResponse response)
        {
            if (response?.Answers != null && response.Answers.Count > 0) return true;
            if (response?.SelectedIds != null && response.SelectedIds.Count > 0) return true;
            if (!string.IsNullOrWhiteSpace(response?.FreeText)) return true;
            return false;
        }

        private static void MergeAnswersToSharedContext(
            AgentToolContext context, string contextKey, AgentAskUserResponse response)
        {
            JObject merged;
            var existing = AppAgentSharedContextBL.ReadContext(
                context.WorkflowId, contextKey, context.DataSourceId);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                try { merged = JObject.Parse(existing); }
                catch { merged = new JObject(); }
            }
            else
            {
                merged = new JObject();
            }

            if (response.Answers != null)
            {
                foreach (var kv in response.Answers)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    merged[kv.Key] = kv.Value ?? "";
                }
            }

            if (response.SelectedIds != null && response.SelectedIds.Count > 0)
                merged["selectedIds"] = new JArray(response.SelectedIds);

            if (!string.IsNullOrWhiteSpace(response.FreeText))
                merged["freeText"] = response.FreeText;

            AppAgentSharedContextBL.WriteContext(
                context.WorkflowId, contextKey, merged.ToString(Formatting.None), context.DataSourceId);
        }

        private static List<AgentAskUserField> ParseFields(string fieldsJson)
        {
            var list = new List<AgentAskUserField>();
            if (string.IsNullOrWhiteSpace(fieldsJson)) return list;
            try
            {
                var arr = JArray.Parse(fieldsJson);
                foreach (var token in arr)
                {
                    if (token is not JObject o) continue;
                    var name = o.Value<string>("name") ?? o.Value<string>("Name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    list.Add(new AgentAskUserField
                    {
                        Name = name.Trim(),
                        Label = (o.Value<string>("label") ?? o.Value<string>("Label") ?? name).Trim(),
                        Required = o.Value<bool?>("required") ?? o.Value<bool?>("Required") ?? false
                    });
                }
            }
            catch { /* ignore bad JSON — LLM gets empty fields */ }
            return list;
        }

        private static List<AgentAskUserOption> ParseOptions(string optionsJson)
        {
            var list = new List<AgentAskUserOption>();
            if (string.IsNullOrWhiteSpace(optionsJson)) return list;
            try
            {
                var arr = JArray.Parse(optionsJson);
                foreach (var token in arr)
                {
                    if (token is not JObject o) continue;
                    var id = o.Value<string>("id") ?? o.Value<string>("Id");
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    list.Add(new AgentAskUserOption
                    {
                        Id = id.Trim(),
                        Label = (o.Value<string>("label") ?? o.Value<string>("Label") ?? id).Trim()
                    });
                }
            }
            catch { /* ignore bad JSON */ }
            return list;
        }
    }
}
