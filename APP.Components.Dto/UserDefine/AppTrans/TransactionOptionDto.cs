using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class TransactionOptionDto
    {
        public bool IsApiIntegrationTransaction { get; set; }

        public int? ApiIntegrationTransactionCrudType { get; set; }

        public string ApiLogicKeyParameterName { get; set; }        

        public bool IsEnableSaveByApiCall { get; set; }

        public int? SaveByApiCallDataTransferId { get; set; }

        public bool NeedToRefreshAfterSaveByApiCall { get; set; }

        public bool NeedToSaveFormAfterPublishByApiCall { get; set; }        

        public bool IsEnableDeleteByApi { get; set; }

        public int? DeleteDataTransferId { get; set; }

        public int? DeleteApiId { get; set; }

        public int? ErDiagramId { get; set; }

        public int? ImportSettingId { get; set; }

        public bool IsDraft { get; set; }

        public int? TransactionDataUpdateImportSettingId { get; set; }        

        public int? CommunicationGroupByType { get; set; }

        public int? CommunicationFilterByType { get; set; }

        public int? CommunicationToUserIdTransField { get; set; }

        public bool IsOpenCommunicationByDefault { get; set; }

        public bool IsEnableTransactionAiAgent { get; set; }

        public bool IsOpenAiAgentByDefault { get; set; }

        /// <summary>Right or Bottom when AI is on and Communication is off. Communication dock wins when both are on.</summary>
        public int? AiAgentDockPosition { get; set; }

       
        //public List<AppProjectWorkFlowActionExDto> WorkflowAutomationCommandList { get; set; }

    }
}
