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
    /// DTO class for the entity 'AppSearchField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchFieldDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.SearchId);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string PositionRowProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.PositionRow);
        public static readonly string PositionColumnProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.PositionColumn);
        public static readonly string OperationIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.OperationId);
        public static readonly string DisplayTextProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.DisplayText);
        public static readonly string IsVisibleProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.Boolean>(o => o.IsVisible);
        public static readonly string DefaultValueProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.DefaultValue);
        public static readonly string IsReadOnlyProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.Boolean>(o => o.IsReadOnly);
        public static readonly string IsAutoPopulateProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.Boolean>(o => o.IsAutoPopulate);
        public static readonly string ParentFieldIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.ParentFieldId);
        public static readonly string IsLoadOnDemandProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Boolean>>(o => o.IsLoadOnDemand);
        public static readonly string SysTableFiledPathProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.SysTableFiledPath);
        public static readonly string SysTableFiledFullPathProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.SysTableFiledFullPath);
        public static readonly string ControlTypeProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.ControlType);
        public static readonly string EntityIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.EntityId);
        public static readonly string DataTypeProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.DataType);
        public static readonly string IsFilterByCurrentUserProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Boolean>>(o => o.IsFilterByCurrentUser);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string IsChangedAutoExecuteProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Boolean>>(o => o.IsChangedAutoExecute);
        public static readonly string StartValueEntityFieldProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.StartValueEntityField);
        public static readonly string EndValueEntityFieldProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.EndValueEntityField);
        public static readonly string StartValueDataSetFieldProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.StartValueDataSetField);
        public static readonly string EndValueDataSetFieldProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.EndValueDataSetField);
        public static readonly string SubControlTypeProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.SubControlType);
        public static readonly string CascadingRelationTableProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.CascadingRelationTable);
        public static readonly string CascadingRelationTableParentKeyFieldProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.CascadingRelationTableParentKeyField);
        public static readonly string CascadingRelationTableChildKeyFieldProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.CascadingRelationTableChildKeyField);
        public static readonly string IsAllowMultipleSelectProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Boolean>>(o => o.IsAllowMultipleSelect);
        public static readonly string MasterEntityFieldlIdProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.MasterEntityFieldlId);
        public static readonly string InnerEntitySubscribeFiledProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.InnerEntitySubscribeFiled);
        public static readonly string IsSkipSearchProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Boolean>>(o => o.IsSkipSearch);
        public static readonly string DataRetrieveTypeProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.DataRetrieveType);
        public static readonly string CascadingRelationTableSchemaOwnerProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.CascadingRelationTableSchemaOwner);
        public static readonly string AppExternalSourceFromProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.AppExternalSourceFrom);
        public static readonly string DdlQueryTextProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.DdlQueryText);
        public static readonly string WhereClauseExpressProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,System.String>(o => o.WhereClauseExpress);
        public static readonly string EmInternalCodeRegistrationProperty = ObjectInfoHelper.GetName<AppSearchFieldDto ,Nullable<System.Int32>>(o => o.EmInternalCodeRegistration);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSearchFieldDto()
        {        
        }
		
		static AppSearchFieldDto()
        {
                    
			MandatoryProperties.Add(IsVisibleProperty);   
			MandatoryProperties.Add(IsReadOnlyProperty);  
			MandatoryProperties.Add(IsAutoPopulateProperty);                                 
			  
			ForeignKeyProperties.Add(SearchIdProperty);                
			ForeignKeyProperties.Add(EntityIdProperty);                           		
                   
			DictStringPropertyMaxLength.Add(DisplayTextProperty,250);  
			DictStringPropertyMaxLength.Add(DefaultValueProperty,1000);     
			DictStringPropertyMaxLength.Add(SysTableFiledPathProperty,800); 
			DictStringPropertyMaxLength.Add(SysTableFiledFullPathProperty,800);           
			DictStringPropertyMaxLength.Add(StartValueEntityFieldProperty,800); 
			DictStringPropertyMaxLength.Add(EndValueEntityFieldProperty,800); 
			DictStringPropertyMaxLength.Add(StartValueDataSetFieldProperty,800); 
			DictStringPropertyMaxLength.Add(EndValueDataSetFieldProperty,800);  
			DictStringPropertyMaxLength.Add(CascadingRelationTableProperty,500); 
			DictStringPropertyMaxLength.Add(CascadingRelationTableParentKeyFieldProperty,500); 
			DictStringPropertyMaxLength.Add(CascadingRelationTableChildKeyFieldProperty,500);   
			DictStringPropertyMaxLength.Add(InnerEntitySubscribeFiledProperty,100);   
			DictStringPropertyMaxLength.Add(CascadingRelationTableSchemaOwnerProperty,500);  
			DictStringPropertyMaxLength.Add(DdlQueryTextProperty,4000); 
			DictStringPropertyMaxLength.Add(WhereClauseExpressProperty,1000);   
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
    


        /// <summary> The SearchId property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The PositionRow property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PositionRow
        {
            get { return  GetValue<Nullable<System.Int32>>( PositionRowProperty);}
            set { SetValue(PositionRowProperty,value); }
        }

        /// <summary> The PositionColumn property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PositionColumn
        {
            get { return  GetValue<Nullable<System.Int32>>( PositionColumnProperty);}
            set { SetValue(PositionColumnProperty,value); }
        }

        /// <summary> The OperationId property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OperationId
        {
            get { return  GetValue<Nullable<System.Int32>>( OperationIdProperty);}
            set { SetValue(OperationIdProperty,value); }
        }

        /// <summary> The DisplayText property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayText
        {
            get { return  GetValue<System.String>( DisplayTextProperty);}
            set { SetValue(DisplayTextProperty,value); }
        }

        /// <summary> The IsVisible property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsVisible
        {
            get { return  GetValue<System.Boolean>( IsVisibleProperty);}
            set { SetValue(IsVisibleProperty,value); }
        }

        /// <summary> The DefaultValue property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefaultValue
        {
            get { return  GetValue<System.String>( DefaultValueProperty);}
            set { SetValue(DefaultValueProperty,value); }
        }

        /// <summary> The IsReadOnly property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsReadOnly
        {
            get { return  GetValue<System.Boolean>( IsReadOnlyProperty);}
            set { SetValue(IsReadOnlyProperty,value); }
        }

        /// <summary> The IsAutoPopulate property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsAutoPopulate
        {
            get { return  GetValue<System.Boolean>( IsAutoPopulateProperty);}
            set { SetValue(IsAutoPopulateProperty,value); }
        }

        /// <summary> The ParentFieldId property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentFieldIdProperty);}
            set { SetValue(ParentFieldIdProperty,value); }
        }

        /// <summary> The IsLoadOnDemand property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsLoadOnDemand
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsLoadOnDemandProperty);}
            set { SetValue(IsLoadOnDemandProperty,value); }
        }

        /// <summary> The SysTableFiledPath property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SysTableFiledPath
        {
            get { return  GetValue<System.String>( SysTableFiledPathProperty);}
            set { SetValue(SysTableFiledPathProperty,value); }
        }

        /// <summary> The SysTableFiledFullPath property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SysTableFiledFullPath
        {
            get { return  GetValue<System.String>( SysTableFiledFullPathProperty);}
            set { SetValue(SysTableFiledFullPathProperty,value); }
        }

        /// <summary> The ControlType property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ControlType
        {
            get { return  GetValue<Nullable<System.Int32>>( ControlTypeProperty);}
            set { SetValue(ControlTypeProperty,value); }
        }

        /// <summary> The EntityId property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EntityId
        {
            get { return  GetValue<Nullable<System.Int32>>( EntityIdProperty);}
            set { SetValue(EntityIdProperty,value); }
        }

        /// <summary> The DataType property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTypeProperty);}
            set { SetValue(DataTypeProperty,value); }
        }

        /// <summary> The IsFilterByCurrentUser property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFilterByCurrentUser
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFilterByCurrentUserProperty);}
            set { SetValue(IsFilterByCurrentUserProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The IsChangedAutoExecute property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsChangedAutoExecute
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsChangedAutoExecuteProperty);}
            set { SetValue(IsChangedAutoExecuteProperty,value); }
        }

        /// <summary> The StartValueEntityField property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StartValueEntityField
        {
            get { return  GetValue<System.String>( StartValueEntityFieldProperty);}
            set { SetValue(StartValueEntityFieldProperty,value); }
        }

        /// <summary> The EndValueEntityField property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String EndValueEntityField
        {
            get { return  GetValue<System.String>( EndValueEntityFieldProperty);}
            set { SetValue(EndValueEntityFieldProperty,value); }
        }

        /// <summary> The StartValueDataSetField property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StartValueDataSetField
        {
            get { return  GetValue<System.String>( StartValueDataSetFieldProperty);}
            set { SetValue(StartValueDataSetFieldProperty,value); }
        }

        /// <summary> The EndValueDataSetField property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String EndValueDataSetField
        {
            get { return  GetValue<System.String>( EndValueDataSetFieldProperty);}
            set { SetValue(EndValueDataSetFieldProperty,value); }
        }

        /// <summary> The SubControlType property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SubControlType
        {
            get { return  GetValue<Nullable<System.Int32>>( SubControlTypeProperty);}
            set { SetValue(SubControlTypeProperty,value); }
        }

        /// <summary> The CascadingRelationTable property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTable
        {
            get { return  GetValue<System.String>( CascadingRelationTableProperty);}
            set { SetValue(CascadingRelationTableProperty,value); }
        }

        /// <summary> The CascadingRelationTableParentKeyField property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTableParentKeyField
        {
            get { return  GetValue<System.String>( CascadingRelationTableParentKeyFieldProperty);}
            set { SetValue(CascadingRelationTableParentKeyFieldProperty,value); }
        }

        /// <summary> The CascadingRelationTableChildKeyField property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTableChildKeyField
        {
            get { return  GetValue<System.String>( CascadingRelationTableChildKeyFieldProperty);}
            set { SetValue(CascadingRelationTableChildKeyFieldProperty,value); }
        }

        /// <summary> The IsAllowMultipleSelect property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowMultipleSelect
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowMultipleSelectProperty);}
            set { SetValue(IsAllowMultipleSelectProperty,value); }
        }

        /// <summary> The MasterEntityFieldlId property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MasterEntityFieldlId
        {
            get { return  GetValue<Nullable<System.Int32>>( MasterEntityFieldlIdProperty);}
            set { SetValue(MasterEntityFieldlIdProperty,value); }
        }

        /// <summary> The InnerEntitySubscribeFiled property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InnerEntitySubscribeFiled
        {
            get { return  GetValue<System.String>( InnerEntitySubscribeFiledProperty);}
            set { SetValue(InnerEntitySubscribeFiledProperty,value); }
        }

        /// <summary> The IsSkipSearch property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSkipSearch
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSkipSearchProperty);}
            set { SetValue(IsSkipSearchProperty,value); }
        }

        /// <summary> The DataRetrieveType property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataRetrieveType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataRetrieveTypeProperty);}
            set { SetValue(DataRetrieveTypeProperty,value); }
        }

        /// <summary> The CascadingRelationTableSchemaOwner property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CascadingRelationTableSchemaOwner
        {
            get { return  GetValue<System.String>( CascadingRelationTableSchemaOwnerProperty);}
            set { SetValue(CascadingRelationTableSchemaOwnerProperty,value); }
        }

        /// <summary> The AppExternalSourceFrom property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppExternalSourceFrom
        {
            get { return  GetValue<Nullable<System.Int32>>( AppExternalSourceFromProperty);}
            set { SetValue(AppExternalSourceFromProperty,value); }
        }

        /// <summary> The DdlQueryText property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DdlQueryText
        {
            get { return  GetValue<System.String>( DdlQueryTextProperty);}
            set { SetValue(DdlQueryTextProperty,value); }
        }

        /// <summary> The WhereClauseExpress property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WhereClauseExpress
        {
            get { return  GetValue<System.String>( WhereClauseExpressProperty);}
            set { SetValue(WhereClauseExpressProperty,value); }
        }

        /// <summary> The EmInternalCodeRegistration property of the Entity AppSearchField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmInternalCodeRegistration
        {
            get { return  GetValue<Nullable<System.Int32>>( EmInternalCodeRegistrationProperty);}
            set { SetValue(EmInternalCodeRegistrationProperty,value); }
        }
        
        #endregion

       
        
    }
}

