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

    public partial class AppProjectTaskCircularCheckDto
    {
        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowTaskExDto NeedToCheckTask
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppProjectOrWorkFlowExDto AppProjectOrWorkFlowExDto
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectWorkFlowTaskExDto> CircularPath
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppProjectWorkFlowTaskExDto LockingTask
        {
            get;
            set;
        }
    }
}

