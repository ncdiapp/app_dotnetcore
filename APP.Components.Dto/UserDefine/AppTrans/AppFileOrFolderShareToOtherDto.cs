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
    public partial class AppFileOrFolderShareToOtherDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedNotifyUser
        {
            get;
            set;
        }        
    }
}

