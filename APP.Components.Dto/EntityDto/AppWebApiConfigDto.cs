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
    /// DTO class for the entity 'AppWebApiConfig'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWebApiConfigDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string WebApiNameProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,System.String>(o => o.WebApiName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,System.String>(o => o.Description);
        public static readonly string WebApiProviderIdProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.Int32>>(o => o.WebApiProviderId);
        public static readonly string WebApiBaseUrlProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,System.String>(o => o.WebApiBaseUrl);
        public static readonly string PathParameterNameProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,System.String>(o => o.PathParameterName);
        public static readonly string WebMethodProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.Int32>>(o => o.WebMethod);
        public static readonly string WebApiFullUrlFormatProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,System.String>(o => o.WebApiFullUrlFormat);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppWebApiConfigDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppWebApiConfigDto()
        {        
        }
		
		static AppWebApiConfigDto()
        {
                         
			    
			ForeignKeyProperties.Add(WebApiProviderIdProperty);          		
              
			DictStringPropertyMaxLength.Add(WebApiNameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);  
			DictStringPropertyMaxLength.Add(WebApiBaseUrlProperty,500); 
			DictStringPropertyMaxLength.Add(PathParameterNameProperty,50);  
			DictStringPropertyMaxLength.Add(WebApiFullUrlFormatProperty,1000);       
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
    


        /// <summary> The WebApiName property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WebApiName
        {
            get { return  GetValue<System.String>( WebApiNameProperty);}
            set { SetValue(WebApiNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The WebApiProviderId property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WebApiProviderId
        {
            get { return  GetValue<Nullable<System.Int32>>( WebApiProviderIdProperty);}
            set { SetValue(WebApiProviderIdProperty,value); }
        }

        /// <summary> The WebApiBaseUrl property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WebApiBaseUrl
        {
            get { return  GetValue<System.String>( WebApiBaseUrlProperty);}
            set { SetValue(WebApiBaseUrlProperty,value); }
        }

        /// <summary> The PathParameterName property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PathParameterName
        {
            get { return  GetValue<System.String>( PathParameterNameProperty);}
            set { SetValue(PathParameterNameProperty,value); }
        }

        /// <summary> The WebMethod property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WebMethod
        {
            get { return  GetValue<Nullable<System.Int32>>( WebMethodProperty);}
            set { SetValue(WebMethodProperty,value); }
        }

        /// <summary> The WebApiFullUrlFormat property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WebApiFullUrlFormat
        {
            get { return  GetValue<System.String>( WebApiFullUrlFormatProperty);}
            set { SetValue(WebApiFullUrlFormatProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppWebApiConfig</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppWebApiConfig</summary>
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

