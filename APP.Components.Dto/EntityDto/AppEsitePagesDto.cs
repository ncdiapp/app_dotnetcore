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
    /// DTO class for the entity 'AppEsitePages'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEsitePagesDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string EsiteIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.EsiteId);
        public static readonly string TitleProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.Title);
        public static readonly string EmresourceContentTypeProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.EmresourceContentType);
        public static readonly string HtmlContentProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.HtmlContent);
        public static readonly string LoadOrderProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.LoadOrder);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string MetaDesciptionProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.MetaDesciption);
        public static readonly string UrlAndHandleProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.UrlAndHandle);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string IsDefaultProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Boolean>>(o => o.IsDefault);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string ControllerNameProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.ControllerName);
        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.SearchId);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string IsMasterLayoutPageProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,Nullable<System.Boolean>>(o => o.IsMasterLayoutPage);
        public static readonly string PageJsMethodProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.PageJsMethod);
        public static readonly string PageCssStyleProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.PageCssStyle);
        public static readonly string NavigationCtrlJavascriptProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.NavigationCtrlJavascript);
        public static readonly string FileFullPathProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.FileFullPath);
        public static readonly string DesignLayoutProperty = ObjectInfoHelper.GetName<AppEsitePagesDto ,System.String>(o => o.DesignLayout);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEsitePagesDto()
        {        
        }
		
		static AppEsitePagesDto()
        {
                                     
			  
			ForeignKeyProperties.Add(EsiteIdProperty);         
			ForeignKeyProperties.Add(TransactionIdProperty);                		
               
			DictStringPropertyMaxLength.Add(TitleProperty,100);  
			DictStringPropertyMaxLength.Add(HtmlContentProperty,2147483647);   
			DictStringPropertyMaxLength.Add(MetaDesciptionProperty,250); 
			DictStringPropertyMaxLength.Add(UrlAndHandleProperty,100);        
			DictStringPropertyMaxLength.Add(ControllerNameProperty,500);    
			DictStringPropertyMaxLength.Add(PageJsMethodProperty,2147483647); 
			DictStringPropertyMaxLength.Add(PageCssStyleProperty,2147483647); 
			DictStringPropertyMaxLength.Add(NavigationCtrlJavascriptProperty,4000); 
			DictStringPropertyMaxLength.Add(FileFullPathProperty,800); 
			DictStringPropertyMaxLength.Add(DesignLayoutProperty,2147483647);  
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
    


        /// <summary> The EsiteId property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EsiteId
        {
            get { return  GetValue<Nullable<System.Int32>>( EsiteIdProperty);}
            set { SetValue(EsiteIdProperty,value); }
        }

        /// <summary> The Title property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Title
        {
            get { return  GetValue<System.String>( TitleProperty);}
            set { SetValue(TitleProperty,value); }
        }

        /// <summary> The EmresourceContentType property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmresourceContentType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmresourceContentTypeProperty);}
            set { SetValue(EmresourceContentTypeProperty,value); }
        }

        /// <summary> The HtmlContent property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String HtmlContent
        {
            get { return  GetValue<System.String>( HtmlContentProperty);}
            set { SetValue(HtmlContentProperty,value); }
        }

        /// <summary> The LoadOrder property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LoadOrder
        {
            get { return  GetValue<Nullable<System.Int32>>( LoadOrderProperty);}
            set { SetValue(LoadOrderProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The MetaDesciption property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MetaDesciption
        {
            get { return  GetValue<System.String>( MetaDesciptionProperty);}
            set { SetValue(MetaDesciptionProperty,value); }
        }

        /// <summary> The UrlAndHandle property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UrlAndHandle
        {
            get { return  GetValue<System.String>( UrlAndHandleProperty);}
            set { SetValue(UrlAndHandleProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The IsDefault property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDefault
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDefaultProperty);}
            set { SetValue(IsDefaultProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The ControllerName property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ControllerName
        {
            get { return  GetValue<System.String>( ControllerNameProperty);}
            set { SetValue(ControllerNameProperty,value); }
        }

        /// <summary> The SearchId property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The IsMasterLayoutPage property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMasterLayoutPage
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMasterLayoutPageProperty);}
            set { SetValue(IsMasterLayoutPageProperty,value); }
        }

        /// <summary> The PageJsMethod property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PageJsMethod
        {
            get { return  GetValue<System.String>( PageJsMethodProperty);}
            set { SetValue(PageJsMethodProperty,value); }
        }

        /// <summary> The PageCssStyle property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PageCssStyle
        {
            get { return  GetValue<System.String>( PageCssStyleProperty);}
            set { SetValue(PageCssStyleProperty,value); }
        }

        /// <summary> The NavigationCtrlJavascript property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NavigationCtrlJavascript
        {
            get { return  GetValue<System.String>( NavigationCtrlJavascriptProperty);}
            set { SetValue(NavigationCtrlJavascriptProperty,value); }
        }

        /// <summary> The FileFullPath property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FileFullPath
        {
            get { return  GetValue<System.String>( FileFullPathProperty);}
            set { SetValue(FileFullPathProperty,value); }
        }

        /// <summary> The DesignLayout property of the Entity AppEsitePages</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DesignLayout
        {
            get { return  GetValue<System.String>( DesignLayoutProperty);}
            set { SetValue(DesignLayoutProperty,value); }
        }
        
        #endregion

       
        
    }
}

