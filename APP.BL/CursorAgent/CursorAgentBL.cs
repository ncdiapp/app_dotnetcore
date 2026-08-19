using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;

namespace App.BL.CursorAgent
{
    public static class CursorAgentBL
    {
        public static CursorAgentStartResultDto StartSession(CursorAgentStartRequestDto request, AppClientIdentity? identity)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
                throw new ArgumentException("UserMessage is required.");
            if (!request.SaasApplicationId.HasValue || request.SaasApplicationId.Value <= 0)
                throw new ArgumentException("SaasApplicationId is required.");
            if (string.IsNullOrWhiteSpace(CursorAgentConfig.ApiKey))
                throw new InvalidOperationException("Cursor:ApiKey is not configured.");

            var live = CursorAgentSessionStore.CreateSession();
            live.SaasApplicationId = request.SaasApplicationId;
            live.DataSourceRegisterId = request.DataSourceRegisterId;
            CursorAgentSkillCatalogBL.ApplyToSession(live, request.SkillKey);
            CursorAgentIdentity.Capture(live, identity);
            live.WorkspaceRelativePath = live.SessionId;
            live.ConversationHistory = request.ConversationHistory ?? new List<CursorAgentMessageDto>();
            live.ConversationHistory.Add(new CursorAgentMessageDto { Role = "user", Content = request.UserMessage, Timestamp = DateTime.UtcNow.ToString("o") });
            CursorWorkspaceBL.EnsureSessionDir(live.WorkspaceRelativePath, live.CompanyId);
            CursorAgentSessionBL.SaveNew(live, request.UserMessage);

            live.RunCts = new CancellationTokenSource();
            var ct = live.RunCts.Token;
            var sessionId = live.SessionId;
            var userMessage = request.UserMessage;
            Task.Run(() => RunCreateAsync(sessionId, userMessage, ct));

            return new CursorAgentStartResultDto
            {
                IsStarted = true,
                SessionId = live.SessionId,
                WorkspaceRelativePath = live.WorkspaceRelativePath
            };
        }

        public static void FollowUp(CursorAgentFollowUpRequestDto request, AppClientIdentity? identity = null)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
                throw new ArgumentException("SessionId is required.");
            if (string.IsNullOrWhiteSpace(request.UserMessage))
                throw new ArgumentException("UserMessage is required.");

            var live = GetOrHydrate(request.SessionId);
            CursorAgentIdentity.Capture(live, identity);
            if (string.IsNullOrWhiteSpace(live.CursorAgentId))
                throw new InvalidOperationException("Cursor agent has not been created for this session yet.");

            if (!string.IsNullOrWhiteSpace(request.SkillKey))
                CursorAgentSkillCatalogBL.ApplyToSession(live, request.SkillKey);
            if (request.SaasApplicationId.HasValue && request.SaasApplicationId.Value > 0)
                live.SaasApplicationId = request.SaasApplicationId;
            if (request.DataSourceRegisterId.HasValue)
                live.DataSourceRegisterId = request.DataSourceRegisterId > 0 ? request.DataSourceRegisterId : null;

