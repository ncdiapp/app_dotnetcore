
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

    public enum EmClientAgentCallingFrom { BrowserApp=1, MobileApp =2}
    public  class ExternalSignInTokenDto
    {

            // 1: BrowserApp
            // 2: MobileApp
        [DataMember(EmitDefaultValue = false)]
        public int? ClientAgentCallingFrom
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? EmExternalSigninType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ExternalAcessToken
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? NewUserPartnerType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? RegisterFromEsiteId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string PostEmailActivationRedirectUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TimeZoneInfoToken
        {
            get;
            set;
        }
        

        [DataMember(EmitDefaultValue = false)]
        public string LogoImgUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string UserEmail
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string MessageTempalte
        {
            get;
            set;
        }
    }
}
