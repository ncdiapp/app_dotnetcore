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
    /// DTO class for the entity 'AppDesktopItem'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDesktopItemDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DesktopIdProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.DesktopId);
        public static readonly string WidgetItemTypeProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.WidgetItemType);
        public static readonly string FlowOrGridLayoutSortOrderProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.FlowOrGridLayoutSortOrder);
        public static readonly string StyleLayoutInfoProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,System.String>(o => o.StyleLayoutInfo);
        public static readonly string DomElementTagProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,System.String>(o => o.DomElementTag);
        public static readonly string ParameterKeyValueProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,System.String>(o => o.ParameterKeyValue);
        public static readonly string DisplayTitleProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,System.String>(o => o.DisplayTitle);
        public static readonly string RowIndexProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.RowIndex);
        public static readonly string ColumnIndexProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.ColumnIndex);
        public static readonly string RowSpanProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.RowSpan);
        public static readonly string ColumnSpanProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.ColumnSpan);
        public static readonly string GridLayoutParentIdProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.GridLayoutParentId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppDesktopItemDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppDesktopItemDto()
        {        
        }
		
		static AppDesktopItemDto()
        {
                              
			  
			ForeignKeyProperties.Add(DesktopIdProperty);                 		
                 
			DictStringPropertyMaxLength.Add(StyleLayoutInfoProperty,2000); 
			DictStringPropertyMaxLength.Add(DomElementTagProperty,2000); 
			DictStringPropertyMaxLength.Add(ParameterKeyValueProperty,2000); 
			DictStringPropertyMaxLength.Add(DisplayTitleProperty,500);            
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
    


        /// <summary> The DesktopId property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DesktopId
        {
            get { return  GetValue<Nullable<System.Int32>>( DesktopIdProperty);}
            set { SetValue(DesktopIdProperty,value); }
        }

        /// <summary> The WidgetItemType property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WidgetItemType
        {
            get { return  GetValue<Nullable<System.Int32>>( WidgetItemTypeProperty);}
            set { SetValue(WidgetItemTypeProperty,value); }
        }

        /// <summary> The FlowOrGridLayoutSortOrder property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FlowOrGridLayoutSortOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( FlowOrGridLayoutSortOrderProperty);}
            set { SetValue(FlowOrGridLayoutSortOrderProperty,value); }
        }

        /// <summary> The StyleLayoutInfo property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StyleLayoutInfo
        {
            get { return  GetValue<System.String>( StyleLayoutInfoProperty);}
            set { SetValue(StyleLayoutInfoProperty,value); }
        }

        /// <summary> The DomElementTag property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DomElementTag
        {
            get { return  GetValue<System.String>( DomElementTagProperty);}
            set { SetValue(DomElementTagProperty,value); }
        }

        /// <summary> The ParameterKeyValue property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParameterKeyValue
        {
            get { return  GetValue<System.String>( ParameterKeyValueProperty);}
            set { SetValue(ParameterKeyValueProperty,value); }
        }

        /// <summary> The DisplayTitle property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayTitle
        {
            get { return  GetValue<System.String>( DisplayTitleProperty);}
            set { SetValue(DisplayTitleProperty,value); }
        }

        /// <summary> The RowIndex property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowIndex
        {
            get { return  GetValue<Nullable<System.Int32>>( RowIndexProperty);}
            set { SetValue(RowIndexProperty,value); }
        }

        /// <summary> The ColumnIndex property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ColumnIndex
        {
            get { return  GetValue<Nullable<System.Int32>>( ColumnIndexProperty);}
            set { SetValue(ColumnIndexProperty,value); }
        }

        /// <summary> The RowSpan property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowSpan
        {
            get { return  GetValue<Nullable<System.Int32>>( RowSpanProperty);}
            set { SetValue(RowSpanProperty,value); }
        }

        /// <summary> The ColumnSpan property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ColumnSpan
        {
            get { return  GetValue<Nullable<System.Int32>>( ColumnSpanProperty);}
            set { SetValue(ColumnSpanProperty,value); }
        }

        /// <summary> The GridLayoutParentId property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GridLayoutParentId
        {
            get { return  GetValue<Nullable<System.Int32>>( GridLayoutParentIdProperty);}
            set { SetValue(GridLayoutParentIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppDesktopItem</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppDesktopItem</summary>
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

