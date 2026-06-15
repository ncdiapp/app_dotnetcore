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

    public partial class AppProjectTeamDto
	{


		[DataMember(EmitDefaultValue = false)]
		public List<int> ParticipateDomainIdList
		{
			get;
			set;
		}

		[DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<AppProjectTeamMemberExDto>> DictDomainOrOrgIdTeamMemberExDto
		{
            get;
            set;
        }

    }
}

