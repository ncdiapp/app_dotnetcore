using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if NETFRAMEWORK
using System.Web;
#endif
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent
{
    /// <summary>
    /// Manages markdown-based cross-session memory for the AppBuilder AI Agent.
    ///
    /// Memory layout (under {AppRoot}/memory/appbuilder/):
    ///   platform-state.md    — what transactions and tables currently exist
    ///   build-history.md     — summary of every successful build session
    ///   agent-notes.md       — free-form notes the agent can write to itself
    /// </summary>
    public static class AppBuilderAgentMemoryBL
    {
        // ── Paths ─────────────────────────────────────────────────────────────

        private static string MemoryRoot
        {
            get
            {
#if NETFRAMEWORK
                // TODO-PHASE4: Replace with IWebHostEnvironment.ContentRootPath
                try
                {
                    // Running under IIS / ASP.NET
                    return Path.Combine(HttpRuntime.AppDomainAppPath, "memory", "appbuilder");
                }
                catch
                {
                    // Unit-test / standalone fallback
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory", "appbuilder");
                }
#else
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory", "appbuilder");
#endif
            }
        }

        private static string PlatformStateFile => Path.Combine(MemoryRoot, "platform-state.md");
        private static string BuildHistoryFile   => Path.Combine(MemoryRoot, "build-history.md");
        private static string AgentNotesFile     => Path.Combine(MemoryRoot, "agent-notes.md");

        // ── Ensure directory ─────────────────────────────────────────────────

        private static void EnsureDirectory()
        {
            if (!Directory.Exists(MemoryRoot))
                Directory.CreateDirectory(MemoryRoot);
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ: Load all memory into a single markdown string for the system prompt
        // ─────────────────────────────────────────────────────────────────────

        // Maximum chars per memory section injected into the system prompt.
        // Keeps the system prompt from bloating as platform history grows.
        private const int MaxPlatformStateChars = 3000;
        private const int MaxBuildHistoryChars   = 4000;
        private const int MaxAgentNotesChars     = 2000;

        public static string LoadMemoryContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## AppBuilder Agent Memory");
            sb.AppendLine();

            sb.AppendLine("### Platform State (last updated)");
            sb.AppendLine(TailChars(ReadFile(PlatformStateFile, "No platform state recorded yet."), MaxPlatformStateChars));
            sb.AppendLine();

            sb.AppendLine("### Build History (last 5 sessions)");
            sb.AppendLine(TailChars(ReadFile(BuildHistoryFile, "No builds recorded yet."), MaxBuildHistoryChars));
            sb.AppendLine();

            sb.AppendLine("### Agent Notes");
            sb.AppendLine(TailChars(ReadFile(AgentNotesFile, "No notes yet."), MaxAgentNotesChars));

            return sb.ToString();
        }

        /// <summary>
        /// Returns the last <paramref name="maxChars"/> characters of a string,
        /// prefixed with a truncation notice if text was dropped.
        /// Keeps the most-recent content (which is always at the end of append-only files).
        /// </summary>
        private static string TailChars(string text, int maxChars)
        {
            if (text == null || text.Length <= maxChars) return text;
            return "[...older entries omitted...]\n" + text.Substring(text.Length - maxChars);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SEARCH: RAG-style retrieval — return only relevant memory sections
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Searches all memory files for sections containing the query keywords.
        /// Returns up to <paramref name="maxSections"/> matching sections so the
        /// agent retrieves only the context it needs rather than the full memory dump.
        /// </summary>
        public static string SearchMemory(string query, int maxSections = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "No query provided.";

            var keywords = query.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var results  = new System.Text.StringBuilder();
            int found    = 0;

            void SearchFile(string path, string label)
            {
                if (!File.Exists(path)) return;
                var text     = ReadFile(path, "");
                var sections = text.Split(new[] { "\n---\n", "---\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var section in sections)
                {
                    if (found >= maxSections) break;
                    bool matches = keywords.Any(k =>
                        section.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matches)
                    {
                        results.AppendLine($"[{label}]");
                        // Return up to 1000 chars per matching section
                        results.AppendLine(section.Length > 1000 ? section.Substring(0, 1000) + "…" : section);
                        results.AppendLine();
                        found++;
                    }
                }
            }

            SearchFile(PlatformStateFile, "Platform State");
            SearchFile(BuildHistoryFile,  "Build History");
            SearchFile(AgentNotesFile,    "Agent Notes");

            return found == 0
                ? $"No memory entries found matching: {query}"
                : results.ToString().Trim();
        }

        // ─────────────────────────────────────────────────────────────────────
        // WRITE: After a successful build, record what was created
        // ─────────────────────────────────────────────────────────────────────

        public static void RecordBuildSession(
            string userRequest,
            string agentSummary,
            List<string> createdTables,
            List<(int Id, string Name)> createdTransactions)
        {
            try
            {
                EnsureDirectory();

                // Append to build history (keep last 5 entries)
                AppendBuildHistory(userRequest, agentSummary, createdTables, createdTransactions);

                // Refresh platform state snapshot
                UpdatePlatformState(createdTables, createdTransactions);
            }
            catch
            {
                // Memory failures must never crash the agent
            }
        }

        public static void SaveAgentNote(string note)
        {
            try
            {
                EnsureDirectory();
                var entry = $"\n---\n*{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*\n{note}\n";
                File.AppendAllText(AgentNotesFile, entry, Encoding.UTF8);
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private static string ReadFile(string path, string defaultContent)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : defaultContent;
            }
            catch
            {
                return defaultContent;
            }
        }

        private static void AppendBuildHistory(
            string request,
            string summary,
            List<string> tables,
            List<(int Id, string Name)> transactions)
        {
            var entry = new StringBuilder();
            entry.AppendLine($"\n---\n### Build on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            entry.AppendLine($"**Request:** {request}");
            entry.AppendLine($"**Summary:** {summary}");

            if (tables?.Count > 0)
            {
                entry.AppendLine("**Tables created:**");
                tables.ForEach(t => entry.AppendLine($"- {t}"));
            }

            if (transactions?.Count > 0)
            {
                entry.AppendLine("**Transactions created:**");
                transactions.ForEach(t => entry.AppendLine($"- ID={t.Id}: {t.Name}"));
            }

            // Append and trim to last 5 entries
            var existing = ReadFile(BuildHistoryFile, "");
            var combined = existing + entry;
            var sections = combined.Split(new[] { "\n---\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length > 5)
                combined = "---\n" + string.Join("\n---\n", sections.Skip(sections.Length - 5));

            File.WriteAllText(BuildHistoryFile, combined, Encoding.UTF8);
        }

        private static void UpdatePlatformState(
            List<string> newTables,
            List<(int Id, string Name)> newTransactions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"*Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");
            sb.AppendLine();

            if (newTables?.Count > 0)
            {
                sb.AppendLine("**Recently created database tables:**");
                newTables.ForEach(t => sb.AppendLine($"- {t}"));
            }

            if (newTransactions?.Count > 0)
            {
                sb.AppendLine("**Recently created transactions:**");
                newTransactions.ForEach(t => sb.AppendLine($"- [{t.Id}] {t.Name}"));
            }

            File.WriteAllText(PlatformStateFile, sb.ToString(), Encoding.UTF8);
        }
    }
}
