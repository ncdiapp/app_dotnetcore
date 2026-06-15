using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppTransactionGroupItemDto
    {
        // <TransactionGroupItemId, <transactionId, Rid>>
        [DataMember(EmitDefaultValue = false)]
        public int? TransId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object TransRid
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DisplayName
        {
            get;
            set;
        }
    }
}