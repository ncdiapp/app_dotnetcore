using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using Newtonsoft.Json;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationAgentBL
    {
        public static AppDataIntegrationAgentStartResultDto StartSession(AppDataIntegrationAgentStartRequestDto request, AppClientIdentity? identity)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
                throw new ArgumentException("UserMessage is required.");
            if (!request.SaasApplicationId.HasValue || request.SaasApplicationId.Value <= 0)
                throw new ArgumentException("SaasApplicationId is required.");
            if (string.IsNullOrWhiteSpace(AppDataIntegrationAgentConfig.ApiKey))
                throw new InvalidOperationException("Cursor:ApiKey is not configured.");

            var live = AppDataIntegrationAgentSessionStore.CreateSession();
            live.SaasApplicationId = request.SaasApplicationId;
            live.DataSourceRegisterId = AppDataIntegrationAgentDataSourceBL.NormalizeSessionDataSource(
                request.DataSourceRegisterId);
            AppDataIntegrationAgentSkillCatalogBL.ApplyToSession(live, request.SkillKey);
            AppDataIntegrationAgentIdentity.Capture(live, identity);
            live.WorkspaceRelativePath = live.SessionId;
            live.ConversationHistory = request.ConversationHistory ?? new List<AppDataIntegrationAgentMessageDto>();
            live.ConversationHistory.Add(new AppDataIntegrationAgentMessageDto { Role = "user", Content = request.UserMessage, Timestamp = DateTime.UtcNow.ToString("o") });
            AppDataIntegrationWorkspaceBL.EnsureSessionDir(live.WorkspaceRelativePath, live.CompanyId);
            AppDataIntegrationAgentSessionBL.SaveNew(live, request.UserMessage);

            live.RunCts = new CancellationTokenSource();
            var ct = live.RunCts.Token;
            var sessionId = live.SessionId;
            var userMessage = request.UserMessage;
            Task.Run(() => RunCreateAsync(sessionId, userMessage, ct));

            return new AppDataIntegrationAgentStartResultDto
            {
                IsStarted = true,
                SessionId = live.SessionId,
                WorkspaceRelativePath = live.WorkspaceRelativePath
            };
        }

        public static void FollowUp(AppDataIntegrationAgentFollowUpRequestDto request, AppClientIdentity? identity = null)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
                throw new ArgumentException("SessionId is required.");
            if (string.IsNullOrWhiteSpace(request.UserMessage))
                throw new ArgumentException("UserMessage is required.");

            var live = GetOrHydrate(request.SessionId);
            AppDataIntegrationAgentIdentity.Capture(live, identity);
            if (string.IsNullOrWhiteSpace(live.CloudAgentId))
                throw new InvalidOperationException("Cursor agent has not been created for this session yet.");

            if (!string.IsNullOrWhiteSpace(request.SkillKey))
                AppDataIntegrationAgentSkillCatalogBL.ApplyToSession(live, request.SkillKey);
            if (request.SaasApplicationId.HasValue && request.SaasApplicationId.Value > 0)
                live.SaasApplicationId = request.SaasApplicationId;
            if (request.DataSourceRegisterId.HasValue)
            {
                var reqDs = request.DataSourceRegisterId > 0 ? request.DataSourceRegisterId : null;
                live.DataSourceRegisterId = AppDataIntegrationAgentDataSourceBL.NormalizeSessionDataSource(reqDs);
            }

            live.ConversationHistory.Add(new AppDataIntegrationAgentMessageDto { Role = "user", Content = request.UserMessage, Timestamp = DateTime.UtcNow.ToString("o") });
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

        public static void Resume(AppDataIntegrationAgentResumeRequestDto request, AppClientIdentity? identity = null)
        {
            var live = GetOrHydrate(request?.SessionId);
            AppDataIntegrationAgentIdentity.Capture(live, identity);
            if (string.IsNullOrWhiteSpace(live.CloudAgentId))
                throw new InvalidOperationException("Cannot resume: CloudAgentId is missing.");

            var text = string.IsNullOrWhiteSpace(request.UserMessage)
                ? "Continue from where we left off."
                : request.UserMessage;
            FollowUp(new AppDataIntegrationAgentFollowUpRequestDto { SessionId = live.SessionId, UserMessage = text }, identity);
        }

        public static async Task CancelAsync(string sessionId)
        {
            AppDataIntegrationAgentSessionStore.SessionData live;
            if (!AppDataIntegrationAgentSessionStore.TryGet(sessionId, out live) || live == null)
                live = AppDataIntegrationAgentSessionBL.HydrateLive(sessionId);
            if (live == null) return;
            try { live.RunCts?.Cancel(); } catch { }
            try
            {
                await CursorCloudClient.CancelAsync(live.CloudAgentId, live.LatestRunId, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch { }
            AppDataIntegrationAgentSessionStore.Enqueue(sessionId, new AppDataIntegrationAgentEventDto
            {
                EventType = "error",
                Error = "Cancelled."
            });
        }

        private static async Task RunCreateAsync(string sessionId, string userMessage, CancellationToken ct)
        {
            try
            {
                AppDataIntegrationAgentSessionStore.SessionData live;
                if (!AppDataIntegrationAgentSessionStore.TryGet(sessionId, out live)) return;
                AppDataIntegrationAgentIdentity.Restore(live);
                AppDataIntegrationAgentSessionStore.BeginAssistantTurn(live);

                var prompt = BuildPrompt(live, userMessage);
                var mcp = AppDataIntegrationAgentMcpBL.McpServerSpec(AppDataIntegrationAgentConfig.McpPublicBaseUrl, live.McpToken);
                var created = await CursorCloudClient.CreateAgentAsync(prompt, mcp, ct).ConfigureAwait(false);
                live.CloudAgentId = created.AgentId;
                live.LatestRunId = created.RunId;
                AppDataIntegrationAgentSessionBL.Update(live, "InProgress", null, null);
                await StreamAsync(live, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                AppDataIntegrationAgentSessionStore.Enqueue(sessionId, new AppDataIntegrationAgentEventDto { EventType = "error", Error = "Cancelled." });
            }
            catch (Exception ex)
            {
                AppDataIntegrationAgentSessionStore.Enqueue(sessionId, new AppDataIntegrationAgentEventDto { EventType = "error", Error = FormatError(ex) });
                AppDataIntegrationAgentSessionStore.SessionData live;
                if (AppDataIntegrationAgentSessionStore.TryGet(sessionId, out live))
                    AppDataIntegrationAgentSessionBL.Update(live, "Failed", FormatError(ex), null);
            }
        }

        private static async Task RunFollowUpAsync(string sessionId, string userMessage, CancellationToken ct)
        {
            try
            {
                AppDataIntegrationAgentSessionStore.SessionData live;
                if (!AppDataIntegrationAgentSessionStore.TryGet(sessionId, out live))
                {
                    AppDataIntegrationAgentSessionStore.Enqueue(sessionId, new AppDataIntegrationAgentEventDto
                    {
                        EventType = "error",
                        Error = "Session not found on server. Try Resume or start a new chat."
                    });
                    return;
                }
                AppDataIntegrationAgentIdentity.Restore(live);
                AppDataIntegrationAgentSessionStore.BeginAssistantTurn(live);

                if (!string.IsNullOrWhiteSpace(live.LatestRunId))
                {
                    var activeRun = await CursorCloudClient.GetRunAsync(live.CloudAgentId, live.LatestRunId, ct)
                        .ConfigureAwait(false);
                    var activeStatus = ((string)activeRun?["status"] ?? "").ToUpperInvariant();
                    if (CursorCloudClient.IsActiveStatus(activeStatus))
                    {
                        AppDataIntegrationAgentSessionBL.Update(live, "InProgress", null, null);
                        await StreamAsync(live, ct).ConfigureAwait(false);
                        return;
                    }
                }

                await CursorCloudClient.EnsureIdleAsync(live.CloudAgentId, live.LatestRunId, ct).ConfigureAwait(false);
                var mcp = AppDataIntegrationAgentMcpBL.McpServerSpec(AppDataIntegrationAgentConfig.McpPublicBaseUrl, live.McpToken);
                var prompt = AppDataIntegrationAgentSkillCatalogBL.BuildFollowUpPrompt(live, userMessage);
                CursorCloudClient.CreateResult created;
                try
                {
                    created = await CursorCloudClient.FollowUpAsync(live.CloudAgentId, prompt, mcp, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (CursorCloudClient.IsArchivedError(ex))
                {
                    await CursorCloudClient.UnarchiveAgentAsync(live.CloudAgentId, ct).ConfigureAwait(false);
                    created = await CursorCloudClient.FollowUpAsync(live.CloudAgentId, prompt, mcp, ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (CursorCloudClient.IsBusyError(ex))
                {
                    await CursorCloudClient.EnsureIdleAsync(live.CloudAgentId, live.LatestRunId, ct).ConfigureAwait(false);
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                    created = await CursorCloudClient.FollowUpAsync(live.CloudAgentId, prompt, mcp, ct)
                        .ConfigureAwait(false);
                }
                live.LatestRunId = created.RunId;
                if (string.IsNullOrWhiteSpace(live.LatestRunId))
                    throw new InvalidOperationException("Follow-up did not return a run id.");
                AppDataIntegrationAgentSessionBL.Update(live, "InProgress", null, null);
                await StreamAsync(live, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                AppDataIntegrationAgentSessionStore.Enqueue(sessionId, new AppDataIntegrationAgentEventDto { EventType = "error", Error = "Cancelled." });
            }
            catch (Exception ex)
            {
                AppDataIntegrationAgentSessionStore.Enqueue(sessionId, new AppDataIntegrationAgentEventDto { EventType = "error", Error = FormatError(ex) });
            }
        }

        private class StreamCapture
        {
            public string Error;
            public bool SawSimplifiedText;
            public bool StreamDisconnected;
        }

        private class RunRecoveryResult
        {
            public string Text { get; set; }
            public string TerminalStatus { get; set; }
            public string ErrorMessage { get; set; }
            public bool RunStillActive { get; set; }
            public string LastRunStatus { get; set; }
        }

        private static async Task StreamAsync(AppDataIntegrationAgentSessionStore.SessionData live, CancellationToken ct)
        {
            var assistant = new System.Text.StringBuilder();
            var capture = new StreamCapture();
            RunRecoveryResult recovery = null;
            try
            {
                await CursorCloudClient.StreamRunAsync(live.CloudAgentId, live.LatestRunId, (evt, payload) =>
                {
                    HandleStreamEvent(live, assistant, evt, payload, capture);
                }, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (CursorCloudClient.IsStreamGone(ex) && !ct.IsCancellationRequested)
            {
                capture.StreamDisconnected = true;
            }

            var needsRecovery = !ct.IsCancellationRequested
                && (assistant.Length == 0 || capture.StreamDisconnected
                    || CursorCloudClient.IsRecoverableStreamMessage(capture.Error));

            if (needsRecovery)
            {
                if (CursorCloudClient.IsRecoverableStreamMessage(capture.Error))
                    capture.Error = null;

                try { await PullCloudArtifactsAsync(live, ct).ConfigureAwait(false); }
                catch { }

                recovery = await RecoverRunAsync(live, ct, capture.StreamDisconnected).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(recovery.Text))
                    assistant.Append(recovery.Text);
                if (assistant.Length == 0 && !string.IsNullOrEmpty(recovery.ErrorMessage))
                    capture.Error = recovery.ErrorMessage;
                else if (assistant.Length == 0 && recovery.TerminalStatus == "TIMEOUT")
                    capture.Error = "Cursor run did not finish within "
                        + AppDataIntegrationAgentConfig.RunRecoveryMaxMinutes
                        + " minutes. Send Continue to re-attach, or New for a fresh chat.";
            }

            try
            {
                await PullCloudArtifactsAsync(live, ct).ConfigureAwait(false);
            }
            catch { }

            var isIncomplete = recovery != null
                && string.Equals(recovery.TerminalStatus, "TIMEOUT", StringComparison.OrdinalIgnoreCase)
                && (recovery.RunStillActive || string.IsNullOrEmpty(recovery.Text));

            if (!string.IsNullOrEmpty(capture.Error) && assistant.Length == 0)
            {
                var fallback = BuildWorkspaceFallbackMessage(live);
                if (!string.IsNullOrEmpty(fallback))
                    assistant.Append(fallback);
                else if (CursorCloudClient.IsRecoverableStreamMessage(capture.Error))
                    throw new InvalidOperationException(capture.Error);
                else
                    throw new InvalidOperationException(capture.Error);
            }

            var final = AppDataIntegrationWorkspaceBL.RewriteCloudPaths(assistant.ToString(), live.WorkspaceRelativePath, live.CompanyId);
            if (string.IsNullOrWhiteSpace(final))
                final = BuildWorkspaceFallbackMessage(live) ?? "";

            if (isIncomplete)
            {
                var notice = BuildIncompleteRunNotice(live, recovery);
                if (!string.IsNullOrWhiteSpace(final))
                    final = final.TrimEnd() + "\n\n" + notice;
                else
                    final = notice;
            }

            var openOffers = AppDataIntegrationAgentSessionStore.TakeTurnOpenOffers(live);
            live.ConversationHistory.Add(new AppDataIntegrationAgentMessageDto
            {
                Role = "assistant",
                Content = final,
                Timestamp = DateTime.UtcNow.ToString("o"),
                WrittenPackPaths = AppDataIntegrationAgentSessionStore.TakeTurnPackPaths(live),
                OpenUiOffers = openOffers
            });
            var files = AppDataIntegrationWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId)
                .Where(f => !f.IsDirectory)
                .Select(f => f.RelativePath)
                .ToList();
            AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto
            {
                EventType = "done",
                Done = new AppDataIntegrationAgentDoneEvent
                {
                    FinalResponse = final,
                    UpdatedHistory = live.ConversationHistory.ToList(),
                    WorkspaceFiles = files,
                    OpenUiOffers = openOffers,
                    IsIncomplete = isIncomplete
                }
            });
            AppDataIntegrationAgentSessionBL.Update(live, isIncomplete ? "InProgress" : "Completed", final, null);
        }

        private static void HandleStreamEvent(
            AppDataIntegrationAgentSessionStore.SessionData live,
            System.Text.StringBuilder assistant,
            string evt,
            Newtonsoft.Json.Linq.JObject payload,
            StreamCapture capture)
        {
            if (string.Equals(evt, "error", StringComparison.OrdinalIgnoreCase))
            {
                var err = (string)payload?["message"] ?? (string)payload?["code"] ?? "Cursor run error";
                if (CursorCloudClient.IsRecoverableStreamMessage(err))
                    capture.StreamDisconnected = true;
                else
                    capture.Error = err;
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
                AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto
                {
                    EventType = "step",
                    Step = new AppDataIntegrationAgentStepEvent
                    {
                        Type = "tool_call",
                        ToolName = name,
                        Description = FormatToolCallStepDescription(name, status, payload),
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

        private static string FormatToolCallStepDescription(string name, string status, Newtonsoft.Json.Linq.JObject payload)
        {
            var args = payload?["args"] as Newtonsoft.Json.Linq.JObject;
            var taskDesc = (string)args?["description"];
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(name))
                sb.Append(name.Trim());
            if (!string.IsNullOrWhiteSpace(status))
                sb.Append(sb.Length > 0 ? " " : "").Append(status.Trim());
            if (!string.IsNullOrWhiteSpace(taskDesc))
                sb.Append(" — ").Append(Trim(taskDesc.Trim(), 160));
            return sb.Length > 0 ? sb.ToString() : "tool";
        }

        private static void AppendAssistant(
            AppDataIntegrationAgentSessionStore.SessionData live,
            System.Text.StringBuilder assistant,
            string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            assistant.Append(text);
            AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto { EventType = "token", Token = text });
        }

        private static void EnqueueThinking(AppDataIntegrationAgentSessionStore.SessionData live, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto
            {
                EventType = "step",
                Step = new AppDataIntegrationAgentStepEvent { Type = "thinking", Description = text, Details = text }
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

        private static async Task<RunRecoveryResult> RecoverRunAsync(
            AppDataIntegrationAgentSessionStore.SessionData live,
            CancellationToken ct,
            bool streamDisconnected)
        {
            var result = new RunRecoveryResult();
            var pollSec = AppDataIntegrationAgentConfig.RunRecoveryPollSeconds;
            var maxMinutes = AppDataIntegrationAgentConfig.RunRecoveryMaxMinutes;
            var maxAttempts = Math.Max(1, (maxMinutes * 60) / pollSec);
            var stillWorkingEvery = Math.Max(1, 60 / pollSec);
            var artifactPullEvery = Math.Max(1, 15 / pollSec);

            if (streamDisconnected)
                EnqueueStillWorking(live, "Real-time stream ended; waiting for Cursor cloud run to finish…");

            for (var i = 0; i < maxAttempts && !ct.IsCancellationRequested; i++)
            {
                if (i > 0 && i % stillWorkingEvery == 0)
                    EnqueueStillWorking(live, "Cursor agent still running (" + (i * pollSec) + "s)…");

                if (i > 0 && i % artifactPullEvery == 0)
                {
                    try { await PullCloudArtifactsAsync(live, ct).ConfigureAwait(false); }
                    catch { }
                }

                var run = await CursorCloudClient.GetRunAsync(live.CloudAgentId, live.LatestRunId, ct)
                    .ConfigureAwait(false);
                if (run == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollSec), ct).ConfigureAwait(false);
                    continue;
                }

                var status = ((string)run["status"] ?? "").ToUpperInvariant();
                result.LastRunStatus = status;
                if (CursorCloudClient.IsActiveStatus(status))
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollSec), ct).ConfigureAwait(false);
                    continue;
                }

                result.TerminalStatus = status;
                if (string.Equals(status, "ERROR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = RunResultText(run["result"])
                        ?? (string)run["message"]
                        ?? ("Cursor run " + status);
                    return result;
                }

                result.Text = RunResultText(run["result"]) ?? (string)run["text"] ?? (string)run["message"];
                return result;
            }

            try
            {
                var finalRun = await CursorCloudClient.GetRunAsync(live.CloudAgentId, live.LatestRunId, ct)
                    .ConfigureAwait(false);
                var finalStatus = ((string)finalRun?["status"] ?? "").ToUpperInvariant();
                result.LastRunStatus = finalStatus;
                if (!string.IsNullOrEmpty(finalStatus) && !CursorCloudClient.IsActiveStatus(finalStatus))
                {
                    result.TerminalStatus = finalStatus;
                    if (string.Equals(finalStatus, "ERROR", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(finalStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                    {
                        result.ErrorMessage = RunResultText(finalRun?["result"])
                            ?? (string)finalRun?["message"]
                            ?? ("Cursor run " + finalStatus);
                    }
                    else
                        result.Text = RunResultText(finalRun?["result"])
                            ?? (string)finalRun?["text"]
                            ?? (string)finalRun?["message"];
                    return result;
                }

                result.RunStillActive = CursorCloudClient.IsActiveStatus(finalStatus);
            }
            catch
            {
                result.RunStillActive = true;
            }

            result.TerminalStatus = "TIMEOUT";
            return result;
        }

        private static void EnqueueStillWorking(AppDataIntegrationAgentSessionStore.SessionData live, string message)
        {
            if (live == null || string.IsNullOrWhiteSpace(message)) return;
            AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto
            {
                EventType = "step",
                Step = new AppDataIntegrationAgentStepEvent
                {
                    Type = "still_working",
                    Description = message,
                    Details = message,
                    IsSuccess = true
                }
            });
        }

        private static string BuildWorkspaceFallbackMessage(AppDataIntegrationAgentSessionStore.SessionData live)
        {
            if (live == null) return null;
            var packs = AppDataIntegrationAgentSessionStore.PeekTurnPackPaths(live);
            var files = AppDataIntegrationWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId)
                .Where(f => !f.IsDirectory)
                .Select(f => f.RelativePath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var paths = new List<string>();
            if (packs != null)
                paths.AddRange(packs.Where(p => !string.IsNullOrWhiteSpace(p)));
            foreach (var f in files)
            {
                if (!paths.Any(p => string.Equals(p, f, StringComparison.OrdinalIgnoreCase)))
                    paths.Add(f);
            }

            if (paths.Count == 0) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Cursor did not return a text reply, but workspace files were updated:");
            foreach (var p in paths.Take(20))
                sb.AppendLine("- " + p);
            if (paths.Count > 20)
                sb.AppendLine("… and " + (paths.Count - 20) + " more.");
            return sb.ToString().Trim();
        }

        private static string BuildIncompleteRunNotice(
            AppDataIntegrationAgentSessionStore.SessionData live,
            RunRecoveryResult recovery)
        {
            if (live == null) return null;
            var maxMin = AppDataIntegrationAgentConfig.RunRecoveryMaxMinutes;
            var files = AppDataIntegrationWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId)
                .Where(f => !f.IsDirectory)
                .Select(f => f.RelativePath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("⚠ Task may be incomplete.");
            sb.AppendLine("Polling stopped after " + maxMin + " minutes.");
            if (recovery != null && recovery.RunStillActive)
                sb.AppendLine("Cursor cloud run is still "
                    + (string.IsNullOrWhiteSpace(recovery.LastRunStatus) ? "RUNNING" : recovery.LastRunStatus)
                    + ".");
            sb.AppendLine("Workspace currently has " + files.Count + " file(s). Large Phase B uploads may still be in progress.");
            if (files.Count > 0)
            {
                sb.AppendLine("Files so far:");
                foreach (var p in files.Take(20))
                    sb.AppendLine("- " + p);
                if (files.Count > 20)
                    sb.AppendLine("… and " + (files.Count - 20) + " more.");
            }
            sb.AppendLine();
            sb.AppendLine("Next step: send Continue (or click Resume) to re-attach and finish. Use New only if you want to discard this run.");
            sb.AppendLine("For large files, use Download in the workspace panel — preview may truncate very large files.");
            return sb.ToString().Trim();
        }

        public static AppDataIntegrationAgentSkillMenuDto ListSkillMenu()
        {
            return AppDataIntegrationAgentSkillCatalogBL.ListMenu();
        }

        private static string BuildPrompt(AppDataIntegrationAgentSessionStore.SessionData live, string userMessage)
        {
            return AppDataIntegrationAgentSkillCatalogBL.BuildInjectedPrompt(live, userMessage);
        }

        private static AppDataIntegrationAgentSessionStore.SessionData GetOrHydrate(string sessionId)
        {
            return AppDataIntegrationAgentSessionBL.RequireHydrated(sessionId);
        }

        /// <summary>
        /// Pull Cursor cloud artifacts into the session workspace (same path as generated images).
        /// Returns a JSON summary for MCP.
        /// </summary>
        public static async Task<string> SyncCloudArtifactsAsync(
            AppDataIntegrationAgentSessionStore.SessionData live,
            CancellationToken ct)
        {
            if (live == null)
                throw new InvalidOperationException("Session is required.");
            if (string.IsNullOrWhiteSpace(live.CloudAgentId))
                throw new InvalidOperationException("No Cursor cloud agent id on this session yet.");

            var pulled = await PullCloudArtifactsAsync(live, ct).ConfigureAwait(false);
            var files = AppDataIntegrationWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId)
                .Where(f => f != null && !f.IsDirectory)
                .Select(f => new
                {
                    f.RelativePath,
                    f.SizeBytes,
                    f.PublicUrl
                })
                .ToList();

            return JsonConvert.SerializeObject(new
            {
                cloudAgentId = live.CloudAgentId,
                artifactsListed = pulled.ListedCount,
                artifactsPulled = pulled.PulledCount,
                pulledPaths = pulled.PulledPaths,
                workspaceFiles = files
            }, Formatting.Indented);
        }

        private class ArtifactPullResult
        {
            public int ListedCount;
            public int PulledCount;
            public List<string> PulledPaths = new List<string>();
        }

        private static async Task<ArtifactPullResult> PullCloudArtifactsAsync(
            AppDataIntegrationAgentSessionStore.SessionData live,
            CancellationToken ct)
        {
            var result = new ArtifactPullResult();
            if (live == null || string.IsNullOrWhiteSpace(live.CloudAgentId)) return result;
            var paths = await CursorCloudClient.ListArtifactPathsAsync(live.CloudAgentId, ct).ConfigureAwait(false);
            result.ListedCount = paths?.Count ?? 0;
            if (paths == null || paths.Count == 0) return result;

            foreach (var path in paths)
            {
                var bytes = await CursorCloudClient.DownloadArtifactBytesAsync(live.CloudAgentId, path, ct)
                    .ConfigureAwait(false);
                if (bytes == null || bytes.Length == 0) continue;
                var rel = NormalizeArtifactPath(path);
                AppDataIntegrationWorkspaceBL.WriteBytesFromArtifact(live.WorkspaceRelativePath, rel, bytes, live.CompanyId);
                AppDataIntegrationAgentSessionStore.NotePackPath(live, rel);
                AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto
                {
                    EventType = "file",
                    File = new AppDataIntegrationAgentFileEvent { Action = "artifact", RelativePath = rel }
                });
                result.PulledCount++;
                result.PulledPaths.Add(rel + " (" + bytes.Length + " bytes)");
            }
            return result;
        }

        private static string NormalizeArtifactPath(string path)
        {
            var p = (path ?? "").Replace('\\', '/').TrimStart('/');
            const string opt = "opt/cursor/artifacts/";
            if (p.StartsWith("/opt/cursor/artifacts/", StringComparison.OrdinalIgnoreCase))
                p = p.Substring("/opt/cursor/artifacts/".Length);
            else if (p.StartsWith(opt, StringComparison.OrdinalIgnoreCase))
                p = p.Substring(opt.Length);

            if (p.StartsWith("agent/", StringComparison.OrdinalIgnoreCase))
                p = p.Substring("agent/".Length);

            // artifacts/output/... → output/... (and same for packs/scripts/notes/source)
            if (p.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase))
            {
                var rest = p.Substring("artifacts/".Length);
                if (rest.StartsWith("output/", StringComparison.OrdinalIgnoreCase)
                    || rest.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase)
                    || rest.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)
                    || rest.StartsWith("notes/", StringComparison.OrdinalIgnoreCase)
                    || rest.StartsWith("source/", StringComparison.OrdinalIgnoreCase))
                    return rest;
                return p;
            }

            if (p.StartsWith("output/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("packs/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("notes/", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("source/", StringComparison.OrdinalIgnoreCase))
                return p;

            return "artifacts/" + p;
        }

        private static string FormatError(Exception ex)
        {
            if (ex == null) return "Unknown error.";
            if (CursorCloudClient.IsBusyError(ex))
                return "上一轮 Cursor 任务还在跑。请等几秒再发，或点 New 开新对话。";
            if (CursorCloudClient.IsStreamGone(ex))
                return "Cursor 实时流已结束，但云端任务可能仍在执行。请稍候或再发 Continue；若超过 "
                    + AppDataIntegrationAgentConfig.RunRecoveryMaxMinutes
                    + " 分钟仍未完成，请再发 Continue 或点 New。";
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
