using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public class NavigationGroupDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Object Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string GroupName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Description
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EmBuseinssScope
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionDto> HeaderTransactionList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionDto> MainTransactionList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppProjectOrWorkFlowDto> ProjectList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> FileIdList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> SearchViewIdList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsDefaultGroup
        {
            get;
            set;
        }





    }
}