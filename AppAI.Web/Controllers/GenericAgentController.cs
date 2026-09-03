using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppAI.Web.Controllers;

/// <summary>
/// Generic AI agent endpoint — any skill registered in AppAgentSkillSet.
///
/// Streaming pattern (SSE):
///   1. POST /RunAgent   → { SessionId }
///   2. GET  /StreamEvents?sessionId=...  → SSE stream
///   3. React stops on event type "done" or "error"
///
/// Polling fallback:
///   GET /PollEvents?sessionId=...  (call every 500 ms)
///
/// Plan/schema gates:
///   POST /ConfirmPlan    → unblocks propose_plan
///   POST /ConfirmSchema  → unblocks propose_schema
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

        var sessionId = GenericAgentSessionStore.CreateSession();

        var callbacks = new GenericAgentCallbacks
        {
            OnStep = step => { GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "step", Step = step }); return Task.CompletedTask; },
            OnToken = token => { GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "token", Token = token }); return Task.CompletedTask; },
            OnDone = done => { GenericAgentSessionStore.Enqueue(sessionId, new AgentEventDto { EventType = "done", Done = new AgentDoneEvent { FinalResponse = done } }); return Task.CompletedTask; },
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
                CancellationToken.None).ConfigureAwait(false);
        });

        result.Object = new GenericAgentStartResultDto { IsStarted = true, SessionId = sessionId };
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
}
