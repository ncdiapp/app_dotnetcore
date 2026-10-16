namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Fixed presentation for every level. Callers cannot override it.
    /// </summary>
    public static class DocumentStyles
    {
        public const int MaxDataRows = 5000;
        public const int MaxPreviewRows = 500;
        public const int MaxColumns = 50;
        public const int MaxSectionLevel = 3;
        public const int PdfTextCap = 12000;
        public const int PptRowsPerSlide = 12;

        public const string FontFamily = "Arial";
        public const string HtmlFont = "Arial,Helvetica,sans-serif";

        public const string Ink = "#1a1a1a";
        public const string Label = "#555555";
        public const string Notes = "#333333";
        public const string HeaderInk = "#ffffff";
        public const string SectionFill = "#f0f2f4";
        public const string HeaderFill = "#3d4f5f";
        public const string Line = "#dddddd";

        public const string TitleCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:18pt;font-weight:bold;text-align:center;color:#1a1a1a;padding:6px 8px;";

        public const string SectionCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:13pt;font-weight:bold;text-align:left;color:#1a1a1a;background-color:#f0f2f4;padding:6px 8px;";

        public const string NestedCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:12pt;font-weight:bold;text-align:left;color:#1a1a1a;padding:6px 8px;";

        public const string LabelCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:10pt;font-weight:bold;text-align:left;color:#555555;padding:3px 8px;vertical-align:top;width:18%;";

        public const string ValueCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:10pt;font-weight:normal;text-align:left;color:#1a1a1a;padding:3px 8px;vertical-align:top;width:32%;";

        public const string HeaderCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:10pt;font-weight:bold;text-align:left;color:#ffffff;background-color:#3d4f5f;padding:4px 8px;border:1px solid #dddddd;";

        public const string CellCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:10pt;font-weight:normal;text-align:left;color:#1a1a1a;padding:3px 8px;border:1px solid #dddddd;";

        public const string NumberCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:10pt;font-weight:normal;text-align:right;color:#1a1a1a;padding:3px 8px;border:1px solid #dddddd;";

        public const string NotesCss =
            "font-family:Arial,Helvetica,sans-serif;font-size:10pt;font-weight:normal;text-align:left;color:#333333;padding:6px 8px;";

        public const string BlankCss = "font-size:8pt;line-height:8pt;padding:0;height:8px;";
    }
}
