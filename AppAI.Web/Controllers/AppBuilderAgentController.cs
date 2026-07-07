using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.BL;
using App.BL.AppBuilderAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

/// <summary>
/// REST endpoint for the AppBuilder AI Agent.
///
/// Streaming uses polling (no SignalR):
///   1. POST /RunAgent  → starts agent, returns { SessionId }
///   2. GET  /PollEvents?sessionId=... → returns queued events (call every 500 ms)
///   3. React stops polling when it receives an event of type "done" or "error"
/// </summary>
[Route("webapi/[controller]/[action]")]
public class AppBuilderAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<AppBuilderAgentStartResultDto> RunAgent(
        [FromBody] AppBuilderAgentRequestDto request)
    {
        AppClientIdentity? agentIdentity = null;
        var currentIdentity = ServerContext.Instance.CurrnetClientIdentity;
        if (currentIdentity is AppClientIdentity)
            agentIdentity = (AppClientIdentity)currentIdentity;

        var result = new OperationCallResult<AppBuilderAgentStartResultDto>();

        if (string.IsNullOrWhiteSpace(request?.UserMessage))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AppBuilderAgentController),
                "Agent_NoMessage", ValidationItemType.Error, "A user message is required."));
            return result;
        }

        // Create session and wire callbacks to the event queue
        var sessionId = AppBuilderAgentSessionStore.CreateSession();

        var callbacks = new AgentCallbacks
        {
            OnStep = step =>
            {
                AppBuilderAgentSessionStore.Enqueue(sessionId,
                    new AgentEventDto { EventType = "step", Step = step });
                return Task.FromResult(0);
            },
            OnToken = token =>
            {
                AppBuilderAgentSessionStore.Enqueue(sessionId,
                    new AgentEventDto { EventType = "token", Token = token });
                return Task.FromResult(0);
            },
            OnDone = done =>
            {
                AppBuilderAgentSessionStore.Enqueue(sessionId,
                    new AgentEventDto { EventType = "done", Done = done });
                return Task.FromResult(0);
            },
            OnError = msg =>
            {
                AppBuilderAgentSessionStore.Enqueue(sessionId,
                    new AgentEventDto { EventType = "error", Error = msg });
                return Task.FromResult(0);
            },

            // ── Plan-confirm gate ─────────────────────────────────────────
            OnPlanReady = async planEvent =>
            {
                AppBuilderAgentSessionStore.Enqueue(sessionId,
                    new AgentEventDto { EventType = "plan", Plan = planEvent });

                var tcs = AppBuilderAgentSessionStore.RegisterPlanConfirmation(sessionId);

                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                {
                    cts.Token.Register(() => tcs.TrySetResult(false));
                    return await tcs.Task.ConfigureAwait(false);
                }
            },

            // ── Schema-review gate ────────────────────────────────────────
            OnSchemaReady = async schemaEvent =>
            {
                AppBuilderAgentSessionStore.Enqueue(sessionId,
                    new AgentEventDto { EventType = "schema", Schema = schemaEvent });

                var tcs = AppBuilderAgentSessionStore.RegisterSchemaConfirmation(sessionId);

                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                {
                    cts.Token.Register(() => tcs.TrySetResult(
                        new AgentSchemaResponse { Confirmed = false, Feedback = "Schema review timed out." }));
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
        };

        // Fire-and-forget — client polls for results
        Task.Run(async () =>
        {
            await AppBuilderAgentBL.RunAgentAsync(request, callbacks, agentIdentity).ConfigureAwait(false);
        });

        result.Object = new AppBuilderAgentStartResultDto
        {
            IsStarted = true,
            SessionId = sessionId
        };
        return result;
    }

    [HttpGet]
    public AgentPollResponseDto PollEvents(string sessionId)
    {
        return AppBuilderAgentSessionStore.DequeueAll(sessionId);
    }

    [HttpPost]
    public OperationCallResult<bool> ConfirmPlan([FromBody] AgentConfirmPlanRequestDto request)
    {
        var result = new OperationCallResult<bool>();

        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AppBuilderAgentController),
                "ConfirmPlan_NoSession", ValidationItemType.Error, "SessionId is required."));
            return result;
        }

        bool found = AppBuilderAgentSessionStore.ConfirmPlan(request.SessionId, request.Confirmed);

        if (!found)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AppBuilderAgentController),
                "ConfirmPlan_NoPending", ValidationItemType.Warning,
                "No pending plan confirmation found for this session. It may have already been resolved or timed out."));
        }

        result.Object = found;
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> ConfirmSchema([FromBody] AgentConfirmSchemaRequestDto request)
    {
        var result = new OperationCallResult<bool>();

        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AppBuilderAgentController),
                "ConfirmSchema_NoSession", ValidationItemType.Error, "SessionId is required."));
            return result;
        }

        var response = new AgentSchemaResponse
        {
            Confirmed  = request.Confirmed,
            SchemaJson = request.SchemaJson,
            Feedback   = request.Feedback
        };

        bool found = AppBuilderAgentSessionStore.ConfirmSchema(request.SessionId, response);

        if (!found)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AppBuilderAgentController),
                "ConfirmSchema_NoPending", ValidationItemType.Warning,
                "No pending schema confirmation found for this session. It may have already been resolved or timed out."));
        }

        result.Object = found;
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<App.BL.AppBuilderAgent.AgentSessionRecord>> RecentSessions(int limit = 20)
    {
        var result = new OperationCallResult<List<App.BL.AppBuilderAgent.AgentSessionRecord>>();
        result.Object = AppBuilderAgentSessionBL.GetRecentSessions(limit, AppSecurityUserBL.CurrentUserId);
        return result;
    }

    [HttpGet]
    public OperationCallResult<App.BL.AppBuilderAgent.AgentSessionRecord> GetSession(string sessionGuid)
    {
        var result = new OperationCallResult<App.BL.AppBuilderAgent.AgentSessionRecord>();
        result.Object = AppBuilderAgentSessionBL.GetSession(sessionGuid);
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteSession(string sessionGuid)
    {
        var result = new OperationCallResult<bool>();
        result.Object = AppBuilderAgentSessionBL.DeleteSession(sessionGuid);
        return result;
    }
}
