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
    /// DTO class for the entity 'AppCalendarBaseDate'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCalendarBaseDateDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string DayDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,Nullable<System.DateTime>>(o => o.DayDesc);
        public static readonly string WeekProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.Week);
        public static readonly string WeekDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.WeekDesc);
        public static readonly string BiWeekProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.BiWeek);
        public static readonly string BiWeekDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.BiWeekDesc);
        public static readonly string HlfMonthProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.HlfMonth);
        public static readonly string HlfMonthDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.HlfMonthDesc);
        public static readonly string MonthProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.Month);
        public static readonly string MonthDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.MonthDesc);
        public static readonly string QuarterProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.Quarter);
        public static readonly string QuarterDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.QuarterDesc);
        public static readonly string PlnHlfYrProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.PlnHlfYr);
        public static readonly string PlnHlfYrDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.PlnHlfYrDesc);
        public static readonly string PlnYrProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.PlnYr);
        public static readonly string PlnYrDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.PlnYrDesc);
        public static readonly string FiscalHlfYrProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.FiscalHlfYr);
        public static readonly string FiscalHlfYrDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.FiscalHlfYrDesc);
        public static readonly string FiscalYrProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.FiscalYr);
        public static readonly string FiscalYrDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.FiscalYrDesc);
        public static readonly string RangePeriodProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.RangePeriod);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string RangePeriodDescProperty = ObjectInfoHelper.GetName<AppCalendarBaseDateDto ,System.String>(o => o.RangePeriodDesc);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCalendarBaseDateDto()
        {        
        }
		
		static AppCalendarBaseDateDto()
        {
                                       
			                           		
               
			DictStringPropertyMaxLength.Add(WeekProperty,255); 
			DictStringPropertyMaxLength.Add(WeekDescProperty,255); 
			DictStringPropertyMaxLength.Add(BiWeekProperty,255); 
			DictStringPropertyMaxLength.Add(BiWeekDescProperty,255); 
			DictStringPropertyMaxLength.Add(HlfMonthProperty,255); 
			DictStringPropertyMaxLength.Add(HlfMonthDescProperty,255); 
			DictStringPropertyMaxLength.Add(MonthProperty,255); 
			DictStringPropertyMaxLength.Add(MonthDescProperty,255); 
			DictStringPropertyMaxLength.Add(QuarterProperty,255); 
			DictStringPropertyMaxLength.Add(QuarterDescProperty,255); 
			DictStringPropertyMaxLength.Add(PlnHlfYrProperty,255); 
			DictStringPropertyMaxLength.Add(PlnHlfYrDescProperty,255); 
			DictStringPropertyMaxLength.Add(PlnYrProperty,255); 
			DictStringPropertyMaxLength.Add(PlnYrDescProperty,255); 
			DictStringPropertyMaxLength.Add(FiscalHlfYrProperty,255); 
			DictStringPropertyMaxLength.Add(FiscalHlfYrDescProperty,255); 
			DictStringPropertyMaxLength.Add(FiscalYrProperty,255); 
			DictStringPropertyMaxLength.Add(FiscalYrDescProperty,255); 
			DictStringPropertyMaxLength.Add(RangePeriodProperty,255);      
			DictStringPropertyMaxLength.Add(RangePeriodDescProperty,255);  
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
    


        /// <summary> The DayDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DayDesc
        {
            get { return  GetValue<Nullable<System.DateTime>>( DayDescProperty);}
            set { SetValue(DayDescProperty,value); }
        }

        /// <summary> The Week property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Week
        {
            get { return  GetValue<System.String>( WeekProperty);}
            set { SetValue(WeekProperty,value); }
        }

        /// <summary> The WeekDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String WeekDesc
        {
            get { return  GetValue<System.String>( WeekDescProperty);}
            set { SetValue(WeekDescProperty,value); }
        }

        /// <summary> The BiWeek property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BiWeek
        {
            get { return  GetValue<System.String>( BiWeekProperty);}
            set { SetValue(BiWeekProperty,value); }
        }

        /// <summary> The BiWeekDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BiWeekDesc
        {
            get { return  GetValue<System.String>( BiWeekDescProperty);}
            set { SetValue(BiWeekDescProperty,value); }
        }

        /// <summary> The HlfMonth property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String HlfMonth
        {
            get { return  GetValue<System.String>( HlfMonthProperty);}
            set { SetValue(HlfMonthProperty,value); }
        }

        /// <summary> The HlfMonthDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String HlfMonthDesc
        {
            get { return  GetValue<System.String>( HlfMonthDescProperty);}
            set { SetValue(HlfMonthDescProperty,value); }
        }

        /// <summary> The Month property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Month
        {
            get { return  GetValue<System.String>( MonthProperty);}
            set { SetValue(MonthProperty,value); }
        }

        /// <summary> The MonthDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String MonthDesc
        {
            get { return  GetValue<System.String>( MonthDescProperty);}
            set { SetValue(MonthDescProperty,value); }
        }

        /// <summary> The Quarter property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Quarter
        {
            get { return  GetValue<System.String>( QuarterProperty);}
            set { SetValue(QuarterProperty,value); }
        }

        /// <summary> The QuarterDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String QuarterDesc
        {
            get { return  GetValue<System.String>( QuarterDescProperty);}
            set { SetValue(QuarterDescProperty,value); }
        }

        /// <summary> The PlnHlfYr property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PlnHlfYr
        {
            get { return  GetValue<System.String>( PlnHlfYrProperty);}
            set { SetValue(PlnHlfYrProperty,value); }
        }

        /// <summary> The PlnHlfYrDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PlnHlfYrDesc
        {
            get { return  GetValue<System.String>( PlnHlfYrDescProperty);}
            set { SetValue(PlnHlfYrDescProperty,value); }
        }

        /// <summary> The PlnYr property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PlnYr
        {
            get { return  GetValue<System.String>( PlnYrProperty);}
            set { SetValue(PlnYrProperty,value); }
        }

        /// <summary> The PlnYrDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PlnYrDesc
        {
            get { return  GetValue<System.String>( PlnYrDescProperty);}
            set { SetValue(PlnYrDescProperty,value); }
        }

        /// <summary> The FiscalHlfYr property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FiscalHlfYr
        {
            get { return  GetValue<System.String>( FiscalHlfYrProperty);}
            set { SetValue(FiscalHlfYrProperty,value); }
        }

        /// <summary> The FiscalHlfYrDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FiscalHlfYrDesc
        {
            get { return  GetValue<System.String>( FiscalHlfYrDescProperty);}
            set { SetValue(FiscalHlfYrDescProperty,value); }
        }

        /// <summary> The FiscalYr property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FiscalYr
        {
            get { return  GetValue<System.String>( FiscalYrProperty);}
            set { SetValue(FiscalYrProperty,value); }
        }

        /// <summary> The FiscalYrDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String FiscalYrDesc
        {
            get { return  GetValue<System.String>( FiscalYrDescProperty);}
            set { SetValue(FiscalYrDescProperty,value); }
        }

        /// <summary> The RangePeriod property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RangePeriod
        {
            get { return  GetValue<System.String>( RangePeriodProperty);}
            set { SetValue(RangePeriodProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The RangePeriodDesc property of the Entity AppCalendarBaseDate</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String RangePeriodDesc
        {
            get { return  GetValue<System.String>( RangePeriodDescProperty);}
            set { SetValue(RangePeriodDescProperty,value); }
        }
        
        #endregion

       
        
    }
}

