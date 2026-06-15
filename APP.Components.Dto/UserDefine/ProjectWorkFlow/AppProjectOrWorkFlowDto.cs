using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{

    public partial class AppProjectOrWorkFlowDto
    {



        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectOrWorkFlowDto> Children
        {
            get;
            set;
        }


        public int CountContent
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> ParticipateDomainIdList
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<AppProjectTeamMemberExDto>> DictDomainOrOrgIdTeamMemberExDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ImportProjectTeamLibaryId
        {
            get;
            set;
        }

        [DataMember]
        public DateTime DueDate
        {
            get; set;
        }

        [DataMember]
        public bool IsForGanttChart
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public EmAppProjectStage? EmAppProjectStage
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ProjectStageDisplay
        {
            get;
            set;
        }



        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? NewWorkflowTransactionId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppTransactionDto ForeignTransactionDto { get; set; }
        



    }
}

