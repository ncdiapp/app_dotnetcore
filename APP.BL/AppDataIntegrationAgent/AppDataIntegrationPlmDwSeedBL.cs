using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppDataIntegrationAgent
{
    /// <summary>
    /// Seeds official ImportFromPLMDW generator/templates into the session workspace
    /// and validates Phase B deliverables (size + blueprintFields).
    /// </summary>
    public static class AppDataIntegrationPlmDwSeedBL
    {
        public const string SkillKey = "plm-dw";

        private static readonly string[] SeedRelativePaths =
        {
            "ImportFromPLMDW/source/_gen_plmdw_import_sql.ps1",
            "ImportFromPLMDW/source/_gen_plmdw_bom_colorway.ps1",
            "ImportFromPLMDW/source/_gen_tchp_import_sql.ps1",
            "ImportFromPLMDW/source/_gen_simple_qc.ps1",
            "ImportFromPLMDW/source/PlmDw_ImportFromDW.sql",
            "ImportFromPLMDW/source/PlmDw_ImportBomColorwayGrandchild.sql",
            "ImportFromPLMDW/source/PlmDw_CleanupBomColorwayStaging.sql",
            "ImportFromPLMDW/source/dwTabImportConfig.example.json",
            "ImportFromPLMDW/source/bomColorwayImportConfig.example.json",
            "ImportFromPLMDW/source/_plm_probe_template.sql",
            "ImportFromPLMDW/source/_dw_probe_by_tabids.sql"
        };

        /// <summary>Minimum SizeBytes for known Phase B files under output/{templateId}/.</summary>
        private static readonly Dictionary<string, long> MinSizeByFileName =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                { "1_PlmDw_Tables.sql", 400 * 1024L },
                { "2_PlmDw_FieldMapping.sql", 100 * 1024L },
                { "3_PlmDw_ImportFromDW.sql", 12 * 1024L },
                { "4_PlmDw_ImportBlueprint.json", 500 * 1024L },
                { "5_PlmDw_ImportBomColorwayGrandchild.sql", 40 * 1024L },
                { "6_PlmDw_CleanupBomColorwayStaging.sql", 8 * 1024L }
            };

        public static bool IsPlmDwSkill(string skillKey)
        {
            return string.Equals(Normalize(skillKey), SkillKey, StringComparison.OrdinalIgnoreCase);
        }

        public static int SeedOfficialSourceFiles(AppDataIntegrationAgentSessionStore.SessionData live)
        {
            if (live == null || !IsPlmDwSkill(live.SkillKey)) return 0;
            var root = AppDataIntegrationAgentSkillCatalogBL.FindImportDocRoot();
            if (string.IsNullOrWhiteSpace(root)) return 0;

            var count = 0;
            foreach (var importRel in SeedRelativePaths)
            {
                var full = Path.GetFullPath(Path.Combine(root, importRel.Replace('/', Path.DirectorySeparatorChar)));
                if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!File.Exists(full)) continue;

                // Workspace path: source/<filename> (flat under session source/)
                var fileName = Path.GetFileName(full);
                var destRel = "source/" + fileName;
                var bytes = File.ReadAllBytes(full);
                AppDataIntegrationWorkspaceBL.WriteBytesFromArtifact(
                    live.WorkspaceRelativePath, destRel, bytes, live.CompanyId);
                count++;
                AppDataIntegrationAgentSessionStore.Enqueue(live.SessionId, new AppDataIntegrationAgentEventDto
                {
                    EventType = "file",
                    File = new AppDataIntegrationAgentFileEvent { Action = "seed", RelativePath = destRel }
                });
            }
            return count;
        }

        public class DeliverableValidation
        {
            public bool Ok { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<object> FilesChecked { get; set; } = new List<object>();
        }

        public static DeliverableValidation ValidatePhaseBDeliverables(
            AppDataIntegrationAgentSessionStore.SessionData live)
        {
            var result = new DeliverableValidation { Ok = true };
            if (live == null || !IsPlmDwSkill(live.SkillKey))
                return result;

            var files = AppDataIntegrationWorkspaceBL.ListFiles(live.WorkspaceRelativePath, live.CompanyId)
                ?? new List<AppDataIntegrationAgentWorkspaceFileDto>();
            var byName = files
                .Where(f => f != null && !f.IsDirectory)
                .GroupBy(f => Path.GetFileName(f.RelativePath ?? ""), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.SizeBytes).First(), StringComparer.OrdinalIgnoreCase);

            // Prefer output/*/1_PlmDw_Tables.sql if present
            string templateId = null;
            foreach (var f in files.Where(x => !x.IsDirectory))
            {
                var p = (f.RelativePath ?? "").Replace('\\', '/');
                var m = System.Text.RegularExpressions.Regex.Match(p, @"output/(\d+)/1_PlmDw_Tables\.sql$",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) { templateId = m.Groups[1].Value; break; }
            }

            foreach (var kv in MinSizeByFileName)
            {
                AppDataIntegrationAgentWorkspaceFileDto hit = null;
                if (templateId != null)
                {
                    var expect = "output/" + templateId + "/" + kv.Key;
                    hit = files.FirstOrDefault(f =>
                        !f.IsDirectory
                        && string.Equals((f.RelativePath ?? "").Replace('\\', '/'), expect, StringComparison.OrdinalIgnoreCase));
                }
                if (hit == null)
                    byName.TryGetValue(kv.Key, out hit);

                // Steps 5/6 optional unless present or BOM bindings exist
                var optional = kv.Key.StartsWith("5_", StringComparison.OrdinalIgnoreCase)
                    || kv.Key.StartsWith("6_", StringComparison.OrdinalIgnoreCase);

                if (hit == null)
                {
                    if (!optional)
                    {
                        result.Ok = false;
                        result.Errors.Add("Missing deliverable: " + kv.Key);
                    }
                    continue;
                }

                result.FilesChecked.Add(new { hit.RelativePath, hit.SizeBytes, minRequired = kv.Value });
                if (hit.SizeBytes < kv.Value)
                {
                    if (optional && hit.SizeBytes < 1024)
                    {
                        // tiny stub for optional step → fail
                        result.Ok = false;
                        result.Errors.Add(hit.RelativePath + " looks like a stub (" + hit.SizeBytes
                            + " bytes; need ≥ " + kv.Value + "). Expand full BOM/cleanup SQL from official templates.");
                    }
                    else if (!optional)
                    {
                        result.Ok = false;
                        result.Errors.Add(hit.RelativePath + " is too small (" + hit.SizeBytes
                            + " bytes; need ≥ " + kv.Value + "). Official generator output required — not a short custom script.");
                    }
                }

                if (kv.Key.Equals("4_PlmDw_ImportBlueprint.json", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var content = AppDataIntegrationWorkspaceBL.ReadFile(
                            live.WorkspaceRelativePath, hit.RelativePath, live.CompanyId);
                        var jo = JObject.Parse(content.Content ?? "{}");
                        var fields = jo["blueprintFields"] as JArray;
                        var n = fields?.Count ?? 0;
                        if (n < 200)
                        {
                            result.Ok = false;
                            result.Errors.Add(hit.RelativePath
                                + " missing full blueprintFields (found " + n
                                + "; need hundreds/thousands from official generator).");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Ok = false;
                        result.Errors.Add("Could not parse blueprint: " + ex.Message);
                    }
                }

                if (kv.Key.Equals("1_PlmDw_Tables.sql", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var content = AppDataIntegrationWorkspaceBL.ReadFile(
                            live.WorkspaceRelativePath, hit.RelativePath, live.CompanyId);
                        var text = content.Content ?? "";
                        var alterCount = CountIgnoreCase(text, "ALTER TABLE");
                        var hasPrefix = text.IndexOf("DECLARE @TablePrefix", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (alterCount < 50 && !hasPrefix)
                        {
                            result.Ok = false;
                            result.Errors.Add(hit.RelativePath
                                + " lacks official DDL structure (ALTER TABLE/FK or @TablePrefix dynamic SQL). Do not ship CREATE-TABLE-only shells.");
                        }
                    }
                    catch { /* size check already applied */ }
                }
            }

            // Seeded generators must still be present for this skill
            var gen = files.FirstOrDefault(f =>
                !f.IsDirectory
                && (f.RelativePath ?? "").Replace('\\', '/').EndsWith("source/_gen_plmdw_import_sql.ps1", StringComparison.OrdinalIgnoreCase));
            if (gen == null)
                result.Errors.Add("Official generator source/_gen_plmdw_import_sql.ps1 is missing from workspace (seed failed).");

            if (result.Errors.Count > 0)
                result.Ok = false;
            return result;
        }

        public static string FormatValidationNotice(DeliverableValidation v)
        {
            if (v == null || v.Ok) return null;
            var sb = new StringBuilder();
            sb.AppendLine("Phase B deliverables look incomplete (official ImportFromPLMDW generator required):");
            foreach (var e in v.Errors.Take(12))
                sb.AppendLine("- " + e);
            sb.AppendLine("Use workspace source/_gen_plmdw_import_sql.ps1 (and templates). Do not use a temporary gen_plmdw_*.py as the final producer. Probe PLM/DW via MCP run_select (App DataSources) — the Cursor VM has no tenant SQL Server.");
            return sb.ToString();
        }

        private static string Normalize(string key)
        {
            return (key ?? "").Trim();
        }

        private static int CountIgnoreCase(string text, string needle)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;
            var n = 0;
            var i = 0;
            while (i < text.Length)
            {
                var j = text.IndexOf(needle, i, StringComparison.OrdinalIgnoreCase);
                if (j < 0) break;
                n++;
                i = j + needle.Length;
            }
            return n;
        }
    }
}
