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
    /// DTO class for the entity 'AppSearchView'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchViewDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.Description);
        public static readonly string NoSecurityProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.Boolean>(o => o.NoSecurity);
        public static readonly string GridOutputModeProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.Int32>(o => o.GridOutputMode);
        public static readonly string OptionsProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.Int32>(o => o.Options);
        public static readonly string ViewTypeProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.Int32>(o => o.ViewType);
        public static readonly string WhereUsedDefaultViewIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.WhereUsedDefaultViewId);
        public static readonly string PivotOrChartSettingProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.PivotOrChartSetting);
        public static readonly string ColumnCountProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.Int32>(o => o.ColumnCount);
        public static readonly string RowPerPageProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.Int32>(o => o.RowPerPage);
        public static readonly string IsFilterByCurrentUserProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsFilterByCurrentUser);
        public static readonly string DataSetIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.DataSetId);
        public static readonly string ChartInnerRadiusProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Decimal>>(o => o.ChartInnerRadius);
        public static readonly string ChartTypeProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.ChartType);
        public static readonly string CatalogueSearchIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.CatalogueSearchId);
        public static readonly string FilterSearchIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.FilterSearchId);
        public static readonly string EntityInternalCodeProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.EntityInternalCode);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string ProductDetaiViewMapUnitIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.ProductDetaiViewMapUnitId);
        public static readonly string IsMasterEditInSamePageProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.IsMasterEditInSamePage);
        public static readonly string UpdateTransctionIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.UpdateTransctionId);
        public static readonly string UpdateTransctionRootFieldNameProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.UpdateTransctionRootFieldName);
        public static readonly string UpdateChildParentFkfieldNameProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.UpdateChildParentFkfieldName);
        public static readonly string UpdateBaseTranscationUnitIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.UpdateBaseTranscationUnitId);
        public static readonly string AppRestResourceUriProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.AppRestResourceUri);
        public static readonly string AppRestResourceUriDisplayProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,System.String>(o => o.AppRestResourceUriDisplay);
        public static readonly string NbFrozenColumnProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.NbFrozenColumn);
        public static readonly string IsMassUpdateViewProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsMassUpdateView);
        public static readonly string IsAllowAddRowProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsAllowAddRow);
        public static readonly string IsAllowDeleteRowProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsAllowDeleteRow);
        public static readonly string IsAllowUpdateRowProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsAllowUpdateRow);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string CanlendarDefaultViewModeProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.CanlendarDefaultViewMode);
        public static readonly string IsEnableCalendarMonthViewProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsEnableCalendarMonthView);
        public static readonly string IsEnableCalendarWeekViewProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsEnableCalendarWeekView);
        public static readonly string IsEnableCalendarDayViewProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsEnableCalendarDayView);
        public static readonly string IsEnableCalendarNavigatorProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsEnableCalendarNavigator);
        public static readonly string IsDisableClientTimeConvertProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsDisableClientTimeConvert);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string IsForPublicAcesssProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsForPublicAcesss);
        public static readonly string IsFilterByUserTypeEntityProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Boolean>>(o => o.IsFilterByUserTypeEntity);
        public static readonly string CalendarStartHourProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.CalendarStartHour);
        public static readonly string CalendarEndHourProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.CalendarEndHour);
        public static readonly string HierachyParentViewIdProperty = ObjectInfoHelper.GetName<AppSearchViewDto ,Nullable<System.Int32>>(o => o.HierachyParentViewId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSearchViewDto()
        {        
        }
		
		static AppSearchViewDto()
        {
              
			MandatoryProperties.Add(NameProperty);   
			MandatoryProperties.Add(NoSecurityProperty);  
			MandatoryProperties.Add(GridOutputModeProperty);  
			MandatoryProperties.Add(OptionsProperty);  
			MandatoryProperties.Add(ViewTypeProperty);    
			MandatoryProperties.Add(ColumnCountProperty);  
			MandatoryProperties.Add(RowPerPageProperty);                                       
			             
			ForeignKeyProperties.Add(DataSetIdProperty);    
			ForeignKeyProperties.Add(CatalogueSearchIdProperty);  
			ForeignKeyProperties.Add(FilterSearchIdProperty);   
			ForeignKeyProperties.Add(TransactionIdProperty);  
			ForeignKeyProperties.Add(ProductDetaiViewMapUnitIdProperty);                              		
              
			DictStringPropertyMaxLength.Add(NameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,2500);      
			DictStringPropertyMaxLength.Add(PivotOrChartSettingProperty,2147483647);         
			DictStringPropertyMaxLength.Add(EntityInternalCodeProperty,100);   
			DictStringPropertyMaxLength.Add(IsMasterEditInSamePageProperty,10);  
			DictStringPropertyMaxLength.Add(UpdateTransctionRootFieldNameProperty,50); 
			DictStringPropertyMaxLength.Add(UpdateChildParentFkfieldNameProperty,50);  
			DictStringPropertyMaxLength.Add(AppRestResourceUriProperty,1000); 
			DictStringPropertyMaxLength.Add(AppRestResourceUriDisplayProperty,1000);                        
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
    


        /// <summary> The Name property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The NoSecurity property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean NoSecurity
        {
            get { return  GetValue<System.Boolean>( NoSecurityProperty);}
            set { SetValue(NoSecurityProperty,value); }
        }

        /// <summary> The GridOutputMode property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 GridOutputMode
        {
            get { return  GetValue<System.Int32>( GridOutputModeProperty);}
            set { SetValue(GridOutputModeProperty,value); }
        }

        /// <summary> The Options property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 Options
        {
            get { return  GetValue<System.Int32>( OptionsProperty);}
            set { SetValue(OptionsProperty,value); }
        }

        /// <summary> The ViewType property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ViewType
        {
            get { return  GetValue<System.Int32>( ViewTypeProperty);}
            set { SetValue(ViewTypeProperty,value); }
        }

        /// <summary> The WhereUsedDefaultViewId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WhereUsedDefaultViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( WhereUsedDefaultViewIdProperty);}
            set { SetValue(WhereUsedDefaultViewIdProperty,value); }
        }

        /// <summary> The PivotOrChartSetting property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PivotOrChartSetting
        {
            get { return  GetValue<System.String>( PivotOrChartSettingProperty);}
            set { SetValue(PivotOrChartSettingProperty,value); }
        }

        /// <summary> The ColumnCount property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ColumnCount
        {
            get { return  GetValue<System.Int32>( ColumnCountProperty);}
            set { SetValue(ColumnCountProperty,value); }
        }

        /// <summary> The RowPerPage property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 RowPerPage
        {
            get { return  GetValue<System.Int32>( RowPerPageProperty);}
            set { SetValue(RowPerPageProperty,value); }
        }

        /// <summary> The IsFilterByCurrentUser property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFilterByCurrentUser
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFilterByCurrentUserProperty);}
            set { SetValue(IsFilterByCurrentUserProperty,value); }
        }

        /// <summary> The DataSetId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSetIdProperty);}
            set { SetValue(DataSetIdProperty,value); }
        }

        /// <summary> The ChartInnerRadius property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Decimal> ChartInnerRadius
        {
            get { return  GetValue<Nullable<System.Decimal>>( ChartInnerRadiusProperty);}
            set { SetValue(ChartInnerRadiusProperty,value); }
        }

        /// <summary> The ChartType property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ChartType
        {
            get { return  GetValue<Nullable<System.Int32>>( ChartTypeProperty);}
            set { SetValue(ChartTypeProperty,value); }
        }

        /// <summary> The CatalogueSearchId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CatalogueSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( CatalogueSearchIdProperty);}
            set { SetValue(CatalogueSearchIdProperty,value); }
        }

        /// <summary> The FilterSearchId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FilterSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( FilterSearchIdProperty);}
            set { SetValue(FilterSearchIdProperty,value); }
        }

        /// <summary> The EntityInternalCode property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String EntityInternalCode
        {
            get { return  GetValue<System.String>( EntityInternalCodeProperty);}
            set { SetValue(EntityInternalCodeProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The ProductDetaiViewMapUnitId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProductDetaiViewMapUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProductDetaiViewMapUnitIdProperty);}
            set { SetValue(ProductDetaiViewMapUnitIdProperty,value); }
        }

        /// <summary> The IsMasterEditInSamePage property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IsMasterEditInSamePage
        {
            get { return  GetValue<System.String>( IsMasterEditInSamePageProperty);}
            set { SetValue(IsMasterEditInSamePageProperty,value); }
        }

        /// <summary> The UpdateTransctionId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UpdateTransctionId
        {
            get { return  GetValue<Nullable<System.Int32>>( UpdateTransctionIdProperty);}
            set { SetValue(UpdateTransctionIdProperty,value); }
        }

        /// <summary> The UpdateTransctionRootFieldName property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UpdateTransctionRootFieldName
        {
            get { return  GetValue<System.String>( UpdateTransctionRootFieldNameProperty);}
            set { SetValue(UpdateTransctionRootFieldNameProperty,value); }
        }

        /// <summary> The UpdateChildParentFkfieldName property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UpdateChildParentFkfieldName
        {
            get { return  GetValue<System.String>( UpdateChildParentFkfieldNameProperty);}
            set { SetValue(UpdateChildParentFkfieldNameProperty,value); }
        }

        /// <summary> The UpdateBaseTranscationUnitId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UpdateBaseTranscationUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( UpdateBaseTranscationUnitIdProperty);}
            set { SetValue(UpdateBaseTranscationUnitIdProperty,value); }
        }

        /// <summary> The AppRestResourceUri property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AppRestResourceUri
        {
            get { return  GetValue<System.String>( AppRestResourceUriProperty);}
            set { SetValue(AppRestResourceUriProperty,value); }
        }

        /// <summary> The AppRestResourceUriDisplay property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AppRestResourceUriDisplay
        {
            get { return  GetValue<System.String>( AppRestResourceUriDisplayProperty);}
            set { SetValue(AppRestResourceUriDisplayProperty,value); }
        }

        /// <summary> The NbFrozenColumn property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> NbFrozenColumn
        {
            get { return  GetValue<Nullable<System.Int32>>( NbFrozenColumnProperty);}
            set { SetValue(NbFrozenColumnProperty,value); }
        }

        /// <summary> The IsMassUpdateView property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMassUpdateView
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMassUpdateViewProperty);}
            set { SetValue(IsMassUpdateViewProperty,value); }
        }

        /// <summary> The IsAllowAddRow property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowAddRow
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowAddRowProperty);}
            set { SetValue(IsAllowAddRowProperty,value); }
        }

        /// <summary> The IsAllowDeleteRow property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowDeleteRow
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowDeleteRowProperty);}
            set { SetValue(IsAllowDeleteRowProperty,value); }
        }

        /// <summary> The IsAllowUpdateRow property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAllowUpdateRow
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAllowUpdateRowProperty);}
            set { SetValue(IsAllowUpdateRowProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The CanlendarDefaultViewMode property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CanlendarDefaultViewMode
        {
            get { return  GetValue<Nullable<System.Int32>>( CanlendarDefaultViewModeProperty);}
            set { SetValue(CanlendarDefaultViewModeProperty,value); }
        }

        /// <summary> The IsEnableCalendarMonthView property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsEnableCalendarMonthView
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsEnableCalendarMonthViewProperty);}
            set { SetValue(IsEnableCalendarMonthViewProperty,value); }
        }

        /// <summary> The IsEnableCalendarWeekView property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsEnableCalendarWeekView
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsEnableCalendarWeekViewProperty);}
            set { SetValue(IsEnableCalendarWeekViewProperty,value); }
        }

        /// <summary> The IsEnableCalendarDayView property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsEnableCalendarDayView
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsEnableCalendarDayViewProperty);}
            set { SetValue(IsEnableCalendarDayViewProperty,value); }
        }

        /// <summary> The IsEnableCalendarNavigator property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsEnableCalendarNavigator
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsEnableCalendarNavigatorProperty);}
            set { SetValue(IsEnableCalendarNavigatorProperty,value); }
        }

        /// <summary> The IsDisableClientTimeConvert property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDisableClientTimeConvert
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDisableClientTimeConvertProperty);}
            set { SetValue(IsDisableClientTimeConvertProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The IsForPublicAcesss property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsForPublicAcesss
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsForPublicAcesssProperty);}
            set { SetValue(IsForPublicAcesssProperty,value); }
        }

        /// <summary> The IsFilterByUserTypeEntity property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFilterByUserTypeEntity
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFilterByUserTypeEntityProperty);}
            set { SetValue(IsFilterByUserTypeEntityProperty,value); }
        }

        /// <summary> The CalendarStartHour property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CalendarStartHour
        {
            get { return  GetValue<Nullable<System.Int32>>( CalendarStartHourProperty);}
            set { SetValue(CalendarStartHourProperty,value); }
        }

        /// <summary> The CalendarEndHour property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CalendarEndHour
        {
            get { return  GetValue<Nullable<System.Int32>>( CalendarEndHourProperty);}
            set { SetValue(CalendarEndHourProperty,value); }
        }

        /// <summary> The HierachyParentViewId property of the Entity AppSearchView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> HierachyParentViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( HierachyParentViewIdProperty);}
            set { SetValue(HierachyParentViewIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

