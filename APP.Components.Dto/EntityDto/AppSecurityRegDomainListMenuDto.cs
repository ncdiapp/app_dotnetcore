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
    /// DTO class for the entity 'AppSecurityRegDomainListMenu'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSecurityRegDomainListMenuDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string MenuIdProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,System.Int32>(o => o.MenuId);
        public static readonly string DomainIdProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.Int32>>(o => o.DomainId);
        public static readonly string OrganizationIdProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.Int32>>(o => o.OrganizationId);
        public static readonly string SystemTimeStampProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,System.Byte[]>(o => o.SystemTimeStamp);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppSecurityRegDomainListMenuDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSecurityRegDomainListMenuDto()
        {        
        }
		
		static AppSecurityRegDomainListMenuDto()
        {
              
			MandatoryProperties.Add(MenuIdProperty);         
			  
			ForeignKeyProperties.Add(MenuIdProperty);  
			ForeignKeyProperties.Add(DomainIdProperty);  
			ForeignKeyProperties.Add(OrganizationIdProperty);       		
                        
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
    


        /// <summary> The MenuId property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 MenuId
        {
            get { return  GetValue<System.Int32>( MenuIdProperty);}
            set { SetValue(MenuIdProperty,value); }
        }

        /// <summary> The DomainId property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DomainId
        {
            get { return  GetValue<Nullable<System.Int32>>( DomainIdProperty);}
            set { SetValue(DomainIdProperty,value); }
        }

        /// <summary> The OrganizationId property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OrganizationId
        {
            get { return  GetValue<Nullable<System.Int32>>( OrganizationIdProperty);}
            set { SetValue(OrganizationIdProperty,value); }
        }

        /// <summary> The SystemTimeStamp property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Byte[] SystemTimeStamp
        {
            get { return  GetValue<System.Byte[]>( SystemTimeStampProperty);}
            set { SetValue(SystemTimeStampProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSecurityRegDomainListMenu</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppSecurityRegDomainListMenu</summary>
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

