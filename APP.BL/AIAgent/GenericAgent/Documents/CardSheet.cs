using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// One record per card, matching data_render ui=card.
    /// cardsPerRow is 1-6. cardRows is the page height for PDF, Word, and PowerPoint.
    /// </summary>
    public sealed class CardSheet
    {
        public const int MaxCardsPerRow = 6;
        public const int DefaultCardRows = 3;

        public string Title { get; set; } = "";
        public List<List<CardField>> Cards { get; set; } = new List<List<CardField>>();
        public int CardsPerRow { get; set; } = 1;
        public int CardRows { get; set; } = DefaultCardRows;
        public bool CacheLimitReached { get; set; }

        public int CardCount => Cards.Count;

        public List<string> MissingImages { get; set; } = new List<string>();

        public static int ClampPerRow(int value)
        {
            if (value < 1) return 1;
            if (value > MaxCardsPerRow) return MaxCardsPerRow;
            return value;
        }

        public static int ClampPageRows(int value)
        {
            if (value < 1) return DefaultCardRows;
            if (value > 20) return 20;
            return value;
        }

        /// <summary>
        /// Print/export page grid when the tool/user did not pass cardsPerRow / cardRows / pageGrid.
        /// Avoids 1×N strips (one wide short card per row). Catalog-style columns×rows by count.
        /// </summary>
        public static void SuggestPrintGrid(int cardCount, out int cols, out int rows)
        {
            if (cardCount <= 0) { cols = 3; rows = 3; return; }
            if (cardCount == 1) { cols = 1; rows = 1; return; }
            if (cardCount == 2) { cols = 2; rows = 1; return; }
            if (cardCount <= 4) { cols = 2; rows = 2; return; }
            if (cardCount <= 6) { cols = 3; rows = 2; return; }
            if (cardCount <= 9) { cols = 3; rows = 3; return; }
            if (cardCount <= 12) { cols = 3; rows = 4; return; }
            // Many cards: fixed page tile 3×3 (page through).
            cols = 3;
            rows = 3;
        }

        /// <summary>
        /// pageGrid is columns×rows (any positive ints). cardsPerPage derives rows from cardsPerRow when pageGrid is omitted.
        /// </summary>
        public void ApplyPageGrid(string pageGrid, int cardsPerPage)
        {
            if (TryParseGrid(pageGrid, out var columns, out var rows))
            {
                CardsPerRow = ClampPerRow(columns);
                CardRows = rows > 20 ? 20 : rows;
                return;
            }
            if (cardsPerPage > 0)
                CardRows = ClampPageRows((int)Math.Ceiling(cardsPerPage / (double)Math.Max(1, CardsPerRow)));
        }

        public static bool TryParseGrid(string pageGrid, out int columns, out int rows)
        {
            columns = 0;
            rows = 0;
            if (string.IsNullOrWhiteSpace(pageGrid)) return false;
            var match = Regex.Match(pageGrid.Trim(), @"(\d+)\s*[xX×*]\s*(\d+)");
            if (!match.Success) return false;
            if (!int.TryParse(match.Groups[1].Value, out columns) || !int.TryParse(match.Groups[2].Value, out rows))
                return false;
            return columns > 0 && rows > 0;
        }

        public int PageSize => Math.Max(1, CardRows) * Math.Max(1, CardsPerRow);

        public static CardSheet FromRows(string title, JArray rows, int cardsPerRow, int cardRows)
            => FromRows(title, rows, cardsPerRow, cardRows, polish: null);

        /// <summary>
        /// When <paramref name="polish"/> has bindings, only primary image + selected fields are included
        /// (structure-driven; GENERATE still loops all rows).
        /// </summary>
        public static CardSheet FromRows(string title, JArray rows, int cardsPerRow, int cardRows, AiPolishedRenderSpec polish)
        {
            // Tool/user cardsPerRow + cardRows win. Polish LayoutHints must not override explicit page grid.
            var perRow = cardsPerRow;
            var pageRows = cardRows;
            if (perRow <= 0 && polish?.LayoutHints?.CardsPerRow is int cpr && cpr > 0)
                perRow = cpr;
            if (pageRows <= 0 && polish?.LayoutHints?.CardRows is int cr && cr > 0)
                pageRows = cr;
            // Neither tool nor polish set a grid → suggest catalog tile (not 1×N wide strips).
            if (perRow <= 0 && pageRows <= 0)
            {
                SuggestPrintGrid(rows?.Count ?? 0, out perRow, out pageRows);
            }
            else if (perRow <= 0)
            {
                SuggestPrintGrid(rows?.Count ?? 0, out perRow, out _);
            }
            else if (pageRows <= 0)
            {
                pageRows = DefaultCardRows;
            }

            var sheet = new CardSheet
            {
                Title = !string.IsNullOrWhiteSpace(polish?.Title) ? polish.Title : (title ?? ""),
                CardsPerRow = ClampPerRow(perRow),
                CardRows = ClampPageRows(pageRows)
            };
            if (rows == null) return sheet;
            if (rows.Count > DocumentStyles.MaxDataRows)
                throw new InvalidOperationException("Row limit is " + DocumentStyles.MaxDataRows + ".");
            foreach (var token in rows)
            {
                if (token is JObject obj)
                    sheet.Cards.Add(FieldsOf(obj, polish));
            }
            return sheet;
        }

        public static JArray ReadRecords(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("Card records are empty.");
            var token = JToken.Parse(json);
            if (token is JArray arr) return arr;
            if (token is JObject obj)
            {
                if (obj["records"] is JArray records) return records;
                if (obj["rows"] is JArray rows)
                    return RowsToObjects(obj["columns"] as JArray, rows);
            }
            throw new InvalidOperationException("Card records must be a JSON array.");
        }

        public FormDocument AsFlatTable()
        {
            var columns = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var card in Cards)
            {
                foreach (var field in card)
                {
                    if (seen.Add(field.Label))
                        columns.Add(field.Label);
                }
            }
            var data = new List<List<FormCell>>();
            foreach (var card in Cards)
            {
                var line = new List<FormCell>();
                foreach (var name in columns)
                {
                    string text = "";
                    foreach (var field in card)
                    {
                        if (string.Equals(field.Label, name, StringComparison.OrdinalIgnoreCase))
                        {
                            text = field.Value;
                            break;
                        }
                    }
                    line.Add(new FormCell { Text = text });
                }
                data.Add(line);
            }
            return FormDocument.FromTable(Title, columns, data);
        }

        private static JArray RowsToObjects(JArray columns, JArray rows)
        {
            var arr = new JArray();
            var names = new List<string>();
            if (columns != null)
            {
                foreach (var col in columns)
                    names.Add(col.Type == JTokenType.String ? col.ToString() : "");
            }
            foreach (var row in rows)
            {
                if (row is JObject obj)
                {
                    arr.Add(obj);
                    continue;
                }
                if (row is not JArray cells) continue;
                var item = new JObject();
                for (var i = 0; i < cells.Count; i++)
                {
                    var name = i < names.Count && names[i].Length > 0 ? names[i] : "Column" + (i + 1).ToString();
                    item[name] = cells[i];
                }
                arr.Add(item);
            }
            return arr;
        }

        private static List<CardField> FieldsOf(JObject obj, AiPolishedRenderSpec polish = null, bool allowHeuristicFallback = true)
        {
            var fields = new List<CardField>();
            if (obj == null) return fields;

            var bindings = polish?.Bindings;
            var hasSelection = bindings != null
                && ((!string.IsNullOrWhiteSpace(bindings.PrimaryImageField))
                    || (bindings.Fields != null && bindings.Fields.Count > 0));

            if (!hasSelection)
            {
                // No polish bindings → still MCP-shaped (not a full label:value dump).
                var heuristic = AiRenderPolishHeuristics.Build(
                    "cards", "", obj.Properties().Select(p => p.Name).ToList(), new JArray(obj));
                var hb = heuristic?.Bindings;
                var canApply = hb != null
                    && ((!string.IsNullOrWhiteSpace(hb.PrimaryImageField))
                        || (hb.Fields != null && hb.Fields.Count > 0));
                if (canApply)
                    return FieldsOf(obj, heuristic, allowHeuristicFallback: false);
                // Last resort: image columns only (field name means Image + URL).
                foreach (var prop in obj.Properties())
                {
                    var value = ValueText(prop.Value, out var image);
                    if (!(image || IsImageValue(value, prop.Name))) continue;
                    fields.Add(new CardField
                    {
                        Label = prop.Name,
                        Value = value,
                        IsImage = true,
                        Role = "primaryImage"
                    });
                }
                return fields;
            }

            // Primary image first — field name means Image + value is URL.
            var imageProp = FindProperty(obj, bindings.PrimaryImageField);
            if (imageProp != null)
            {
                var value = ValueText(imageProp.Value, out var image);
                var showImage = image || IsImageValue(value, imageProp.Name);
                fields.Add(new CardField
                {
                    Label = imageProp.Name,
                    Value = value ?? "",
                    IsImage = showImage,
                    Role = "primaryImage",
                    TypographyHint = ""
                });
            }

            if (bindings.Fields != null)
            {
                foreach (var b in bindings.Fields)
                {
                    if (b == null || string.IsNullOrWhiteSpace(b.Name)) continue;
                    if (string.Equals(b.Name, bindings.PrimaryImageField, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var prop = FindProperty(obj, b.Name);
                    if (prop == null) continue;
                    var value = ValueText(prop.Value, out var image);
                    var isImage = image || IsImageValue(value, prop.Name);
                    // Empty text → skip (status ::before / color swatch would show a lone dot).
                    if (!isImage && !HasMeaningfulDisplayValue(value)) continue;
                    if (!isImage) value = FormatDisplayValue(value);
                    var displayLabel = !string.IsNullOrWhiteSpace(b.Label)
                        ? ShortenFieldLabel(b.Label)
                        : (b.ShowLabel ? ShortenFieldLabel(prop.Name) : null);
                    var compact = b.Compact
                        || LooksCompactDisplay(displayLabel, value, b.ShowLabel);
                    fields.Add(new CardField
                    {
                        Label = prop.Name,
                        Value = value,
                        IsImage = isImage,
                        Role = string.IsNullOrWhiteSpace(b.Role) ? "body" : b.Role,
                        TypographyHint = b.TypographyHint ?? "",
                        ShowLabel = b.ShowLabel,
                        DisplayLabel = displayLabel,
                        Compact = compact
                    });
                }
            }

            // AI often emits role=body — gallery template only paints idLink/title/status/meta.
            PromoteBodyRolesToGallery(fields);
            DemoteDateStatusPills(fields);

            // Still only one visible text role → rebuild from column heuristic.
            var visible = fields.Count(f => f != null && !f.IsImage && IsGalleryTextRole(f.Role));
            if (allowHeuristicFallback && visible <= 1)
            {
                var heuristic = AiRenderPolishHeuristics.Build(
                    "cards", "", obj.Properties().Select(p => p.Name).ToList(), new JArray(obj));
                if (heuristic?.Bindings?.Fields != null && heuristic.Bindings.Fields.Count > 0)
                    return FieldsOf(obj, heuristic, allowHeuristicFallback: false);
            }
            return fields;
        }

        private static bool IsGalleryTextRole(string role)
        {
            var r = (role ?? "").Trim().ToLowerInvariant();
            return r is "idlink" or "title" or "statuspill" or "status" or "meta" or "colorswatch";
        }

        /// <summary>Map body/empty roles onto idLink → title → statusPill → meta so cards show AI fields.</summary>
        private static void PromoteBodyRolesToGallery(List<CardField> fields)
        {
            if (fields == null || fields.Count == 0) return;
            var text = fields
                .Where(f => f != null && !f.IsImage
                    && !string.Equals(f.Role, "primaryImage", StringComparison.OrdinalIgnoreCase))
                .ToList();
            bool Has(string role) => text.Any(f =>
                string.Equals(f.Role, role, StringComparison.OrdinalIgnoreCase));

            var bodies = text
                .Where(f => string.IsNullOrWhiteSpace(f.Role)
                    || string.Equals(f.Role, "body", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (bodies.Count == 0) return;

            var slot = 0;
            void Take(string role, bool allowMany = false)
            {
                if (!allowMany && Has(role)) return;
                if (slot >= bodies.Count) return;
                bodies[slot].Role = role;
                slot++;
            }

            Take("idLink");
            Take("title");
            Take("colorSwatch");
            // Dates are not status — leave them for meta.
            while (slot < bodies.Count && LooksLikeDateValue(bodies[slot].Value))
                Take("meta", allowMany: true);
            if (slot < bodies.Count && !LooksLikeDateValue(bodies[slot].Value))
                Take("statusPill");
            while (slot < bodies.Count)
                Take("meta", allowMany: true);
        }

        /// <summary>ISO dates must not render as green status pills.</summary>
        private static void DemoteDateStatusPills(List<CardField> fields)
        {
            if (fields == null) return;
            foreach (var f in fields)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.Value)) continue;
                var role = (f.Role ?? "").Trim().ToLowerInvariant();
                if ((role == "statuspill" || role == "status") && LooksLikeDateValue(f.Value))
                    f.Role = "meta";
            }
        }

        /// <summary>Resolve polish field name to a row property (case / space / underscore insensitive).</summary>
        private static JProperty FindProperty(JObject obj, string name)
        {
            if (obj == null || string.IsNullOrWhiteSpace(name)) return null;
            var exact = obj.Property(name);
            if (exact != null) return exact;
            var target = NormalizeFieldKey(name);
            foreach (var p in obj.Properties())
            {
                if (NormalizeFieldKey(p.Name) == target)
                    return p;
            }
            // Soft contains (min length 4): "Product Code" ↔ "Product", "1 Adoption Date" ↔ "Adoption Date"
            if (target.Length >= 4)
            {
                foreach (var p in obj.Properties())
                {
                    var pn = NormalizeFieldKey(p.Name);
                    if (pn.Length >= 4 && (pn.Contains(target) || target.Contains(pn)))
                        return p;
                }
            }
            return null;
        }

        private static string NormalizeFieldKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var chars = name.Trim().ToLowerInvariant()
                .Where(ch => ch != ' ' && ch != '_' && ch != '-' && ch != '.')
                .ToArray();
            return new string(chars);
        }

        private static string ValueText(JToken token, out bool image)
        {
            image = false;
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.Date)
                return FormatDisplayValue(token.Value<DateTime>().ToString("o", CultureInfo.InvariantCulture));
            if (token is JObject obj)
            {
                if (obj["url"] != null)
                {
                    image = true;
                    return obj["url"].ToString();
                }
                if (obj["fileId"] != null)
                {
                    image = true;
                    return obj["fileId"].ToString();
                }
            }
            var raw = token.Type == JTokenType.String ? token.ToString() : token.ToString(Newtonsoft.Json.Formatting.None);
            return FormatDisplayValue(raw);
        }

        private static readonly Regex IsoDateValue = new Regex(
            @"^\d{4}-\d{2}-\d{2}([T ]\d{2}:\d{2}(:\d{2}(\.\d+)?)?(Z|[+-]\d{2}:?\d{2})?)?$",
            RegexOptions.Compiled);

        /// <summary>Column name → short display label (Product_Code → Product Code).</summary>
        public static string PrettyFieldLabel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            var parts = name.Trim().Replace('-', ' ').Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (sb.Length > 0)
                    sb.Append(' ');
                if (part.Length == 1)
                    sb.Append(char.ToUpperInvariant(part[0]));
                else
                    sb.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Card face label: drop redundant Date/Time suffixes (Created Date → Created, Revised_Date → Revised).
        /// </summary>
        public static string ShortenFieldLabel(string nameOrLabel)
        {
            var pretty = PrettyFieldLabel(nameOrLabel);
            if (string.IsNullOrWhiteSpace(pretty)) return "";
            var parts = pretty.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            // Strip trailing noise: Date, Time, Datetime, Dt, On (keep "Created" / "Revised").
            while (parts.Count > 1)
            {
                var last = parts[^1];
                if (last.Equals("Date", StringComparison.OrdinalIgnoreCase)
                    || last.Equals("Time", StringComparison.OrdinalIgnoreCase)
                    || last.Equals("Datetime", StringComparison.OrdinalIgnoreCase)
                    || last.Equals("Timestamp", StringComparison.OrdinalIgnoreCase)
                    || last.Equals("Dt", StringComparison.OrdinalIgnoreCase)
                    || last.Equals("On", StringComparison.OrdinalIgnoreCase))
                {
                    parts.RemoveAt(parts.Count - 1);
                    continue;
                }
                break;
            }
            // "Date Created" → "Created"
            if (parts.Count >= 2 && parts[0].Equals("Date", StringComparison.OrdinalIgnoreCase))
                parts.RemoveAt(0);
            return string.Join(' ', parts);
        }

        /// <summary>
        /// True when label+value are short enough to share a meta row (MCP-style · packing).
        /// </summary>
        public static bool LooksCompactDisplay(string displayLabel, string value, bool showLabel)
        {
            var v = FormatDisplayValue(value ?? "");
            if (string.IsNullOrWhiteSpace(v)) return false;
            var len = v.Length;
            if (showLabel && !string.IsNullOrWhiteSpace(displayLabel))
                len += displayLabel.Trim().Length + 1;
            return len <= 24;
        }

        /// <summary>ISO / DateTime JSON → short display date (e.g. Jun 24, 2026). Other text unchanged.</summary>
        public static string FormatDisplayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var t = value.Trim().Trim('"', '\'');
            // PLM multi-value cells often end with ";" — drop trailing separators for card face.
            while (t.Length > 0 && (t[^1] == ';' || t[^1] == ','))
                t = t[..^1].TrimEnd();
            if (string.IsNullOrWhiteSpace(t)) return "";
            if (!LooksLikeDateValue(t)) return t;
            if (DateTime.TryParse(t, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var dt))
                return dt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
            return t;
        }

        /// <summary>False for blank / punctuation-only — do not paint swatch/status/meta (avoids lone dots).</summary>
        public static bool HasMeaningfulDisplayValue(string value)
        {
            var t = FormatDisplayValue(value);
            if (string.IsNullOrWhiteSpace(t)) return false;
            if (t.Equals("null", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Equals("n/a", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Equals("-", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Equals("—", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        public static bool LooksLikeDateValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var t = value.Trim().Trim('"', '\'');
            return IsoDateValue.IsMatch(t);
        }

        private static readonly Regex ResourceImage = new Regex(
            @"/api/resources/\S+/(?:image|regular|thumbnail)(/|\?|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex FileImage = new Regex(
            @"/api/files/(?:regular|image|thumbnail|latest)/\d+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExtImage = new Regex(
            @"^https?://\S+\.(?:png|jpe?g|gif|webp|svg|bmp)(?:\?\S*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HttpOrPathUrl = new Regex(
            @"^(https?://\S+|/\S+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ImageFieldName = new Regex(
            @"^(image|img|sketch|photo|thumbnail|thumb|picture|pic|artwork|displayimage|display_image)$|(^|[^a-z])(image|sketch|photo|thumb|picture)([^a-z]|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsImageFieldName(string name)
            => !string.IsNullOrWhiteSpace(name) && ImageFieldName.IsMatch(name.Trim());

        public static bool IsUrlValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim().Trim('"', '\'');
            if (text.Length == 0 || text.Length > 2048 || text.IndexOf(' ') >= 0) return false;
            return HttpOrPathUrl.IsMatch(text);
        }

        /// <summary>
        /// Image cell when value is a known app/file/extension image URL,
        /// or (preferred) field name means image AND value is any URL.
        /// </summary>
        public static bool IsImageValue(string value)
            => IsImageValue(value, fieldName: null);

        public static bool IsImageValue(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim().Trim('"', '\'');
            if (IsImageFieldName(fieldName))
            {
                if (IsUrlValue(text)) return true;
                // Numeric AppFile id in an Image/Sketch column.
                if (int.TryParse(text, out var fileId) && fileId > 0) return true;
            }
            return ResourceImage.IsMatch(text) || FileImage.IsMatch(text) || ExtImage.IsMatch(text);
        }

        /// <summary>
        /// True when a card/grid cell should render as an image (bytes, flagged IsImage+URL,
        /// image field name+URL, or known app/file/extension URL).
        /// </summary>
        public static bool IsRenderableImage(string value, string fieldName = null, bool flaggedIsImage = false)
        {
            if (flaggedIsImage && IsUrlValue(value)) return true;
            return IsImageValue(value, fieldName);
        }
    }

    public sealed class CardField
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public bool IsImage { get; set; }
        public byte[] ImageBytes { get; set; }
        /// <summary>AI polish role: idLink, title, statusPill, meta, colorSwatch, body, primaryImage.</summary>
        public string Role { get; set; } = "body";
        public string TypographyHint { get; set; } = "";
        /// <summary>When true, render DisplayLabel (or PrettyLabel) with the value.</summary>
        public bool ShowLabel { get; set; }
        /// <summary>Optional short label from polish; when null and ShowLabel, use column Label.</summary>
        public string DisplayLabel { get; set; }
        /// <summary>Short value — may share a meta row with siblings.</summary>
        public bool Compact { get; set; }
    }
}
