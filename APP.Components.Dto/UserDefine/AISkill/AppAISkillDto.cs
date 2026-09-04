using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class AppAISkillDto
    {
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SkillContent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<AppAISkillRefDto> References { get; set; } = new List<AppAISkillRefDto>();
    }

    public class AppAISkillRefDto
    {
        public int RefId { get; set; }
        public int SkillId { get; set; }
        public string FileName { get; set; }
        public string FileContent { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AppAgentSkillSetDto
    {
        public string SkillKey            { get; set; } = "";
        public string DisplayName         { get; set; } = "";
        public string Description         { get; set; } = "";
        public string SystemPrompt        { get; set; } = "";
        public int    CapabilityFlags     { get; set; }
        public bool   IsActive            { get; set; } = true;
        public int    SortOrder           { get; set; }
        public int    Version             { get; set; } = 1;
        public int    MaxHistoryTokens    { get; set; } = 80000;
        public int    SummarizeThreshold  { get; set; } = 60000;
        public int    MaxToolResultChars  { get; set; } = 4000;
        public int    RecentWindowSize    { get; set; } = 10;
        public int    MaxIterations       { get; set; } = 40;
        public string ExecutionMode       { get; set; } = "Interactive";
    }

    public class AppAgentToolRegisterDto
    {
        public int    Id          { get; set; }
        public string SkillKey    { get; set; } = "";
        public string ToolName    { get; set; } = "";
        public string Description { get; set; } = "";
        public string ToolType    { get; set; } = "BuiltIn";
        public string ToolConfig  { get; set; } = "{}";
        public bool   IsActive    { get; set; } = true;
        public int    SortOrder   { get; set; }
    }

    public class AppAgentMcpServerDto
    {
        public string McpServerKey { get; set; } = "";
        public string ServerUrl    { get; set; } = "";
        public string Transport    { get; set; } = "streamable-http";
        public string AuthType     { get; set; } = "none";
        public string AuthValue    { get; set; } = "";
        public bool   IsActive     { get; set; } = true;
    }
}