            live.ConversationHistory.Add(new CursorAgentMessageDto { Role = "user", Content = request.UserMessage, Timestamp = DateTime.UtcNow.ToString("o") });
            if (live.RunCts != null && !live.RunCts.IsCancellationRequested)
            {
                try { live.RunCts.Cancel(); } catch { }
            }
            live.RunCts = new CancellationTokenSource();
            var ct = live.RunCts.Token;
            var sessionId = live.SessionId;
            var text = request.UserMessage;
            Task.Run(() => RunFollowUpAsync(sessionId, text, ct));
        }

        public static void Resume(CursorAgentResumeRequestDto request, AppClientIdentity? identity = null)
        {
            var live = GetOrHydrate(request?.SessionId);
            CursorAgentIdentity.Capture(live, identity);
            if (string.IsNullOrWhiteSpace(live.CursorAgentId))
                throw new InvalidOperationException("Cannot resume: CursorAgentId is missing.");

            var text = string.IsNullOrWhiteSpace(request.UserMessage)
                ? "Continue from where we left off."
                : request.UserMessage;
            FollowUp(new CursorAgentFollowUpRequestDto { SessionId = live.SessionId, UserMessage = text }, identity);
        }

        public static async Task CancelAsync(string sessionId)
        {
            CursorAgentSessionStore.SessionData live;
            if (!CursorAgentSessionStore.TryGet(sessionId, out live) || live == null)
                live = CursorAgentSessionBL.HydrateLive(sessionId);
            if (live == null) return;
            try { live.RunCts?.Cancel(); } catch { }
            try
            {
                await CursorCloudClient.CancelAsync(live.CursorAgentId, live.LatestRunId, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch { }
            CursorAgentSessionStore.Enqueue(sessionId, new CursorAgentEventDto
            {
                EventType = "error",
                Error = "Cancelled."
            });
        }

        private static async Task RunCreateAsync(string sessionId, string userMessage, CancellationToken ct)
        {
            try
            {
                CursorAgentSessionStore.SessionData live;
                if (!CursorAgentSessionStore.TryGet(sessionId, out live)) return;
                CursorAgentIdentity.Restore(live);

                var prompt = BuildPrompt(live, userMessage);
                var mcp = CursorAgentMcpBL.McpServerSpec(CursorAgentConfig.McpPublicBaseUrl, live.McpToken);
                var created = await CursorCloudClient.CreateAgentAsync(prompt, mcp, ct).ConfigureAwait(false);
                live.CursorAgentId = created.AgentId;
                live.LatestRunId = created.RunId;
                CursorAgentSessionBL.Update(live, "InProgress", null, null);
                await StreamAsync(live, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                CursorAgentSessionStore.Enqueue(sessionId, new CursorAgentEventDto { EventType = "error", Error = "Cancelled." });
            }
            catch (Exception ex)
            {
                CursorAgentSessionStore.Enqueue(sessionId, new CursorAgentEventDto { EventType = "error", Error = FormatError(ex) });
                CursorAgentSessionStore.SessionData live;
                if (CursorAgentSessionStore.TryGet(sessionId, out live))
                    CursorAgentSessionBL.Update(live, "Failed", FormatError(ex), null);
            }
        }

        private static async Task RunFollowUpAsync(string sessionId, string userMessage, CancellationToken ct)
        {
            try
            {
                CursorAgentSessionStore.SessionData live;
                if (!CursorAgentSessionStore.TryGet(sessionId, out live))
                {
                    CursorAgentSessionStore.Enqueue(sessionId, new CursorAgentEventDto
                    {
                        EventType = "error",
                        Error = "Session not found on server. Try Resume or start a new chat."
                    });
                    return;
                }
                CursorAgentIdentity.Restore(live);
                await CursorCloudClient.EnsureIdleAsync(live.CursorAgentId, live.LatestRunId, ct).ConfigureAwait(false);
                var mcp = CursorAgentMcpBL.McpServerSpec(CursorAgentConfig.McpPublicBaseUrl, live.McpToken);
                var prompt = userMessage;
                if (!string.IsNullOrWhiteSpace(live.SkillKey))
                    prompt = "Active skill: " + live.SkillKey + "\n\n" + userMessage;
                CursorCloudClient.CreateResult created;
                try
                {
                    created = await CursorCloudClient.FollowUpAsync(live.CursorAgentId, prompt, mcp, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (CursorCloudClient.IsBusyError(ex))
                {
                    await CursorCloudClient.EnsureIdleAsync(live.CursorAgentId, live.LatestRunId, ct).ConfigureAwait(false);
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                    created = await CursorCloudClient.FollowUpAsync(live.CursorAgentId, prompt, mcp, ct)
                        .ConfigureAwait(false);
                }
                live.LatestRunId = created.RunId;
                if (string.IsNullOrWhiteSpace(live.LatestRunId))
                    throw new InvalidOperationException("Follow-up did not return a run id.");
                CursorAgentSessionBL.Update(live, "InProgress", null, null);
                await StreamAsync(live, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                CursorAgentSessionStore.Enqueue(sessionId, new CursorAgentEventDto { EventType = "error", Error = "Cancelled." });
            }
            catch (Exception ex)
            {
                CursorAgentSessionStore.Enqueue(sessionId, new CursorAgentEventDto { EventType = "error", Error = FormatError(ex) });
            }
        }

        private class StreamCapture
        {
            public string Error;
            public bool SawSimplifiedText;
        }

        private static async Task StreamAsync(CursorAgentSessionStore.SessionData live, CancellationToken ct)
        {
            var assistant = new System.Text.StringBuilder();
            var capture = new StreamCapture();
            try
            {
                await CursorCloudClient.StreamRunAsync(live.CursorAgentId, live.LatestRunId, (evt, payload) =>
                {
                    HandleStreamEvent(live, assistant, evt, payload, capture);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (CursorCloudClient.IsStreamGone(ex) && !ct.IsCancellationRequested)
            {
            }

            if (assistant.Length == 0 && !ct.IsCancellationRequested)
            {
                var recovered = await RecoverFinishedRunTextAsync(live, ct).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(recovered))
                    assistant.Append(recovered);
            }

            if (!string.IsNullOrEmpty(capture.Error) && assistant.Length == 0)
                throw new InvalidOperationException(capture.Error);

            try
            {
                await PullCloudArtifactsAsync(live, ct).ConfigureAwait(false);
            }
            catch { }

            var final = CursorWorkspaceBL.RewriteCloudPaths(assistant.ToString(), live.WorkspaceRelativePath, live.CompanyId);
            live.ConversationHistory.Add(new CursorAgentMessageDto { Role = "assistant", Content = final, Timestamp = DateTime.UtcNow.ToString("o") });
            var files = CursorWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId)
                .Where(f => !f.IsDirectory)
                .Select(f => f.RelativePath)
                .ToList();
            CursorAgentSessionStore.Enqueue(live.SessionId, new CursorAgentEventDto
            {
                EventType = "done",
                Done = new CursorAgentDoneEvent
                {
                    FinalResponse = final,
                    UpdatedHistory = live.ConversationHistory.ToList(),
                    WorkspaceFiles = files
                }
            });
            CursorAgentSessionBL.Update(live, "Completed", final, null);
        }

        private static void HandleStreamEvent(
            CursorAgentSessionStore.SessionData live,
            System.Text.StringBuilder assistant,
            string evt,
            Newtonsoft.Json.Linq.JObject payload,
            StreamCapture capture)
        {
            if (string.Equals(evt, "error", StringComparison.OrdinalIgnoreCase))
            {
                capture.Error = (string)payload?["message"] ?? (string)payload?["code"] ?? "Cursor run error";
                return;
            }
            if (string.Equals(evt, "interaction_update", StringComparison.OrdinalIgnoreCase))
            {
                var type = (string)payload?["type"];
                if (string.Equals(type, "thinking-delta", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "thinking", StringComparison.OrdinalIgnoreCase))
                {
                    if (!capture.SawSimplifiedText)
                        EnqueueThinking(live, PayloadText(payload) ?? (string)payload?["text"]);
                    return;
                }
                if (capture.SawSimplifiedText) return;
                if (string.Equals(type, "text-delta", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(type, "token-delta", StringComparison.OrdinalIgnoreCase))
                {
                    AppendAssistant(live, assistant, PayloadText(payload));
                }
                return;
            }
            if (string.Equals(evt, "assistant", StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt, "delta", StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt, "message", StringComparison.OrdinalIgnoreCase))
            {
                capture.SawSimplifiedText = true;
                AppendAssistant(live, assistant, PayloadText(payload));
                return;
            }
            if (string.Equals(evt, "thinking", StringComparison.OrdinalIgnoreCase))
            {
                capture.SawSimplifiedText = true;
                EnqueueThinking(live, (string)payload?["text"] ?? PayloadText(payload));
                return;
            }
            if (string.Equals(evt, "tool_call", StringComparison.OrdinalIgnoreCase))
            {
                var name = (string)payload["name"];
                var status = (string)payload["status"];
                CursorAgentSessionStore.Enqueue(live.SessionId, new CursorAgentEventDto
                {
                    EventType = "step",
                    Step = new CursorAgentStepEvent
                    {
                        Type = "tool_call",
                        ToolName = name,
                        Description = (name ?? "tool") + " " + (status ?? ""),
                        Details = payload.ToString(),
                        IsSuccess = !string.Equals(status, "error", StringComparison.OrdinalIgnoreCase)
                    }
                });
                return;
            }
            if (string.Equals(evt, "result", StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt, "status", StringComparison.OrdinalIgnoreCase)
                || string.Equals(evt, "done", StringComparison.OrdinalIgnoreCase))
            {
                var status = (string)payload?["status"];
                if (string.Equals(status, "ERROR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    capture.Error = PayloadText(payload)
                        ?? (string)payload?["message"]
                        ?? ("Cursor run " + status);
                }
                var text = PayloadText(payload);
                if (string.IsNullOrEmpty(text))
                    text = RunResultText(payload["result"]);
                if (!string.IsNullOrEmpty(text) && assistant.Length == 0)
                    assistant.Append(text);
            }
        }

        private static void AppendAssistant(
            CursorAgentSessionStore.SessionData live,
            System.Text.StringBuilder assistant,
            string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            assistant.Append(text);
            CursorAgentSessionStore.Enqueue(live.SessionId, new CursorAgentEventDto { EventType = "token", Token = text });
        }

        private static void EnqueueThinking(CursorAgentSessionStore.SessionData live, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            CursorAgentSessionStore.Enqueue(live.SessionId, new CursorAgentEventDto
            {
                EventType = "step",
                Step = new CursorAgentStepEvent { Type = "thinking", Description = Trim(text, 200), Details = text }
            });
        }

        private static string PayloadText(Newtonsoft.Json.Linq.JObject payload)
        {
            if (payload == null) return null;
            var text = (string)payload["text"]
                ?? (string)payload["content"]
                ?? (string)payload["delta"]
                ?? (string)payload["message"];
            if (!string.IsNullOrEmpty(text)) return text;
            var nested = payload["delta"] as Newtonsoft.Json.Linq.JObject
                ?? payload["message"] as Newtonsoft.Json.Linq.JObject;
            if (nested != null)
                return (string)nested["text"] ?? (string)nested["content"];
            return null;
        }

        private static string RunResultText(Newtonsoft.Json.Linq.JToken token)
        {
            if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null) return null;
            if (token.Type == Newtonsoft.Json.Linq.JTokenType.String) return (string)token;
            var obj = token as Newtonsoft.Json.Linq.JObject;
            if (obj != null)
                return (string)(obj["text"] ?? obj["content"] ?? obj["message"]);
            return token.ToString();
        }

        private static async Task<string> RecoverFinishedRunTextAsync(
            CursorAgentSessionStore.SessionData live, CancellationToken ct)
        {
            for (var i = 0; i < 30 && !ct.IsCancellationRequested; i++)
            {
                var run = await CursorCloudClient.GetRunAsync(live.CursorAgentId, live.LatestRunId, ct)
                    .ConfigureAwait(false);
                var status = ((string)run?["status"] ?? "").ToUpperInvariant();
                if (CursorCloudClient.IsActiveStatus(status))
                {
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                    continue;
                }
                return RunResultText(run?["result"]) ?? (string)run?["text"] ?? (string)run?["message"];
            }
            return null;
        }

        public static CursorAgentSkillMenuDto ListSkillMenu()
        {
            return CursorAgentSkillCatalogBL.ListMenu();
        }

        private static string BuildPrompt(CursorAgentSessionStore.SessionData live, string userMessage)
        {
            return CursorAgentSkillCatalogBL.BuildInjectedPrompt(live, userMessage);
        }

        private static CursorAgentSessionStore.SessionData GetOrHydrate(string sessionId)
        {
            CursorAgentSessionStore.SessionData live;
            if (CursorAgentSessionStore.TryGet(sessionId, out live) && live != null)
                return live;
            live = CursorAgentSessionBL.HydrateLive(sessionId);
            if (live == null)
                throw new InvalidOperationException("Session not found.");
            return live;
        }

        private static async Task PullCloudArtifactsAsync(CursorAgentSessionStore.SessionData live, CancellationToken ct)
        {
            if (live == null || string.IsNullOrWhiteSpace(live.CursorAgentId)) return;
            var paths = await CursorCloudClient.ListArtifactPathsAsync(live.CursorAgentId, ct).ConfigureAwait(false);
            foreach (var path in paths)
            {
                var bytes = await CursorCloudClient.DownloadArtifactBytesAsync(live.CursorAgentId, path, ct)
                    .ConfigureAwait(false);
                if (bytes == null || bytes.Length == 0) continue;
                var rel = NormalizeArtifactPath(path);
                CursorWorkspaceBL.WriteBytes(live.WorkspaceRelativePath, rel, bytes, live.CompanyId);
                CursorAgentSessionStore.Enqueue(live.SessionId, new CursorAgentEventDto
                {
                    EventType = "file",
                    File = new CursorAgentFileEvent { Action = "artifact", RelativePath = rel }
                });
            }
        }

        private static string NormalizeArtifactPath(string path)
        {
            var p = (path ?? "").Replace('\\', '/').TrimStart('/');
            const string opt = "opt/cursor/artifacts/";
            if (p.StartsWith("/opt/cursor/artifacts/", StringComparison.OrdinalIgnoreCase))
                p = p.Substring("/opt/cursor/artifacts/".Length);
            else if (p.StartsWith(opt, StringComparison.OrdinalIgnoreCase))
                p = p.Substring(opt.Length);
            if (p.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase))
                return p;
            return "artifacts/" + p;
        }

        private static string FormatError(Exception ex)
        {
            if (ex == null) return "Unknown error.";
            if (CursorCloudClient.IsBusyError(ex))
                return "上一轮 Cursor 任务还在跑。请等几秒再发，或点 New 开新对话。";
            if (CursorCloudClient.IsStreamGone(ex))
                return "这一轮的实时流已经结束。请再发一次消息继续，或点 New。";
            var msg = ex.GetType().Name + ": " + ex.Message;
            if (ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message))
                msg += " (" + ex.InnerException.Message + ")";
            return msg;
        }

        private static string Trim(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
            return text.Substring(0, max) + "…";
        }
    }
}
