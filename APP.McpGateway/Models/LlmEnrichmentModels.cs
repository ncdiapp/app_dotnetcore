namespace McpGateway.Models;

/// <summary>
/// LLM-generated enrichment data for a single API endpoint.
/// </summary>
public record EnrichedEndpointData
{
    public string OperationId { get; init; } = "";

    /// <summary>Action-oriented snake_case tool name (e.g., get_inventory_status).</summary>
    public string Name { get; init; } = "";

    /// <summary>3-4 sentence agent instruction covering when, context, caution, and format.</summary>
    public string AgentDescription { get; init; } = "";

    /// <summary>System note for the agent: edge cases, call ordering, rate limits.</summary>
    public string? PromptHelp { get; init; }

    /// <summary>LLM-generated JSON Schema for the endpoint's parameters.</summary>
    public string? InputSchemaJson { get; init; }
}

// ── LLM Enrichment — endpoint description generation ────────────────────────

/// <summary>
/// Settings for LLM-based endpoint description enrichment.
/// Bound to the "LlmEnrichment" config section.
/// Set <see cref="Provider"/> to: Anthropic | OpenAI | AzureOpenAI | Gemini
/// </summary>
public class LlmEnrichmentSettings
{
    public const string SectionName = "LlmEnrichment";

    /// <summary>Enable LLM description generation on startup. Requires the chosen Provider to be configured.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>LLM used for generating endpoint descriptions: Anthropic | OpenAI | AzureOpenAI | Gemini</summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Directory to persist description caches. Relative paths resolve from the working directory.</summary>
    public string CacheDirectory { get; set; } = "enrichment-cache";

    /// <summary>Max endpoints per LLM batch call. Keep ≤ 30 to stay within context limits.</summary>
    public int MaxEndpointsPerBatch { get; set; } = 20;

    public AnthropicProviderConfig Anthropic { get; set; } = new();
    public OpenAiProviderConfig OpenAI { get; set; } = new();
    public AzureOpenAiProviderConfig AzureOpenAI { get; set; } = new();
    public GeminiProviderConfig Gemini { get; set; } = new();
}

// ── Semantic Search — RAG embedding vectors ──────────────────────────────────

/// <summary>
/// Settings for RAG semantic search via embedding vectors.
/// Bound to the "SemanticSearch" config section.
/// Set <see cref="Provider"/> to: LocalOnnx | AzureOpenAI | OpenAI | None
/// </summary>
public class SemanticSearchSettings
{
    public const string SectionName = "SemanticSearch";

    /// <summary>Embedding backend for semantic search: LocalOnnx | AzureOpenAI | OpenAI | None</summary>
    public string Provider { get; set; } = "None";

    /// <summary>Directory to persist embedding vector caches. Relative paths resolve from the working directory.</summary>
    public string CacheDirectory { get; set; } = "embedding-cache";

    public LocalOnnxConfig LocalOnnx { get; set; } = new();
    public AzureOpenAiEmbeddingConfig AzureOpenAI { get; set; } = new();
    public OpenAiEmbeddingConfig OpenAI { get; set; } = new();
}

// ── Data Analysis — CSV/JSON cache + columnar in-memory store ────────────────

/// <summary>
/// Settings for the DataAnalysis ui_hint pipeline.
/// Bound to the "DataAnalysis" config section.
/// </summary>
public class DataAnalysisSettings
{
    public const string SectionName = "DataAnalysis";

    /// <summary>Directory for global (shared) dataset JSON files. Relative paths resolve from the working directory.</summary>
    public string CacheDirectory { get; set; } = "data-analysis-cache";

    /// <summary>Maximum rows allowed per dataset. Requests exceeding this limit return an error instead of caching.</summary>
    public int MaxRows { get; set; } = 50_000;
}

// ── LLM provider configs ─────────────────────────────────────────────────────

public class AnthropicProviderConfig
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-4-6";
}

public class OpenAiProviderConfig
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o";
}

public class AzureOpenAiProviderConfig
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    /// <summary>Chat completion deployment name (e.g. gpt-4.1).</summary>
    public string DeploymentName { get; set; } = "";
    public string ApiVersion { get; set; } = "2024-02-01";
}

public class GeminiProviderConfig
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-1.5-pro";
}

// ── Embedding provider configs ───────────────────────────────────────────────

public class LocalOnnxConfig
{
    /// <summary>Path to the ONNX model file. Download from https://huggingface.co/optimum/all-MiniLM-L6-v2</summary>
    public string ModelPath { get; set; } = "EmbedModels/all-MiniLM-L6-v2.onnx";

    /// <summary>Path to the BERT vocab.txt from the same HuggingFace repo.</summary>
    public string VocabPath { get; set; } = "EmbedModels/vocab.txt";

    public int MaxSequenceLength { get; set; } = 128;
    public int EmbeddingDimension { get; set; } = 384;
}

public class AzureOpenAiEmbeddingConfig
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    /// <summary>Embedding deployment name (e.g. text-embedding-ada-002).</summary>
    public string DeploymentName { get; set; } = "";
    public string ApiVersion { get; set; } = "2024-02-01";
}

public class OpenAiEmbeddingConfig
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "text-embedding-ada-002";
}
