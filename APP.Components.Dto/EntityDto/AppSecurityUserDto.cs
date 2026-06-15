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
    /// <summary>
    /// DTO class for the entity 'AppSecurityUser'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityUserDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string LoginNameProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.LoginName);
        public static readonly string UserNameProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.UserName);
        public static readonly string EmailProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.Email);
        public static readonly string DomainIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.Int32>(o => o.DomainId);
        public static readonly string OrganizationIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.OrganizationId);
        public static readonly string ExchangeServiceUrlProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.ExchangeServiceUrl);
        public static readonly string MenuSettingProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.MenuSetting);
        public static readonly string UserLanguageProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.UserLanguage);
        public static readonly string PasswordProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.Password);
        public static readonly string CultureInfoCodeProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.CultureInfoCode);
        public static readonly string IsBuiltIntUserProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Boolean>>(o => o.IsBuiltIntUser);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.Boolean>(o => o.IsActive);
        public static readonly string IsDeletedProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.Boolean>(o => o.IsDeleted);
        public static readonly string DefaultVendorRequestFolderIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.DefaultVendorRequestFolderId);
        public static readonly string LanguageIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.LanguageId);
        public static readonly string AdloginNameProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.AdloginName);
        public static readonly string DocumentIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.DocumentId);
        public static readonly string TimeZoneInfoTokenProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.TimeZoneInfoToken);
        public static readonly string CurrentContactEmailProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.CurrentContactEmail);
        public static readonly string AdpasswordProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.Adpassword);
        public static readonly string AddomainProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.Addomain);
        public static readonly string RefreshTokenProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.RefreshToken);
        public static readonly string CompanyCalendarIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.CompanyCalendarId);
        public static readonly string PersonalRateProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Double>>(o => o.PersonalRate);
        public static readonly string CurrencyIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.CurrencyId);
        public static readonly string IsPersonalEmailAccountProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Boolean>>(o => o.IsPersonalEmailAccount);
        public static readonly string EmailNotificationReceiveModeProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.EmailNotificationReceiveMode);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string DefaultDesktopIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.DefaultDesktopId);
        public static readonly string GlobalGuidProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Guid>>(o => o.GlobalGuid);
        public static readonly string IsRegisterCompletedProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Boolean>>(o => o.IsRegisterCompleted);
        public static readonly string MyOwnCompnanyIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,Nullable<System.Int32>>(o => o.MyOwnCompnanyId);
        public static readonly string MappingExternalEmployeeAccountIdProperty = ObjectInfoHelper.GetName<AppSecurityUserDto ,System.String>(o => o.MappingExternalEmployeeAccountId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityUserDto()
        {        
        }
		
		static AppSecurityUserDto()
        {
              
			MandatoryProperties.Add(LoginNameProperty);  
			MandatoryProperties.Add(UserNameProperty);  
			MandatoryProperties.Add(EmailProperty);  
			MandatoryProperties.Add(DomainIdProperty);      
			MandatoryProperties.Add(PasswordProperty);    
			MandatoryProperties.Add(IsActiveProperty);  
			MandatoryProperties.Add(IsDeletedProperty);                         
			     
			ForeignKeyProperties.Add(DomainIdProperty);  
			ForeignKeyProperties.Add(OrganizationIdProperty);          
			ForeignKeyProperties.Add(DefaultVendorRequestFolderIdProperty);          
			ForeignKeyProperties.Add(CompanyCalendarIdProperty);              
			ForeignKeyProperties.Add(MyOwnCompnanyIdProperty);  		
              
			DictStringPropertyMaxLength.Add(LoginNameProperty,50); 
			DictStringPropertyMaxLength.Add(UserNameProperty,50); 
			DictStringPropertyMaxLength.Add(EmailProperty,80);   
			DictStringPropertyMaxLength.Add(ExchangeServiceUrlProperty,800);   
			DictStringPropertyMaxLength.Add(PasswordProperty,800); 
			DictStringPropertyMaxLength.Add(CultureInfoCodeProperty,10);      
			DictStringPropertyMaxLength.Add(AdloginNameProperty,200);  
			DictStringPropertyMaxLength.Add(TimeZoneInfoTokenProperty,200); 
			DictStringPropertyMaxLength.Add(CurrentContactEmailProperty,200); 
			DictStringPropertyMaxLength.Add(AdpasswordProperty,800); 
			DictStringPropertyMaxLength.Add(AddomainProperty,800); 
			DictStringPropertyMaxLength.Add(RefreshTokenProperty,800);               
			DictStringPropertyMaxLength.Add(MappingExternalEmployeeAccountIdProperty,50);  
        }

		protected override void OnInitialize()
        {
            base.OnInitialize();
            PropertyNeedToValidationList = new List<string>();
            PropertyNeedToValidationList.AddRange (MandatoryProperties);
            PropertyNeedToValidationList.AddRange(DictStringPropertyMaxLength.Keys);  
            OnInitialized();

        }
  

        public override ValidationResult ValidateDto()
        {
              ValidationResult aValidationResult =FirstLevelVlidationDtoFactory.ValidateDtoStringmaxLengthAndMandatory(this, MandatoryProperties, ForeignKeyProperties, DictStringPropertyMaxLength);
              CustomerValidateDto(aValidationResult);
              return aValidationResult;
        
        }
    
        public override ValidationResult ValidateProperty(string PropertyName)
        {
             ValidationResult aValidationResult = FirstLevelVlidationDtoFactory.ValidatePropertyStringmaxLengthAndMandatory( this,PropertyName, MandatoryProperties, ForeignKeyProperties, DictStringPropertyMaxLength);
             CustomerValidateProperty( PropertyName,  aValidationResult);
             return aValidationResult;
        }


        partial void OnInitialized();
        partial void CustomerValidateDto(ValidationResult aValidationResult);
        partial void CustomerValidateProperty(string PropertyName, ValidationResult aValidationResult);       
   
        #region  Entity Dto Properties 
    


        /// <summary> The LoginName property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LoginName
        {
            get { return  GetValue<System.String>( LoginNameProperty);}
            set { SetValue(LoginNameProperty,value); }
        }

        /// <summary> The UserName property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UserName
        {
            get { return  GetValue<System.String>( UserNameProperty);}
            set { SetValue(UserNameProperty,value); }
        }

        /// <summary> The Email property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Email
        {
            get { return  GetValue<System.String>( EmailProperty);}
            set { SetValue(EmailProperty,value); }
        }

        /// <summary> The DomainId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 DomainId
        {
            get { return  GetValue<System.Int32>( DomainIdProperty);}
            set { SetValue(DomainIdProperty,value); }
        }

        /// <summary> The OrganizationId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrganizationId
        {
            get { return  GetValue<Nullable<System.Int32>>( OrganizationIdProperty);}
            set { SetValue(OrganizationIdProperty,value); }
        }

        /// <summary> The ExchangeServiceUrl property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ExchangeServiceUrl
        {
            get { return  GetValue<System.String>( ExchangeServiceUrlProperty);}
            set { SetValue(ExchangeServiceUrlProperty,value); }
        }

        /// <summary> The MenuSetting property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MenuSetting
        {
            get { return  GetValue<Nullable<System.Int32>>( MenuSettingProperty);}
            set { SetValue(MenuSettingProperty,value); }
        }

        /// <summary> The UserLanguage property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserLanguage
        {
            get { return  GetValue<Nullable<System.Int32>>( UserLanguageProperty);}
            set { SetValue(UserLanguageProperty,value); }
        }

        /// <summary> The Password property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Password
        {
            get { return  GetValue<System.String>( PasswordProperty);}
            set { SetValue(PasswordProperty,value); }
        }

        /// <summary> The CultureInfoCode property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CultureInfoCode
        {
            get { return  GetValue<System.String>( CultureInfoCodeProperty);}
            set { SetValue(CultureInfoCodeProperty,value); }
        }

        /// <summary> The IsBuiltIntUser property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsBuiltIntUser
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsBuiltIntUserProperty);}
            set { SetValue(IsBuiltIntUserProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsActive
        {
            get { return  GetValue<System.Boolean>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The IsDeleted property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsDeleted
        {
            get { return  GetValue<System.Boolean>( IsDeletedProperty);}
            set { SetValue(IsDeletedProperty,value); }
        }

        /// <summary> The DefaultVendorRequestFolderId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DefaultVendorRequestFolderId
        {
            get { return  GetValue<Nullable<System.Int32>>( DefaultVendorRequestFolderIdProperty);}
            set { SetValue(DefaultVendorRequestFolderIdProperty,value); }
        }

        /// <summary> The LanguageId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LanguageId
        {
            get { return  GetValue<Nullable<System.Int32>>( LanguageIdProperty);}
            set { SetValue(LanguageIdProperty,value); }
        }

        /// <summary> The AdloginName property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AdloginName
        {
            get { return  GetValue<System.String>( AdloginNameProperty);}
            set { SetValue(AdloginNameProperty,value); }
        }

        /// <summary> The DocumentId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DocumentId
        {
            get { return  GetValue<Nullable<System.Int32>>( DocumentIdProperty);}
            set { SetValue(DocumentIdProperty,value); }
        }

        /// <summary> The TimeZoneInfoToken property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TimeZoneInfoToken
        {
            get { return  GetValue<System.String>( TimeZoneInfoTokenProperty);}
            set { SetValue(TimeZoneInfoTokenProperty,value); }
        }

        /// <summary> The CurrentContactEmail property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CurrentContactEmail
        {
            get { return  GetValue<System.String>( CurrentContactEmailProperty);}
            set { SetValue(CurrentContactEmailProperty,value); }
        }

        /// <summary> The Adpassword property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Adpassword
        {
            get { return  GetValue<System.String>( AdpasswordProperty);}
            set { SetValue(AdpasswordProperty,value); }
        }

        /// <summary> The Addomain property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Addomain
        {
            get { return  GetValue<System.String>( AddomainProperty);}
            set { SetValue(AddomainProperty,value); }
        }

        /// <summary> The RefreshToken property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RefreshToken
        {
            get { return  GetValue<System.String>( RefreshTokenProperty);}
            set { SetValue(RefreshTokenProperty,value); }
        }

        /// <summary> The CompanyCalendarId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CompanyCalendarId
        {
            get { return  GetValue<Nullable<System.Int32>>( CompanyCalendarIdProperty);}
            set { SetValue(CompanyCalendarIdProperty,value); }
        }

        /// <summary> The PersonalRate property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PersonalRate
        {
            get { return  GetValue<Nullable<System.Double>>( PersonalRateProperty);}
            set { SetValue(PersonalRateProperty,value); }
        }

        /// <summary> The CurrencyId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CurrencyId
        {
            get { return  GetValue<Nullable<System.Int32>>( CurrencyIdProperty);}
            set { SetValue(CurrencyIdProperty,value); }
        }

        /// <summary> The IsPersonalEmailAccount property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPersonalEmailAccount
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPersonalEmailAccountProperty);}
            set { SetValue(IsPersonalEmailAccountProperty,value); }
        }

        /// <summary> The EmailNotificationReceiveMode property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmailNotificationReceiveMode
        {
            get { return  GetValue<Nullable<System.Int32>>( EmailNotificationReceiveModeProperty);}
            set { SetValue(EmailNotificationReceiveModeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The DefaultDesktopId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DefaultDesktopId
        {
            get { return  GetValue<Nullable<System.Int32>>( DefaultDesktopIdProperty);}
            set { SetValue(DefaultDesktopIdProperty,value); }
        }

        /// <summary> The GlobalGuid property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> GlobalGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( GlobalGuidProperty);}
            set { SetValue(GlobalGuidProperty,value); }
        }

        /// <summary> The IsRegisterCompleted property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsRegisterCompleted
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsRegisterCompletedProperty);}
            set { SetValue(IsRegisterCompletedProperty,value); }
        }

        /// <summary> The MyOwnCompnanyId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MyOwnCompnanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( MyOwnCompnanyIdProperty);}
            set { SetValue(MyOwnCompnanyIdProperty,value); }
        }

        /// <summary> The MappingExternalEmployeeAccountId property of the Entity AppSecurityUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MappingExternalEmployeeAccountId
        {
            get { return  GetValue<System.String>( MappingExternalEmployeeAccountIdProperty);}
            set { SetValue(MappingExternalEmployeeAccountIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

