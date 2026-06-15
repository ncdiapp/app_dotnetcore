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
    /// DTO class for the entity 'AppEntityInfo'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEntityInfoDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string EntityCodeProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.EntityCode);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.Description);
        public static readonly string EntityTypeProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.EntityType);
        public static readonly string TableNameProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.TableName);
        public static readonly string SchemaOwnerProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.SchemaOwner);
        public static readonly string IdentityFieldProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.IdentityField);
        public static readonly string DisplayFiled1Property = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.DisplayFiled1);
        public static readonly string DisplayFiled3Property = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.DisplayFiled3);
        public static readonly string DisplayFiled2Property = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.DisplayFiled2);
        public static readonly string PartnerFilterFiledProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.PartnerFilterFiled);
        public static readonly string QueryTextProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.QueryText);
        public static readonly string DataSourceFromProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.DataSourceFrom);
        public static readonly string IsSystemDefineProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Boolean>>(o => o.IsSystemDefine);
        public static readonly string IsSharedbyMutipleCompanyProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Boolean>>(o => o.IsSharedbyMutipleCompany);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string ColorCodeFieldProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.ColorCodeField);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string ExternalKeyFieldProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.ExternalKeyField);
        public static readonly string OtherSettingsProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,System.String>(o => o.OtherSettings);
        public static readonly string IdentityCoumnDataTypeProperty = ObjectInfoHelper.GetName<AppEntityInfoDto ,Nullable<System.Int32>>(o => o.IdentityCoumnDataType);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEntityInfoDto()
        {        
        }
		
		static AppEntityInfoDto()
        {
              
			MandatoryProperties.Add(EntityCodeProperty);                        
			             
			ForeignKeyProperties.Add(DataSourceFromProperty);          
			ForeignKeyProperties.Add(SaasApplicationIdProperty);    		
              
			DictStringPropertyMaxLength.Add(EntityCodeProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);  
			DictStringPropertyMaxLength.Add(TableNameProperty,100); 
			DictStringPropertyMaxLength.Add(SchemaOwnerProperty,50); 
			DictStringPropertyMaxLength.Add(IdentityFieldProperty,100); 
			DictStringPropertyMaxLength.Add(DisplayFiled1Property,100); 
			DictStringPropertyMaxLength.Add(DisplayFiled3Property,100); 
			DictStringPropertyMaxLength.Add(DisplayFiled2Property,100); 
			DictStringPropertyMaxLength.Add(PartnerFilterFiledProperty,100); 
			DictStringPropertyMaxLength.Add(QueryTextProperty,3000);         
			DictStringPropertyMaxLength.Add(ColorCodeFieldProperty,100);  
			DictStringPropertyMaxLength.Add(ExternalKeyFieldProperty,500); 
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
    


        /// <summary> The EntityCode property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String EntityCode
        {
            get { return  GetValue<System.String>( EntityCodeProperty);}
            set { SetValue(EntityCodeProperty,value); }
        }

        /// <summary> The Description property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The EntityType property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EntityType
        {
            get { return  GetValue<Nullable<System.Int32>>( EntityTypeProperty);}
            set { SetValue(EntityTypeProperty,value); }
        }

        /// <summary> The TableName property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TableName
        {
            get { return  GetValue<System.String>( TableNameProperty);}
            set { SetValue(TableNameProperty,value); }
        }

        /// <summary> The SchemaOwner property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String SchemaOwner
        {
            get { return  GetValue<System.String>( SchemaOwnerProperty);}
            set { SetValue(SchemaOwnerProperty,value); }
        }

        /// <summary> The IdentityField property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String IdentityField
        {
            get { return  GetValue<System.String>( IdentityFieldProperty);}
            set { SetValue(IdentityFieldProperty,value); }
        }

        /// <summary> The DisplayFiled1 property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayFiled1
        {
            get { return  GetValue<System.String>( DisplayFiled1Property);}
            set { SetValue(DisplayFiled1Property,value); }
        }

        /// <summary> The DisplayFiled3 property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayFiled3
        {
            get { return  GetValue<System.String>( DisplayFiled3Property);}
            set { SetValue(DisplayFiled3Property,value); }
        }

        /// <summary> The DisplayFiled2 property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DisplayFiled2
        {
            get { return  GetValue<System.String>( DisplayFiled2Property);}
            set { SetValue(DisplayFiled2Property,value); }
        }

        /// <summary> The PartnerFilterFiled property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PartnerFilterFiled
        {
            get { return  GetValue<System.String>( PartnerFilterFiledProperty);}
            set { SetValue(PartnerFilterFiledProperty,value); }
        }

        /// <summary> The QueryText property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String QueryText
        {
            get { return  GetValue<System.String>( QueryTextProperty);}
            set { SetValue(QueryTextProperty,value); }
        }

        /// <summary> The DataSourceFrom property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceFrom
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceFromProperty);}
            set { SetValue(DataSourceFromProperty,value); }
        }

        /// <summary> The IsSystemDefine property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSystemDefine
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSystemDefineProperty);}
            set { SetValue(IsSystemDefineProperty,value); }
        }

        /// <summary> The IsSharedbyMutipleCompany property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsSharedbyMutipleCompany
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsSharedbyMutipleCompanyProperty);}
            set { SetValue(IsSharedbyMutipleCompanyProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The ColorCodeField property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ColorCodeField
        {
            get { return  GetValue<System.String>( ColorCodeFieldProperty);}
            set { SetValue(ColorCodeFieldProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The ExternalKeyField property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ExternalKeyField
        {
            get { return  GetValue<System.String>( ExternalKeyFieldProperty);}
            set { SetValue(ExternalKeyFieldProperty,value); }
        }

        /// <summary> The OtherSettings property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String OtherSettings
        {
            get { return  GetValue<System.String>( OtherSettingsProperty);}
            set { SetValue(OtherSettingsProperty,value); }
        }

        /// <summary> The IdentityCoumnDataType property of the Entity AppEntityInfo</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> IdentityCoumnDataType
        {
            get { return  GetValue<Nullable<System.Int32>>( IdentityCoumnDataTypeProperty);}
            set { SetValue(IdentityCoumnDataTypeProperty,value); }
        }
        
        #endregion

       
        
    }
}

