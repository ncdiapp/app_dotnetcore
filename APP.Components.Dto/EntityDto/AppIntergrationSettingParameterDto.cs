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
    /// DTO class for the entity 'AppIntergrationSettingParameter'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppIntergrationSettingParameterDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string IntergrationSettingIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.IntergrationSettingId);
        public static readonly string MappingInternalCodeProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.MappingInternalCode);
        public static readonly string InternalFiledNameProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.InternalFiledName);
        public static readonly string ExternalFieldNameProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.ExternalFieldName);
        public static readonly string TranscationIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.TranscationId);
        public static readonly string TranscationFieIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.TranscationFieId);
        public static readonly string DefaultValueProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.DefaultValue);
        public static readonly string ValidationRuleProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.ValidationRule);
        public static readonly string ActionCodeProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.ActionCode);
        public static readonly string ActionDescriptionProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.ActionDescription);
        public static readonly string JsonQueryProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.JsonQuery);
        public static readonly string WhereClauseFormatProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.WhereClauseFormat);
        public static readonly string IsSimpleQueryProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Boolean>>(o => o.IsSimpleQuery);
        public static readonly string JsonSampleDataProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.JsonSampleData);
        public static readonly string JsonSchemaProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.JsonSchema);
        public static readonly string SchemaDataSetMappingProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.SchemaDataSetMapping);
        public static readonly string HttpMethdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.HttpMethd);
        public static readonly string DataSourceIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.DataSourceId);
        public static readonly string SchemaFromDataSetMappingProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.SchemaFromDataSetMapping);
        public static readonly string PostProcessScriptProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.PostProcessScript);
        public static readonly string ApiconfigParametersProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.ApiconfigParameters);
        public static readonly string TablePrefixProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,System.String>(o => o.TablePrefix);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppIntergrationSettingParameterDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppIntergrationSettingParameterDto()
        {        
        }
		
		static AppIntergrationSettingParameterDto()
        {
                                        
			  
			ForeignKeyProperties.Add(IntergrationSettingIdProperty);                           		
               
			DictStringPropertyMaxLength.Add(MappingInternalCodeProperty,100); 
			DictStringPropertyMaxLength.Add(InternalFiledNameProperty,100); 
			DictStringPropertyMaxLength.Add(ExternalFieldNameProperty,100);   
			DictStringPropertyMaxLength.Add(DefaultValueProperty,2147483647); 
			DictStringPropertyMaxLength.Add(ValidationRuleProperty,500); 
			DictStringPropertyMaxLength.Add(ActionCodeProperty,200); 
			DictStringPropertyMaxLength.Add(ActionDescriptionProperty,500); 
			DictStringPropertyMaxLength.Add(JsonQueryProperty,2147483647); 
			DictStringPropertyMaxLength.Add(WhereClauseFormatProperty,500);  
			DictStringPropertyMaxLength.Add(JsonSampleDataProperty,2147483647); 
			DictStringPropertyMaxLength.Add(JsonSchemaProperty,2147483647); 
			DictStringPropertyMaxLength.Add(SchemaDataSetMappingProperty,2147483647); 
			DictStringPropertyMaxLength.Add(HttpMethdProperty,20);  
			DictStringPropertyMaxLength.Add(SchemaFromDataSetMappingProperty,2147483647); 
			DictStringPropertyMaxLength.Add(PostProcessScriptProperty,2147483647); 
			DictStringPropertyMaxLength.Add(ApiconfigParametersProperty,2147483647); 
			DictStringPropertyMaxLength.Add(TablePrefixProperty,400);       
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
    


        /// <summary> The IntergrationSettingId property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> IntergrationSettingId
        {
            get { return  GetValue<Nullable<System.Int32>>( IntergrationSettingIdProperty);}
            set { SetValue(IntergrationSettingIdProperty,value); }
        }

        /// <summary> The MappingInternalCode property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MappingInternalCode
        {
            get { return  GetValue<System.String>( MappingInternalCodeProperty);}
            set { SetValue(MappingInternalCodeProperty,value); }
        }

        /// <summary> The InternalFiledName property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String InternalFiledName
        {
            get { return  GetValue<System.String>( InternalFiledNameProperty);}
            set { SetValue(InternalFiledNameProperty,value); }
        }

        /// <summary> The ExternalFieldName property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ExternalFieldName
        {
            get { return  GetValue<System.String>( ExternalFieldNameProperty);}
            set { SetValue(ExternalFieldNameProperty,value); }
        }

        /// <summary> The TranscationId property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TranscationId
        {
            get { return  GetValue<Nullable<System.Int32>>( TranscationIdProperty);}
            set { SetValue(TranscationIdProperty,value); }
        }

        /// <summary> The TranscationFieId property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TranscationFieId
        {
            get { return  GetValue<Nullable<System.Int32>>( TranscationFieIdProperty);}
            set { SetValue(TranscationFieIdProperty,value); }
        }

        /// <summary> The DefaultValue property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefaultValue
        {
            get { return  GetValue<System.String>( DefaultValueProperty);}
            set { SetValue(DefaultValueProperty,value); }
        }

        /// <summary> The ValidationRule property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ValidationRule
        {
            get { return  GetValue<System.String>( ValidationRuleProperty);}
            set { SetValue(ValidationRuleProperty,value); }
        }

        /// <summary> The ActionCode property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ActionCode
        {
            get { return  GetValue<System.String>( ActionCodeProperty);}
            set { SetValue(ActionCodeProperty,value); }
        }

        /// <summary> The ActionDescription property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ActionDescription
        {
            get { return  GetValue<System.String>( ActionDescriptionProperty);}
            set { SetValue(ActionDescriptionProperty,value); }
        }

        /// <summary> The JsonQuery property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String JsonQuery
        {
            get { return  GetValue<System.String>( JsonQueryProperty);}
            set { SetValue(JsonQueryProperty,value); }
        }

        /// <summary> The WhereClauseFormat property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WhereClauseFormat
        {
            get { return  GetValue<System.String>( WhereClauseFormatProperty);}
            set { SetValue(WhereClauseFormatProperty,value); }
        }

        /// <summary> The IsSimpleQuery property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSimpleQuery
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSimpleQueryProperty);}
            set { SetValue(IsSimpleQueryProperty,value); }
        }

        /// <summary> The JsonSampleData property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String JsonSampleData
        {
            get { return  GetValue<System.String>( JsonSampleDataProperty);}
            set { SetValue(JsonSampleDataProperty,value); }
        }

        /// <summary> The JsonSchema property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String JsonSchema
        {
            get { return  GetValue<System.String>( JsonSchemaProperty);}
            set { SetValue(JsonSchemaProperty,value); }
        }

        /// <summary> The SchemaDataSetMapping property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SchemaDataSetMapping
        {
            get { return  GetValue<System.String>( SchemaDataSetMappingProperty);}
            set { SetValue(SchemaDataSetMappingProperty,value); }
        }

        /// <summary> The HttpMethd property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String HttpMethd
        {
            get { return  GetValue<System.String>( HttpMethdProperty);}
            set { SetValue(HttpMethdProperty,value); }
        }

        /// <summary> The DataSourceId property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceIdProperty);}
            set { SetValue(DataSourceIdProperty,value); }
        }

        /// <summary> The SchemaFromDataSetMapping property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SchemaFromDataSetMapping
        {
            get { return  GetValue<System.String>( SchemaFromDataSetMappingProperty);}
            set { SetValue(SchemaFromDataSetMappingProperty,value); }
        }

        /// <summary> The PostProcessScript property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PostProcessScript
        {
            get { return  GetValue<System.String>( PostProcessScriptProperty);}
            set { SetValue(PostProcessScriptProperty,value); }
        }

        /// <summary> The ApiconfigParameters property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ApiconfigParameters
        {
            get { return  GetValue<System.String>( ApiconfigParametersProperty);}
            set { SetValue(ApiconfigParametersProperty,value); }
        }

        /// <summary> The TablePrefix property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TablePrefix
        {
            get { return  GetValue<System.String>( TablePrefixProperty);}
            set { SetValue(TablePrefixProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppIntergrationSettingParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppIntergrationSettingParameter</summary>
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

