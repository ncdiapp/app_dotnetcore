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
    /// DTO class for the entity 'AppTransactionUnitSearchViewFieldMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitSearchViewFieldMappingDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitLinkedSearchIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,System.Int32>(o => o.TransactionUnitLinkedSearchId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string SearchViewFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Int32>>(o => o.SearchViewFieldId);
        public static readonly string ExternalAppFieldMappingCodeProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,System.String>(o => o.ExternalAppFieldMappingCode);
        public static readonly string IsUniqueProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Boolean>>(o => o.IsUnique);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string TargetUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,Nullable<System.Int32>>(o => o.TargetUnitId);
        public static readonly string TargetTransactionFieldDbnameProperty = ObjectInfoHelper.GetName<AppTransactionUnitSearchViewFieldMappingDto ,System.String>(o => o.TargetTransactionFieldDbname);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitSearchViewFieldMappingDto()
        {        
        }
		
		static AppTransactionUnitSearchViewFieldMappingDto()
        {
              
			MandatoryProperties.Add(TransactionUnitLinkedSearchIdProperty);            
			  
			ForeignKeyProperties.Add(TransactionUnitLinkedSearchIdProperty);  
			ForeignKeyProperties.Add(TransactionFieldIdProperty);  
			ForeignKeyProperties.Add(SearchViewFieldIdProperty);         
			ForeignKeyProperties.Add(TargetUnitIdProperty);  		
                 
			DictStringPropertyMaxLength.Add(ExternalAppFieldMappingCodeProperty,50);        
			DictStringPropertyMaxLength.Add(TargetTransactionFieldDbnameProperty,200);  
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
    


        /// <summary> The TransactionUnitLinkedSearchId property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 TransactionUnitLinkedSearchId
        {
            get { return  GetValue<System.Int32>( TransactionUnitLinkedSearchIdProperty);}
            set { SetValue(TransactionUnitLinkedSearchIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The SearchViewFieldId property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewFieldIdProperty);}
            set { SetValue(SearchViewFieldIdProperty,value); }
        }

        /// <summary> The ExternalAppFieldMappingCode property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ExternalAppFieldMappingCode
        {
            get { return  GetValue<System.String>( ExternalAppFieldMappingCodeProperty);}
            set { SetValue(ExternalAppFieldMappingCodeProperty,value); }
        }

        /// <summary> The IsUnique property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUnique
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUniqueProperty);}
            set { SetValue(IsUniqueProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The TargetUnitId property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetUnitIdProperty);}
            set { SetValue(TargetUnitIdProperty,value); }
        }

        /// <summary> The TargetTransactionFieldDbname property of the Entity AppTransactionUnitSearchViewFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TargetTransactionFieldDbname
        {
            get { return  GetValue<System.String>( TargetTransactionFieldDbnameProperty);}
            set { SetValue(TargetTransactionFieldDbnameProperty,value); }
        }
        
        #endregion

       
        
    }
}

