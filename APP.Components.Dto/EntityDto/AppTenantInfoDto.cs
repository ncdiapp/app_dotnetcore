using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppTenantInfoDto
    {
        [DataMember]
        public bool IsFound { get; set; }

        [DataMember]
        public string CompanyName { get; set; }

        [DataMember]
        public string DomainToken { get; set; }

        [DataMember]
        public string CustomDomain { get; set; }
    }
}
