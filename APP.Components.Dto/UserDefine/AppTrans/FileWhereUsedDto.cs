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
    public class FileWhereUsedInFormDto
    {

        [DataMember(EmitDefaultValue = false)]
        public int? FileId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string FileCode
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? FileType
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public TransactionFieldValueDto TransactionFieldValue
        {
            get;
            set;
        }
       
    }

}

