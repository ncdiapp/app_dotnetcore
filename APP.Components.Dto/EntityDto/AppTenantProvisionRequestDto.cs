using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppTenantProvisionRequestDto
    {
        [DataMember]
        public string CompanyName { get; set; }

        [DataMember]
        public string DomainToken { get; set; }

        [DataMember]
        public string AdminEmail { get; set; }

        [DataMember]
        public string AdminLoginName { get; set; }

        [DataMember]
        public string AdminPassword { get; set; }

        // Optional: ID of a registered template DB to copy definitions from.
        [DataMember]
        public string TemplateId { get; set; }
    }
}
