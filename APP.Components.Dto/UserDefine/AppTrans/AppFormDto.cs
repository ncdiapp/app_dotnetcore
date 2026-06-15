using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.Dto;

namespace APP.Components.EntityDto
{
    public partial class AppFormDto
    {
        [DataMember(EmitDefaultValue = false)]
        public string TransactionOrganizedType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string PrimaryKeyField { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? AssociatedTransactionId { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsPhysicalModelTableCreated { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsApiIntegrationTransaction { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? AssociatedSearchViewId { get; set; }



        public string HtmlViewContent { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultDeviceType { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? DefaultNbColumns { get; set; }


        //[DataMember(EmitDefaultValue = false)]
        //public int? SaasApplicationId { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public AppTransactionExDto AssociatedTransactionExDto { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool IsPrintInLandscape { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public bool IsPrintForm
        {
            get
            {
                bool? isPrintForm = ControlTypeValueConverter.ConvertValueToBoolean(RouteParamter3);
                return isPrintForm.HasValue && isPrintForm.Value;

            }
            set
            { 
            
            }
        }

    }
}