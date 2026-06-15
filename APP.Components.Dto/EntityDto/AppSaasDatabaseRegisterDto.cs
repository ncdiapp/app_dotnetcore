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
    /// DTO class for the entity 'AppSaasDatabaseRegister'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppSaasDatabaseRegisterDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  
        public static readonly string DataBaseRegisterIdProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.Int32>>(o => o.DataBaseRegisterId);
        public static readonly string DataSourceNameProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,System.String>(o => o.DataSourceName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,System.String>(o => o.Description);
        public static readonly string DataSourceTypeProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.Int32>>(o => o.DataSourceType);
        public static readonly string ConnectionStringProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,System.String>(o => o.ConnectionString);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string DedicatedCompnayIdProperty = ObjectInfoHelper.GetName<AppSaasDatabaseRegisterDto ,Nullable<System.Int32>>(o => o.DedicatedCompnayId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppSaasDatabaseRegisterDto()
        {        
        }
		
		static AppSaasDatabaseRegisterDto()
        {
                       
			           
			ForeignKeyProperties.Add(DedicatedCompnayIdProperty); 		
              
			DictStringPropertyMaxLength.Add(DataSourceNameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,300);  
			DictStringPropertyMaxLength.Add(ConnectionStringProperty,1000);        
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
    

        /// <summary> The DataBaseRegisterId property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataBaseRegisterId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataBaseRegisterIdProperty);}
            set { SetValue(DataBaseRegisterIdProperty,value); }
        }

        /// <summary> The DataSourceName property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataSourceName
        {
            get { return  GetValue<System.String>( DataSourceNameProperty);}
            set { SetValue(DataSourceNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The DataSourceType property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceType
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceTypeProperty);}
            set { SetValue(DataSourceTypeProperty,value); }
        }

        /// <summary> The ConnectionString property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ConnectionString
        {
            get { return  GetValue<System.String>( ConnectionStringProperty);}
            set { SetValue(ConnectionStringProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The DedicatedCompnayId property of the Entity AppSaasDatabaseRegister</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DedicatedCompnayId
        {
            get { return  GetValue<Nullable<System.Int32>>( DedicatedCompnayIdProperty);}
            set { SetValue(DedicatedCompnayIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

