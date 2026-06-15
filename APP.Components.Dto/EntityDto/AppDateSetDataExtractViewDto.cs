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
    /// DTO class for the entity 'AppDateSetDataExtractView'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppDateSetDataExtractViewDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,System.String>(o => o.Description);
        public static readonly string DataSetIdProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.Int32>>(o => o.DataSetId);
        public static readonly string DbfiledNameProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,System.String>(o => o.DbfiledName);
        public static readonly string IsGroupProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.Boolean>>(o => o.IsGroup);
        public static readonly string AggFunctionProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.Int32>>(o => o.AggFunction);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppDateSetDataExtractViewDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppDateSetDataExtractViewDto()
        {        
        }
		
		static AppDateSetDataExtractViewDto()
        {
                       
			   
			ForeignKeyProperties.Add(DataSetIdProperty);         		
              
			DictStringPropertyMaxLength.Add(DescriptionProperty,200);  
			DictStringPropertyMaxLength.Add(DbfiledNameProperty,200);         
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
    


        /// <summary> The Description property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The DataSetId property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataSetId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataSetIdProperty);}
            set { SetValue(DataSetIdProperty,value); }
        }

        /// <summary> The DbfiledName property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DbfiledName
        {
            get { return  GetValue<System.String>( DbfiledNameProperty);}
            set { SetValue(DbfiledNameProperty,value); }
        }

        /// <summary> The IsGroup property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsGroup
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsGroupProperty);}
            set { SetValue(IsGroupProperty,value); }
        }

        /// <summary> The AggFunction property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AggFunction
        {
            get { return  GetValue<Nullable<System.Int32>>( AggFunctionProperty);}
            set { SetValue(AggFunctionProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppDateSetDataExtractView</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppDateSetDataExtractView</summary>
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

