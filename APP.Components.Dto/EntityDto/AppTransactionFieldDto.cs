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
    /// DTO class for the entity 'AppTransactionField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionFieldDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.Int32>(o => o.TransactionUnitId);
        public static readonly string DisplayNameProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.DisplayName);
        public static readonly string DataBaseFieldNameProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.DataBaseFieldName);
        public static readonly string ControlTypeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.Int32>(o => o.ControlType);
        public static readonly string DataTypeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.DataType);
        public static readonly string EntityIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.EntityId);
        public static readonly string InternalCodeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.InternalCode);
        public static readonly string NeedValidatorProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.NeedValidator);
        public static readonly string ValidatorTypeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.ValidatorType);
        public static readonly string NbdecimalProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.Nbdecimal);
        public static readonly string SortOrderProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.SortOrder);
        public static readonly string MaxCharLegnthProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.MaxCharLegnth);
        public static readonly string MaxNumberProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Decimal>>(o => o.MaxNumber);
        public static readonly string DdlparentLevelIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.DdlparentLevelId);
        public static readonly string AutoIncrementSeedProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.AutoIncrementSeed);
        public static readonly string AutoIncrementPrefixProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.AutoIncrementPrefix);
        public static readonly string AutoIncrementLastIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.AutoIncrementLastId);
        public static readonly string IsNeedLogProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsNeedLog);
        public static readonly string IsAllowEmptyProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsAllowEmpty);
        public static readonly string ToolTipProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.ToolTip);
        public static readonly string IsConvertToUpperCaseProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsConvertToUpperCase);
        public static readonly string DefaultValueProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.DefaultValue);
        public static readonly string CascadingRelationTableSchemaOwnerProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.CascadingRelationTableSchemaOwner);
        public static readonly string CascadingRelationTableProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.CascadingRelationTable);
        public static readonly string CascadingRelationTableParentKeyFieldProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.CascadingRelationTableParentKeyField);
        public static readonly string CascadingRelationTableChildKeyFieldProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.CascadingRelationTableChildKeyField);
        public static readonly string MasterEntityFieldlIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.MasterEntityFieldlId);
        public static readonly string InnerEntitySubscribeFiledProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.InnerEntitySubscribeFiled);
        public static readonly string DisplayWidthProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.DisplayWidth);
        public static readonly string IsReadonlyProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsReadonly);
        public static readonly string ChildUnitSubscribeParentFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.ChildUnitSubscribeParentFieldId);
        public static readonly string ParentUnitSubscribeChildAggFunctionIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.ParentUnitSubscribeChildAggFunctionId);
        public static readonly string IsGridUseAvailableEntitySourceProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsGridUseAvailableEntitySource);
        public static readonly string IsUniqueProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsUnique);
        public static readonly string DataRetrieveTypeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.DataRetrieveType);
        public static readonly string AppExternalSourceFromProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.AppExternalSourceFrom);
        public static readonly string IsGroupByProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsGroupBy);
        public static readonly string GroupByLevelProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.GroupByLevel);
        public static readonly string MatrixKeyTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.MatrixKeyTransactionFieldId);
        public static readonly string IsPrimaryKeyProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.Boolean>(o => o.IsPrimaryKey);
        public static readonly string IsLinkToParentPrimaryKeyProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.Boolean>(o => o.IsLinkToParentPrimaryKey);
        public static readonly string LinkToParentPrimaryKeyFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.LinkToParentPrimaryKeyFieldId);
        public static readonly string IsVisibleProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsVisible);
        public static readonly string IsFilterByCurrentUserProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsFilterByCurrentUser);
        public static readonly string SystemVariableEnumCodeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.SystemVariableEnumCode);
        public static readonly string MatrixForeignKeyFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.MatrixForeignKeyFieldId);
        public static readonly string IsPivotRowProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsPivotRow);
        public static readonly string IsPivotColumnProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsPivotColumn);
        public static readonly string DdlQueryTextProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.DdlQueryText);
        public static readonly string WhereClauseExpressProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.WhereClauseExpress);
        public static readonly string DdlForeignUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.DdlForeignUnitId);
        public static readonly string DdlForeignUnitDisplayDbFiedsProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.DdlForeignUnitDisplayDbFieds);
        public static readonly string FileControlTypeFolderTransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.FileControlTypeFolderTransactionId);
        public static readonly string RowIdentityGuidProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Guid>>(o => o.RowIdentityGuid);
        public static readonly string MappingEmSystemTokenFieldProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.MappingEmSystemTokenField);
        public static readonly string IsLogicalDisplayProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsLogicalDisplay);
        public static readonly string IsChangeTrigerNotificationProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsChangeTrigerNotification);
        public static readonly string SiblingUnitLogicalKeyFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.SiblingUnitLogicalKeyFieldId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsFieldExclusiveForOwnerProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsFieldExclusiveForOwner);
        public static readonly string IsAllowEditOnMobileRowPopupProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsAllowEditOnMobileRowPopup);
        public static readonly string EmInternalCodeRegistrationProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.EmInternalCodeRegistration);
        public static readonly string HostFormLayoutItemIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.HostFormLayoutItemId);
        public static readonly string IsPivotValueProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsPivotValue);
        public static readonly string PivotAggregationTypeProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.PivotAggregationType);
        public static readonly string ControlTypeParam1Property = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.ControlTypeParam1);
        public static readonly string ControlTypeParam2Property = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.ControlTypeParam2);
        public static readonly string ControlTypeParam3Property = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.ControlTypeParam3);
        public static readonly string IsPrintVisibleProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsPrintVisible);
        public static readonly string OnChangeTriggerToCommandIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.OnChangeTriggerToCommandId);
        public static readonly string IsTempVariableProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsTempVariable);
        public static readonly string MappingToAvailableSourceUnitTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Int32>>(o => o.MappingToAvailableSourceUnitTransactionFieldId);
        public static readonly string IsStoreToExtendTableProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,Nullable<System.Boolean>>(o => o.IsStoreToExtendTable);
        public static readonly string InnerEntityLabelSubscribeFiledProperty = ObjectInfoHelper.GetName<AppTransactionFieldDto ,System.String>(o => o.InnerEntityLabelSubscribeFiled);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionFieldDto()
        {        
        }
		
		static AppTransactionFieldDto()
        {
              
			MandatoryProperties.Add(TransactionUnitIdProperty);  
			MandatoryProperties.Add(DisplayNameProperty);  
			MandatoryProperties.Add(DataBaseFieldNameProperty);  
			MandatoryProperties.Add(ControlTypeProperty);                                     
			MandatoryProperties.Add(IsPrimaryKeyProperty);  
			MandatoryProperties.Add(IsLinkToParentPrimaryKeyProperty);                                      
			  
			ForeignKeyProperties.Add(TransactionUnitIdProperty);      
			ForeignKeyProperties.Add(EntityIdProperty);         
			ForeignKeyProperties.Add(DdlparentLevelIdProperty);              
			ForeignKeyProperties.Add(MasterEntityFieldlIdProperty);     
			ForeignKeyProperties.Add(ChildUnitSubscribeParentFieldIdProperty);  
			ForeignKeyProperties.Add(ParentUnitSubscribeChildAggFunctionIdProperty);        
			ForeignKeyProperties.Add(MatrixKeyTransactionFieldIdProperty);    
			ForeignKeyProperties.Add(LinkToParentPrimaryKeyFieldIdProperty);     
			ForeignKeyProperties.Add(MatrixForeignKeyFieldIdProperty);      
			ForeignKeyProperties.Add(DdlForeignUnitIdProperty);   
			ForeignKeyProperties.Add(FileControlTypeFolderTransactionIdProperty);      
			ForeignKeyProperties.Add(SiblingUnitLogicalKeyFieldIdProperty);          
			ForeignKeyProperties.Add(HostFormLayoutItemIdProperty);        
			ForeignKeyProperties.Add(OnChangeTriggerToCommandIdProperty);     		
               
			DictStringPropertyMaxLength.Add(DisplayNameProperty,500); 
			DictStringPropertyMaxLength.Add(DataBaseFieldNameProperty,500);    
			DictStringPropertyMaxLength.Add(InternalCodeProperty,50);         
			DictStringPropertyMaxLength.Add(AutoIncrementPrefixProperty,200);    
			DictStringPropertyMaxLength.Add(ToolTipProperty,500);  
			DictStringPropertyMaxLength.Add(DefaultValueProperty,500); 
			DictStringPropertyMaxLength.Add(CascadingRelationTableSchemaOwnerProperty,500); 
			DictStringPropertyMaxLength.Add(CascadingRelationTableProperty,500); 
			DictStringPropertyMaxLength.Add(CascadingRelationTableParentKeyFieldProperty,500); 
			DictStringPropertyMaxLength.Add(CascadingRelationTableChildKeyFieldProperty,500);  
			DictStringPropertyMaxLength.Add(InnerEntitySubscribeFiledProperty,100); 
			DictStringPropertyMaxLength.Add(DisplayWidthProperty,50);                
			DictStringPropertyMaxLength.Add(SystemVariableEnumCodeProperty,100);    
			DictStringPropertyMaxLength.Add(DdlQueryTextProperty,4000); 
			DictStringPropertyMaxLength.Add(WhereClauseExpressProperty,1000);  
			DictStringPropertyMaxLength.Add(DdlForeignUnitDisplayDbFiedsProperty,1000);                  
			DictStringPropertyMaxLength.Add(ControlTypeParam1Property,500); 
			DictStringPropertyMaxLength.Add(ControlTypeParam2Property,500); 
			DictStringPropertyMaxLength.Add(ControlTypeParam3Property,500);      
			DictStringPropertyMaxLength.Add(InnerEntityLabelSubscribeFiledProperty,100);  
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
    


        /// <summary> The TransactionUnitId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 TransactionUnitId
        {
            get { return  GetValue<System.Int32>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The DisplayName property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayName
        {
            get { return  GetValue<System.String>( DisplayNameProperty);}
            set { SetValue(DisplayNameProperty,value); }
        }

        /// <summary> The DataBaseFieldName property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataBaseFieldName
        {
            get { return  GetValue<System.String>( DataBaseFieldNameProperty);}
            set { SetValue(DataBaseFieldNameProperty,value); }
        }

        /// <summary> The ControlType property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ControlType
        {
            get { return  GetValue<System.Int32>( ControlTypeProperty);}
            set { SetValue(ControlTypeProperty,value); }
        }

        /// <summary> The DataType property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTypeProperty);}
            set { SetValue(DataTypeProperty,value); }
        }

        /// <summary> The EntityId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EntityId
        {
            get { return  GetValue<Nullable<System.Int32>>( EntityIdProperty);}
            set { SetValue(EntityIdProperty,value); }
        }

        /// <summary> The InternalCode property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InternalCode
        {
            get { return  GetValue<System.String>( InternalCodeProperty);}
            set { SetValue(InternalCodeProperty,value); }
        }

        /// <summary> The NeedValidator property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> NeedValidator
        {
            get { return  GetValue<Nullable<System.Boolean>>( NeedValidatorProperty);}
            set { SetValue(NeedValidatorProperty,value); }
        }

        /// <summary> The ValidatorType property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ValidatorType
        {
            get { return  GetValue<Nullable<System.Int32>>( ValidatorTypeProperty);}
            set { SetValue(ValidatorTypeProperty,value); }
        }

        /// <summary> The Nbdecimal property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Nbdecimal
        {
            get { return  GetValue<Nullable<System.Int32>>( NbdecimalProperty);}
            set { SetValue(NbdecimalProperty,value); }
        }

        /// <summary> The SortOrder property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SortOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( SortOrderProperty);}
            set { SetValue(SortOrderProperty,value); }
        }

        /// <summary> The MaxCharLegnth property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MaxCharLegnth
        {
            get { return  GetValue<Nullable<System.Int32>>( MaxCharLegnthProperty);}
            set { SetValue(MaxCharLegnthProperty,value); }
        }

        /// <summary> The MaxNumber property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Decimal> MaxNumber
        {
            get { return  GetValue<Nullable<System.Decimal>>( MaxNumberProperty);}
            set { SetValue(MaxNumberProperty,value); }
        }

        /// <summary> The DdlparentLevelId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DdlparentLevelId
        {
            get { return  GetValue<Nullable<System.Int32>>( DdlparentLevelIdProperty);}
            set { SetValue(DdlparentLevelIdProperty,value); }
        }

        /// <summary> The AutoIncrementSeed property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AutoIncrementSeed
        {
            get { return  GetValue<Nullable<System.Int32>>( AutoIncrementSeedProperty);}
            set { SetValue(AutoIncrementSeedProperty,value); }
        }

        /// <summary> The AutoIncrementPrefix property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AutoIncrementPrefix
        {
            get { return  GetValue<System.String>( AutoIncrementPrefixProperty);}
            set { SetValue(AutoIncrementPrefixProperty,value); }
        }

        /// <summary> The AutoIncrementLastId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AutoIncrementLastId
        {
            get { return  GetValue<Nullable<System.Int32>>( AutoIncrementLastIdProperty);}
            set { SetValue(AutoIncrementLastIdProperty,value); }
        }

        /// <summary> The IsNeedLog property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedLog
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedLogProperty);}
            set { SetValue(IsNeedLogProperty,value); }
        }

        /// <summary> The IsAllowEmpty property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowEmpty
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowEmptyProperty);}
            set { SetValue(IsAllowEmptyProperty,value); }
        }

        /// <summary> The ToolTip property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ToolTip
        {
            get { return  GetValue<System.String>( ToolTipProperty);}
            set { SetValue(ToolTipProperty,value); }
        }

        /// <summary> The IsConvertToUpperCase property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsConvertToUpperCase
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsConvertToUpperCaseProperty);}
            set { SetValue(IsConvertToUpperCaseProperty,value); }
        }

        /// <summary> The DefaultValue property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefaultValue
        {
            get { return  GetValue<System.String>( DefaultValueProperty);}
            set { SetValue(DefaultValueProperty,value); }
        }

        /// <summary> The CascadingRelationTableSchemaOwner property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTableSchemaOwner
        {
            get { return  GetValue<System.String>( CascadingRelationTableSchemaOwnerProperty);}
            set { SetValue(CascadingRelationTableSchemaOwnerProperty,value); }
        }

        /// <summary> The CascadingRelationTable property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTable
        {
            get { return  GetValue<System.String>( CascadingRelationTableProperty);}
            set { SetValue(CascadingRelationTableProperty,value); }
        }

        /// <summary> The CascadingRelationTableParentKeyField property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTableParentKeyField
        {
            get { return  GetValue<System.String>( CascadingRelationTableParentKeyFieldProperty);}
            set { SetValue(CascadingRelationTableParentKeyFieldProperty,value); }
        }

        /// <summary> The CascadingRelationTableChildKeyField property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTableChildKeyField
        {
            get { return  GetValue<System.String>( CascadingRelationTableChildKeyFieldProperty);}
            set { SetValue(CascadingRelationTableChildKeyFieldProperty,value); }
        }

        /// <summary> The MasterEntityFieldlId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MasterEntityFieldlId
        {
            get { return  GetValue<Nullable<System.Int32>>( MasterEntityFieldlIdProperty);}
            set { SetValue(MasterEntityFieldlIdProperty,value); }
        }

        /// <summary> The InnerEntitySubscribeFiled property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InnerEntitySubscribeFiled
        {
            get { return  GetValue<System.String>( InnerEntitySubscribeFiledProperty);}
            set { SetValue(InnerEntitySubscribeFiledProperty,value); }
        }

        /// <summary> The DisplayWidth property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayWidth
        {
            get { return  GetValue<System.String>( DisplayWidthProperty);}
            set { SetValue(DisplayWidthProperty,value); }
        }

        /// <summary> The IsReadonly property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsReadonly
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsReadonlyProperty);}
            set { SetValue(IsReadonlyProperty,value); }
        }

        /// <summary> The ChildUnitSubscribeParentFieldId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChildUnitSubscribeParentFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( ChildUnitSubscribeParentFieldIdProperty);}
            set { SetValue(ChildUnitSubscribeParentFieldIdProperty,value); }
        }

        /// <summary> The ParentUnitSubscribeChildAggFunctionId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentUnitSubscribeChildAggFunctionId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentUnitSubscribeChildAggFunctionIdProperty);}
            set { SetValue(ParentUnitSubscribeChildAggFunctionIdProperty,value); }
        }

        /// <summary> The IsGridUseAvailableEntitySource property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsGridUseAvailableEntitySource
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsGridUseAvailableEntitySourceProperty);}
            set { SetValue(IsGridUseAvailableEntitySourceProperty,value); }
        }

        /// <summary> The IsUnique property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUnique
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUniqueProperty);}
            set { SetValue(IsUniqueProperty,value); }
        }

        /// <summary> The DataRetrieveType property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataRetrieveType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataRetrieveTypeProperty);}
            set { SetValue(DataRetrieveTypeProperty,value); }
        }

        /// <summary> The AppExternalSourceFrom property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppExternalSourceFrom
        {
            get { return  GetValue<Nullable<System.Int32>>( AppExternalSourceFromProperty);}
            set { SetValue(AppExternalSourceFromProperty,value); }
        }

        /// <summary> The IsGroupBy property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsGroupBy
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsGroupByProperty);}
            set { SetValue(IsGroupByProperty,value); }
        }

        /// <summary> The GroupByLevel property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GroupByLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupByLevelProperty);}
            set { SetValue(GroupByLevelProperty,value); }
        }

        /// <summary> The MatrixKeyTransactionFieldId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MatrixKeyTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( MatrixKeyTransactionFieldIdProperty);}
            set { SetValue(MatrixKeyTransactionFieldIdProperty,value); }
        }

        /// <summary> The IsPrimaryKey property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsPrimaryKey
        {
            get { return  GetValue<System.Boolean>( IsPrimaryKeyProperty);}
            set { SetValue(IsPrimaryKeyProperty,value); }
        }

        /// <summary> The IsLinkToParentPrimaryKey property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsLinkToParentPrimaryKey
        {
            get { return  GetValue<System.Boolean>( IsLinkToParentPrimaryKeyProperty);}
            set { SetValue(IsLinkToParentPrimaryKeyProperty,value); }
        }

        /// <summary> The LinkToParentPrimaryKeyFieldId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkToParentPrimaryKeyFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkToParentPrimaryKeyFieldIdProperty);}
            set { SetValue(LinkToParentPrimaryKeyFieldIdProperty,value); }
        }

        /// <summary> The IsVisible property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsVisible
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsVisibleProperty);}
            set { SetValue(IsVisibleProperty,value); }
        }

        /// <summary> The IsFilterByCurrentUser property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFilterByCurrentUser
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFilterByCurrentUserProperty);}
            set { SetValue(IsFilterByCurrentUserProperty,value); }
        }

        /// <summary> The SystemVariableEnumCode property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SystemVariableEnumCode
        {
            get { return  GetValue<System.String>( SystemVariableEnumCodeProperty);}
            set { SetValue(SystemVariableEnumCodeProperty,value); }
        }

        /// <summary> The MatrixForeignKeyFieldId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MatrixForeignKeyFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( MatrixForeignKeyFieldIdProperty);}
            set { SetValue(MatrixForeignKeyFieldIdProperty,value); }
        }

        /// <summary> The IsPivotRow property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPivotRow
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPivotRowProperty);}
            set { SetValue(IsPivotRowProperty,value); }
        }

        /// <summary> The IsPivotColumn property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPivotColumn
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPivotColumnProperty);}
            set { SetValue(IsPivotColumnProperty,value); }
        }

        /// <summary> The DdlQueryText property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DdlQueryText
        {
            get { return  GetValue<System.String>( DdlQueryTextProperty);}
            set { SetValue(DdlQueryTextProperty,value); }
        }

        /// <summary> The WhereClauseExpress property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WhereClauseExpress
        {
            get { return  GetValue<System.String>( WhereClauseExpressProperty);}
            set { SetValue(WhereClauseExpressProperty,value); }
        }

        /// <summary> The DdlForeignUnitId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DdlForeignUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( DdlForeignUnitIdProperty);}
            set { SetValue(DdlForeignUnitIdProperty,value); }
        }

        /// <summary> The DdlForeignUnitDisplayDbFieds property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DdlForeignUnitDisplayDbFieds
        {
            get { return  GetValue<System.String>( DdlForeignUnitDisplayDbFiedsProperty);}
            set { SetValue(DdlForeignUnitDisplayDbFiedsProperty,value); }
        }

        /// <summary> The FileControlTypeFolderTransactionId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FileControlTypeFolderTransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( FileControlTypeFolderTransactionIdProperty);}
            set { SetValue(FileControlTypeFolderTransactionIdProperty,value); }
        }

        /// <summary> The RowIdentityGuid property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> RowIdentityGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( RowIdentityGuidProperty);}
            set { SetValue(RowIdentityGuidProperty,value); }
        }

        /// <summary> The MappingEmSystemTokenField property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MappingEmSystemTokenField
        {
            get { return  GetValue<Nullable<System.Int32>>( MappingEmSystemTokenFieldProperty);}
            set { SetValue(MappingEmSystemTokenFieldProperty,value); }
        }

        /// <summary> The IsLogicalDisplay property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsLogicalDisplay
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsLogicalDisplayProperty);}
            set { SetValue(IsLogicalDisplayProperty,value); }
        }

        /// <summary> The IsChangeTrigerNotification property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsChangeTrigerNotification
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsChangeTrigerNotificationProperty);}
            set { SetValue(IsChangeTrigerNotificationProperty,value); }
        }

        /// <summary> The SiblingUnitLogicalKeyFieldId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SiblingUnitLogicalKeyFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SiblingUnitLogicalKeyFieldIdProperty);}
            set { SetValue(SiblingUnitLogicalKeyFieldIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsFieldExclusiveForOwner property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFieldExclusiveForOwner
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFieldExclusiveForOwnerProperty);}
            set { SetValue(IsFieldExclusiveForOwnerProperty,value); }
        }

        /// <summary> The IsAllowEditOnMobileRowPopup property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowEditOnMobileRowPopup
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowEditOnMobileRowPopupProperty);}
            set { SetValue(IsAllowEditOnMobileRowPopupProperty,value); }
        }

        /// <summary> The EmInternalCodeRegistration property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmInternalCodeRegistration
        {
            get { return  GetValue<Nullable<System.Int32>>( EmInternalCodeRegistrationProperty);}
            set { SetValue(EmInternalCodeRegistrationProperty,value); }
        }

        /// <summary> The HostFormLayoutItemId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> HostFormLayoutItemId
        {
            get { return  GetValue<Nullable<System.Int32>>( HostFormLayoutItemIdProperty);}
            set { SetValue(HostFormLayoutItemIdProperty,value); }
        }

        /// <summary> The IsPivotValue property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPivotValue
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPivotValueProperty);}
            set { SetValue(IsPivotValueProperty,value); }
        }

        /// <summary> The PivotAggregationType property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PivotAggregationType
        {
            get { return  GetValue<Nullable<System.Int32>>( PivotAggregationTypeProperty);}
            set { SetValue(PivotAggregationTypeProperty,value); }
        }

        /// <summary> The ControlTypeParam1 property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ControlTypeParam1
        {
            get { return  GetValue<System.String>( ControlTypeParam1Property);}
            set { SetValue(ControlTypeParam1Property,value); }
        }

        /// <summary> The ControlTypeParam2 property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ControlTypeParam2
        {
            get { return  GetValue<System.String>( ControlTypeParam2Property);}
            set { SetValue(ControlTypeParam2Property,value); }
        }

        /// <summary> The ControlTypeParam3 property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ControlTypeParam3
        {
            get { return  GetValue<System.String>( ControlTypeParam3Property);}
            set { SetValue(ControlTypeParam3Property,value); }
        }

        /// <summary> The IsPrintVisible property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPrintVisible
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPrintVisibleProperty);}
            set { SetValue(IsPrintVisibleProperty,value); }
        }

        /// <summary> The OnChangeTriggerToCommandId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OnChangeTriggerToCommandId
        {
            get { return  GetValue<Nullable<System.Int32>>( OnChangeTriggerToCommandIdProperty);}
            set { SetValue(OnChangeTriggerToCommandIdProperty,value); }
        }

        /// <summary> The IsTempVariable property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsTempVariable
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsTempVariableProperty);}
            set { SetValue(IsTempVariableProperty,value); }
        }

        /// <summary> The MappingToAvailableSourceUnitTransactionFieldId property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MappingToAvailableSourceUnitTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( MappingToAvailableSourceUnitTransactionFieldIdProperty);}
            set { SetValue(MappingToAvailableSourceUnitTransactionFieldIdProperty,value); }
        }

        /// <summary> The IsStoreToExtendTable property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsStoreToExtendTable
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsStoreToExtendTableProperty);}
            set { SetValue(IsStoreToExtendTableProperty,value); }
        }

        /// <summary> The InnerEntityLabelSubscribeFiled property of the Entity AppTransactionField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InnerEntityLabelSubscribeFiled
        {
            get { return  GetValue<System.String>( InnerEntityLabelSubscribeFiledProperty);}
            set { SetValue(InnerEntityLabelSubscribeFiledProperty,value); }
        }
        
        #endregion

       
        
    }
}

