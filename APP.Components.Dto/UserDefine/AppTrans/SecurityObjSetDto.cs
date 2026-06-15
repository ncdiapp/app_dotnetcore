using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public class SecurityObjSetDto
    {
        public ObservableSet<AppSecuritySysObjGroupUserExDto> AppSecuritySysObjGroupUserSet { get; set; }   

        public int AppSecuritySysObjType { get; set; }

        public int? AppSecuritySysObjId { get; set; }

        public int? OrganizationId { get; set; }

        public int? PartnerType { get; set; }
        
        public int? ActionCode { get; set; }
        
        public int? UserType { get; set; }

        public bool IsIgnoreCurrentUserFilterSetup { get; set; }



        //public List<object> DeletedItemIds { get; set; }
    }
}
