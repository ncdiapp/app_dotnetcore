#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Minimal SK IChatCompletionService wrapper for the Anthropic Messages API.
    /// Used because no official SK Anthropic connector package is available on NuGet.
    ///
    /// Supports FunctionChoiceBehavior.Auto(): tools are extracted from the kernel,
    /// serialized to Anthropic's tool format, and function call responses are returned
    /// as FunctionCallContent items that SK's ChatCompletionAgent dispatches.
    /// </summary>
    internal sealed class AnthropicChatCompletionService : IChatCompletionService
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        private readonly string _model;
        private readonly string _apiKey;

        public AnthropicChatCompletionService(string model, string apiKey)
        {
            _model  = model;
            _apiKey = apiKey;
        }

        public IReadOnlyDictionary<string, object?> Attributes
            => new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

        // ─────────────────────────────────────────────────────────────────────
        // IChatCompletionService — non-streaming
        // ─────────────────────────────────────────────────────────────────────

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory              chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel?                  kernel            = null,
            CancellationToken        cancellationToken = default)
        {
            var tools    = BuildAnthropicTools(chatHistory, executionSettings, kernel);
            var messages = BuildAnthropicMessages(chatHistory);
            var system   = BuildSystemPrompt(chatHistory);

            var body = new Dictionary<string, object>
            {
                ["model"]      = _model,
                ["max_tokens"] = 8192,
                ["system"]     = system,
                ["messages"]   = messages
            };
            if (tools != null && tools.Count > 0)
                body["tools"] = tools;

            var json    = JsonConvert.SerializeObject(body);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var raw      = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Anthropic API error {(int)response.StatusCode}: {raw}");

            return ParseAnthropicResponse(raw);
        }

        // ─────────────────────────────────────────────────────────────────────
        // IChatCompletionService — streaming (simulated as one-shot response)
        // ─────────────────────────────────────────────────────────────────────

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory              chatHistory,
            PromptExecutionSettings? executionSettings         = null,
            Kernel?                  kernel                    = null,
            [EnumeratorCancellation]
            CancellationToken        cancellationToken         = default)
        {
            var results = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken)
                              .ConfigureAwait(false);

            foreach (var msg in results)
            {
                foreach (var item in msg.Items)
                {
                    if (item is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                    {
                        yield return new StreamingChatMessageContent(msg.Role, tc.Text) { ModelId = _model };
                    }
                    else if (item is FunctionCallContent fc)
                    {
                        var argJson = fc.Arguments != null
                            ? JsonConvert.SerializeObject(fc.Arguments.ToDictionary(kv => kv.Key, kv => kv.Value))
                            : "{}";

                        var items = new StreamingKernelContentItemCollection
                        {
                            new StreamingFunctionCallUpdateContent(fc.Id, fc.FunctionName, argJson, 0)
                        };
                        yield return new StreamingChatMessageContent(msg.Role, content: null)
                        {
                            Items   = items,
                            ModelId = _model
                        };
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Anthropic message format builders
        // ─────────────────────────────────────────────────────────────────────

        private static string BuildSystemPrompt(ChatHistory history)
        {
            var parts = history
                .Where(m => m.Role == AuthorRole.System)
                .Select(m => m.Content ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join("\n", parts);
        }

        private static List<object> BuildAnthropicMessages(ChatHistory history)
        {
            var msgs = new List<object>();
            foreach (var msg in history.Where(m => m.Role != AuthorRole.System))
            {
                if (msg.Role == AuthorRole.User)
                {
                    var funcResults = msg.Items.OfType<FunctionResultContent>().ToList();
                    if (funcResults.Count > 0)
                    {
                        var content = funcResults.Select(fr => (object)new
                        {
                            type        = "tool_result",
                            tool_use_id = fr.CallId ?? "",
                            content     = fr.Result?.ToString() ?? ""
                        }).ToList();
                        msgs.Add(new { role = "user", content });
                    }
                    else
                    {
                        msgs.Add(new { role = "user", content = msg.Content ?? "" });
                    }
                }
                else if (msg.Role == AuthorRole.Assistant)
                {
                    var blocks = new List<object>();
                    foreach (var item in msg.Items)
                    {
                        if (item is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                            blocks.Add(new { type = "text", text = tc.Text });
                        else if (item is FunctionCallContent fc)
                        {
                            var input = fc.Arguments?.ToDictionary(kv => kv.Key, kv => kv.Value)
                                        ?? new Dictionary<string, object?>();
                            blocks.Add(new { type = "tool_use", id = fc.Id ?? "", name = fc.FunctionName ?? "", input });
                        }
                    }
                    if (blocks.Count > 0)
                        msgs.Add(new { role = "assistant", content = blocks });
                    else if (!string.IsNullOrEmpty(msg.Content))
                        msgs.Add(new { role = "assistant", content = msg.Content });
                }
            }
            return msgs;
        }

        private static List<object>? BuildAnthropicTools(ChatHistory history, PromptExecutionSettings? settings, Kernel? kernel)
        {
            if (settings?.FunctionChoiceBehavior == null || kernel == null) return null;
            try
            {
                var ctx = new FunctionChoiceBehaviorConfigurationContext(history) { Kernel = kernel };
                var cfg = settings.FunctionChoiceBehavior.GetConfiguration(ctx);
                if (cfg.Functions == null || !cfg.Functions.Any()) return null;
                return cfg.Functions.Select(f => (object)new
                {
                    name         = f.Name,
                    description  = f.Description ?? "",
                    input_schema = BuildInputSchema(f)
                }).ToList();
            }
            catch { return null; }
        }

        private static object BuildInputSchema(KernelFunction f)
        {
            var props    = new Dictionary<string, object>();
            var required = new List<string>();
            foreach (var p in f.Metadata.Parameters)
            {
                var schema = p.Schema?.ToString() ?? "{\"type\":\"string\"}";
                try { props[p.Name] = JObject.Parse(schema); }
                catch { props[p.Name] = new { type = "string" }; }
                if (p.IsRequired) required.Add(p.Name);
            }
            return new { type = "object", properties = props, required };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Response parser
        // ─────────────────────────────────────────────────────────────────────

        private static IReadOnlyList<ChatMessageContent> ParseAnthropicResponse(string raw)
        {
            var j     = JObject.Parse(raw);
            var items = new ChatMessageContentItemCollection();

            foreach (var block in j["content"] as JArray ?? new JArray())
            {
                var type = block["type"]?.ToString();
                if (type == "text")
                {
                    items.Add(new TextContent(block["text"]?.ToString() ?? ""));
                }
                else if (type == "tool_use")
                {
                    var name   = block["name"]?.ToString() ?? "";
                    var callId = block["id"]?.ToString() ?? Guid.NewGuid().ToString("N");
                    var inputJ = block["input"] as JObject ?? new JObject();
                    var kargs  = new KernelArguments();
                    foreach (var prop in inputJ.Properties())
                        kargs[prop.Name] = prop.Value?.Type == JTokenType.String
                            ? (object?)prop.Value.ToString()
                            : prop.Value?.ToString() ?? "";
                    items.Add(new FunctionCallContent(name, null, callId, kargs));
                }
            }

            return new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, items: items)
            };
        }
    }
}
