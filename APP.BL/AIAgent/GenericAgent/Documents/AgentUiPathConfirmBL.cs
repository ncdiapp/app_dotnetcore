using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Chat-session gate after ask_user when the request looks like data analysis:
    /// KPI/Dashboard batch analysis vs fetch-and-display only.
    /// </summary>
    public static class AgentUiPathConfirmBL
    {
        public const string ContextKey = "confirm_kpi_dashboard";
        public const string OptionKpiDashboard = "kpi_dashboard";
        public const string OptionFetchDisplay = "fetch_display";

        private static readonly ConcurrentDictionary<string, string> ChoiceBySession =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void MarkKpiDashboard(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            ChoiceBySession[chatSessionKey.Trim()] = OptionKpiDashboard;
        }

        public static void MarkFetchDisplay(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            ChoiceBySession[chatSessionKey.Trim()] = OptionFetchDisplay;
        }

        /// <summary>User chose KPI/Dashboard batch analysis.</summary>
        public static bool IsKpiDashboardConfirmed(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            return ChoiceBySession.TryGetValue(chatSessionKey.Trim(), out var v)
                   && string.Equals(v, OptionKpiDashboard, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Backward-compatible alias for render/export gates.</summary>
        public static bool IsConfirmed(string chatSessionKey) => IsKpiDashboardConfirmed(chatSessionKey);

        public static bool IsFetchDisplayOnly(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return false;
            return ChoiceBySession.TryGetValue(chatSessionKey.Trim(), out var v)
                   && string.Equals(v, OptionFetchDisplay, StringComparison.OrdinalIgnoreCase);
        }

        public static void Clear(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            ChoiceBySession.TryRemove(chatSessionKey.Trim(), out _);
        }

        public static bool LooksLikeKpiDashboardChoice(IEnumerable<string> selectedIds, string freeText)
        {
            if (selectedIds != null)
            {
                foreach (var id in selectedIds)
                {
                    if (IsKpiToken(id)) return true;
                }
            }
            return IsKpiToken(freeText);
        }

        public static bool LooksLikeFetchDisplayChoice(IEnumerable<string> selectedIds, string freeText)
        {
            if (selectedIds != null)
            {
                foreach (var id in selectedIds)
                {
                    if (IsFetchToken(id)) return true;
                }
            }
            return IsFetchToken(freeText);
        }

        /// <summary>Legacy helper used by tests — KPI path tokens.</summary>
        public static bool LooksLikeYes(IEnumerable<string> selectedIds, string freeText)
            => LooksLikeKpiDashboardChoice(selectedIds, freeText);

        public static void MarkConfirmed(string chatSessionKey) => MarkKpiDashboard(chatSessionKey);

        private static bool IsKpiToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim().ToLowerInvariant();
            return t is "kpi_dashboard" or "dashboard" or "kpi" or "yes" or "y" or "true" or "ok" or "proceed" or "confirm"
                || t.Contains("kpi")
                || t.Contains("dashboard")
                || t.StartsWith("yes")
                || t.Contains("batch analysis");
        }

        private static bool IsFetchToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var t = s.Trim().ToLowerInvariant();
            return t is "fetch_display" or "no" or "n" or "false" or "display" or "show"
                || t.Contains("fetch")
                || t.Contains("display only")
                || t.Contains("show only")
                || t.Contains("without kpi")
                || t.Contains("no dashboard");
        }

        public static string AskUserOptionsJson() =>
            "[{\"id\":\"kpi_dashboard\",\"label\":\"KPI / Dashboard — batch analysis\"}," +
            "{\"id\":\"fetch_display\",\"label\":\"Only fetch and show the data I asked for\"}]";

        public static string AskUserPromptHint() =>
            "This looks like a data-analysis request. How should I proceed?";
    }
}
