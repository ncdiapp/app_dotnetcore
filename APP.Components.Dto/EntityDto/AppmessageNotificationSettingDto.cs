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
    /// DTO class for the entity 'AppmessageNotificationSetting'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppmessageNotificationSettingDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TranscationIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.TranscationId);
        public static readonly string ProejctIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.ProejctId);
        public static readonly string SettingNameProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,System.String>(o => o.SettingName);
        public static readonly string NotificationQueryProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,System.String>(o => o.NotificationQuery);
        public static readonly string MessageUsageTypeProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.MessageUsageType);
        public static readonly string MessageTemplateIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.MessageTemplateId);
        public static readonly string MessageTemplateProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,System.String>(o => o.MessageTemplate);
        public static readonly string EmScanPeriodProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.EmScanPeriod);
        public static readonly string NotificationQueryContentSettingIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.NotificationQueryContentSettingId);
        public static readonly string AlertSpanTimeProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.AlertSpanTime);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppmessageNotificationSettingDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppmessageNotificationSettingDto()
        {        
        }
		
		static AppmessageNotificationSettingDto()
        {
                            
			  
			ForeignKeyProperties.Add(TranscationIdProperty);         
			ForeignKeyProperties.Add(NotificationQueryContentSettingIdProperty);       		
                
			DictStringPropertyMaxLength.Add(SettingNameProperty,500); 
			DictStringPropertyMaxLength.Add(NotificationQueryProperty,2147483647);   
			DictStringPropertyMaxLength.Add(MessageTemplateProperty,2147483647);          
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
    


        /// <summary> The TranscationId property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TranscationId
        {
            get { return  GetValue<Nullable<System.Int32>>( TranscationIdProperty);}
            set { SetValue(TranscationIdProperty,value); }
        }

        /// <summary> The ProejctId property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProejctId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProejctIdProperty);}
            set { SetValue(ProejctIdProperty,value); }
        }

        /// <summary> The SettingName property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SettingName
        {
            get { return  GetValue<System.String>( SettingNameProperty);}
            set { SetValue(SettingNameProperty,value); }
        }

        /// <summary> The NotificationQuery property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationQuery
        {
            get { return  GetValue<System.String>( NotificationQueryProperty);}
            set { SetValue(NotificationQueryProperty,value); }
        }

        /// <summary> The MessageUsageType property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessageUsageType
        {
            get { return  GetValue<Nullable<System.Int32>>( MessageUsageTypeProperty);}
            set { SetValue(MessageUsageTypeProperty,value); }
        }

        /// <summary> The MessageTemplateId property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessageTemplateId
        {
            get { return  GetValue<Nullable<System.Int32>>( MessageTemplateIdProperty);}
            set { SetValue(MessageTemplateIdProperty,value); }
        }

        /// <summary> The MessageTemplate property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MessageTemplate
        {
            get { return  GetValue<System.String>( MessageTemplateProperty);}
            set { SetValue(MessageTemplateProperty,value); }
        }

        /// <summary> The EmScanPeriod property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmScanPeriod
        {
            get { return  GetValue<Nullable<System.Int32>>( EmScanPeriodProperty);}
            set { SetValue(EmScanPeriodProperty,value); }
        }

        /// <summary> The NotificationQueryContentSettingId property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NotificationQueryContentSettingId
        {
            get { return  GetValue<Nullable<System.Int32>>( NotificationQueryContentSettingIdProperty);}
            set { SetValue(NotificationQueryContentSettingIdProperty,value); }
        }

        /// <summary> The AlertSpanTime property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AlertSpanTime
        {
            get { return  GetValue<Nullable<System.Int32>>( AlertSpanTimeProperty);}
            set { SetValue(AlertSpanTimeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppmessageNotificationSetting</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppmessageNotificationSetting</summary>
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

