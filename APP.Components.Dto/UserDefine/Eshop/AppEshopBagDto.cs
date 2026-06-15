
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
    public  class AppEshopBagDto
    {

        public AppEshopBagDto() 
        {

        }

        [DataMember(EmitDefaultValue = false)]
        public object ESiteId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<AppEshopBagItemDto> EshopBagItemList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal Subtotal
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal ShippingCost
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal TotalTax
        {
            get;
            set;
        }
        

        [DataMember(EmitDefaultValue = false)]
        public decimal Total
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal Discount
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppEshopShippingAdressDto ShippingAdress
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppEshopShippingAdressDto BillingAdress
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsBillingAddressSameAsShippingAddress
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string GuestEmail
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? PaymentMethod
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CustomerId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? OrderId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? InvoiceId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? CustomerDataModelId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? OrderDataModelId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? InvoiceDataModelId
        {
            get;
            set;
        }

    }
}
