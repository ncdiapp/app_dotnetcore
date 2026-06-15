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
    /// DTO class for the entity 'AppSearchViewField'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchViewFieldDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,System.Int32>(o => o.SearchViewId);
        public static readonly string IsVisibleProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,System.Boolean>(o => o.IsVisible);
        public static readonly string DisplayTextProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,System.String>(o => o.DisplayText);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string WidthProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.Width);
        public static readonly string SysTableFiledPathProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,System.String>(o => o.SysTableFiledPath);
        public static readonly string MassUpdateTransactionFieldIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.MassUpdateTransactionFieldId);
        public static readonly string ControlTypeProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.ControlType);
        public static readonly string EntityIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.EntityId);
        public static readonly string DataTypeProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.DataType);
        public static readonly string IsGroupByProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsGroupBy);
        public static readonly string GroupByLevelProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.GroupByLevel);
        public static readonly string AggregationFunctionTypeProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.AggregationFunctionType);
        public static readonly string IsFilterByCurrentUserProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsFilterByCurrentUser);
        public static readonly string IsMapToChartXProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsMapToChartX);
        public static readonly string IsMapToChartYProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsMapToChartY);
        public static readonly string ChartYmappingOrderProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.ChartYmappingOrder);
        public static readonly string TreeLevelProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.TreeLevel);
        public static readonly string IsTreeNodeIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsTreeNodeId);
        public static readonly string IsTreeNodeDisplayProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsTreeNodeDisplay);
        public static readonly string IsTreeNodeDescProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsTreeNodeDesc);
        public static readonly string IsTreeNodeImageUrlProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsTreeNodeImageUrl);
        public static readonly string MappingSearchFieldIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.MappingSearchFieldId);
        public static readonly string ProductDetaiMapTransFiledIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.ProductDetaiMapTransFiledId);
        public static readonly string IsUserDefined1Property = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsUserDefined1);
        public static readonly string IsUserDefined2Property = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsUserDefined2);
        public static readonly string IsUserDefined3Property = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsUserDefined3);
        public static readonly string IsUserDefined4Property = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsUserDefined4);
        public static readonly string IsFileFoderIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsFileFoderId);
        public static readonly string IsTransRootIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsTransRootId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string RowNumberProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.RowNumber);
        public static readonly string ColumnNumberProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.ColumnNumber);
        public static readonly string OrderByLevelProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.OrderByLevel);
        public static readonly string IsDescOrderProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsDescOrder);
        public static readonly string IsReadOnlyProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsReadOnly);
        public static readonly string PullCriteriaAsDefaultValueSearchFieldIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.PullCriteriaAsDefaultValueSearchFieldId);
        public static readonly string EmInternalCodeRegistrationProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.EmInternalCodeRegistration);
        public static readonly string IsPartnerFilterFiledProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsPartnerFilterFiled);
        public static readonly string JoinToParentViewFieldIdProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Int32>>(o => o.JoinToParentViewFieldId);
        public static readonly string IsCalulationFieldProperty = ObjectInfoHelper.GetName<AppSearchViewFieldDto ,Nullable<System.Boolean>>(o => o.IsCalulationField);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSearchViewFieldDto()
        {        
        }
		
		static AppSearchViewFieldDto()
        {
              
			MandatoryProperties.Add(SearchViewIdProperty);  
			MandatoryProperties.Add(IsVisibleProperty);                                            
			  
			ForeignKeyProperties.Add(SearchViewIdProperty);       
			ForeignKeyProperties.Add(MassUpdateTransactionFieldIdProperty);   
			ForeignKeyProperties.Add(EntityIdProperty);               
			ForeignKeyProperties.Add(MappingSearchFieldIdProperty);  
			ForeignKeyProperties.Add(ProductDetaiMapTransFiledIdProperty);                  
			ForeignKeyProperties.Add(PullCriteriaAsDefaultValueSearchFieldIdProperty);     		
                
			DictStringPropertyMaxLength.Add(DisplayTextProperty,250);   
			DictStringPropertyMaxLength.Add(SysTableFiledPathProperty,800);                                         
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
    


        /// <summary> The SearchViewId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 SearchViewId
        {
            get { return  GetValue<System.Int32>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The IsVisible property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsVisible
        {
            get { return  GetValue<System.Boolean>( IsVisibleProperty);}
            set { SetValue(IsVisibleProperty,value); }
        }

        /// <summary> The DisplayText property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayText
        {
            get { return  GetValue<System.String>( DisplayTextProperty);}
            set { SetValue(DisplayTextProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The Width property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Width
        {
            get { return  GetValue<Nullable<System.Int32>>( WidthProperty);}
            set { SetValue(WidthProperty,value); }
        }

        /// <summary> The SysTableFiledPath property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SysTableFiledPath
        {
            get { return  GetValue<System.String>( SysTableFiledPathProperty);}
            set { SetValue(SysTableFiledPathProperty,value); }
        }

        /// <summary> The MassUpdateTransactionFieldId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MassUpdateTransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( MassUpdateTransactionFieldIdProperty);}
            set { SetValue(MassUpdateTransactionFieldIdProperty,value); }
        }

        /// <summary> The ControlType property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ControlType
        {
            get { return  GetValue<Nullable<System.Int32>>( ControlTypeProperty);}
            set { SetValue(ControlTypeProperty,value); }
        }

        /// <summary> The EntityId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EntityId
        {
            get { return  GetValue<Nullable<System.Int32>>( EntityIdProperty);}
            set { SetValue(EntityIdProperty,value); }
        }

        /// <summary> The DataType property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataTypeProperty);}
            set { SetValue(DataTypeProperty,value); }
        }

        /// <summary> The IsGroupBy property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsGroupBy
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsGroupByProperty);}
            set { SetValue(IsGroupByProperty,value); }
        }

        /// <summary> The GroupByLevel property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GroupByLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupByLevelProperty);}
            set { SetValue(GroupByLevelProperty,value); }
        }

        /// <summary> The AggregationFunctionType property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AggregationFunctionType
        {
            get { return  GetValue<Nullable<System.Int32>>( AggregationFunctionTypeProperty);}
            set { SetValue(AggregationFunctionTypeProperty,value); }
        }

        /// <summary> The IsFilterByCurrentUser property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFilterByCurrentUser
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFilterByCurrentUserProperty);}
            set { SetValue(IsFilterByCurrentUserProperty,value); }
        }

        /// <summary> The IsMapToChartX property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMapToChartX
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMapToChartXProperty);}
            set { SetValue(IsMapToChartXProperty,value); }
        }

        /// <summary> The IsMapToChartY property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMapToChartY
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMapToChartYProperty);}
            set { SetValue(IsMapToChartYProperty,value); }
        }

        /// <summary> The ChartYmappingOrder property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChartYmappingOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( ChartYmappingOrderProperty);}
            set { SetValue(ChartYmappingOrderProperty,value); }
        }

        /// <summary> The TreeLevel property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TreeLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( TreeLevelProperty);}
            set { SetValue(TreeLevelProperty,value); }
        }

        /// <summary> The IsTreeNodeId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsTreeNodeId
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsTreeNodeIdProperty);}
            set { SetValue(IsTreeNodeIdProperty,value); }
        }

        /// <summary> The IsTreeNodeDisplay property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsTreeNodeDisplay
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsTreeNodeDisplayProperty);}
            set { SetValue(IsTreeNodeDisplayProperty,value); }
        }

        /// <summary> The IsTreeNodeDesc property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsTreeNodeDesc
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsTreeNodeDescProperty);}
            set { SetValue(IsTreeNodeDescProperty,value); }
        }

        /// <summary> The IsTreeNodeImageUrl property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsTreeNodeImageUrl
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsTreeNodeImageUrlProperty);}
            set { SetValue(IsTreeNodeImageUrlProperty,value); }
        }

        /// <summary> The MappingSearchFieldId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MappingSearchFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( MappingSearchFieldIdProperty);}
            set { SetValue(MappingSearchFieldIdProperty,value); }
        }

        /// <summary> The ProductDetaiMapTransFiledId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProductDetaiMapTransFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProductDetaiMapTransFiledIdProperty);}
            set { SetValue(ProductDetaiMapTransFiledIdProperty,value); }
        }

        /// <summary> The IsUserDefined1 property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUserDefined1
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUserDefined1Property);}
            set { SetValue(IsUserDefined1Property,value); }
        }

        /// <summary> The IsUserDefined2 property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUserDefined2
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUserDefined2Property);}
            set { SetValue(IsUserDefined2Property,value); }
        }

        /// <summary> The IsUserDefined3 property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUserDefined3
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUserDefined3Property);}
            set { SetValue(IsUserDefined3Property,value); }
        }

        /// <summary> The IsUserDefined4 property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsUserDefined4
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsUserDefined4Property);}
            set { SetValue(IsUserDefined4Property,value); }
        }

        /// <summary> The IsFileFoderId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFileFoderId
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFileFoderIdProperty);}
            set { SetValue(IsFileFoderIdProperty,value); }
        }

        /// <summary> The IsTransRootId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsTransRootId
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsTransRootIdProperty);}
            set { SetValue(IsTransRootIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The RowNumber property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowNumber
        {
            get { return  GetValue<Nullable<System.Int32>>( RowNumberProperty);}
            set { SetValue(RowNumberProperty,value); }
        }

        /// <summary> The ColumnNumber property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ColumnNumber
        {
            get { return  GetValue<Nullable<System.Int32>>( ColumnNumberProperty);}
            set { SetValue(ColumnNumberProperty,value); }
        }

        /// <summary> The OrderByLevel property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrderByLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( OrderByLevelProperty);}
            set { SetValue(OrderByLevelProperty,value); }
        }

        /// <summary> The IsDescOrder property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDescOrder
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDescOrderProperty);}
            set { SetValue(IsDescOrderProperty,value); }
        }

        /// <summary> The IsReadOnly property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsReadOnly
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsReadOnlyProperty);}
            set { SetValue(IsReadOnlyProperty,value); }
        }

        /// <summary> The PullCriteriaAsDefaultValueSearchFieldId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PullCriteriaAsDefaultValueSearchFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( PullCriteriaAsDefaultValueSearchFieldIdProperty);}
            set { SetValue(PullCriteriaAsDefaultValueSearchFieldIdProperty,value); }
        }

        /// <summary> The EmInternalCodeRegistration property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmInternalCodeRegistration
        {
            get { return  GetValue<Nullable<System.Int32>>( EmInternalCodeRegistrationProperty);}
            set { SetValue(EmInternalCodeRegistrationProperty,value); }
        }

        /// <summary> The IsPartnerFilterFiled property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPartnerFilterFiled
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPartnerFilterFiledProperty);}
            set { SetValue(IsPartnerFilterFiledProperty,value); }
        }

        /// <summary> The JoinToParentViewFieldId property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> JoinToParentViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( JoinToParentViewFieldIdProperty);}
            set { SetValue(JoinToParentViewFieldIdProperty,value); }
        }

        /// <summary> The IsCalulationField property of the Entity AppSearchViewField</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsCalulationField
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsCalulationFieldProperty);}
            set { SetValue(IsCalulationFieldProperty,value); }
        }
        
        #endregion

       
        
    }
}

