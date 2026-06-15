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
    /// DTO class for the entity 'AppSecurityGroup'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityGroupDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string GroupNameProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,System.String>(o => o.GroupName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,System.String>(o => o.Description);
        public static readonly string LoginEventProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,System.String>(o => o.LoginEvent);
        public static readonly string InternalCodeProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,System.String>(o => o.InternalCode);
        public static readonly string IsBuiltInProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Boolean>>(o => o.IsBuiltIn);
        public static readonly string OrganizationIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.OrganizationId);
        public static readonly string GroupUsageProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.GroupUsage);
        public static readonly string IsSharedbyMutipleCompanyProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Boolean>>(o => o.IsSharedbyMutipleCompany);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string DefaultDesktopIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.DefaultDesktopId);
        public static readonly string RoleUserTypeIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.RoleUserTypeId);
        public static readonly string BusinessPartnerIdProperty = ObjectInfoHelper.GetName<AppSecurityGroupDto ,Nullable<System.Int32>>(o => o.BusinessPartnerId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityGroupDto()
        {        
        }
		
		static AppSecurityGroupDto()
        {
              
			MandatoryProperties.Add(GroupNameProperty);                
			       
			ForeignKeyProperties.Add(OrganizationIdProperty);        
			ForeignKeyProperties.Add(AppCreatedByCompanyIdProperty);    		
              
			DictStringPropertyMaxLength.Add(GroupNameProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,250); 
			DictStringPropertyMaxLength.Add(LoginEventProperty,50); 
			DictStringPropertyMaxLength.Add(InternalCodeProperty,80);              
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
    


        /// <summary> The GroupName property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String GroupName
        {
            get { return  GetValue<System.String>( GroupNameProperty);}
            set { SetValue(GroupNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The LoginEvent property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String LoginEvent
        {
            get { return  GetValue<System.String>( LoginEventProperty);}
            set { SetValue(LoginEventProperty,value); }
        }

        /// <summary> The InternalCode property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InternalCode
        {
            get { return  GetValue<System.String>( InternalCodeProperty);}
            set { SetValue(InternalCodeProperty,value); }
        }

        /// <summary> The IsBuiltIn property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsBuiltIn
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsBuiltInProperty);}
            set { SetValue(IsBuiltInProperty,value); }
        }

        /// <summary> The OrganizationId property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrganizationId
        {
            get { return  GetValue<Nullable<System.Int32>>( OrganizationIdProperty);}
            set { SetValue(OrganizationIdProperty,value); }
        }

        /// <summary> The GroupUsage property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> GroupUsage
        {
            get { return  GetValue<Nullable<System.Int32>>( GroupUsageProperty);}
            set { SetValue(GroupUsageProperty,value); }
        }

        /// <summary> The IsSharedbyMutipleCompany property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSharedbyMutipleCompany
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSharedbyMutipleCompanyProperty);}
            set { SetValue(IsSharedbyMutipleCompanyProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The DefaultDesktopId property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DefaultDesktopId
        {
            get { return  GetValue<Nullable<System.Int32>>( DefaultDesktopIdProperty);}
            set { SetValue(DefaultDesktopIdProperty,value); }
        }

        /// <summary> The RoleUserTypeId property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RoleUserTypeId
        {
            get { return  GetValue<Nullable<System.Int32>>( RoleUserTypeIdProperty);}
            set { SetValue(RoleUserTypeIdProperty,value); }
        }

        /// <summary> The BusinessPartnerId property of the Entity AppSecurityGroup</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> BusinessPartnerId
        {
            get { return  GetValue<Nullable<System.Int32>>( BusinessPartnerIdProperty);}
            set { SetValue(BusinessPartnerIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

