using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.BL.AppReportAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppWebPluin.Models;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

/// <summary>
/// REST endpoint for the AppReport AI Agent.
///
/// Streaming uses polling (no SignalR):
///   1. POST /RunAgent  → starts agent, returns { SessionId }
///   2. GET  /PollEvents?sessionId=... → returns queued events (call every 500 ms)
///   3. React stops polling when it receives an event of type "done" or "error"
/// </summary>
[Route("webapi/[controller]/[action]")]
public class AppReportAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<AppReportAgentStartResultDto> RunAgent(
        [FromBody] AppReportAgentRequestDto request)
    {
        AppClientIdentity? agentIdentity = null;
        var currentIdentity = ServerContext.Instance.CurrnetClientIdentity;
        if (currentIdentity is AppClientIdentity)
            agentIdentity = (AppClientIdentity)currentIdentity;

        HttpBasicAuthenticator.RegisterSysTemAgentWebUserIdentity();

        if (!agentIdentity.HasValue)
        {
            currentIdentity = ServerContext.Instance.CurrnetClientIdentity;
            if (currentIdentity is AppClientIdentity)
                agentIdentity = (AppClientIdentity)currentIdentity;
        }

        var result = new OperationCallResult<AppReportAgentStartResultDto>();

        if (string.IsNullOrWhiteSpace(request?.UserMessage))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AppReportAgentController),
                "Agent_NoMessage", ValidationItemType.Error, "A user message is required."));
            return result;
        }

        // Create session and wire callbacks to the event queue
        var sessionId = AppReportAgentSessionStore.CreateSession();

        var callbacks = new ReportAgentCallbacks
        {
            OnStep = step =>
            {
                AppReportAgentSessionStore.Enqueue(sessionId,
                    new AppReportAgentEventDto { EventType = "step", Step = step });
                return Task.FromResult(0);
            },
            OnToken = token =>
            {
                AppReportAgentSessionStore.Enqueue(sessionId,
                    new AppReportAgentEventDto { EventType = "token", Token = token });
                return Task.FromResult(0);
            },
            OnDone = done =>
            {
                AppReportAgentSessionStore.Enqueue(sessionId,
                    new AppReportAgentEventDto { EventType = "done", Done = done });
                return Task.FromResult(0);
            },
            OnError = msg =>
            {
                AppReportAgentSessionStore.Enqueue(sessionId,
                    new AppReportAgentEventDto { EventType = "error", Error = msg });
                return Task.FromResult(0);
            }
        };

        // Fire-and-forget — client polls for results
        Task.Run(async () =>
        {
            await AppReportAgentBL.RunAgentAsync(request, callbacks, agentIdentity).ConfigureAwait(false);
        });

        result.Object = new AppReportAgentStartResultDto
        {
            IsStarted = true,
            SessionId = sessionId
        };
        return result;
    }

    [HttpGet]
    public AppReportAgentPollResponseDto PollEvents(string sessionId)
    {
        return AppReportAgentSessionStore.DequeueAll(sessionId);
    }
}
