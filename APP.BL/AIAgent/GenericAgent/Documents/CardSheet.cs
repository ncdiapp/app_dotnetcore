using System;
using System.Collections.Generic;
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
        /// pageGrid "2x2" is two columns and two rows. cardsPerPage wins over a cardRows value the model set to 1.
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
        {
            var sheet = new CardSheet
            {
                Title = title ?? "",
                CardsPerRow = ClampPerRow(cardsPerRow),
                CardRows = ClampPageRows(cardRows)
            };
            if (rows == null) return sheet;
            if (rows.Count > DocumentStyles.MaxDataRows)
                throw new InvalidOperationException("Row limit is " + DocumentStyles.MaxDataRows + ".");
            foreach (var token in rows)
            {
                if (token is JObject obj)
                    sheet.Cards.Add(FieldsOf(obj));
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

        private static List<CardField> FieldsOf(JObject obj)
        {
            var fields = new List<CardField>();
            foreach (var prop in obj.Properties())
            {
                var value = ValueText(prop.Value, out var image);
                fields.Add(new CardField
                {
                    Label = prop.Name,
                    Value = value,
                    IsImage = image || IsImageValue(value)
                });
            }
            return fields;
        }

        private static string ValueText(JToken token, out bool image)
        {
            image = false;
            if (token == null || token.Type == JTokenType.Null) return "";
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
            return token.Type == JTokenType.String ? token.ToString() : token.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static readonly Regex ResourceImage = new Regex(@"/api/resources/\S+/image/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex FileImage = new Regex(@"/api/files/(?:regular|image|thumbnail|latest)/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExtImage = new Regex(@"^https?://\S+\.(?:png|jpe?g|gif|webp|svg|bmp)(?:\?\S*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsImageValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim();
            return ResourceImage.IsMatch(text) || FileImage.IsMatch(text) || ExtImage.IsMatch(text);
        }
    }

    public sealed class CardField
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public bool IsImage { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
