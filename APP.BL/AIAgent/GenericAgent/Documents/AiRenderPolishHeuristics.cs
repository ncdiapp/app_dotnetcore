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
            // Prefer non-empty sample values: Article / Product over empty → ReferenceId for MCP blue id.
            var idField = PickByNamesPreferValue(cols, sample, "article", "product_code", "productcode", "product",
                "style_no", "styleno", "sku", "referenceid", "reference_id", "code", "id", "style");
            var titleField = PickByNamesPreferValue(cols, sample, "product_name", "productname", "description",
                "name", "title", "style_name", "stylename");
            var statusField = PickByNamesPreferValue(cols, sample, "sample_status", "samplestatus", "style_status", "stylestatus",
                "status", "state", "approval");
            var colorField = PickByNamesPreferValue(cols, sample, "colorname", "color", "colour", "colorway");
            var metaField = PickByNamesPreferValue(cols, sample, "classification", "sub_classification", "season", "season_3",
                "category", "department", "vendor", "projectofficer");
            var metaField2 = PickByNamesPreferValue(cols, sample, "sub_classification", "season_3", "season", "vendor", "mil_cid_number");

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
            if (!string.IsNullOrWhiteSpace(metaField2)
                && !string.Equals(metaField2, metaField, StringComparison.OrdinalIgnoreCase))
                AddField(metaField2, "meta", "muted-small");

            // Do not pad with label:value body fields — MCP gallery is role-only.

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
                    // Leave null — page grid comes from render/export cardsPerRow + cardRows (user 2x2 / 4x4).
                    CardsPerRow = null,
                    CardRows = null,
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
                    // Field name means Image + URL value (PLM /image/{id} without extension).
                    if (CardSheet.IsImageValue(v, c)) return c;
                }
            }
            return null;
        }

        private static string PickByNames(List<string> cols, params string[] needles)
            => PickByNamesPreferValue(cols, null, needles);

        /// <summary>Prefer a matching column that has at least one non-empty sample value.</summary>
        private static string PickByNamesPreferValue(List<string> cols, List<JObject> sample, params string[] needles)
        {
            string Fallback(string[] ns)
            {
                foreach (var needle in ns)
                {
                    var hit = cols.FirstOrDefault(c =>
                        string.Equals(c, needle, StringComparison.OrdinalIgnoreCase)
                        || c.Replace("_", "", StringComparison.OrdinalIgnoreCase)
                            .Equals(needle.Replace("_", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));
                    if (hit != null) return hit;
                }
                foreach (var needle in ns)
                {
                    var hit = cols.FirstOrDefault(c =>
                        c.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (hit != null) return hit;
                }
                return null;
            }

            if (sample == null || sample.Count == 0)
                return Fallback(needles);

            foreach (var needle in needles)
            {
                var hit = cols.FirstOrDefault(c =>
                    string.Equals(c, needle, StringComparison.OrdinalIgnoreCase)
                    || c.Replace("_", "", StringComparison.OrdinalIgnoreCase)
                        .Equals(needle.Replace("_", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));
                if (hit == null) continue;
                if (sample.Any(r => !string.IsNullOrWhiteSpace(r[hit]?.ToString())))
                    return hit;
            }
            return Fallback(needles);
        }

        private static bool LooksLikeUrlOnly(string name)
        {
            var n = (name ?? "").ToLowerInvariant();
            return n.Contains("url") || n.EndsWith("link") || n.Contains("href");
        }
    }
}
