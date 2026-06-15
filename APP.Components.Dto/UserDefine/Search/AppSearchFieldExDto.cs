using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    
   
    public partial class AppSearchFieldExDto 
    {


		public List<AppSearchFieldExDto> CascadngChildren
		{
			get;
			set;
		}


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> Expression
        {
            get;
            set;
        }
    }
}

