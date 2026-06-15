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
    /// DTO class for the entity 'AppSecurityLoginAuditor'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityLoginAuditorDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string LoginNameProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,System.String>(o => o.LoginName);
        public static readonly string PasswordProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,System.String>(o => o.Password);
        public static readonly string HostAddressProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,System.String>(o => o.HostAddress);
        public static readonly string StateProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,System.String>(o => o.State);
        public static readonly string LoginTimeProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,Nullable<System.DateTime>>(o => o.LoginTime);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityLoginAuditorDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityLoginAuditorDto()
        {        
        }
		
		static AppSecurityLoginAuditorDto()
        {
                       
			           		
              
			DictStringPropertyMaxLength.Add(LoginNameProperty,100); 
			DictStringPropertyMaxLength.Add(PasswordProperty,100); 
			DictStringPropertyMaxLength.Add(HostAddressProperty,200); 
			DictStringPropertyMaxLength.Add(StateProperty,50);        
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
    


        /// <summary> The LoginName property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LoginName
        {
            get { return  GetValue<System.String>( LoginNameProperty);}
            set { SetValue(LoginNameProperty,value); }
        }

        /// <summary> The Password property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Password
        {
            get { return  GetValue<System.String>( PasswordProperty);}
            set { SetValue(PasswordProperty,value); }
        }

        /// <summary> The HostAddress property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String HostAddress
        {
            get { return  GetValue<System.String>( HostAddressProperty);}
            set { SetValue(HostAddressProperty,value); }
        }

        /// <summary> The State property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String State
        {
            get { return  GetValue<System.String>( StateProperty);}
            set { SetValue(StateProperty,value); }
        }

        /// <summary> The LoginTime property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> LoginTime
        {
            get { return  GetValue<Nullable<System.DateTime>>( LoginTimeProperty);}
            set { SetValue(LoginTimeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityLoginAuditor</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityLoginAuditor</summary>
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

