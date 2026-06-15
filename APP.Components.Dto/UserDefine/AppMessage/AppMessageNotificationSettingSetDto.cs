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
   
    public partial class AppMessageNotificationSettingSetDto
    {
        [DataMember(EmitDefaultValue = false)]
        public ObservableSet<AppmessageNotificationSettingExDto> AppMessageNotificationSettingSet { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public List<object> DeletedItemIds { get; set; }


        [DataMember(EmitDefaultValue = false)]
        public int? TransactionId
        {
            get;
            set;
        }
      

        [DataMember(EmitDefaultValue = false)]
        public int? ProjectId
        {
            get;
            set;
        }

      
    }


}

