using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class TemplateFolderNavigationConfigDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TemplateSearchId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? HostTransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? RootFolderId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchViewId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableFolderSecurity { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class TemplateFolderNavigationConfigResultDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TemplateSearchId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string TemplateSearchName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? HostTransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? RootFolderId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? SearchViewId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsConfigured { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsEnableFolderSecurity { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class FolderNavigationRuntimeContextDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsTemplateMode { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TemplateSearchId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string TemplateSearchName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? HostTransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultFolderViewId { get; set; }
    }
}
