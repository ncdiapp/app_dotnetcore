using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using App.BL.AppMgr.AiSkill;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.CursorAgent
{
    public static class CursorAgentSkillCatalogBL
    {
        public const string DefaultKey = "app-config-builder";
        public const string GeneralKey = "general";
        public const string SavedPrefix = "saved:";

        private static readonly string[] HiddenSavedNames =
        {
            CursorAgentSkillBL.Policy,
            CursorAgentSkillBL.ImportPack,
            CursorAgentSkillBL.GatedSql,
            CursorAgentSkillBL.Workspace,
            "DbGenie.SqlSkill"
        };

        public static string FindImportDocRoot()
        {
            var starts = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory,
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."))
            };
            foreach (var start in starts.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(start)) continue;
                var candidates = new[]
                {
                    Path.Combine(start, "AppReact", "ImportDoc"),
                    Path.Combine(start, "ImportDoc")
                };
                foreach (var dir in candidates)
                {
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "cursor-agent-skills.json")))
                        return dir;
                }
            }
            return null;
        }

        public static CursorAgentSkillMenuDto ListMenu()
        {
            var menu = new CursorAgentSkillMenuDto { DefaultKey = DefaultKey };
            var catalog = LoadCatalog();
            foreach (var skill in catalog.Skills ?? new List<CatalogSkill>())
            {
                menu.Items.Add(new CursorAgentSkillMenuItemDto
                {
                    Key = skill.Id,
                    Label = skill.Label,
                    Group = skill.Group,
                    GroupLabel = skill.GroupLabel
                });
            }

            var ds = AppAISkillBL.GetDefaultDataSourceId();
            if (!ds.HasValue) return menu;
            try
            {
                foreach (var skill in AppAISkillBL.GetAllSkills(ds.Value) ?? new List<AppAISkillDto>())
                {
                    if (skill == null || !skill.IsActive) continue;
                    if (IsHiddenSavedName(skill.Name)) continue;
                    menu.Items.Add(new CursorAgentSkillMenuItemDto
                    {
                        Key = SavedPrefix + skill.SkillId,
                        Label = skill.Name,
                        Group = "saved",
                        GroupLabel = "Saved skills"
                    });
                }
            }
            catch { }
            return menu;
        }

        public static void ApplyToSession(CursorAgentSessionStore.SessionData live, string skillKey)
        {
            if (live == null) return;
            live.SkillKey = NormalizeKey(skillKey);
            live.AllowProposeImport = AllowsProposeImport(live.SkillKey);
        }

        public static string NormalizeKey(string skillKey)
        {
            if (string.IsNullOrWhiteSpace(skillKey)) return DefaultKey;
            var key = skillKey.Trim();
            if (key.StartsWith(SavedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                int id;
                if (int.TryParse(key.Substring(SavedPrefix.Length), out id) && id > 0)
                    return SavedPrefix + id;
                return DefaultKey;
            }
            var catalog = LoadCatalog();
            if (catalog.Skills != null && catalog.Skills.Any(s => string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase)))
                return catalog.Skills.First(s => string.Equals(s.Id, key, StringComparison.OrdinalIgnoreCase)).Id;
            return DefaultKey;
        }

        public static bool AllowsProposeImport(string skillKey)
        {
            var key = NormalizeKey(skillKey);
            if (key.StartsWith(SavedPrefix, StringComparison.OrdinalIgnoreCase))
                return true;
            var skill = FindCatalog(key);
            return skill != null && skill.AllowProposeImport;
        }

        public static string BuildInjectedPrompt(CursorAgentSessionStore.SessionData live, string userMessage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the AppAI Cursor Agent.");
            sb.AppendLine();
            sb.AppendLine("## This session");
            sb.AppendLine("- Target Application (SaasApplicationId): " + (live?.SaasApplicationId ?? 0));
            if (live?.DataSourceRegisterId != null)
                sb.AppendLine("- Default DataSourceRegisterId: " + live.DataSourceRegisterId.Value);
            sb.AppendLine("- Use MCP tools on server \"appai\" for schema, files, and SQL.");
            sb.AppendLine("- Writable disk is the MCP workspace (packs/, scripts/, output/, notes/).");
            sb.AppendLine("- Do not modify cloned application source (.cs/.tsx). Do not open a pull request.");
            sb.AppendLine("- SELECT may run via run_select. INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD must go through propose_sql and wait.");
            if (live != null && !live.AllowProposeImport)
                sb.AppendLine("- propose_import_pack is disabled for this skill. Do not call it.");
            sb.AppendLine();

            var key = NormalizeKey(live?.SkillKey);
            if (key.StartsWith(SavedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                int id;
                int.TryParse(key.Substring(SavedPrefix.Length), out id);
                sb.AppendLine("## Selected skill (Saved)");
                var composed = LoadSavedComposed(id);
                if (string.IsNullOrWhiteSpace(composed))
                    sb.AppendLine("Saved skill was not found or is inactive.");
                else
                    sb.AppendLine(composed);
            }
            else
            {
                var skill = FindCatalog(key) ?? FindCatalog(DefaultKey);
                if (skill != null)
                {
                    sb.AppendLine("## Selected skill: " + skill.Label);
                    if (!string.IsNullOrWhiteSpace(skill.ExtraPrompt))
                    {
                        sb.AppendLine(skill.ExtraPrompt);
                        sb.AppendLine();
                    }
                    AppendInjectFiles(sb, skill);
                }
            }

            sb.AppendLine();
            sb.AppendLine("## User request");
            sb.AppendLine(userMessage ?? "");
            return sb.ToString();
        }

        private static void AppendInjectFiles(StringBuilder sb, CatalogSkill skill)
        {
            var root = FindImportDocRoot();
            if (string.IsNullOrWhiteSpace(root) || skill.InjectFiles == null) return;
            var examples = new HashSet<string>(
                skill.ExampleFiles ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var rel in skill.InjectFiles)
            {
                if (string.IsNullOrWhiteSpace(rel)) continue;
                var full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar)));
                if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!File.Exists(full))
                {
                    sb.AppendLine();
                    sb.AppendLine("### Missing file: " + rel);
                    continue;
                }
                var isExample = examples.Contains(rel);
                sb.AppendLine();
                sb.AppendLine(isExample
                    ? "### EXAMPLE FILE (do not copy IDs): " + rel
                    : "### " + rel);
                sb.AppendLine();
                sb.AppendLine(File.ReadAllText(full));
            }
        }

        private static string LoadSavedComposed(int skillId)
        {
            var ds = AppAISkillBL.GetDefaultDataSourceId();
            if (!ds.HasValue || skillId <= 0) return null;
            var skill = AppAISkillBL.GetSkillById(ds.Value, skillId);
            if (skill == null || !skill.IsActive || IsHiddenSavedName(skill.Name))
                return null;
            return AppAISkillBL.GetComposedSkillPrompt(ds.Value, skillId);
        }

        private static bool IsHiddenSavedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            if (name.StartsWith("cursor-agent-", StringComparison.OrdinalIgnoreCase)) return true;
            return HiddenSavedNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        }

        private static CatalogSkill FindCatalog(string id)
        {
            var catalog = LoadCatalog();
            return catalog.Skills?.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private static CatalogFile LoadCatalog()
        {
            var root = FindImportDocRoot();
            if (string.IsNullOrWhiteSpace(root))
                return BuiltInCatalog();
            var path = Path.Combine(root, "cursor-agent-skills.json");
            try
            {
                var json = File.ReadAllText(path);
                var catalog = JsonConvert.DeserializeObject<CatalogFile>(json);
                if (catalog?.Skills != null && catalog.Skills.Count > 0)
                    return catalog;
            }
            catch { }
            return BuiltInCatalog();
        }

        private static CatalogFile BuiltInCatalog()
        {
            return new CatalogFile
            {
                DefaultKey = DefaultKey,
                Skills = new List<CatalogSkill>
                {
                    new CatalogSkill { Id = GeneralKey, Group = "general", GroupLabel = "General", Label = "General", AllowProposeImport = false },
                    new CatalogSkill { Id = DefaultKey, Group = "app", GroupLabel = "App Config Builder", Label = "App Config Builder", AllowProposeImport = false }
                }
            };
        }

        private sealed class CatalogFile
        {
            [JsonProperty("defaultKey")]
            public string DefaultKey { get; set; }
            [JsonProperty("skills")]
            public List<CatalogSkill> Skills { get; set; }
        }

        private sealed class CatalogSkill
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("group")]
            public string Group { get; set; }
            [JsonProperty("groupLabel")]
            public string GroupLabel { get; set; }
            [JsonProperty("label")]
            public string Label { get; set; }
            [JsonProperty("allowProposeImport")]
            public bool AllowProposeImport { get; set; }
            [JsonProperty("injectFiles")]
            public List<string> InjectFiles { get; set; }
            [JsonProperty("exampleFiles")]
            public List<string> ExampleFiles { get; set; }
            [JsonProperty("extraPrompt")]
            public string ExtraPrompt { get; set; }
        }
    }
}
