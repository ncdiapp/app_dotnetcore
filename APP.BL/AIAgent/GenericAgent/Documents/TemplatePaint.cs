using System;
using System.Text.RegularExpressions;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>Font facts copied from data-role inline styles. Slide geometry stays in PptxForm.</summary>
    public sealed class TemplatePaint
    {
        public int TitlePt { get; set; } = 18;
        public bool TitleBold { get; set; } = true;

        public static TemplatePaint FromHtml(string html)
        {
            var paint = new TemplatePaint();
            var match = Regex.Match(html ?? "", "data-role=\"title\"[^>]*style=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            if (!match.Success)
                return paint;
            var style = match.Groups[1].Value;
            var size = Regex.Match(style, "font-size:\\s*(\\d+(?:\\.\\d+)?)(pt|px)", RegexOptions.IgnoreCase);
            if (size.Success && double.TryParse(size.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n))
            {
                if (size.Groups[2].Value.Equals("px", StringComparison.OrdinalIgnoreCase))
                    n *= 0.75;
                paint.TitlePt = Math.Max(8, (int)Math.Round(n));
            }
            paint.TitleBold = style.IndexOf("font-weight:bold", StringComparison.OrdinalIgnoreCase) >= 0
                || style.IndexOf("font-weight:700", StringComparison.OrdinalIgnoreCase) >= 0;
            return paint;
        }
    }
}
