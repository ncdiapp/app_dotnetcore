using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Deterministic polish when the model has not cached an AiPolishedRenderSpec yet.
    /// Structure only — does not merge rows into HTML.
    /// </summary>
    public static class AiRenderPolishHeuristics
    {
        public static AiPolishedRenderSpec Build(
            string layout,
            string title,
            IList<string> columnNames,
            JArray sampleRows,
            bool selectAllColumns = false)
        {
            layout = (layout ?? "table").Trim().ToLowerInvariant();
            var cols = (columnNames ?? Array.Empty<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sample = sampleRows?.OfType<JObject>().Take(AgentAiPolishCacheBL.SampleMaxRows).ToList()
                         ?? new List<JObject>();

            var imageField = PickImageField(cols, sample);
            var idField = PickByNames(cols, "product_code", "style_no", "styleno", "referenceid", "reference_id",
                "article", "code", "id", "sku", "style");
            var titleField = PickByNames(cols, "description", "name", "title", "style_name", "stylename", "product_name");
            var statusField = PickByNames(cols, "stylestatus", "samplestatus", "status", "state", "approval");
            var colorField = PickByNames(cols, "colorname", "color", "colour", "colorway");
            var metaField = PickByNames(cols, "season", "classification", "category", "department", "projectofficer", "date");

            var fields = new List<AiPolishFieldBinding>();
            void AddField(string name, string role, string typo)
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                if (fields.Any(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))) return;
                if (fields.Count >= AiPolishedRenderSpec.MaxDisplayFields) return;
                fields.Add(new AiPolishFieldBinding { Name = name, Role = role, TypographyHint = typo });
            }

            AddField(idField, "idLink", "accent-semibold");
            AddField(titleField, "title", "body");
            AddField(colorField, "colorSwatch", "meta");
            AddField(statusField, "statusPill", "status");
            AddField(metaField, "meta", "muted-small");

            // Only fill extras when we have fewer than 3 role-picked fields (keep cards uncluttered).
            const int minPrimary = 3;
            if (fields.Count < minPrimary)
            {
                foreach (var c in cols)
                {
                    if (fields.Count >= minPrimary) break;
                    if (string.Equals(c, imageField, StringComparison.OrdinalIgnoreCase)) continue;
                    if (LooksLikeUrlOnly(c)) continue;
                    AddField(c, "body", "body");
                }
            }

            List<string> columns;
            if (selectAllColumns || layout == "form")
            {
                columns = cols.ToList();
            }
            else
            {
                columns = new List<string>();
                void AddCol(string n)
                {
                    if (string.IsNullOrWhiteSpace(n)) return;
                    if (columns.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase))) return;
                    if (columns.Count >= AiPolishedRenderSpec.MaxColumnsDefault) return;
                    columns.Add(n);
                }
                AddCol(idField);
                AddCol(titleField);
                AddCol(statusField);
                AddCol(colorField);
                AddCol(metaField);
                foreach (var c in cols)
                {
                    if (string.Equals(c, imageField, StringComparison.OrdinalIgnoreCase)) continue;
                    AddCol(c);
                }
            }

            var filters = new List<AiPolishFilterField>
            {
                new AiPolishFilterField
                {
                    Name = "_search",
                    Control = "search",
                    SearchFields = new List<string>(
                        new[] { idField, titleField }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }
            };
            if (!string.IsNullOrWhiteSpace(statusField))
            {
                filters.Add(new AiPolishFilterField
                {
                    Name = statusField,
                    Control = "multiSelect"
                });
            }

            return new AiPolishedRenderSpec
            {
                Version = AiPolishedRenderSpec.CurrentVersion,
                Layout = layout,
                Title = title,
                SelectAllColumns = selectAllColumns,
                Bindings = new AiPolishBindings
                {
                    PrimaryImageField = imageField,
                    Fields = fields,
                    Columns = columns,
                    FilterFields = filters
                },
                LayoutHints = new AiPolishLayoutHints
                {
                    CardsPerRow = layout == "cards" ? 4 : null,
                    CardRows = layout == "cards" ? 4 : null,
                    StatusColorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Approved"] = "success",
                        ["Dropped"] = "danger",
                        ["Hold"] = "muted",
                        ["In Development"] = "info",
                        ["InDevelopment"] = "info"
                    }
                }
            };
        }

        private static string PickImageField(List<string> cols, List<JObject> sample)
        {
            foreach (var c in cols)
            {
                var n = c.ToLowerInvariant();
                if (n.Contains("image") || n.Contains("sketch") || n.Contains("photo")
                    || n.Contains("picture") || n.Contains("thumb") || n.EndsWith("link") && n.Contains("img"))
                    return c;
            }
            foreach (var c in cols)
            {
                foreach (var row in sample)
                {
                    var v = row[c]?.ToString();
                    if (CardSheet.IsImageValue(v)) return c;
                }
            }
            return null;
        }

        private static string PickByNames(List<string> cols, params string[] needles)
        {
            foreach (var needle in needles)
            {
                var hit = cols.FirstOrDefault(c =>
                    string.Equals(c, needle, StringComparison.OrdinalIgnoreCase)
                    || c.Replace("_", "", StringComparison.OrdinalIgnoreCase)
                        .Equals(needle.Replace("_", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            foreach (var needle in needles)
            {
                var hit = cols.FirstOrDefault(c =>
                    c.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
                if (hit != null) return hit;
            }
            return null;
        }

        private static bool LooksLikeUrlOnly(string name)
        {
            var n = (name ?? "").ToLowerInvariant();
            return n.Contains("url") || n.EndsWith("link") || n.Contains("href");
        }
    }
}
