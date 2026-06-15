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
    /// DTO class for the entity 'AppEmployee'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppEmployeeDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string HireDateProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.DateTime>>(o => o.HireDate);
        public static readonly string VacationHoursProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Int32>>(o => o.VacationHours);
        public static readonly string GenderProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Int32>>(o => o.Gender);
        public static readonly string SalariedFlagProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Boolean>>(o => o.SalariedFlag);
        public static readonly string SickLeaveHoursProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Int32>>(o => o.SickLeaveHours);
        public static readonly string CurrentFlagProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Boolean>>(o => o.CurrentFlag);
        public static readonly string JobTitleProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,System.String>(o => o.JobTitle);
        public static readonly string BirthDateProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.DateTime>>(o => o.BirthDate);
        public static readonly string MaritalStatusProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Boolean>>(o => o.MaritalStatus);
        public static readonly string NationalIdnumberProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,System.String>(o => o.NationalIdnumber);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppEmployeeDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppEmployeeDto()
        {        
        }
		
		static AppEmployeeDto()
        {
                            
			                		
                    
			DictStringPropertyMaxLength.Add(JobTitleProperty,50);   
			DictStringPropertyMaxLength.Add(NationalIdnumberProperty,50);       
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
    


        /// <summary> The HireDate property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> HireDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( HireDateProperty);}
            set { SetValue(HireDateProperty,value); }
        }

        /// <summary> The VacationHours property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> VacationHours
        {
            get { return  GetValue<Nullable<System.Int32>>( VacationHoursProperty);}
            set { SetValue(VacationHoursProperty,value); }
        }

        /// <summary> The Gender property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Gender
        {
            get { return  GetValue<Nullable<System.Int32>>( GenderProperty);}
            set { SetValue(GenderProperty,value); }
        }

        /// <summary> The SalariedFlag property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> SalariedFlag
        {
            get { return  GetValue<Nullable<System.Boolean>>( SalariedFlagProperty);}
            set { SetValue(SalariedFlagProperty,value); }
        }

        /// <summary> The SickLeaveHours property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SickLeaveHours
        {
            get { return  GetValue<Nullable<System.Int32>>( SickLeaveHoursProperty);}
            set { SetValue(SickLeaveHoursProperty,value); }
        }

        /// <summary> The CurrentFlag property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> CurrentFlag
        {
            get { return  GetValue<Nullable<System.Boolean>>( CurrentFlagProperty);}
            set { SetValue(CurrentFlagProperty,value); }
        }

        /// <summary> The JobTitle property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String JobTitle
        {
            get { return  GetValue<System.String>( JobTitleProperty);}
            set { SetValue(JobTitleProperty,value); }
        }

        /// <summary> The BirthDate property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> BirthDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( BirthDateProperty);}
            set { SetValue(BirthDateProperty,value); }
        }

        /// <summary> The MaritalStatus property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> MaritalStatus
        {
            get { return  GetValue<Nullable<System.Boolean>>( MaritalStatusProperty);}
            set { SetValue(MaritalStatusProperty,value); }
        }

        /// <summary> The NationalIdnumber property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String NationalIdnumber
        {
            get { return  GetValue<System.String>( NationalIdnumberProperty);}
            set { SetValue(NationalIdnumberProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppEmployee</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppEmployee</summary>
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

