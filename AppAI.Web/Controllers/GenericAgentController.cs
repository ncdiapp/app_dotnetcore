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
/// Plan/schema gates:
///   POST /ConfirmPlan    → unblocks propose_plan
///   POST /ConfirmSchema  → unblocks propose_schema
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

        var callbacks = new GenericAgentCallbacks
        {
            OnStep = step => { GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "step", Step = step }); return Task.CompletedTask; },
            OnToken = token => { GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "token", Token = token }); return Task.CompletedTask; },
            OnDone = done =>
            {
                GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "done", Done = new AgentDoneEvent { FinalResponse = done } });
                if (agentUserId > 0 && agentDsId > 0)
                {
                    var updated = new List<JObject>(request.Messages ?? new List<JObject>());
                    updated.Add(JObject.FromObject(new { role = "user",      content = request.UserMessage }));
                    updated.Add(JObject.FromObject(new { role = "assistant", content = done ?? "" }));
                    SessionBL.SaveSession(
                        request.SkillKey, agentUserId, agentDsId, updated,
                        sessionKey: chatSessionKey);
                    if (agentCompanyId > 0 && !string.IsNullOrWhiteSpace(chatSessionKey))
                    {
                        try { GenericAgentFileBL.EnsureRoot(chatSessionKey, agentCompanyId); }
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
            try { GenericAgentFileBL.EnsureRoot(key, companyId); }
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

    // POST /webapi/GenericAgent/ClearSession?skillKey=...  (fixed test key)
    [HttpPost]
    public IActionResult ClearSession([FromQuery] string skillKey)
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is AppClientIdentity ai && ai.UserId != null)
            SessionBL.DeleteSession(skillKey, Convert.ToInt32(ai.UserId));
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
        [FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] string path = null)
    {
        var result = new OperationCallResult<List<GenericAgentFileDto>>();
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
            result.Object = GenericAgentFileBL.List(sessionKey, path, companyId);
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
        [FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] string path)
    {
        var result = new OperationCallResult<GenericAgentFileContentDto>();
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
            result.Object = GenericAgentFileBL.ReadText(sessionKey, path, companyId);
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
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<string>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AssertChatOwned(skillKey, body?.SessionKey, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
            result.Object = GenericAgentFileBL.WriteText(body.SessionKey, body.RelativePath, body.Content, companyId);
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
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<bool>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AssertChatOwned(skillKey, body?.SessionKey, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
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
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<bool>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AssertChatOwned(skillKey, body?.SessionKey, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
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
        [FromBody] GenericAgentFilePathDto body)
    {
        var result = new OperationCallResult<bool>();
        if (!TryFilesIdentity(out var ai, out var companyId, out var err))
        {
            result.ValidationResult = err.ValidationResult;
            return result;
        }
        var boxed = new OperationCallResult<object>();
        if (!AssertChatOwned(skillKey, body?.SessionKey, Convert.ToInt32(ai.UserId), boxed))
        {
            result.ValidationResult = boxed.ValidationResult;
            return result;
        }
        try
        {
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
        IFormFile file)
    {
        var result = new OperationCallResult<string>();
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
            var rel = string.IsNullOrWhiteSpace(path)
                ? file.FileName
                : path.TrimEnd('/') + "/" + file.FileName;
            result.Object = GenericAgentFileBL.WriteBytes(sessionKey, rel, ms.ToArray(), companyId);
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
        [FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] string path)
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity is not AppClientIdentity ai || ai.UserId == null)
            return Unauthorized();
        var companyId = ai.CurrentWorkingCompanyId != null ? Convert.ToInt32(ai.CurrentWorkingCompanyId) : 0;
        if (companyId <= 0) return BadRequest("Company id required.");
        var boxed = new OperationCallResult<object>();
        if (!AssertChatOwned(skillKey, sessionKey, Convert.ToInt32(ai.UserId), boxed))
            return NotFound();
        try
        {
            var bytes = GenericAgentFileBL.ReadBytes(sessionKey, path, companyId);
            var name = Path.GetFileName(path) ?? "file";
            return File(bytes, "application/octet-stream", name);
        }
        catch
        {
            return NotFound();
        }
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
            AgentName = string.IsNullOrWhiteSpace(a.DisplayName) ? a.SkillKey : a.DisplayName,
            AgentUi   = a.AgentUi
        }).ToList();
        return result;
    }
}

public class ActiveAgentDto
{
    public string SkillKey  { get; set; }
    public string AgentName { get; set; }
    public int    AgentUi   { get; set; }
}
