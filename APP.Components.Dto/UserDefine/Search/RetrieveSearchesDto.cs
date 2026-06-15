using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using APP.Framework;
using APP.Framework.Collections;
using System.Runtime.Serialization;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{

    [DataContract(Namespace = ContractNamespaces.Dto)]
    public  class RetrieveSearchesDto
    {
         [DataMember(EmitDefaultValue = false)]
        public IEnumerable<SearchDefinitionDto> MySearches { get; set; }

         [DataMember(EmitDefaultValue = false)]
        public IEnumerable<SearchDefinitionDto> SavedSearches { get; set; }

    }
}
