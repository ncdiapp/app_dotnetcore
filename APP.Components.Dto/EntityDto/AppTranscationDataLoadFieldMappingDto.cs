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
    /// DTO class for the entity 'AppTranscationDataLoadFieldMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTranscationDataLoadFieldMappingDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataLoadIdProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.Int32>>(o => o.DataLoadId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string DbcolumnNameProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,System.String>(o => o.DbcolumnName);
        public static readonly string IsConditionMappingProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.Boolean>>(o => o.IsConditionMapping);
        public static readonly string WhereClauseProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,System.String>(o => o.WhereClause);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTranscationDataLoadFieldMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTranscationDataLoadFieldMappingDto()
        {        
        }
		
		static AppTranscationDataLoadFieldMappingDto()
        {
                       
			  
			ForeignKeyProperties.Add(DataLoadIdProperty);          		
                
			DictStringPropertyMaxLength.Add(DbcolumnNameProperty,100);  
			DictStringPropertyMaxLength.Add(WhereClauseProperty,500);       
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
    


        /// <summary> The DataLoadId property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataLoadId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataLoadIdProperty);}
            set { SetValue(DataLoadIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The DbcolumnName property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DbcolumnName
        {
            get { return  GetValue<System.String>( DbcolumnNameProperty);}
            set { SetValue(DbcolumnNameProperty,value); }
        }

        /// <summary> The IsConditionMapping property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsConditionMapping
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsConditionMappingProperty);}
            set { SetValue(IsConditionMappingProperty,value); }
        }

        /// <summary> The WhereClause property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WhereClause
        {
            get { return  GetValue<System.String>( WhereClauseProperty);}
            set { SetValue(WhereClauseProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTranscationDataLoadFieldMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTranscationDataLoadFieldMapping</summary>
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

