using Microsoft.AspNetCore.SignalR;

namespace AppAI.Web.Hubs;

/// <summary>
/// SignalR hub used by the AppBuilder AI Agent to stream real-time events to the browser.
/// Registered at: /signalr/apphub
///
/// Server → client events:
///   agentStep(step)      — one action the agent took (tool call / result / thinking)
///   agentToken(text)     — streaming text from the LLM's final response
///   agentDone(payload)   — run complete: final answer + created transaction list
///   agentError(message)  — unrecoverable error
///
/// Migration note: Hub base class and Clients API are identical to legacy SignalR 2.
/// React client: replace $.connection proxy with @microsoft/signalr HubConnectionBuilder.
/// </summary>
public class AppBuilderAgentHub : Hub
{
    public string GetConnectionId() => Context.ConnectionId;
}
