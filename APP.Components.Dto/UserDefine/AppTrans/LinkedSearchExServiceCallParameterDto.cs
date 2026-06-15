using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class LinkedSearchExServiceCallParameterDto
    {
        public int? CurrentUserId
        {
            get; set;
        }

        public List<object> LinkedSearchSelectedItems
        {
            get; set;
        }

        public List<KeyValuePair<object,object>> LinkedSearchSelectedItemsKeyValuePair
        {
            get; set;
        }

        public int? UnitId
        {
            get; set;
        }

        public AppMasterDetailDto FormData
        {
            get; set;
        }

    }
}
