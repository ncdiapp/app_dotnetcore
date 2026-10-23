using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Deterministic polish when the model has not cached an AiPolishedRenderSpec yet.
    /// Theme/name fit first; fill-rate is a hard floor (not fill-rate-first). Non-product-specific.
    /// </summary>
    public static class AiRenderPolishHeuristics
    {
        /// <summary>Reject columns below this non-empty rate in the sample (0–1).</summary>
        public const double MinFillRate = 0.30;

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
            var fillRates = ComputeFillRates(cols, sample);

            var imageField = PickImageField(cols, sample, fillRates);

            // Candidates: meet fill-rate floor (or no sample), exclude image column.
            var candidates = cols
                .Where(c => !string.Equals(c, imageField, StringComparison.OrdinalIgnoreCase))
                .Where(c => sample.Count == 0 || fillRates.GetValueOrDefault(c, 0) >= MinFillRate)
                .Select(c => new ColScore
                {
                    Name = c,
                    Theme = ThemeScore(c),
                    Fill = fillRates.GetValueOrDefault(c, sample.Count == 0 ? 1.0 : 0),
                    Kind = ClassifyKind(c, sample)
                })
                .OrderByDescending(x => x.Theme)
                .ThenByDescending(x => x.Fill)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var picked = new List<ColScore>();
            void TryPick(Func<ColScore, bool> pred, int max = 1)
            {
                var n = 0;
                foreach (var c in candidates)
                {
                    if (n >= max) break;
                    if (!pred(c)) continue;
                    if (picked.Any(p => string.Equals(p.Name, c.Name, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    if (picked.Count >= AiPolishedRenderSpec.MaxDisplayFields) break;
                    picked.Add(c);
                    n++;
                }
            }

            // Role slots: theme-first among fill-rate-qualified columns.
            TryPick(c => c.Kind == FieldKind.Id);
            TryPick(c => c.Kind == FieldKind.Title);
            TryPick(c => c.Kind == FieldKind.Color);
            TryPick(c => c.Kind == FieldKind.Status);
            // Meta needs a real theme hit (season/category/…) — skip opaque columns like "Noise".
            TryPick(c => c.Kind == FieldKind.Date || c.Kind == FieldKind.Position
                || (c.Kind == FieldKind.Meta && c.Theme >= 40), max: 4);
            // Remaining slots: only columns with meaningful theme fit (skip noise like "Noise").
            TryPick(c => c.Theme >= 40, max: AiPolishedRenderSpec.MaxDisplayFields);

            ApplyShowLabels(picked);

            var fields = new List<AiPolishFieldBinding>();
            foreach (var c in picked.Take(AiPolishedRenderSpec.MaxDisplayFields))
            {
                fields.Add(new AiPolishFieldBinding
                {
                    Name = c.Name,
                    Role = RoleFor(c.Kind, fields),
                    TypographyHint = TypoFor(c.Kind),
                    ShowLabel = c.ShowLabel,
                    Label = c.ShowLabel ? ShortLabel(c.Name) : null
                });
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
                foreach (var f in fields)
                    AddCol(f.Name);
                foreach (var c in cols)
                {
                    if (string.Equals(c, imageField, StringComparison.OrdinalIgnoreCase)) continue;
                    AddCol(c);
                }
            }

            var idName = fields.FirstOrDefault(f => f.Role == "idLink")?.Name;
            var titleName = fields.FirstOrDefault(f => f.Role == "title")?.Name;
            var statusName = fields.FirstOrDefault(f =>
                string.Equals(f.Role, "statusPill", StringComparison.OrdinalIgnoreCase))?.Name;

            var filters = new List<AiPolishFilterField>
            {
                new AiPolishFilterField
                {
                    Name = "_search",
                    Control = "search",
                    SearchFields = new List<string>(
                        new[] { idName, titleName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }
            };
            if (!string.IsNullOrWhiteSpace(statusName))
            {
                filters.Add(new AiPolishFilterField
                {
                    Name = statusName,
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

        /// <summary>Per-column non-empty rate in sample (0–1). Used by prepare_ui_render_polish.</summary>
        public static Dictionary<string, double> ComputeFillRates(IList<string> cols, IList<JObject> sample)
        {
            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (cols == null) return map;
            var n = sample?.Count ?? 0;
            foreach (var c in cols)
            {
                if (string.IsNullOrWhiteSpace(c)) continue;
                if (n == 0)
                {
                    map[c] = 0;
                    continue;
                }
                var filled = sample.Count(r => !string.IsNullOrWhiteSpace(r?[c]?.ToString()));
                map[c] = filled / (double)n;
            }
            return map;
        }

        private static void ApplyShowLabels(List<ColScore> picked)
        {
            if (picked == null || picked.Count == 0) return;
            var dateCount = picked.Count(p => p.Kind == FieldKind.Date);
            var posCount = picked.Count(p => p.Kind == FieldKind.Position);
            foreach (var p in picked)
            {
                // Ambiguity among selected siblings — not "all dates always labeled".
                if (p.Kind == FieldKind.Date && dateCount >= 2)
                    p.ShowLabel = true;
                else if (p.Kind == FieldKind.Position && posCount >= 2)
                    p.ShowLabel = true;
                else if (p.Kind == FieldKind.Meta && p.Theme < 40)
                    p.ShowLabel = true; // obscure business column
                else if (p.Kind is FieldKind.Id or FieldKind.Title or FieldKind.Status or FieldKind.Color)
                    p.ShowLabel = false;
                else if (p.Kind == FieldKind.Date || p.Kind == FieldKind.Position)
                    p.ShowLabel = false; // single clear field of that kind
                else
                    p.ShowLabel = p.Theme < 35;
            }
        }

        private static string RoleFor(FieldKind kind, List<AiPolishFieldBinding> existing)
        {
            bool Has(string role) => existing.Any(f =>
                string.Equals(f.Role, role, StringComparison.OrdinalIgnoreCase));
            return kind switch
            {
                FieldKind.Id when !Has("idLink") => "idLink",
                FieldKind.Title when !Has("title") => "title",
                FieldKind.Color when !Has("colorSwatch") => "colorSwatch",
                FieldKind.Status when !Has("statusPill") => "statusPill",
                _ => "meta"
            };
        }

        private static string TypoFor(FieldKind kind) => kind switch
        {
            FieldKind.Id => "accent-semibold",
            FieldKind.Title => "body",
            FieldKind.Color => "meta",
            FieldKind.Status => "status",
            _ => "muted-small"
        };

        private static string ShortLabel(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var parts = name.Trim().Replace('-', ' ').Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (sb.Length > 0) sb.Append(' ');
                if (part.Length == 1)
                    sb.Append(char.ToUpperInvariant(part[0]));
                else
                    sb.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
            }
            return sb.ToString();
        }

        private static string PickImageField(
            List<string> cols, List<JObject> sample, Dictionary<string, double> fillRates)
        {
            foreach (var c in cols)
            {
                var n = c.ToLowerInvariant();
                if (n.Contains("image") || n.Contains("sketch") || n.Contains("photo")
                    || n.Contains("picture") || n.Contains("thumb") || n.EndsWith("link") && n.Contains("img"))
                {
                    if (sample.Count == 0 || fillRates.GetValueOrDefault(c, 0) >= MinFillRate * 0.5)
                        return c;
                }
            }
            foreach (var c in cols)
            {
                foreach (var row in sample)
                {
                    var v = row[c]?.ToString();
                    if (CardSheet.IsImageValue(v, c))
                        return c;
                }
            }
            return null;
        }

        private static int ThemeScore(string name)
        {
            var n = Normalize(name);
            if (n.Length == 0) return 0;
            // Strong identity / title signals (domain-agnostic).
            if (ExactOrEnds(n, "id", "code", "sku", "key", "number", "no", "num")) return 90;
            if (ContainsAny(n, "productcode", "stylecode", "article", "referenceid", "refid")) return 88;
            if (ExactOrEnds(n, "name", "title", "description", "label", "caption")) return 85;
            if (ContainsAny(n, "productname", "stylename", "displayname", "fullname")) return 84;
            if (ContainsAny(n, "status", "state", "approval")) return 70;
            if (ContainsAny(n, "color", "colour", "colorway", "colorname")) return 65;
            if (ContainsAny(n, "date", "target", "actual", "deadline", "ship", "adopt", "exfactory")) return 55;
            if (ContainsAny(n, "position", "placement", "location", "align", "corner")) return 55;
            if (ContainsAny(n, "season", "category", "department", "vendor", "classification", "type", "kind")) return 50;
            if (LooksLikeUrlOnly(name)) return 5;
            if (n.Contains("delta") || n.EndsWith("qty") || n.Contains("quantity") || n.Contains("amount")) return 25;
            return 30; // unknown text column — modest theme score
        }

        private static FieldKind ClassifyKind(string name, List<JObject> sample)
        {
            var n = Normalize(name);
            if (ContainsAny(n, "status", "state", "approval") && !ContainsAny(n, "date"))
                return FieldKind.Status;
            if (ContainsAny(n, "color", "colour", "colorway"))
                return FieldKind.Color;
            if (ContainsAny(n, "position", "placement", "location", "align", "corner")
                && !ContainsAny(n, "date"))
                return FieldKind.Position;
            if (ContainsAny(n, "date", "deadline", "exfactory")
                || (ContainsAny(n, "target", "actual", "adopt", "ship") && !ContainsAny(n, "delta")))
                return FieldKind.Date;
            // Sample values look like ISO dates → Date even without name hint.
            if (sample != null && sample.Take(5).Any(r => CardSheet.LooksLikeDateValue(r?[name]?.ToString())))
                return FieldKind.Date;
            if (ExactOrEnds(n, "id", "code", "sku", "key", "number", "no", "num")
                || ContainsAny(n, "productcode", "stylecode", "article", "referenceid", "refid", "styleno"))
                return FieldKind.Id;
            if (ExactOrEnds(n, "name", "title", "description", "label", "caption")
                || ContainsAny(n, "productname", "stylename", "displayname", "fullname"))
                return FieldKind.Title;
            return FieldKind.Meta;
        }

        private static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            return new string(name.Trim().ToLowerInvariant()
                .Where(ch => ch != ' ' && ch != '_' && ch != '-' && ch != '.')
                .ToArray());
        }

        private static bool ExactOrEnds(string n, params string[] needles)
        {
            foreach (var needle in needles)
            {
                if (n == needle || n.EndsWith(needle, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool ContainsAny(string n, params string[] needles)
            => needles.Any(x => n.IndexOf(x, StringComparison.Ordinal) >= 0);

        private static bool LooksLikeUrlOnly(string name)
        {
            var n = (name ?? "").ToLowerInvariant();
            return n.Contains("url") || n.EndsWith("link") || n.Contains("href");
        }

        private enum FieldKind
        {
            Id,
            Title,
            Status,
            Color,
            Date,
            Position,
            Meta
        }

        private sealed class ColScore
        {
            public string Name;
            public int Theme;
            public double Fill;
            public FieldKind Kind;
            public bool ShowLabel;
        }
    }
}
