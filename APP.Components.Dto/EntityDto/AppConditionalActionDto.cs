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
    /// DTO class for the entity 'AppConditionalAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppConditionalActionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,System.String>(o => o.Name);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string ConditionUnitIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.ConditionUnitId);
        public static readonly string BooleanConditionFieldIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.BooleanConditionFieldId);
        public static readonly string UitriggerTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.UitriggerTransactionFieldId);
        public static readonly string BooleanConditionFormulaProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,System.String>(o => o.BooleanConditionFormula);
        public static readonly string LockingTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.LockingTransactionFieldId);
        public static readonly string LockingFieldUnitIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.LockingFieldUnitId);
        public static readonly string IsLockingTransactionProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Boolean>>(o => o.IsLockingTransaction);
        public static readonly string LockingTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.LockingTransactionUnitId);
        public static readonly string NotificationTemplateMessgeIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.NotificationTemplateMessgeId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsLockForSpecailEditPrivilegeProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Boolean>>(o => o.IsLockForSpecailEditPrivilege);
        public static readonly string NeedToHideTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppConditionalActionDto ,Nullable<System.Int32>>(o => o.NeedToHideTransactionFieldId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppConditionalActionDto()
        {        
        }
		
		static AppConditionalActionDto()
        {
                               
			   
			ForeignKeyProperties.Add(TransactionIdProperty);  
			ForeignKeyProperties.Add(ConditionUnitIdProperty);  
			ForeignKeyProperties.Add(BooleanConditionFieldIdProperty);  
			ForeignKeyProperties.Add(UitriggerTransactionFieldIdProperty);   
			ForeignKeyProperties.Add(LockingTransactionFieldIdProperty);  
			ForeignKeyProperties.Add(LockingFieldUnitIdProperty);   
			ForeignKeyProperties.Add(LockingTransactionUnitIdProperty);  
			ForeignKeyProperties.Add(NotificationTemplateMessgeIdProperty);        
			ForeignKeyProperties.Add(NeedToHideTransactionFieldIdProperty); 		
              
			DictStringPropertyMaxLength.Add(NameProperty,200);     
			DictStringPropertyMaxLength.Add(BooleanConditionFormulaProperty,500);              
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
    


        /// <summary> The Name property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The ConditionUnitId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConditionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( ConditionUnitIdProperty);}
            set { SetValue(ConditionUnitIdProperty,value); }
        }

        /// <summary> The BooleanConditionFieldId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> BooleanConditionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( BooleanConditionFieldIdProperty);}
            set { SetValue(BooleanConditionFieldIdProperty,value); }
        }

        /// <summary> The UitriggerTransactionFieldId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UitriggerTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( UitriggerTransactionFieldIdProperty);}
            set { SetValue(UitriggerTransactionFieldIdProperty,value); }
        }

        /// <summary> The BooleanConditionFormula property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BooleanConditionFormula
        {
            get { return  GetValue<System.String>( BooleanConditionFormulaProperty);}
            set { SetValue(BooleanConditionFormulaProperty,value); }
        }

        /// <summary> The LockingTransactionFieldId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LockingTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( LockingTransactionFieldIdProperty);}
            set { SetValue(LockingTransactionFieldIdProperty,value); }
        }

        /// <summary> The LockingFieldUnitId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LockingFieldUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( LockingFieldUnitIdProperty);}
            set { SetValue(LockingFieldUnitIdProperty,value); }
        }

        /// <summary> The IsLockingTransaction property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsLockingTransaction
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsLockingTransactionProperty);}
            set { SetValue(IsLockingTransactionProperty,value); }
        }

        /// <summary> The LockingTransactionUnitId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LockingTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( LockingTransactionUnitIdProperty);}
            set { SetValue(LockingTransactionUnitIdProperty,value); }
        }

        /// <summary> The NotificationTemplateMessgeId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NotificationTemplateMessgeId
        {
            get { return  GetValue<Nullable<System.Int32>>( NotificationTemplateMessgeIdProperty);}
            set { SetValue(NotificationTemplateMessgeIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsLockForSpecailEditPrivilege property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsLockForSpecailEditPrivilege
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsLockForSpecailEditPrivilegeProperty);}
            set { SetValue(IsLockForSpecailEditPrivilegeProperty,value); }
        }

        /// <summary> The NeedToHideTransactionFieldId property of the Entity AppConditionalAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NeedToHideTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( NeedToHideTransactionFieldIdProperty);}
            set { SetValue(NeedToHideTransactionFieldIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

