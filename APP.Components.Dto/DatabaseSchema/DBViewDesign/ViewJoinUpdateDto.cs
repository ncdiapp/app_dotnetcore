using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;

namespace APP.Components.EntityDto
{
    public class ViewJoinUpdateDto : DatabaseViewDto
    {
        [DataMember(EmitDefaultValue = false)]
        public DatabaseViewJoinConditionDto NeedToAddJoinCondition { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<Guid> NeedToRemoveJoinConditionGUIDs { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public DatabaseViewJoinConditionDto NeedToUpdateJoinMethodConditionDto { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsUpdateJoinMethodForLeftTable { get; set; }


    }
}





