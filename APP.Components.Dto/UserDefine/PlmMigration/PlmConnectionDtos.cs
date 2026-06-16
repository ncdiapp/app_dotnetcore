using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmConnectionTestRequestDto
    {
        [DataMember]
        public string ConnectionString { get; set; }

        [DataMember]
        public int? TargetCompanyId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmConnectionTestResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ServerVersion { get; set; }

        [DataMember]
        public string DatabaseName { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDiscoverDataSourcesRequestDto
    {
        [DataMember]
        public string PlmConnectionString { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }

        [DataMember]
        public int? TargetCompanyId { get; set; }

        [DataMember]
        public int? SessionId { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDataSourceDiscoveryItemDto
    {
        [DataMember]
        public int DataSourceFrom { get; set; }

        [DataMember]
        public string DataSourceFromName { get; set; }

        [DataMember]
        public bool HasConnectionString { get; set; }

        [DataMember]
        public bool ConnectionTestSuccess { get; set; }

        [DataMember]
        public string ConnectionTestMessage { get; set; }

        [DataMember]
        public int? RegisteredDataSourceId { get; set; }

        [DataMember]
        public string RegisteredDataSourceName { get; set; }

        [DataMember]
        public bool IsReusedRegister { get; set; }
    }

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class PlmDiscoverDataSourcesResultDto
    {
        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public List<PlmDataSourceDiscoveryItemDto> DataSources { get; set; } = new List<PlmDataSourceDiscoveryItemDto>();
    }
}
