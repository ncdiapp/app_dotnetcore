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
    /// DTO class for the entity 'AppProjectOrTaskTranscation'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectOrTaskTranscationDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProejctIdProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.Int32>>(o => o.ProejctId);
        public static readonly string ProjectTaskIdProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.Int32>>(o => o.ProjectTaskId);
        public static readonly string TranscationIdProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.Int32>>(o => o.TranscationId);
        public static readonly string TransactionRidProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,System.String>(o => o.TransactionRid);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectOrTaskTranscationDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectOrTaskTranscationDto()
        {        
        }
		
		static AppProjectOrTaskTranscationDto()
        {
                      
			  
			ForeignKeyProperties.Add(ProejctIdProperty);  
			ForeignKeyProperties.Add(ProjectTaskIdProperty);  
			ForeignKeyProperties.Add(TranscationIdProperty);       		
                 
			DictStringPropertyMaxLength.Add(TransactionRidProperty,200);       
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
    


        /// <summary> The ProejctId property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProejctId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProejctIdProperty);}
            set { SetValue(ProejctIdProperty,value); }
        }

        /// <summary> The ProjectTaskId property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectTaskIdProperty);}
            set { SetValue(ProjectTaskIdProperty,value); }
        }

        /// <summary> The TranscationId property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TranscationId
        {
            get { return  GetValue<Nullable<System.Int32>>( TranscationIdProperty);}
            set { SetValue(TranscationIdProperty,value); }
        }

        /// <summary> The TransactionRid property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRid
        {
            get { return  GetValue<System.String>( TransactionRidProperty);}
            set { SetValue(TransactionRidProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectOrTaskTranscation</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectOrTaskTranscation</summary>
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

