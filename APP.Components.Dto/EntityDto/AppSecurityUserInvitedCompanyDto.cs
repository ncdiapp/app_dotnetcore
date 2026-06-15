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
    /// DTO class for the entity 'AppSecurityUserInvitedCompany'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityUserInvitedCompanyDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  
        public static readonly string UserInviteCompnayIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitedCompanyDto ,Nullable<System.Int32>>(o => o.UserInviteCompnayId);
        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitedCompanyDto ,Nullable<System.Int32>>(o => o.UserId);
        public static readonly string InviteCompnayIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitedCompanyDto ,Nullable<System.Int32>>(o => o.InviteCompnayId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitedCompanyDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityUserInvitedCompanyDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityUserInvitedCompanyDto()
        {        
        }
		
		static AppSecurityUserInvitedCompanyDto()
        {
                 
			  
			ForeignKeyProperties.Add(UserIdProperty);  
			ForeignKeyProperties.Add(InviteCompnayIdProperty);   		
                   
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
    

        /// <summary> The UserInviteCompnayId property of the Entity AppSecurityUserInvitedCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserInviteCompnayId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserInviteCompnayIdProperty);}
            set { SetValue(UserInviteCompnayIdProperty,value); }
        }

        /// <summary> The UserId property of the Entity AppSecurityUserInvitedCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The InviteCompnayId property of the Entity AppSecurityUserInvitedCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> InviteCompnayId
        {
            get { return  GetValue<Nullable<System.Int32>>( InviteCompnayIdProperty);}
            set { SetValue(InviteCompnayIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityUserInvitedCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityUserInvitedCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }
        
        #endregion

       
        
    }
}

