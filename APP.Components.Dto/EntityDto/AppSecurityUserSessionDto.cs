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
    /// DTO class for the entity 'AppSecurityUserSession'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityUserSessionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,System.Int32>(o => o.UserId);
        public static readonly string ExpirationDateProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,System.DateTime>(o => o.ExpirationDate);
        public static readonly string SessionIdProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,System.String>(o => o.SessionId);
        public static readonly string ApplicationTypeProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,System.Int32>(o => o.ApplicationType);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string EmExternalSigninTypeProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,Nullable<System.Int32>>(o => o.EmExternalSigninType);
        public static readonly string ExternalAcessTokenProperty = ObjectInfoHelper.GetName<AppSecurityUserSessionDto ,System.String>(o => o.ExternalAcessToken);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityUserSessionDto()
        {        
        }
		
		static AppSecurityUserSessionDto()
        {
              
			MandatoryProperties.Add(UserIdProperty);  
			MandatoryProperties.Add(ExpirationDateProperty);  
			MandatoryProperties.Add(SessionIdProperty);  
			MandatoryProperties.Add(ApplicationTypeProperty);        
			  
			ForeignKeyProperties.Add(UserIdProperty);           		
                
			DictStringPropertyMaxLength.Add(SessionIdProperty,100);        
			DictStringPropertyMaxLength.Add(ExternalAcessTokenProperty,4000);  
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
    


        /// <summary> The UserId property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 UserId
        {
            get { return  GetValue<System.Int32>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The ExpirationDate property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.DateTime ExpirationDate
        {
            get { return  GetValue<System.DateTime>( ExpirationDateProperty);}
            set { SetValue(ExpirationDateProperty,value); }
        }

        /// <summary> The SessionId property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SessionId
        {
            get { return  GetValue<System.String>( SessionIdProperty);}
            set { SetValue(SessionIdProperty,value); }
        }

        /// <summary> The ApplicationType property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ApplicationType
        {
            get { return  GetValue<System.Int32>( ApplicationTypeProperty);}
            set { SetValue(ApplicationTypeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The EmExternalSigninType property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmExternalSigninType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmExternalSigninTypeProperty);}
            set { SetValue(EmExternalSigninTypeProperty,value); }
        }

        /// <summary> The ExternalAcessToken property of the Entity AppSecurityUserSession</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ExternalAcessToken
        {
            get { return  GetValue<System.String>( ExternalAcessTokenProperty);}
            set { SetValue(ExternalAcessTokenProperty,value); }
        }
        
        #endregion

       
        
    }
}

