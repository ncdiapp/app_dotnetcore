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
    /// DTO class for the entity 'AppBusienssAssormentNavigation'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppBusienssAssormentNavigationDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string AssormentNameProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,System.String>(o => o.AssormentName);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,System.String>(o => o.Description);
        public static readonly string MainTraisactionIdProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.Int32>>(o => o.MainTraisactionId);
        public static readonly string MainProjectTemplateIdProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.Int32>>(o => o.MainProjectTemplateId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppBusienssAssormentNavigationDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppBusienssAssormentNavigationDto()
        {        
        }
		
		static AppBusienssAssormentNavigationDto()
        {
                      
			          		
              
			DictStringPropertyMaxLength.Add(AssormentNameProperty,100); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);         
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
    


        /// <summary> The AssormentName property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String AssormentName
        {
            get { return  GetValue<System.String>( AssormentNameProperty);}
            set { SetValue(AssormentNameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The MainTraisactionId property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MainTraisactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( MainTraisactionIdProperty);}
            set { SetValue(MainTraisactionIdProperty,value); }
        }

        /// <summary> The MainProjectTemplateId property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MainProjectTemplateId
        {
            get { return  GetValue<Nullable<System.Int32>>( MainProjectTemplateIdProperty);}
            set { SetValue(MainProjectTemplateIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppBusienssAssormentNavigation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppBusienssAssormentNavigation</summary>
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

