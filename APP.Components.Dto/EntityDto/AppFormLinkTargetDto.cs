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
    /// DTO class for the entity 'AppFormLinkTarget'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormLinkTargetDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string SourceColumn1Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.SourceColumn1);
        public static readonly string SourceColumn2Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.SourceColumn2);
        public static readonly string SourceColumn3Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.SourceColumn3);
        public static readonly string TargetColumn1Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.TargetColumn1);
        public static readonly string TargetColumn2Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.TargetColumn2);
        public static readonly string TargetColumn3Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.TargetColumn3);
        public static readonly string NavigationActionNameProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.NavigationActionName);
        public static readonly string ActionTypeProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.ActionType);
        public static readonly string IsReadonlyProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Boolean>>(o => o.IsReadonly);
        public static readonly string RowDisplayDbFieldProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.RowDisplayDbField);
        public static readonly string SourceConditionColumnProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.SourceConditionColumn);
        public static readonly string ConditionWarningMessageProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.ConditionWarningMessage);
        public static readonly string GroupNameProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.GroupName);
        public static readonly string LinkTargetTransactionIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.LinkTargetTransactionId);
        public static readonly string LinkTargetTransactionGroupIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.LinkTargetTransactionGroupId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string LinkTargetSearchIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.LinkTargetSearchId);
        public static readonly string LinkTargetUsageTypeProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.LinkTargetUsageType);
        public static readonly string SourceColumnTypeProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.SourceColumnType);
        public static readonly string LinkTargetUrlOrRouteCodeProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.LinkTargetUrlOrRouteCode);
        public static readonly string LayoutDisplayModeProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.LayoutDisplayMode);
        public static readonly string SourceViewColumnId1Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.SourceViewColumnId1);
        public static readonly string SourceViewColumnId2Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.SourceViewColumnId2);
        public static readonly string SourceViewColumnId3Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.SourceViewColumnId3);
        public static readonly string TargetSearchFieldId1Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.TargetSearchFieldId1);
        public static readonly string TargetSearchFieldId2Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.TargetSearchFieldId2);
        public static readonly string TargetSearchFieldId3Property = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.TargetSearchFieldId3);
        public static readonly string RowDisplayViewColumnIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.RowDisplayViewColumnId);
        public static readonly string SourceConditionViewColumnIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.SourceConditionViewColumnId);
        public static readonly string DataTransferSettingIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.DataTransferSettingId);
        public static readonly string ConditionTransFieldIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.ConditionTransFieldId);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string OpennedFormAutoExecuteCommandIdProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.OpennedFormAutoExecuteCommandId);
        public static readonly string IsPopupProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Boolean>>(o => o.IsPopup);
        public static readonly string PopupWidthProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.PopupWidth);
        public static readonly string PopupHeightProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,Nullable<System.Int32>>(o => o.PopupHeight);
        public static readonly string IconNameProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.IconName);
        public static readonly string OtherSettingsProperty = ObjectInfoHelper.GetName<AppFormLinkTargetDto ,System.String>(o => o.OtherSettings);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppFormLinkTargetDto()
        {        
        }
		
		static AppFormLinkTargetDto()
        {
                                                         
			  
			ForeignKeyProperties.Add(SearchViewIdProperty);  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);               
			ForeignKeyProperties.Add(LinkTargetTransactionIdProperty);        
			ForeignKeyProperties.Add(LinkTargetSearchIdProperty);      
			ForeignKeyProperties.Add(SourceViewColumnId1Property);  
			ForeignKeyProperties.Add(SourceViewColumnId2Property);  
			ForeignKeyProperties.Add(SourceViewColumnId3Property);  
			ForeignKeyProperties.Add(TargetSearchFieldId1Property);  
			ForeignKeyProperties.Add(TargetSearchFieldId2Property);  
			ForeignKeyProperties.Add(TargetSearchFieldId3Property);  
			ForeignKeyProperties.Add(RowDisplayViewColumnIdProperty);  
			ForeignKeyProperties.Add(SourceConditionViewColumnIdProperty);  
			ForeignKeyProperties.Add(DataTransferSettingIdProperty);         		
                
			DictStringPropertyMaxLength.Add(SourceColumn1Property,100); 
			DictStringPropertyMaxLength.Add(SourceColumn2Property,100); 
			DictStringPropertyMaxLength.Add(SourceColumn3Property,100); 
			DictStringPropertyMaxLength.Add(TargetColumn1Property,100); 
			DictStringPropertyMaxLength.Add(TargetColumn2Property,100); 
			DictStringPropertyMaxLength.Add(TargetColumn3Property,100); 
			DictStringPropertyMaxLength.Add(NavigationActionNameProperty,200);   
			DictStringPropertyMaxLength.Add(RowDisplayDbFieldProperty,100); 
			DictStringPropertyMaxLength.Add(SourceConditionColumnProperty,100); 
			DictStringPropertyMaxLength.Add(ConditionWarningMessageProperty,800); 
			DictStringPropertyMaxLength.Add(GroupNameProperty,100);           
			DictStringPropertyMaxLength.Add(LinkTargetUrlOrRouteCodeProperty,500);                 
			DictStringPropertyMaxLength.Add(IconNameProperty,500); 
			DictStringPropertyMaxLength.Add(OtherSettingsProperty,2147483647);  
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
    


        /// <summary> The SearchViewId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The TransactionUnitId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The SourceColumn1 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SourceColumn1
        {
            get { return  GetValue<System.String>( SourceColumn1Property);}
            set { SetValue(SourceColumn1Property,value); }
        }

        /// <summary> The SourceColumn2 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SourceColumn2
        {
            get { return  GetValue<System.String>( SourceColumn2Property);}
            set { SetValue(SourceColumn2Property,value); }
        }

        /// <summary> The SourceColumn3 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SourceColumn3
        {
            get { return  GetValue<System.String>( SourceColumn3Property);}
            set { SetValue(SourceColumn3Property,value); }
        }

        /// <summary> The TargetColumn1 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TargetColumn1
        {
            get { return  GetValue<System.String>( TargetColumn1Property);}
            set { SetValue(TargetColumn1Property,value); }
        }

        /// <summary> The TargetColumn2 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TargetColumn2
        {
            get { return  GetValue<System.String>( TargetColumn2Property);}
            set { SetValue(TargetColumn2Property,value); }
        }

        /// <summary> The TargetColumn3 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TargetColumn3
        {
            get { return  GetValue<System.String>( TargetColumn3Property);}
            set { SetValue(TargetColumn3Property,value); }
        }

        /// <summary> The NavigationActionName property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NavigationActionName
        {
            get { return  GetValue<System.String>( NavigationActionNameProperty);}
            set { SetValue(NavigationActionNameProperty,value); }
        }

        /// <summary> The ActionType property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ActionType
        {
            get { return  GetValue<Nullable<System.Int32>>( ActionTypeProperty);}
            set { SetValue(ActionTypeProperty,value); }
        }

        /// <summary> The IsReadonly property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsReadonly
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsReadonlyProperty);}
            set { SetValue(IsReadonlyProperty,value); }
        }

        /// <summary> The RowDisplayDbField property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RowDisplayDbField
        {
            get { return  GetValue<System.String>( RowDisplayDbFieldProperty);}
            set { SetValue(RowDisplayDbFieldProperty,value); }
        }

        /// <summary> The SourceConditionColumn property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SourceConditionColumn
        {
            get { return  GetValue<System.String>( SourceConditionColumnProperty);}
            set { SetValue(SourceConditionColumnProperty,value); }
        }

        /// <summary> The ConditionWarningMessage property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ConditionWarningMessage
        {
            get { return  GetValue<System.String>( ConditionWarningMessageProperty);}
            set { SetValue(ConditionWarningMessageProperty,value); }
        }

        /// <summary> The GroupName property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String GroupName
        {
            get { return  GetValue<System.String>( GroupNameProperty);}
            set { SetValue(GroupNameProperty,value); }
        }

        /// <summary> The LinkTargetTransactionId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkTargetTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkTargetTransactionIdProperty);}
            set { SetValue(LinkTargetTransactionIdProperty,value); }
        }

        /// <summary> The LinkTargetTransactionGroupId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkTargetTransactionGroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkTargetTransactionGroupIdProperty);}
            set { SetValue(LinkTargetTransactionGroupIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The LinkTargetSearchId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkTargetSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkTargetSearchIdProperty);}
            set { SetValue(LinkTargetSearchIdProperty,value); }
        }

        /// <summary> The LinkTargetUsageType property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkTargetUsageType
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkTargetUsageTypeProperty);}
            set { SetValue(LinkTargetUsageTypeProperty,value); }
        }

        /// <summary> The SourceColumnType property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceColumnType
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceColumnTypeProperty);}
            set { SetValue(SourceColumnTypeProperty,value); }
        }

        /// <summary> The LinkTargetUrlOrRouteCode property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LinkTargetUrlOrRouteCode
        {
            get { return  GetValue<System.String>( LinkTargetUrlOrRouteCodeProperty);}
            set { SetValue(LinkTargetUrlOrRouteCodeProperty,value); }
        }

        /// <summary> The LayoutDisplayMode property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LayoutDisplayMode
        {
            get { return  GetValue<Nullable<System.Int32>>( LayoutDisplayModeProperty);}
            set { SetValue(LayoutDisplayModeProperty,value); }
        }

        /// <summary> The SourceViewColumnId1 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceViewColumnId1
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceViewColumnId1Property);}
            set { SetValue(SourceViewColumnId1Property,value); }
        }

        /// <summary> The SourceViewColumnId2 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceViewColumnId2
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceViewColumnId2Property);}
            set { SetValue(SourceViewColumnId2Property,value); }
        }

        /// <summary> The SourceViewColumnId3 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceViewColumnId3
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceViewColumnId3Property);}
            set { SetValue(SourceViewColumnId3Property,value); }
        }

        /// <summary> The TargetSearchFieldId1 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetSearchFieldId1
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetSearchFieldId1Property);}
            set { SetValue(TargetSearchFieldId1Property,value); }
        }

        /// <summary> The TargetSearchFieldId2 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetSearchFieldId2
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetSearchFieldId2Property);}
            set { SetValue(TargetSearchFieldId2Property,value); }
        }

        /// <summary> The TargetSearchFieldId3 property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetSearchFieldId3
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetSearchFieldId3Property);}
            set { SetValue(TargetSearchFieldId3Property,value); }
        }

        /// <summary> The RowDisplayViewColumnId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowDisplayViewColumnId
        {
            get { return  GetValue<Nullable<System.Int32>>( RowDisplayViewColumnIdProperty);}
            set { SetValue(RowDisplayViewColumnIdProperty,value); }
        }

        /// <summary> The SourceConditionViewColumnId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceConditionViewColumnId
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceConditionViewColumnIdProperty);}
            set { SetValue(SourceConditionViewColumnIdProperty,value); }
        }

        /// <summary> The DataTransferSettingId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataTransferSettingId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTransferSettingIdProperty);}
            set { SetValue(DataTransferSettingIdProperty,value); }
        }

        /// <summary> The ConditionTransFieldId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ConditionTransFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( ConditionTransFieldIdProperty);}
            set { SetValue(ConditionTransFieldIdProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The OpennedFormAutoExecuteCommandId property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OpennedFormAutoExecuteCommandId
        {
            get { return  GetValue<Nullable<System.Int32>>( OpennedFormAutoExecuteCommandIdProperty);}
            set { SetValue(OpennedFormAutoExecuteCommandIdProperty,value); }
        }

        /// <summary> The IsPopup property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPopup
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPopupProperty);}
            set { SetValue(IsPopupProperty,value); }
        }

        /// <summary> The PopupWidth property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PopupWidth
        {
            get { return  GetValue<Nullable<System.Int32>>( PopupWidthProperty);}
            set { SetValue(PopupWidthProperty,value); }
        }

        /// <summary> The PopupHeight property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PopupHeight
        {
            get { return  GetValue<Nullable<System.Int32>>( PopupHeightProperty);}
            set { SetValue(PopupHeightProperty,value); }
        }

        /// <summary> The IconName property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IconName
        {
            get { return  GetValue<System.String>( IconNameProperty);}
            set { SetValue(IconNameProperty,value); }
        }

        /// <summary> The OtherSettings property of the Entity AppFormLinkTarget</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OtherSettings
        {
            get { return  GetValue<System.String>( OtherSettingsProperty);}
            set { SetValue(OtherSettingsProperty,value); }
        }
        
        #endregion

       
        
    }
}

