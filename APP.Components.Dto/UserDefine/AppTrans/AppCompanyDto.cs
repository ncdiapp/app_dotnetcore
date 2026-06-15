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
    public partial class AppCompanyDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool IsSaasUserCompany
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string LogoImageUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public List<string> BackgroundImageUrlList
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public AppDataSourceRegisterDto DataSourceRegisterInfo
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool IsLinkToHostCompanyDb
        {
            get;
            set;
        }
        

        [DataMember(EmitDefaultValue = false)]
        public AppCompanyOtherSettingsDto OtherSettingsDto
        {
            get;
            set;
        }
    }


    public partial class AppCompanyOtherSettingsDto
    {
        [DataMember(EmitDefaultValue = false)]
        public bool? IsEnableClientSelfRegistration
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsEnableSupplierSelfRegistration
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsEnableClientAgentSelfRegistration
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool? IsEnableSupplierAgentSelfRegistration
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ClientLabelName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SupplierLabelName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ClientAgentLabelName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string SupplierAgentLabelName
        {
            get;
            set;
        }


        //[DataMember(EmitDefaultValue = false)]
        //public int? LogoImageId
        //{
        //    get;
        //    set;
        //}


        //[DataMember(EmitDefaultValue = false)]
        //public List<int> LoginPageBackgroundImageIdList
        //{
        //    get;
        //    set;
        //}
    }
}

