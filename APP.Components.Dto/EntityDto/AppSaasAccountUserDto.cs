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
    /// DTO class for the entity 'AppSaasAccountUser'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSaasAccountUserDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string LoginNameProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.String>(o => o.LoginName);
        public static readonly string UserNameProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.String>(o => o.UserName);
        public static readonly string EmailProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.String>(o => o.Email);
        public static readonly string AppCompanyIdProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.Int32>>(o => o.AppCompanyId);
        public static readonly string PasswordProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.String>(o => o.Password);
        public static readonly string WebDomainHostProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.String>(o => o.WebDomainHost);
        public static readonly string CultureInfoCodeProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.String>(o => o.CultureInfoCode);
        public static readonly string IsBuiltIntUserProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.Boolean>>(o => o.IsBuiltIntUser);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.Boolean>(o => o.IsActive);
        public static readonly string IsDeletedProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,System.Boolean>(o => o.IsDeleted);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string GlobalUserGuidProperty = ObjectInfoHelper.GetName<AppSaasAccountUserDto ,Nullable<System.Guid>>(o => o.GlobalUserGuid);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSaasAccountUserDto()
        {        
        }
		
		static AppSaasAccountUserDto()
        {
              
			MandatoryProperties.Add(LoginNameProperty);  
			MandatoryProperties.Add(UserNameProperty);  
			MandatoryProperties.Add(EmailProperty);   
			MandatoryProperties.Add(PasswordProperty);     
			MandatoryProperties.Add(IsActiveProperty);  
			MandatoryProperties.Add(IsDeletedProperty);      
			     
			ForeignKeyProperties.Add(AppCompanyIdProperty);            		
              
			DictStringPropertyMaxLength.Add(LoginNameProperty,50); 
			DictStringPropertyMaxLength.Add(UserNameProperty,50); 
			DictStringPropertyMaxLength.Add(EmailProperty,80);  
			DictStringPropertyMaxLength.Add(PasswordProperty,800); 
			DictStringPropertyMaxLength.Add(WebDomainHostProperty,300); 
			DictStringPropertyMaxLength.Add(CultureInfoCodeProperty,10);          
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
    


        /// <summary> The LoginName property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LoginName
        {
            get { return  GetValue<System.String>( LoginNameProperty);}
            set { SetValue(LoginNameProperty,value); }
        }

        /// <summary> The UserName property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UserName
        {
            get { return  GetValue<System.String>( UserNameProperty);}
            set { SetValue(UserNameProperty,value); }
        }

        /// <summary> The Email property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Email
        {
            get { return  GetValue<System.String>( EmailProperty);}
            set { SetValue(EmailProperty,value); }
        }

        /// <summary> The AppCompanyId property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCompanyIdProperty);}
            set { SetValue(AppCompanyIdProperty,value); }
        }

        /// <summary> The Password property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Password
        {
            get { return  GetValue<System.String>( PasswordProperty);}
            set { SetValue(PasswordProperty,value); }
        }

        /// <summary> The WebDomainHost property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WebDomainHost
        {
            get { return  GetValue<System.String>( WebDomainHostProperty);}
            set { SetValue(WebDomainHostProperty,value); }
        }

        /// <summary> The CultureInfoCode property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CultureInfoCode
        {
            get { return  GetValue<System.String>( CultureInfoCodeProperty);}
            set { SetValue(CultureInfoCodeProperty,value); }
        }

        /// <summary> The IsBuiltIntUser property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsBuiltIntUser
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsBuiltIntUserProperty);}
            set { SetValue(IsBuiltIntUserProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsActive
        {
            get { return  GetValue<System.Boolean>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The IsDeleted property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsDeleted
        {
            get { return  GetValue<System.Boolean>( IsDeletedProperty);}
            set { SetValue(IsDeletedProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The GlobalUserGuid property of the Entity AppSaasAccountUser</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> GlobalUserGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( GlobalUserGuidProperty);}
            set { SetValue(GlobalUserGuidProperty,value); }
        }
        
        #endregion

       
        
    }
}

