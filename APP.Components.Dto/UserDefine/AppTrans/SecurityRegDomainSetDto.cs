using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class SecurityRegDomainSetDto
    {
        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppSecurityRegDomainExDto> SecurityRegDomainSet { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<object> DeletedItemIds { get; set; }
          
    }
}
