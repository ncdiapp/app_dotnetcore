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
    /// DTO class for the entity 'AppWorkFlowStep'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppWorkFlowStepDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string WorkflowIdProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Int32>>(o => o.WorkflowId);
        public static readonly string StepCodeProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,System.String>(o => o.StepCode);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,System.String>(o => o.Description);
        public static readonly string DataModelTransacionIdProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Int32>>(o => o.DataModelTransacionId);
        public static readonly string BooleanFormulaExpressionProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,System.String>(o => o.BooleanFormulaExpression);
        public static readonly string SuccessiveStepActionIdProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Int32>>(o => o.SuccessiveStepActionId);
        public static readonly string IsDecisionStepProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Boolean>>(o => o.IsDecisionStep);
        public static readonly string DecisionStepPredictValueProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,System.String>(o => o.DecisionStepPredictValue);
        public static readonly string PathUilayoutProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,System.String>(o => o.PathUilayout);
        public static readonly string RowIdentityProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,System.Guid>(o => o.RowIdentity);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppWorkFlowStepDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppWorkFlowStepDto()
        {        
        }
		
		static AppWorkFlowStepDto()
        {
                       
			MandatoryProperties.Add(RowIdentityProperty);      
			  
			ForeignKeyProperties.Add(WorkflowIdProperty);      
			ForeignKeyProperties.Add(SuccessiveStepActionIdProperty);          		
               
			DictStringPropertyMaxLength.Add(StepCodeProperty,50); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);  
			DictStringPropertyMaxLength.Add(BooleanFormulaExpressionProperty,4000);   
			DictStringPropertyMaxLength.Add(DecisionStepPredictValueProperty,100); 
			DictStringPropertyMaxLength.Add(PathUilayoutProperty,1000);        
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
    


        /// <summary> The WorkflowId property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> WorkflowId
        {
            get { return  GetValue<Nullable<System.Int32>>( WorkflowIdProperty);}
            set { SetValue(WorkflowIdProperty,value); }
        }

        /// <summary> The StepCode property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StepCode
        {
            get { return  GetValue<System.String>( StepCodeProperty);}
            set { SetValue(StepCodeProperty,value); }
        }

        /// <summary> The Description property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The DataModelTransacionId property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DataModelTransacionId
        {
            get { return  GetValue<Nullable<System.Int32>>( DataModelTransacionIdProperty);}
            set { SetValue(DataModelTransacionIdProperty,value); }
        }

        /// <summary> The BooleanFormulaExpression property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String BooleanFormulaExpression
        {
            get { return  GetValue<System.String>( BooleanFormulaExpressionProperty);}
            set { SetValue(BooleanFormulaExpressionProperty,value); }
        }

        /// <summary> The SuccessiveStepActionId property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SuccessiveStepActionId
        {
            get { return  GetValue<Nullable<System.Int32>>( SuccessiveStepActionIdProperty);}
            set { SetValue(SuccessiveStepActionIdProperty,value); }
        }

        /// <summary> The IsDecisionStep property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDecisionStep
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDecisionStepProperty);}
            set { SetValue(IsDecisionStepProperty,value); }
        }

        /// <summary> The DecisionStepPredictValue property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String DecisionStepPredictValue
        {
            get { return  GetValue<System.String>( DecisionStepPredictValueProperty);}
            set { SetValue(DecisionStepPredictValueProperty,value); }
        }

        /// <summary> The PathUilayout property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String PathUilayout
        {
            get { return  GetValue<System.String>( PathUilayoutProperty);}
            set { SetValue(PathUilayoutProperty,value); }
        }

        /// <summary> The RowIdentity property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Guid RowIdentity
        {
            get { return  GetValue<System.Guid>( RowIdentityProperty);}
            set { SetValue(RowIdentityProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppWorkFlowStep</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppWorkFlowStep</summary>
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

