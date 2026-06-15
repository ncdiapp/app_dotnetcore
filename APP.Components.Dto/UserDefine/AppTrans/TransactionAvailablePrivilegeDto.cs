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
    public class TransactionPrivilegeDto
    {
        [DataMember(EmitDefaultValue = false)]
        public LookupItemDto Transaction
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppSecuritySysObjGroupUserDto> TransactionActionPrivileges
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppSecuritySysObjGroupUserDto> TransactionUnitActionPrivileges
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppSecuritySysObjGroupUserDto> TransactionUnitPrivileges
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppSecuritySysObjGroupUserDto> TransactionFieldPrivileges
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppSecuritySysObjGroupUserDto> TransactionCommandPrivileges
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? UserType
        {
            get;
            set;
        }

    }
}

