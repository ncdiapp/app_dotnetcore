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
    /// DTO class for the entity 'AppCompanyUserTypeRegister'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCompanyUserTypeRegisterDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CompanyIdProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.CompanyId);
        public static readonly string UserTypeProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.UserType);
        public static readonly string MappingToEntityIdProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.MappingToEntityId);
        public static readonly string AppCompanyIdProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.AppCompanyId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCompanyUserTypeRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCompanyUserTypeRegisterDto()
        {        
        }
		
		static AppCompanyUserTypeRegisterDto()
        {
                      
			  
			ForeignKeyProperties.Add(CompanyIdProperty);         		
                        
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
    


        /// <summary> The CompanyId property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( CompanyIdProperty);}
            set { SetValue(CompanyIdProperty,value); }
        }

        /// <summary> The UserType property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserType
        {
            get { return  GetValue<Nullable<System.Int32>>( UserTypeProperty);}
            set { SetValue(UserTypeProperty,value); }
        }

        /// <summary> The MappingToEntityId property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MappingToEntityId
        {
            get { return  GetValue<Nullable<System.Int32>>( MappingToEntityIdProperty);}
            set { SetValue(MappingToEntityIdProperty,value); }
        }

        /// <summary> The AppCompanyId property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCompanyIdProperty);}
            set { SetValue(AppCompanyIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCompanyUserTypeRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCompanyUserTypeRegister</summary>
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

