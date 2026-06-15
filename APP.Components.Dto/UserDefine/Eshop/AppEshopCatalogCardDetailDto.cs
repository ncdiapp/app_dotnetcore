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
    public  class AppEshopCatalogCardDetailDto
    {

        public AppEshopCatalogCardDetailDto() 
        {

        }

        // Key: level,
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> DictOptionLevelLookup
        {
            get;
            set;
        }

        // Key: level,
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, List<LookupItemDto>> OrgDictOptionLevelLookup
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, object> DictOptionLevelSelectedValue
        {
            get;
            set;
        }
        

        //key: level
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, string> DictOptionLable
        {
            get;
            set;
        }

        //key: level
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<int, AppEshopCatalogLevelOptionDto> DictOptionAndDto
        {
            get;
            set;
        }
        

            //Key: Search View ColumnName,product display info
            [DataMember(EmitDefaultValue = false)]
        public Dictionary<string,  List<string>> ProductDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string>  ImageUrl
        {
            get;
            set;
        }

        //Key combine Key, value SkuUnit( DetailID)
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictMatrixKeySku
        {
            get;
            set;
        }

        //Key combine Key, value SkuUnit( DetailID)

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Decimal> DictSkuPrice
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Decimal> DictSkuSelectedQty
        {
            get;
            set;
        }

        //kye1: sku,  key2: FiledDispaly Text, Display
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> DictSkuDescription
        {
            get;
            set;
        }

        //kye1: sku,  key2: FiledDispaly Text, ImageValue
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> DictSkuImageUrl
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictSkuDetailId
        {
            get;
            set;
        }

        //Sku
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, AppEshopBagItemDto> DictSkuAppEshopBagItem
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public int? ChangedOptionLevel
        {
            get;
            set;
        }

        //[DataMember(EmitDefaultValue = false)]
        //public string SkuNo
        //{
        //    get;
        //    set;
        //}

        //[DataMember(EmitDefaultValue = false)]
        //public Decimal? Price
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public bool IsProductHaveMultiSelectOption
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsSelectedQtyReadOnly
        {
            get;
            set;
        }


    }
}
