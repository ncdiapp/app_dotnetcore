using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmFolderImportScopePreviewDto
    {
        [DataMember]
        public int PlmFolderType { get; set; }

        [DataMember]
        public string PlmFolderTypeName { get; set; }

        [DataMember]
        public int? AppTransactionId { get; set; }

        [DataMember]
        public int? AppAnchorFolderId { get; set; }

        [DataMember]
        public int TotalPlmFolders { get; set; }

        [DataMember]
        public int ExistingMappedFolders { get; set; }

        [DataMember]
        public int ToCreateCount { get; set; }

        [DataMember]
        public int MissingParentCount { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmFolderImportPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmFolderImportScopePreviewDto> Scopes { get; set; } = new List<PlmFolderImportScopePreviewDto>();

        [DataMember]
        public int ColorDetailSourceCount { get; set; }

        [DataMember]
        public int ColorDetailReadyToImport { get; set; }

        [DataMember]
        public int ColorDetailExistingCount { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmFolderImportResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int FoldersCreated { get; set; }

        [DataMember]
        public int FoldersSkippedExisting { get; set; }

        [DataMember]
        public int MappingsWritten { get; set; }

        [DataMember]
        public int ColorDetailsInserted { get; set; }

        [DataMember]
        public int ColorDetailsSkipped { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmFolderPlacementPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int ProductReadyCount { get; set; }

        [DataMember]
        public int ProductMissingFolderMapCount { get; set; }

        [DataMember]
        public int ColorDetailReadyCount { get; set; }

        [DataMember]
        public int ColorDetailMissingFolderMapCount { get; set; }

        [DataMember]
        public int ImageReadyCount { get; set; }

        [DataMember]
        public int ImageMissingFolderMapCount { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmFolderPlacementResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int ProductsUpdated { get; set; }

        [DataMember]
        public int ColorDetailsUpdated { get; set; }

        [DataMember]
        public int AppFilesUpdated { get; set; }

        [DataMember]
        public int SkippedNoMapping { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();
    }
}
