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
    public partial class AppSearchViewExDto
    {
        [DataMember(EmitDefaultValue = false)]
        public List<AppSearchViewExDto> ChildViewExDtoList { get; set; }
    }


}

