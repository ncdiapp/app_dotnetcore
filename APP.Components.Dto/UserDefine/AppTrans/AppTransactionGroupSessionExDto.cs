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
    public partial class AppTransactionGroupSessionExDto
    {
        // <TransactionGroupItemId, <transactionId, Rid>>
        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionGroupItemDto> RegularGroupItemList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppTransactionGroupItemDto> HeaderGroupItemList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsPreview
        {
            get;
            set;
        }
    }
}