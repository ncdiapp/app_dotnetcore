using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Session AI polish output: TemplateContent fragments + field/column structure only.
    /// Must never carry full Result row payloads. See AI-Agent-Result-And-Doc-Export-UI-Layout-Redesign.md.
    /// </summary>
    public sealed class AiPolishedRenderSpec
    {
        public const int CurrentVersion = 1;
        public const int MaxDisplayFields = 8;
        public const int MaxColumnsDefault = 12;
        public const int MaxFilterFields = 6;
        public const int MaxDistinctFilterValues = 50;

        public int Version { get; set; } = CurrentVersion;
        public string Layout { get; set; }
        public string Title { get; set; }
        public bool SelectAllColumns { get; set; }

        /// <summary>Optional map layout→HTML for interactive TemplateContent (session only).</summary>
        public JObject TemplateContentInteractive { get; set; }

        /// <summary>Optional map layout→HTML for none-interactive TemplateContent (session only).</summary>
        public JObject TemplateContentNoneInteractive { get; set; }

        public AiPolishBindings Bindings { get; set; } = new AiPolishBindings();
        public AiPolishLayoutHints LayoutHints { get; set; } = new AiPolishLayoutHints();

        public static bool TryParse(string json, out AiPolishedRenderSpec spec, out string error)
        {
            spec = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "aiPolishedRenderSpec is empty.";
                return false;
            }
            try
            {
                var obj = JObject.Parse(json);
                // Reject accidental full result payloads
                if (obj["rows"] != null || obj["data"] is JArray || obj["records"] is JArray)
                {
                    error = "aiPolishedRenderSpec must not include result rows/data/records (structure only).";
                    return false;
                }

                spec = new AiPolishedRenderSpec
                {
                    // LLMs often emit version/cardsPerRow as 1.0 — Value<int?> throws on float.
                    Version = ReadInt(obj["version"] ?? obj["Version"]) ?? CurrentVersion,
                    Layout = (obj["layout"] ?? obj["Layout"])?.ToString(),
                    Title = (obj["title"] ?? obj["Title"])?.ToString(),
                    SelectAllColumns = ReadBool(obj["selectAllColumns"] ?? obj["SelectAllColumns"]) ?? false,
                    TemplateContentInteractive = obj["templateContentInteractive"] as JObject
                        ?? obj["TemplateContentInteractive"] as JObject,
                    TemplateContentNoneInteractive = obj["templateContentNoneInteractive"] as JObject
                        ?? obj["TemplateContentNoneInteractive"] as JObject
                };

                var bindingsTok = obj["bindings"] ?? obj["Bindings"];
                if (bindingsTok is JObject b)
                    spec.Bindings = ParseBindings(b);

                var hintsTok = obj["layoutHints"] ?? obj["LayoutHints"];
                if (hintsTok is JObject h)
                    spec.LayoutHints = ParseHints(h);

                // Soft-trim oversized AI lists so cache_ui_render_polish does not fail entirely
                // (was: "bindings.fields allows at most 8" → empty polish → cards show 1 field / no image).
                TrimBindingsToLimits(spec);

                error = spec.ValidateStructure();
                if (!string.IsNullOrEmpty(error))
                {
                    spec = null;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "aiPolishedRenderSpec is not valid JSON: " + ex.Message;
                return false;
            }
        }

        public string ValidateStructure()
        {
            if (Bindings == null)
                Bindings = new AiPolishBindings();
            if (Bindings.Fields != null && Bindings.Fields.Count > MaxDisplayFields)
                return "bindings.fields allows at most " + MaxDisplayFields + " fields.";
            if (Bindings.FilterFields != null && Bindings.FilterFields.Count > MaxFilterFields)
                return "bindings.filterFields allows at most " + MaxFilterFields + " filters.";
            if (Bindings.Columns != null && !SelectAllColumns && Bindings.Columns.Count > MaxColumnsDefault)
                return "bindings.columns allows at most " + MaxColumnsDefault + " unless selectAllColumns=true.";
            if (Bindings.Fields != null)
            {
                foreach (var f in Bindings.Fields)
                {
                    if (string.IsNullOrWhiteSpace(f?.Name))
                        return "Each bindings.fields entry needs a name.";
                }
            }
            return null;
        }

        /// <summary>Keep first N fields/columns/filters so oversized AI polish still caches.</summary>
        private static void TrimBindingsToLimits(AiPolishedRenderSpec spec)
        {
            if (spec?.Bindings == null) return;
            if (spec.Bindings.Fields != null && spec.Bindings.Fields.Count > MaxDisplayFields)
                spec.Bindings.Fields = spec.Bindings.Fields.Take(MaxDisplayFields).ToList();
            if (spec.Bindings.FilterFields != null && spec.Bindings.FilterFields.Count > MaxFilterFields)
                spec.Bindings.FilterFields = spec.Bindings.FilterFields.Take(MaxFilterFields).ToList();
            if (!spec.SelectAllColumns && spec.Bindings.Columns != null
                && spec.Bindings.Columns.Count > MaxColumnsDefault)
                spec.Bindings.Columns = spec.Bindings.Columns.Take(MaxColumnsDefault).ToList();
        }

        public string TemplateBody(bool interactive, string layoutKey)
        {
            var map = interactive ? TemplateContentInteractive : TemplateContentNoneInteractive;
            if (map == null || string.IsNullOrWhiteSpace(layoutKey)) return null;
            var token = map[layoutKey] ?? map[layoutKey.ToLowerInvariant()];
            var text = token?.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>Slim JSON for SSE / React chrome (no large template bodies). CamelCase for Result Filter UI.</summary>
        public JObject ToClientJson()
        {
            var ser = new JsonSerializer
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            return new JObject
            {
                ["version"] = Version,
                ["layout"] = Layout,
                ["title"] = Title,
                ["selectAllColumns"] = SelectAllColumns,
                ["bindings"] = JObject.FromObject(Bindings ?? new AiPolishBindings(), ser),
                ["layoutHints"] = JObject.FromObject(LayoutHints ?? new AiPolishLayoutHints(), ser)
            };
        }

        public string ToJson(bool includeTemplates = true)
        {
            var obj = ToClientJson();
            if (includeTemplates)
            {
                if (TemplateContentInteractive != null)
                    obj["templateContentInteractive"] = TemplateContentInteractive;
                if (TemplateContentNoneInteractive != null)
                    obj["templateContentNoneInteractive"] = TemplateContentNoneInteractive;
            }
            return obj.ToString(Formatting.None);
        }

        private static AiPolishBindings ParseBindings(JObject b)
        {
            var bindings = new AiPolishBindings
            {
                PrimaryImageField = (b["primaryImageField"] ?? b["PrimaryImageField"])?.ToString()
            };
            if (b["fields"] is JArray fields || b["Fields"] is JArray)
            {
                var arr = (b["fields"] ?? b["Fields"]) as JArray;
                foreach (var f in arr.OfType<JObject>())
                {
                    bindings.Fields.Add(new AiPolishFieldBinding
                    {
                        Name = (f["name"] ?? f["Name"])?.ToString(),
                        Role = (f["role"] ?? f["Role"])?.ToString() ?? "body",
                        TypographyHint = (f["typographyHint"] ?? f["TypographyHint"])?.ToString(),
                        ShowLabel = ReadBool(f["showLabel"] ?? f["ShowLabel"]) ?? false,
                        Label = (f["label"] ?? f["Label"])?.ToString()
                    });
                }
            }
            if (b["columns"] is JArray cols || b["Columns"] is JArray)
            {
                var arr = (b["columns"] ?? b["Columns"]) as JArray;
                foreach (var c in arr)
                {
                    var name = c?.Type == JTokenType.String
                        ? c.ToString()
                        : (c as JObject)?["name"]?.ToString() ?? (c as JObject)?["Name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name))
                        bindings.Columns.Add(name.Trim());
                }
            }
            if (b["filterFields"] is JArray filters || b["FilterFields"] is JArray)
            {
                var arr = (b["filterFields"] ?? b["FilterFields"]) as JArray;
                foreach (var f in arr.OfType<JObject>())
                {
                    var ff = new AiPolishFilterField
                    {
                        Name = (f["name"] ?? f["Name"])?.ToString(),
                        Control = (f["control"] ?? f["Control"])?.ToString() ?? "multiSelect"
                    };
                    if (f["searchFields"] is JArray sf || f["SearchFields"] is JArray)
                    {
                        var sarr = (f["searchFields"] ?? f["SearchFields"]) as JArray;
                        foreach (var s in sarr)
                        {
                            if (!string.IsNullOrWhiteSpace(s?.ToString()))
                                ff.SearchFields.Add(s.ToString().Trim());
                        }
                    }
                    bindings.FilterFields.Add(ff);
                }
            }
            return bindings;
        }

        private static AiPolishLayoutHints ParseHints(JObject h)
        {
            var hints = new AiPolishLayoutHints
            {
                CardsPerRow = ReadInt(h["cardsPerRow"] ?? h["CardsPerRow"]),
                CardRows = ReadInt(h["cardRows"] ?? h["CardRows"])
            };
            if (h["statusColorMap"] is JObject map || h["StatusColorMap"] is JObject)
            {
                var m = (h["statusColorMap"] ?? h["StatusColorMap"]) as JObject;
                foreach (var p in m.Properties())
                    hints.StatusColorMap[p.Name] = p.Value?.ToString();
            }
            return hints;
        }

        /// <summary>Accepts 3, 3.0, "3", "3.0" (models often emit floats for ints).</summary>
        private static int? ReadInt(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return null;
            try
            {
                switch (token.Type)
                {
                    case JTokenType.Integer:
                        return token.Value<int>();
                    case JTokenType.Float:
                        return (int)Math.Round(token.Value<double>(), MidpointRounding.AwayFromZero);
                    case JTokenType.String:
                        var s = token.Value<string>()?.Trim();
                        if (string.IsNullOrEmpty(s)) return null;
                        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                            return i;
                        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                            return (int)Math.Round(d, MidpointRounding.AwayFromZero);
                        return null;
                    default:
                        if (token.Type == JTokenType.Boolean)
                            return token.Value<bool>() ? 1 : 0;
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool? ReadBool(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return null;
            try
            {
                if (token.Type == JTokenType.Boolean)
                    return token.Value<bool>();
                if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                    return Math.Abs(token.Value<double>()) > 0.0000001;
                if (token.Type == JTokenType.String)
                {
                    var s = token.Value<string>()?.Trim();
                    if (bool.TryParse(s, out var b)) return b;
                    if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(s, "no", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }

    public sealed class AiPolishBindings
    {
        public string PrimaryImageField { get; set; }
        public List<AiPolishFieldBinding> Fields { get; set; } = new List<AiPolishFieldBinding>();
        public List<string> Columns { get; set; } = new List<string>();
        public List<AiPolishFilterField> FilterFields { get; set; } = new List<AiPolishFilterField>();
    }

    public sealed class AiPolishFieldBinding
    {
        public string Name { get; set; }
        public string Role { get; set; } = "body";
        public string TypographyHint { get; set; }
        /// <summary>
        /// When true, render a short label with the value (ambiguous siblings on the same card).
        /// </summary>
        public bool ShowLabel { get; set; }
        /// <summary>Optional short display label when ShowLabel is true; else PrettyLabel(Name).</summary>
        public string Label { get; set; }
    }

    public sealed class AiPolishFilterField
    {
        public string Name { get; set; }
        public string Control { get; set; } = "multiSelect";
        public List<string> SearchFields { get; set; } = new List<string>();
    }

    public sealed class AiPolishLayoutHints
    {
        public int? CardsPerRow { get; set; }
        public int? CardRows { get; set; }
        public Dictionary<string, string> StatusColorMap { get; set; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
