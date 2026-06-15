using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{

    public partial class AppBusinessPartnerExDto
    {




        /// <summary> Gets the Entity Dto Collection with the related entities of type 'AppTransactionGroupEntity' which are related to this entity via a relation of type '1:n'.
        /// If the Entity Dto Collection hasn't been fetched yet, the collection returned will be empty.</summary>
        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public List<AppSecurityUserDto> BusinessPartnerUserList
        {
            get; set;
        }


        [DataMember(EmitDefaultValue = false), EditableMemberAttribute]
        public List<LookupItemDto> ExternalBusinessEntityLookUpItemList
        {
            get; set;
        }






    }
}

