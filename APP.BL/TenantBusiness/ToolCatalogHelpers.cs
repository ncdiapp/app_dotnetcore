using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace App.BL.TenantBusiness
{
    // Guesses whether a tool only reads data or changes it. Used as a hint for the AI agent designer and shown
    // in the UI — it does not gate anything at run time.
    public static class ToolRiskGuesser
    {
        private static readonly HashSet<string> DeleteWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "delete", "remove", "drop", "purge", "truncate", "destroy" };

        private static readonly HashSet<string> WriteWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "create", "add", "update", "edit", "set", "insert", "save", "post", "send", "submit", "sync", "push", "write", "upsert", "import", "upload", "register" };

        public static string Guess(string toolName, bool? readOnlyHint = null, bool? destructiveHint = null)
        {
            if (destructiveHint == true) return "delete";
            if (readOnlyHint == true) return "read";

            var words = Tokenize(toolName);
            if (words.Any(DeleteWords.Contains)) return "delete";
            if (words.Any(WriteWords.Contains)) return "write";
            return "read";
        }

        // "DataExchange_Analyze_sales" / "createAppPackage" -> lower-case words
        public static List<string> Tokenize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<string>();
            var spaced = Regex.Replace(name, @"([a-z0-9])([A-Z])", "$1 $2");
            return Regex.Split(spaced, @"[^A-Za-z0-9]+").Where(w => w.Length > 0).Select(w => w.ToLowerInvariant()).ToList();
        }

        // Parameter names from a JSON-schema string, required ones marked with *.
        public static string SummarizeSchema(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson)) return "";
            try
            {
                var schema = JObject.Parse(schemaJson);
                var props = schema["properties"] as JObject;
                if (props == null) return "";
                var required = new HashSet<string>((schema["required"] as JArray ?? new JArray()).Select(t => t.ToString()), StringComparer.OrdinalIgnoreCase);
                var summary = string.Join(", ", props.Properties().Select(p => required.Contains(p.Name) ? p.Name + "*" : p.Name));
                return summary.Length > 400 ? summary.Substring(0, 400) : summary;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Debug(ex, "Tool schema is not valid JSON");
                return "";
            }
        }
    }

    // Chooses which catalog tools go into the AI prompt. Today: everything when the catalog is small, otherwise a
    // keyword-scored shortlist. Replace Current with an embedding-based implementation for RAG later — callers
    // only depend on this interface.
    public interface IToolCatalogRetriever
    {
        List<AppAgentToolCatalogDto> Retrieve(IReadOnlyList<AppAgentToolCatalogDto> all, string useCase, int max);
    }

    public sealed class KeywordToolCatalogRetriever : IToolCatalogRetriever
    {
        private static readonly HashSet<string> Stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "the", "and", "for", "with", "from", "that", "this", "into", "all", "any", "are", "can", "that", "new", "use", "get", "need" };

        public List<AppAgentToolCatalogDto> Retrieve(IReadOnlyList<AppAgentToolCatalogDto> all, string useCase, int max)
        {
            if (all.Count <= max) return all.ToList();

            var words = ToolRiskGuesser.Tokenize(useCase).Where(w => w.Length >= 3 && !Stop.Contains(w)).Distinct().ToList();
            return all
                .Select((t, i) => new { Tool = t, Index = i, Score = Score(t, words) })
                .OrderByDescending(x => x.Score).ThenBy(x => x.Index)
                .Take(max)
                .OrderBy(x => x.Index)
                .Select(x => x.Tool)
                .ToList();
        }

        private static int Score(AppAgentToolCatalogDto t, List<string> words)
        {
            var hay = new HashSet<string>(
                ToolRiskGuesser.Tokenize(t.ToolName + " " + t.LibraryKey + " " + t.Description), StringComparer.OrdinalIgnoreCase);
            return words.Count(hay.Contains);
        }
    }

    public static class ToolCatalogRetrieval
    {
        public static IToolCatalogRetriever Current { get; set; } = new KeywordToolCatalogRetriever();
    }
}
