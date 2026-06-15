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
    /// DTO class for the entity 'AppTransactionGroupItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionGroupItemDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionGroupIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.TransactionGroupId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionCaculationFlowOrderProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.TransactionCaculationFlowOrder);
        public static readonly string TransactionLayoutOrderProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.TransactionLayoutOrder);
        public static readonly string IsCrossGroupSharedHeaderProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Boolean>>(o => o.IsCrossGroupSharedHeader);
        public static readonly string IsGroupSharedHeaderProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Boolean>>(o => o.IsGroupSharedHeader);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionGroupItemDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionGroupItemDto()
        {        
        }
		
		static AppTransactionGroupItemDto()
        {
                        
			  
			ForeignKeyProperties.Add(TransactionGroupIdProperty);  
			ForeignKeyProperties.Add(TransactionIdProperty);          		
                          
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
    


        /// <summary> The TransactionGroupId property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionGroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionGroupIdProperty);}
            set { SetValue(TransactionGroupIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionCaculationFlowOrder property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionCaculationFlowOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionCaculationFlowOrderProperty);}
            set { SetValue(TransactionCaculationFlowOrderProperty,value); }
        }

        /// <summary> The TransactionLayoutOrder property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionLayoutOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionLayoutOrderProperty);}
            set { SetValue(TransactionLayoutOrderProperty,value); }
        }

        /// <summary> The IsCrossGroupSharedHeader property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsCrossGroupSharedHeader
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsCrossGroupSharedHeaderProperty);}
            set { SetValue(IsCrossGroupSharedHeaderProperty,value); }
        }

        /// <summary> The IsGroupSharedHeader property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsGroupSharedHeader
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsGroupSharedHeaderProperty);}
            set { SetValue(IsGroupSharedHeaderProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionGroupItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionGroupItem</summary>
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

