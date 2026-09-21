using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSketchImportPreviewDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int SourceSketchCount { get; set; }

        [DataMember]
        public int SourceWithBinaryCount { get; set; }

        [DataMember]
        public int ImageCount { get; set; }

        [DataMember]
        public int FileCount { get; set; }

        [DataMember]
        public int ExistingAppFileCount { get; set; }

        [DataMember]
        public int ReadyToImportCount { get; set; }

        [DataMember]
        public int MissingBinaryCount { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSketchImportResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public int SourceSketchCount { get; set; }

        [DataMember]
        public int InsertedCount { get; set; }

        [DataMember]
        public int SkippedExistingCount { get; set; }

        [DataMember]
        public int SkippedMissingBinaryCount { get; set; }

        [DataMember]
        public int ImageInsertedCount { get; set; }

        [DataMember]
        public int FileInsertedCount { get; set; }

        [DataMember]
        public int FailedCount { get; set; }

        [DataMember]
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>One sketch exported by ExternalDll to a staging folder (binaries on disk, not in JSON).</summary>
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSketchStagingItemDto
    {
        [DataMember]
        public int SketchId { get; set; }

        [DataMember]
        public string SketchCode { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Extension { get; set; }

        [DataMember]
        public int FileType { get; set; }

        [DataMember]
        public bool IsImage { get; set; }

        [DataMember]
        public DateTime? CreatedDate { get; set; }

        [DataMember]
        public DateTime? ModifyDate { get; set; }

        /// <summary>Absolute path under staging for OriginalImage (images).</summary>
        [DataMember]
        public string OriginalStagingPath { get; set; }

        [DataMember]
        public string ThumbnailStagingPath { get; set; }

        [DataMember]
        public string RegularStagingPath { get; set; }

        /// <summary>Absolute path for non-image FileContent blob.</summary>
        [DataMember]
        public string ContentStagingPath { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmSketchStagingManifestDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string StagingRoot { get; set; }

        [DataMember]
        public int SourceSketchCount { get; set; }

        [DataMember]
        public int ExportedCount { get; set; }

        [DataMember]
        public int SkippedExistingCount { get; set; }

        [DataMember]
        public int SkippedMissingBinaryCount { get; set; }

        [DataMember]
        public List<PlmSketchStagingItemDto> Items { get; set; } = new List<PlmSketchStagingItemDto>();
    }
}
