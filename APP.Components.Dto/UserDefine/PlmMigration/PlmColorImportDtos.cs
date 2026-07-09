using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmColorImportPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int PlmColorRootFolderCount { get; set; }

        [DataMember]
        public string RootFolderStrategy { get; set; }

        [DataMember]
        public int? ResolvedAppRootFolderId { get; set; }

        [DataMember]
        public string ResolvedAppRootFolderName { get; set; }

        [DataMember]
        public List<PlmColorRootFolderPreviewDto> PlmColorRootFolders { get; set; } = new List<PlmColorRootFolderPreviewDto>();

        [DataMember]
        public bool HasRgbColorTable { get; set; }

        [DataMember]
        public int RgbColorRowCount { get; set; }

        [DataMember]
        public int ColorGroupDetailRowCount { get; set; }

        [DataMember]
        public int? ExistingTransactionId { get; set; }

        [DataMember]
        public int? ExistingListSearchId { get; set; }

        [DataMember]
        public int? ExistingFolderTemplateSearchId { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();

        [DataMember]
        public List<string> PlannedActions { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmColorRootFolderPreviewDto
    {
        [DataMember]
        public int PlmFolderId { get; set; }

        [DataMember]
        public string PlmFolderName { get; set; }

        [DataMember]
        public int? AppFolderId { get; set; }

        [DataMember]
        public string AppFolderName { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmColorImportExecuteRequestDto
    {
        [DataMember]
        public int? SessionId { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmColorImportExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string RootFolderStrategy { get; set; }

        [DataMember]
        public int? AppRootFolderId { get; set; }

        [DataMember]
        public int? TransactionId { get; set; }

        [DataMember]
        public int? FormId { get; set; }

        [DataMember]
        public int? ListSearchId { get; set; }

        [DataMember]
        public int? FolderTemplateSearchId { get; set; }

        [DataMember]
        public int? FolderSearchViewId { get; set; }

        [DataMember]
        public int? ListSearchViewId { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();
    }
}
