using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APP.BL.AppConfigPack;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tools: teach App Config Pack JSON contract and validate/preview/execute packs.
    /// ToolConfig example: {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AppConfigPackPlugin","MethodName":"GetAppConfigPackContract"}
    /// </summary>
    public class AppConfigPackPlugin
    {
        private static readonly object ContractLock = new object();
        private static string _cachedFullContract;

        /// <summary>
        /// Return the fixed App Config Pack JSON contract (from ImportAppConfig/PROMPT.md).
        /// Optional section: overview | tables | transactions | searches | listedit | samples | pipeline | all
        /// </summary>
        public Task<string> GetAppConfigPackContract(CancellationToken ct, string section = null)
        {
            ct.ThrowIfCancellationRequested();
            string full = LoadContractText();
            string key = (section ?? "all").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key) || key == "all")
                return Task.FromResult(full);

            string excerpt = ExtractMarkdownSection(full, key);
            if (string.IsNullOrWhiteSpace(excerpt))
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    Error = $"Unknown section '{section}'. Use: overview, tables, transactions, searches, listedit, samples, pipeline, all.",
                    Hint = "Call again with section=all or omit section."
                }));
            }

            return Task.FromResult(excerpt);
        }

        public Task<string> ValidateAppConfigPack(CancellationToken ct, string packJson)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(packJson))
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "packJson is required." }));

            var load = AppConfigPackBL.Load(new AppConfigPackLoadRequestDto { PackJson = packJson });
            if (load.Object == null)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    IsValid = false,
                    Errors = load.ValidationResult?.Items?.Select(i => i.Message).ToList()
                        ?? new List<string> { "PackJson could not be loaded." }
                }));
            }

            var validation = AppConfigPackBL.Validate(load.Object);
            return Task.FromResult(JsonConvert.SerializeObject(validation.Object ?? new AppConfigPackValidationDto
            {
                IsValid = false,
                Errors = new List<string> { "Validate returned no object." }
            }));
        }

        public Task<string> PreviewAppConfigPack(CancellationToken ct, string packJson, int? saasApplicationId = null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(packJson))
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "packJson is required." }));

            var load = AppConfigPackBL.Load(new AppConfigPackLoadRequestDto { PackJson = packJson });
            if (load.Object == null)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    ErrorMessage = load.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "PackJson could not be loaded."
                }));
            }

            var preview = AppConfigPackBL.Preview(new AppConfigPackExecuteRequestDto
            {
                Pack = load.Object,
                SaasApplicationId = saasApplicationId
            });
            return Task.FromResult(JsonConvert.SerializeObject(preview.Object));
        }

        public Task<string> ExecuteAppConfigPack(CancellationToken ct, string packJson, int? saasApplicationId = null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(packJson))
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "packJson is required." }));

            var load = AppConfigPackBL.Load(new AppConfigPackLoadRequestDto { PackJson = packJson });
            if (load.Object == null)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    ErrorMessage = load.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "PackJson could not be loaded."
                }));
            }

            var exec = AppConfigPackBL.Execute(new AppConfigPackExecuteRequestDto
            {
                Pack = load.Object,
                SaasApplicationId = saasApplicationId
            });
            return Task.FromResult(JsonConvert.SerializeObject(exec.Object));
        }

        private static string LoadContractText()
        {
            if (!string.IsNullOrEmpty(_cachedFullContract))
                return _cachedFullContract;

            lock (ContractLock)
            {
                if (!string.IsNullOrEmpty(_cachedFullContract))
                    return _cachedFullContract;

                foreach (var path in CandidateContractPaths())
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            _cachedFullContract = File.ReadAllText(path, Encoding.UTF8);
                            return _cachedFullContract;
                        }
                    }
                    catch
                    {
                        // try next
                    }
                }

                _cachedFullContract = BuiltInContractFallback;
                return _cachedFullContract;
            }
        }

        private static IEnumerable<string> CandidateContractPaths()
        {
            string cwd = Directory.GetCurrentDirectory();
            yield return Path.Combine(cwd, "AppReact", "ImportDoc", "ImportAppConfig", "PROMPT.md");
            yield return Path.Combine(cwd, "..", "AppReact", "ImportDoc", "ImportAppConfig", "PROMPT.md");
            yield return Path.Combine(cwd, "ImportDoc", "ImportAppConfig", "PROMPT.md");
            yield return Path.Combine(cwd, "wwwroot", "ImportDoc", "ImportAppConfig", "PROMPT.md");
            // Dev: repo root relative to APP.BL bin
            yield return Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", "..", "AppReact", "ImportDoc", "ImportAppConfig", "PROMPT.md"));
        }

        private static string ExtractMarkdownSection(string markdown, string sectionKey)
        {
            // Map aliases to heading substrings
            string needle = sectionKey switch
            {
                "overview" or "top" => "## Top-level shape",
                "pipeline" => "## Pipeline",
                "tables" => "## Tables",
                "transactions" or "tx" => "## Screen patterns",
                "searches" or "search" => "## Searches",
                "listedit" or "list" => "ListEdit",
                "samples" or "sample" => "```json",
                _ => null
            };

            if (needle == null)
                return null;

            if (sectionKey is "listedit" or "list")
            {
                // Return screen patterns section which contains ListEdit guidance
                needle = "## Screen patterns";
            }

            int idx = markdown.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            // From this heading to next ## (or EOF for samples: take first json fence after)
            if (sectionKey is "samples" or "sample")
            {
                int fence = markdown.IndexOf("```json", idx, StringComparison.OrdinalIgnoreCase);
                if (fence < 0) return null;
                int endFence = markdown.IndexOf("```", fence + 6, StringComparison.Ordinal);
                if (endFence < 0) return markdown.Substring(fence);
                return markdown.Substring(fence, endFence + 3 - fence);
            }

            int next = markdown.IndexOf("\n## ", idx + needle.Length, StringComparison.Ordinal);
            if (next < 0)
                return markdown.Substring(idx);
            return markdown.Substring(idx, next - idx);
        }

        private const string BuiltInContractFallback = @"# App Config Pack (fallback summary)

Full contract file PROMPT.md was not found on disk. Key rules:

- schemaVersion, tables[], views[], simpleListEntities[], transactions[], transactionGroup, searches[]
- Stable key: integrationId on each transaction and search (never numeric TransactionId/SearchId)
- Pattern A: Search + MasterDetail. Pattern B: ListEdit (organizedType List) with transactions[].menu — no Search
- Pipeline: Validate → Preview → Execute via validate_app_config_pack / preview_app_config_pack / execute_app_config_pack
- Never DROP tables/columns; never pass connection strings

Place AppReact/ImportDoc/ImportAppConfig/PROMPT.md where the web host can read it for the full contract.
";
    }
}
