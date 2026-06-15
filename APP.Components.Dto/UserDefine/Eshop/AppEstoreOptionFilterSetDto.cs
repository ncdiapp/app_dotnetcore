
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
    public  class AppEstoreOptionFilterSetDto
    {
        //key1:OptionLevel  Key2:rowidentityKey
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictOptionCheckedLevel
        {
            get;
            set;
        }


        public int? StoreId
        {
            get;
            set;
        }
       
    }
}
