using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityDto
{
    
    public partial class AppProjectTaskResourcePlannedHoursDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? UserId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TaskId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ProjectId
        {
            get;
            set;
        }
    }
}

