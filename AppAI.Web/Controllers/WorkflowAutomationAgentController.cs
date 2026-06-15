using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.BL;
using App.BL.WorkflowAutomationAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppWebPluin.Models;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

/// <summary>
/// REST endpoint for the Workflow Automation AI Agent.
///
/// Streaming uses polling (no SignalR):
///   1. POST /RunAgent  → starts agent, returns { SessionId }
///   2. GET  /PollEvents?sessionId=... → returns queued events (call every 500 ms)
///   3. React stops polling when it receives an event of type "done" or "error"
///
/// Plan confirmation:
///   - When agent calls propose_workflow_changes, a "plan" event is queued
///   - UI shows WfPlanApprovalCard; user clicks Approve or Reject
///   - POST /ConfirmPlan resolves the TCS, agent continues or revises
/// </summary>
[Route("webapi/[controller]/[action]")]
public class WorkflowAutomationAgentController : SecureBaseController
{
    [HttpPost]
    public OperationCallResult<AppBuilderAgentStartResultDto> RunAgent(
        [FromBody] WfAgentRequestDto request)
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

        var result = new OperationCallResult<AppBuilderAgentStartResultDto>();

        if (string.IsNullOrWhiteSpace(request?.UserMessage))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(WorkflowAutomationAgentController),
                "WfAgent_NoMessage", ValidationItemType.Error, "A user message is required."));
            return result;
        }

        if (request.TransactionId <= 0)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(WorkflowAutomationAgentController),
                "WfAgent_NoTransaction", ValidationItemType.Error, "A valid TransactionId is required."));
            return result;
        }

        var sessionId = WorkflowAutomationAgentSessionStore.CreateSession();

        var callbacks = new WfAgentCallbacks
        {
            OnStep = step =>
            {
                WorkflowAutomationAgentSessionStore.Enqueue(sessionId,
                    new WfAgentEventDto { EventType = "step", Step = step });
                return Task.FromResult(0);
            },
            OnToken = token =>
            {
                WorkflowAutomationAgentSessionStore.Enqueue(sessionId,
                    new WfAgentEventDto { EventType = "token", Token = token });
                return Task.FromResult(0);
            },
            OnDone = done =>
            {
                WorkflowAutomationAgentSessionStore.Enqueue(sessionId,
                    new WfAgentEventDto { EventType = "done", Done = done });
                return Task.FromResult(0);
            },
            OnError = msg =>
            {
                WorkflowAutomationAgentSessionStore.Enqueue(sessionId,
                    new WfAgentEventDto { EventType = "error", Error = msg });
                return Task.FromResult(0);
            },

            // ── Plan-confirm gate ────────────────────────────────────────
            OnPlanReady = async planEvent =>
            {
                WorkflowAutomationAgentSessionStore.Enqueue(sessionId,
                    new WfAgentEventDto { EventType = "plan", Plan = planEvent });

                var tcs = WorkflowAutomationAgentSessionStore.RegisterPlanConfirmation(sessionId);

                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10)))
                {
                    cts.Token.Register(() => tcs.TrySetResult(false));
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
        };

        // Fire-and-forget — client polls for results
        Task.Run(async () =>
        {
            await WorkflowAutomationAgentBL.RunAgentAsync(request, callbacks, agentIdentity)
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
    public WfAgentPollResponseDto PollEvents(string sessionId)
    {
        return WorkflowAutomationAgentSessionStore.DequeueAll(sessionId);
    }

    /// <summary>
    /// Called by the React UI after the user reviews the changes proposed by propose_workflow_changes.
    /// Resolves the TaskCompletionSource, allowing the agent to proceed (IsApproved=true)
    /// or revise (IsApproved=false with Feedback).
    /// </summary>
    [HttpPost]
    public OperationCallResult<bool> ConfirmPlan([FromBody] WfAgentConfirmPlanRequestDto request)
    {
        var result = new OperationCallResult<bool>();

        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(WorkflowAutomationAgentController),
                "WfAgent_ConfirmPlan_NoSession", ValidationItemType.Error, "SessionId is required."));
            return result;
        }

        bool found = WorkflowAutomationAgentSessionStore.ConfirmPlan(request.SessionId, request.IsApproved);

        if (!found)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(WorkflowAutomationAgentController),
                "WfAgent_ConfirmPlan_NoPending", ValidationItemType.Warning,
                "No pending plan confirmation found for this session. It may have already been resolved or timed out."));
        }

        result.Object = found;
        return result;
    }
}
