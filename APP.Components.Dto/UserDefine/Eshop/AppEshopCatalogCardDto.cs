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
    public  class AppEshopCatalogCardDto
    {

        public AppEshopCatalogCardDto() 
        {

        }



      

        [DataMember(EmitDefaultValue = false)]
        public Object GroupKey
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSingleSku
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object CardDetailSearchId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object StoreId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object GroupRootKeyMappingSearchFiled
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<string> Display
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string  ImageUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SingleProductSkuNo
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Decimal? Price
        {
            get;
            set;
        }

       // appEshopCatalogCardDto
    }
}
