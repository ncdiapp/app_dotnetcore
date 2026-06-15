using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class DataSourceRegisterSetDto
    {
        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppDataSourceRegisterExDto> DataSourceRegisterSet { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<object> DeletedItemIds { get; set; }
        
        
          
    }
}
