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

    public partial class AppSearchDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public bool NeedToCreateDefaultViewWithAllDataSetColumns {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? DataServiceType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DataServiceTypeDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsNeedToSynchronizeDefaultViewDataSetId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DataSourceFrom
        {
            get;
            set;
        }
    }
}

