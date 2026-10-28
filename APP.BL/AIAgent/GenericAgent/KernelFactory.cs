#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0070, SKEXP0110
using System;
using System.Net.Http;
using App.BL.DbGenie;
using App.BL.GenericAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Builds the Semantic Kernel (and bare chat service) for the tenant's configured LLM provider.
    /// Shared by the agent loop and by background jobs, which pass an explicit identity instead of ServerContext.
    /// </summary>
    internal static class KernelFactory
    {
        public static Kernel Build(AppClientIdentity? identity)
        {
            EmLLMProvider provider;
            string        apiKey;
            string        model;

            if (identity.HasValue)
            {
                var providerStr = AIConfigSettingBL.GetProvider(identity.Value) ?? "";
                provider = Enum.TryParse<EmLLMProvider>(providerStr, true, out var parsed) ? parsed : EmLLMProvider.Gemini;
                apiKey   = AIConfigSettingBL.GetApiKey(identity.Value) ?? "";
                model    = AIConfigSettingBL.GetModel(identity.Value)  ?? "";
            }
            else
            {
                provider = KernelProviderHelper.GetProvider();
                apiKey   = KernelProviderHelper.GetApiKey() ?? "";
                model    = KernelProviderHelper.GetModel()  ?? "";
            }

            var builder = Kernel.CreateBuilder();
            switch (provider)
            {
                case EmLLMProvider.Anthropic:
                    // No official SK connector package for Anthropic — use a custom wrapper.
                    builder.Services.AddSingleton<IChatCompletionService>(new AnthropicChatCompletionService(model, apiKey));
                    break;
                case EmLLMProvider.Gemini:
                    // Bound hang risk: default HttpClient timeout is 100s and can stall forever on
                    // some streaming paths. Keep a hard ceiling; CancelRun still cancels via CT.
                    var geminiHttp = new HttpClient(new SharedAgentEngine.GeminiRoleFixHandler())
                    {
                        Timeout = TimeSpan.FromMinutes(10)
                    };
                    builder.AddGoogleAIGeminiChatCompletion(model, apiKey, httpClient: geminiHttp);
                    break;
                default:
                    builder.AddOpenAIChatCompletion(model, apiKey);
                    break;
            }
            return builder.Build();
        }

        public static IChatCompletionService CreateChatService(AppClientIdentity identity)
            => Build(identity).GetRequiredService<IChatCompletionService>();
    }
}
