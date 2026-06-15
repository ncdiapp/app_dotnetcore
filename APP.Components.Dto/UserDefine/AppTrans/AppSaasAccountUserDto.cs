using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{


    public partial class AppSaasAccountUserDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string SaasAccountUserId
        {
            get;
            set;
        }



    }
}