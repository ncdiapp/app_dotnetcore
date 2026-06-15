using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class RoleAndUserListDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<AppSecurityGroupDto> RoleList { get; set; }      


        [DataMember(EmitDefaultValue = false)]
        public List<AppSecurityUserDto> UserList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<int> OrganizationIdList { get; set; }

    }
}
