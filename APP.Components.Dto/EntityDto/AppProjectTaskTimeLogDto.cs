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
    /// DTO class for the entity 'AppProjectTaskTimeLog'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectTaskTimeLogDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProjectTaskIdProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Int32>>(o => o.ProjectTaskId);
        public static readonly string TeamMemberIdProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Int32>>(o => o.TeamMemberId);
        public static readonly string TiemSpanHoursProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Double>>(o => o.TiemSpanHours);
        public static readonly string RateByHourProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Double>>(o => o.RateByHour);
        public static readonly string ApprovedByProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Int32>>(o => o.ApprovedBy);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectTaskTimeLogDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectTaskTimeLogDto()
        {        
        }
		
		static AppProjectTaskTimeLogDto()
        {
                       
			  
			ForeignKeyProperties.Add(ProjectTaskIdProperty);          		
                         
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
    


        /// <summary> The ProjectTaskId property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectTaskIdProperty);}
            set { SetValue(ProjectTaskIdProperty,value); }
        }

        /// <summary> The TeamMemberId property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TeamMemberId
        {
            get { return  GetValue<Nullable<System.Int32>>( TeamMemberIdProperty);}
            set { SetValue(TeamMemberIdProperty,value); }
        }

        /// <summary> The TiemSpanHours property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> TiemSpanHours
        {
            get { return  GetValue<Nullable<System.Double>>( TiemSpanHoursProperty);}
            set { SetValue(TiemSpanHoursProperty,value); }
        }

        /// <summary> The RateByHour property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> RateByHour
        {
            get { return  GetValue<Nullable<System.Double>>( RateByHourProperty);}
            set { SetValue(RateByHourProperty,value); }
        }

        /// <summary> The ApprovedBy property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ApprovedBy
        {
            get { return  GetValue<Nullable<System.Int32>>( ApprovedByProperty);}
            set { SetValue(ApprovedByProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectTaskTimeLog</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectTaskTimeLog</summary>
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

