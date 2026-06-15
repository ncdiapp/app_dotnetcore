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
    /// DTO class for the entity 'AppCompany'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCompanyDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CodeProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Code);
        public static readonly string ShortNameProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.ShortName);
        public static readonly string FullNameProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.FullName);
        public static readonly string RegistrationNumberProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.RegistrationNumber);
        public static readonly string ValueAddedTaxIdProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.ValueAddedTaxId);
        public static readonly string Adress1Property = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Adress1);
        public static readonly string Adress2Property = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Adress2);
        public static readonly string Adress3Property = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Adress3);
        public static readonly string CityProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.City);
        public static readonly string LanguageProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Language);
        public static readonly string StateProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.State);
        public static readonly string PostCodeProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.PostCode);
        public static readonly string CountryProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Country);
        public static readonly string StatusProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.Status);
        public static readonly string CurrencyCodeProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.CurrencyCode);
        public static readonly string ContactPhoneProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.ContactPhone);
        public static readonly string ContactNameProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.ContactName);
        public static readonly string ContactFaxProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.ContactFax);
        public static readonly string ParentCompayIdProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.Int32>>(o => o.ParentCompayId);
        public static readonly string EmApplicationVersionEditionProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.Int32>>(o => o.EmApplicationVersionEdition);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string GlobalGuidProperty = ObjectInfoHelper.GetName<AppCompanyDto ,Nullable<System.Guid>>(o => o.GlobalGuid);
        public static readonly string CompanyDomainIdentityTokenProperty = ObjectInfoHelper.GetName<AppCompanyDto ,System.String>(o => o.CompanyDomainIdentityToken);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCompanyDto()
        {        
        }
		
		static AppCompanyDto()
        {
                                        
			                    
			ForeignKeyProperties.Add(ParentCompayIdProperty);         		
              
			DictStringPropertyMaxLength.Add(CodeProperty,50); 
			DictStringPropertyMaxLength.Add(ShortNameProperty,200); 
			DictStringPropertyMaxLength.Add(FullNameProperty,500); 
			DictStringPropertyMaxLength.Add(RegistrationNumberProperty,50); 
			DictStringPropertyMaxLength.Add(ValueAddedTaxIdProperty,50); 
			DictStringPropertyMaxLength.Add(Adress1Property,200); 
			DictStringPropertyMaxLength.Add(Adress2Property,200); 
			DictStringPropertyMaxLength.Add(Adress3Property,200); 
			DictStringPropertyMaxLength.Add(CityProperty,50); 
			DictStringPropertyMaxLength.Add(LanguageProperty,50); 
			DictStringPropertyMaxLength.Add(StateProperty,50); 
			DictStringPropertyMaxLength.Add(PostCodeProperty,20); 
			DictStringPropertyMaxLength.Add(CountryProperty,20); 
			DictStringPropertyMaxLength.Add(StatusProperty,10); 
			DictStringPropertyMaxLength.Add(CurrencyCodeProperty,10); 
			DictStringPropertyMaxLength.Add(ContactPhoneProperty,20); 
			DictStringPropertyMaxLength.Add(ContactNameProperty,20); 
			DictStringPropertyMaxLength.Add(ContactFaxProperty,20);         
			DictStringPropertyMaxLength.Add(CompanyDomainIdentityTokenProperty,500);  
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
    


        /// <summary> The Code property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Code
        {
            get { return  GetValue<System.String>( CodeProperty);}
            set { SetValue(CodeProperty,value); }
        }

        /// <summary> The ShortName property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ShortName
        {
            get { return  GetValue<System.String>( ShortNameProperty);}
            set { SetValue(ShortNameProperty,value); }
        }

        /// <summary> The FullName property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FullName
        {
            get { return  GetValue<System.String>( FullNameProperty);}
            set { SetValue(FullNameProperty,value); }
        }

        /// <summary> The RegistrationNumber property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RegistrationNumber
        {
            get { return  GetValue<System.String>( RegistrationNumberProperty);}
            set { SetValue(RegistrationNumberProperty,value); }
        }

        /// <summary> The ValueAddedTaxId property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ValueAddedTaxId
        {
            get { return  GetValue<System.String>( ValueAddedTaxIdProperty);}
            set { SetValue(ValueAddedTaxIdProperty,value); }
        }

        /// <summary> The Adress1 property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Adress1
        {
            get { return  GetValue<System.String>( Adress1Property);}
            set { SetValue(Adress1Property,value); }
        }

        /// <summary> The Adress2 property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Adress2
        {
            get { return  GetValue<System.String>( Adress2Property);}
            set { SetValue(Adress2Property,value); }
        }

        /// <summary> The Adress3 property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Adress3
        {
            get { return  GetValue<System.String>( Adress3Property);}
            set { SetValue(Adress3Property,value); }
        }

        /// <summary> The City property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String City
        {
            get { return  GetValue<System.String>( CityProperty);}
            set { SetValue(CityProperty,value); }
        }

        /// <summary> The Language property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Language
        {
            get { return  GetValue<System.String>( LanguageProperty);}
            set { SetValue(LanguageProperty,value); }
        }

        /// <summary> The State property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String State
        {
            get { return  GetValue<System.String>( StateProperty);}
            set { SetValue(StateProperty,value); }
        }

        /// <summary> The PostCode property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PostCode
        {
            get { return  GetValue<System.String>( PostCodeProperty);}
            set { SetValue(PostCodeProperty,value); }
        }

        /// <summary> The Country property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Country
        {
            get { return  GetValue<System.String>( CountryProperty);}
            set { SetValue(CountryProperty,value); }
        }

        /// <summary> The Status property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Status
        {
            get { return  GetValue<System.String>( StatusProperty);}
            set { SetValue(StatusProperty,value); }
        }

        /// <summary> The CurrencyCode property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CurrencyCode
        {
            get { return  GetValue<System.String>( CurrencyCodeProperty);}
            set { SetValue(CurrencyCodeProperty,value); }
        }

        /// <summary> The ContactPhone property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ContactPhone
        {
            get { return  GetValue<System.String>( ContactPhoneProperty);}
            set { SetValue(ContactPhoneProperty,value); }
        }

        /// <summary> The ContactName property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ContactName
        {
            get { return  GetValue<System.String>( ContactNameProperty);}
            set { SetValue(ContactNameProperty,value); }
        }

        /// <summary> The ContactFax property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ContactFax
        {
            get { return  GetValue<System.String>( ContactFaxProperty);}
            set { SetValue(ContactFaxProperty,value); }
        }

        /// <summary> The ParentCompayId property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentCompayId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentCompayIdProperty);}
            set { SetValue(ParentCompayIdProperty,value); }
        }

        /// <summary> The EmApplicationVersionEdition property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmApplicationVersionEdition
        {
            get { return  GetValue<Nullable<System.Int32>>( EmApplicationVersionEditionProperty);}
            set { SetValue(EmApplicationVersionEditionProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The GlobalGuid property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> GlobalGuid
        {
            get { return  GetValue<Nullable<System.Guid>>( GlobalGuidProperty);}
            set { SetValue(GlobalGuidProperty,value); }
        }

        /// <summary> The CompanyDomainIdentityToken property of the Entity AppCompany</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CompanyDomainIdentityToken
        {
            get { return  GetValue<System.String>( CompanyDomainIdentityTokenProperty);}
            set { SetValue(CompanyDomainIdentityTokenProperty,value); }
        }
        
        #endregion

       
        
    }
}

