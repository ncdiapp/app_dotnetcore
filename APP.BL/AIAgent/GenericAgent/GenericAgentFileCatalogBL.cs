using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Sidecar catalog for Default Source file descriptions:
    /// AgentStarter/{skillKey}/.agent-file-catalog.json
    /// Copied into chat as source/.agent-file-catalog.json with New Chat.
    /// </summary>
    public static class GenericAgentFileCatalogBL
    {
        public const string CatalogFileName = ".agent-file-catalog.json";
        public const int MaxDescriptionChars = 4000;
        public const int MaxPromptSummaryChars = 12000;

        public static bool IsCatalogFileName(string name) =>
            string.Equals(Path.GetFileName(name ?? ""), CatalogFileName, StringComparison.OrdinalIgnoreCase);

        public static AgentFileCatalogDto LoadStarterCatalog(string skillKey, int companyId)
        {
            try
            {
                var full = GenericAgentFileBL.ResolveStarter(skillKey, CatalogFileName, companyId);
                return LoadFromFile(full);
            }
            catch
            {
                return new AgentFileCatalogDto();
            }
        }

        public static AgentFileCatalogDto LoadChatSourceCatalog(string sessionKey, int companyId)
        {
            try
            {
                var full = GenericAgentFileBL.Resolve(sessionKey, "source/" + CatalogFileName, companyId);
                return LoadFromFile(full);
            }
            catch
            {
                return new AgentFileCatalogDto();
            }
        }

        public static void SaveStarterCatalog(string skillKey, int companyId, AgentFileCatalogDto catalog)
        {
            GenericAgentFileBL.EnsureStarterRoot(skillKey, companyId);
            var full = GenericAgentFileBL.ResolveStarter(skillKey, CatalogFileName, companyId);
            catalog ??= new AgentFileCatalogDto();
            catalog.Version = catalog.Version > 0 ? catalog.Version : 1;
            catalog.Files ??= new List<AgentFileCatalogEntryDto>();
            // Drop empty descriptions to keep file small; keep explicit empty? User said optional — omit empty.
            catalog.Files = catalog.Files
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.Path))
                .Select(f => new AgentFileCatalogEntryDto
                {
                    Path = NormalizePath(f.Path),
                    Description = Truncate(f.Description)
                })
                .Where(f => !string.IsNullOrWhiteSpace(f.Description))
                .GroupBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last())
                .OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var json = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            File.WriteAllText(full, json, Encoding.UTF8);
        }

        public static void SetDescription(string skillKey, string relativePath, string description, int companyId)
        {
            var path = NormalizePath(relativePath);
            if (string.IsNullOrWhiteSpace(path) || IsCatalogFileName(path))
                throw new ArgumentException("relativePath is required.");
            if (path.IndexOfAny(new[] { '\\', ':' }) >= 0 || path.Contains(".."))
                throw new ArgumentException("Invalid relativePath.");

            var catalog = LoadStarterCatalog(skillKey, companyId);
            catalog.Files ??= new List<AgentFileCatalogEntryDto>();
            var desc = Truncate(description);
            var existing = catalog.Files.FirstOrDefault(f =>
                string.Equals(NormalizePath(f.Path), path, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(desc))
            {
                if (existing != null)
                    catalog.Files.Remove(existing);
            }
            else if (existing != null)
            {
                existing.Description = desc;
                existing.Path = path;
            }
            else
            {
                catalog.Files.Add(new AgentFileCatalogEntryDto { Path = path, Description = desc });
            }

            SaveStarterCatalog(skillKey, companyId, catalog);
        }

        /// <summary>
        /// Fill missing/empty descriptions only; never overwrite a non-empty user/official description.
        /// </summary>
        public static int MergeMissingDescriptions(string skillKey, int companyId, IEnumerable<KeyValuePair<string, string>> descriptions)
        {
            if (string.IsNullOrWhiteSpace(skillKey) || companyId <= 0 || descriptions == null)
                return 0;

            var catalog = LoadStarterCatalog(skillKey, companyId);
            catalog.Files ??= new List<AgentFileCatalogEntryDto>();
            var changed = 0;

            foreach (var kv in descriptions)
            {
                var path = NormalizePath(kv.Key);
                var desc = Truncate(kv.Value);
                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(desc) || IsCatalogFileName(path))
                    continue;
                if (path.IndexOfAny(new[] { '\\', ':' }) >= 0 || path.Contains(".."))
                    continue;

                var existing = catalog.Files.FirstOrDefault(f =>
                    string.Equals(NormalizePath(f.Path), path, StringComparison.OrdinalIgnoreCase));
                if (existing != null && !string.IsNullOrWhiteSpace(existing.Description))
                    continue;

                if (existing != null)
                {
                    existing.Description = desc;
                    existing.Path = path;
                }
                else
                {
                    catalog.Files.Add(new AgentFileCatalogEntryDto { Path = path, Description = desc });
                }
                changed++;
            }

            if (changed > 0)
                SaveStarterCatalog(skillKey, companyId, catalog);
            return changed;
        }

        public static void OnRenamed(string skillKey, string oldPath, string newPath, int companyId)
        {
            var from = NormalizePath(oldPath);
            var to = NormalizePath(newPath);
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) return;
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return;

            var catalog = LoadStarterCatalog(skillKey, companyId);
            if (catalog.Files == null || catalog.Files.Count == 0) return;

            var changed = false;
            foreach (var entry in catalog.Files)
            {
                var p = NormalizePath(entry.Path);
                if (string.Equals(p, from, StringComparison.OrdinalIgnoreCase))
                {
                    entry.Path = to;
                    changed = true;
                }
                else if (p.StartsWith(from + "/", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Path = to + p.Substring(from.Length);
                    changed = true;
                }
            }
            if (changed)
                SaveStarterCatalog(skillKey, companyId, catalog);
        }

        public static void OnDeleted(string skillKey, string relativePath, int companyId)
        {
            var path = NormalizePath(relativePath);
            if (string.IsNullOrWhiteSpace(path)) return;

            var catalog = LoadStarterCatalog(skillKey, companyId);
            if (catalog.Files == null || catalog.Files.Count == 0) return;

            var before = catalog.Files.Count;
            catalog.Files = catalog.Files
                .Where(f =>
                {
                    var p = NormalizePath(f.Path);
                    return !string.Equals(p, path, StringComparison.OrdinalIgnoreCase)
                           && !p.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            if (catalog.Files.Count != before)
                SaveStarterCatalog(skillKey, companyId, catalog);
        }

        public static void AttachDescriptions(IList<GenericAgentFileDto> files, AgentFileCatalogDto catalog, string pathPrefixToStrip = null)
        {
            if (files == null || files.Count == 0) return;
            var map = BuildMap(catalog);
            if (map.Count == 0) return;

            foreach (var f in files)
            {
                if (f == null || f.IsDirectory) continue;
                var key = NormalizePath(f.RelativePath);
                if (!string.IsNullOrWhiteSpace(pathPrefixToStrip))
                {
                    var prefix = NormalizePath(pathPrefixToStrip).TrimEnd('/') + "/";
                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        key = key.Substring(prefix.Length);
                }
                if (map.TryGetValue(key, out var desc))
                    f.Description = desc;
            }
        }

        public static string BuildPromptSummary(AgentFileCatalogDto catalog, string pathPrefix = "source/")
        {
            if (catalog?.Files == null || catalog.Files.Count == 0)
                return null;

            var sb = new StringBuilder();
            sb.AppendLine("## Registered source files (path + description)");
            sb.AppendLine("Use these descriptions to choose files under the chat source/ folder. Prefer list_agent_files / file tools with the listed paths.");
            foreach (var f in catalog.Files
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Path) && !string.IsNullOrWhiteSpace(x.Description))
                .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
            {
                var path = NormalizePath(f.Path);
                var display = string.IsNullOrWhiteSpace(pathPrefix)
                    ? path
                    : (pathPrefix.TrimEnd('/') + "/" + path);
                var line = "- `" + display + "` — " + Truncate(f.Description).Replace("\r\n", " ").Replace("\n", " ");
                if (sb.Length + line.Length + 1 > MaxPromptSummaryChars)
                {
                    sb.AppendLine("…(catalog truncated)");
                    break;
                }
                sb.AppendLine(line);
            }
            return sb.ToString().TrimEnd();
        }

        public static string BuildStarterPromptSummary(string skillKey, int companyId) =>
            BuildPromptSummary(LoadStarterCatalog(skillKey, companyId), "source/");

        private static AgentFileCatalogDto LoadFromFile(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
                return new AgentFileCatalogDto();
            var json = File.ReadAllText(fullPath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
                return new AgentFileCatalogDto();
            var catalog = JsonConvert.DeserializeObject<AgentFileCatalogDto>(json) ?? new AgentFileCatalogDto();
            catalog.Files ??= new List<AgentFileCatalogEntryDto>();
            return catalog;
        }

        private static Dictionary<string, string> BuildMap(AgentFileCatalogDto catalog)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (catalog?.Files == null) return map;
            foreach (var f in catalog.Files)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.Path) || string.IsNullOrWhiteSpace(f.Description))
                    continue;
                map[NormalizePath(f.Path)] = Truncate(f.Description);
            }
            return map;
        }

        private static string NormalizePath(string path) =>
            (path ?? "").Replace('\\', '/').Trim().TrimStart('/');

        private static string Truncate(string description)
        {
            var s = description ?? "";
            if (s.Length <= MaxDescriptionChars) return s;
            return s.Substring(0, MaxDescriptionChars);
        }
    }
}
