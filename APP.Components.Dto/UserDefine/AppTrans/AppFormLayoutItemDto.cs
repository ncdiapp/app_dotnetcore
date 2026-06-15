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
    public partial class AppFormLayoutItemDto
    {      

        [DataMember(EmitDefaultValue = false)]
        public AppFormDomAttributeDto DomAttribute
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsInvalid
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string CurrentHostId
        //{
        //    get;
        //    set;
        //}


        //[DataMember(EmitDefaultValue = false)]
        //public string ParentHostId
        //{
        //    get;
        //    set;
        //}

      
        public int ItemRuntimeOrder
        {
            get;
            set;
        }
        
    }
}

