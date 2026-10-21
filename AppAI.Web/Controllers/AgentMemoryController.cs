using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using App.BL;
using App.BL.AIAgent.GenericAgent.Memory;
using APP.Components.Dto;
using APP.Framework;
using APP.Framework.Communication;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

public class AgentMemoryFactEditDto
{
    public int    FactId { get; set; }
    public string Value  { get; set; } = "";
}

/// <summary>
/// Facts and past-conversation summaries the platform remembers about the signed-in user (Layer 2 and 3 memory),
/// plus a SysAdmin trigger for the extraction job. Every action is scoped to the authenticated user and tenant;
/// no user id or data source comes from the request.
/// </summary>
[Route("webapi/[controller]/[action]")]
public class AgentMemoryController : SecureBaseController
{
    // GET /webapi/AgentMemory/ListFacts?skillKey=...   (omit skillKey for every agent)
    [HttpGet]
    public OperationCallResult<List<AgentMemoryFactDto>> ListFacts([FromQuery] string skillKey = null)
    {
        var result = new OperationCallResult<List<AgentMemoryFactDto>> { Object = new List<AgentMemoryFactDto>() };
        if (TryGetUser(out var userId, out var dsId))
            result.Object = AgentMemoryFactBL.List(userId, skillKey, dsId);
        return result;
    }

    // POST /webapi/AgentMemory/ConfirmFact?factId=...
    [HttpPost]
    public OperationCallResult<bool> ConfirmFact([FromQuery] int factId) =>
        Run((u, ds) => AgentMemoryFactBL.Confirm(factId, u, ds));

    // POST /webapi/AgentMemory/UpdateFact
    [HttpPost]
    public OperationCallResult<bool> UpdateFact([FromBody] AgentMemoryFactEditDto request) =>
        Run((u, ds) => request != null && AgentMemoryFactBL.UpdateValue(request.FactId, u, request.Value, ds));

    // POST /webapi/AgentMemory/DeleteFact?factId=...
    [HttpPost]
    public OperationCallResult<bool> DeleteFact([FromQuery] int factId) =>
        Run((u, ds) => AgentMemoryFactBL.Delete(factId, u, ds));

    // GET /webapi/AgentMemory/ListEpisodes?skillKey=...
    [HttpGet]
    public OperationCallResult<List<AgentEpisodeDto>> ListEpisodes([FromQuery] string skillKey = null)
    {
        var result = new OperationCallResult<List<AgentEpisodeDto>> { Object = new List<AgentEpisodeDto>() };
        if (TryGetUser(out var userId, out var dsId))
            result.Object = AgentEpisodeBL.List(userId, skillKey, dsId);
        return result;
    }

    // POST /webapi/AgentMemory/DeleteEpisode?episodeId=...
    [HttpPost]
    public OperationCallResult<bool> DeleteEpisode([FromQuery] int episodeId) =>
        Run((u, ds) => AgentEpisodeBL.Delete(episodeId, u, ds));

    // POST /webapi/AgentMemory/ForgetEverything — all facts and episodes of the signed-in user.
    [HttpPost]
    public OperationCallResult<bool> ForgetEverything() =>
        Run((u, ds) =>
        {
            AgentMemoryFactBL.DeleteAll(u, ds);
            AgentEpisodeBL.DeleteAll(u, ds);
            return true;
        });

    // POST /webapi/AgentMemory/SetChatMemoryOptOut?skillKey=...&sessionKey=...&optOut=true
    // An opted-out chat is never used for extraction.
    [HttpPost]
    public OperationCallResult<bool> SetChatMemoryOptOut([FromQuery] string skillKey, [FromQuery] string sessionKey, [FromQuery] bool optOut) =>
        Run((u, ds) => AgentMemorySessionBL.SetOptOut(ds, sessionKey, skillKey, u, optOut));

    // POST /webapi/AgentMemory/RunExtraction?mode=DryRun|Preview|On   (SysAdmin)
    // Runs one extraction round over every company-master tenant now. DryRun lists chats, Preview logs proposals
    // without writing, On stores. Does not change the configured AgentMemory:Mode.
    [HttpPost]
    public async Task<IActionResult> RunExtraction([FromQuery] string mode = "DryRun", CancellationToken ct = default)
    {
        if (!AppSecurityUserBL.IsAdminUser())
            throw new Microsoft.AspNetCore.Http.BadHttpRequestException("Forbidden", (int)HttpStatusCode.Forbidden);
        if (!Enum.TryParse<AgentMemoryMode>(mode, true, out var parsed) || parsed == AgentMemoryMode.Off)
            return BadRequest("mode must be DryRun, Preview or On");

        var options = AgentMemoryJob.Current;
        var run = await AgentMemoryJob.RunOnceAsync(new AgentMemoryJobOptions
        {
            Mode = parsed, IdleMinutes = options.IdleMinutes, BatchPerTenant = options.BatchPerTenant, MinNewTurns = options.MinNewTurns
        }, ct);
        return Ok(new { Summary = run.ToString(), run.Lines });
    }

    private static bool TryGetUser(out int userId, out int dataSourceId)
    {
        userId = 0;
        dataSourceId = 0;
        if (ServerContext.Instance.CurrnetClientIdentity is not AppClientIdentity ai || ai.UserId == null) return false;
        userId       = Convert.ToInt32(ai.UserId);
        dataSourceId = ai.DataSourceId;
        return userId > 0;
    }

    private static OperationCallResult<bool> Run(Func<int, int, bool> action)
    {
        var result = new OperationCallResult<bool>();
        if (TryGetUser(out var userId, out var dsId)) result.Object = action(userId, dsId);
        return result;
    }
}
