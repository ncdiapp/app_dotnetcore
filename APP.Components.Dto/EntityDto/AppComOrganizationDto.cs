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
    /// DTO class for the entity 'AppComOrganization'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppComOrganizationDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string AppCompanyIdProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.AppCompanyId);
        public static readonly string CodeProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.Code);
        public static readonly string ShortNameProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.ShortName);
        public static readonly string FullNameProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.FullName);
        public static readonly string ClassificationLevelProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.ClassificationLevel);
        public static readonly string ContactPersonProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.ContactPerson);
        public static readonly string TelphoneProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.Telphone);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string MemoProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.Memo);
        public static readonly string BelongToIdProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.BelongToId);
        public static readonly string AdressProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,System.String>(o => o.Adress);
        public static readonly string LeaderIdProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.LeaderId);
        public static readonly string UserTypeEmProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.UserTypeEm);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppComOrganizationDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppComOrganizationDto()
        {        
        }
		
		static AppComOrganizationDto()
        {
                               
			  
			ForeignKeyProperties.Add(AppCompanyIdProperty);                  		
               
			DictStringPropertyMaxLength.Add(CodeProperty,50); 
			DictStringPropertyMaxLength.Add(ShortNameProperty,100); 
			DictStringPropertyMaxLength.Add(FullNameProperty,200);  
			DictStringPropertyMaxLength.Add(ContactPersonProperty,50); 
			DictStringPropertyMaxLength.Add(TelphoneProperty,20);  
			DictStringPropertyMaxLength.Add(MemoProperty,500);  
			DictStringPropertyMaxLength.Add(AdressProperty,500);         
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
    


        /// <summary> The AppCompanyId property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCompanyIdProperty);}
            set { SetValue(AppCompanyIdProperty,value); }
        }

        /// <summary> The Code property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Code
        {
            get { return  GetValue<System.String>( CodeProperty);}
            set { SetValue(CodeProperty,value); }
        }

        /// <summary> The ShortName property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ShortName
        {
            get { return  GetValue<System.String>( ShortNameProperty);}
            set { SetValue(ShortNameProperty,value); }
        }

        /// <summary> The FullName property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FullName
        {
            get { return  GetValue<System.String>( FullNameProperty);}
            set { SetValue(FullNameProperty,value); }
        }

        /// <summary> The ClassificationLevel property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ClassificationLevel
        {
            get { return  GetValue<Nullable<System.Int32>>( ClassificationLevelProperty);}
            set { SetValue(ClassificationLevelProperty,value); }
        }

        /// <summary> The ContactPerson property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ContactPerson
        {
            get { return  GetValue<System.String>( ContactPersonProperty);}
            set { SetValue(ContactPersonProperty,value); }
        }

        /// <summary> The Telphone property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Telphone
        {
            get { return  GetValue<System.String>( TelphoneProperty);}
            set { SetValue(TelphoneProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The Memo property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Memo
        {
            get { return  GetValue<System.String>( MemoProperty);}
            set { SetValue(MemoProperty,value); }
        }

        /// <summary> The BelongToId property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> BelongToId
        {
            get { return  GetValue<Nullable<System.Int32>>( BelongToIdProperty);}
            set { SetValue(BelongToIdProperty,value); }
        }

        /// <summary> The Adress property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Adress
        {
            get { return  GetValue<System.String>( AdressProperty);}
            set { SetValue(AdressProperty,value); }
        }

        /// <summary> The LeaderId property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> LeaderId
        {
            get { return  GetValue<Nullable<System.Int32>>( LeaderIdProperty);}
            set { SetValue(LeaderIdProperty,value); }
        }

        /// <summary> The UserTypeEm property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserTypeEm
        {
            get { return  GetValue<Nullable<System.Int32>>( UserTypeEmProperty);}
            set { SetValue(UserTypeEmProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppComOrganization</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppComOrganization</summary>
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

