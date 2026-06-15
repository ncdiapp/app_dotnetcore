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
	
	public partial class AppSecuritySysObjGroupUserDto
	{
        [DataMember(EmitDefaultValue = false)]
        public String Display
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public String UnitUiName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsRestrict
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string ResourceString
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String CommandUiName
        {
            get;
            set;
        }
    }
}

