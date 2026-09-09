using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using App.BL.CursorCloudAgent;
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
public class CursorCloudAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<CursorCloudAgentStartResultDto> StartSession([FromBody] CursorCloudAgentStartRequestDto request)
    {
        var result = new OperationCallResult<CursorCloudAgentStartResultDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            result.Object = CursorCloudAgentBL.StartSession(request, CurrentIdentity());
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_Start", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> FollowUp([FromBody] CursorCloudAgentFollowUpRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            CursorCloudAgentBL.FollowUp(request, CurrentIdentity());
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_FollowUp", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ResumeSession([FromBody] CursorCloudAgentResumeRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            CursorCloudAgentBL.Resume(request, CurrentIdentity());
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_Resume", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public async Task<OperationCallResult<bool>> Cancel([FromBody] CursorCloudAgentCancelRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            await CursorCloudAgentBL.CancelAsync(request?.SessionId).ConfigureAwait(false);
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_Cancel", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public CursorCloudAgentPollResponseDto PollEvents(string sessionId)
    {
        if (CursorCloudAgentConfig.AdminOnly && !AppSecurityUserBL.IsAdminUser())
            return new CursorCloudAgentPollResponseDto { SessionExists = false };
        return CursorCloudAgentSessionStore.DequeueAll(sessionId);
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
            await CursorCloudAgentSessionStore.WaitForEventAsync(sessionId, TimeSpan.FromSeconds(30), cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;
            var poll = CursorCloudAgentSessionStore.DequeueAll(sessionId);
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
    public OperationCallResult<bool> ConfirmGate([FromBody] CursorCloudAgentConfirmGateRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            Fail(result, "CursorCloudAgent_ConfirmGate", "SessionId is required.");
            return result;
        }
        result.Object = CursorCloudAgentSessionStore.ConfirmGate(request.SessionId, request.GateId, request.Confirmed, request.Feedback);
        if (!result.Object)
            Fail(result, "CursorCloudAgent_ConfirmGate", "No pending gate for this session.");
        return result;
    }

    [HttpGet]
    public OperationCallResult<CursorCloudAgentSkillMenuDto> ListSkillMenu()
    {
        var result = new OperationCallResult<CursorCloudAgentSkillMenuDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            result.Object = CursorCloudAgentBL.ListSkillMenu();
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_ListSkillMenu", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<CursorCloudAgentSessionFullDto> GetSession(string sessionId)
    {
        var result = new OperationCallResult<CursorCloudAgentSessionFullDto>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorCloudAgentSessionBL.Get(sessionId);
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<CursorCloudAgentSessionSummaryDto>> RecentSessions(int limit = 30)
    {
        var result = new OperationCallResult<System.Collections.Generic.List<CursorCloudAgentSessionSummaryDto>>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorCloudAgentSessionBL.ListRecent(limit, CurrentUserId());
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<CursorCloudAgentSessionSummaryDto>> ListAllSessions()
    {
        var result = new OperationCallResult<System.Collections.Generic.List<CursorCloudAgentSessionSummaryDto>>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorCloudAgentSessionBL.ListAll(CurrentUserId());
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> RenameSession([FromBody] CursorCloudAgentRenameSessionRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            Fail(result, "CursorCloudAgent_Rename", "SessionId is required.");
            return result;
        }
        result.Object = CursorCloudAgentSessionBL.Rename(request.SessionId, request.Title);
        return result;
    }

    [HttpPost]
    public OperationCallResult<int> ArchiveSessions([FromBody] CursorCloudAgentArchiveSessionsRequestDto request)
    {
        var result = new OperationCallResult<int>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorCloudAgentSessionBL.SetArchived(request?.SessionIds, request != null && request.Archived);
        return result;
    }

    [HttpPost]
    public OperationCallResult<int> DeleteSessions([FromBody] CursorCloudAgentDeleteSessionsRequestDto request)
    {
        var result = new OperationCallResult<int>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorCloudAgentSessionBL.DeleteMany(request?.SessionIds);
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ReorderSessions([FromBody] CursorCloudAgentReorderSessionsRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        result.Object = CursorCloudAgentSessionBL.Reorder(request?.SessionIds);
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<CursorCloudAgentWorkspaceFileDto>> ListWorkspaceFiles(string sessionId)
    {
        var result = new OperationCallResult<System.Collections.Generic.List<CursorCloudAgentWorkspaceFileDto>>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(sessionId);
            result.Object = CursorCloudAgentWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId);
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_ListFiles", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<CursorCloudAgentFileContentDto> ReadWorkspaceFile(string sessionId, string relativePath)
    {
        var result = new OperationCallResult<CursorCloudAgentFileContentDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(sessionId);
            result.Object = CursorCloudAgentWorkspaceBL.ReadFile(live.WorkspaceRelativePath, relativePath, live.CompanyId);
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_ReadFile", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public IActionResult DownloadWorkspaceFile(string sessionId, string relativePath)
    {
        if (CursorCloudAgentConfig.AdminOnly && !AppSecurityUserBL.IsAdminUser())
            return StatusCode(403);
        try
        {
            var live = RequireLive(sessionId);
            var bytes = CursorCloudAgentWorkspaceBL.ReadBytes(live.WorkspaceRelativePath, relativePath, live.CompanyId);
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
    public OperationCallResult<bool> DeleteWorkspaceFile([FromBody] CursorCloudAgentFileRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(request?.SessionId);
            CursorCloudAgentWorkspaceBL.DeleteFile(live.WorkspaceRelativePath, request.RelativePath, live.CompanyId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "CursorCloudAgent_DeleteFile", ex.Message);
        }
        return result;
    }

    private static CursorCloudAgentSessionStore.SessionData RequireLive(string sessionId)
    {
        return CursorCloudAgentSessionBL.RequireHydrated(sessionId);
    }

    private static AppClientIdentity? CurrentIdentity()
    {
        var current = ServerContext.Instance.CurrnetClientIdentity;
        if (current is AppClientIdentity)
            return (AppClientIdentity)current;
        return null;
    }

    private static int? CurrentUserId()
    {
        var identity = CurrentIdentity();
        if (identity.HasValue && identity.Value.UserId != null)
            return Convert.ToInt32(identity.Value.UserId);
        return null;
    }

    private static bool EnsureAdmin<T>(OperationCallResult<T> result)
    {
        if (!CursorCloudAgentConfig.AdminOnly || AppSecurityUserBL.IsAdminUser())
            return true;
        Fail(result, "CursorCloudAgent_Forbidden", "Administrator access is required.");
        return false;
    }

    private static void Fail<T>(OperationCallResult<T> result, string code, string message)
    {
        result.ValidationResult.Items.Add(new ValidationItem(
            typeof(CursorCloudAgentController), code, ValidationItemType.Error, message));
    }
}
