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
    /// DTO class for the entity 'AppCalendarRecurringDay'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCalendarRecurringDayDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CalendarIdProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.CalendarId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,System.String>(o => o.Description);
        public static readonly string DateTokenTypeProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.DateTokenType);
        public static readonly string MonthProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.Month);
        public static readonly string DayOfMonthProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.DayOfMonth);
        public static readonly string DayOfWeekProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.DayOfWeek);
        public static readonly string WorkStatusProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.WorkStatus);
        public static readonly string RecurringStartDateProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,System.DateTime>(o => o.RecurringStartDate);
        public static readonly string RecurringEndDateProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.DateTime>>(o => o.RecurringEndDate);
        public static readonly string RecurringTypeProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.RecurringType);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCalendarRecurringDayDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCalendarRecurringDayDto()
        {        
        }
		
		static AppCalendarRecurringDayDto()
        {
                      
			MandatoryProperties.Add(RecurringStartDateProperty);        
			  
			ForeignKeyProperties.Add(CalendarIdProperty);                		
               
			DictStringPropertyMaxLength.Add(NameProperty,200); 
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
    


        /// <summary> The CalendarId property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CalendarId
        {
            get { return  GetValue<Nullable<System.Int32>>( CalendarIdProperty);}
            set { SetValue(CalendarIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The DateTokenType property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DateTokenType
        {
            get { return  GetValue<Nullable<System.Int32>>( DateTokenTypeProperty);}
            set { SetValue(DateTokenTypeProperty,value); }
        }

        /// <summary> The Month property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Month
        {
            get { return  GetValue<Nullable<System.Int32>>( MonthProperty);}
            set { SetValue(MonthProperty,value); }
        }

        /// <summary> The DayOfMonth property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DayOfMonth
        {
            get { return  GetValue<Nullable<System.Int32>>( DayOfMonthProperty);}
            set { SetValue(DayOfMonthProperty,value); }
        }

        /// <summary> The DayOfWeek property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DayOfWeek
        {
            get { return  GetValue<Nullable<System.Int32>>( DayOfWeekProperty);}
            set { SetValue(DayOfWeekProperty,value); }
        }

        /// <summary> The WorkStatus property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WorkStatus
        {
            get { return  GetValue<Nullable<System.Int32>>( WorkStatusProperty);}
            set { SetValue(WorkStatusProperty,value); }
        }

        /// <summary> The RecurringStartDate property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.DateTime RecurringStartDate
        {
            get { return  GetValue<System.DateTime>( RecurringStartDateProperty);}
            set { SetValue(RecurringStartDateProperty,value); }
        }

        /// <summary> The RecurringEndDate property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> RecurringEndDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( RecurringEndDateProperty);}
            set { SetValue(RecurringEndDateProperty,value); }
        }

        /// <summary> The RecurringType property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RecurringType
        {
            get { return  GetValue<Nullable<System.Int32>>( RecurringTypeProperty);}
            set { SetValue(RecurringTypeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCalendarRecurringDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCalendarRecurringDay</summary>
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

