using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PushAgentTemplatesRequest
    {
        // DataSourceId of the tenant DB to use as the agent template source.
        [DataMember]
        public int TemplateDataSourceId { get; set; }
    }
}
