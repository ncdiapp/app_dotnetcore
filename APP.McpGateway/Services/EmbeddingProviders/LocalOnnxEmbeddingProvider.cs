using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.Onnx;
using McpGateway.Models;

#pragma warning disable CS0618 // BertOnnxTextEmbeddingGenerationService is obsolete — static Create() is the only public instantiation path

namespace McpGateway.Services.EmbeddingProviders;

/// <summary>
/// Runs embeddings fully in-process via a local ONNX BERT model (e.g. all-MiniLM-L6-v2).
/// Zero per-call API cost. ~50ms cold start on first use; subsequent calls are under 5ms.
///
/// One-time model setup:
///   1. Download from https://huggingface.co/optimum/all-MiniLM-L6-v2
///      Files needed:  model.onnx   and   tokenizer/vocab.txt
///   2. Set in appsettings.json (or env vars):
///        SemanticSearch:Provider = "LocalOnnx"
///        SemanticSearch:LocalOnnx:ModelPath = "models/all-MiniLM-L6-v2.onnx"
///        SemanticSearch:LocalOnnx:VocabPath = "models/vocab.txt"
/// </summary>
public class LocalOnnxEmbeddingProvider : IEmbeddingProvider, IDisposable
{
    private readonly LocalOnnxConfig _config;
    private readonly ILogger<LocalOnnxEmbeddingProvider> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private BertOnnxTextEmbeddingGenerationService? _service;
    private bool _initAttempted;
    private bool _disposed;

    public bool IsAvailable =>
        !string.IsNullOrEmpty(_config.ModelPath) &&
        !string.IsNullOrEmpty(_config.VocabPath) &&
        File.Exists(_config.ModelPath) &&
        File.Exists(_config.VocabPath);

    public LocalOnnxEmbeddingProvider(
        IOptions<SemanticSearchSettings> settings,
        ILogger<LocalOnnxEmbeddingProvider> logger)
    {
        _config = settings.Value.LocalOnnx;
        _logger = logger;
    }

    public async Task<float[][]?> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (!IsAvailable || texts.Count == 0) return null;

        var svc = await EnsureInitializedAsync(ct);
        if (svc == null) return null;

        try
        {
            var embeddings = await svc.GenerateEmbeddingsAsync(texts.ToList(), kernel: null, ct);
            return [.. embeddings.Select(e => e.ToArray())];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local ONNX embedding inference failed");
            return null;
        }
    }

    private async Task<BertOnnxTextEmbeddingGenerationService?> EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initAttempted) return _service;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initAttempted) return _service;
            _initAttempted = true;

            _logger.LogInformation("Loading local ONNX model from {ModelPath}", _config.ModelPath);

            var options = new BertOnnxOptions
            {
                MaximumTokens = _config.MaxSequenceLength,
                NormalizeEmbeddings = true
            };

            _service = await BertOnnxTextEmbeddingGenerationService.CreateAsync(
                _config.ModelPath,
                _config.VocabPath,
                options,
                ct);

            _logger.LogInformation("Local ONNX embedding model ready (dim={Dim}, maxTokens={MaxTokens})",
                _config.EmbeddingDimension, _config.MaxSequenceLength);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load local ONNX model from {Path} — semantic search will be disabled",
                _config.ModelPath);
        }
        finally
        {
            _initLock.Release();
        }

        return _service;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _service?.Dispose();
        _initLock.Dispose();
    }
}
