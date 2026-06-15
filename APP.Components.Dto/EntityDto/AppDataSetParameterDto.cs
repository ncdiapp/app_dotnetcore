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
    /// DTO class for the entity 'AppDataSetParameter'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDataSetParameterDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DataSetIdProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,Nullable<System.Int32>>(o => o.DataSetId);
        public static readonly string ParameterNameProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,System.String>(o => o.ParameterName);
        public static readonly string DataTypeProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,System.String>(o => o.DataType);
        public static readonly string DirectionInOutProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,System.String>(o => o.DirectionInOut);
        public static readonly string DefautValueProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,System.String>(o => o.DefautValue);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppDataSetParameterDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppDataSetParameterDto()
        {        
        }
		
		static AppDataSetParameterDto()
        {
                       
			  
			ForeignKeyProperties.Add(DataSetIdProperty);          		
               
			DictStringPropertyMaxLength.Add(ParameterNameProperty,50); 
			DictStringPropertyMaxLength.Add(DataTypeProperty,50); 
			DictStringPropertyMaxLength.Add(DirectionInOutProperty,10); 
			DictStringPropertyMaxLength.Add(DefautValueProperty,100);       
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
    


        /// <summary> The DataSetId property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSetIdProperty);}
            set { SetValue(DataSetIdProperty,value); }
        }

        /// <summary> The ParameterName property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParameterName
        {
            get { return  GetValue<System.String>( ParameterNameProperty);}
            set { SetValue(ParameterNameProperty,value); }
        }

        /// <summary> The DataType property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DataType
        {
            get { return  GetValue<System.String>( DataTypeProperty);}
            set { SetValue(DataTypeProperty,value); }
        }

        /// <summary> The DirectionInOut property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DirectionInOut
        {
            get { return  GetValue<System.String>( DirectionInOutProperty);}
            set { SetValue(DirectionInOutProperty,value); }
        }

        /// <summary> The DefautValue property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DefautValue
        {
            get { return  GetValue<System.String>( DefautValueProperty);}
            set { SetValue(DefautValueProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppDataSetParameter</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppDataSetParameter</summary>
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

