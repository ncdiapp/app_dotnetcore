using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    public partial class APIPostResponseDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string SqlUpdateScript { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ResponseJsonData { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string ResponseJsonSchema { get; set; }
    }
}
