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
    /// DTO class for the entity 'AppEstore'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEstoreDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppEstoreDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppEstoreDto ,System.String>(o => o.Description);
        public static readonly string EmAppEstoreLayoutProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.EmAppEstoreLayout);
        public static readonly string EmAppEstoreThemeProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.EmAppEstoreTheme);
        public static readonly string NavigationTreeViewIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.NavigationTreeViewId);
        public static readonly string CatalogCardViewIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.CatalogCardViewId);
        public static readonly string CatalogCardDetailIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.CatalogCardDetailId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string IsDefaultProperty = ObjectInfoHelper.GetName<AppEstoreDto ,Nullable<System.Boolean>>(o => o.IsDefault);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEstoreDto()
        {        
        }
		
		static AppEstoreDto()
        {
                            
			      
			ForeignKeyProperties.Add(NavigationTreeViewIdProperty);  
			ForeignKeyProperties.Add(CatalogCardViewIdProperty);  
			ForeignKeyProperties.Add(CatalogCardDetailIdProperty);         		
              
			DictStringPropertyMaxLength.Add(NameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,1000);               
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
    


        /// <summary> The Name property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The EmAppEstoreLayout property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppEstoreLayout
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppEstoreLayoutProperty);}
            set { SetValue(EmAppEstoreLayoutProperty,value); }
        }

        /// <summary> The EmAppEstoreTheme property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppEstoreTheme
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppEstoreThemeProperty);}
            set { SetValue(EmAppEstoreThemeProperty,value); }
        }

        /// <summary> The NavigationTreeViewId property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NavigationTreeViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( NavigationTreeViewIdProperty);}
            set { SetValue(NavigationTreeViewIdProperty,value); }
        }

        /// <summary> The CatalogCardViewId property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CatalogCardViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( CatalogCardViewIdProperty);}
            set { SetValue(CatalogCardViewIdProperty,value); }
        }

        /// <summary> The CatalogCardDetailId property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CatalogCardDetailId
        {
            get { return  GetValue<Nullable<System.Int32>>( CatalogCardDetailIdProperty);}
            set { SetValue(CatalogCardDetailIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The IsDefault property of the Entity AppEstore</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDefault
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDefaultProperty);}
            set { SetValue(IsDefaultProperty,value); }
        }
        
        #endregion

       
        
    }
}

