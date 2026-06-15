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
    /// DTO class for the entity 'AppProjectWorkFlowAction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectWorkFlowActionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string WorkFlowConditionIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.WorkFlowConditionId);
        public static readonly string CommandTransactionIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.CommandTransactionId);
        public static readonly string CommandConditionTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.CommandConditionTransactionFieldId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.Description);
        public static readonly string ActionTypeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.ActionType);
        public static readonly string UpdateActionTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.UpdateActionTransactionFieldId);
        public static readonly string FormulaExpressionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.FormulaExpression);
        public static readonly string NextWorkFlowIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.NextWorkFlowId);
        public static readonly string RowIdentityProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.Guid>(o => o.RowIdentity);
        public static readonly string NextTransactionRidProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.NextTransactionRid);
        public static readonly string NextTransactionIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.NextTransactionId);
        public static readonly string NextProjectIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.NextProjectId);
        public static readonly string ExcutionDateTimeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.DateTime>>(o => o.ExcutionDateTime);
        public static readonly string ExcutedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.ExcutedById);
        public static readonly string NotificationSubjectProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.NotificationSubject);
        public static readonly string NotificationMessageProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.NotificationMessage);
        public static readonly string NotificationDestinationProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.NotificationDestination);
        public static readonly string NotificationDestinationUserIdtransactionFiledIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.NotificationDestinationUserIdtransactionFiledId);
        public static readonly string PathUilayoutProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.PathUilayout);
        public static readonly string ActionFlowOrderProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.ActionFlowOrder);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string NotificationDestinationRoleIdtransactionFiledIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.NotificationDestinationRoleIdtransactionFiledId);
        public static readonly string MessageContentQueryDataSetIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.MessageContentQueryDataSetId);
        public static readonly string DataSetQeuryStringProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.DataSetQeuryString);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string ChangeTypeForTransactionUnitFieldProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.ChangeTypeForTransactionUnitField);
        public static readonly string MessageTemplateIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.MessageTemplateId);
        public static readonly string IsNeedToAttachFormProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Boolean>>(o => o.IsNeedToAttachForm);
        public static readonly string IsNeedToAttachAllFormFilesProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Boolean>>(o => o.IsNeedToAttachAllFormFiles);
        public static readonly string DataLoadIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.DataLoadId);
        public static readonly string ActionGuidProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Guid>>(o => o.ActionGuid);
        public static readonly string DataTransferSettingIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.DataTransferSettingId);
        public static readonly string OtherOptionsProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.OtherOptions);
        public static readonly string CommandUioptionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.CommandUioption);
        public static readonly string CommandSearchViewIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,Nullable<System.Int32>>(o => o.CommandSearchViewId);
        public static readonly string CommandConditionExpressionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowActionDto ,System.String>(o => o.CommandConditionExpression);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectWorkFlowActionDto()
        {        
        }
		
		static AppProjectWorkFlowActionDto()
        {
                       
			MandatoryProperties.Add(RowIdentityProperty);                                  
			  
			ForeignKeyProperties.Add(WorkFlowConditionIdProperty);   
			ForeignKeyProperties.Add(CommandConditionTransactionFieldIdProperty);     
			ForeignKeyProperties.Add(UpdateActionTransactionFieldIdProperty);   
			ForeignKeyProperties.Add(NextWorkFlowIdProperty);    
			ForeignKeyProperties.Add(NextTransactionIdProperty);  
			ForeignKeyProperties.Add(NextProjectIdProperty);                
			ForeignKeyProperties.Add(MessageContentQueryDataSetIdProperty);   
			ForeignKeyProperties.Add(TransactionIdProperty);   
			ForeignKeyProperties.Add(TransactionFieldIdProperty);   
			ForeignKeyProperties.Add(MessageTemplateIdProperty);    
			ForeignKeyProperties.Add(DataLoadIdProperty);   
			ForeignKeyProperties.Add(DataTransferSettingIdProperty);    
			ForeignKeyProperties.Add(CommandSearchViewIdProperty);  		
                 
			DictStringPropertyMaxLength.Add(NameProperty,500); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);   
			DictStringPropertyMaxLength.Add(FormulaExpressionProperty,2147483647);        
			DictStringPropertyMaxLength.Add(NotificationSubjectProperty,500); 
			DictStringPropertyMaxLength.Add(NotificationMessageProperty,2147483647); 
			DictStringPropertyMaxLength.Add(NotificationDestinationProperty,500);  
			DictStringPropertyMaxLength.Add(PathUilayoutProperty,1000);         
			DictStringPropertyMaxLength.Add(DataSetQeuryStringProperty,2147483647);           
			DictStringPropertyMaxLength.Add(OtherOptionsProperty,4000); 
			DictStringPropertyMaxLength.Add(CommandUioptionProperty,4000);  
			DictStringPropertyMaxLength.Add(CommandConditionExpressionProperty,2000);  
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
    


        /// <summary> The WorkFlowConditionId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WorkFlowConditionId
        {
            get { return  GetValue<Nullable<System.Int32>>( WorkFlowConditionIdProperty);}
            set { SetValue(WorkFlowConditionIdProperty,value); }
        }

        /// <summary> The CommandTransactionId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CommandTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( CommandTransactionIdProperty);}
            set { SetValue(CommandTransactionIdProperty,value); }
        }

        /// <summary> The CommandConditionTransactionFieldId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CommandConditionTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( CommandConditionTransactionFieldIdProperty);}
            set { SetValue(CommandConditionTransactionFieldIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ActionType property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ActionType
        {
            get { return  GetValue<Nullable<System.Int32>>( ActionTypeProperty);}
            set { SetValue(ActionTypeProperty,value); }
        }

        /// <summary> The UpdateActionTransactionFieldId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UpdateActionTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( UpdateActionTransactionFieldIdProperty);}
            set { SetValue(UpdateActionTransactionFieldIdProperty,value); }
        }

        /// <summary> The FormulaExpression property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FormulaExpression
        {
            get { return  GetValue<System.String>( FormulaExpressionProperty);}
            set { SetValue(FormulaExpressionProperty,value); }
        }

        /// <summary> The NextWorkFlowId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NextWorkFlowId
        {
            get { return  GetValue<Nullable<System.Int32>>( NextWorkFlowIdProperty);}
            set { SetValue(NextWorkFlowIdProperty,value); }
        }

        /// <summary> The RowIdentity property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Guid RowIdentity
        {
            get { return  GetValue<System.Guid>( RowIdentityProperty);}
            set { SetValue(RowIdentityProperty,value); }
        }

        /// <summary> The NextTransactionRid property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NextTransactionRid
        {
            get { return  GetValue<Nullable<System.Int32>>( NextTransactionRidProperty);}
            set { SetValue(NextTransactionRidProperty,value); }
        }

        /// <summary> The NextTransactionId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NextTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( NextTransactionIdProperty);}
            set { SetValue(NextTransactionIdProperty,value); }
        }

        /// <summary> The NextProjectId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NextProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( NextProjectIdProperty);}
            set { SetValue(NextProjectIdProperty,value); }
        }

        /// <summary> The ExcutionDateTime property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> ExcutionDateTime
        {
            get { return  GetValue<Nullable<System.DateTime>>( ExcutionDateTimeProperty);}
            set { SetValue(ExcutionDateTimeProperty,value); }
        }

        /// <summary> The ExcutedById property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ExcutedById
        {
            get { return  GetValue<Nullable<System.Int32>>( ExcutedByIdProperty);}
            set { SetValue(ExcutedByIdProperty,value); }
        }

        /// <summary> The NotificationSubject property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationSubject
        {
            get { return  GetValue<System.String>( NotificationSubjectProperty);}
            set { SetValue(NotificationSubjectProperty,value); }
        }

        /// <summary> The NotificationMessage property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationMessage
        {
            get { return  GetValue<System.String>( NotificationMessageProperty);}
            set { SetValue(NotificationMessageProperty,value); }
        }

        /// <summary> The NotificationDestination property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationDestination
        {
            get { return  GetValue<System.String>( NotificationDestinationProperty);}
            set { SetValue(NotificationDestinationProperty,value); }
        }

        /// <summary> The NotificationDestinationUserIdtransactionFiledId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NotificationDestinationUserIdtransactionFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( NotificationDestinationUserIdtransactionFiledIdProperty);}
            set { SetValue(NotificationDestinationUserIdtransactionFiledIdProperty,value); }
        }

        /// <summary> The PathUilayout property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PathUilayout
        {
            get { return  GetValue<System.String>( PathUilayoutProperty);}
            set { SetValue(PathUilayoutProperty,value); }
        }

        /// <summary> The ActionFlowOrder property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ActionFlowOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( ActionFlowOrderProperty);}
            set { SetValue(ActionFlowOrderProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The NotificationDestinationRoleIdtransactionFiledId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NotificationDestinationRoleIdtransactionFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( NotificationDestinationRoleIdtransactionFiledIdProperty);}
            set { SetValue(NotificationDestinationRoleIdtransactionFiledIdProperty,value); }
        }

        /// <summary> The MessageContentQueryDataSetId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessageContentQueryDataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( MessageContentQueryDataSetIdProperty);}
            set { SetValue(MessageContentQueryDataSetIdProperty,value); }
        }

        /// <summary> The DataSetQeuryString property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataSetQeuryString
        {
            get { return  GetValue<System.String>( DataSetQeuryStringProperty);}
            set { SetValue(DataSetQeuryStringProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionUnitId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The ChangeTypeForTransactionUnitField property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChangeTypeForTransactionUnitField
        {
            get { return  GetValue<Nullable<System.Int32>>( ChangeTypeForTransactionUnitFieldProperty);}
            set { SetValue(ChangeTypeForTransactionUnitFieldProperty,value); }
        }

        /// <summary> The MessageTemplateId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MessageTemplateId
        {
            get { return  GetValue<Nullable<System.Int32>>( MessageTemplateIdProperty);}
            set { SetValue(MessageTemplateIdProperty,value); }
        }

        /// <summary> The IsNeedToAttachForm property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedToAttachForm
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedToAttachFormProperty);}
            set { SetValue(IsNeedToAttachFormProperty,value); }
        }

        /// <summary> The IsNeedToAttachAllFormFiles property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedToAttachAllFormFiles
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedToAttachAllFormFilesProperty);}
            set { SetValue(IsNeedToAttachAllFormFilesProperty,value); }
        }

        /// <summary> The DataLoadId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataLoadId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataLoadIdProperty);}
            set { SetValue(DataLoadIdProperty,value); }
        }

        /// <summary> The ActionGuid property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> ActionGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( ActionGuidProperty);}
            set { SetValue(ActionGuidProperty,value); }
        }

        /// <summary> The DataTransferSettingId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataTransferSettingId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTransferSettingIdProperty);}
            set { SetValue(DataTransferSettingIdProperty,value); }
        }

        /// <summary> The OtherOptions property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OtherOptions
        {
            get { return  GetValue<System.String>( OtherOptionsProperty);}
            set { SetValue(OtherOptionsProperty,value); }
        }

        /// <summary> The CommandUioption property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CommandUioption
        {
            get { return  GetValue<System.String>( CommandUioptionProperty);}
            set { SetValue(CommandUioptionProperty,value); }
        }

        /// <summary> The CommandSearchViewId property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CommandSearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( CommandSearchViewIdProperty);}
            set { SetValue(CommandSearchViewIdProperty,value); }
        }

        /// <summary> The CommandConditionExpression property of the Entity AppProjectWorkFlowAction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CommandConditionExpression
        {
            get { return  GetValue<System.String>( CommandConditionExpressionProperty);}
            set { SetValue(CommandConditionExpressionProperty,value); }
        }
        
        #endregion

       
        
    }
}

