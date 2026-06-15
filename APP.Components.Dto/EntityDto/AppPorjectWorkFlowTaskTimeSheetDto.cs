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
    /// DTO class for the entity 'AppPorjectWorkFlowTaskTimeSheet'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppPorjectWorkFlowTaskTimeSheetDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProjectWorkFlowTaskIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.ProjectWorkFlowTaskId);
        public static readonly string StartTimeProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.TimeSpan>>(o => o.StartTime);
        public static readonly string EndTimeProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.TimeSpan>>(o => o.EndTime);
        public static readonly string TimeSpanProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Double>>(o => o.TimeSpan);
        public static readonly string HourByRateProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Double>>(o => o.HourByRate);
        public static readonly string ApprovedByIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.ApprovedById);
        public static readonly string ApprovedByDateProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.DateTime>>(o => o.ApprovedByDate);
        public static readonly string CommentsProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,System.String>(o => o.Comments);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string DateIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.DateId);
        public static readonly string ResourceUserIdProperty = ObjectInfoHelper.GetName<AppPorjectWorkFlowTaskTimeSheetDto ,Nullable<System.Int32>>(o => o.ResourceUserId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppPorjectWorkFlowTaskTimeSheetDto()
        {        
        }
		
		static AppPorjectWorkFlowTaskTimeSheetDto()
        {
                            
			  
			ForeignKeyProperties.Add(ProjectWorkFlowTaskIdProperty);               		
                     
			DictStringPropertyMaxLength.Add(CommentsProperty,500);         
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
    


        /// <summary> The ProjectWorkFlowTaskId property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectWorkFlowTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectWorkFlowTaskIdProperty);}
            set { SetValue(ProjectWorkFlowTaskIdProperty,value); }
        }

        /// <summary> The StartTime property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.TimeSpan> StartTime
        {
            get { return  GetValue<Nullable<System.TimeSpan>>( StartTimeProperty);}
            set { SetValue(StartTimeProperty,value); }
        }

        /// <summary> The EndTime property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.TimeSpan> EndTime
        {
            get { return  GetValue<Nullable<System.TimeSpan>>( EndTimeProperty);}
            set { SetValue(EndTimeProperty,value); }
        }

        /// <summary> The TimeSpan property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> TimeSpan
        {
            get { return  GetValue<Nullable<System.Double>>( TimeSpanProperty);}
            set { SetValue(TimeSpanProperty,value); }
        }

        /// <summary> The HourByRate property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> HourByRate
        {
            get { return  GetValue<Nullable<System.Double>>( HourByRateProperty);}
            set { SetValue(HourByRateProperty,value); }
        }

        /// <summary> The ApprovedById property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ApprovedById
        {
            get { return  GetValue<Nullable<System.Int32>>( ApprovedByIdProperty);}
            set { SetValue(ApprovedByIdProperty,value); }
        }

        /// <summary> The ApprovedByDate property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> ApprovedByDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( ApprovedByDateProperty);}
            set { SetValue(ApprovedByDateProperty,value); }
        }

        /// <summary> The Comments property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Comments
        {
            get { return  GetValue<System.String>( CommentsProperty);}
            set { SetValue(CommentsProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The DateId property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DateId
        {
            get { return  GetValue<Nullable<System.Int32>>( DateIdProperty);}
            set { SetValue(DateIdProperty,value); }
        }

        /// <summary> The ResourceUserId property of the Entity AppPorjectWorkFlowTaskTimeSheet</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ResourceUserId
        {
            get { return  GetValue<Nullable<System.Int32>>( ResourceUserIdProperty);}
            set { SetValue(ResourceUserIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

