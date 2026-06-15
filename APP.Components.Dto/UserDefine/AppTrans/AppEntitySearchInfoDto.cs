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
    public partial class AppEntitySearchInfoDto
    {
        [DataMember]
        public int ? TransactionId
        {
            get;
            set;
        }

        [DataMember]
        public object PrimayKeyValue
        {
            get;
            set;
        }


        [DataMember]
        public Dictionary<string,object> DictDisplayFiledValue
        {
            get;
            set;
        }
    }
}