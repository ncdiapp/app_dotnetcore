using System;
using System.Collections.Generic;
using System.Configuration;
using APP.Framework;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.DbGenie
{
    /// <summary>
    /// Helper class for making API calls to various LLM providers
    /// </summary>
    public static class LLMProviderHelper
    {
        // Default models for each provider — read from config, fall back to hardcoded values
        public static string OpenAIDefaultModel => AppConfig.Get("DbaGenieOpenAIModel") ?? "gpt-4o";
        public static string GeminiDefaultModel => AppConfig.Get("DbaGenieGeminiModel") ?? "gemini-1.5-pro";
        public static string AnthropicDefaultModel => AppConfig.Get("DbaGenieAnthropicModel") ?? "claude-3-5-sonnet-20241022";

        // API endpoints
        private static readonly string OpenAIBaseUrl = "https://api.openai.com/v1";
        private static readonly string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
        private static readonly string AnthropicBaseUrl = "https://api.anthropic.com/v1";

        /// <summary>
        /// Calls the appropriate LLM provider based on the request
        /// </summary>
        public static async Task<LLMResponseDto> CallLLMAsync(LLMRequestDto request)
        {
            if (request == null)
            {
                return new LLMResponseDto
                {
                    IsSuccess = false,
                    Error = "Request cannot be null"
                };
            }

            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return new LLMResponseDto
                {
                    IsSuccess = false,
                    Error = "API key is required"
                };
            }

            try
            {
                switch (request.Provider)
                {
                    case EmLLMProvider.OpenAI:
                        return await CallOpenAIAsync(request);

                    case EmLLMProvider.Gemini:
                        return await CallGeminiAsync(request);

                    case EmLLMProvider.Anthropic:
                        return await CallAnthropicAsync(request);

                    default:
                        return new LLMResponseDto
                        {
                            IsSuccess = false,
                            Error = $"Unsupported LLM provider: {request.Provider}"
                        };
                }
            }
            catch (Exception ex)
            {
                return new LLMResponseDto
                {
                    IsSuccess = false,
                    Error = $"Error calling LLM API: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Calls OpenAI Chat Completions API
        /// </summary>
        private static async Task<LLMResponseDto> CallOpenAIAsync(LLMRequestDto request)
        {
            string model = string.IsNullOrWhiteSpace(request.Model) ? OpenAIDefaultModel : request.Model;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                var messages = new List<object>();

                if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                {
                    messages.Add(new { role = "system", content = request.SystemPrompt });
                }

                messages.Add(new { role = "user", content = request.Prompt });

                var requestBody = new
                {
                    model = model,
                    messages = messages,
                    temperature = request.Temperature ?? 0.7,
                    max_tokens = request.MaxTokens ?? 4096
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{OpenAIBaseUrl}/chat/completions", content).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new LLMResponseDto
                    {
                        IsSuccess = false,
                        Error = $"OpenAI API error: {responseString}",
                        Model = model
                    };
                }

                var responseJson = JObject.Parse(responseString);
                var messageContent = responseJson["choices"]?[0]?["message"]?["content"]?.ToString();
                var tokensUsed = responseJson["usage"]?["total_tokens"]?.Value<int>();

                return new LLMResponseDto
                {
                    IsSuccess = true,
                    Content = messageContent,
                    TokensUsed = tokensUsed,
                    Model = model
                };
            }
        }

        /// <summary>
        /// Calls Google Gemini API
        /// </summary>
        private static async Task<LLMResponseDto> CallGeminiAsync(LLMRequestDto request)
        {
            string model = string.IsNullOrWhiteSpace(request.Model) ? GeminiDefaultModel : request.Model;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                var contents = new List<object>();

                if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                {
                    contents.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = request.SystemPrompt } }
                    });
                    contents.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = "I understand. I will follow these instructions." } }
                    });
                }

                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = request.Prompt } }
                });

                var requestBody = new
                {
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = request.Temperature ?? 0.7,
                        maxOutputTokens = request.MaxTokens ?? 4096
                    }
                };

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var url = $"{GeminiBaseUrl}/models/{model}:generateContent?key={request.ApiKey}";
                var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new LLMResponseDto
                    {
                        IsSuccess = false,
                        Error = $"Gemini API error: {responseString}",
                        Model = model
                    };
                }

                var responseJson = JObject.Parse(responseString);
                var messageContent = responseJson["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                var tokensUsed = responseJson["usageMetadata"]?["totalTokenCount"]?.Value<int>();

                return new LLMResponseDto
                {
                    IsSuccess = true,
                    Content = messageContent,
                    TokensUsed = tokensUsed,
                    Model = model
                };
            }
        }

        /// <summary>
        /// Calls Anthropic Claude API
        /// </summary>
        private static async Task<LLMResponseDto> CallAnthropicAsync(LLMRequestDto request)
        {
            string model = string.IsNullOrWhiteSpace(request.Model) ? AnthropicDefaultModel : request.Model;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("x-api-key", request.ApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.Timeout = TimeSpan.FromMinutes(5);

                var messages = new List<object>
                {
                    new { role = "user", content = request.Prompt }
                };

                var requestBody = new Dictionary<string, object>
                {
                    { "model", model },
                    { "max_tokens", request.MaxTokens ?? 4096 },
                    { "messages", messages }
                };

                if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                {
                    requestBody["system"] = request.SystemPrompt;
                }

                if (request.Temperature.HasValue)
                {
                    requestBody["temperature"] = request.Temperature.Value;
                }

                var jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{AnthropicBaseUrl}/messages", content).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new LLMResponseDto
                    {
                        IsSuccess = false,
                        Error = $"Anthropic API error: {responseString}",
                        Model = model
                    };
                }

                var responseJson = JObject.Parse(responseString);
                var messageContent = responseJson["content"]?[0]?["text"]?.ToString();
                var inputTokens = responseJson["usage"]?["input_tokens"]?.Value<int>() ?? 0;
                var outputTokens = responseJson["usage"]?["output_tokens"]?.Value<int>() ?? 0;

                return new LLMResponseDto
                {
                    IsSuccess = true,
                    Content = messageContent,
                    TokensUsed = inputTokens + outputTokens,
                    Model = model
                };
            }
        }

        /// <summary>
        /// Validates an API key by making a simple test request
        /// </summary>
        public static async Task<bool> ValidateApiKeyAsync(EmLLMProvider provider, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            var testRequest = new LLMRequestDto
            {
                Provider = provider,
                ApiKey = apiKey,
                Prompt = "Say 'OK' and nothing else.",
                MaxTokens = 10
            };

            try
            {
                var response = await CallLLMAsync(testRequest).ConfigureAwait(false);
                return response.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the configured LLM provider from web.config (DbaGenieLLMProvider key).
        /// Returns OpenAI as default if not configured.
        /// </summary>
        public static EmLLMProvider GetConfiguredProvider()
        {
            var providerName = AppConfig.Get("DbaGenieLLMProvider") ?? "OpenAI";
            if (Enum.TryParse(providerName, true, out EmLLMProvider provider))
                return provider;
            return EmLLMProvider.OpenAI;
        }

        /// <summary>
        /// Gets the configured API key from web.config (DbaGenieApiKey key).
        /// </summary>
        public static string GetConfiguredApiKey()
        {
            return AppConfig.Get("DbaGenieApiKey") ?? string.Empty;
        }

        /// <summary>
        /// Gets list of supported LLM providers with their info
        /// </summary>
        public static List<LLMProviderInfoDto> GetLLMProviders()
        {
            return new List<LLMProviderInfoDto>
            {
                new LLMProviderInfoDto
                {
                    Provider = EmLLMProvider.OpenAI,
                    Name = "OpenAI",
                    Description = "OpenAI GPT models including GPT-4",
                    DefaultModel = OpenAIDefaultModel,
                    SupportedModels = new List<string> { "gpt-4o", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo" }
                },
                new LLMProviderInfoDto
                {
                    Provider = EmLLMProvider.Gemini,
                    Name = "Google Gemini",
                    Description = "Google's Gemini AI models",
                    DefaultModel = GeminiDefaultModel,
                    SupportedModels = new List<string> { "gemini-1.5-pro", "gemini-1.5-flash", "gemini-pro" }
                },
                new LLMProviderInfoDto
                {
                    Provider = EmLLMProvider.Anthropic,
                    Name = "Anthropic Claude",
                    Description = "Anthropic's Claude AI models",
                    DefaultModel = AnthropicDefaultModel,
                    SupportedModels = new List<string> { "claude-3-5-sonnet-20241022", "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" }
                }
            };
        }
    }
}
