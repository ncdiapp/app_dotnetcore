using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppViewFiledSearchFiledMappingSetDto
    {
        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppViewFiledSearchFiledMappingExDto> AppViewFiledSearchFiledMappingSet { get; set; }

        public int? SearchViewId { get; set; }
          
    }
}
