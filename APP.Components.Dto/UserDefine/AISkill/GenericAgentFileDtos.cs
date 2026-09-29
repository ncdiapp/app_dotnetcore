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
}
