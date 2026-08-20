using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using App.BL.AppMgr.AiSkill;
using App.BL.AppReportAgent;
using App.BL.DbGenie;
using APP.Components.EntityDto;

namespace App.BL.CursorAgent
{
    public static class CursorAgentSkillBL
    {
        public const string Policy = "cursor-agent-policy";
        public const string ImportPack = "cursor-agent-import-pack";
        public const string GatedSql = "cursor-agent-gated-sql";
        public const string Workspace = "cursor-agent-workspace";

        public static string BuildAlwaysOnPolicy(int? skillDataSourceId, int saasApplicationId, int? dataSourceRegisterId)
        {
            EnsureSeeded(skillDataSourceId);
            var sb = new StringBuilder();
            sb.AppendLine(LoadComposed(skillDataSourceId, Policy));
            sb.AppendLine();
            sb.AppendLine("## This session");
            sb.AppendLine("- Target Application (SaasApplicationId): " + saasApplicationId);
            if (dataSourceRegisterId.HasValue)
                sb.AppendLine("- Default DataSourceRegisterId: " + dataSourceRegisterId.Value);
            sb.AppendLine("- Use MCP tools on server \"appai\" for schema, files, import, and SQL.");
            sb.AppendLine("- Do not modify cloned application source (.cs/.tsx). Do not open a pull request.");
            return sb.ToString();
        }

        public static object ListSkills(int? skillDataSourceId)
        {
            EnsureSeeded(skillDataSourceId);
            if (!skillDataSourceId.HasValue) return new object[0];
            var names = new[] { Policy, ImportPack, GatedSql, Workspace };
            return names.Select(n =>
            {
                var skill = AppAISkillBL.GetSkillByName(skillDataSourceId.Value, n);
                return new
                {
                    Name = n,
                    Description = skill?.Description,
                    AlwaysOn = n == Policy
                };
            }).ToList();
        }

        public static string GetSkill(int? skillDataSourceId, string name)
        {
            EnsureSeeded(skillDataSourceId);
            if (string.Equals(name, CursorAgentSkillCatalogBL.SqlSkillName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "SqlSkill", StringComparison.OrdinalIgnoreCase))
                return AppDbGenieBL.GetComposedSqlSkillPrompt();
            if (string.Equals(name, CursorAgentSkillCatalogBL.ReportSkillName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "ReportSkill", StringComparison.OrdinalIgnoreCase))
                return AppReportAgentBL.GetComposedReportSkillPrompt();
            return LoadComposed(skillDataSourceId, name);
        }

        public static void EnsureSeeded(int? skillDataSourceId)
        {
            if (!skillDataSourceId.HasValue) return;
            try
            {
                SeedOne(skillDataSourceId.Value, Policy, "Always-on App Data Integration Agent policy (read-only source, gated import/SQL).", PolicyContent);
                SeedOne(skillDataSourceId.Value, ImportPack, "Write AppConfigPack JSON and import after user confirmation.", ImportContent);
                SeedOne(skillDataSourceId.Value, GatedSql, "Read schema / SELECT immediately; write SQL after user confirmation.", SqlContent);
                SeedOne(skillDataSourceId.Value, Workspace, "Read/write the per-session server workspace folders.", WorkspaceContent);
                EnsureImportPackRef(skillDataSourceId.Value);
            }
            catch { }
        }

        private static void SeedOne(int dsId, string name, string description, string content)
        {
            var existing = AppAISkillBL.GetSkillByName(dsId, name);
            if (existing != null)
            {
                // Keep managed cursor-agent-* skills in sync with code (Seed used to insert-only).
                if (!string.Equals(existing.SkillContent ?? "", content ?? "", StringComparison.Ordinal))
                {
                    existing.Description = description;
                    existing.SkillContent = content;
                    existing.IsActive = true;
                    AppAISkillBL.UpdateSkill(dsId, existing);
                }
                return;
            }
            AppAISkillBL.CreateSkill(dsId, new AppAISkillDto
            {
                Name = name,
                Description = description,
                SkillContent = content,
                IsActive = true
            });
        }

