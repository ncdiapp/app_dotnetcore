using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppProjectWorkFlowPathDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string PathLabel
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FromTaskRowIdentity
        {
            get;
            set; 
        }

        [DataMember(EmitDefaultValue = false)]
        public string ToTaskRowIdentity
        {
            get;
            set;
        }
        
        //[DataMember(EmitDefaultValue = false)]
        //public AppProjectWorkFlowTaskExDto FromTaskDto
        //{
        //    get;
        //    set;
        //}
        
        //[DataMember(EmitDefaultValue = false)]
        //public AppProjectWorkFlowTaskExDto ToTaskDto
        //{
        //    get;
        //    set;
        //}               

        //[DataMember(EmitDefaultValue = false)]
        //public AppProjectWorkFlowConditionExDto ConditionDto
        //{
        //    get;
        //    set;
        //}
        
        //[DataMember(EmitDefaultValue = false)]
        //public AppProjectWorkFlowActionExDto ActionDto
        //{
        //    get;
        //    set;
        //}

    }
}

