using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class WorkflowAutomationRuntimeDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string BatchNumber { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int TransactionId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string TransactionRId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string WorkflowProgressStatus { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public DateTime? StartTime { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public DateTime? EndTime { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectWorkFlowActionDto> WorkflowCommandNodeTree
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public string ExecutingNodeLogicId { get; set; }


    }
}