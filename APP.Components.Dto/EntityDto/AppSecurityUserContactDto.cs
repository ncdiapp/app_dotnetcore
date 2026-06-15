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
    /// DTO class for the entity 'AppSecurityUserContact'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityUserContactDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.Int32>>(o => o.UserId);
        public static readonly string ContactTypeProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.Int32>>(o => o.ContactType);
        public static readonly string ContactFormatProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,System.String>(o => o.ContactFormat);
        public static readonly string AdditionalContactInfoProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,System.String>(o => o.AdditionalContactInfo);
        public static readonly string CommentsProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,System.String>(o => o.Comments);
        public static readonly string IsForwardMessageToThisContactProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.Boolean>>(o => o.IsForwardMessageToThisContact);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityUserContactDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityUserContactDto()
        {        
        }
		
		static AppSecurityUserContactDto()
        {
                        
			  
			ForeignKeyProperties.Add(UserIdProperty);           		
                
			DictStringPropertyMaxLength.Add(ContactFormatProperty,200); 
			DictStringPropertyMaxLength.Add(AdditionalContactInfoProperty,300); 
			DictStringPropertyMaxLength.Add(CommentsProperty,500);        
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
    


        /// <summary> The UserId property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The ContactType property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ContactType
        {
            get { return  GetValue<Nullable<System.Int32>>( ContactTypeProperty);}
            set { SetValue(ContactTypeProperty,value); }
        }

        /// <summary> The ContactFormat property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ContactFormat
        {
            get { return  GetValue<System.String>( ContactFormatProperty);}
            set { SetValue(ContactFormatProperty,value); }
        }

        /// <summary> The AdditionalContactInfo property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AdditionalContactInfo
        {
            get { return  GetValue<System.String>( AdditionalContactInfoProperty);}
            set { SetValue(AdditionalContactInfoProperty,value); }
        }

        /// <summary> The Comments property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Comments
        {
            get { return  GetValue<System.String>( CommentsProperty);}
            set { SetValue(CommentsProperty,value); }
        }

        /// <summary> The IsForwardMessageToThisContact property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsForwardMessageToThisContact
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsForwardMessageToThisContactProperty);}
            set { SetValue(IsForwardMessageToThisContactProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityUserContact</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityUserContact</summary>
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

