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
    /// DTO class for the entity 'AppWorkflowLibAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWorkflowLibActionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.String>(o => o.Description);
        public static readonly string ActionTypeProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.ActionType);
        public static readonly string RowIdentityProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.Guid>(o => o.RowIdentity);
        public static readonly string NotificationSubjectProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.String>(o => o.NotificationSubject);
        public static readonly string NotificationMessageProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.String>(o => o.NotificationMessage);
        public static readonly string NotificationDestinationProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.String>(o => o.NotificationDestination);
        public static readonly string NotificationDestinationUserIdtransactionFiledIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.NotificationDestinationUserIdtransactionFiledId);
        public static readonly string NotificationDestinationRoleIdtransactionFiledIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.NotificationDestinationRoleIdtransactionFiledId);
        public static readonly string MessageContentQueryDataSetIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.MessageContentQueryDataSetId);
        public static readonly string DataSetQeuryStringProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,System.String>(o => o.DataSetQeuryString);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string ChangeTypeForTransactionUnitFieldProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.ChangeTypeForTransactionUnitField);
        public static readonly string MessageTemplateIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.MessageTemplateId);
        public static readonly string IsNeedToAttachFormProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Boolean>>(o => o.IsNeedToAttachForm);
        public static readonly string IsNeedToAttachAllFormFilesProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Boolean>>(o => o.IsNeedToAttachAllFormFiles);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppWorkflowLibActionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppWorkflowLibActionDto()
        {        
        }
		
		static AppWorkflowLibActionDto()
        {
                 
			MandatoryProperties.Add(RowIdentityProperty);                    
			                        		
              
			DictStringPropertyMaxLength.Add(NameProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);   
			DictStringPropertyMaxLength.Add(NotificationSubjectProperty,500); 
			DictStringPropertyMaxLength.Add(NotificationMessageProperty,4000); 
			DictStringPropertyMaxLength.Add(NotificationDestinationProperty,500);    
			DictStringPropertyMaxLength.Add(DataSetQeuryStringProperty,2000);              
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
    


        /// <summary> The Name property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ActionType property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ActionType
        {
            get { return  GetValue<Nullable<System.Int32>>( ActionTypeProperty);}
            set { SetValue(ActionTypeProperty,value); }
        }

        /// <summary> The RowIdentity property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Guid RowIdentity
        {
            get { return  GetValue<System.Guid>( RowIdentityProperty);}
            set { SetValue(RowIdentityProperty,value); }
        }

        /// <summary> The NotificationSubject property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationSubject
        {
            get { return  GetValue<System.String>( NotificationSubjectProperty);}
            set { SetValue(NotificationSubjectProperty,value); }
        }

        /// <summary> The NotificationMessage property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationMessage
        {
            get { return  GetValue<System.String>( NotificationMessageProperty);}
            set { SetValue(NotificationMessageProperty,value); }
        }

        /// <summary> The NotificationDestination property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationDestination
        {
            get { return  GetValue<System.String>( NotificationDestinationProperty);}
            set { SetValue(NotificationDestinationProperty,value); }
        }

        /// <summary> The NotificationDestinationUserIdtransactionFiledId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NotificationDestinationUserIdtransactionFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( NotificationDestinationUserIdtransactionFiledIdProperty);}
            set { SetValue(NotificationDestinationUserIdtransactionFiledIdProperty,value); }
        }

        /// <summary> The NotificationDestinationRoleIdtransactionFiledId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NotificationDestinationRoleIdtransactionFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( NotificationDestinationRoleIdtransactionFiledIdProperty);}
            set { SetValue(NotificationDestinationRoleIdtransactionFiledIdProperty,value); }
        }

        /// <summary> The MessageContentQueryDataSetId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessageContentQueryDataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( MessageContentQueryDataSetIdProperty);}
            set { SetValue(MessageContentQueryDataSetIdProperty,value); }
        }

        /// <summary> The DataSetQeuryString property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataSetQeuryString
        {
            get { return  GetValue<System.String>( DataSetQeuryStringProperty);}
            set { SetValue(DataSetQeuryStringProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionUnitId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The ChangeTypeForTransactionUnitField property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChangeTypeForTransactionUnitField
        {
            get { return  GetValue<Nullable<System.Int32>>( ChangeTypeForTransactionUnitFieldProperty);}
            set { SetValue(ChangeTypeForTransactionUnitFieldProperty,value); }
        }

        /// <summary> The MessageTemplateId property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessageTemplateId
        {
            get { return  GetValue<Nullable<System.Int32>>( MessageTemplateIdProperty);}
            set { SetValue(MessageTemplateIdProperty,value); }
        }

        /// <summary> The IsNeedToAttachForm property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedToAttachForm
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedToAttachFormProperty);}
            set { SetValue(IsNeedToAttachFormProperty,value); }
        }

        /// <summary> The IsNeedToAttachAllFormFiles property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedToAttachAllFormFiles
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedToAttachAllFormFilesProperty);}
            set { SetValue(IsNeedToAttachAllFormFilesProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppWorkflowLibAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppWorkflowLibAction</summary>
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

