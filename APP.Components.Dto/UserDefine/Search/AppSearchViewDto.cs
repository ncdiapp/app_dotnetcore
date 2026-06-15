using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using Newtonsoft.Json;

namespace APP.Components.EntityDto
{
    public partial class AppSearchViewDto
    {
        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? EshopCardViewSearchId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? EshopProductDetailViewSearchId { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public bool NeedShoppingCartAndCheckOutPages { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public int? EshopShoppingCartTransactionId { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public int? EshopOrderTransactionId { get; set; }

        //[DataMember(EmitDefaultValue = false)]
        //public int? ShoppingCartCheckOutCommandId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsUpdateClusterMainViewItemSource { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? NeedToSaveAsFromEshopProductBaseDataModelId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppSearchViewOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }
    }


    public partial class AppSearchViewOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public int? LogicKeyFieldId
        {
            get;
            set;
        }



        [DataMember(EmitDefaultValue = false)]
        public List<AppDesktopItemExDto> FlexLayoutItems
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppDesktopItemExDto> ClusterChildViewItemList
        {
            get; 
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppViewLinkedSeaechOrUrlDto EshopCategorySearchMapping
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EshopCardViewItemDetailTransactionId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ImportSettingId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public bool IsDraft
        {
            get;
            set;
        }

    }


}

