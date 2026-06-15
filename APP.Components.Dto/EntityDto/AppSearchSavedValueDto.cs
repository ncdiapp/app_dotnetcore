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
    /// DTO class for the entity 'AppSearchSavedValue'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchSavedValueDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchSavedIdProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,System.Int32>(o => o.SearchSavedId);
        public static readonly string SearchFieldIdProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,System.Int32>(o => o.SearchFieldId);
        public static readonly string SearchValueProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,System.String>(o => o.SearchValue);
        public static readonly string OperationIdProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,Nullable<System.Int32>>(o => o.OperationId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSearchSavedValueDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSearchSavedValueDto()
        {        
        }
		
		static AppSearchSavedValueDto()
        {
              
			MandatoryProperties.Add(SearchSavedIdProperty);  
			MandatoryProperties.Add(SearchFieldIdProperty);        
			  
			ForeignKeyProperties.Add(SearchSavedIdProperty);  
			ForeignKeyProperties.Add(SearchFieldIdProperty);        		
                
			DictStringPropertyMaxLength.Add(SearchValueProperty,1000);        
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
    


        /// <summary> The SearchSavedId property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 SearchSavedId
        {
            get { return  GetValue<System.Int32>( SearchSavedIdProperty);}
            set { SetValue(SearchSavedIdProperty,value); }
        }

        /// <summary> The SearchFieldId property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 SearchFieldId
        {
            get { return  GetValue<System.Int32>( SearchFieldIdProperty);}
            set { SetValue(SearchFieldIdProperty,value); }
        }

        /// <summary> The SearchValue property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SearchValue
        {
            get { return  GetValue<System.String>( SearchValueProperty);}
            set { SetValue(SearchValueProperty,value); }
        }

        /// <summary> The OperationId property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OperationId
        {
            get { return  GetValue<Nullable<System.Int32>>( OperationIdProperty);}
            set { SetValue(OperationIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSearchSavedValue</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSearchSavedValue</summary>
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

