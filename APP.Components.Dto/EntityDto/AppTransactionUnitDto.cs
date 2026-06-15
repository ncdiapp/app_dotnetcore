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
    /// DTO class for the entity 'AppTransactionUnit'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppTransactionUnitDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string UnitDisplayNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.UnitDisplayName);
        public static readonly string DataBaseTableNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.DataBaseTableName);
        public static readonly string SchemaOwnerProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.SchemaOwner);
        public static readonly string TransactionFlowProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.TransactionFlow);
        public static readonly string ParentTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.ParentTransactionUnitId);
        public static readonly string IsReadOnlyProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsReadOnly);
        public static readonly string IsMatrixUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsMatrixUnit);
        public static readonly string IsMatrixPivotUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsMatrixPivotUnit);
        public static readonly string IsSynchToDatabaseTableProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsSynchToDatabaseTable);
        public static readonly string IsMasterSiblingUnitProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsMasterSiblingUnit);
        public static readonly string IsPrimaryKeyIdentityInsertProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.Boolean>(o => o.IsPrimaryKeyIdentityInsert);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsExclusiveForOwnerProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsExclusiveForOwner);
        public static readonly string IsDisableAddButtonProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsDisableAddButton);
        public static readonly string IsDisableDeleteButtonProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsDisableDeleteButton);
        public static readonly string BaseDataBaseTableNameProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.BaseDataBaseTableName);
        public static readonly string TransactionUnitIentityGuidProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Guid>>(o => o.TransactionUnitIentityGuid);
        public static readonly string TreeViewKeyFieldProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.TreeViewKeyField);
        public static readonly string TreeViewParentKeyFieldProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.TreeViewParentKeyField);
        public static readonly string EmGridViewDisplayTypeProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.EmGridViewDisplayType);
        public static readonly string ImageHeightProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.ImageHeight);
        public static readonly string AvailableSourceUnitIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.AvailableSourceUnitId);
        public static readonly string AvailableSourceFilterByParentTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.AvailableSourceFilterByParentTransactionFieldId);
        public static readonly string AvailableSourceFilterWhereClauseProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.AvailableSourceFilterWhereClause);
        public static readonly string DataSourceQueryProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,System.String>(o => o.DataSourceQuery);
        public static readonly string MinRowCountProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.MinRowCount);
        public static readonly string MaxRowCountProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.MaxRowCount);
        public static readonly string IsUsedForLoadingAvailableSourceProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Boolean>>(o => o.IsUsedForLoadingAvailableSource);
        public static readonly string AvailableSourceMatchToParentUnitTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppTransactionUnitDto ,Nullable<System.Int32>>(o => o.AvailableSourceMatchToParentUnitTransactionFieldId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppTransactionUnitDto()
        {        
        }
		
		static AppTransactionUnitDto()
        {
                         
			MandatoryProperties.Add(IsPrimaryKeyIdentityInsertProperty);                       
			  
			ForeignKeyProperties.Add(TransactionIdProperty);      
			ForeignKeyProperties.Add(ParentTransactionUnitIdProperty);                             		
               
			DictStringPropertyMaxLength.Add(UnitDisplayNameProperty,200); 
			DictStringPropertyMaxLength.Add(DataBaseTableNameProperty,200); 
			DictStringPropertyMaxLength.Add(SchemaOwnerProperty,50);                 
			DictStringPropertyMaxLength.Add(BaseDataBaseTableNameProperty,200);  
			DictStringPropertyMaxLength.Add(TreeViewKeyFieldProperty,200); 
			DictStringPropertyMaxLength.Add(TreeViewParentKeyFieldProperty,200);     
			DictStringPropertyMaxLength.Add(AvailableSourceFilterWhereClauseProperty,1000); 
			DictStringPropertyMaxLength.Add(DataSourceQueryProperty,4000);      
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
    


        /// <summary> The TransactionId property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The UnitDisplayName property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UnitDisplayName
        {
            get { return  GetValue<System.String>( UnitDisplayNameProperty);}
            set { SetValue(UnitDisplayNameProperty,value); }
        }

        /// <summary> The DataBaseTableName property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataBaseTableName
        {
            get { return  GetValue<System.String>( DataBaseTableNameProperty);}
            set { SetValue(DataBaseTableNameProperty,value); }
        }

        /// <summary> The SchemaOwner property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SchemaOwner
        {
            get { return  GetValue<System.String>( SchemaOwnerProperty);}
            set { SetValue(SchemaOwnerProperty,value); }
        }

        /// <summary> The TransactionFlow property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFlow
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFlowProperty);}
            set { SetValue(TransactionFlowProperty,value); }
        }

        /// <summary> The ParentTransactionUnitId property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentTransactionUnitIdProperty);}
            set { SetValue(ParentTransactionUnitIdProperty,value); }
        }

        /// <summary> The IsReadOnly property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsReadOnly
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsReadOnlyProperty);}
            set { SetValue(IsReadOnlyProperty,value); }
        }

        /// <summary> The IsMatrixUnit property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMatrixUnit
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMatrixUnitProperty);}
            set { SetValue(IsMatrixUnitProperty,value); }
        }

        /// <summary> The IsMatrixPivotUnit property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMatrixPivotUnit
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMatrixPivotUnitProperty);}
            set { SetValue(IsMatrixPivotUnitProperty,value); }
        }

        /// <summary> The IsSynchToDatabaseTable property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSynchToDatabaseTable
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSynchToDatabaseTableProperty);}
            set { SetValue(IsSynchToDatabaseTableProperty,value); }
        }

        /// <summary> The IsMasterSiblingUnit property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMasterSiblingUnit
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMasterSiblingUnitProperty);}
            set { SetValue(IsMasterSiblingUnitProperty,value); }
        }

        /// <summary> The IsPrimaryKeyIdentityInsert property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsPrimaryKeyIdentityInsert
        {
            get { return  GetValue<System.Boolean>( IsPrimaryKeyIdentityInsertProperty);}
            set { SetValue(IsPrimaryKeyIdentityInsertProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsExclusiveForOwner property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsExclusiveForOwner
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsExclusiveForOwnerProperty);}
            set { SetValue(IsExclusiveForOwnerProperty,value); }
        }

        /// <summary> The IsDisableAddButton property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDisableAddButton
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDisableAddButtonProperty);}
            set { SetValue(IsDisableAddButtonProperty,value); }
        }

        /// <summary> The IsDisableDeleteButton property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDisableDeleteButton
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDisableDeleteButtonProperty);}
            set { SetValue(IsDisableDeleteButtonProperty,value); }
        }

        /// <summary> The BaseDataBaseTableName property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BaseDataBaseTableName
        {
            get { return  GetValue<System.String>( BaseDataBaseTableNameProperty);}
            set { SetValue(BaseDataBaseTableNameProperty,value); }
        }

        /// <summary> The TransactionUnitIentityGuid property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> TransactionUnitIentityGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( TransactionUnitIentityGuidProperty);}
            set { SetValue(TransactionUnitIentityGuidProperty,value); }
        }

        /// <summary> The TreeViewKeyField property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TreeViewKeyField
        {
            get { return  GetValue<System.String>( TreeViewKeyFieldProperty);}
            set { SetValue(TreeViewKeyFieldProperty,value); }
        }

        /// <summary> The TreeViewParentKeyField property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TreeViewParentKeyField
        {
            get { return  GetValue<System.String>( TreeViewParentKeyFieldProperty);}
            set { SetValue(TreeViewParentKeyFieldProperty,value); }
        }

        /// <summary> The EmGridViewDisplayType property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmGridViewDisplayType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmGridViewDisplayTypeProperty);}
            set { SetValue(EmGridViewDisplayTypeProperty,value); }
        }

        /// <summary> The ImageHeight property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ImageHeight
        {
            get { return  GetValue<Nullable<System.Int32>>( ImageHeightProperty);}
            set { SetValue(ImageHeightProperty,value); }
        }

        /// <summary> The AvailableSourceUnitId property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AvailableSourceUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( AvailableSourceUnitIdProperty);}
            set { SetValue(AvailableSourceUnitIdProperty,value); }
        }

        /// <summary> The AvailableSourceFilterByParentTransactionFieldId property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AvailableSourceFilterByParentTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( AvailableSourceFilterByParentTransactionFieldIdProperty);}
            set { SetValue(AvailableSourceFilterByParentTransactionFieldIdProperty,value); }
        }

        /// <summary> The AvailableSourceFilterWhereClause property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AvailableSourceFilterWhereClause
        {
            get { return  GetValue<System.String>( AvailableSourceFilterWhereClauseProperty);}
            set { SetValue(AvailableSourceFilterWhereClauseProperty,value); }
        }

        /// <summary> The DataSourceQuery property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataSourceQuery
        {
            get { return  GetValue<System.String>( DataSourceQueryProperty);}
            set { SetValue(DataSourceQueryProperty,value); }
        }

        /// <summary> The MinRowCount property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MinRowCount
        {
            get { return  GetValue<Nullable<System.Int32>>( MinRowCountProperty);}
            set { SetValue(MinRowCountProperty,value); }
        }

        /// <summary> The MaxRowCount property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MaxRowCount
        {
            get { return  GetValue<Nullable<System.Int32>>( MaxRowCountProperty);}
            set { SetValue(MaxRowCountProperty,value); }
        }

        /// <summary> The IsUsedForLoadingAvailableSource property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUsedForLoadingAvailableSource
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUsedForLoadingAvailableSourceProperty);}
            set { SetValue(IsUsedForLoadingAvailableSourceProperty,value); }
        }

        /// <summary> The AvailableSourceMatchToParentUnitTransactionFieldId property of the Entity AppTransactionUnit</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AvailableSourceMatchToParentUnitTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( AvailableSourceMatchToParentUnitTransactionFieldIdProperty);}
            set { SetValue(AvailableSourceMatchToParentUnitTransactionFieldIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

