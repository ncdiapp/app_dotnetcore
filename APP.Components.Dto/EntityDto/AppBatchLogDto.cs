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
    /// DTO class for the entity 'AppBatchLog'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBatchLogDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string BatchNumberProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,System.String>(o => o.BatchNumber);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,System.String>(o => o.Description);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionRidProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,System.String>(o => o.TransactionRid);
        public static readonly string SequenceNumberProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.Int32>>(o => o.SequenceNumber);
        public static readonly string StartTimeProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.DateTime>>(o => o.StartTime);
        public static readonly string EndTimeProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.DateTime>>(o => o.EndTime);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppBatchLogDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppBatchLogDto()
        {        
        }
		
		static AppBatchLogDto()
        {
                         
			             		
              
			DictStringPropertyMaxLength.Add(BatchNumberProperty,2000); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,4000);  
			DictStringPropertyMaxLength.Add(TransactionRidProperty,4000);          
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
    


        /// <summary> The BatchNumber property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BatchNumber
        {
            get { return  GetValue<System.String>( BatchNumberProperty);}
            set { SetValue(BatchNumberProperty,value); }
        }

        /// <summary> The Description property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionRid property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRid
        {
            get { return  GetValue<System.String>( TransactionRidProperty);}
            set { SetValue(TransactionRidProperty,value); }
        }

        /// <summary> The SequenceNumber property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SequenceNumber
        {
            get { return  GetValue<Nullable<System.Int32>>( SequenceNumberProperty);}
            set { SetValue(SequenceNumberProperty,value); }
        }

        /// <summary> The StartTime property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> StartTime
        {
            get { return  GetValue<Nullable<System.DateTime>>( StartTimeProperty);}
            set { SetValue(StartTimeProperty,value); }
        }

        /// <summary> The EndTime property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> EndTime
        {
            get { return  GetValue<Nullable<System.DateTime>>( EndTimeProperty);}
            set { SetValue(EndTimeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppBatchLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppBatchLog</summary>
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

