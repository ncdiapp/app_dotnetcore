using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.BL.AppMgr.AiSkill;
using App.BL.AppReportAgent;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AppDataIntegrationAgent
{
    public static class AppDataIntegrationAgentSkillCatalogBL
    {
        public const string DefaultKey = "app-config-builder";
        public const string GeneralKey = "general";
        public const string SavedPrefix = "saved:";
        public const string NamedPrefix = "named:";
        public const string OtherGroup = "other";
        public const string OtherGroupLabel = "Other skills";
        public const string SqlSkillName = "DbGenie.SqlSkill";
        public const string ReportSkillName = "AppReport.ReportSkill";

        private static readonly string[] HiddenSavedNames =
        {
            AppDataIntegrationAgentSkillBL.Policy,
            AppDataIntegrationAgentSkillBL.ImportPack,
            AppDataIntegrationAgentSkillBL.GatedSql,
            AppDataIntegrationAgentSkillBL.Workspace
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
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "app-data-integration-agent-skills.json")))
                        return dir;
                }
            }
            return null;
        }

        public static AppDataIntegrationAgentSkillMenuDto ListMenu()
        {
            var menu = new AppDataIntegrationAgentSkillMenuDto { DefaultKey = DefaultKey };
            var catalog = LoadCatalog();
            foreach (var skill in catalog.Skills ?? new List<CatalogSkill>())
            {
                menu.Items.Add(new AppDataIntegrationAgentSkillMenuItemDto
                {
                    Key = skill.Id,
                    Label = skill.Label,
                    Group = skill.Group,
                    GroupLabel = skill.GroupLabel
                });
            }

            var ds = AppAISkillBL.GetDefaultDataSourceId();
            var all = new List<AppAISkillDto>();
            if (ds.HasValue)
            {
                try { all = AppAISkillBL.GetAllSkills(ds.Value) ?? new List<AppAISkillDto>(); }
                catch { }
                EnsureNamedSkill(ds.Value, all, SqlSkillName, "SQL Skill (shared with DBA Agent).", AppDbGenieBL.GetComposedSqlSkillPrompt());
                EnsureNamedSkill(ds.Value, all, ReportSkillName, "Report Skill (shared with App Report Agent).", AppReportAgentBL.GetComposedReportSkillPrompt());
                try { all = AppAISkillBL.GetAllSkills(ds.Value) ?? all; }
                catch { }
            }

            var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddOtherSkill(menu, all, SqlSkillName, "SQL Skill", addedNames);
            AddOtherSkill(menu, all, ReportSkillName, "Report Skill", addedNames);

            foreach (var skill in all)
            {
                if (skill == null || !skill.IsActive) continue;
                if (IsHiddenSavedName(skill.Name)) continue;
                if (!addedNames.Add(skill.Name)) continue;
                menu.Items.Add(new AppDataIntegrationAgentSkillMenuItemDto
                {
                    Key = SavedPrefix + skill.SkillId,
                    Label = skill.Name,
                    Group = OtherGroup,
                    GroupLabel = OtherGroupLabel
                });
            }
            return menu;
        }

        private static void EnsureNamedSkill(int dsId, List<AppAISkillDto> all, string name, string description, string content)
        {
            if (all.Any(s => s != null && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                return;
            if (string.IsNullOrWhiteSpace(content)) return;
            try
            {
                AppAISkillBL.CreateSkill(dsId, new AppAISkillDto
                {
                    Name = name,
                    Description = description,
                    SkillContent = content,
                    IsActive = true
                });
            }
            catch { }
        }

        private static void AddOtherSkill(
            AppDataIntegrationAgentSkillMenuDto menu,
            List<AppAISkillDto> all,
            string name,
            string label,
            HashSet<string> addedNames)
        {
            addedNames.Add(name);
            var found = all.FirstOrDefault(s =>
                s != null && s.IsActive && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            menu.Items.Add(new AppDataIntegrationAgentSkillMenuItemDto
            {
                Key = found != null && found.SkillId > 0 ? SavedPrefix + found.SkillId : NamedPrefix + name,
                Label = label,
                Group = OtherGroup,
                GroupLabel = OtherGroupLabel
            });
        }

        public static void ApplyToSession(AppDataIntegrationAgentSessionStore.SessionData live, string skillKey)
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
            if (key.StartsWith(NamedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var name = key.Substring(NamedPrefix.Length).Trim();
                if (IsWellKnownOtherName(name))
                    return NamedPrefix + name;
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
            if (key.StartsWith(NamedPrefix, StringComparison.OrdinalIgnoreCase))
                return false;
            if (key.StartsWith(SavedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                int id;
                int.TryParse(key.Substring(SavedPrefix.Length), out id);
                var ds = AppAISkillBL.GetDefaultDataSourceId();
                if (!ds.HasValue || id <= 0) return false;
                var skill = AppAISkillBL.GetSkillById(ds.Value, id);
                if (skill == null) return false;
                if (IsWellKnownOtherName(skill.Name)) return false;
                return true;
            }
            var catalogSkill = FindCatalog(key);
            return catalogSkill != null && catalogSkill.AllowProposeImport;
        }

        public static string BuildInjectedPrompt(AppDataIntegrationAgentSessionStore.SessionData live, string userMessage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are the AppAI App Data Integration Agent.");
            sb.AppendLine();
            AppendOpenUiCapability(sb);
            AppendDataSourceScopeGuidance(sb, live);
            sb.AppendLine("## This session");
            sb.AppendLine("- Target Application (SaasApplicationId): " + (live?.SaasApplicationId ?? 0));
            if (live?.DataSourceRegisterId != null)
                sb.AppendLine("- Default DataSourceRegisterId: " + live.DataSourceRegisterId.Value);
            sb.AppendLine("- Use MCP tools on server \"appai\" for schema, files, SQL, and opening App UI pages.");
            sb.AppendLine("- Writable disk is the MCP workspace (packs/, scripts/, output/, notes/).");
            sb.AppendLine("- Do not modify cloned application source (.cs/.tsx). Do not open a pull request.");
            sb.AppendLine("- SELECT may run via run_select. INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD must go through propose_sql and wait.");
            if (live != null && !live.AllowProposeImport)
                sb.AppendLine("- propose_import_pack is disabled for this skill. Do not call it.");

            var key = NormalizeKey(live?.SkillKey);
            AppendPlmSkillMismatchGuidance(sb, key, userMessage);
            sb.AppendLine();
            if (key.StartsWith(NamedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var name = key.Substring(NamedPrefix.Length);
                sb.AppendLine("## Selected skill (Other): " + OtherSkillLabel(name));
                var composed = LoadNamedComposed(name);
                if (string.IsNullOrWhiteSpace(composed))
                    sb.AppendLine("Skill was not found.");
                else
                    sb.AppendLine(AdaptOtherSkillForThisAgent(name, composed));
            }
            else if (key.StartsWith(SavedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                int id;
                int.TryParse(key.Substring(SavedPrefix.Length), out id);
                sb.AppendLine("## Selected skill (Other)");
                var composed = LoadSavedComposed(id);
                var skillName = SavedSkillName(id);
                if (string.IsNullOrWhiteSpace(composed))
                    sb.AppendLine("Skill was not found or is inactive.");
                else
                    sb.AppendLine(AdaptOtherSkillForThisAgent(skillName, composed));
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

        /// <summary>
        /// Follow-up turns do not re-send the full create prompt; restate UI open capability so the
        /// model does not refuse with "I cannot open App pages".
        /// </summary>
        public static string BuildFollowUpPrompt(AppDataIntegrationAgentSessionStore.SessionData live, string userMessage)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(live?.SkillKey))
                sb.AppendLine("Active skill: " + live.SkillKey);
            AppendOpenUiCapability(sb);
            AppendDataSourceScopeGuidance(sb, live);
            AppendPlmSkillMismatchGuidance(sb, NormalizeKey(live?.SkillKey), userMessage);
            sb.AppendLine("## User follow-up");
            sb.AppendLine(userMessage ?? "");
            return sb.ToString();
        }

        private static bool IsPlmCatalogSkill(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            return key.Equals("plm-dw", StringComparison.OrdinalIgnoreCase)
                || key.Equals("plm-pom-grading", StringComparison.OrdinalIgnoreCase)
                || key.Equals("plm-search-view", StringComparison.OrdinalIgnoreCase);
        }

        private static string DetectPlmSkillHint(string userMessage)
        {
            var t = (userMessage ?? "").Trim();
            if (string.IsNullOrEmpty(t)) return null;

            int ScoreDw()
            {
                var s = 0;
                if (Regex.IsMatch(t, @"\bplm\s*data\s*warehouse\b", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"\bplm\s*dw\b", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"data\s*warehouse\s*import", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"importfromplmdw", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"dw\s*tab", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"bom\s*colorway", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"plm\s*数据仓库", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"从\s*plm.*数据仓库", RegexOptions.IgnoreCase)) s += 3;
                return s;
            }

            int ScorePom()
            {
                var s = 0;
                if (Regex.IsMatch(t, @"\bpom\s*(and\s*)?grading\b", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"import\s*plm\s*pom", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"importplmpom", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"\bsize\s*run\b", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"grading\s*import", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"pom\s*评分", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"导入.*pom", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"\bgrading\b", RegexOptions.IgnoreCase) && Regex.IsMatch(t, @"\bpom\b", RegexOptions.IgnoreCase)) s += 2;
                return s;
            }

            int ScoreSearchView()
            {
                var s = 0;
                if (Regex.IsMatch(t, @"\bplm\s*search\s*view\b", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"importplmsearchview", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"plm\s*搜索视图", RegexOptions.IgnoreCase)) s += 3;
                if (Regex.IsMatch(t, @"search\s*view\s*import", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"mass\s*update\s*view", RegexOptions.IgnoreCase)) s += 2;
                if (Regex.IsMatch(t, @"sibling\s*view", RegexOptions.IgnoreCase)) s += 2;
                return s;
            }

            var dw = ScoreDw();
            var pom = ScorePom();
            var sv = ScoreSearchView();
            var max = Math.Max(dw, Math.Max(pom, sv));
            if (max > 0)
            {
                if (dw >= pom && dw >= sv) return "plm-dw";
                if (pom >= dw && pom >= sv) return "plm-pom-grading";
                return "plm-search-view";
            }

            if (Regex.IsMatch(t, @"\bplm\s*integration\b", RegexOptions.IgnoreCase)
                || Regex.IsMatch(t, @"plm\s*集成", RegexOptions.IgnoreCase)
                || Regex.IsMatch(t, @"\bimport\s*from\s*plm\b", RegexOptions.IgnoreCase)
                || Regex.IsMatch(t, @"\b从\s*plm\s*导入", RegexOptions.IgnoreCase)
                || (Regex.IsMatch(t, @"\bplm\b", RegexOptions.IgnoreCase) && Regex.IsMatch(t, @"\bimport\b", RegexOptions.IgnoreCase)))
                return "plm";

            return null;
        }

        private static string PlmSkillLabel(string hintKey)
        {
            if (hintKey == "plm-dw") return "Import Data Model and Reference Data";
            if (hintKey == "plm-pom-grading") return "Import PLM POM and Grading";
            if (hintKey == "plm-search-view") return "Import PLM Search View";
            if (hintKey == "plm") return "PLM Integration";
            return hintKey;
        }

        private static void AppendPlmSkillMismatchGuidance(StringBuilder sb, string activeSkillKey, string userMessage)
        {
            var hint = DetectPlmSkillHint(userMessage);
            if (string.IsNullOrEmpty(hint)) return;

            if (hint == "plm")
            {
                if (IsPlmCatalogSkill(activeSkillKey)) return;
                sb.AppendLine("## PLM Integration skill mismatch");
                sb.AppendLine("The user message targets PLM Integration, but the active skill is not a PLM Integration skill.");
                sb.AppendLine("Briefly recommend switching Skill to one of: Import Data Model and Reference Data, Import PLM POM and Grading, or Import PLM Search View.");
                sb.AppendLine("The chat UI shows clickable skill buttons below your reply — mention that the user can click them (or switch Skill at the top and send again / start a new chat).");
                sb.AppendLine("Do not proceed with App Config Builder workflows until the user switches skill.");
                sb.AppendLine();
                return;
            }

            if (activeSkillKey != null && activeSkillKey.Equals(hint, StringComparison.OrdinalIgnoreCase)) return;

            sb.AppendLine("## PLM Integration skill mismatch");
            sb.AppendLine("The user message targets: " + PlmSkillLabel(hint) + " (skill key: " + hint + ").");
            sb.AppendLine("Active skill key: " + (activeSkillKey ?? "(none)") + ".");
            sb.AppendLine("Briefly recommend switching Skill to \"" + PlmSkillLabel(hint) + "\".");
            sb.AppendLine("The chat UI shows a clickable skill button below your reply — point the user to it (or the Skill picker at the top).");
            sb.AppendLine("Do not stack App Config Builder or unrelated skills for this PLM task.");
            sb.AppendLine();
        }

        private static void AppendDataSourceScopeGuidance(StringBuilder sb, AppDataIntegrationAgentSessionStore.SessionData live)
        {
            if (live == null) return;
            var allowed = AppDataIntegrationAgentDataSourceBL.ListTenantCompanyDataSources();
            sb.AppendLine("## Data sources (tenant company scope)");
            sb.AppendLine("1) If the user gives an explicit database **connectionString**, pass it to run_select / get_table_schema / propose_sql (direct access; validated).");
            sb.AppendLine("   - After connectionString run_select: show results in chat only. Do NOT call open_query_result and do NOT mention an Open / SQL Workbench button.");
            sb.AppendLine("2) Tenant boundary: MasterDB AppDataSourceRegister where DataSourceOwnerCompanyId = CurrentCompanyId — same as System Settings → Database Registration. Call list_datasources; do not scan other tenants.");
            sb.AppendLine("- Default UI DataSourceRegisterId: " + (live.DataSourceRegisterId?.ToString() ?? "(not set)") + ".");
            if (allowed != null && allowed.Count > 0)
            {
                sb.AppendLine("- Tenant-accessible register ids:");
                foreach (var d in allowed)
                    sb.AppendLine("  - " + d.Id + ": " + (d.Name ?? ""));
            }
            else
                sb.AppendLine("- No data sources registered for this tenant company yet.");
            sb.AppendLine();
        }

        private static void AppendOpenUiCapability(StringBuilder sb)
        {
            sb.AppendLine("## Open App UI");
            sb.AppendLine("You CAN request App UI via MCP tools on server \"appai\". Never say you cannot open App pages.");
            sb.AppendLine("Calling open_* / preview_tables_data / open_query_result does NOT open immediately — the chat shows an Open button for the user.");
            sb.AppendLine();
            sb.AppendLine("### When user already asked to open UI");
            sb.AppendLine("- DB Table/View Data Preview / browse Erp_* rows → call preview_tables_data (tableName + DataSourceRegisterId). Do NOT build Search/Config.");
            sb.AppendLine("- Free-form SELECT / query result grid → call open_query_result with the same sql (SQL Workbench).");
            sb.AppendLine("- Search / ListEdit / MasterDetail / editors → open_search, open_list_edit_form, open_master_detail_form, open_* editors, or list_application_menus + open_app_page.");
            sb.AppendLine();
            sb.AppendLine("### After SQL / query-result answers (important)");
            sb.AppendLine("- Registered tenant DataSource: answer with run_select and a short markdown/table summary in the same turn.");
            sb.AppendLine("- In that SAME turn, also call open_query_result with the exact SQL and dataSourceRegisterId (SQL Workbench Open box).");
            sb.AppendLine("- User-supplied connectionString (unregistered DB): run_select with connectionString only; summarize in chat; never open_query_result or mention Open.");
            sb.AppendLine("- End registered-DS replies with one short line, e.g. Chinese: 「也可在 SQL Workbench 打开这条 Query 查看结果（点下方 Open）。」");
            sb.AppendLine("- Prefer open_query_result for custom SELECT / joins; use preview_tables_data only for simple single-table browse when the user asked for Table Preview.");
            sb.AppendLine("- Never say \"click Open\" unless you actually called the tool that creates the Open box.");
            sb.AppendLine();
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

        private static string SavedSkillName(int skillId)
        {
            var ds = AppAISkillBL.GetDefaultDataSourceId();
            if (!ds.HasValue || skillId <= 0) return null;
            return AppAISkillBL.GetSkillById(ds.Value, skillId)?.Name;
        }

        private static string AdaptOtherSkillForThisAgent(string name, string composed)
        {
            if (string.IsNullOrWhiteSpace(composed)) return composed;
            if (string.Equals(name, SqlSkillName, StringComparison.OrdinalIgnoreCase))
            {
                return composed + @"

## Running inside App Data Integration Agent
Use MCP tools list_datasources, get_table_schema, run_select, and propose_sql.
Writes (INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD) must go through propose_sql and wait for the user.";
            }
            if (string.Equals(name, ReportSkillName, StringComparison.OrdinalIgnoreCase))
            {
                return composed + @"

## Running inside App Data Integration Agent
You do not have list_available_searches / get_search_criteria / execute_report.
Use list_application_assets, get_table_schema, and run_select. Summarize results in chat. Do not invent SearchId values.";
            }
            return composed;
        }

        private static string LoadNamedComposed(string name)
        {
            if (string.Equals(name, SqlSkillName, StringComparison.OrdinalIgnoreCase))
                return AppDbGenieBL.GetComposedSqlSkillPrompt();
            if (string.Equals(name, ReportSkillName, StringComparison.OrdinalIgnoreCase))
                return AppReportAgentBL.GetComposedReportSkillPrompt();
            var ds = AppAISkillBL.GetDefaultDataSourceId();
            if (!ds.HasValue) return null;
            var skill = AppAISkillBL.GetSkillByName(ds.Value, name);
            if (skill == null || IsHiddenSavedName(skill.Name)) return null;
            return AppAISkillBL.GetComposedSkillPrompt(ds.Value, skill.SkillId);
        }

        private static bool IsWellKnownOtherName(string name)
        {
            return string.Equals(name, SqlSkillName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, ReportSkillName, StringComparison.OrdinalIgnoreCase);
        }

        private static string OtherSkillLabel(string name)
        {
            if (string.Equals(name, SqlSkillName, StringComparison.OrdinalIgnoreCase)) return "SQL Skill";
            if (string.Equals(name, ReportSkillName, StringComparison.OrdinalIgnoreCase)) return "Report Skill";
            return name;
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
            if (name.StartsWith("app-data-integration-agent-", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.StartsWith("cursor-agent-", StringComparison.OrdinalIgnoreCase)) return true; // legacy seeded names
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
            var path = Path.Combine(root, "app-data-integration-agent-skills.json");
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
