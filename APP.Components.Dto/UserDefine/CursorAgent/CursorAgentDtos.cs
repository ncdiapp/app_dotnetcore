using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class CursorAgentStartRequestDto
    {
        public string UserMessage { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public string SkillKey { get; set; }
        public List<CursorAgentMessageDto> ConversationHistory { get; set; }
    }

    public class CursorAgentSkillMenuItemDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
        public string GroupLabel { get; set; }
    }

    public class CursorAgentSkillMenuDto
    {
        public string DefaultKey { get; set; }
        public List<CursorAgentSkillMenuItemDto> Items { get; set; } = new List<CursorAgentSkillMenuItemDto>();
    }

    public class CursorAgentFollowUpRequestDto
    {
        public string SessionId { get; set; }
        public string UserMessage { get; set; }
        public string SkillKey { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
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
        public string Timestamp { get; set; }

        /// <summary>
        /// AppConfigPack paths written/validated in this assistant turn (for Start Build UI after reload).
        /// </summary>
        public List<string> WrittenPackPaths { get; set; }

        /// <summary>
        /// Open-page / table-preview offers for this assistant turn (Open button after reload).
        /// </summary>
        public List<CursorAgentOpenUiOfferDto> OpenUiOffers { get; set; }
    }

    /// <summary>Persisted Open box payload (navigate or table_preview).</summary>
    public class CursorAgentOpenUiOfferDto
    {
        /// <summary>navigate | table_preview</summary>
        public string Kind { get; set; }
        public string Label { get; set; }
        public string RouteCode { get; set; }
        public string Link { get; set; }
        public Dictionary<string, object> ParamObj { get; set; }
        public List<CursorAgentTablePreviewItemDto> Tables { get; set; }
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

    /// <summary>Ask the browser UI to open an App tab (RouteCode + paramObj).</summary>
    public class CursorAgentNavigateEvent
    {
        public string RouteCode { get; set; }
        public string Label { get; set; }
        public string Link { get; set; }
        /// <summary>Optional full param object; when null, UI builds from RouteCode + Link.</summary>
        public Dictionary<string, object> ParamObj { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    /// <summary>Ask the browser UI to open TablesDataPreviewModal (multi-table header tabs).</summary>
    public class CursorAgentTablePreviewEvent
    {
        public List<CursorAgentTablePreviewItemDto> Tables { get; set; }
            = new List<CursorAgentTablePreviewItemDto>();
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class CursorAgentTablePreviewItemDto
    {
        public string TableName { get; set; }
        public int? DataSourceId { get; set; }
        public string SchemaOwner { get; set; }
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
        /// <summary>Open offers from this turn (in case navigate events were polled after done).</summary>
        public List<CursorAgentOpenUiOfferDto> OpenUiOffers { get; set; }
    }

    public class CursorAgentEventDto
    {
        /// <summary>step | token | file | gate | navigate | table_preview | done | error</summary>
        public string EventType { get; set; }
        public CursorAgentStepEvent Step { get; set; }
        public string Token { get; set; }
        public CursorAgentFileEvent File { get; set; }
        public CursorAgentGateEvent Gate { get; set; }
        public CursorAgentNavigateEvent Navigate { get; set; }
        public CursorAgentTablePreviewEvent TablePreview { get; set; }
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
        public string DisplayTitle { get; set; }
        public string Status { get; set; }
        public string CursorAgentId { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public string SkillKey { get; set; }
        public string WorkspaceRelativePath { get; set; }
        public string FinalResponse { get; set; }
        public bool IsArchived { get; set; }
        public int SortOrder { get; set; }
    }

    public class CursorAgentRenameSessionRequestDto
    {
        public string SessionId { get; set; }
        public string Title { get; set; }
    }

    public class CursorAgentArchiveSessionsRequestDto
    {
        public List<string> SessionIds { get; set; }
        public bool Archived { get; set; }
    }

    public class CursorAgentDeleteSessionsRequestDto
    {
        public List<string> SessionIds { get; set; }
    }

    public class CursorAgentReorderSessionsRequestDto
    {
        public List<string> SessionIds { get; set; }
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
