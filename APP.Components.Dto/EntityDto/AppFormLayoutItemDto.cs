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
    /// DTO class for the entity 'AppFormLayoutItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppFormLayoutItemDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string FormIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.FormId);
        public static readonly string WidgetItemTypeProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.WidgetItemType);
        public static readonly string FlowOrGridLayoutSortOrderProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.FlowOrGridLayoutSortOrder);
        public static readonly string StyleLayoutInfoProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,System.String>(o => o.StyleLayoutInfo);
        public static readonly string DomElementTagProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,System.String>(o => o.DomElementTag);
        public static readonly string ParameterKeyValueProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,System.String>(o => o.ParameterKeyValue);
        public static readonly string DisplayTitleProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,System.String>(o => o.DisplayTitle);
        public static readonly string RowIndexProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.RowIndex);
        public static readonly string ColumnIndexProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.ColumnIndex);
        public static readonly string RowSpanProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.RowSpan);
        public static readonly string ColumnSpanProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.ColumnSpan);
        public static readonly string UigridLayoutParentIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.UigridLayoutParentId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string GridTransactionUnitIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.GridTransactionUnitId);
        public static readonly string SearchViewFieldIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.SearchViewFieldId);
        public static readonly string AutoExcuteSearchIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.AutoExcuteSearchId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string CurrentHostIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,System.String>(o => o.CurrentHostId);
        public static readonly string ParentHostIdProperty = ObjectInfoHelper.GetName<AppFormLayoutItemDto ,System.String>(o => o.ParentHostId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppFormLayoutItemDto()
        {        
        }
		
		static AppFormLayoutItemDto()
        {
                                    
			  
			ForeignKeyProperties.Add(FormIdProperty);            
			ForeignKeyProperties.Add(UigridLayoutParentIdProperty);  
			ForeignKeyProperties.Add(TransactionFieldIdProperty);  
			ForeignKeyProperties.Add(GridTransactionUnitIdProperty);  
			ForeignKeyProperties.Add(SearchViewFieldIdProperty);         		
                 
			DictStringPropertyMaxLength.Add(StyleLayoutInfoProperty,2000); 
			DictStringPropertyMaxLength.Add(DomElementTagProperty,2000); 
			DictStringPropertyMaxLength.Add(ParameterKeyValueProperty,2147483647); 
			DictStringPropertyMaxLength.Add(DisplayTitleProperty,500);               
			DictStringPropertyMaxLength.Add(CurrentHostIdProperty,50); 
			DictStringPropertyMaxLength.Add(ParentHostIdProperty,50);  
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
    


        /// <summary> The FormId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormIdProperty);}
            set { SetValue(FormIdProperty,value); }
        }

        /// <summary> The WidgetItemType property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WidgetItemType
        {
            get { return  GetValue<Nullable<System.Int32>>( WidgetItemTypeProperty);}
            set { SetValue(WidgetItemTypeProperty,value); }
        }

        /// <summary> The FlowOrGridLayoutSortOrder property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FlowOrGridLayoutSortOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( FlowOrGridLayoutSortOrderProperty);}
            set { SetValue(FlowOrGridLayoutSortOrderProperty,value); }
        }

        /// <summary> The StyleLayoutInfo property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StyleLayoutInfo
        {
            get { return  GetValue<System.String>( StyleLayoutInfoProperty);}
            set { SetValue(StyleLayoutInfoProperty,value); }
        }

        /// <summary> The DomElementTag property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DomElementTag
        {
            get { return  GetValue<System.String>( DomElementTagProperty);}
            set { SetValue(DomElementTagProperty,value); }
        }

        /// <summary> The ParameterKeyValue property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParameterKeyValue
        {
            get { return  GetValue<System.String>( ParameterKeyValueProperty);}
            set { SetValue(ParameterKeyValueProperty,value); }
        }

        /// <summary> The DisplayTitle property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayTitle
        {
            get { return  GetValue<System.String>( DisplayTitleProperty);}
            set { SetValue(DisplayTitleProperty,value); }
        }

        /// <summary> The RowIndex property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowIndex
        {
            get { return  GetValue<Nullable<System.Int32>>( RowIndexProperty);}
            set { SetValue(RowIndexProperty,value); }
        }

        /// <summary> The ColumnIndex property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ColumnIndex
        {
            get { return  GetValue<Nullable<System.Int32>>( ColumnIndexProperty);}
            set { SetValue(ColumnIndexProperty,value); }
        }

        /// <summary> The RowSpan property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowSpan
        {
            get { return  GetValue<Nullable<System.Int32>>( RowSpanProperty);}
            set { SetValue(RowSpanProperty,value); }
        }

        /// <summary> The ColumnSpan property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ColumnSpan
        {
            get { return  GetValue<Nullable<System.Int32>>( ColumnSpanProperty);}
            set { SetValue(ColumnSpanProperty,value); }
        }

        /// <summary> The UigridLayoutParentId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UigridLayoutParentId
        {
            get { return  GetValue<Nullable<System.Int32>>( UigridLayoutParentIdProperty);}
            set { SetValue(UigridLayoutParentIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The GridTransactionUnitId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GridTransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( GridTransactionUnitIdProperty);}
            set { SetValue(GridTransactionUnitIdProperty,value); }
        }

        /// <summary> The SearchViewFieldId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewFieldIdProperty);}
            set { SetValue(SearchViewFieldIdProperty,value); }
        }

        /// <summary> The AutoExcuteSearchId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AutoExcuteSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( AutoExcuteSearchIdProperty);}
            set { SetValue(AutoExcuteSearchIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The CurrentHostId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CurrentHostId
        {
            get { return  GetValue<System.String>( CurrentHostIdProperty);}
            set { SetValue(CurrentHostIdProperty,value); }
        }

        /// <summary> The ParentHostId property of the Entity AppFormLayoutItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParentHostId
        {
            get { return  GetValue<System.String>( ParentHostIdProperty);}
            set { SetValue(ParentHostIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

