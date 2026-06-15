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
    /// DTO class for the entity 'AppTransactionSaveAsMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionSaveAsMappingDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataTransferSettingIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.DataTransferSettingId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,System.String>(o => o.Name);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string MappingUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.MappingUnitId);
        public static readonly string SourceFiledIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.SourceFiledId);
        public static readonly string TargetFiledIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.TargetFiledId);
        public static readonly string IsBlankTargetFieldProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Boolean>>(o => o.IsBlankTargetField);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string JsonPropertyPathNameProperty = ObjectInfoHelper.GetName<AppTransactionSaveAsMappingDto ,System.String>(o => o.JsonPropertyPathName);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionSaveAsMappingDto()
        {        
        }
		
		static AppTransactionSaveAsMappingDto()
        {
                          
			  
			ForeignKeyProperties.Add(DataTransferSettingIdProperty);   
			ForeignKeyProperties.Add(TransactionIdProperty);  
			ForeignKeyProperties.Add(MappingUnitIdProperty);  
			ForeignKeyProperties.Add(SourceFiledIdProperty);  
			ForeignKeyProperties.Add(TargetFiledIdProperty);        		
               
			DictStringPropertyMaxLength.Add(NameProperty,200);           
			DictStringPropertyMaxLength.Add(JsonPropertyPathNameProperty,200);  
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
    


        /// <summary> The DataTransferSettingId property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataTransferSettingId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTransferSettingIdProperty);}
            set { SetValue(DataTransferSettingIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The MappingUnitId property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MappingUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( MappingUnitIdProperty);}
            set { SetValue(MappingUnitIdProperty,value); }
        }

        /// <summary> The SourceFiledId property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceFiledIdProperty);}
            set { SetValue(SourceFiledIdProperty,value); }
        }

        /// <summary> The TargetFiledId property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetFiledIdProperty);}
            set { SetValue(TargetFiledIdProperty,value); }
        }

        /// <summary> The IsBlankTargetField property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsBlankTargetField
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsBlankTargetFieldProperty);}
            set { SetValue(IsBlankTargetFieldProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The JsonPropertyPathName property of the Entity AppTransactionSaveAsMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String JsonPropertyPathName
        {
            get { return  GetValue<System.String>( JsonPropertyPathNameProperty);}
            set { SetValue(JsonPropertyPathNameProperty,value); }
        }
        
        #endregion

       
        
    }
}

