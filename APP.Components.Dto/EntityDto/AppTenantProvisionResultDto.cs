using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppTenantProvisionResultDto
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public int? CompanyId { get; set; }

        [DataMember]
        public string DatabaseName { get; set; }

        [DataMember]
        public string LoginUrl { get; set; }

        [DataMember]
        public int MigrationsApplied { get; set; }

        [DataMember]
        public string SeededFromTemplate { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }
    }
}
