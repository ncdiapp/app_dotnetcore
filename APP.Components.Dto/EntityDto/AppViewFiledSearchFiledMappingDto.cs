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
    /// DTO class for the entity 'AppViewFiledSearchFiledMapping'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppViewFiledSearchFiledMappingDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchViewIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.SearchViewId);
        public static readonly string SearchViewFiledIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.SearchViewFiledId);
        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.SearchId);
        public static readonly string SearchFieldIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.SearchFieldId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppViewFiledSearchFiledMappingDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppViewFiledSearchFiledMappingDto()
        {        
        }
		
		static AppViewFiledSearchFiledMappingDto()
        {
                      
			  
			ForeignKeyProperties.Add(SearchViewIdProperty);  
			ForeignKeyProperties.Add(SearchViewFiledIdProperty);  
			ForeignKeyProperties.Add(SearchIdProperty);  
			ForeignKeyProperties.Add(SearchFieldIdProperty);      		
                        
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
    


        /// <summary> The SearchViewId property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewIdProperty);}
            set { SetValue(SearchViewIdProperty,value); }
        }

        /// <summary> The SearchViewFiledId property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchViewFiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchViewFiledIdProperty);}
            set { SetValue(SearchViewFiledIdProperty,value); }
        }

        /// <summary> The SearchId property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The SearchFieldId property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SearchFieldId
        {
            get { return  GetValue<Nullable<System.Int32>>( SearchFieldIdProperty);}
            set { SetValue(SearchFieldIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppViewFiledSearchFiledMapping</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppViewFiledSearchFiledMapping</summary>
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

