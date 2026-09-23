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
