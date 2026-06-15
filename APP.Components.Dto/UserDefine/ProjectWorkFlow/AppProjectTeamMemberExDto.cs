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
   
    public partial class AppProjectTeamMemberExDto  
    {
		public int DomainId
		{
			get;set;
		}

		public int OrganizationId 
		{
			get; set;
		}

	}
}

