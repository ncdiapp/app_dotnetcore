using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public class SearchDefinitionDto : ObservableObject
    {
        [DataMember(EmitDefaultValue = false)]
        public string Display { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSavedSearch { get; set; }
        //    public bool? IsBuiltIn { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public bool? IsStaticBuiltInSearch { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public virtual int? BlqueryId { get; set; }

        // sometime called scope , or RefTxtype
        [DataMember(EmitDefaultValue = false)]
        public int SearchType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? TechPackTypeId
        {
            get;
            set;
        }

        public bool IsSearchGroup
        {
            get;
            set;
        }

        public bool IsOpenByDefault
        {
            get;
            set;
        }
        
        public List<SearchDefinitionDto> ChildSearchDefinitionDtoList { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? FolderTransactionId
        {
            get;
            set;
        }


        

    }
}