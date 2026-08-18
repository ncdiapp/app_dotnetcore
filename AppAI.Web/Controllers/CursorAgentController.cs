using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using App.BL.CursorAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class CursorAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<CursorAgentStartResultDto> StartSession([FromBody] CursorAgentStartRequestDto request)
    {
        var result = new OperationCallResult<CursorAgentStartResultDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            result.Object = CursorAgentBL.StartSession(request, CurrentIdentity());
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_Start", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> FollowUp([FromBody] CursorAgentFollowUpRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            CursorAgentBL.FollowUp(request, CurrentIdentity());
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_FollowUp", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ResumeSession([FromBody] CursorAgentResumeRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            CursorAgentBL.Resume(request, CurrentIdentity());
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_Resume", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public async Task<OperationCallResult<bool>> Cancel([FromBody] CursorAgentCancelRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            await CursorAgentBL.CancelAsync(request?.SessionId).ConfigureAwait(false);
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_Cancel", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public CursorAgentPollResponseDto PollEvents(string sessionId)
    {
        if (CursorAgentConfig.AdminOnly && !AppSecurityUserBL.IsAdminUser())
            return new CursorAgentPollResponseDto { SessionExists = false };
        return CursorAgentSessionStore.DequeueAll(sessionId);
    }

    [HttpGet]
    public async Task StreamEvents(string sessionId, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Connection"] = "keep-alive";
        await Response.Body.FlushAsync(cancellationToken);

        var done = false;
        while (!done && !cancellationToken.IsCancellationRequested)
        {
            await CursorAgentSessionStore.WaitForEventAsync(sessionId, TimeSpan.FromSeconds(30), cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;
            var poll = CursorAgentSessionStore.DequeueAll(sessionId);
            if (!poll.SessionExists)
            {
                var err = Encoding.UTF8.GetBytes("event: error\ndata: {\"Error\":\"Session not found\"}\n\n");
                await Response.Body.WriteAsync(err, 0, err.Length, cancellationToken);
                break;
            }
            foreach (var evt in poll.Events)
            {
                var data = JsonConvert.SerializeObject(evt);
                var bytes = Encoding.UTF8.GetBytes("event: " + evt.EventType + "\ndata: " + data + "\n\n");
                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                if (evt.EventType == "done" || evt.EventType == "error")
                    done = true;
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
    public OperationCallResult<bool> ConfirmGate([FromBody] CursorAgentConfirmGateRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            Fail(result, "CursorAgent_ConfirmGate", "SessionId is required.");
            return result;
        }
        result.Object = CursorAgentSessionStore.ConfirmGate(request.SessionId, request.GateId, request.Confirmed, request.Feedback);
        if (!result.Object)
            Fail(result, "CursorAgent_ConfirmGate", "No pending gate for this session.");
        return result;
    }

    [HttpGet]
    public OperationCallResult<CursorAgentSessionFullDto> GetSession(string sessionId)
    {
        var result = new OperationCallResult<CursorAgentSessionFullDto>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorAgentSessionBL.Get(sessionId);
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<CursorAgentSessionSummaryDto>> RecentSessions(int limit = 30)
    {
        var result = new OperationCallResult<System.Collections.Generic.List<CursorAgentSessionSummaryDto>>();
        if (!EnsureAdmin(result)) return result;
        int? userId = null;
        var identity = CurrentIdentity();
        if (identity.HasValue && identity.Value.UserId != null)
            userId = Convert.ToInt32(identity.Value.UserId);
        result.Object = CursorAgentSessionBL.ListRecent(limit, userId);
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<CursorAgentWorkspaceFileDto>> ListWorkspaceFiles(string sessionId)
    {
        var result = new OperationCallResult<System.Collections.Generic.List<CursorAgentWorkspaceFileDto>>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(sessionId);
            result.Object = CursorWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId);
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_ListFiles", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<CursorAgentFileContentDto> ReadWorkspaceFile(string sessionId, string relativePath)
    {
        var result = new OperationCallResult<CursorAgentFileContentDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(sessionId);
            result.Object = CursorWorkspaceBL.ReadFile(live.WorkspaceRelativePath, relativePath, live.CompanyId);
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_ReadFile", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public IActionResult DownloadWorkspaceFile(string sessionId, string relativePath)
    {
        if (CursorAgentConfig.AdminOnly && !AppSecurityUserBL.IsAdminUser())
            return StatusCode(403);
        try
        {
            var live = RequireLive(sessionId);
            var bytes = CursorWorkspaceBL.ReadBytes(live.WorkspaceRelativePath, relativePath, live.CompanyId);
            var ext = System.IO.Path.GetExtension(relativePath ?? "");
            var contentType = string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
                : string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg"
                : string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase) ? "image/gif"
                : string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
                : "application/octet-stream";
            return File(bytes, contentType, System.IO.Path.GetFileName(relativePath));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public OperationCallResult<bool> DeleteWorkspaceFile([FromBody] CursorAgentFileRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(request?.SessionId);
            CursorWorkspaceBL.DeleteFile(live.WorkspaceRelativePath, request.RelativePath, live.CompanyId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorAgent_DeleteFile", ex.Message);
        }
        return result;
    }

    private static CursorAgentSessionStore.SessionData RequireLive(string sessionId)
    {
        CursorAgentSessionStore.SessionData live;
        if (CursorAgentSessionStore.TryGet(sessionId, out live) && live != null)
            return live;
        live = CursorAgentSessionBL.HydrateLive(sessionId);
        if (live == null) throw new InvalidOperationException("Session not found.");
        return live;
    }

    private static AppClientIdentity? CurrentIdentity()
    {
        var current = ServerContext.Instance.CurrnetClientIdentity;
        if (current is AppClientIdentity)
            return (AppClientIdentity)current;
        return null;
    }

    private static bool EnsureAdmin<T>(OperationCallResult<T> result)
    {
        if (!CursorAgentConfig.AdminOnly || AppSecurityUserBL.IsAdminUser())
            return true;
        Fail(result, "CursorAgent_Forbidden", "Administrator access is required.");
        return false;
    }

    private static void Fail<T>(OperationCallResult<T> result, string code, string message)
    {
        result.ValidationResult.Items.Add(new ValidationItem(
            typeof(CursorAgentController), code, ValidationItemType.Error, message));
    }
}
