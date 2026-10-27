using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL.GenericAgent;
using APP.Components.Dto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Memory
{
    public sealed class EmbeddingResult
    {
        public float[] Vector { get; init; } = Array.Empty<float>();
        /// <summary>"provider:model"; vectors are only comparable when this matches.</summary>
        public string  Model  { get; init; } = "";
    }

    /// <summary>
    /// Text embeddings for episodic recall. Uses the tenant's default provider when it has an embedding
    /// endpoint (Gemini, OpenAI). Anthropic has none, so it falls back to a configured Gemini or OpenAI key;
    /// with neither, <see cref="EmbedAsync"/> returns null and recall is skipped.
    /// </summary>
    public static class EmbeddingClient
    {
        private const string GeminiModel = "gemini-embedding-001";
        private const string OpenAiModel = "text-embedding-3-small";
        private const int    MaxInputChars = 6000;

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        /// <summary>Cosine similarity at or above which an episode counts as relevant. Scales differ per model.</summary>
        public static double RecallThreshold(string model) =>
            model.StartsWith("openai:", StringComparison.Ordinal) ? 0.45 : 0.70;

        public static async Task<EmbeddingResult> EmbedAsync(string text, AppClientIdentity identity, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var input = text.Length > MaxInputChars ? text.Substring(0, MaxInputChars) : text;
            try
            {
                var provider = (AIConfigSettingBL.GetProvider(identity) ?? "").Trim().ToLowerInvariant();
                var geminiKey = AIConfigSettingBL.GetApiKeyForProvider("gemini", identity);
                var openAiKey = AIConfigSettingBL.GetApiKeyForProvider("openai", identity);

                if (provider == "openai" && !string.IsNullOrWhiteSpace(openAiKey))
                    return await OpenAiAsync(input, openAiKey, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(geminiKey) && provider != "openai")
                    return await GeminiAsync(input, geminiKey, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(openAiKey))
                    return await OpenAiAsync(input, openAiKey, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Embedding request failed — recall skipped");
            }
            return null;
        }

        private static async Task<EmbeddingResult> GeminiAsync(string input, string apiKey, CancellationToken ct)
        {
            var url  = $"https://generativelanguage.googleapis.com/v1beta/models/{GeminiModel}:embedContent";
            var body = new JObject
            {
                ["model"] = "models/" + GeminiModel,
                ["content"] = new JObject { ["parts"] = new JArray(new JObject { ["text"] = input }) },
                ["outputDimensionality"] = 768
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("x-goog-api-key", apiKey);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("Gemini embedding failed: {0}", (int)resp.StatusCode);
                return null;
            }
            var values = JObject.Parse(json)["embedding"]?["values"] as JArray;
            return values == null || values.Count == 0
                ? null
                : new EmbeddingResult { Vector = Normalize(values), Model = "gemini:" + GeminiModel };
        }

        private static async Task<EmbeddingResult> OpenAiAsync(string input, string apiKey, CancellationToken ct)
        {
            var body = new JObject { ["model"] = OpenAiModel, ["input"] = input };
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
            {
                Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("OpenAI embedding failed: {0}", (int)resp.StatusCode);
                return null;
            }
            var values = JObject.Parse(json)["data"]?[0]?["embedding"] as JArray;
            return values == null || values.Count == 0
                ? null
                : new EmbeddingResult { Vector = Normalize(values), Model = "openai:" + OpenAiModel };
        }

        private static float[] Normalize(JArray values)
        {
            var v = new float[values.Count];
            double sum = 0;
            for (var i = 0; i < v.Length; i++)
            {
                v[i] = values[i].Value<float>();
                sum += (double)v[i] * v[i];
            }
            var norm = Math.Sqrt(sum);
            if (norm > 0)
                for (var i = 0; i < v.Length; i++) v[i] = (float)(v[i] / norm);
            return v;
        }

        public static byte[] ToBytes(float[] vector)
        {
            var bytes = new byte[vector.Length * sizeof(float)];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[] FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length % sizeof(float) != 0) return Array.Empty<float>();
            var v = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, v, 0, bytes.Length);
            return v;
        }

        /// <summary>Dot product; equals cosine similarity because stored vectors are unit length.</summary>
        public static double Cosine(float[] a, float[] b)
        {
            if (a.Length == 0 || a.Length != b.Length) return 0;
            double dot = 0;
            for (var i = 0; i < a.Length; i++) dot += (double)a[i] * b[i];
            return dot;
        }
    }
}
