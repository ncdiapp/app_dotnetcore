
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
    public  class AppEshopOrderDto
    {        
        [DataMember(EmitDefaultValue = false)]
        public object OrderId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string OrderNumber
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string OrderDescription
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public decimal? Subtotal
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal? ShippingCost
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal? TotalTax
        {
            get;
            set;
        }
        

        [DataMember(EmitDefaultValue = false)]
        public decimal? Total
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public decimal? Discount
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
        public bool? IsBillingAddressSameAsShippingAddress
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
        public int PaymentMethod
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
        public int? ShippingStatus
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? OrderStatus
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
        public DateTime? OrderPlacedDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? OrderShipDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? OrderExpectedDeliverDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? OrderDeliveredDate
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? OrderCanceledDate
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public List<AppEshopBagItemDto> OrderItemList
        {
            get;
            set;
        }

    }
}
