using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent;
using SkillSetBL = App.BL.AIAgent.AiSkill.AppAgentSkillSetBL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SessionBL = App.BL.AIAgent.GenericAgent.AppGenericAgentSessionBL;

namespace AppAI.Web.Controllers;

/// <summary>
/// Generic AI agent endpoint — any skill registered in AppAgentSkillSet.
///
/// Streaming pattern (SSE):
///   1. POST /RunAgent   → { SessionId, ChatSessionKey }
///   2. GET  /StreamEvents?sessionId=...  → SSE stream
///   3. React stops on event type "done" or "error"
///
/// Polling fallback:
///   GET /PollEvents?sessionId=...  (call every 500 ms)
///
/// Plan/schema/ask_user gates:
///   POST /ConfirmPlan    → unblocks propose_plan
///   POST /ConfirmSchema  → unblocks propose_schema
///   POST /ConfirmAskUser → unblocks ask_user
///
/// ChatSessionKey = AppGenericAgentSession.SessionKey:
///   empty on request → fixed SkillKey:UserId (Agent Management test RUN)
///   GUID → New/Continue Chat
/// File area folder = ChatSessionKey (sanitized).
/// </summary>
[Route("webapi/[controller]/[action]")]
public class GenericAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<GenericAgentStartResultDto> RunAgent(
        [FromBody] GenericAgentRequestDto request)
    {
        AppClientIdentity? agentIdentity = null;
        var currentIdentity = ServerContext.Instance.CurrnetClientIdentity;
        if (currentIdentity is AppClientIdentity ai) agentIdentity = ai;

        var agentUserId    = agentIdentity.HasValue && agentIdentity.Value.UserId != null
            ? Convert.ToInt32(agentIdentity.Value.UserId) : 0;
        var agentDsId      = agentIdentity.HasValue ? agentIdentity.Value.DataSourceId : 0;
        var agentCompanyId = agentIdentity.HasValue && agentIdentity.Value.CurrentWorkingCompanyId != null
            ? Convert.ToInt32(agentIdentity.Value.CurrentWorkingCompanyId) : 0;

        var result = new OperationCallResult<GenericAgentStartResultDto>();

        if (string.IsNullOrWhiteSpace(request?.SkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "GenericAgent_NoSkill",
                ValidationItemType.Error, "SkillKey is required."));
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "GenericAgent_NoMessage",
                ValidationItemType.Error, "UserMessage is required."));
            return result;
        }

        string chatSessionKey;
        if (string.IsNullOrWhiteSpace(request.ChatSessionKey))
        {
            chatSessionKey = agentUserId > 0
                ? SessionBL.MakeFixedKey(request.SkillKey.Trim(), agentUserId)
                : "";
        }
        else
        {
            chatSessionKey = request.ChatSessionKey.Trim();
            if (agentUserId > 0 &&
                !SessionBL.IsFixedKey(chatSessionKey, request.SkillKey.Trim(), agentUserId) &&
                SessionBL.LoadBySessionKey(chatSessionKey, request.SkillKey.Trim(), agentUserId) == null)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(GenericAgentController), "GenericAgent_BadChat",
                    ValidationItemType.Error, "ChatSessionKey not found for this agent."));
                return result;
            }
        }

        var sessionId = GenericAgentSessionStore.CreateSession();
        GenericAgentSessionStore.BindChat(
            sessionId, request.SkillKey.Trim(), chatSessionKey, agentUserId, agentDsId);

        if (agentUserId > 0 && !string.IsNullOrWhiteSpace(chatSessionKey))
            SessionBL.TryAutoTitle(chatSessionKey, request.SkillKey, agentUserId, request.UserMessage);

        // Mark the chat as started so reopening it does not fire a second [session_start].
        if (agentUserId > 0 && agentDsId > 0 && !string.IsNullOrWhiteSpace(chatSessionKey))
        {
            try
            {
                var existing = SessionBL.LoadBySessionKey(chatSessionKey, request.SkillKey.Trim(), agentUserId);
                if (existing != null && (existing.Messages == null || existing.Messages.Count == 0))
                {
                    SessionBL.SaveSession(
                        request.SkillKey, agentUserId, agentDsId,
                        new List<JObject>
                        {
                            new JObject
                            {
                                ["role"] = "system",
                                ["content"] = "[run_in_progress]",
                                ["runSessionId"] = sessionId
                            }
                        },
                        sessionKey: chatSessionKey);
                }
            }
            catch { /* must not block the run */ }
        }

        // Accumulate tool steps for this run so they can be persisted with the chat session.
        var persistedToolSteps = new List<JObject>();
        var tokenBuf = new System.Text.StringBuilder();

        var callbacks = new GenericAgentCallbacks
        {
            OnStep = step =>
            {
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "step", Step = step });
                if (step != null && !string.IsNullOrWhiteSpace(step.ToolName))
                {
                    if (string.Equals(step.Type, "tool_call", StringComparison.OrdinalIgnoreCase))
                    {
                        persistedToolSteps.Add(JObject.FromObject(new
                        {
                            toolName = step.ToolName,
                            label = step.Description,
                            args = step.Details,
                            isSuccess = true
                        }));
                    }
                    else if (string.Equals(step.Type, "tool_result", StringComparison.OrdinalIgnoreCase))
                    {
                        for (int i = persistedToolSteps.Count - 1; i >= 0; i--)
                        {
                            var row = persistedToolSteps[i];
                            if (string.Equals(row.Value<string>("toolName"), step.ToolName, StringComparison.OrdinalIgnoreCase)
                                && row["result"] == null)
                            {
                                row["result"] = step.Details;
                                row["isSuccess"] = step.IsSuccess;
                                break;
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            },
            OnToken = token =>
            {
                if (!string.IsNullOrEmpty(token)) tokenBuf.Append(token);
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "token", Token = token });
                return Task.CompletedTask;
            },
            OnDone = done =>
            {
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "done", Done = new AgentDoneEvent { FinalResponse = done } });
                if (agentUserId > 0 && agentDsId > 0)
                {
                    var isSessionStart = string.Equals(
                        request.UserMessage?.Trim(), "[session_start]", StringComparison.Ordinal);
                    SessionBL.PersistTurnDone(
                        request.SkillKey, agentUserId, agentDsId, chatSessionKey,
                        request.UserMessage, isSessionStart, done ?? "", persistedToolSteps);
                    SessionBL.TryAutoTitle(chatSessionKey, request.SkillKey, agentUserId, request.UserMessage);
                    if (agentCompanyId > 0 && !string.IsNullOrWhiteSpace(chatSessionKey))
                    {
                        try { GenericAgentFileBL.EnsureRoot(chatSessionKey, agentCompanyId, request.SkillKey); }
                        catch { /* file root ensure must not fail the run */ }
                    }
                }
                return Task.CompletedTask;
            },
            OnError = msg => { GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "error", Error = msg }); return Task.CompletedTask; },

            OnPlanReady = async planEvent =>
            {
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "plan", Plan = planEvent });
                var tcs = GenericAgentSessionStore.RegisterPlanConfirmation(sessionId);
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                cts.Token.Register(() => tcs.TrySetResult(false));
                return await tcs.Task.ConfigureAwait(false);
            },

            OnAskUser = async askEvent =>
            {
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "ask_user", AskUser = askEvent });
                if (agentUserId > 0 && agentDsId > 0)
                {
                    try
                    {
                        var draft = tokenBuf.ToString();
                        if (string.IsNullOrWhiteSpace(draft) && !string.IsNullOrWhiteSpace(askEvent?.Prompt))
                            draft = askEvent.Prompt;
                        SessionBL.PersistAskUserPending(
                            request.SkillKey, agentUserId, agentDsId, chatSessionKey,
                            draft,
                            askEvent != null ? JObject.FromObject(askEvent) : null,
                            sessionId,
                            persistedToolSteps);
                    }
                    catch { /* persist HITL snapshot must not fail the gate */ }
                }
                var tcs = GenericAgentSessionStore.RegisterAskUserConfirmation(sessionId);
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(60));
                cts.Token.Register(() => tcs.TrySetResult(new AgentAskUserResponse { Cancelled = true }));
                return await tcs.Task.ConfigureAwait(false);
            },

            OnSchemaReady = async schemaEvent =>
            {
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "schema", Schema = schemaEvent });
                var tcs = GenericAgentSessionStore.RegisterSchemaConfirmation(sessionId);
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                cts.Token.Register(() => tcs.TrySetResult(new AgentSchemaResponse { Confirmed = false, Feedback = "Schema review timed out." }));
                return await tcs.Task.ConfigureAwait(false);
            }
        };

        Task.Run(async () =>
        {
            await GenericAgentBL.RunAsync(
                request.SkillKey, request.UserMessage,
                request.Messages ?? new List<JObject>(),
                callbacks, agentIdentity,
                CancellationToken.None,
                chatSessionKey: chatSessionKey).ConfigureAwait(false);
        });

        result.Object = new GenericAgentStartResultDto
        {
            IsStarted = true,
            SessionId = sessionId,
            ChatSessionKey = chatSessionKey
        };
        return result;
    }

    [HttpGet]
    public AgentPollResponseDto PollEvents(string sessionId)
    {
        return GenericAgentSessionStore.DequeueAll(sessionId);
    }

    [HttpGet]
    public async Task StreamEvents(string sessionId, CancellationToken cancellationToken)
    {
        Response.ContentType              = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Connection"]    = "keep-alive";
        await Response.Body.FlushAsync(cancellationToken);

        bool done = false;
        while (!done && !cancellationToken.IsCancellationRequested)
        {
            await GenericAgentSessionStore.WaitForEventAsync(
                sessionId, TimeSpan.FromSeconds(30), cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            var poll = GenericAgentSessionStore.DequeueAll(sessionId);

            if (!poll.SessionExists)
            {
                var err = Encoding.UTF8.GetBytes("event: error\ndata: {\"error\":\"Session not found\"}\n\n");
                await Response.Body.WriteAsync(err, 0, err.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                break;
            }

            foreach (var evt in poll.Events)
            {
                var data  = JsonConvert.SerializeObject(evt);
                var bytes = Encoding.UTF8.GetBytes($"event: {evt.EventType}\ndata: {data}\n\n");
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                if (evt.EventType == "done" || evt.EventType == "error") done = true;
            }

            if (!done && poll.Events.Count == 0)
            {
                var ka = Encoding.UTF8.GetBytes(": keepalive\n\n");
                await Response.Body.WriteAsync(ka, 0, ka.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }

    [HttpPost]
    public OperationCallResult<bool> ConfirmPlan([FromBody] GenericAgentConfirmPlanDto request)
    {
        var result = new OperationCallResult<bool>();

        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "ConfirmPlan_NoSession",
                ValidationItemType.Error, "SessionId is required."));
            return result;
        }

        bool found = GenericAgentSessionStore.ConfirmPlan(request.SessionId, request.Confirmed);

        if (!found)
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "ConfirmPlan_NoPending",
                ValidationItemType.Warning, "No pending plan confirmation found. It may have already resolved or timed out."));

        result.Object = found;
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ConfirmSchema([FromBody] GenericAgentConfirmSchemaDto request)
    {
        var result = new OperationCallResult<bool>();

        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "ConfirmSchema_NoSession",
                ValidationItemType.Error, "SessionId is required."));
            return result;
        }

        var response = new AgentSchemaResponse
        {
            Confirmed  = request.Confirmed,
            SchemaJson = request.SchemaJson,
            Feedback   = request.Feedback
        };

        bool found = GenericAgentSessionStore.ConfirmSchema(request.SessionId, response);

        if (!found)
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "ConfirmSchema_NoPending",
                ValidationItemType.Warning, "No pending schema confirmation found. It may have already resolved or timed out."));

        result.Object = found;
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ConfirmAskUser([FromBody] GenericAgentConfirmAskUserDto request)
    {
        var result = new OperationCallResult<bool>();

        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "ConfirmAskUser_NoSession",
                ValidationItemType.Error, "SessionId is required."));
            return result;
        }

        var response = new AgentAskUserResponse
        {
            Cancelled = request.Cancelled,
            Answers = request.Answers ?? new Dictionary<string, string>(),
            SelectedIds = request.SelectedIds ?? new List<string>(),
            FreeText = request.FreeText
        };

        AppClientIdentity? identity = null;
        if (ServerContext.Instance.CurrnetClientIdentity is AppClientIdentity ai)
            identity = ai;
        var userId = identity.HasValue && identity.Value.UserId != null
            ? Convert.ToInt32(identity.Value.UserId) : 0;
        var dsId = identity.HasValue ? identity.Value.DataSourceId : 0;
        var bind = GenericAgentSessionStore.TryGet(request.SessionId);
        var skillKey = !string.IsNullOrWhiteSpace(request.SkillKey)
            ? request.SkillKey.Trim()
            : bind?.SkillKey;
        var chatKey = !string.IsNullOrWhiteSpace(request.ChatSessionKey)
            ? request.ChatSessionKey.Trim()
            : bind?.ChatSessionKey;
        if (userId <= 0 && bind != null) userId = bind.UserId;
        if (dsId <= 0 && bind != null) dsId = bind.DataSourceId;

        if (userId > 0 && dsId > 0
            && !string.IsNullOrWhiteSpace(skillKey)
            && !string.IsNullOrWhiteSpace(chatKey))
        {
            try
            {
                var existing = SessionBL.LoadBySessionKey(chatKey, skillKey, userId);
                JObject pending = null;
                var last = existing?.Messages != null && existing.Messages.Count > 0
                    ? existing.Messages[existing.Messages.Count - 1]
                    : null;
                if (last != null)
                    pending = last["pendingAskUser"] as JObject ?? last["PendingAskUser"] as JObject;
                var answerText = SessionBL.FormatAskUserAnswer(pending, response);
                SessionBL.PersistAskUserAnswer(skillKey, userId, dsId, chatKey, answerText);
            }
            catch { /* answer persist must not block the gate */ }
        }

        bool found = GenericAgentSessionStore.ConfirmAskUser(request.SessionId, response);

        if (!found)
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "ConfirmAskUser_NoPending",
                ValidationItemType.Warning, "No pending ask_user confirmation found. It may have already resolved or timed out."));

        result.Object = found;
        return result;
    }

    // GET /webapi/GenericAgent/GetFixedSessionKey?skillKey=...
    [HttpGet]
    public OperationCallResult<string> GetFixedSessionKey(string skillKey)
    {
        var result = new OperationCallResult<string>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null || string.IsNullOrWhiteSpace(skillKey))
            return result;
        result.Object = SessionBL.MakeFixedKey(skillKey.Trim(), Convert.ToInt32(ai.UserId));
        return result;
    }

    // GET /webapi/GenericAgent/LoadSession?skillKey=...  (fixed test key)
    [HttpGet]
    public OperationCallResult<List<JObject>> LoadSession(string skillKey)
    {
        var result   = new OperationCallResult<List<JObject>>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null) return result;
        result.Object = SessionBL.LoadFixedSession(skillKey, Convert.ToInt32(ai.UserId));
        return result;
    }

    // GET /webapi/GenericAgent/LoadChat?skillKey=...&sessionKey=...
    [HttpGet]
    public OperationCallResult<GenericAgentSessionDetailDto> LoadChat(string skillKey, string sessionKey)
    {
        var result = new OperationCallResult<GenericAgentSessionDetailDto>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null) return result;
        result.Object = SessionBL.LoadBySessionKey(sessionKey, skillKey, Convert.ToInt32(ai.UserId));
        return result;
    }

    // GET /webapi/GenericAgent/ListChats?skillKey=...
    [HttpGet]
    public OperationCallResult<List<GenericAgentSessionSummaryDto>> ListChats(string skillKey)
    {
        var result = new OperationCallResult<List<GenericAgentSessionSummaryDto>>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null) return result;
        result.Object = SessionBL.ListChats(skillKey, Convert.ToInt32(ai.UserId));
        return result;
    }

    // POST /webapi/GenericAgent/CreateChat?skillKey=...
    [HttpPost]
    public OperationCallResult<GenericAgentSessionSummaryDto> CreateChat([FromQuery] string skillKey)
    {
        var result = new OperationCallResult<GenericAgentSessionSummaryDto>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "CreateChat_NoUser",
                ValidationItemType.Error, "User identity required."));
            return result;
        }

        var userId = Convert.ToInt32(ai.UserId);
        var key = SessionBL.CreateChat(skillKey, userId, ai.DataSourceId);
        if (string.IsNullOrWhiteSpace(key))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "CreateChat_Failed",
                ValidationItemType.Error, "Failed to create chat."));
            return result;
        }

        var companyId = ai.CurrentWorkingCompanyId != null ? Convert.ToInt32(ai.CurrentWorkingCompanyId) : 0;
        if (companyId > 0)
        {
            try { GenericAgentFileBL.EnsureRoot(key, companyId, skillKey); }
            catch { /* ignore */ }
        }

        result.Object = new GenericAgentSessionSummaryDto
        {
            SessionKey = key,
            SkillKey   = skillKey,
            Title      = null,
            UpdatedAt  = DateTime.UtcNow,
            IsFixedTestSession = false
        };
        return result;
    }

    // POST /webapi/GenericAgent/ClearSession?skillKey=...&sessionKey=...
    // sessionKey omitted → wipe/delete the fixed test key SkillKey:UserId.
    [HttpPost]
    public IActionResult ClearSession([FromQuery] string skillKey, [FromQuery] string sessionKey = null)
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is AppClientIdentity ai && ai.UserId != null)
        {
            var userId = Convert.ToInt32(ai.UserId);
            if (!string.IsNullOrWhiteSpace(sessionKey))
                SessionBL.ClearBySessionKey(sessionKey, skillKey, userId);
            else
                SessionBL.DeleteSession(skillKey, userId);
        }
        return Ok();
    }

    // POST /webapi/GenericAgent/DeleteChat?skillKey=...&sessionKey=...
    [HttpPost]
    public OperationCallResult<bool> DeleteChat([FromQuery] string skillKey, [FromQuery] string sessionKey)
    {
        var result = new OperationCallResult<bool>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null) return result;
        result.Object = SessionBL.DeleteBySessionKey(sessionKey, skillKey, Convert.ToInt32(ai.UserId));
        return result;
    }

    // POST /webapi/GenericAgent/RenameChat
    [HttpPost]
    public OperationCallResult<bool> RenameChat([FromBody] GenericAgentRenameChatDto request)
    {
        var result = new OperationCallResult<bool>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "RenameChat_NoUser",
                ValidationItemType.Error, "User identity required."));
            return result;
        }
        if (request == null || string.IsNullOrWhiteSpace(request.SkillKey) || string.IsNullOrWhiteSpace(request.SessionKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "RenameChat_BadRequest",
                ValidationItemType.Error, "SkillKey and SessionKey are required."));
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "RenameChat_NoTitle",
                ValidationItemType.Error, "Title is required."));
            return result;
        }
        result.Object = SessionBL.Rename(
            request.SessionKey, request.SkillKey, Convert.ToInt32(ai.UserId), request.Title);
        return result;
    }

    // ── Agent Files APIs ──────────────────────────────────────────────────────

    private bool TryFilesIdentity(out AppClientIdentity ai, out int companyId, out OperationCallResult<object> err)
    {
        ai = default;
        companyId = 0;
        err = null;
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity a)
        {
            err = new OperationCallResult<object>();
            err.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_NoUser",
                ValidationItemType.Error, "User identity required."));
            return false;
        }
        ai = a;
        companyId = a.CurrentWorkingCompanyId != null ? Convert.ToInt32(a.CurrentWorkingCompanyId) : 0;
        if (companyId <= 0)
        {
            err = new OperationCallResult<object>();
            err.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_NoCompany",
                ValidationItemType.Error, "Company id required."));
            return false;
        }
        return true;
    }

    private static bool IsDefaultSourceScope(string scope) =>
        string.Equals(scope, "defaultSource", StringComparison.OrdinalIgnoreCase);

    private bool AuthorizeAgentFiles(string skillKey, string sessionKey, string scope, int userId, OperationCallResult<object> result)
    {
        if (IsDefaultSourceScope(scope))
        {
            if (string.IsNullOrWhiteSpace(skillKey))
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(GenericAgentController), "Files_NoSkill",
                    ValidationItemType.Error, "skillKey is required."));
                return false;
            }
            return true;
        }
        return AssertChatOwned(skillKey, sessionKey, userId, result);
    }

    private bool AssertChatOwned(string skillKey, string sessionKey, int userId, OperationCallResult<object> result)
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_NoSession",
                ValidationItemType.Error, "sessionKey is required."));
            return false;
        }
        if (SessionBL.IsFixedKey(sessionKey, skillKey, userId)) return true;
        if (SessionBL.LoadBySessionKey(sessionKey, skillKey, userId) != null) return true;
        result.ValidationResult.Items.Add(new ValidationItem(
            typeof(GenericAgentController), "Files_BadChat",
            ValidationItemType.Error, "Chat session not found."));
        return false;
    }

    [HttpGet]
    public OperationCallResult<List<GenericAgentFileDto>> ListAgentFiles(
        [FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] string path = null,
        [FromQuery] string scope = null)
    {
        var result = new OperationCallResult<List<GenericAgentFileDto>>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, sessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            result.Object = IsDefaultSourceScope(scope)
                ? GenericAgentFileBL.ListDefaultSource(skillKey, path, companyId)
                : GenericAgentFileBL.List(sessionKey, path, companyId, skillKey);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_List",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<GenericAgentFileContentDto> ReadAgentFile(
        [FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] string path,
        [FromQuery] string scope = null)
    {
        var result = new OperationCallResult<GenericAgentFileContentDto>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, sessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            result.Object = IsDefaultSourceScope(scope)
                ? GenericAgentFileBL.ReadDefaultSourceText(skillKey, path, companyId)
                : GenericAgentFileBL.ReadText(sessionKey, path, companyId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_Read",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<string> WriteAgentFile(
        [FromQuery] string skillKey,
        [FromQuery] string scope,
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<string>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, body?.SessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            result.Object = IsDefaultSourceScope(scope)
                ? GenericAgentFileBL.WriteDefaultSourceText(skillKey, body.RelativePath, body.Content, companyId)
                : GenericAgentFileBL.WriteText(body.SessionKey, body.RelativePath, body.Content, companyId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_Write",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> MkdirAgentFile(
        [FromQuery] string skillKey,
        [FromQuery] string scope,
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<bool>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, body?.SessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            if (IsDefaultSourceScope(scope))
                GenericAgentFileBL.MkdirDefaultSource(skillKey, body.RelativePath, companyId);
            else
                GenericAgentFileBL.Mkdir(body.SessionKey, body.RelativePath, companyId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_Mkdir",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> RenameAgentFile(
        [FromQuery] string skillKey,
        [FromQuery] string scope,
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<bool>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, body?.SessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            if (IsDefaultSourceScope(scope))
                GenericAgentFileBL.RenameDefaultSource(skillKey, body.RelativePath, body.NewPath, companyId);
            else
                GenericAgentFileBL.Rename(body.SessionKey, body.RelativePath, body.NewPath, companyId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_Rename",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> DeleteAgentFile(
        [FromQuery] string skillKey,
        [FromQuery] string scope,
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<bool>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, body?.SessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            if (IsDefaultSourceScope(scope))
                GenericAgentFileBL.DeleteDefaultSource(skillKey, body.RelativePath, companyId);
            else
                GenericAgentFileBL.Delete(body.SessionKey, body.RelativePath, companyId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_Delete",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpPost]
    public async Task<OperationCallResult<string>> UploadAgentFile(
        [FromQuery] string skillKey,
        [FromQuery] string sessionKey,
        [FromQuery] string path,
        [FromQuery] string scope,
        IFormFile file)
    {
        var result = new OperationCallResult<string>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, sessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        if (file == null || file.Length == 0)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_UploadEmpty",
                ValidationItemType.Error, "File is required."));
            return result;
        }
        try
        {
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var dest = string.IsNullOrWhiteSpace(path) ? file.FileName : path.Replace('\\', '/').Trim();
            var fileName = Path.GetFileName(file.FileName) ?? file.FileName;
            var destName = Path.GetFileName(dest);
            var rel = string.Equals(destName, fileName, StringComparison.OrdinalIgnoreCase)
                ? dest
                : (string.IsNullOrWhiteSpace(path) ? fileName : dest.TrimEnd('/') + "/" + fileName);
            result.Object = IsDefaultSourceScope(scope)
                ? GenericAgentFileBL.WriteDefaultSourceBytes(skillKey, rel, ms.ToArray(), companyId)
                : GenericAgentFileBL.WriteBytes(sessionKey, rel, ms.ToArray(), companyId);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_Upload",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpGet]
    public IActionResult DownloadAgentFile(
        [FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] string path,
        [FromQuery] string scope = null)
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null)
            return Unauthorized();
        var companyId = ai.CurrentWorkingCompanyId != null ? Convert.ToInt32(ai.CurrentWorkingCompanyId) : 0;
        if (companyId <= 0) return BadRequest("Company id required.");
        var boxed = new OperationCallResult<object>();
        if (!AuthorizeAgentFiles(skillKey, sessionKey, scope, Convert.ToInt32(ai.UserId), boxed))
            return NotFound();
        try
        {
            var bytes = IsDefaultSourceScope(scope)
                ? GenericAgentFileBL.ReadDefaultSourceBytes(skillKey, path, companyId)
                : GenericAgentFileBL.ReadBytes(sessionKey, path, companyId);
            var name = Path.GetFileName(path) ?? "file";
            return File(bytes, "application/octet-stream", name);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost]
    public OperationCallResult<int> RestoreDefaultSourceFiles(
        [FromQuery] string skillKey, [FromQuery] string sessionKey)
    {
        var result = new OperationCallResult<int>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AssertChatOwned(skillKey, sessionKey, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            GenericAgentFileBL.EnsureRoot(sessionKey, companyId, skillKey);
            result.Object = GenericAgentFileBL.CopyDefaultSourceToChat(skillKey, sessionKey, companyId, overwrite: true);
        }
        catch (Exception ex)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(GenericAgentController), "Files_RestoreDefault",
                ValidationItemType.Error, ex.Message));
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<ActiveAgentDto>> GetActiveAgents()
    {
        var result   = new OperationCallResult<List<ActiveAgentDto>>();
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai) return result;

        var agents = ai.DataSourceId > 0
            ? SkillSetBL.GetAll(ai.DataSourceId)
            : SkillSetBL.GetAll();

        result.Object = agents.Select(a => new ActiveAgentDto
        {
            SkillKey  = a.SkillKey,
            AgentName = string.IsNullOrWhiteSpace(a.DisplayName) ? a.SkillKey : a.DisplayName
        }).ToList();
        return result;
    }
}

public class ActiveAgentDto
{
    public string SkillKey  { get; set; }
    public string AgentName { get; set; }
}
