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
    /// DTO class for the entity 'AppSecurityUserInvitation'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityUserInvitationDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string InvitingUserIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.InvitingUserId);
        public static readonly string InvitingOrgIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.InvitingOrgId);
        public static readonly string InvitingBusinessPartnerIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.InvitingBusinessPartnerId);
        public static readonly string InvitedUserIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.InvitedUserId);
        public static readonly string InvitedUserEmailProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,System.String>(o => o.InvitedUserEmail);
        public static readonly string EmUserTypeProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.EmUserType);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitationDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityUserInvitationDto()
        {        
        }
		
		static AppSecurityUserInvitationDto()
        {
                        
			  
			ForeignKeyProperties.Add(InvitingUserIdProperty);           		
                  
			DictStringPropertyMaxLength.Add(InvitedUserEmailProperty,200);        
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
    


        /// <summary> The InvitingUserId property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InvitingUserId
        {
            get { return  GetValue<Nullable<System.Int32>>( InvitingUserIdProperty);}
            set { SetValue(InvitingUserIdProperty,value); }
        }

        /// <summary> The InvitingOrgId property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InvitingOrgId
        {
            get { return  GetValue<Nullable<System.Int32>>( InvitingOrgIdProperty);}
            set { SetValue(InvitingOrgIdProperty,value); }
        }

        /// <summary> The InvitingBusinessPartnerId property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InvitingBusinessPartnerId
        {
            get { return  GetValue<Nullable<System.Int32>>( InvitingBusinessPartnerIdProperty);}
            set { SetValue(InvitingBusinessPartnerIdProperty,value); }
        }

        /// <summary> The InvitedUserId property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InvitedUserId
        {
            get { return  GetValue<Nullable<System.Int32>>( InvitedUserIdProperty);}
            set { SetValue(InvitedUserIdProperty,value); }
        }

        /// <summary> The InvitedUserEmail property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InvitedUserEmail
        {
            get { return  GetValue<System.String>( InvitedUserEmailProperty);}
            set { SetValue(InvitedUserEmailProperty,value); }
        }

        /// <summary> The EmUserType property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmUserType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmUserTypeProperty);}
            set { SetValue(EmUserTypeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityUserInvitation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityUserInvitation</summary>
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

