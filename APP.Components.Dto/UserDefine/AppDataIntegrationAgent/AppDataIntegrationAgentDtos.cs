using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class AppDataIntegrationAgentStartRequestDto
    {
        public string UserMessage { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public string SkillKey { get; set; }
        public List<AppDataIntegrationAgentMessageDto> ConversationHistory { get; set; }
    }

    public class AppDataIntegrationAgentSkillMenuItemDto
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
        public string GroupLabel { get; set; }
    }

    public class AppDataIntegrationAgentSkillMenuDto
    {
        public string DefaultKey { get; set; }
        public List<AppDataIntegrationAgentSkillMenuItemDto> Items { get; set; } = new List<AppDataIntegrationAgentSkillMenuItemDto>();
    }

    public class AppDataIntegrationAgentFollowUpRequestDto
    {
        public string SessionId { get; set; }
        public string UserMessage { get; set; }
        public string SkillKey { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
    }

    public class AppDataIntegrationAgentCancelRequestDto
    {
        public string SessionId { get; set; }
    }

    public class AppDataIntegrationAgentConfirmGateRequestDto
    {
        public string SessionId { get; set; }
        public string GateId { get; set; }
        public bool Confirmed { get; set; }
        public string Feedback { get; set; }
    }

    public class AppDataIntegrationAgentResumeRequestDto
    {
        public string SessionId { get; set; }
        public string UserMessage { get; set; }
    }

    public class AppDataIntegrationAgentFileRequestDto
    {
        public string SessionId { get; set; }
        public string RelativePath { get; set; }
    }

    public class AppDataIntegrationAgentMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string Timestamp { get; set; }

        /// <summary>Assistant turn start (UTC ISO). User messages use Timestamp only.</summary>
        public string StartedAt { get; set; }

        /// <summary>Assistant turn wall-clock seconds (Cursor run through reply).</summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// AppConfigPack paths written/validated in this assistant turn (for Start Build UI after reload).
        /// </summary>
        public List<string> WrittenPackPaths { get; set; }

        /// <summary>
        /// Open-page / table-preview offers for this assistant turn (Open button after reload).
        /// </summary>
        public List<AppDataIntegrationAgentOpenUiOfferDto> OpenUiOffers { get; set; }
    }

    /// <summary>Persisted Open box payload (navigate or table_preview).</summary>
    public class AppDataIntegrationAgentOpenUiOfferDto
    {
        /// <summary>navigate | table_preview</summary>
        public string Kind { get; set; }
        public string Label { get; set; }
        public string RouteCode { get; set; }
        public string Link { get; set; }
        public Dictionary<string, object> ParamObj { get; set; }
        public List<AppDataIntegrationAgentTablePreviewItemDto> Tables { get; set; }
    }

    public class AppDataIntegrationAgentStartResultDto
    {
        public bool IsStarted { get; set; }
        public string SessionId { get; set; }
        public string CloudAgentId { get; set; }
        public string WorkspaceRelativePath { get; set; }
        public string Error { get; set; }
    }

    public class AppDataIntegrationAgentStepEvent
    {
        public string Type { get; set; }
        public string ToolName { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class AppDataIntegrationAgentFileEvent
    {
        public string Action { get; set; }
        public string RelativePath { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    /// <summary>Ask the browser UI to open an App tab (RouteCode + paramObj).</summary>
    public class AppDataIntegrationAgentNavigateEvent
    {
        public string RouteCode { get; set; }
        public string Label { get; set; }
        public string Link { get; set; }
        /// <summary>Optional full param object; when null, UI builds from RouteCode + Link.</summary>
        public Dictionary<string, object> ParamObj { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    /// <summary>Ask the browser UI to open TablesDataPreviewModal (multi-table header tabs).</summary>
    public class AppDataIntegrationAgentTablePreviewEvent
    {
        public List<AppDataIntegrationAgentTablePreviewItemDto> Tables { get; set; }
            = new List<AppDataIntegrationAgentTablePreviewItemDto>();
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class AppDataIntegrationAgentTablePreviewItemDto
    {
        public string TableName { get; set; }
        public int? DataSourceId { get; set; }
        public string SchemaOwner { get; set; }
    }

    public class AppDataIntegrationAgentGateEvent
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

    public class AppDataIntegrationAgentDoneEvent
    {
        public string FinalResponse { get; set; }
        public List<AppDataIntegrationAgentMessageDto> UpdatedHistory { get; set; } = new List<AppDataIntegrationAgentMessageDto>();
        public List<string> WorkspaceFiles { get; set; } = new List<string>();
        /// <summary>Open offers from this turn (in case navigate events were polled after done).</summary>
        public List<AppDataIntegrationAgentOpenUiOfferDto> OpenUiOffers { get; set; }
        /// <summary>Run timed out or Cursor cloud run still active — workspace files may be partial.</summary>
        public bool IsIncomplete { get; set; }
    }

    public class AppDataIntegrationAgentEventDto
    {
        /// <summary>step | token | file | gate | navigate | table_preview | done | error</summary>
        public string EventType { get; set; }
        public AppDataIntegrationAgentStepEvent Step { get; set; }
        public string Token { get; set; }
        public AppDataIntegrationAgentFileEvent File { get; set; }
        public AppDataIntegrationAgentGateEvent Gate { get; set; }
        public AppDataIntegrationAgentNavigateEvent Navigate { get; set; }
        public AppDataIntegrationAgentTablePreviewEvent TablePreview { get; set; }
        public AppDataIntegrationAgentDoneEvent Done { get; set; }
        public string Error { get; set; }
    }

    public class AppDataIntegrationAgentPollResponseDto
    {
        public List<AppDataIntegrationAgentEventDto> Events { get; set; } = new List<AppDataIntegrationAgentEventDto>();
        public bool SessionExists { get; set; }
    }

    public class AppDataIntegrationAgentSessionSummaryDto
    {
        public string SessionGuid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserRequest { get; set; }
        public string DisplayTitle { get; set; }
        public string Status { get; set; }
        public string CloudAgentId { get; set; }
        public int? SaasApplicationId { get; set; }
        public int? DataSourceRegisterId { get; set; }
        public string SkillKey { get; set; }
        public string WorkspaceRelativePath { get; set; }
        public string FinalResponse { get; set; }
        public bool IsArchived { get; set; }
        public int SortOrder { get; set; }
    }

    public class AppDataIntegrationAgentRenameSessionRequestDto
    {
        public string SessionId { get; set; }
        public string Title { get; set; }
    }

    public class AppDataIntegrationAgentArchiveSessionsRequestDto
    {
        public List<string> SessionIds { get; set; }
        public bool Archived { get; set; }
    }

    public class AppDataIntegrationAgentDeleteSessionsRequestDto
    {
        public List<string> SessionIds { get; set; }
    }

    public class AppDataIntegrationAgentReorderSessionsRequestDto
    {
        public List<string> SessionIds { get; set; }
    }

    public class AppDataIntegrationAgentSessionFullDto : AppDataIntegrationAgentSessionSummaryDto
    {
        public List<AppDataIntegrationAgentMessageDto> ConversationHistory { get; set; }
        public string LatestRunId { get; set; }
        public string PendingGateJson { get; set; }
    }

    public class AppDataIntegrationAgentWorkspaceFileDto
    {
        public string RelativePath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDirectory { get; set; }
        public string PublicUrl { get; set; }
    }

    public class AppDataIntegrationAgentFileContentDto
    {
        public string RelativePath { get; set; }
        public string Content { get; set; }
        public bool Truncated { get; set; }
    }

    public class AppDataIntegrationAgentGateResult
    {
        public bool Confirmed { get; set; }
        public string Feedback { get; set; }
        public string ExecutionResult { get; set; }
    }

    public class AppDataIntegrationAgentDataSourceItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DatabaseName { get; set; }
    }
}
