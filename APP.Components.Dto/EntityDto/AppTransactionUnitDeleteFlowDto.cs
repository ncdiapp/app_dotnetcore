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
    /// DTO class for the entity 'AppTransactionUnitDeleteFlow'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitDeleteFlowDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string RelativeTableNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,System.String>(o => o.RelativeTableName);
        public static readonly string RelativeForeignKeyNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,System.String>(o => o.RelativeForeignKeyName);
        public static readonly string IsForcedDeleteProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Boolean>>(o => o.IsForcedDelete);
        public static readonly string IsSetEmptyProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Boolean>>(o => o.IsSetEmpty);
        public static readonly string IsNotAllowedDeleteWithMsgProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Boolean>>(o => o.IsNotAllowedDeleteWithMsg);
        public static readonly string DeleteFlowPriorityProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Int32>>(o => o.DeleteFlowPriority);
        public static readonly string WarningMessageProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,System.String>(o => o.WarningMessage);
        public static readonly string StoredProcedureNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,System.String>(o => o.StoredProcedureName);
        public static readonly string SpParameterOptionsProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,System.String>(o => o.SpParameterOptions);
        public static readonly string DeleteValidationStoredProcedureNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,System.String>(o => o.DeleteValidationStoredProcedureName);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDeleteFlowDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitDeleteFlowDto()
        {        
        }
		
		static AppTransactionUnitDeleteFlowDto()
        {
                             
			  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);                		
               
			DictStringPropertyMaxLength.Add(RelativeTableNameProperty,200); 
			DictStringPropertyMaxLength.Add(RelativeForeignKeyNameProperty,200);     
			DictStringPropertyMaxLength.Add(WarningMessageProperty,200); 
			DictStringPropertyMaxLength.Add(StoredProcedureNameProperty,200); 
			DictStringPropertyMaxLength.Add(SpParameterOptionsProperty,500); 
			DictStringPropertyMaxLength.Add(DeleteValidationStoredProcedureNameProperty,200);       
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
    


        /// <summary> The TransactionUnitId property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The RelativeTableName property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RelativeTableName
        {
            get { return  GetValue<System.String>( RelativeTableNameProperty);}
            set { SetValue(RelativeTableNameProperty,value); }
        }

        /// <summary> The RelativeForeignKeyName property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RelativeForeignKeyName
        {
            get { return  GetValue<System.String>( RelativeForeignKeyNameProperty);}
            set { SetValue(RelativeForeignKeyNameProperty,value); }
        }

        /// <summary> The IsForcedDelete property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsForcedDelete
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsForcedDeleteProperty);}
            set { SetValue(IsForcedDeleteProperty,value); }
        }

        /// <summary> The IsSetEmpty property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSetEmpty
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSetEmptyProperty);}
            set { SetValue(IsSetEmptyProperty,value); }
        }

        /// <summary> The IsNotAllowedDeleteWithMsg property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNotAllowedDeleteWithMsg
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNotAllowedDeleteWithMsgProperty);}
            set { SetValue(IsNotAllowedDeleteWithMsgProperty,value); }
        }

        /// <summary> The DeleteFlowPriority property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DeleteFlowPriority
        {
            get { return  GetValue<Nullable<System.Int32>>( DeleteFlowPriorityProperty);}
            set { SetValue(DeleteFlowPriorityProperty,value); }
        }

        /// <summary> The WarningMessage property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WarningMessage
        {
            get { return  GetValue<System.String>( WarningMessageProperty);}
            set { SetValue(WarningMessageProperty,value); }
        }

        /// <summary> The StoredProcedureName property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StoredProcedureName
        {
            get { return  GetValue<System.String>( StoredProcedureNameProperty);}
            set { SetValue(StoredProcedureNameProperty,value); }
        }

        /// <summary> The SpParameterOptions property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SpParameterOptions
        {
            get { return  GetValue<System.String>( SpParameterOptionsProperty);}
            set { SetValue(SpParameterOptionsProperty,value); }
        }

        /// <summary> The DeleteValidationStoredProcedureName property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DeleteValidationStoredProcedureName
        {
            get { return  GetValue<System.String>( DeleteValidationStoredProcedureNameProperty);}
            set { SetValue(DeleteValidationStoredProcedureNameProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnitDeleteFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnitDeleteFlow</summary>
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

