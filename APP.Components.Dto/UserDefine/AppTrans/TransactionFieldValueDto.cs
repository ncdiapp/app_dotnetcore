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
    public class TransactionFieldValueDto: AppTransactionFieldDto
    {                      
       
        [DataMember(EmitDefaultValue = false)]
        public int? TransactionRId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FolderTransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FolderId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? FolderPath
        {
            get;
            set;
        }
    }

}

