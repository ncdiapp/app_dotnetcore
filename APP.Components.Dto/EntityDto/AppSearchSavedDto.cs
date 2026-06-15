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
    /// DTO class for the entity 'AppSearchSaved'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSearchSavedDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string SearchIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,System.Int32>(o => o.SearchId);
        public static readonly string SavedSearchNameProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,System.String>(o => o.SavedSearchName);
        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,System.Int32>(o => o.UserId);
        public static readonly string SystemTimeStampProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,System.Byte[]>(o => o.SystemTimeStamp);
        public static readonly string IsDefaultProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Boolean>>(o => o.IsDefault);
        public static readonly string IsAutoExecuteProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Boolean>>(o => o.IsAutoExecute);
        public static readonly string GroupIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Int32>>(o => o.GroupId);
        public static readonly string DefaultSearchViewIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Int32>>(o => o.DefaultSearchViewId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSearchSavedDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSearchSavedDto()
        {        
        }
		
		static AppSearchSavedDto()
        {
              
			MandatoryProperties.Add(SearchIdProperty);  
			MandatoryProperties.Add(SavedSearchNameProperty);  
			MandatoryProperties.Add(UserIdProperty);           
			  
			ForeignKeyProperties.Add(SearchIdProperty);   
			ForeignKeyProperties.Add(UserIdProperty);     
			ForeignKeyProperties.Add(GroupIdProperty);  
			ForeignKeyProperties.Add(DefaultSearchViewIdProperty);      		
               
			DictStringPropertyMaxLength.Add(SavedSearchNameProperty,500);             
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
    


        /// <summary> The SearchId property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 SearchId
        {
            get { return  GetValue<System.Int32>( SearchIdProperty);}
            set { SetValue(SearchIdProperty,value); }
        }

        /// <summary> The SavedSearchName property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SavedSearchName
        {
            get { return  GetValue<System.String>( SavedSearchNameProperty);}
            set { SetValue(SavedSearchNameProperty,value); }
        }

        /// <summary> The UserId property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 UserId
        {
            get { return  GetValue<System.Int32>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The SystemTimeStamp property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Byte[] SystemTimeStamp
        {
            get { return  GetValue<System.Byte[]>( SystemTimeStampProperty);}
            set { SetValue(SystemTimeStampProperty,value); }
        }

        /// <summary> The IsDefault property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDefault
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDefaultProperty);}
            set { SetValue(IsDefaultProperty,value); }
        }

        /// <summary> The IsAutoExecute property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAutoExecute
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAutoExecuteProperty);}
            set { SetValue(IsAutoExecuteProperty,value); }
        }

        /// <summary> The GroupId property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupIdProperty);}
            set { SetValue(GroupIdProperty,value); }
        }

        /// <summary> The DefaultSearchViewId property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DefaultSearchViewId
        {
            get { return  GetValue<Nullable<System.Int32>>( DefaultSearchViewIdProperty);}
            set { SetValue(DefaultSearchViewIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSearchSaved</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSearchSaved</summary>
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

