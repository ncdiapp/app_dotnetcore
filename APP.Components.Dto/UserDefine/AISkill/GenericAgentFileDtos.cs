using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class GenericAgentSessionSummaryDto
    {
        public string   SessionKey { get; set; }
        public string   SkillKey   { get; set; }
        /// <summary>DisplayTitle if renamed; otherwise first real user message.</summary>
        public string   Title      { get; set; }
        public DateTime UpdatedAt  { get; set; }
        public bool     IsFixedTestSession { get; set; }
    }

    public class GenericAgentRenameChatDto
    {
        public string SkillKey   { get; set; }
        public string SessionKey { get; set; }
        public string Title      { get; set; }
    }

    public class GenericAgentSavedSkillDto
    {
        public string   SkillKey   { get; set; }
        public string   SessionKey { get; set; }
        public int      SkillId    { get; set; }
        public string   SkillName  { get; set; }
        public DateTime SavedAt    { get; set; }
    }

    public class GenericAgentFileDto
    {
        public string   RelativePath { get; set; }
        public long     SizeBytes    { get; set; }
        public DateTime UpdatedAt    { get; set; }
        public bool     IsDirectory  { get; set; }
        /// <summary>Optional registered description from .agent-file-catalog.json (Default Source / chat source).</summary>
        public string   Description  { get; set; }
    }

    public class AgentFileCatalogDto
    {
        public int Version { get; set; } = 1;
        public List<AgentFileCatalogEntryDto> Files { get; set; } = new List<AgentFileCatalogEntryDto>();
    }

    public class AgentFileCatalogEntryDto
    {
        public string Path { get; set; }
        public string Description { get; set; }
    }

    public class GenericAgentFileDescriptionDto
    {
        public string RelativePath { get; set; }
        public string Description { get; set; }
    }

    public class GenericAgentFileContentDto
    {
        public string RelativePath { get; set; }
        public string Content      { get; set; }
        public bool   Truncated    { get; set; }
    }

    public class GenericAgentFilePathDto
    {
        public string SessionKey   { get; set; }
        public string RelativePath { get; set; }
        public string Content      { get; set; }
        public string NewPath      { get; set; }
    }

    /// <summary>Result chrome Print/Export — server GENERATE via document plugin.</summary>
    public class GenericAgentExportResultRequestDto
    {
        public string SkillKey { get; set; }
        public string SessionKey { get; set; }
        public string Format { get; set; }
        public string Layout { get; set; }
        public string Title { get; set; }
        public string DatasetName { get; set; }
        /// <summary>Cards per row (columns). User 2x2 → 2.</summary>
        public int CardsPerRow { get; set; }
        /// <summary>Card rows per printed/exported page. User 2x2 → 2.</summary>
        public int CardRows { get; set; }
        /// <summary>Optional columns×rows e.g. 2x2 (overrides CardsPerRow/CardRows when parseable).</summary>
        public string PageGrid { get; set; }
        /// <summary>Required for layout kpi/chart/dashboard (kpis/charts from Result SpecJson).</summary>
        public string SpecJson { get; set; }
        /// <summary>
        /// Result rows JSON array (from restored DataJson). Used when session analyze cache
        /// is gone after reopen — Print/Export must not require a live dataset cache.
        /// </summary>
        public string RecordsJson { get; set; }
    }

    public class GenericAgentExportResultDto
    {
        public string Path { get; set; }
        public string Error { get; set; }
    }
}
