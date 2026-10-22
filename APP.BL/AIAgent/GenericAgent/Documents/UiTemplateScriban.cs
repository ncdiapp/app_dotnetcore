using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Scriban;
using Scriban.Runtime;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Safe Scriban merge for TemplateContent Views (MVC-style).
    /// String fields are HTML-encoded before bind.
    /// </summary>
    public static class UiTemplateScriban
    {
        public static string Render(string templateText, object model)
        {
            if (string.IsNullOrWhiteSpace(templateText))
                return "";
            var template = Template.Parse(templateText);
            if (template.HasErrors)
            {
                var err = string.Join("; ", template.Messages.Select(m => m.Message));
                throw new InvalidOperationException("Template parse error: " + err);
            }

            var scriptObject = new ScriptObject();
            if (model != null)
                scriptObject.Import(model, renamer: member => member.Name);

            var context = new TemplateContext
            {
                MemberRenamer = member => member.Name,
                EnableRelaxedMemberAccess = true,
                StrictVariables = false
            };
            context.PushGlobal(scriptObject);
            return template.Render(context) ?? "";
        }

        public static object ModelFromForm(FormDocument doc)
        {
            doc ??= new FormDocument();
            FormTable table = null;
            if (doc.Sections != null)
            {
                foreach (var sec in doc.Sections)
                {
                    if (sec?.Tables == null) continue;
                    foreach (var t in sec.Tables)
                    {
                        if (t != null) { table = t; break; }
                    }
                    if (table != null) break;
                }
            }

            var columns = table?.Columns ?? new List<string>();
            var rows = new List<object>();
            if (table?.Rows != null)
            {
                foreach (var row in table.Rows)
                {
                    var cells = new List<object>();
                    if (row != null)
                    {
                        foreach (var c in row)
                        {
                            cells.Add(new
                            {
                                text = Enc(c?.Text),
                                is_image = c?.IsImage == true,
                                is_number = c?.IsNumber == true
                            });
                        }
                    }
                    rows.Add(cells);
                }
            }
            return new
            {
                title = Enc(doc.Title),
                columns = columns.Select(Enc).ToList(),
                rows
            };
        }

        public static object ModelFromCards(CardSheet sheet)
        {
            sheet ??= new CardSheet();
            var cards = new List<object>();
            if (sheet.Cards != null)
            {
                foreach (var cardFields in sheet.Cards)
                {
                    var fields = new List<object>();
                    string cardTitle = "";
                    if (cardFields != null)
                    {
                        foreach (var f in cardFields)
                        {
                            if (string.IsNullOrEmpty(cardTitle) && !string.IsNullOrWhiteSpace(f?.Label))
                                cardTitle = f.Label;
                            // Image = role/flag + field name means Image + value is URL (PLM /image/300 OK).
                            string imageSrc = null;
                            if (f?.ImageBytes != null && f.ImageBytes.Length > 0)
                                imageSrc = "data:" + ImageMime(f.ImageBytes) + ";base64,"
                                           + Convert.ToBase64String(f.ImageBytes);
                            else if (f != null && (f.IsImage || CardSheet.IsImageValue(f.Value, f.Label)
                                     || (string.Equals(f.Role, "primaryImage", StringComparison.OrdinalIgnoreCase)
                                         && CardSheet.IsUrlValue(f.Value))))
                                imageSrc = (f.Value ?? "").Trim().Trim('"', '\'');
                            var isImage = !string.IsNullOrEmpty(imageSrc);
                            fields.Add(new
                            {
                                label = Enc(f?.Label),
                                value = Enc(f?.Value),
                                is_image = isImage,
                                image_src = EncAttr(imageSrc),
                                role = Enc(string.IsNullOrWhiteSpace(f?.Role) ? "body" : f.Role),
                                typography_hint = Enc(f?.TypographyHint ?? "")
                            });
                            if (string.Equals(f?.Role, "title", StringComparison.OrdinalIgnoreCase)
                                && !string.IsNullOrWhiteSpace(f?.Value))
                                cardTitle = f.Value;
                            else if (string.Equals(f?.Role, "idLink", StringComparison.OrdinalIgnoreCase)
                                     && string.IsNullOrEmpty(cardTitle)
                                     && !string.IsNullOrWhiteSpace(f?.Value))
                                cardTitle = f.Value;
                        }
                        // Prefer first non-image field value as title when roles did not set one
                        if (string.IsNullOrEmpty(cardTitle) && cardFields.Count > 0)
                        {
                            var firstText = cardFields.FirstOrDefault(x => x != null && !x.IsImage && !string.IsNullOrWhiteSpace(x.Value));
                            if (firstText != null) cardTitle = firstText.Value;
                        }
                    }
                    cards.Add(new
                    {
                        title = Enc(cardTitle),
                        fields
                    });
                }
            }
            return new
            {
                title = Enc(sheet.Title),
                cards_per_row = sheet.CardsPerRow > 0 ? sheet.CardsPerRow : 3,
                cards
            };
        }

        public static object ModelFromRows(string title, JArray rows)
        {
            rows ??= new JArray();
            var columns = new List<string>();
            if (rows.Count > 0 && rows[0] is JObject first)
                columns.AddRange(first.Properties().Select(p => p.Name));

            var rowModels = new List<object>();
            foreach (var token in rows)
            {
                if (token is not JObject obj) continue;
                var cells = columns.Select(col =>
                {
                    var v = obj[col];
                    return new
                    {
                        text = Enc(v == null || v.Type == JTokenType.Null ? "" : v.ToString()),
                        is_image = false,
                        is_number = v != null && (v.Type == JTokenType.Integer || v.Type == JTokenType.Float)
                    };
                }).Cast<object>().ToList();
                rowModels.Add(cells);
            }

            return new
            {
                title = Enc(title),
                columns = columns.Select(Enc).ToList(),
                rows = rowModels,
                rows_json = rows.ToString(Newtonsoft.Json.Formatting.None)
            };
        }

        public static object ModelFromKpis(string title, JArray rows, IEnumerable<UiKpiSpec> kpis)
        {
            rows ??= new JArray();
            var items = new List<object>();
            if (kpis != null)
            {
                foreach (var k in kpis.Take(UiRenderSpec.MaxKpis))
                {
                    if (k == null) continue;
                    var label = string.IsNullOrWhiteSpace(k.Label) ? (k.Field ?? "") : k.Label;
                    items.Add(new
                    {
                        label = Enc(label),
                        value = Enc(UiDashboardHtml.ComputeKpi(rows, k.Field, k.Op)),
                        field = Enc(k.Field ?? ""),
                        op = Enc(k.Op ?? "")
                    });
                }
            }
            return new
            {
                title = Enc(title),
                items
            };
        }

        private static string Enc(string s) => WebUtility.HtmlEncode(s ?? "");

        private static string EncAttr(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return WebUtility.HtmlEncode(s);
        }

        private static string ImageMime(byte[] bytes)
        {
            if (bytes != null && bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes != null && bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
            if (bytes != null && bytes.Length >= 3 && bytes[0] == (byte)'G' && bytes[1] == (byte)'I') return "image/gif";
            return "application/octet-stream";
        }
    }
}
