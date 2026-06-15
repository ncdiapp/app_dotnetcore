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
    /// DTO class for the entity 'AppCurrentUserFavouriteFolderOrFile'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCurrentUserFavouriteFolderOrFileDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CurrentUserIdProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.Int32>>(o => o.CurrentUserId);
        public static readonly string FiledIdProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.Int32>>(o => o.FiledId);
        public static readonly string FolderIdProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.Int32>>(o => o.FolderId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCurrentUserFavouriteFolderOrFileDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCurrentUserFavouriteFolderOrFileDto()
        {        
        }
		
		static AppCurrentUserFavouriteFolderOrFileDto()
        {
                     
			  
			ForeignKeyProperties.Add(CurrentUserIdProperty);  
			ForeignKeyProperties.Add(FiledIdProperty);  
			ForeignKeyProperties.Add(FolderIdProperty);      		
                       
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
    


        /// <summary> The CurrentUserId property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CurrentUserId
        {
            get { return  GetValue<Nullable<System.Int32>>( CurrentUserIdProperty);}
            set { SetValue(CurrentUserIdProperty,value); }
        }

        /// <summary> The FiledId property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FiledId
        {
            get { return  GetValue<Nullable<System.Int32>>( FiledIdProperty);}
            set { SetValue(FiledIdProperty,value); }
        }

        /// <summary> The FolderId property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FolderId
        {
            get { return  GetValue<Nullable<System.Int32>>( FolderIdProperty);}
            set { SetValue(FolderIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCurrentUserFavouriteFolderOrFile</summary>
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

