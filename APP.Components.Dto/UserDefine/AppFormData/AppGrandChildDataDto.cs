using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using System.Data;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class AppGrandChildDataDto
    {

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> CascadingNeedToBeLockedFields
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CascadingUnitId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CascadingFieldId
        {
            get;
            set;
        }

        //key1:child unit fieldid  
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, List<LookupItemDto>> DictCascadingFiledDataSource
        {
            get;
            set;
        }


        [DataMember]
        public bool IsDirty
        {
            get;
            set;
        }

        [DataMember]
        public bool IsNew
        {
            get;
            set;
        }

      



		[DataMember]
        public object  ChildUnitId
        {
            get;
            set;
        }

       
        public string PKValueCombinString
        {
            get;
            set;
        }
    }
}
