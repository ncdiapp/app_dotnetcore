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
    /// DTO class for the entity 'AppSearch'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.String>(o => o.Description);
        public static readonly string TypeProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.Int32>(o => o.Type);
        public static readonly string IsBuiltInProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Boolean>>(o => o.IsBuiltIn);
        public static readonly string WhereUsedSearchIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.WhereUsedSearchId);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string IsAutoExecuteProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.Boolean>(o => o.IsAutoExecute);
        public static readonly string DataSetIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.DataSetId);
        public static readonly string FilterByCurrentUserMappingFieldProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.String>(o => o.FilterByCurrentUserMappingField);
        public static readonly string FilterByCurrentUserDomainTypeMappingFieldProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.String>(o => o.FilterByCurrentUserDomainTypeMappingField);
        public static readonly string BusinessScopeIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.BusinessScopeId);
        public static readonly string FilterByCurrentUserRoleMappingFieldProperty = ObjectInfoHelper.GetName<AppSearchDto ,System.String>(o => o.FilterByCurrentUserRoleMappingField);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string FolderTransactionIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.FolderTransactionId);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsHideAllToolsBarProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Boolean>>(o => o.IsHideAllToolsBar);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string IsForPublicAcesssProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Boolean>>(o => o.IsForPublicAcesss);
        public static readonly string IsFilterByUserTypeEntityProperty = ObjectInfoHelper.GetName<AppSearchDto ,Nullable<System.Boolean>>(o => o.IsFilterByUserTypeEntity);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSearchDto()
        {        
        }
		
		static AppSearchDto()
        {
              
			MandatoryProperties.Add(NameProperty);   
			MandatoryProperties.Add(TypeProperty);     
			MandatoryProperties.Add(IsAutoExecuteProperty);                
			       
			ForeignKeyProperties.Add(SearchViewIdProperty);   
			ForeignKeyProperties.Add(DataSetIdProperty);    
			ForeignKeyProperties.Add(BusinessScopeIdProperty);       
			ForeignKeyProperties.Add(FolderTransactionIdProperty);    
			ForeignKeyProperties.Add(SaasApplicationIdProperty);   		
              
			DictStringPropertyMaxLength.Add(NameProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,250);       
			DictStringPropertyMaxLength.Add(FilterByCurrentUserMappingFieldProperty,100); 
			DictStringPropertyMaxLength.Add(FilterByCurrentUserDomainTypeMappingFieldProperty,100);  
			DictStringPropertyMaxLength.Add(FilterByCurrentUserRoleMappingFieldProperty,100);            
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
    


        /// <summary> The Name property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The Type property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 Type
        {
            get { return  GetValue<System.Int32>( TypeProperty);}
            set { SetValue(TypeProperty,value); }
        }

        /// <summary> The IsBuiltIn property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsBuiltIn
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsBuiltInProperty);}
            set { SetValue(IsBuiltInProperty,value); }
        }

        /// <summary> The WhereUsedSearchId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WhereUsedSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( WhereUsedSearchIdProperty);}
            set { SetValue(WhereUsedSearchIdProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The IsAutoExecute property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsAutoExecute
        {
            get { return  GetValue<System.Boolean>( IsAutoExecuteProperty);}
            set { SetValue(IsAutoExecuteProperty,value); }
        }

        /// <summary> The DataSetId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSetIdProperty);}
            set { SetValue(DataSetIdProperty,value); }
        }

        /// <summary> The FilterByCurrentUserMappingField property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FilterByCurrentUserMappingField
        {
            get { return  GetValue<System.String>( FilterByCurrentUserMappingFieldProperty);}
            set { SetValue(FilterByCurrentUserMappingFieldProperty,value); }
        }

        /// <summary> The FilterByCurrentUserDomainTypeMappingField property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FilterByCurrentUserDomainTypeMappingField
        {
            get { return  GetValue<System.String>( FilterByCurrentUserDomainTypeMappingFieldProperty);}
            set { SetValue(FilterByCurrentUserDomainTypeMappingFieldProperty,value); }
        }

        /// <summary> The BusinessScopeId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> BusinessScopeId
        {
            get { return  GetValue<Nullable<System.Int32>>( BusinessScopeIdProperty);}
            set { SetValue(BusinessScopeIdProperty,value); }
        }

        /// <summary> The FilterByCurrentUserRoleMappingField property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FilterByCurrentUserRoleMappingField
        {
            get { return  GetValue<System.String>( FilterByCurrentUserRoleMappingFieldProperty);}
            set { SetValue(FilterByCurrentUserRoleMappingFieldProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The FolderTransactionId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FolderTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( FolderTransactionIdProperty);}
            set { SetValue(FolderTransactionIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsHideAllToolsBar property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsHideAllToolsBar
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsHideAllToolsBarProperty);}
            set { SetValue(IsHideAllToolsBarProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The IsForPublicAcesss property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsForPublicAcesss
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsForPublicAcesssProperty);}
            set { SetValue(IsForPublicAcesssProperty,value); }
        }

        /// <summary> The IsFilterByUserTypeEntity property of the Entity AppSearch</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFilterByUserTypeEntity
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFilterByUserTypeEntityProperty);}
            set { SetValue(IsFilterByUserTypeEntityProperty,value); }
        }
        
        #endregion

       
        
    }
}

