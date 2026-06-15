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
    /// DTO class for the entity 'AppTransactionUnitExtendFieldValue'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitExtendFieldValueDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string UnitExtendFiledIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.Int32>>(o => o.UnitExtendFiledId);
        public static readonly string UnitPkvalueProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,System.String>(o => o.UnitPkvalue);
        public static readonly string ValueTextProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,System.String>(o => o.ValueText);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldValueDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitExtendFieldValueDto()
        {        
        }
		
		static AppTransactionUnitExtendFieldValueDto()
        {
                      
			   
			ForeignKeyProperties.Add(UnitExtendFiledIdProperty);        		
                
			DictStringPropertyMaxLength.Add(UnitPkvalueProperty,100); 
			DictStringPropertyMaxLength.Add(ValueTextProperty,2147483647);       
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
    


        /// <summary> The TransactionUnitId property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The UnitExtendFiledId property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UnitExtendFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( UnitExtendFiledIdProperty);}
            set { SetValue(UnitExtendFiledIdProperty,value); }
        }

        /// <summary> The UnitPkvalue property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UnitPkvalue
        {
            get { return  GetValue<System.String>( UnitPkvalueProperty);}
            set { SetValue(UnitPkvalueProperty,value); }
        }

        /// <summary> The ValueText property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ValueText
        {
            get { return  GetValue<System.String>( ValueTextProperty);}
            set { SetValue(ValueTextProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnitExtendFieldValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }
        
        #endregion

       
        
    }
}

