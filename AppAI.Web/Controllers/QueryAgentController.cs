using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.BL;
using App.BL.QueryAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

/// <summary>
/// REST endpoint for the Query AI Agent.
///
/// Streaming uses polling (no SignalR):
///   1. POST /RunAgent  → starts agent, returns { SessionId }
///   2. GET  /PollEvents?sessionId=... → returns queued events (call every 500 ms)
///   3. React stops polling when it receives an event of type "done" or "error"
/// </summary>
[Route("webapi/[controller]/[action]")]
public class QueryAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<AppBuilderAgentStartResultDto> RunAgent(
        [FromBody] QueryAgentRequestDto request)
    {
        AppClientIdentity? agentIdentity = null;
        var currentIdentity = ServerContext.Instance.CurrnetClientIdentity;
        if (currentIdentity is AppClientIdentity)
            agentIdentity = (AppClientIdentity)currentIdentity;

        var result = new OperationCallResult<AppBuilderAgentStartResultDto>();

        if (string.IsNullOrWhiteSpace(request?.UserMessage))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(QueryAgentController),
                "QueryAgent_NoMessage", ValidationItemType.Error, "A user message is required."));
            return result;
        }

        var sessionId = QueryAgentSessionStore.CreateSession();

        var callbacks = new QueryAgentCallbacks
        {
            OnStep = step =>
            {
                QueryAgentSessionStore.Enqueue(sessionId,
                    new QueryAgentEventDto { EventType = "step", Step = step });
                return Task.FromResult(0);
            },
            OnToken = token =>
            {
                QueryAgentSessionStore.Enqueue(sessionId,
                    new QueryAgentEventDto { EventType = "token", Token = token });
                return Task.FromResult(0);
            },
            OnDone = done =>
            {
                QueryAgentSessionStore.Enqueue(sessionId,
                    new QueryAgentEventDto { EventType = "done", Done = done });
                return Task.FromResult(0);
            },
            OnError = msg =>
            {
                QueryAgentSessionStore.Enqueue(sessionId,
                    new QueryAgentEventDto { EventType = "error", Error = msg });
                return Task.FromResult(0);
            }
        };

        // Fire-and-forget — client polls for results
        Task.Run(async () =>
        {
            await QueryAgentBL.RunAgentAsync(request, callbacks, agentIdentity)
                .ConfigureAwait(false);
        });

        result.Object = new AppBuilderAgentStartResultDto
        {
            IsStarted = true,
            SessionId = sessionId
        };
        return result;
    }

    [HttpGet]
    public QueryAgentPollResponseDto PollEvents(string sessionId)
    {
        return QueryAgentSessionStore.DequeueAll(sessionId);
    }
}
