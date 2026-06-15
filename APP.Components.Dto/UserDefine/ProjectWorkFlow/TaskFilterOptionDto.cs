using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class TaskFilterOptionDto
    {       

        [DataMember(EmitDefaultValue = false)]
        public List<EmAppTaskDueDateType> SelectedTaskDueDateTypeList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<EmAppProjectTaskProgress> SelectedTaskProgressList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<EmAppProjectTaskPriority> SelectedTaskPriorityList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<EmAppTaskSystemDefinedCategory> SelectedTaskTypeList { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsCurrentUserTaskOnly { get; set; }

        

    }
}
