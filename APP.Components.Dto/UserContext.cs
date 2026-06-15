using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using APP.Framework;
using APP.Framework.Collections;
using APP.Components.EntityDto;

namespace APP.Components.Dto
{
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public  class UserContext : ObservableObject, IDisposable
    {
        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<UserContext, object>(o => o.UserId);
        public static readonly string EmailProperty = ObjectInfoHelper.GetName<UserContext, string>(o => o.Email);
        public static readonly string DisplayNameProperty = ObjectInfoHelper.GetName<UserContext, string>(o => o.DisplayName);
        public static readonly string LanguageIdProperty = ObjectInfoHelper.GetName<UserContext, object>(o => o.LanguageId);
        public static readonly string SessionIdProperty = ObjectInfoHelper.GetName<UserContext, object>(o => o.SessionId);
        public static readonly string CultureInfoCodeProperty = ObjectInfoHelper.GetName<UserContext, string>(o => o.CultureInfoCode);
        public static readonly string DomainIdProperty = ObjectInfoHelper.GetName<UserContext, object>(o => o.DomainId);
        public static readonly string UserAvailableActionListProperty = ObjectInfoHelper.GetName<UserContext, HashSet<string>>(o => o.UserAvailableActionList);
        public static readonly string IsSystemAdminProperty = ObjectInfoHelper.GetName<UserContext, bool>(o => o.IsInSysAdminDomain);
        public static readonly string DictAppSetupProperty = ObjectInfoHelper.GetName<UserContext, Dictionary<string, string>>(o => o.DictAppSetup);
        public static readonly string IsLoginFailedProperty = ObjectInfoHelper.GetName<UserContext, bool>(o => o.IsLoginFailed);
        public static readonly string IsExceededMaximumSessionProperty = ObjectInfoHelper.GetName<UserContext, bool>(o => o.IsExceededMaximumSession);
        public static readonly string ResourceIdProperty = ObjectInfoHelper.GetName<UserContext, Nullable<System.Int32>>(o => o.ResourceId);
        public static readonly string CatalogueIdProperty = ObjectInfoHelper.GetName<UserContext, Nullable<System.Int32>>(o => o.CatalogueId);
       
        /// 
        public static readonly string LicenseModeProperty = ObjectInfoHelper.GetName<UserContext, string>(o => o.LicenseMode);

        /// 
        public static readonly string LoginFailedErroMessageProperty = ObjectInfoHelper.GetName<UserContext, string>(o => o.LoginFailedErroMessage);

        public static readonly string BusinessUserIdProperty = ObjectInfoHelper.GetName<UserContext, int?>(o => o.BusinessUserId);

        


        public UserContext()
        {
            CloseTimeout = new TimeSpan(1, 0, 0);
            OpenTimeout = new TimeSpan(1, 0, 0);
            ReceiveTimeout = new TimeSpan(1, 0, 0);
            SendTimeout = new TimeSpan(1, 0, 0);
        }
        
        
        [DataMember(EmitDefaultValue = false)]
        public int? BusinessUserId
        {
            get { return GetValue<int?>(BusinessUserIdProperty); }
            set { SetValue(BusinessUserIdProperty, value); }
        }

      

        /// <summary> The LoginFailedErroMessage property of the UserContext</summary>
        [DataMember(EmitDefaultValue = false)]
        public string LoginFailedErroMessage
        {
            get { return GetValue<string>(LoginFailedErroMessageProperty); }
            set { SetValue(LoginFailedErroMessageProperty, value); }
        }

		/// <summary> The LoginFailedErroMessage property of the UserContext</summary>
		[DataMember(EmitDefaultValue = false)]
		public Dictionary<string, Dictionary<string,int>> DictEnumApp
		{
			get;set;
		}


		/// <summary> The LicenseMode property of the UserContext</summary>
		[DataMember(EmitDefaultValue = false)]
        public string LicenseMode
        {
            get { return GetValue<string>(LicenseModeProperty); }
            set { SetValue(LicenseModeProperty, value); }
        }

        /// <summary> The IsExceededMaximumSession property of the UserContext </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsExceededMaximumSession
        {
            get { return GetValue<bool>(IsExceededMaximumSessionProperty); }
            set { SetValue(IsExceededMaximumSessionProperty, value); }
        }



        /// <summary> The IsLoginFailed property of the UserContext </summary>
        [DataMember]
        public bool IsLoginFailed
        {
            get { return GetValue<bool>(IsLoginFailedProperty); }
            set { SetValue(IsLoginFailedProperty, value); }
        }

        [DataMember]
        public int? LoginFailedType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan CloseTimeout { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan OpenTimeout { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan ReceiveTimeout { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan SendTimeout { get; set; }

        /// <summary> The   DictPdmSetup property of the UserContext</summary>
        [DataMember(EmitDefaultValue = false)]
        public Dictionary<string, string> DictAppSetup
        {
            get { return GetValue<Dictionary<string, string>>(DictAppSetupProperty); }
            set { SetValue(DictAppSetupProperty, value); }
        }





        /// <summary> The IsSystemAdmin property of the UserContext</summary>
        [DataMember(EmitDefaultValue = false)]
        public bool IsInSysAdminDomain
        {
            get { return GetValue<bool>(IsSystemAdminProperty); }
            set { SetValue(IsSystemAdminProperty, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public int UserCategory
        {
            get;
            set;
        }

        


        /// <summary> The UserAvailableActionList property of the UserContext </summary>
        [DataMember(EmitDefaultValue = false)]
        public HashSet<string> UserAvailableActionList
        {
            get { return GetValue<HashSet<string>>(UserAvailableActionListProperty); }
            set { SetValue(UserAvailableActionListProperty, value); }
        }

        //public HashSet<string> HashAvailableActionList
        //{
        //    get
        //    {
        //        if (UserAvailableActionList != null)
        //        {
        //            return new HashSet<string>(UserAvailableActionList);
        //        }

        //        return new HashSet<string>();

        //    }
        //}


        [DataMember(EmitDefaultValue = false)]
        public bool IsUseExternalFileServer
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ImageRequestServerBaseURL
        {
            get;
            set;
        }

        [IgnoreDataMember]
        public object UserId
        {
            get;set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string Email
        {
            get
            {
                return GetValue<string>(EmailProperty);
            }
            set
            {
                SetValue(EmailProperty, value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string DisplayName
        {
            get
            {
                return GetValue<string>(DisplayNameProperty);
            }
            set
            {
                SetValue(DisplayNameProperty, value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public object DomainId
        {
            get
            {
                return GetValue<object>(DomainIdProperty);
            }
            set
            {
                SetValue(DomainIdProperty, value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DocumentId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object LanguageId
        {
            get
            {
                return GetValue<object>(LanguageIdProperty);
            }
            set
            {
                SetValue(LanguageIdProperty, value);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public string TimeZoneKey
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TimeZoneDisplay
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? DefaultDesktopId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public object DefaultFileFolderId
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ThemeId
        {
            get;
            set;
        }


        [DataMember(EmitDefaultValue = false)]
        public string CultureInfoCode
        {
            get
            {
                return GetValue<string>(CultureInfoCodeProperty);
            }
            set
            {
                SetValue(CultureInfoCodeProperty, value);

            }
        }

        [DataMember(EmitDefaultValue = false)]
        public object SessionId
        {
            get
            {
                return GetValue<object>(SessionIdProperty);
            }
            set
            {
                SetValue(SessionIdProperty, value);
            }
        }

        public string DomainIdentityToken
        {
            get;set;
        }

        


        [DataMember(EmitDefaultValue = false)]
        public List<LookupItemDto> AvailableCompnay
        {
            get;set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string  CurrentCompnayId
        {
            get; set;
        }


        public string CompnayDomainIdentityToken
        {
            get; set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string TempToken
        {
            get; set;
        }

        [IgnoreDataMember]
        public int?  ServerSideCurrentCompnayId
        {
            get; set;
        }
      

        /// <summary> The CatalogueId property of the Entity PdmSecurityWebUser</summary>
        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> CatalogueId
        {
            get { return GetValue<Nullable<System.Int32>>(CatalogueIdProperty); }
            set { SetValue(CatalogueIdProperty, value); }
        }

        /// <summary> The ResourceId property of the Entity PdmSecurityWebUser</summary>
        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> ResourceId
        {
            get { return GetValue<Nullable<System.Int32>>(ResourceIdProperty); }
            set { SetValue(ResourceIdProperty, value); }
        }

        [DataMember(EmitDefaultValue = false)]
        public Nullable<System.Int32> EmExternalSigninType
        {
            get;set;
        }

        [DataMember(EmitDefaultValue = false)]
        public string ExternalAcessToken
        {
            get; set;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        [DataMember]
        public bool IsWaitingForEmailActivation
        {
            get;
            set;
        }

        [DataMember]
        public bool IsFirstTimeLogin
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public bool RequiresCompanySelection
        {
            get;
            set;
        }


    }
}