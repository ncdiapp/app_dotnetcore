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
    /// DTO class for the entity 'AppTransactionUnitExtendField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitExtendFieldDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string FieldNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,System.String>(o => o.FieldName);
        public static readonly string ControlTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.ControlType);
        public static readonly string DataTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.DataType);
        public static readonly string DisplayNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,System.String>(o => o.DisplayName);
        public static readonly string DefaultValueProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,System.String>(o => o.DefaultValue);
        public static readonly string EntityIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.EntityId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitExtendFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitExtendFieldDto()
        {        
        }
		
		static AppTransactionUnitExtendFieldDto()
        {
                  
			MandatoryProperties.Add(DisplayNameProperty);        
			  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);            		
               
			DictStringPropertyMaxLength.Add(FieldNameProperty,100);   
			DictStringPropertyMaxLength.Add(DisplayNameProperty,500); 
			DictStringPropertyMaxLength.Add(DefaultValueProperty,500);        
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
    


        /// <summary> The TransactionUnitId property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The FieldName property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FieldName
        {
            get { return  GetValue<System.String>( FieldNameProperty);}
            set { SetValue(FieldNameProperty,value); }
        }

        /// <summary> The ControlType property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ControlType
        {
            get { return  GetValue<Nullable<System.Int32>>( ControlTypeProperty);}
            set { SetValue(ControlTypeProperty,value); }
        }

        /// <summary> The DataType property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTypeProperty);}
            set { SetValue(DataTypeProperty,value); }
        }

        /// <summary> The DisplayName property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayName
        {
            get { return  GetValue<System.String>( DisplayNameProperty);}
            set { SetValue(DisplayNameProperty,value); }
        }

        /// <summary> The DefaultValue property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefaultValue
        {
            get { return  GetValue<System.String>( DefaultValueProperty);}
            set { SetValue(DefaultValueProperty,value); }
        }

        /// <summary> The EntityId property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EntityId
        {
            get { return  GetValue<Nullable<System.Int32>>( EntityIdProperty);}
            set { SetValue(EntityIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnitExtendField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnitExtendField</summary>
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

