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
    public partial class AppListMenuDto : NotifyDataErrorDto 
    {
        [DataMember(EmitDefaultValue = false)]
        public string ImageUrl { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSelectedForDomainOrUser { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public bool IsPackageInstalled { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? InstalledPackageUserDBMenuId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string MenuPath { get; set; }



        [DataMember(EmitDefaultValue = false)]
        public string Param1 { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public string Param2 { get; set; }

    }
}

