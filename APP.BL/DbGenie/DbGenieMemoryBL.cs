using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if NETFRAMEWORK
using System.Web;
#endif
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.DbGenie
{
    /// <summary>
    /// Manages two-tier cross-session memory for DBA-Genie:
    ///
    ///  Tier 1 — sessions/{sessionId}.json
    ///      Full conversation history for a specific session.
    ///      Loaded at the start of each turn so the agent can resume exactly.
    ///
    ///  Tier 2 — memory.md
    ///      Rolling cross-session summary (last 5 sessions).
    ///      Injected into the system prompt so a brand-new session still has context.
    /// </summary>
    public static class DbGenieMemoryBL
    {
        // ── Paths ─────────────────────────────────────────────────────────────

        private static string MemoryRoot
        {
            get
            {
#if NETFRAMEWORK
                // TODO-PHASE4: Replace with IWebHostEnvironment.ContentRootPath
                try { return Path.Combine(HttpRuntime.AppDomainAppPath, "memory", "dbgenie"); }
                catch { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory", "dbgenie"); }
#else
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory", "dbgenie");
#endif
            }
        }

        private static string SessionsDir   => Path.Combine(MemoryRoot, "sessions");
        private static string MemoryMdFile  => Path.Combine(MemoryRoot, "memory.md");

        private const int MaxSummaryEntries  = 5;
        private const int SessionMaxAgeDays  = 30;

        // ── Ensure directories ────────────────────────────────────────────────

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MemoryRoot))    Directory.CreateDirectory(MemoryRoot);
            if (!Directory.Exists(SessionsDir))   Directory.CreateDirectory(SessionsDir);
        }

        // ─────────────────────────────────────────────────────────────────────
        // TIER 1 — Session files
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the stored conversation history for <paramref name="sessionId"/>,
        /// or null if no session file exists yet.
        /// </summary>
        public static List<DbGenieChatMessageDto> LoadSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return null;
            try
            {
                var path = SessionFilePath(sessionId);
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<DbGenieChatMessageDto>>(json)
                       ?? new List<DbGenieChatMessageDto>();
            }
            catch { return null; }
        }

        /// <summary>
        /// Appends one exchange (user + assistant) to the session file and saves it.
        /// Creates the file on first call for this <paramref name="sessionId"/>.
        /// </summary>
        public static void SaveExchange(
            string sessionId,
            string userMessage,
            string assistantResponse,
            string generatedSql          = null,
            int?   dataSourceRegisterId  = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;
            try
            {
                EnsureDirectories();

                // Load existing messages (or start fresh)
                var messages = LoadSession(sessionId) ?? new List<DbGenieChatMessageDto>();

                messages.Add(new DbGenieChatMessageDto
                {
                    Role      = "user",
                    Content   = userMessage,
                    Timestamp = DateTime.UtcNow
                });

                var assistantMsg = new DbGenieChatMessageDto
                {
                    Role         = "assistant",
                    Content      = assistantResponse,
                    Timestamp    = DateTime.UtcNow,
                    HasSQL       = !string.IsNullOrWhiteSpace(generatedSql),
                    GeneratedSQL = generatedSql
                };
                messages.Add(assistantMsg);

                // Write session file
                var json = JsonConvert.SerializeObject(messages, Formatting.Indented);
                File.WriteAllText(SessionFilePath(sessionId), json, Encoding.UTF8);

                // Update cross-session summary
                AppendToMemoryMd(sessionId, userMessage, assistantResponse, generatedSql, dataSourceRegisterId);

                // Prune old session files (best-effort, non-blocking)
                PruneOldSessions();
            }
            catch { /* memory failures must never crash the agent */ }
        }

        // ─────────────────────────────────────────────────────────────────────
        // TIER 2 — Cross-session summary
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the contents of memory.md to inject into the system prompt.
        /// Returns null when the file is empty or missing (nothing to inject).
        /// </summary>
        public static string LoadMemoryContext()
        {
            try
            {
                if (!File.Exists(MemoryMdFile)) return null;
                var content = File.ReadAllText(MemoryMdFile, Encoding.UTF8).Trim();
                if (string.IsNullOrWhiteSpace(content)) return null;

                return "## DBA-Genie Memory (from previous sessions)\n\n" + content;
            }
            catch { return null; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private static string SessionFilePath(string sessionId)
        {
            // Sanitise sessionId so it is safe as a filename
            var safe = Regex.Replace(sessionId, @"[^A-Za-z0-9\-_]", "_");
            return Path.Combine(SessionsDir, safe + ".json");
        }

        private static void AppendToMemoryMd(
            string sessionId,
            string userMessage,
            string assistantResponse,
            string sql,
            int?   dataSourceId)
        {
            try
            {
                EnsureDirectories();

                var tables = ExtractTableNames(sql);

                var entry = new StringBuilder();
                entry.AppendLine($"\n---\n*{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*" +
                                 (dataSourceId.HasValue ? $" | DataSource: {dataSourceId}" : ""));
                entry.AppendLine($"**Question:** {Truncate(userMessage, 250)}");

                if (tables.Count > 0)
                    entry.AppendLine($"**Tables:** {string.Join(", ", tables)}");

                if (!string.IsNullOrWhiteSpace(sql))
                {
                    entry.AppendLine("**SQL:**");
                    entry.AppendLine("```sql");
                    entry.AppendLine(Truncate(sql, 600));
                    entry.AppendLine("```");
                }
                else
                {
                    entry.AppendLine($"**Answer:** {Truncate(assistantResponse, 300)}");
                }

                var existing = File.Exists(MemoryMdFile)
                    ? File.ReadAllText(MemoryMdFile, Encoding.UTF8)
                    : "";

                var combined = existing + entry;

                // Keep last MaxSummaryEntries sections (split on "\n---\n")
                var sections = combined.Split(new[] { "\n---\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (sections.Length > MaxSummaryEntries)
                    combined = "---\n" + string.Join("\n---\n", sections.Skip(sections.Length - MaxSummaryEntries));

                File.WriteAllText(MemoryMdFile, combined, Encoding.UTF8);
            }
            catch { }
        }

        private static void PruneOldSessions()
        {
            try
            {
                if (!Directory.Exists(SessionsDir)) return;
                var cutoff = DateTime.UtcNow.AddDays(-SessionMaxAgeDays);
                foreach (var file in Directory.GetFiles(SessionsDir, "*.json"))
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                        File.Delete(file);
                }
            }
            catch { }
        }

        private static List<string> ExtractTableNames(string sql)
        {
            var tables = new List<string>();
            if (string.IsNullOrWhiteSpace(sql)) return tables;

            var keywords = new[] { "FROM", "JOIN", "UPDATE", "INTO", "TABLE" };
            var tokens   = sql.Split(new[] { ' ', '\t', '\r', '\n', '(', ')' },
                                     StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (keywords.Contains(tokens[i].ToUpperInvariant()))
                {
                    var name = tokens[i + 1].Trim('[', ']', '`', '"', ',', ';');
                    if (!string.IsNullOrWhiteSpace(name) &&
                        !name.StartsWith("(")            &&
                        !name.StartsWith("@"))
                        tables.Add(name);
                }
            }

            return tables.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "…";
        }
    }
}
