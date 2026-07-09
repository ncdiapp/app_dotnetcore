using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmPomImportPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public bool HasBodyPartTable { get; set; }

        [DataMember]
        public int BodyPartRowCount { get; set; }

        [DataMember]
        public bool HasBodyTypeTable { get; set; }

        [DataMember]
        public int BodyTypeRowCount { get; set; }

        [DataMember]
        public bool HasBodyTypeDetailTable { get; set; }

        [DataMember]
        public int BodyTypeDetailRowCount { get; set; }

        [DataMember]
        public int BodyTypeDetailSourceRowCount { get; set; }

        [DataMember]
        public bool HasSpecBodyPartGradingTable { get; set; }

        [DataMember]
        public int SpecBodyPartGradingRowCount { get; set; }

        [DataMember]
        public int SpecBodyPartGradingSourceRowCount { get; set; }

        [DataMember]
        public int PlmPomRootFolderCount { get; set; }

        [DataMember]
        public int? PomAppRootFolderId { get; set; }

        [DataMember]
        public string PomAppRootFolderName { get; set; }

        [DataMember]
        public List<PlmPomRootFolderPreviewDto> PlmPomRootFolders { get; set; } = new List<PlmPomRootFolderPreviewDto>();

        [DataMember]
        public int PlmPomTemplateRootFolderCount { get; set; }

        [DataMember]
        public int? PomTemplateAppRootFolderId { get; set; }

        [DataMember]
        public string PomTemplateAppRootFolderName { get; set; }

        [DataMember]
        public List<PlmPomRootFolderPreviewDto> PlmPomTemplateRootFolders { get; set; } = new List<PlmPomRootFolderPreviewDto>();

        [DataMember]
        public int? ExistingPomTransactionId { get; set; }

        [DataMember]
        public int? ExistingPomListSearchId { get; set; }

        [DataMember]
        public int? ExistingPomFolderSearchId { get; set; }

        [DataMember]
        public int? ExistingPomTemplateTransactionId { get; set; }

        [DataMember]
        public int? ExistingPomTemplateListSearchId { get; set; }

        [DataMember]
        public int? ExistingPomTemplateFolderSearchId { get; set; }

        [DataMember]
        public int PomFolderIdReadyToRemap { get; set; }

        [DataMember]
        public int PomFolderIdUnmappedCount { get; set; }

        [DataMember]
        public int PomTemplateFolderIdReadyToRemap { get; set; }

        [DataMember]
        public int PomTemplateFolderIdUnmappedCount { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();

        [DataMember]
        public List<string> PlannedActions { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmPomRootFolderPreviewDto
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
    public class PlmPomImportExecuteRequestDto
    {
        [DataMember]
        public int? SessionId { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }

        [DataMember]
        public bool ImportJunctionTables { get; set; } = true;

        [DataMember]
        public bool ImportFoldersIfMissing { get; set; } = true;
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmPomImportExecuteResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int? PomTransactionId { get; set; }

        [DataMember]
        public int? PomFormId { get; set; }

        [DataMember]
        public int? PomListSearchId { get; set; }

        [DataMember]
        public int? PomFolderSearchId { get; set; }

        [DataMember]
        public int? PomAppRootFolderId { get; set; }

        [DataMember]
        public int? PomTemplateTransactionId { get; set; }

        [DataMember]
        public int? PomTemplateFormId { get; set; }

        [DataMember]
        public int? PomTemplateListSearchId { get; set; }

        [DataMember]
        public int? PomTemplateFolderSearchId { get; set; }

        [DataMember]
        public int? PomTemplateAppRootFolderId { get; set; }

        [DataMember]
        public int BodyTypeDetailRowsImported { get; set; }

        [DataMember]
        public int SpecBodyPartGradingRowsImported { get; set; }

        [DataMember]
        public int FoldersImported { get; set; }

        [DataMember]
        public int PomFolderIdsRemapped { get; set; }

        [DataMember]
        public int PomTemplateFolderIdsRemapped { get; set; }

        [DataMember]
        public List<string> Messages { get; set; } = new List<string>();
    }
}
