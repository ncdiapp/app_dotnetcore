using System;
using System.Collections.Generic;
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
                    Version = obj["version"]?.Value<int?>() ?? obj["Version"]?.Value<int?>() ?? CurrentVersion,
                    Layout = (obj["layout"] ?? obj["Layout"])?.ToString(),
                    Title = (obj["title"] ?? obj["Title"])?.ToString(),
                    SelectAllColumns = obj["selectAllColumns"]?.Value<bool?>()
                        ?? obj["SelectAllColumns"]?.Value<bool?>()
                        ?? false,
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

        public string TemplateBody(bool interactive, string layoutKey)
        {
            var map = interactive ? TemplateContentInteractive : TemplateContentNoneInteractive;
            if (map == null || string.IsNullOrWhiteSpace(layoutKey)) return null;
            var token = map[layoutKey] ?? map[layoutKey.ToLowerInvariant()];
            var text = token?.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        /// <summary>Slim JSON for SSE / React chrome (no large template bodies).</summary>
        public JObject ToClientJson()
        {
            return new JObject
            {
                ["version"] = Version,
                ["layout"] = Layout,
                ["title"] = Title,
                ["selectAllColumns"] = SelectAllColumns,
                ["bindings"] = JObject.FromObject(Bindings ?? new AiPolishBindings()),
                ["layoutHints"] = JObject.FromObject(LayoutHints ?? new AiPolishLayoutHints())
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
                        TypographyHint = (f["typographyHint"] ?? f["TypographyHint"])?.ToString()
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
                CardsPerRow = h["cardsPerRow"]?.Value<int?>() ?? h["CardsPerRow"]?.Value<int?>(),
                CardRows = h["cardRows"]?.Value<int?>() ?? h["CardRows"]?.Value<int?>()
            };
            if (h["statusColorMap"] is JObject map || h["StatusColorMap"] is JObject)
            {
                var m = (h["statusColorMap"] ?? h["StatusColorMap"]) as JObject;
                foreach (var p in m.Properties())
                    hints.StatusColorMap[p.Name] = p.Value?.ToString();
            }
            return hints;
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
