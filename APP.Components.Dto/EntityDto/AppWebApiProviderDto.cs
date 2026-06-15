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
    /// DTO class for the entity 'AppWebApiProvider'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWebApiProviderDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProviderNameProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,System.String>(o => o.ProviderName);
        public static readonly string ApiKeyProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,System.String>(o => o.ApiKey);
        public static readonly string ApiSecretProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,System.String>(o => o.ApiSecret);
        public static readonly string AuthorizationTypeProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,Nullable<System.Int32>>(o => o.AuthorizationType);
        public static readonly string AuthorizationTypePrefixProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,System.String>(o => o.AuthorizationTypePrefix);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppWebApiProviderDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppWebApiProviderDto()
        {        
        }
		
		static AppWebApiProviderDto()
        {
                       
			           		
              
			DictStringPropertyMaxLength.Add(ProviderNameProperty,100); 
			DictStringPropertyMaxLength.Add(ApiKeyProperty,200); 
			DictStringPropertyMaxLength.Add(ApiSecretProperty,200);  
			DictStringPropertyMaxLength.Add(AuthorizationTypePrefixProperty,100);       
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
    


        /// <summary> The ProviderName property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ProviderName
        {
            get { return  GetValue<System.String>( ProviderNameProperty);}
            set { SetValue(ProviderNameProperty,value); }
        }

        /// <summary> The ApiKey property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ApiKey
        {
            get { return  GetValue<System.String>( ApiKeyProperty);}
            set { SetValue(ApiKeyProperty,value); }
        }

        /// <summary> The ApiSecret property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ApiSecret
        {
            get { return  GetValue<System.String>( ApiSecretProperty);}
            set { SetValue(ApiSecretProperty,value); }
        }

        /// <summary> The AuthorizationType property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AuthorizationType
        {
            get { return  GetValue<Nullable<System.Int32>>( AuthorizationTypeProperty);}
            set { SetValue(AuthorizationTypeProperty,value); }
        }

        /// <summary> The AuthorizationTypePrefix property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AuthorizationTypePrefix
        {
            get { return  GetValue<System.String>( AuthorizationTypePrefixProperty);}
            set { SetValue(AuthorizationTypePrefixProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppWebApiProvider</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

