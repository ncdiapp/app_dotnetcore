using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APP.Framework.Validation;
using APP.Framework;
using System.Runtime.Serialization;

namespace APP.Components.EntityDto
{
    public partial class ValidationMessageDto : ObservableObject
    {

        /// 
        /// 
        public static readonly string IsSuccesfulProperty = ObjectInfoHelper.GetName<ValidationMessageDto , bool>(o => o.IsSuccesful);
        public static readonly string IsAllowForceContinueProperty = ObjectInfoHelper.GetName<ValidationMessageDto, bool>(o => o.IsAllowForceContinue);

        /// 
        public static readonly string MessageProperty = ObjectInfoHelper.GetName<ValidationMessageDto , string>(o => o.Message);

        /// <summary> The Message property of the ValidationMessageDto </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Message
        {
            get { return GetValue<string>(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        



        /// <summary> The IsSuccesful property of the ValidationMessageDto </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsSuccesful
        {
            get { return GetValue<bool>(IsSuccesfulProperty); }
            set { SetValue(IsSuccesfulProperty, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsAllowForceContinue
        {
            get { return GetValue<bool>(IsAllowForceContinueProperty); }
            set { SetValue(IsAllowForceContinueProperty, value); }
        }


        [IgnoreDataMember]
        public object ValidationResult
        {
            get;
            set;
        }
        


    }
}