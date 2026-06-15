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
    /// DTO class for the entity 'AppProjectTaskResourcePlannedHours'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTaskResourcePlannedHoursDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string TaskResourceIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.Int32>>(o => o.TaskResourceId);
        public static readonly string DateIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.Int32>>(o => o.DateId);
        public static readonly string PlannedWorkHoursProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.Double>>(o => o.PlannedWorkHours);
        public static readonly string StartTimeProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.TimeSpan>>(o => o.StartTime);
        public static readonly string EndTimeProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.TimeSpan>>(o => o.EndTime);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectTaskResourcePlannedHoursDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectTaskResourcePlannedHoursDto()
        {        
        }
		
		static AppProjectTaskResourcePlannedHoursDto()
        {
                       
			  
			ForeignKeyProperties.Add(TaskResourceIdProperty);          		
                         
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
    


        /// <summary> The TaskResourceId property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TaskResourceId
        {
            get { return  GetValue<Nullable<System.Int32>>( TaskResourceIdProperty);}
            set { SetValue(TaskResourceIdProperty,value); }
        }

        /// <summary> The DateId property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DateId
        {
            get { return  GetValue<Nullable<System.Int32>>( DateIdProperty);}
            set { SetValue(DateIdProperty,value); }
        }

        /// <summary> The PlannedWorkHours property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PlannedWorkHours
        {
            get { return  GetValue<Nullable<System.Double>>( PlannedWorkHoursProperty);}
            set { SetValue(PlannedWorkHoursProperty,value); }
        }

        /// <summary> The StartTime property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.TimeSpan> StartTime
        {
            get { return  GetValue<Nullable<System.TimeSpan>>( StartTimeProperty);}
            set { SetValue(StartTimeProperty,value); }
        }

        /// <summary> The EndTime property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.TimeSpan> EndTime
        {
            get { return  GetValue<Nullable<System.TimeSpan>>( EndTimeProperty);}
            set { SetValue(EndTimeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectTaskResourcePlannedHours</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectTaskResourcePlannedHours</summary>
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

