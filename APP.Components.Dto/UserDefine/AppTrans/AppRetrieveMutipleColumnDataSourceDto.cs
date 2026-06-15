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
    public class AppRetrieveMutipleColumnDataSourceDto
    {

        [DataMember(EmitDefaultValue = false)]
        public AppChildDataDto InputRowData
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public  int   RetrieveDataFiledID
        {
            get;
            set;
        }

        // for one-to-one relationship
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> ReturnRowData
        {
            get;
            set;
        }

        // For one -to-many relationship

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictReturnFieldDataSet
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string WarningMessage
        {
            get;
            set;
        }
      
    }
}
