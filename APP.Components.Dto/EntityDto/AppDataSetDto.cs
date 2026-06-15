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
    /// DTO class for the entity 'AppDataSet'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDataSetDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataSourceFromProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.DataSourceFrom);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.Description);
        public static readonly string BaseDataSetIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.BaseDataSetId);
        public static readonly string QueryTextProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.QueryText);
        public static readonly string QueryTypeProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.QueryType);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string UsageTypeIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.UsageTypeId);
        public static readonly string BaseTableNameProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.BaseTableName);
        public static readonly string WebApiConfigIdProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.WebApiConfigId);
        public static readonly string RestApiHeaderKeyValueProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.RestApiHeaderKeyValue);
        public static readonly string RestApiQueryParameterKeyValueProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.RestApiQueryParameterKeyValue);
        public static readonly string HttpMethodProperty = ObjectInfoHelper.GetName<AppDataSetDto ,Nullable<System.Int32>>(o => o.HttpMethod);
        public static readonly string OtherSettingsProperty = ObjectInfoHelper.GetName<AppDataSetDto ,System.String>(o => o.OtherSettings);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppDataSetDto()
        {        
        }
		
		static AppDataSetDto()
        {
                                
			  
			ForeignKeyProperties.Add(DataSourceFromProperty);    
			ForeignKeyProperties.Add(BaseDataSetIdProperty);         
			ForeignKeyProperties.Add(SaasApplicationIdProperty);    
			ForeignKeyProperties.Add(WebApiConfigIdProperty);     		
               
			DictStringPropertyMaxLength.Add(NameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);  
			DictStringPropertyMaxLength.Add(QueryTextProperty,2147483647);         
			DictStringPropertyMaxLength.Add(BaseTableNameProperty,200);  
			DictStringPropertyMaxLength.Add(RestApiHeaderKeyValueProperty,4000); 
			DictStringPropertyMaxLength.Add(RestApiQueryParameterKeyValueProperty,4000);  
			DictStringPropertyMaxLength.Add(OtherSettingsProperty,2147483647);  
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
    


        /// <summary> The DataSourceFrom property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceFrom
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceFromProperty);}
            set { SetValue(DataSourceFromProperty,value); }
        }

        /// <summary> The Name property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The BaseDataSetId property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> BaseDataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( BaseDataSetIdProperty);}
            set { SetValue(BaseDataSetIdProperty,value); }
        }

        /// <summary> The QueryText property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String QueryText
        {
            get { return  GetValue<System.String>( QueryTextProperty);}
            set { SetValue(QueryTextProperty,value); }
        }

        /// <summary> The QueryType property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> QueryType
        {
            get { return  GetValue<Nullable<System.Int32>>( QueryTypeProperty);}
            set { SetValue(QueryTypeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The UsageTypeId property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UsageTypeId
        {
            get { return  GetValue<Nullable<System.Int32>>( UsageTypeIdProperty);}
            set { SetValue(UsageTypeIdProperty,value); }
        }

        /// <summary> The BaseTableName property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BaseTableName
        {
            get { return  GetValue<System.String>( BaseTableNameProperty);}
            set { SetValue(BaseTableNameProperty,value); }
        }

        /// <summary> The WebApiConfigId property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WebApiConfigId
        {
            get { return  GetValue<Nullable<System.Int32>>( WebApiConfigIdProperty);}
            set { SetValue(WebApiConfigIdProperty,value); }
        }

        /// <summary> The RestApiHeaderKeyValue property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RestApiHeaderKeyValue
        {
            get { return  GetValue<System.String>( RestApiHeaderKeyValueProperty);}
            set { SetValue(RestApiHeaderKeyValueProperty,value); }
        }

        /// <summary> The RestApiQueryParameterKeyValue property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RestApiQueryParameterKeyValue
        {
            get { return  GetValue<System.String>( RestApiQueryParameterKeyValueProperty);}
            set { SetValue(RestApiQueryParameterKeyValueProperty,value); }
        }

        /// <summary> The HttpMethod property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> HttpMethod
        {
            get { return  GetValue<Nullable<System.Int32>>( HttpMethodProperty);}
            set { SetValue(HttpMethodProperty,value); }
        }

        /// <summary> The OtherSettings property of the Entity AppDataSet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OtherSettings
        {
            get { return  GetValue<System.String>( OtherSettingsProperty);}
            set { SetValue(OtherSettingsProperty,value); }
        }
        
        #endregion

       
        
    }
}

