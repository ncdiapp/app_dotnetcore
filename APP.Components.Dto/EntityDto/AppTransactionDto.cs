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
    /// DTO class for the entity 'AppTransaction'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataSourceFromProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.DataSourceFrom);
        public static readonly string TransactionNameProperty = ObjectInfoHelper.GetName<AppTransactionDto ,System.String>(o => o.TransactionName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppTransactionDto ,System.String>(o => o.Description);
        public static readonly string NeedToCheckRowVersionProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.NeedToCheckRowVersion);
        public static readonly string TransactionOrganizedTypeProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.TransactionOrganizedType);
        public static readonly string FolderUsageTypeProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.FolderUsageType);
        public static readonly string PreSaveValidationMethodProperty = ObjectInfoHelper.GetName<AppTransactionDto ,System.String>(o => o.PreSaveValidationMethod);
        public static readonly string PostProcessStoreProcedureProperty = ObjectInfoHelper.GetName<AppTransactionDto ,System.String>(o => o.PostProcessStoreProcedure);
        public static readonly string ListFilterWhereClauseProperty = ObjectInfoHelper.GetName<AppTransactionDto ,System.String>(o => o.ListFilterWhereClause);
        public static readonly string IsReadOnlyProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsReadOnly);
        public static readonly string FormIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.FormId);
        public static readonly string BusinessScopeIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.BusinessScopeId);
        public static readonly string PrintFormIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.PrintFormId);
        public static readonly string IsEnableFolderSecurityProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsEnableFolderSecurity);
        public static readonly string IsSystemBuitInProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsSystemBuitIn);
        public static readonly string IsNeedToSetCriticalPathTrackFlowProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsNeedToSetCriticalPathTrackFlow);
        public static readonly string IsNeedToSetComunicationProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsNeedToSetComunication);
        public static readonly string ConversationBoxDockPositionProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.ConversationBoxDockPosition);
        public static readonly string FolderTransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.FolderTransactionId);
        public static readonly string EmAppTransBusinessTypeProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.EmAppTransBusinessType);
        public static readonly string LogicalDisplayEntityIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.LogicalDisplayEntityId);
        public static readonly string MgtRootFolderIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.MgtRootFolderId);
        public static readonly string TransactionFileStorageRootFolderIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.TransactionFileStorageRootFolderId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsExclusiveForOwnerProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsExclusiveForOwner);
        public static readonly string MasterWorkflowIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.MasterWorkflowId);
        public static readonly string MasterTransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.MasterTransactionId);
        public static readonly string EmGrandChildEditModeProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.EmGrandChildEditMode);
        public static readonly string IsPhysicalModelTableCreatedProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsPhysicalModelTableCreated);
        public static readonly string IsAllowSaveAsProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsAllowSaveAs);
        public static readonly string FormTitleDisplayFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.FormTitleDisplayFieldId);
        public static readonly string IsShowSaveButtonProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsShowSaveButton);
        public static readonly string IsShowCalculateButtonProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsShowCalculateButton);
        public static readonly string IsShowPrintButtonProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsShowPrintButton);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string IsForPublicAcesssProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Boolean>>(o => o.IsForPublicAcesss);
        public static readonly string EmNotificaionMethodProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.EmNotificaionMethod);
        public static readonly string NotificationSettingProperty = ObjectInfoHelper.GetName<AppTransactionDto ,System.String>(o => o.NotificationSetting);
        public static readonly string WebApiConfigIdProperty = ObjectInfoHelper.GetName<AppTransactionDto ,Nullable<System.Int32>>(o => o.WebApiConfigId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionDto()
        {        
        }
		
		static AppTransactionDto()
        {
                                                        
			  
			ForeignKeyProperties.Add(DataSourceFromProperty);           
			ForeignKeyProperties.Add(FormIdProperty);  
			ForeignKeyProperties.Add(BusinessScopeIdProperty);  
			ForeignKeyProperties.Add(PrintFormIdProperty);       
			ForeignKeyProperties.Add(FolderTransactionIdProperty);   
			ForeignKeyProperties.Add(LogicalDisplayEntityIdProperty);          
			ForeignKeyProperties.Add(MasterWorkflowIdProperty);  
			ForeignKeyProperties.Add(MasterTransactionIdProperty);         
			ForeignKeyProperties.Add(SaasApplicationIdProperty);     
			ForeignKeyProperties.Add(WebApiConfigIdProperty); 		
               
			DictStringPropertyMaxLength.Add(TransactionNameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);    
			DictStringPropertyMaxLength.Add(PreSaveValidationMethodProperty,500); 
			DictStringPropertyMaxLength.Add(PostProcessStoreProcedureProperty,2147483647); 
			DictStringPropertyMaxLength.Add(ListFilterWhereClauseProperty,500);                                 
			DictStringPropertyMaxLength.Add(NotificationSettingProperty,2000);   
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
    


        /// <summary> The DataSourceFrom property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceFrom
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceFromProperty);}
            set { SetValue(DataSourceFromProperty,value); }
        }

        /// <summary> The TransactionName property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionName
        {
            get { return  GetValue<System.String>( TransactionNameProperty);}
            set { SetValue(TransactionNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The NeedToCheckRowVersion property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> NeedToCheckRowVersion
        {
            get { return  GetValue<Nullable<System.Boolean>>( NeedToCheckRowVersionProperty);}
            set { SetValue(NeedToCheckRowVersionProperty,value); }
        }

        /// <summary> The TransactionOrganizedType property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionOrganizedType
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionOrganizedTypeProperty);}
            set { SetValue(TransactionOrganizedTypeProperty,value); }
        }

        /// <summary> The FolderUsageType property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FolderUsageType
        {
            get { return  GetValue<Nullable<System.Int32>>( FolderUsageTypeProperty);}
            set { SetValue(FolderUsageTypeProperty,value); }
        }

        /// <summary> The PreSaveValidationMethod property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PreSaveValidationMethod
        {
            get { return  GetValue<System.String>( PreSaveValidationMethodProperty);}
            set { SetValue(PreSaveValidationMethodProperty,value); }
        }

        /// <summary> The PostProcessStoreProcedure property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PostProcessStoreProcedure
        {
            get { return  GetValue<System.String>( PostProcessStoreProcedureProperty);}
            set { SetValue(PostProcessStoreProcedureProperty,value); }
        }

        /// <summary> The ListFilterWhereClause property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ListFilterWhereClause
        {
            get { return  GetValue<System.String>( ListFilterWhereClauseProperty);}
            set { SetValue(ListFilterWhereClauseProperty,value); }
        }

        /// <summary> The IsReadOnly property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsReadOnly
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsReadOnlyProperty);}
            set { SetValue(IsReadOnlyProperty,value); }
        }

        /// <summary> The FormId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormIdProperty);}
            set { SetValue(FormIdProperty,value); }
        }

        /// <summary> The BusinessScopeId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> BusinessScopeId
        {
            get { return  GetValue<Nullable<System.Int32>>( BusinessScopeIdProperty);}
            set { SetValue(BusinessScopeIdProperty,value); }
        }

        /// <summary> The PrintFormId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PrintFormId
        {
            get { return  GetValue<Nullable<System.Int32>>( PrintFormIdProperty);}
            set { SetValue(PrintFormIdProperty,value); }
        }

        /// <summary> The IsEnableFolderSecurity property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsEnableFolderSecurity
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsEnableFolderSecurityProperty);}
            set { SetValue(IsEnableFolderSecurityProperty,value); }
        }

        /// <summary> The IsSystemBuitIn property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSystemBuitIn
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSystemBuitInProperty);}
            set { SetValue(IsSystemBuitInProperty,value); }
        }

        /// <summary> The IsNeedToSetCriticalPathTrackFlow property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedToSetCriticalPathTrackFlow
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedToSetCriticalPathTrackFlowProperty);}
            set { SetValue(IsNeedToSetCriticalPathTrackFlowProperty,value); }
        }

        /// <summary> The IsNeedToSetComunication property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedToSetComunication
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedToSetComunicationProperty);}
            set { SetValue(IsNeedToSetComunicationProperty,value); }
        }

        /// <summary> The ConversationBoxDockPosition property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConversationBoxDockPosition
        {
            get { return  GetValue<Nullable<System.Int32>>( ConversationBoxDockPositionProperty);}
            set { SetValue(ConversationBoxDockPositionProperty,value); }
        }

        /// <summary> The FolderTransactionId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FolderTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( FolderTransactionIdProperty);}
            set { SetValue(FolderTransactionIdProperty,value); }
        }

        /// <summary> The EmAppTransBusinessType property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppTransBusinessType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppTransBusinessTypeProperty);}
            set { SetValue(EmAppTransBusinessTypeProperty,value); }
        }

        /// <summary> The LogicalDisplayEntityId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LogicalDisplayEntityId
        {
            get { return  GetValue<Nullable<System.Int32>>( LogicalDisplayEntityIdProperty);}
            set { SetValue(LogicalDisplayEntityIdProperty,value); }
        }

        /// <summary> The MgtRootFolderId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MgtRootFolderId
        {
            get { return  GetValue<Nullable<System.Int32>>( MgtRootFolderIdProperty);}
            set { SetValue(MgtRootFolderIdProperty,value); }
        }

        /// <summary> The TransactionFileStorageRootFolderId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFileStorageRootFolderId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFileStorageRootFolderIdProperty);}
            set { SetValue(TransactionFileStorageRootFolderIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsExclusiveForOwner property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsExclusiveForOwner
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsExclusiveForOwnerProperty);}
            set { SetValue(IsExclusiveForOwnerProperty,value); }
        }

        /// <summary> The MasterWorkflowId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MasterWorkflowId
        {
            get { return  GetValue<Nullable<System.Int32>>( MasterWorkflowIdProperty);}
            set { SetValue(MasterWorkflowIdProperty,value); }
        }

        /// <summary> The MasterTransactionId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MasterTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( MasterTransactionIdProperty);}
            set { SetValue(MasterTransactionIdProperty,value); }
        }

        /// <summary> The EmGrandChildEditMode property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmGrandChildEditMode
        {
            get { return  GetValue<Nullable<System.Int32>>( EmGrandChildEditModeProperty);}
            set { SetValue(EmGrandChildEditModeProperty,value); }
        }

        /// <summary> The IsPhysicalModelTableCreated property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPhysicalModelTableCreated
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPhysicalModelTableCreatedProperty);}
            set { SetValue(IsPhysicalModelTableCreatedProperty,value); }
        }

        /// <summary> The IsAllowSaveAs property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowSaveAs
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowSaveAsProperty);}
            set { SetValue(IsAllowSaveAsProperty,value); }
        }

        /// <summary> The FormTitleDisplayFieldId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormTitleDisplayFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormTitleDisplayFieldIdProperty);}
            set { SetValue(FormTitleDisplayFieldIdProperty,value); }
        }

        /// <summary> The IsShowSaveButton property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsShowSaveButton
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsShowSaveButtonProperty);}
            set { SetValue(IsShowSaveButtonProperty,value); }
        }

        /// <summary> The IsShowCalculateButton property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsShowCalculateButton
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsShowCalculateButtonProperty);}
            set { SetValue(IsShowCalculateButtonProperty,value); }
        }

        /// <summary> The IsShowPrintButton property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsShowPrintButton
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsShowPrintButtonProperty);}
            set { SetValue(IsShowPrintButtonProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The IsForPublicAcesss property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsForPublicAcesss
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsForPublicAcesssProperty);}
            set { SetValue(IsForPublicAcesssProperty,value); }
        }

        /// <summary> The EmNotificaionMethod property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmNotificaionMethod
        {
            get { return  GetValue<Nullable<System.Int32>>( EmNotificaionMethodProperty);}
            set { SetValue(EmNotificaionMethodProperty,value); }
        }

        /// <summary> The NotificationSetting property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NotificationSetting
        {
            get { return  GetValue<System.String>( NotificationSettingProperty);}
            set { SetValue(NotificationSettingProperty,value); }
        }

        /// <summary> The WebApiConfigId property of the Entity AppTransaction</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WebApiConfigId
        {
            get { return  GetValue<Nullable<System.Int32>>( WebApiConfigIdProperty);}
            set { SetValue(WebApiConfigIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

