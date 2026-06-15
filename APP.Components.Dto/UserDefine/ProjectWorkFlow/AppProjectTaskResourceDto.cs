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


    public partial class AppProjectTaskResourceDto
    {
      

        [DataMember(EmitDefaultValue = false)]
        public double ActualWorkHours
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double PlannedCost
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double ActualCost
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public double AvailableHours
        {
            get;
            set;
        }
    }
}

