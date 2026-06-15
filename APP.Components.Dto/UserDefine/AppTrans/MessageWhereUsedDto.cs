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
    public class MessageWhereUsedDto : AppMessageExDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? TransactionOrganizedType
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string TransactionName
        //{
        //    get;
        //    set;
        //}
        
                       
        
    }

}

