using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL.CursorCloudAgent;
using App.BL.GenericAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Agent Management path for RuntimeProvider = CursorCloudAgents:
    /// Cursor Cloud Agents API + dynamic MCP (AgentSkillMcp) exposing local AppAgentToolRegister tools.
    /// Bridges stream events into GenericAgentCallbacks so existing AgentUi chat polling works.
    /// </summary>
    public static class AgentManagedCursorBL
    {
        public static async Task RunAsync(
            string skillKey,
            string userMessage,
            List<JObject> chatHistory,
            string systemPrompt,
            GenericAgentCallbacks callbacks,
            AppClientIdentity? identity,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(
                    identity.HasValue
                        ? AIConfigSettingBL.GetCursorApiKey(identity.Value)
                        : AIConfigSettingBL.GetCursorApiKey())
                && string.IsNullOrWhiteSpace(CursorCloudAgentConfig.ApiKey))
            {
                await SafeError(callbacks, "Cursor API key is not configured (AIConfigCursorApiKey).").ConfigureAwait(false);
                return;
            }

            var mcpBase = CursorCloudAgentConfig.McpPublicBaseUrl;
            if (string.IsNullOrWhiteSpace(mcpBase))
            {
                await SafeError(callbacks,
                    "Cursor MCP public base URL is not configured (AIConfigCursorMcpPublicBaseUrl / Cursor.McpPublicBaseUrl).").ConfigureAwait(false);
                return;
            }

            var live = CursorCloudAgentSessionStore.CreateSession();
            live.SkillKey = skillKey;
            AppDataIntegrationAgentIdentity.Capture(live, identity);
            live.ConversationHistory = new List<CursorCloudAgentMessageDto>();
            if (chatHistory != null)
            {
                foreach (var m in chatHistory)
                {
                    var role = ((string)(m["role"] ?? m["Role"]) ?? "").Trim().ToLowerInvariant();
                    var content = (string)(m["content"] ?? m["Content"]) ?? "";
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    live.ConversationHistory.Add(new CursorCloudAgentMessageDto
                    {
                        Role = role == "assistant" ? "assistant" : "user",
                        Content = content,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    });
                }
            }
            live.ConversationHistory.Add(new CursorCloudAgentMessageDto
            {
                Role = "user",
                Content = userMessage,
                Timestamp = DateTime.UtcNow.ToString("o")
            });

            try
            {
                CursorCloudAgentSessionStore.BeginAssistantTurn(live);
                await SafeStep(callbacks, "thinking", "Starting Cursor Cloud Agent…").ConfigureAwait(false);

                var prompt = BuildPrompt(systemPrompt, live.ConversationHistory);
                var mcp = AgentSkillMcpBL.McpServerSpec(mcpBase, live.McpToken);
                var created = await CursorCloudAgentsApiClient.CreateAgentAsync(prompt, mcp, ct).ConfigureAwait(false);
                live.CloudAgentId = created.AgentId;
                live.LatestRunId = created.RunId;

                var assistant = new StringBuilder();
                await CursorCloudAgentsApiClient.StreamRunAsync(live.CloudAgentId, live.LatestRunId, (evt, payload) =>
                {
                    HandleStreamEvent(live, assistant, evt, payload, callbacks);
                }, ct).ConfigureAwait(false);

                var final = assistant.ToString().Trim();
                if (string.IsNullOrWhiteSpace(final))
                    final = "Cursor Cloud Agent finished with no text reply.";

                live.ConversationHistory.Add(new CursorCloudAgentMessageDto
                {
                    Role = "assistant",
                    Content = final,
                    Timestamp = DateTime.UtcNow.ToString("o")
                });

                if (callbacks?.OnDone != null)
                    await callbacks.OnDone(final).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await SafeError(callbacks, "Agent run was cancelled.").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeError(callbacks, "Cursor Cloud Agent error: " + ex.Message).ConfigureAwait(false);
            }
        }

        private static string BuildPrompt(string systemPrompt, List<CursorCloudAgentMessageDto> history)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                sb.AppendLine(systemPrompt.Trim());
                sb.AppendLine();
            }
            sb.AppendLine("## Runtime");
            sb.AppendLine("- You are running as a Cursor Cloud Agent for AppAI Agent Management.");
            sb.AppendLine("- MCP server **appai-agent-tools** exposes this agent's registered local tools. Prefer those tools for AppAI actions.");
            sb.AppendLine("- Call get_session_context when you need SkillKey / session ids.");
            sb.AppendLine();
            sb.AppendLine("## Conversation");
            foreach (var m in history ?? new List<CursorCloudAgentMessageDto>())
            {
                if (string.IsNullOrWhiteSpace(m?.Content)) continue;
                sb.Append(m.Role ?? "user").Append(": ").AppendLine(m.Content.Trim());
            }
            return sb.ToString();
        }

        private static void HandleStreamEvent(
            CursorCloudAgentSessionStore.SessionData live,
            StringBuilder assistant,
            string evt,
            JObject payload,
            GenericAgentCallbacks callbacks)
        {
            if (string.Equals(evt, "error", StringComparison.OrdinalIgnoreCase))
                return;

            if (string.Equals(evt, "interaction_update", StringComparison.OrdinalIgnoreCase))
            {
                var type = (string)payload?["type"];
                if (string.Equals(type, "thinking-delta", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "thinking", StringComparison.OrdinalIgnoreCase))
                {
                    var think = PayloadText(payload);
                    if (!string.IsNullOrEmpty(think))
                        _ = SafeStep(callbacks, "thinking", think);
                    return;
                }
                if (string.Equals(type, "text-delta", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "token-delta", StringComparison.OrdinalIgnoreCase))
                {
                    AppendAssistant(assistant, PayloadText(payload), callbacks);
                }
                return;
            }

            if (string.Equals(evt, "assistant", StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt, "delta", StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt, "message", StringComparison.OrdinalIgnoreCase))
            {
                AppendAssistant(assistant, PayloadText(payload), callbacks);
                return;
            }

            if (string.Equals(evt, "thinking", StringComparison.OrdinalIgnoreCase))
            {
                var think = (string)payload?["text"] ?? PayloadText(payload);
                if (!string.IsNullOrEmpty(think))
                    _ = SafeStep(callbacks, "thinking", think);
                return;
            }

            if (string.Equals(evt, "tool_call", StringComparison.OrdinalIgnoreCase))
            {
                var name = (string)payload?["name"];
                var status = (string)payload?["status"];
                _ = SafeStep(callbacks, "tool_call",
                    (name ?? "tool") + (string.IsNullOrWhiteSpace(status) ? "" : " " + status),
                    name);
                return;
            }

            if (string.Equals(evt, "result", StringComparison.OrdinalIgnoreCase))
            {
                var text = PayloadText(payload) ?? (string)payload?["text"];
                if (!string.IsNullOrEmpty(text) && assistant.Length == 0)
                    AppendAssistant(assistant, text, callbacks);
            }
        }

        private static void AppendAssistant(StringBuilder assistant, string text, GenericAgentCallbacks callbacks)
        {
            if (string.IsNullOrEmpty(text)) return;
            assistant.Append(text);
            if (callbacks?.OnToken != null)
                _ = callbacks.OnToken(text);
        }

        private static string PayloadText(JObject payload)
        {
            if (payload == null) return null;
            var text = (string)payload["text"]
                ?? (string)payload["content"]
                ?? (string)payload["delta"]
                ?? (string)payload["message"];
            if (!string.IsNullOrEmpty(text)) return text;
            var nested = payload["delta"] as JObject ?? payload["message"] as JObject;
            if (nested != null)
                return (string)nested["text"] ?? (string)nested["content"];
            return null;
        }

        private static Task SafeStep(GenericAgentCallbacks callbacks, string type, string description, string toolName = null)
        {
            if (callbacks?.OnStep == null) return Task.CompletedTask;
            return callbacks.OnStep(new AgentStepEvent
            {
                Type = type,
                Description = description ?? "",
                ToolName = toolName,
                IsSuccess = true
            });
        }

        private static async Task SafeError(GenericAgentCallbacks callbacks, string message)
        {
            if (callbacks?.OnError == null) return;
            try { await callbacks.OnError(message).ConfigureAwait(false); } catch { }
        }
    }
}
