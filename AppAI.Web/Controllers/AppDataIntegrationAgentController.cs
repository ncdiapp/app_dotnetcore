using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using App.BL.AppDataIntegrationAgent;
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
public class AppDataIntegrationAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<AppDataIntegrationAgentStartResultDto> StartSession([FromBody] AppDataIntegrationAgentStartRequestDto request)
    {
        var result = new OperationCallResult<AppDataIntegrationAgentStartResultDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            result.Object = AppDataIntegrationAgentBL.StartSession(request, CurrentIdentity());
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_Start", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> FollowUp([FromBody] AppDataIntegrationAgentFollowUpRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            AppDataIntegrationAgentBL.FollowUp(request, CurrentIdentity());
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_FollowUp", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ResumeSession([FromBody] AppDataIntegrationAgentResumeRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            AppDataIntegrationAgentBL.Resume(request, CurrentIdentity());
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_Resume", ex.Message);
        }
        return result;
    }

    [HttpPost]
    public async Task<OperationCallResult<bool>> Cancel([FromBody] AppDataIntegrationAgentCancelRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            await AppDataIntegrationAgentBL.CancelAsync(request?.SessionId).ConfigureAwait(false);
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_Cancel", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public AppDataIntegrationAgentPollResponseDto PollEvents(string sessionId)
    {
        if (AppDataIntegrationAgentConfig.AdminOnly && !AppSecurityUserBL.IsAdminUser())
            return new AppDataIntegrationAgentPollResponseDto { SessionExists = false };
        return AppDataIntegrationAgentSessionStore.DequeueAll(sessionId);
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
            await AppDataIntegrationAgentSessionStore.WaitForEventAsync(sessionId, TimeSpan.FromSeconds(30), cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;
            var poll = AppDataIntegrationAgentSessionStore.DequeueAll(sessionId);
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
    public OperationCallResult<bool> ConfirmGate([FromBody] AppDataIntegrationAgentConfirmGateRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            Fail(result, "AppDataIntegrationAgent_ConfirmGate", "SessionId is required.");
            return result;
        }
        result.Object = AppDataIntegrationAgentSessionStore.ConfirmGate(request.SessionId, request.GateId, request.Confirmed, request.Feedback);
        if (!result.Object)
            Fail(result, "AppDataIntegrationAgent_ConfirmGate", "No pending gate for this session.");
        return result;
    }

    [HttpGet]
    public OperationCallResult<AppDataIntegrationAgentSkillMenuDto> ListSkillMenu()
    {
        var result = new OperationCallResult<AppDataIntegrationAgentSkillMenuDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            result.Object = AppDataIntegrationAgentBL.ListSkillMenu();
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_ListSkillMenu", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<AppDataIntegrationAgentSessionFullDto> GetSession(string sessionId)
    {
        var result = new OperationCallResult<AppDataIntegrationAgentSessionFullDto>();
        if (!EnsureAdmin(result)) return result;
        result.Object = AppDataIntegrationAgentSessionBL.Get(sessionId);
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<AppDataIntegrationAgentSessionSummaryDto>> RecentSessions(int limit = 30)
    {
        var result = new OperationCallResult<System.Collections.Generic.List<AppDataIntegrationAgentSessionSummaryDto>>();
        if (!EnsureAdmin(result)) return result;
        result.Object = AppDataIntegrationAgentSessionBL.ListRecent(limit, CurrentUserId());
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<AppDataIntegrationAgentSessionSummaryDto>> ListAllSessions()
    {
        var result = new OperationCallResult<System.Collections.Generic.List<AppDataIntegrationAgentSessionSummaryDto>>();
        if (!EnsureAdmin(result)) return result;
        result.Object = AppDataIntegrationAgentSessionBL.ListAll(CurrentUserId());
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> RenameSession([FromBody] AppDataIntegrationAgentRenameSessionRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        if (request == null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            Fail(result, "AppDataIntegrationAgent_Rename", "SessionId is required.");
            return result;
        }
        result.Object = AppDataIntegrationAgentSessionBL.Rename(request.SessionId, request.Title);
        return result;
    }

    [HttpPost]
    public OperationCallResult<int> ArchiveSessions([FromBody] AppDataIntegrationAgentArchiveSessionsRequestDto request)
    {
        var result = new OperationCallResult<int>();
        if (!EnsureAdmin(result)) return result;
        result.Object = AppDataIntegrationAgentSessionBL.SetArchived(request?.SessionIds, request != null && request.Archived);
        return result;
    }

    [HttpPost]
    public OperationCallResult<int> DeleteSessions([FromBody] AppDataIntegrationAgentDeleteSessionsRequestDto request)
    {
        var result = new OperationCallResult<int>();
        if (!EnsureAdmin(result)) return result;
        result.Object = AppDataIntegrationAgentSessionBL.DeleteMany(request?.SessionIds);
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ReorderSessions([FromBody] AppDataIntegrationAgentReorderSessionsRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        result.Object = AppDataIntegrationAgentSessionBL.Reorder(request?.SessionIds);
        return result;
    }

    [HttpGet]
    public OperationCallResult<System.Collections.Generic.List<AppDataIntegrationAgentWorkspaceFileDto>> ListWorkspaceFiles(string sessionId)
    {
        var result = new OperationCallResult<System.Collections.Generic.List<AppDataIntegrationAgentWorkspaceFileDto>>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(sessionId);
            result.Object = AppDataIntegrationWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId);
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_ListFiles", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public OperationCallResult<AppDataIntegrationAgentFileContentDto> ReadWorkspaceFile(string sessionId, string relativePath)
    {
        var result = new OperationCallResult<AppDataIntegrationAgentFileContentDto>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(sessionId);
            result.Object = AppDataIntegrationWorkspaceBL.ReadFile(live.WorkspaceRelativePath, relativePath, live.CompanyId);
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_ReadFile", ex.Message);
        }
        return result;
    }

    [HttpGet]
    public IActionResult DownloadWorkspaceFile(string sessionId, string relativePath)
    {
        if (AppDataIntegrationAgentConfig.AdminOnly && !AppSecurityUserBL.IsAdminUser())
            return StatusCode(403);
        try
        {
            var live = RequireLive(sessionId);
            var bytes = AppDataIntegrationWorkspaceBL.ReadBytes(live.WorkspaceRelativePath, relativePath, live.CompanyId);
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
    public OperationCallResult<bool> DeleteWorkspaceFile([FromBody] AppDataIntegrationAgentFileRequestDto request)
    {
        var result = new OperationCallResult<bool>();
        if (!EnsureAdmin(result)) return result;
        try
        {
            var live = RequireLive(request?.SessionId);
            AppDataIntegrationWorkspaceBL.DeleteFile(live.WorkspaceRelativePath, request.RelativePath, live.CompanyId);
            result.Object = true;
        }
        catch (Exception ex)
        {
            Fail(result, "AppDataIntegrationAgent_DeleteFile", ex.Message);
        }
        return result;
    }

    private static AppDataIntegrationAgentSessionStore.SessionData RequireLive(string sessionId)
    {
        return AppDataIntegrationAgentSessionBL.RequireHydrated(sessionId);
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
        if (!AppDataIntegrationAgentConfig.AdminOnly || AppSecurityUserBL.IsAdminUser())
            return true;
        Fail(result, "AppDataIntegrationAgent_Forbidden", "Administrator access is required.");
        return false;
    }

    private static void Fail<T>(OperationCallResult<T> result, string code, string message)
    {
        result.ValidationResult.Items.Add(new ValidationItem(
            typeof(AppDataIntegrationAgentController), code, ValidationItemType.Error, message));
    }
}
