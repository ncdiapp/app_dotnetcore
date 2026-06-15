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
    /// DTO class for the entity 'AppProjectTaskResource'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTaskResourceDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProjectWorkFlowTaskIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,System.Int32>(o => o.ProjectWorkFlowTaskId);
        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.Int32>>(o => o.UserId);
        public static readonly string RoleIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.Int32>>(o => o.RoleId);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string PlannedWorkHoursProperty = ObjectInfoHelper.GetName<AppProjectTaskResourceDto ,Nullable<System.Double>>(o => o.PlannedWorkHours);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectTaskResourceDto()
        {        
        }
		
		static AppProjectTaskResourceDto()
        {
              
			MandatoryProperties.Add(ProjectWorkFlowTaskIdProperty);         
			  
			ForeignKeyProperties.Add(ProjectWorkFlowTaskIdProperty);         		
                        
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
    


        /// <summary> The ProjectWorkFlowTaskId property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ProjectWorkFlowTaskId
        {
            get { return  GetValue<System.Int32>( ProjectWorkFlowTaskIdProperty);}
            set { SetValue(ProjectWorkFlowTaskIdProperty,value); }
        }

        /// <summary> The UserId property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UserId
        {
            get { return  GetValue<Nullable<System.Int32>>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The RoleId property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RoleId
        {
            get { return  GetValue<Nullable<System.Int32>>( RoleIdProperty);}
            set { SetValue(RoleIdProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The PlannedWorkHours property of the Entity AppProjectTaskResource</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PlannedWorkHours
        {
            get { return  GetValue<Nullable<System.Double>>( PlannedWorkHoursProperty);}
            set { SetValue(PlannedWorkHoursProperty,value); }
        }
        
        #endregion

       
        
    }
}

