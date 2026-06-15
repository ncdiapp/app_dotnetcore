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
    /// DTO class for the entity 'AppUserAppointment'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppUserAppointmentDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string UserIdProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.Int32>(o => o.UserId);
        public static readonly string SubjectProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.String>(o => o.Subject);
        public static readonly string BodyProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.String>(o => o.Body);
        public static readonly string DateStartProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.DateTime>(o => o.DateStart);
        public static readonly string DateEndProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.DateTime>(o => o.DateEnd);
        public static readonly string IsAllDayProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.Boolean>(o => o.IsAllDay);
        public static readonly string ImportanceProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.Int32>(o => o.Importance);
        public static readonly string LocationProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.String>(o => o.Location);
        public static readonly string OriginalAppointmentIdProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,Nullable<System.Int32>>(o => o.OriginalAppointmentId);
        public static readonly string UserCcProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.String>(o => o.UserCc);
        public static readonly string SystemTimeStampProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,System.Byte[]>(o => o.SystemTimeStamp);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppUserAppointmentDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppUserAppointmentDto()
        {        
        }
		
		static AppUserAppointmentDto()
        {
              
			MandatoryProperties.Add(UserIdProperty);    
			MandatoryProperties.Add(DateStartProperty);  
			MandatoryProperties.Add(DateEndProperty);  
			MandatoryProperties.Add(IsAllDayProperty);  
			MandatoryProperties.Add(ImportanceProperty);          
			                 		
               
			DictStringPropertyMaxLength.Add(SubjectProperty,300); 
			DictStringPropertyMaxLength.Add(BodyProperty,2147483647);     
			DictStringPropertyMaxLength.Add(LocationProperty,300);  
			DictStringPropertyMaxLength.Add(UserCcProperty,2000);        
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
    


        /// <summary> The UserId property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 UserId
        {
            get { return  GetValue<System.Int32>( UserIdProperty);}
            set { SetValue(UserIdProperty,value); }
        }

        /// <summary> The Subject property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Subject
        {
            get { return  GetValue<System.String>( SubjectProperty);}
            set { SetValue(SubjectProperty,value); }
        }

        /// <summary> The Body property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Body
        {
            get { return  GetValue<System.String>( BodyProperty);}
            set { SetValue(BodyProperty,value); }
        }

        /// <summary> The DateStart property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.DateTime DateStart
        {
            get { return  GetValue<System.DateTime>( DateStartProperty);}
            set { SetValue(DateStartProperty,value); }
        }

        /// <summary> The DateEnd property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.DateTime DateEnd
        {
            get { return  GetValue<System.DateTime>( DateEndProperty);}
            set { SetValue(DateEndProperty,value); }
        }

        /// <summary> The IsAllDay property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsAllDay
        {
            get { return  GetValue<System.Boolean>( IsAllDayProperty);}
            set { SetValue(IsAllDayProperty,value); }
        }

        /// <summary> The Importance property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 Importance
        {
            get { return  GetValue<System.Int32>( ImportanceProperty);}
            set { SetValue(ImportanceProperty,value); }
        }

        /// <summary> The Location property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Location
        {
            get { return  GetValue<System.String>( LocationProperty);}
            set { SetValue(LocationProperty,value); }
        }

        /// <summary> The OriginalAppointmentId property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OriginalAppointmentId
        {
            get { return  GetValue<Nullable<System.Int32>>( OriginalAppointmentIdProperty);}
            set { SetValue(OriginalAppointmentIdProperty,value); }
        }

        /// <summary> The UserCc property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String UserCc
        {
            get { return  GetValue<System.String>( UserCcProperty);}
            set { SetValue(UserCcProperty,value); }
        }

        /// <summary> The SystemTimeStamp property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Byte[] SystemTimeStamp
        {
            get { return  GetValue<System.Byte[]>( SystemTimeStampProperty);}
            set { SetValue(SystemTimeStampProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppUserAppointment</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppUserAppointment</summary>
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

