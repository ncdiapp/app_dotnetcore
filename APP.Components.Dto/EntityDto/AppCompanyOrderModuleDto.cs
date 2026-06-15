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
    /// DTO class for the entity 'AppCompanyOrderModule'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCompanyOrderModuleDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ModuleRegisterIdProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.Int32>>(o => o.ModuleRegisterId);
        public static readonly string StartTrialDateProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.DateTime>>(o => o.StartTrialDate);
        public static readonly string EndTrialDateProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.DateTime>>(o => o.EndTrialDate);
        public static readonly string CurrentUsageStatusProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.Int32>>(o => o.CurrentUsageStatus);
        public static readonly string AppCompanyIdProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.Int32>>(o => o.AppCompanyId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCompanyOrderModuleDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCompanyOrderModuleDto()
        {        
        }
		
		static AppCompanyOrderModuleDto()
        {
                       
			  
			ForeignKeyProperties.Add(ModuleRegisterIdProperty);     
			ForeignKeyProperties.Add(AppCompanyIdProperty);      		
                         
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
    


        /// <summary> The ModuleRegisterId property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ModuleRegisterId
        {
            get { return  GetValue<Nullable<System.Int32>>( ModuleRegisterIdProperty);}
            set { SetValue(ModuleRegisterIdProperty,value); }
        }

        /// <summary> The StartTrialDate property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> StartTrialDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( StartTrialDateProperty);}
            set { SetValue(StartTrialDateProperty,value); }
        }

        /// <summary> The EndTrialDate property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> EndTrialDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( EndTrialDateProperty);}
            set { SetValue(EndTrialDateProperty,value); }
        }

        /// <summary> The CurrentUsageStatus property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CurrentUsageStatus
        {
            get { return  GetValue<Nullable<System.Int32>>( CurrentUsageStatusProperty);}
            set { SetValue(CurrentUsageStatusProperty,value); }
        }

        /// <summary> The AppCompanyId property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCompanyIdProperty);}
            set { SetValue(AppCompanyIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCompanyOrderModule</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCompanyOrderModule</summary>
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

