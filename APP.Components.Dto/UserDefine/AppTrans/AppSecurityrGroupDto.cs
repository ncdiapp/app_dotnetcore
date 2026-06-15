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



    public partial class AppSecurityGroupDto
    {

        /// <summary> The User Users property for display infor</summary>
        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> Users
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String GroupUserString
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string OrganizationPath
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public bool IsDisableEdit
        //{
        //    get
        //    {
        //        if (InternalCode == EmAppSecurityGroupInernalCode.CustomerAdmin.ToString()
        //               || InternalCode == EmAppSecurityGroupInernalCode.SupplierAdmin.ToString()
        //               || InternalCode == EmAppSecurityGroupInernalCode.ClientAgentAdmin.ToString())
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    set
        //    {

        //    }
        //}
        
        //public bool IsSystemRole
        //{
        //    get
        //    {
        //        if (IsBuiltIn.HasValue && IsBuiltIn.Value)
        //        {
        //            return true;
        //        }
        //        else if (InternalCode == EmAppSecurityGroupInernalCode.CustomerAdmin.ToString()
        //               || InternalCode == EmAppSecurityGroupInernalCode.SupplierAdmin.ToString()
        //               || InternalCode == EmAppSecurityGroupInernalCode.ClientAgentAdmin.ToString())
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }            
        //}

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowedToAddAvailableUser
        {
            get
            {
                return IsSharedbyMutipleCompany.HasValue && IsSharedbyMutipleCompany.Value;
            }
            set
            {
                IsSharedbyMutipleCompany = IsAllowedToAddAvailableUser;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string Display
        {
            get;
            set;
        }
    }
}

