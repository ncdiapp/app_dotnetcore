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
    public partial class AppTransactionUnitSearchViewFieldMappingDto
    {

        [DataMember(EmitDefaultValue = false)]
        public string DataBaseFieldName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TargetTransFieldDataBaseFieldName
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public bool? IsUnique
        //{
        //    get;
        //    set;
        //}
    }
}