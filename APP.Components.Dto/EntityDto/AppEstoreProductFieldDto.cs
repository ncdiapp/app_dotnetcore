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
    /// DTO class for the entity 'AppEstoreProductField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEstoreProductFieldDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string EstoreIdProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.EstoreId);
        public static readonly string SearchViewFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.SearchViewFieldId);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string IsVisibleProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Boolean>>(o => o.IsVisible);
        public static readonly string IsReuiredProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Boolean>>(o => o.IsReuired);
        public static readonly string EmAppShoppingBagFieldTypeProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.EmAppShoppingBagFieldType);
        public static readonly string MappingToOrderItemTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.MappingToOrderItemTransactionFieldId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEstoreProductFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEstoreProductFieldDto()
        {        
        }
		
		static AppEstoreProductFieldDto()
        {
                         
			  
			ForeignKeyProperties.Add(EstoreIdProperty);            		
                           
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
    


        /// <summary> The EstoreId property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EstoreId
        {
            get { return  GetValue<Nullable<System.Int32>>( EstoreIdProperty);}
            set { SetValue(EstoreIdProperty,value); }
        }

        /// <summary> The SearchViewFieldId property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewFieldIdProperty);}
            set { SetValue(SearchViewFieldIdProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The IsVisible property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsVisible
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsVisibleProperty);}
            set { SetValue(IsVisibleProperty,value); }
        }

        /// <summary> The IsReuired property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsReuired
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsReuiredProperty);}
            set { SetValue(IsReuiredProperty,value); }
        }

        /// <summary> The EmAppShoppingBagFieldType property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppShoppingBagFieldType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppShoppingBagFieldTypeProperty);}
            set { SetValue(EmAppShoppingBagFieldTypeProperty,value); }
        }

        /// <summary> The MappingToOrderItemTransactionFieldId property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MappingToOrderItemTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( MappingToOrderItemTransactionFieldIdProperty);}
            set { SetValue(MappingToOrderItemTransactionFieldIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEstoreProductField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEstoreProductField</summary>
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

