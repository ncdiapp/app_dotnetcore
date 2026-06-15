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
    /// DTO class for the entity 'AppCountry'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCountryDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppCountryDto ,System.String>(o => o.Name);
        public static readonly string DescriptoinProperty = ObjectInfoHelper.GetName<AppCountryDto ,System.String>(o => o.Descriptoin);
        public static readonly string Descriptoin2Property = ObjectInfoHelper.GetName<AppCountryDto ,System.String>(o => o.Descriptoin2);
        public static readonly string Alpha2CodeProperty = ObjectInfoHelper.GetName<AppCountryDto ,System.String>(o => o.Alpha2Code);
        public static readonly string Alpha3CodeProperty = ObjectInfoHelper.GetName<AppCountryDto ,System.String>(o => o.Alpha3Code);
        public static readonly string NumericCodeProperty = ObjectInfoHelper.GetName<AppCountryDto ,System.String>(o => o.NumericCode);
        public static readonly string CurrencyIdProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.Int32>>(o => o.CurrencyId);
        public static readonly string FlagImageIdProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.Int32>>(o => o.FlagImageId);
        public static readonly string ContinentIdProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.Int32>>(o => o.ContinentId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCountryDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCountryDto()
        {        
        }
		
		static AppCountryDto()
        {
                           
			               		
              
			DictStringPropertyMaxLength.Add(NameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptoinProperty,500); 
			DictStringPropertyMaxLength.Add(Descriptoin2Property,500); 
			DictStringPropertyMaxLength.Add(Alpha2CodeProperty,10); 
			DictStringPropertyMaxLength.Add(Alpha3CodeProperty,10); 
			DictStringPropertyMaxLength.Add(NumericCodeProperty,10);          
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
    


        /// <summary> The Name property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Descriptoin property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Descriptoin
        {
            get { return  GetValue<System.String>( DescriptoinProperty);}
            set { SetValue(DescriptoinProperty,value); }
        }

        /// <summary> The Descriptoin2 property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Descriptoin2
        {
            get { return  GetValue<System.String>( Descriptoin2Property);}
            set { SetValue(Descriptoin2Property,value); }
        }

        /// <summary> The Alpha2Code property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Alpha2Code
        {
            get { return  GetValue<System.String>( Alpha2CodeProperty);}
            set { SetValue(Alpha2CodeProperty,value); }
        }

        /// <summary> The Alpha3Code property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Alpha3Code
        {
            get { return  GetValue<System.String>( Alpha3CodeProperty);}
            set { SetValue(Alpha3CodeProperty,value); }
        }

        /// <summary> The NumericCode property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NumericCode
        {
            get { return  GetValue<System.String>( NumericCodeProperty);}
            set { SetValue(NumericCodeProperty,value); }
        }

        /// <summary> The CurrencyId property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CurrencyId
        {
            get { return  GetValue<Nullable<System.Int32>>( CurrencyIdProperty);}
            set { SetValue(CurrencyIdProperty,value); }
        }

        /// <summary> The FlagImageId property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> FlagImageId
        {
            get { return  GetValue<Nullable<System.Int32>>( FlagImageIdProperty);}
            set { SetValue(FlagImageIdProperty,value); }
        }

        /// <summary> The ContinentId property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ContinentId
        {
            get { return  GetValue<Nullable<System.Int32>>( ContinentIdProperty);}
            set { SetValue(ContinentIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCountry</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCountry</summary>
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

