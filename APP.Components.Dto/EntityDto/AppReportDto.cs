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
    /// DTO class for the entity 'AppReport'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppReportDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataSourceIdProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Int32>>(o => o.DataSourceId);
        public static readonly string ReportNameProperty = ObjectInfoHelper.GetName<AppReportDto ,System.String>(o => o.ReportName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppReportDto ,System.String>(o => o.Description);
        public static readonly string ReportFileNameProperty = ObjectInfoHelper.GetName<AppReportDto ,System.String>(o => o.ReportFileName);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string ReportEngineTypeProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Int32>>(o => o.ReportEngineType);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppReportDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppReportDto()
        {        
        }
		
		static AppReportDto()
        {
                         
			  
			ForeignKeyProperties.Add(DataSourceIdProperty);            
			ForeignKeyProperties.Add(SaasApplicationIdProperty); 		
               
			DictStringPropertyMaxLength.Add(ReportNameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500); 
			DictStringPropertyMaxLength.Add(ReportFileNameProperty,400);          
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
    


        /// <summary> The DataSourceId property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSourceId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSourceIdProperty);}
            set { SetValue(DataSourceIdProperty,value); }
        }

        /// <summary> The ReportName property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ReportName
        {
            get { return  GetValue<System.String>( ReportNameProperty);}
            set { SetValue(ReportNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ReportFileName property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ReportFileName
        {
            get { return  GetValue<System.String>( ReportFileNameProperty);}
            set { SetValue(ReportFileNameProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The ReportEngineType property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ReportEngineType
        {
            get { return  GetValue<Nullable<System.Int32>>( ReportEngineTypeProperty);}
            set { SetValue(ReportEngineTypeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppReport</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

