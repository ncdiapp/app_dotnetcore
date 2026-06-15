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

    public partial class AppEshopCatalogViewDto
    {

        //key1:OptionLevel  Key2:rowidentityKey
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictOptionLevel
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictOptionDisplay
        {
            get;
            set;
        }


        [DataMember]
        public int TotalOptionNumber
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppEshopCatalogCardDto> CardList
        {
            get;
            set;
        }




    }
}
