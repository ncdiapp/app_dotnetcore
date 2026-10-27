using System;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Sizes one card so the requested rows and columns fill one page.
    /// Font, picture, and field gap shrink together when the card gets shorter.
    /// </summary>
    public static class CardPageFit
    {
        public const double PageWidthMm = 180;
        /// <summary>Shorter than the 267mm A4 content box so borders and a title cannot push the last row onto the next page.</summary>
        public const double PageHeightMm = 250;
        public const double TitleMm = 8;

        public struct Slot
        {
            public double PadMm;
            public double ImageMm;
            public double LabelMm;
            public double FieldMm;
            public double GapMm;
            public int FontPx;
        }

        public static double RowMm(int cardRows)
            => (PageHeightMm - TitleMm) / Math.Max(1, cardRows);

        public static double CardMm(int cardsPerRow)
            => PageWidthMm / Math.Max(1, cardsPerRow);

        public static Slot Measure(double rowMm, double cardMm, int textFields, bool hasImage)
        {
            var fields = Math.Max(1, textFields);
            var pad = Math.Min(2.4, Math.Max(0.8, rowMm * 0.04));
            var usable = Math.Max(8, rowMm - pad * 2);
            var image = hasImage ? Math.Min(usable * 0.34, Math.Max(12, cardMm * 0.72)) : 0;
            var label = hasImage ? Math.Min(4.0, usable * 0.07) : 0;
            var gapShare = 0.2;
            var rest = Math.Max(1, usable - image - label);
            var gap = Math.Min(1.6, rest / fields * gapShare);
            var field = (rest - gap * Math.Max(0, fields - 1)) / fields;
            if (hasImage && field < 3.2)
            {
                var needed = 3.2 * fields + 0.6 * Math.Max(0, fields - 1) + label;
                image = Math.Max(10, usable - needed);
                rest = Math.Max(1, usable - image - label);
                gap = Math.Min(1.2, rest / fields * 0.16);
                field = (rest - gap * Math.Max(0, fields - 1)) / fields;
            }
            if (field < 2.6)
            {
                gap = Math.Max(0.35, Math.Min(gap, field * 0.2));
                field = (rest - gap * Math.Max(0, fields - 1)) / fields;
            }
            var font = field >= 5.4 ? 12 : field >= 4.4 ? 11 : field >= 3.6 ? 10 : field >= 2.8 ? 8 : 7;
            return new Slot
            {
                PadMm = pad,
                ImageMm = image,
                LabelMm = label,
                FieldMm = field,
                GapMm = gap,
                FontPx = font
            };
        }
    }
}
