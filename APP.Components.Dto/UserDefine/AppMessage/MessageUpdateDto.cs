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
   
    public partial class MessageUpdateDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsRead
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<int> MessgeIdList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsDeleteReceivedMessage
        {
            get;
            set;
        }

        
    }


}

