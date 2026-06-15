using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class DocumentInfoDto
    {
        public const string QryFileId = "FileId";
		public const string ExcelUploadTableName = "ExcelUploadTableName";
        public const string IsDownloadAsNewFile = "IsDownloadAsNewFile";
        public const string QryVideoId = "VideoId";

        public const string QrySketchHeight = "Height";
        public const string QrySketchWidth = "Width";

        public const string QryIsSaveAsSketch = "IsSaveAsSketch";

		public const string QryReportId = "ReportId";

       

        public const string QryExcelExportType = "ExcelExportType";
        public const string QryLanguageId = "LanguageId";
        public const string QryLanguageKeyType = "LanguageKeyType";

        public const string QryParameter = "Parameter";
        public const string QryQrUrl = "QrUrl";
        public const string QryQrDescription = "QrDescription";



        public const string ImageOriginalSizeLocation = "/original/";
		public const string ImageRegularSizeLocation = "/regular/";
		public const string ImageThumbnailLocation = "/thumbnail/";
		public const string AppTemp = "/temp/";

        public const string FileOrgPath = "FileOrgPath";

      //  public static readonly string  rootFilePath = AppDomain.CurrentDomain.BaseDirectory + "FileRepository";//setupFilePath;

		[DataMember(EmitDefaultValue = false)]
        public int? ReferenceId { get; set; }

		public const string ExternalByteFileGuId = "ExternalByteFileGuId";

	}

	public class ReporttInfoDto
	{
	

		public const string QryReportId = "ReportId";
		public const string QryReportParaPrefix = "RepPara";
		
	}


    public class FormPrintInfoDto
    {


        public const string QryMessageTemplateId = "MessageTemplateId";
        public const string QryTransactionId = "TransactionId";
        public const string QryTransactionRId = "TransactionRId";

    }
}