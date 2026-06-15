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
    /// DTO class for the entity 'AppRouteState'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppRouteStateDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string StateCodeProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,System.String>(o => o.StateCode);
        public static readonly string PageRelativeUrlProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,System.String>(o => o.PageRelativeUrl);
        public static readonly string ControllerNameProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,System.String>(o => o.ControllerName);
        public static readonly string TemplateUrlProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,System.String>(o => o.TemplateUrl);
        public static readonly string NoSecurityControlProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,System.Boolean>(o => o.NoSecurityControl);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppRouteStateDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppRouteStateDto()
        {        
        }
		
		static AppRouteStateDto()
        {
                  
			MandatoryProperties.Add(NoSecurityControlProperty);      
			           		
              
			DictStringPropertyMaxLength.Add(StateCodeProperty,150); 
			DictStringPropertyMaxLength.Add(PageRelativeUrlProperty,200); 
			DictStringPropertyMaxLength.Add(ControllerNameProperty,200); 
			DictStringPropertyMaxLength.Add(TemplateUrlProperty,200);        
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
    


        /// <summary> The StateCode property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StateCode
        {
            get { return  GetValue<System.String>( StateCodeProperty);}
            set { SetValue(StateCodeProperty,value); }
        }

        /// <summary> The PageRelativeUrl property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PageRelativeUrl
        {
            get { return  GetValue<System.String>( PageRelativeUrlProperty);}
            set { SetValue(PageRelativeUrlProperty,value); }
        }

        /// <summary> The ControllerName property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ControllerName
        {
            get { return  GetValue<System.String>( ControllerNameProperty);}
            set { SetValue(ControllerNameProperty,value); }
        }

        /// <summary> The TemplateUrl property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TemplateUrl
        {
            get { return  GetValue<System.String>( TemplateUrlProperty);}
            set { SetValue(TemplateUrlProperty,value); }
        }

        /// <summary> The NoSecurityControl property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean NoSecurityControl
        {
            get { return  GetValue<System.Boolean>( NoSecurityControlProperty);}
            set { SetValue(NoSecurityControlProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppRouteState</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppRouteState</summary>
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

