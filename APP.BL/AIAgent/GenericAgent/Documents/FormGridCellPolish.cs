using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Shared print/export cell text for AI-polished formats (delta / percent / date).
    /// Screen Result uses React Wijmo templates; Office/HTML use this.
    /// </summary>
    public static class FormGridCellPolish
    {
        public static List<FormCell> FormatExportRow(IList<FormCell> cells, IList<string> formats)
        {
            var list = new List<FormCell>();
            var count = cells?.Count ?? 0;
            for (var i = 0; i < count; i++)
            {
                var src = cells[i] ?? new FormCell();
                var format = formats != null && i < formats.Count ? formats[i] : null;
                if (src.IsImage || (src.ImageBytes != null && src.ImageBytes.Length > 0)
                    || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(format), "image", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(src);
                    continue;
                }
                var text = FormatPlainText(src.Text, format);
                list.Add(new FormCell
                {
                    Text = text,
                    IsNumber = src.IsNumber
                               || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(format), "delta", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(format), "percent", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(format), "number", StringComparison.OrdinalIgnoreCase),
                    IsImage = src.IsImage,
                    ImageBytes = src.ImageBytes
                });
            }
            return list;
        }

        /// <summary>Plain-text polish for Excel/Word/CSV-like cells (▲/▼ + sign).</summary>
        public static string FormatPlainText(string text, string format)
        {
            var raw = (text ?? "").Trim();
            if (string.IsNullOrEmpty(raw)) return "";
            var fmt = AiPolishedRenderSpec.NormalizeColumnFormat(format);

            if (string.Equals(fmt, "delta", StringComparison.OrdinalIgnoreCase)
                && TryParseSignedNumber(raw, out var n))
            {
                var neg = n < 0;
                var pos = n > 0;
                var arrow = neg ? "▼" : pos ? "▲" : "•";
                var label = Math.Abs(n).ToString("0.##", CultureInfo.InvariantCulture);
                if (raw.IndexOf('d', StringComparison.OrdinalIgnoreCase) >= 0
                    || raw.IndexOf("day", StringComparison.OrdinalIgnoreCase) >= 0)
                    label += "d";
                return arrow + " " + (neg ? "-" : pos ? "+" : "") + label;
            }

            if (string.Equals(fmt, "percent", StringComparison.OrdinalIgnoreCase)
                && TryParseSignedNumber(raw, out var p))
            {
                if (raw.Contains('%')) return raw;
                return p.ToString("0.##", CultureInfo.InvariantCulture) + "%";
            }

            if (string.Equals(fmt, "date", StringComparison.OrdinalIgnoreCase))
                return CardSheet.FormatDisplayValue(raw);

            return CardSheet.FormatDisplayValue(raw);
        }

        public static bool TryParseSignedNumber(string raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim().TrimEnd('%', 'd', 'D').Replace(",", "").Trim();
            if (s.StartsWith("+", StringComparison.Ordinal)) s = s.Substring(1);
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                   || double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Trusted HTML for none-interactive / print grids (delta colors). Empty → use encoded plain text.
        /// </summary>
        public static string FormatHtml(string text, string format)
        {
            var raw = (text ?? "").Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            var fmt = AiPolishedRenderSpec.NormalizeColumnFormat(format);

            if (string.Equals(fmt, "delta", StringComparison.OrdinalIgnoreCase)
                && TryParseSignedNumber(raw, out var n))
            {
                var neg = n < 0;
                var pos = n > 0;
                var color = neg ? "#dc2626" : pos ? "#16a34a" : "#6b7280";
                var plain = FormatPlainText(raw, "delta");
                return "<span style=\"color:" + color + ";font-weight:600;white-space:nowrap;\">"
                       + WebUtility.HtmlEncode(plain) + "</span>";
            }

            if (string.Equals(fmt, "percent", StringComparison.OrdinalIgnoreCase))
            {
                var plain = FormatPlainText(raw, "percent");
                return "<span style=\"white-space:nowrap;\">"
                       + WebUtility.HtmlEncode(plain) + "</span>";
            }

            if (string.Equals(fmt, "date", StringComparison.OrdinalIgnoreCase))
            {
                var plain = FormatPlainText(raw, "date");
                return "<span style=\"white-space:nowrap;\">"
                       + WebUtility.HtmlEncode(plain) + "</span>";
            }

            return null;
        }
    }
}
