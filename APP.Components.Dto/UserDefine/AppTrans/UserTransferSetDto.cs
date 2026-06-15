using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class UserTransferSetDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<int> NeedToTransferUserIdList { get; set; }      


        [DataMember(EmitDefaultValue = false)]
        public int? TargetOrganizationId { get; set; }
        

        [DataMember(EmitDefaultValue = false)]
        public int? TargetDomainId { get; set; }

    }
}
