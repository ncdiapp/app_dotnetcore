using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
namespace APP.Components.EntityDto
{
    public  class AppEshopCatalogLevelOptionDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int Level
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string OptionLabel
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsMultipleSelection
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool EmOptionLayoutType
        {
            get;
            set;
        }

    }
}
