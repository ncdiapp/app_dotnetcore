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
    /// DTO class for the entity 'AppSysLabelLanguage'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSysLabelLanguageDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string LanguageIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.LanguageId);
        public static readonly string MenuIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.MenuId);
        public static readonly string TransactionFieldIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.TransactionFieldId);
        public static readonly string FormIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.FormId);
        public static readonly string LanguageTextProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,System.String>(o => o.LanguageText);
        public static readonly string TransactionUnitLinkedSearchIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.TransactionUnitLinkedSearchId);
        public static readonly string LinkTargetIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.LinkTargetId);
        public static readonly string SearchViewFieldIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.SearchViewFieldId);
        public static readonly string SearchFieldIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.SearchFieldId);
        public static readonly string TransactionUnitIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.TransactionUnitId);
        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.SearchId);
        public static readonly string ApplicationIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.ApplicationId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSysLabelLanguageDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSysLabelLanguageDto()
        {        
        }
		
		static AppSysLabelLanguageDto()
        {
                               
			   
			ForeignKeyProperties.Add(MenuIdProperty);                 		
                  
			DictStringPropertyMaxLength.Add(LanguageTextProperty,500);               
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
    


        /// <summary> The LanguageId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LanguageId
        {
            get { return  GetValue<Nullable<System.Int32>>( LanguageIdProperty);}
            set { SetValue(LanguageIdProperty,value); }
        }

        /// <summary> The MenuId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MenuId
        {
            get { return  GetValue<Nullable<System.Int32>>( MenuIdProperty);}
            set { SetValue(MenuIdProperty,value); }
        }

        /// <summary> The TransactionFieldId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionFieldIdProperty);}
            set { SetValue(TransactionFieldIdProperty,value); }
        }

        /// <summary> The FormId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FormId
        {
            get { return  GetValue<Nullable<System.Int32>>( FormIdProperty);}
            set { SetValue(FormIdProperty,value); }
        }

        /// <summary> The LanguageText property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LanguageText
        {
            get { return  GetValue<System.String>( LanguageTextProperty);}
            set { SetValue(LanguageTextProperty,value); }
        }

        /// <summary> The TransactionUnitLinkedSearchId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitLinkedSearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitLinkedSearchIdProperty);}
            set { SetValue(TransactionUnitLinkedSearchIdProperty,value); }
        }

        /// <summary> The LinkTargetId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LinkTargetId
        {
            get { return  GetValue<Nullable<System.Int32>>( LinkTargetIdProperty);}
            set { SetValue(LinkTargetIdProperty,value); }
        }

        /// <summary> The SearchViewFieldId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewFieldIdProperty);}
            set { SetValue(SearchViewFieldIdProperty,value); }
        }

        /// <summary> The SearchFieldId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchFieldIdProperty);}
            set { SetValue(SearchFieldIdProperty,value); }
        }

        /// <summary> The TransactionUnitId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionUnitId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionUnitIdProperty);}
            set { SetValue(TransactionUnitIdProperty,value); }
        }

        /// <summary> The SearchViewId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The SearchId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The ApplicationId property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( ApplicationIdProperty);}
            set { SetValue(ApplicationIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSysLabelLanguage</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSysLabelLanguage</summary>
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

