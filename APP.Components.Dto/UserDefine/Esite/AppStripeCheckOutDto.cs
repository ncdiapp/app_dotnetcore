
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using APP.Framework;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Framework.Collections;

namespace APP.Components.EntityDto
{
    public partial class AppStripeCheckOutDto
    {
        [DataMember(EmitDefaultValue = false)]
        public double Amount
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CurrencyCode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ProductName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PaymentUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SuccessUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string CancelUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SessionId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string ErrorMessage
        {
            get;
            set;
        }

    }
}
