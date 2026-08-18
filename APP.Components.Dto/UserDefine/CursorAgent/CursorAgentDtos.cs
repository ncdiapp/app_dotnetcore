using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class CursorAgentStartRequestDto
    {
        public string UserMessage { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public List<CursorAgentMessageDto> ConversationHistory { get; set; }
    }

    public class CursorAgentFollowUpRequestDto
    {
        public string SessionId { get; set; }
        public string UserMessage { get; set; }
    }

    public class CursorAgentCancelRequestDto
    {
        public string SessionId { get; set; }
    }

    public class CursorAgentConfirmGateRequestDto
    {
        public string SessionId { get; set; }
        public string GateId { get; set; }
        public bool Confirmed { get; set; }
        public string Feedback { get; set; }
    }

    public class CursorAgentResumeRequestDto
    {
        public string SessionId { get; set; }
        public string UserMessage { get; set; }
    }

    public class CursorAgentFileRequestDto
    {
        public string SessionId { get; set; }
        public string RelativePath { get; set; }
    }

    public class CursorAgentMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class CursorAgentStartResultDto
    {
        public bool IsStarted { get; set; }
        public string SessionId { get; set; }
        public string CursorAgentId { get; set; }
        public string WorkspaceRelativePath { get; set; }
        public string Error { get; set; }
    }

    public class CursorAgentStepEvent
    {
        public string Type { get; set; }
        public string ToolName { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class CursorAgentFileEvent
    {
        public string Action { get; set; }
        public string RelativePath { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class CursorAgentGateEvent
    {
        public string GateId { get; set; }
        public string Kind { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string RelativePath { get; set; }
        public string Sql { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public object Preview { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class CursorAgentDoneEvent
    {
        public string FinalResponse { get; set; }
        public List<CursorAgentMessageDto> UpdatedHistory { get; set; } = new List<CursorAgentMessageDto>();
        public List<string> WorkspaceFiles { get; set; } = new List<string>();
    }

    public class CursorAgentEventDto
    {
        /// <summary>step | token | file | gate | done | error</summary>
        public string EventType { get; set; }
        public CursorAgentStepEvent Step { get; set; }
        public string Token { get; set; }
        public CursorAgentFileEvent File { get; set; }
        public CursorAgentGateEvent Gate { get; set; }
        public CursorAgentDoneEvent Done { get; set; }
        public string Error { get; set; }
    }

    public class CursorAgentPollResponseDto
    {
        public List<CursorAgentEventDto> Events { get; set; } = new List<CursorAgentEventDto>();
        public bool SessionExists { get; set; }
    }

    public class CursorAgentSessionSummaryDto
    {
        public string SessionGuid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserRequest { get; set; }
        public string Status { get; set; }
        public string CursorAgentId { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public string WorkspaceRelativePath { get; set; }
        public string FinalResponse { get; set; }
    }

    public class CursorAgentSessionFullDto : CursorAgentSessionSummaryDto
    {
        public List<CursorAgentMessageDto> ConversationHistory { get; set; }
        public string LatestRunId { get; set; }
        public string PendingGateJson { get; set; }
    }

    public class CursorAgentWorkspaceFileDto
    {
        public string RelativePath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDirectory { get; set; }
        public string PublicUrl { get; set; }
    }

    public class CursorAgentFileContentDto
    {
        public string RelativePath { get; set; }
        public string Content { get; set; }
        public bool Truncated { get; set; }
    }

    public class CursorAgentGateResult
    {
        public bool Confirmed { get; set; }
        public string Feedback { get; set; }
        public string ExecutionResult { get; set; }
    }
}
