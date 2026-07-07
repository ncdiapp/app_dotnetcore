using System;

namespace APP.Components.EntityDto
{
    public class AppReportTemplateDto
    {
        public int ReportTemplateId { get; set; }
        public int ReportId         { get; set; }
        public string TemplateHtml      { get; set; }
        public string DataSpName        { get; set; }
        public string DataApiPath       { get; set; }
        public string PageSize          { get; set; } = "A4";
        public string Orientation       { get; set; } = "portrait";
        public int    MarginMm          { get; set; } = 15;
        public string ExtraParamConfig  { get; set; }
        public DateTime? CreatedDate    { get; set; }
        public DateTime? ModifiedDate   { get; set; }
    }
}
