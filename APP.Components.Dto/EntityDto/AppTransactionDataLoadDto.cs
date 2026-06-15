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
    /// DTO class for the entity 'AppTransactionDataLoad'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionDataLoadDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataSetIdProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.DataSetId);
        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string LoadNameProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,System.String>(o => o.LoadName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,System.String>(o => o.Description);
        public static readonly string LoadOrderProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.LoadOrder);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsAutoExcutedWhenOpenEditFormProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Boolean>>(o => o.IsAutoExcutedWhenOpenEditForm);
        public static readonly string IsAutoExecuteBeforeIntialCscadingProperty = ObjectInfoHelper.GetName<AppTransactionDataLoadDto ,Nullable<System.Boolean>>(o => o.IsAutoExecuteBeforeIntialCscading);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionDataLoadDto()
        {        
        }
		
		static AppTransactionDataLoadDto()
        {
                          
			  
			ForeignKeyProperties.Add(DataSetIdProperty);  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);  
			ForeignKeyProperties.Add(TransactionIdProperty);           		
                 
			DictStringPropertyMaxLength.Add(LoadNameProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,100);          
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
    


        /// <summary> The DataSetId property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSetIdProperty);}
            set { SetValue(DataSetIdProperty,value); }
        }

        /// <summary> The TransactionUnitId property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The LoadName property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LoadName
        {
            get { return  GetValue<System.String>( LoadNameProperty);}
            set { SetValue(LoadNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The LoadOrder property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LoadOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( LoadOrderProperty);}
            set { SetValue(LoadOrderProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsAutoExcutedWhenOpenEditForm property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAutoExcutedWhenOpenEditForm
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAutoExcutedWhenOpenEditFormProperty);}
            set { SetValue(IsAutoExcutedWhenOpenEditFormProperty,value); }
        }

        /// <summary> The IsAutoExecuteBeforeIntialCscading property of the Entity AppTransactionDataLoad</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAutoExecuteBeforeIntialCscading
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAutoExecuteBeforeIntialCscadingProperty);}
            set { SetValue(IsAutoExecuteBeforeIntialCscadingProperty,value); }
        }
        
        #endregion

       
        
    }
}

