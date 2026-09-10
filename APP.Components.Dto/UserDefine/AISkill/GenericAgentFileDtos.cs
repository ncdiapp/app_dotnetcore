using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class GenericAgentSessionSummaryDto
    {
        public string   SessionKey { get; set; }
        public string   SkillKey   { get; set; }
        /// <summary>Derived from first user message in MessagesJson — not a DB column.</summary>
        public string   Title      { get; set; }
        public DateTime UpdatedAt  { get; set; }
        public bool     IsFixedTestSession { get; set; }
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
