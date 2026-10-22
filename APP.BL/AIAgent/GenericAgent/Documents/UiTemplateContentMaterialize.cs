using System;
using System.Threading;
using App.BL.TenantBusiness;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Replaces NULL / {{code_default}} TemplateContent keys with real LAYOUT HTML.
    /// Never overwrites intentionally customized HTML (except known legacy platform shells).
    /// </summary>
    public static class UiTemplateContentMaterialize
    {
        /// <summary>Bump when platform default Views change and DB shells should refresh.</summary>
        private const int RulesVersion = 8;
        private static int _appliedVersion;

        public static void EnsureOnce()
        {
            if (Volatile.Read(ref _appliedVersion) >= RulesVersion) return;
            try
            {
                MaterializeTool(UiTemplateMerge.NoneInteractiveTool, interactive: false);
                MaterializeTool(UiTemplateMerge.InteractiveTool, interactive: true);
                Volatile.Write(ref _appliedVersion, RulesVersion);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, nameof(EnsureOnce));
            }
        }

        private static void MaterializeTool(string toolName, bool interactive)
        {
            var tool = AppAgentLibraryToolBL.GetByLibraryAndToolName(UiTemplateMerge.LibraryKey, toolName);
            if (tool == null || tool.Id <= 0) return;

            JObject map;
            try
            {
                map = string.IsNullOrWhiteSpace(tool.TemplateContent)
                    ? new JObject()
                    : JObject.Parse(tool.TemplateContent);
            }
            catch
            {
                map = new JObject();
            }

            var changed = false;
            void Set(string key, string html)
            {
                var cur = map[key]?.ToString();
                if (!NeedsMaterialize(cur)) return;
                map[key] = html;
                changed = true;
            }

            Set("form", UiDefaultViewTemplates.FormDefault);
            Set("table", interactive ? UiDefaultViewTemplates.TableInteractive : UiDefaultViewTemplates.TableNoneInteractive);
            Set("cards", UiDefaultViewTemplates.CardsNoneInteractive);
            // Refresh platform cards → MCP gallery shell (hero / blue id / status / meta)
            if (IsLegacyCardsShell(map["cards"]?.ToString())
                || IsLegacyCardsWithoutImageBind(map["cards"]?.ToString())
                || IsPreMcpCardsShell(map["cards"]?.ToString()))
            {
                map["cards"] = UiDefaultViewTemplates.CardsNoneInteractive;
                changed = true;
            }
            Set("page", UiTemplateMerge.CodeDefaultMarker);
            Set("kpi", interactive ? UiDefaultViewTemplates.KpiInteractive : UiDefaultViewTemplates.KpiDashboardStub);
            Set("chart", interactive ? UiDefaultViewTemplates.ChartInteractive : UiDefaultViewTemplates.ChartStub);
            Set("dashboard", UiDefaultViewTemplates.KpiDashboardStub);
            // Refresh interactive kpi/chart stubs → designable control LAYOUTs
            if (interactive)
            {
                if (IsLegacyKpiShell(map["kpi"]?.ToString()))
                {
                    map["kpi"] = UiDefaultViewTemplates.KpiInteractive;
                    changed = true;
                }
                if (IsLegacyChartShell(map["chart"]?.ToString()))
                {
                    map["chart"] = UiDefaultViewTemplates.ChartInteractive;
                    changed = true;
                }
            }

            if (!changed) return;

            var json = map.ToString(Formatting.None);
            AppAgentLibraryToolBL.Upsert(tool with { TemplateContent = json });
            NLog.LogManager.GetCurrentClassLogger().Info(
                "Materialized TemplateContent HTML for {0}/{1}", UiTemplateMerge.LibraryKey, toolName);
        }

        private static bool NeedsMaterialize(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur)) return true;
            var t = cur.Trim();
            return string.Equals(t, UiTemplateMerge.CodeDefaultMarker, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(t, "{{ code_default }}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Old platform cards View without agent-cards-grid / agent-cards.css.</summary>
        private static bool IsLegacyCardsShell(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur) || NeedsMaterialize(cur)) return false;
            if (cur.IndexOf("agent-cards-grid", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return cur.IndexOf("data-role=\"cards\"", StringComparison.OrdinalIgnoreCase) >= 0
                   || cur.IndexOf("class=\"cards\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Platform agent-cards template before image_src / is_image bind.</summary>
        private static bool IsLegacyCardsWithoutImageBind(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur) || NeedsMaterialize(cur)) return false;
            if (cur.IndexOf("agent-cards-grid", StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            if (cur.IndexOf("is_image", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return cur.IndexOf("field.value", StringComparison.OrdinalIgnoreCase) >= 0
                   && cur.IndexOf("data-role=\"image\"", StringComparison.OrdinalIgnoreCase) < 0;
        }

        /// <summary>Platform cards before MCP v2 gallery shell (role hierarchy, no label dump).</summary>
        private static bool IsPreMcpCardsShell(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur) || NeedsMaterialize(cur)) return false;
            if (cur.IndexOf("agent-cards-mcp-v3", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return cur.IndexOf("agent-cards-grid", StringComparison.OrdinalIgnoreCase) >= 0
                   || cur.IndexOf("agent-card", StringComparison.OrdinalIgnoreCase) >= 0
                   || cur.IndexOf("agent-cards-mcp-v2", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Empty dashboard stub used as kpi control before KpiInteractive existed.</summary>
        private static bool IsLegacyKpiShell(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur) || NeedsMaterialize(cur)) return false;
            if (cur.IndexOf("agent-kpi-row", StringComparison.OrdinalIgnoreCase) >= 0
                || cur.IndexOf("data-role=\"kpi-card\"", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return cur.IndexOf("data-role=\"dashboard\"", StringComparison.OrdinalIgnoreCase) >= 0
                   && cur.IndexOf("kpi-card", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsLegacyChartShell(string cur)
        {
            if (string.IsNullOrWhiteSpace(cur) || NeedsMaterialize(cur)) return false;
            if (cur.IndexOf("id=\"chartHost\"", StringComparison.OrdinalIgnoreCase) >= 0
                || cur.IndexOf("id='chartHost'", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return cur.IndexOf("data-role=\"chart\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
