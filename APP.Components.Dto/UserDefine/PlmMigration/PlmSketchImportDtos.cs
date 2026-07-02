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
}
