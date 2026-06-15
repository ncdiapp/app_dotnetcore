using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class UserContactSetDto
    {

        [DataMember(EmitDefaultValue = false)]
        public int? UserId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppSecurityUserContactExDto> UserContactList { get; set; }

       

    }
}
