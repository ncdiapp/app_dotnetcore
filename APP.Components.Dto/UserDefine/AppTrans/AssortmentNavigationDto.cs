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
    public class AssortmentNavigationDto
    {
        [DataMember(EmitDefaultValue = false)]
        public Object Id
        {
            get;
            set;
        }
        
        [DataMember(EmitDefaultValue = false)]
        public object TransactionRId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<NavigationGroupDto> NavigationGroupList
        {
            get;
            set;
        }
               

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultGroupId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultGroupType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultOpenItemId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<int> TransactionIdList
        {
            get;
            set;
        }

    }
}