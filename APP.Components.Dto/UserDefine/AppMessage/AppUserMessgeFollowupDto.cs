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

    public partial class AppUserMessgeFollowupDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string UserName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UserEmail
        {
            get;
            set;
        }
    }


}

