namespace McpGateway.Models;

public enum ColumnType { Text, Numeric, Date }

public record ColumnDefinition
{
    public int        Index { get; init; }
    public string     Name  { get; init; } = "";
    public ColumnType Type  { get; init; }
}

public class CachedDataset
{
    public string   Name     { get; set; } = "";
    public DateTime CachedAt { get; set; }
    public string   FilePath { get; set; } = "";

    public List<ColumnDefinition>     Columns        { get; set; } = [];
    public List<string[]>             Rows           { get; set; } = [];
    public Dictionary<int, double?[]> NumericColumns { get; set; } = [];
    public Dictionary<int, double?[]> DateColumns    { get; set; } = [];
}
