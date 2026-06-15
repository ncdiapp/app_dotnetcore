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
    /// DTO class for the entity 'AppViewLinkedSeaechOrUrl'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppViewLinkedSeaechOrUrlDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string LinkTargetUrlOrRouteCodeProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,System.String>(o => o.LinkTargetUrlOrRouteCode);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string LayoutDisplayModeProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.LayoutDisplayMode);
        public static readonly string SourceViewColumnId1Property = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.SourceViewColumnId1);
        public static readonly string SourceViewColumnId2Property = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.SourceViewColumnId2);
        public static readonly string SourceViewColumnId3Property = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.SourceViewColumnId3);
        public static readonly string TargetSearchFieldId1Property = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.TargetSearchFieldId1);
        public static readonly string TargetSearchFieldId2Property = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.TargetSearchFieldId2);
        public static readonly string TargetSearchFieldId3Property = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.TargetSearchFieldId3);
        public static readonly string DisplayTextProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,System.String>(o => o.DisplayText);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string LinkTargetSearchIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.LinkTargetSearchId);
        public static readonly string IsPopupProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Boolean>>(o => o.IsPopup);
        public static readonly string PopupWidthProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.PopupWidth);
        public static readonly string PopupHeightProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.PopupHeight);
        public static readonly string IconNameProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,System.String>(o => o.IconName);
        public static readonly string RowDisplayViewColumnIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.RowDisplayViewColumnId);
        public static readonly string SourceConditionViewColumnIdProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,Nullable<System.Int32>>(o => o.SourceConditionViewColumnId);
        public static readonly string OtherSettingsProperty = ObjectInfoHelper.GetName<AppViewLinkedSeaechOrUrlDto ,System.String>(o => o.OtherSettings);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppViewLinkedSeaechOrUrlDto()
        {        
        }
		
		static AppViewLinkedSeaechOrUrlDto()
        {
                                     
			  
			ForeignKeyProperties.Add(SearchViewIdProperty);                        		
               
			DictStringPropertyMaxLength.Add(LinkTargetUrlOrRouteCodeProperty,500);             
			DictStringPropertyMaxLength.Add(DisplayTextProperty,500);      
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
    


        /// <summary> The SearchViewId property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The LinkTargetUrlOrRouteCode property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LinkTargetUrlOrRouteCode
        {
            get { return  GetValue<System.String>( LinkTargetUrlOrRouteCodeProperty);}
            set { SetValue(LinkTargetUrlOrRouteCodeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The LayoutDisplayMode property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LayoutDisplayMode
        {
            get { return  GetValue<Nullable<System.Int32>>( LayoutDisplayModeProperty);}
            set { SetValue(LayoutDisplayModeProperty,value); }
        }

        /// <summary> The SourceViewColumnId1 property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceViewColumnId1
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceViewColumnId1Property);}
            set { SetValue(SourceViewColumnId1Property,value); }
        }

        /// <summary> The SourceViewColumnId2 property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceViewColumnId2
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceViewColumnId2Property);}
            set { SetValue(SourceViewColumnId2Property,value); }
        }

        /// <summary> The SourceViewColumnId3 property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceViewColumnId3
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceViewColumnId3Property);}
            set { SetValue(SourceViewColumnId3Property,value); }
        }

        /// <summary> The TargetSearchFieldId1 property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetSearchFieldId1
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetSearchFieldId1Property);}
            set { SetValue(TargetSearchFieldId1Property,value); }
        }

        /// <summary> The TargetSearchFieldId2 property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetSearchFieldId2
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetSearchFieldId2Property);}
            set { SetValue(TargetSearchFieldId2Property,value); }
        }

        /// <summary> The TargetSearchFieldId3 property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TargetSearchFieldId3
        {
            get { return  GetValue<Nullable<System.Int32>>( TargetSearchFieldId3Property);}
            set { SetValue(TargetSearchFieldId3Property,value); }
        }

        /// <summary> The DisplayText property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayText
        {
            get { return  GetValue<System.String>( DisplayTextProperty);}
            set { SetValue(DisplayTextProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The LinkTargetSearchId property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkTargetSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkTargetSearchIdProperty);}
            set { SetValue(LinkTargetSearchIdProperty,value); }
        }

        /// <summary> The IsPopup property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsPopup
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsPopupProperty);}
            set { SetValue(IsPopupProperty,value); }
        }

        /// <summary> The PopupWidth property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PopupWidth
        {
            get { return  GetValue<Nullable<System.Int32>>( PopupWidthProperty);}
            set { SetValue(PopupWidthProperty,value); }
        }

        /// <summary> The PopupHeight property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PopupHeight
        {
            get { return  GetValue<Nullable<System.Int32>>( PopupHeightProperty);}
            set { SetValue(PopupHeightProperty,value); }
        }

        /// <summary> The IconName property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IconName
        {
            get { return  GetValue<System.String>( IconNameProperty);}
            set { SetValue(IconNameProperty,value); }
        }

        /// <summary> The RowDisplayViewColumnId property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RowDisplayViewColumnId
        {
            get { return  GetValue<Nullable<System.Int32>>( RowDisplayViewColumnIdProperty);}
            set { SetValue(RowDisplayViewColumnIdProperty,value); }
        }

        /// <summary> The SourceConditionViewColumnId property of the Entity AppViewLinkedSeaechOrUrl</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SourceConditionViewColumnId
        {
            get { return  GetValue<Nullable<System.Int32>>( SourceConditionViewColumnIdProperty);}
            set { SetValue(SourceConditionViewColumnIdProperty,value); }
        }

        /// <summary> The OtherSettings property of the Entity AppViewLinkedSeaechOrUrl</summary>
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

