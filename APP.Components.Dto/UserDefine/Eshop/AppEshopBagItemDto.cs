

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
    public class AppEshopBagItemDto
    {

        public AppEshopBagItemDto()
        {

        }



        [DataMember(EmitDefaultValue = false)]
        public string ImageUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ProductDisplay
        {
            get;
            set;
        }

        //(Red, X)
        [DataMember(EmitDefaultValue = false)]
        public string DictMaxtrixDisplay
        {
            get;
            set;
        }





        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictSkuDisplay
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string SkuNo
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string DetailId
        {
            get;
            set;
        }


        [DataMember]
        public decimal? Price
        {
            get;
            set;
        }

        //[DataMember]
        //public decimal? SelectedQty
        //{
        //    get;
        //    set;
        //}

        //[DataMember]
        //public decimal Quantity
        //{
        //    get;
        //    set;
        //}


        //[DataMember]
        //public decimal Weight
        //{
        //    get;
        //    set;
        //}


        //????




        //[DataMember]
        //public AppChildDataDto AppChildDataDto
        //{
        //    get;
        //    set;
        //}


        //

        [DataMember]
        public string SkuFieldName
        {
            get;
            set;
        }

        [DataMember]
        public string PriceFieldName
        {
            get;
            set;
        }

        //[DataMember]
        //public string SelectedQtyFieldName
        //{
        //    get;
        //    set;
        //}



        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, object> DictOneToOneFields
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



        [DataMember]
        public int? SearchViewId
        {
            get;
            set;
        }

        [DataMember]
        public int? TransactionId
        {
            get;
            set;
        }

        [DataMember]
        public int? ProductDetaiViewMapUnitId { get; set; }


        [DataMember]
        public decimal? SelectedQuantity
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsSkuHaveGrandChildren
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public List<double> PriceList
        //{
        //    get;
        //    set;
        //}

        [DataMember(EmitDefaultValue = false)]
        public bool IsSelectedQtyReadOnly
        {
            get;
            set;
        }

        [DataMember]
        public List<AppEshopBagItemDto> Children
        {
            get;
            set;
        }
    }
}