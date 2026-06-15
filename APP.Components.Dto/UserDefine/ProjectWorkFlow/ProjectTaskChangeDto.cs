using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class ProjectTaskChangeDto
    {
       

        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowTaskExDto Task { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppProjectOrWorkFlowExDto Project { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public EmAppProjectTaskChangeType ChangeType { get; set; }
        

        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToRecalculateProject { get; set; }


        
    }
}
