using System;
using System.Collections.Generic;

namespace App.BL.AIAgent.GenericAgent.DataAnalyze
{
    public enum AgentColumnType { Text, Numeric, Date }

    public sealed class AgentColumnDefinition
    {
        public int Index { get; init; }
        public string Name { get; init; } = "";
        public AgentColumnType Type { get; init; }
    }

    /// <summary>In-memory columnar dataset for session-scoped data_analyze (inspired by MCP AnalysisService shape).</summary>
    public sealed class AgentCachedDataset
    {
        public string Name { get; set; } = "";
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
        public string SourceTool { get; set; } = "";
        public List<AgentColumnDefinition> Columns { get; set; } = new();
        public List<string[]> Rows { get; set; } = new();
        public Dictionary<int, double?[]> NumericColumns { get; set; } = new();
        public Dictionary<int, double?[]> DateColumns { get; set; } = new();
    }
}
