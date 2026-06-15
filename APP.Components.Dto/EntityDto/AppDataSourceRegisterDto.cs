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
    /// DTO class for the entity 'AppDataSourceRegister'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDataSourceRegisterDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataSourceNameProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,System.String>(o => o.DataSourceName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,System.String>(o => o.Description);
        public static readonly string DataSourceTypeProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.Int32>>(o => o.DataSourceType);
        public static readonly string ConnectionStringProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,System.String>(o => o.ConnectionString);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string DataSourceOwnerCompanyIdProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.Int32>>(o => o.DataSourceOwnerCompanyId);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string DatabaseNameProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,System.String>(o => o.DatabaseName);
        public static readonly string IsCompanyMasterDbProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,Nullable<System.Boolean>>(o => o.IsCompanyMasterDb);
        public static readonly string CustomDomainProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,System.String>(o => o.CustomDomain);
        public static readonly string DomainTokenProperty = ObjectInfoHelper.GetName<AppDataSourceRegisterDto ,System.String>(o => o.DomainToken);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppDataSourceRegisterDto()
        {        
        }
		
		static AppDataSourceRegisterDto()
        {
                           
			          
			ForeignKeyProperties.Add(DataSourceOwnerCompanyIdProperty);      		
              
			DictStringPropertyMaxLength.Add(DataSourceNameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,300);  
			DictStringPropertyMaxLength.Add(ConnectionStringProperty,1000);       
			DictStringPropertyMaxLength.Add(DatabaseNameProperty,100);  
			DictStringPropertyMaxLength.Add(CustomDomainProperty,255); 
			DictStringPropertyMaxLength.Add(DomainTokenProperty,100);  
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
    


        /// <summary> The DataSourceName property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataSourceName
        {
            get { return  GetValue<System.String>( DataSourceNameProperty);}
            set { SetValue(DataSourceNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The DataSourceType property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceTypeProperty);}
            set { SetValue(DataSourceTypeProperty,value); }
        }

        /// <summary> The ConnectionString property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ConnectionString
        {
            get { return  GetValue<System.String>( ConnectionStringProperty);}
            set { SetValue(ConnectionStringProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The DataSourceOwnerCompanyId property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceOwnerCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceOwnerCompanyIdProperty);}
            set { SetValue(DataSourceOwnerCompanyIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The DatabaseName property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DatabaseName
        {
            get { return  GetValue<System.String>( DatabaseNameProperty);}
            set { SetValue(DatabaseNameProperty,value); }
        }

        /// <summary> The IsCompanyMasterDb property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsCompanyMasterDb
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsCompanyMasterDbProperty);}
            set { SetValue(IsCompanyMasterDbProperty,value); }
        }

        /// <summary> The CustomDomain property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CustomDomain
        {
            get { return  GetValue<System.String>( CustomDomainProperty);}
            set { SetValue(CustomDomainProperty,value); }
        }

        /// <summary> The DomainToken property of the Entity AppDataSourceRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DomainToken
        {
            get { return  GetValue<System.String>( DomainTokenProperty);}
            set { SetValue(DomainTokenProperty,value); }
        }
        
        #endregion

       
        
    }
}

