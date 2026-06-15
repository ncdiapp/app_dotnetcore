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
    public partial class AppApplicationAssetsItemDto 
    {

        [DataMember(EmitDefaultValue = false)]
        public int? PrintFormId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppFormExDto ForeignAppPrintFormExDto
        {
            get;
            set;
        }

    }
}

