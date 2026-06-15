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
    /// DTO class for the entity 'AppCalendarSpecificDay'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppCalendarSpecificDayDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string CalendarIdProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.CalendarId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,System.String>(o => o.Description);
        public static readonly string StartDateProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.DateTime>>(o => o.StartDate);
        public static readonly string EndDateProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.DateTime>>(o => o.EndDate);
        public static readonly string WorkStatusProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.WorkStatus);
        public static readonly string EmDateDefineTypeProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.EmDateDefineType);
        public static readonly string EmDateRangeTypeProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.EmDateRangeType);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string UserDefined1Property = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,System.String>(o => o.UserDefined1);
        public static readonly string UserDefined2Property = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,System.String>(o => o.UserDefined2);
        public static readonly string UserDefined3Property = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,System.String>(o => o.UserDefined3);
        public static readonly string UserDefined4Property = ObjectInfoHelper.GetName<AppCalendarSpecificDayDto ,Nullable<System.Boolean>>(o => o.UserDefined4);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppCalendarSpecificDayDto()
        {        
        }
		
		static AppCalendarSpecificDayDto()
        {
                              
			  
			ForeignKeyProperties.Add(CalendarIdProperty);                 		
               
			DictStringPropertyMaxLength.Add(NameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);           
			DictStringPropertyMaxLength.Add(UserDefined1Property,2000); 
			DictStringPropertyMaxLength.Add(UserDefined2Property,2000); 
			DictStringPropertyMaxLength.Add(UserDefined3Property,2000);   
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
    


        /// <summary> The CalendarId property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CalendarId
        {
            get { return  GetValue<Nullable<System.Int32>>( CalendarIdProperty);}
            set { SetValue(CalendarIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The StartDate property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> StartDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( StartDateProperty);}
            set { SetValue(StartDateProperty,value); }
        }

        /// <summary> The EndDate property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> EndDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( EndDateProperty);}
            set { SetValue(EndDateProperty,value); }
        }

        /// <summary> The WorkStatus property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WorkStatus
        {
            get { return  GetValue<Nullable<System.Int32>>( WorkStatusProperty);}
            set { SetValue(WorkStatusProperty,value); }
        }

        /// <summary> The EmDateDefineType property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmDateDefineType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmDateDefineTypeProperty);}
            set { SetValue(EmDateDefineTypeProperty,value); }
        }

        /// <summary> The EmDateRangeType property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmDateRangeType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmDateRangeTypeProperty);}
            set { SetValue(EmDateRangeTypeProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The UserDefined1 property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UserDefined1
        {
            get { return  GetValue<System.String>( UserDefined1Property);}
            set { SetValue(UserDefined1Property,value); }
        }

        /// <summary> The UserDefined2 property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UserDefined2
        {
            get { return  GetValue<System.String>( UserDefined2Property);}
            set { SetValue(UserDefined2Property,value); }
        }

        /// <summary> The UserDefined3 property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UserDefined3
        {
            get { return  GetValue<System.String>( UserDefined3Property);}
            set { SetValue(UserDefined3Property,value); }
        }

        /// <summary> The UserDefined4 property of the Entity AppCalendarSpecificDay</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> UserDefined4
        {
            get { return  GetValue<Nullable<System.Boolean>>( UserDefined4Property);}
            set { SetValue(UserDefined4Property,value); }
        }
        
        #endregion

       
        
    }
}

