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
    /// DTO class for the entity 'AppBusinessScopeTag'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusinessScopeTagDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TagNameProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,System.String>(o => o.TagName);
        public static readonly string TagIconStyleProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,System.String>(o => o.TagIconStyle);
        public static readonly string EmTagBusienssScopeProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,Nullable<System.Int32>>(o => o.EmTagBusienssScope);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppBusinessScopeTagDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppBusinessScopeTagDto()
        {        
        }
		
		static AppBusinessScopeTagDto()
        {
                     
			         		
              
			DictStringPropertyMaxLength.Add(TagNameProperty,50); 
			DictStringPropertyMaxLength.Add(TagIconStyleProperty,200);        
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
    


        /// <summary> The TagName property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TagName
        {
            get { return  GetValue<System.String>( TagNameProperty);}
            set { SetValue(TagNameProperty,value); }
        }

        /// <summary> The TagIconStyle property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TagIconStyle
        {
            get { return  GetValue<System.String>( TagIconStyleProperty);}
            set { SetValue(TagIconStyleProperty,value); }
        }

        /// <summary> The EmTagBusienssScope property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmTagBusienssScope
        {
            get { return  GetValue<Nullable<System.Int32>>( EmTagBusienssScopeProperty);}
            set { SetValue(EmTagBusienssScopeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppBusinessScopeTag</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppBusinessScopeTag</summary>
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