        private static void EnsureImportPackRef(int dsId)
        {
            var skill = AppAISkillBL.GetSkillByName(dsId, ImportPack);
            if (skill == null) return;
            if (skill.References != null && skill.References.Count > 0) return;
            var promptPath = FindImportPrompt();
            if (promptPath == null) return;
            AppAISkillBL.CreateRef(dsId, new AppAISkillRefDto
            {
                SkillId = skill.SkillId,
                FileName = "ImportAppConfig/PROMPT.md",
                FileContent = File.ReadAllText(promptPath),
                SortOrder = 1
            });
        }

        private static string FindImportPrompt()
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "AppReact", "ImportDoc", "ImportAppConfig", "PROMPT.md"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "AppReact", "ImportDoc", "ImportAppConfig", "PROMPT.md")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        private static string LoadComposed(int? skillDataSourceId, string name)
        {
            if (!skillDataSourceId.HasValue || string.IsNullOrWhiteSpace(name))
                return Fallback(name);
            var skill = AppAISkillBL.GetSkillByName(skillDataSourceId.Value, name);
            if (skill == null) return Fallback(name);
            return AppAISkillBL.GetComposedSkillPrompt(skillDataSourceId.Value, skill.SkillId) ?? Fallback(name);
        }

        private static string Fallback(string name)
        {
            if (name == ImportPack) return ImportContent;
            if (name == GatedSql) return SqlContent;
            if (name == Workspace) return WorkspaceContent;
            return PolicyContent;
        }

        private const string PolicyContent = @"You are the AppAI App Data Integration Agent.

Hard rules:
1. The cloned git repo is READ-ONLY knowledge. Do not edit .cs/.tsx/.csproj or open a PR.
2. The only writable disk is the MCP workspace (packs/, scripts/, output/, notes/).
3. Creating Transaction/Form/SearchView/Entity is done by writing an AppConfigPack JSON then propose_import_pack. Never invent numeric TransactionId values.
4. SELECT may run via run_select. INSERT/UPDATE/DELETE/CREATE TABLE/ALTER TABLE ADD must go through propose_sql and wait for the user.
5. Stay on the SaasApplicationId given in this session. Do not create a new Application.
6. Call get_skill when you need the import-pack, gated-sql, or workspace procedure.
7. You CAN request App UI (Open button in chat). Never refuse with ""cannot open"". Explicit open / Table Preview → preview_tables_data. For QUERY RESULT answers: in the SAME turn show the result text AND call open_query_result (do not ask first). Never say click Open unless you called the tool.";

        private const string ImportContent = @"When the user wants Transaction / Form / SearchView / Entity configuration:

1. On a new request, ask clarifying questions first and WAIT. Establish screen pattern first: Search+MasterDetail vs ListEdit (organizedType List). If the user said List Edit / ListEdit, use ListEdit only — no Search pair.
2. If the user only wants to see table data / open DB Table/View Data Preview — call preview_tables_data (chat shows Open); do NOT generate Search or AppConfigPack. For a custom SELECT grid use open_query_result.
3. Call get_skill('cursor-agent-import-pack') if you need the JSON contract (PROMPT.md is attached as a skill ref).
4. Write the pack to packs/<name>.appConfigPack.json via write_workspace_file.
5. Call validate_config_pack then preview_config_pack.
6. Do not call propose_import_pack. Tell the user the workspace file is ready so they can click Start Build in this chat.
7. source.generatedBy must be ""ai"". Use integrationId, never numeric TransactionId/SearchId. ListEdit uses organizedType List and transactions[].menu for the main menu.";

        private const string SqlContent = @"Database access:
- list_datasources then get_table_schema for structure.
- run_select for SELECT/WITH (row-capped).
- When answering with a query result: in the SAME turn call open_query_result with that SQL so the chat shows an Open box for SQL Workbench. Do not ask first.
- propose_sql for INSERT, UPDATE, DELETE, CREATE TABLE, ALTER TABLE ... ADD. Wait for the user.
- Forbidden: DROP, TRUNCATE, ALTER DROP, EXEC, multiple statements.";

        private const string WorkspaceContent = @"Session workspace folders:
- packs/   AppConfigPack JSON
- scripts/ SQL/scripts
- output/  import/SQL results
- notes/   working notes
Only these paths are writable. Use list_workspace_files / read_workspace_file / write_workspace_file / delete_workspace_file.";
    }
}
