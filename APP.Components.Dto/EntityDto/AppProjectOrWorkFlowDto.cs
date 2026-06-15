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
    /// DTO class for the entity 'AppProjectOrWorkFlow'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectOrWorkFlowDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.String>(o => o.Description);
        public static readonly string ProjectDirectionIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.Int32>(o => o.ProjectDirectionId);
        public static readonly string ParentProjectIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.ParentProjectId);
        public static readonly string DateModelStartProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DateModelStart);
        public static readonly string DateModelEndProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DateModelEnd);
        public static readonly string DatePlannedStartProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DatePlannedStart);
        public static readonly string DatePlannedEndProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DatePlannedEnd);
        public static readonly string DateActualStartProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DateActualStart);
        public static readonly string DateActualEndProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DateActualEnd);
        public static readonly string DateAbortedProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.DateAborted);
        public static readonly string ProjectPathIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.Int32>(o => o.ProjectPathId);
        public static readonly string IsPredefinedProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.Boolean>(o => o.IsPredefined);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionRidProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.String>(o => o.TransactionRid);
        public static readonly string TimeUnitProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.TimeUnit);
        public static readonly string ProjectWorkflowTypeProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.ProjectWorkflowType);
        public static readonly string ProjectTeamIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.ProjectTeamId);
        public static readonly string ProjectLeaderIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.ProjectLeaderId);
        public static readonly string ProjectSumaryTaskIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.ProjectSumaryTaskId);
        public static readonly string ProjectModelBugestCostProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.ProjectModelBugestCost);
        public static readonly string ProjectPlannedCostProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.ProjectPlannedCost);
        public static readonly string ProjectActualCostProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.ProjectActualCost);
        public static readonly string CurrencyIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.CurrencyId);
        public static readonly string CompanyIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.CompanyId);
        public static readonly string IsNeedBudgetProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Boolean>>(o => o.IsNeedBudget);
        public static readonly string DurationProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.Duration);
        public static readonly string DisplayLayoutTypeProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.DisplayLayoutType);
        public static readonly string EmPrivacyProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.EmPrivacy);
        public static readonly string ParticipatedDmainIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,System.String>(o => o.ParticipatedDmainId);
        public static readonly string EmCostTypeProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.EmCostType);
        public static readonly string CompletedPercentProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.CompletedPercent);
        public static readonly string RequireTaskCompletedPercentAsCompleProjectProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.RequireTaskCompletedPercentAsCompleProject);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string RuntimeOriginalProjectIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.RuntimeOriginalProjectId);
        public static readonly string DefaultGanttDisplayUnitProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.DefaultGanttDisplayUnit);
        public static readonly string IsChildProjectAllowParentTtrickleDownProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Boolean>>(o => o.IsChildProjectAllowParentTtrickleDown);
        public static readonly string IsChildProjectAllowChildBubbleUpParentProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Boolean>>(o => o.IsChildProjectAllowChildBubbleUpParent);
        public static readonly string ProjectLogoImageIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.ProjectLogoImageId);
        public static readonly string PlannedWorkHoursProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.PlannedWorkHours);
        public static readonly string ActualWorkHoursProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.ActualWorkHours);
        public static readonly string PlannedResourceCostProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.PlannedResourceCost);
        public static readonly string ActualResourceCostProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Double>>(o => o.ActualResourceCost);
        public static readonly string SaasApplicationIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.SaasApplicationId);
        public static readonly string TransactionGroupIdProperty = ObjectInfoHelper.GetName<AppProjectOrWorkFlowDto ,Nullable<System.Int32>>(o => o.TransactionGroupId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectOrWorkFlowDto()
        {        
        }
		
		static AppProjectOrWorkFlowDto()
        {
              
			MandatoryProperties.Add(NameProperty);   
			MandatoryProperties.Add(ProjectDirectionIdProperty);          
			MandatoryProperties.Add(ProjectPathIdProperty);  
			MandatoryProperties.Add(IsPredefinedProperty);                                      
			     
			ForeignKeyProperties.Add(ParentProjectIdProperty);            
			ForeignKeyProperties.Add(TransactionIdProperty);      
			ForeignKeyProperties.Add(ProjectLeaderIdProperty);  
			ForeignKeyProperties.Add(ProjectSumaryTaskIdProperty);     
			ForeignKeyProperties.Add(CurrencyIdProperty);                
			ForeignKeyProperties.Add(RuntimeOriginalProjectIdProperty);          
			ForeignKeyProperties.Add(SaasApplicationIdProperty);  		
              
			DictStringPropertyMaxLength.Add(NameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);              
			DictStringPropertyMaxLength.Add(TransactionRidProperty,200);               
			DictStringPropertyMaxLength.Add(ParticipatedDmainIdProperty,500);                     
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
    


        /// <summary> The Name property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The ProjectDirectionId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ProjectDirectionId
        {
            get { return  GetValue<System.Int32>( ProjectDirectionIdProperty);}
            set { SetValue(ProjectDirectionIdProperty,value); }
        }

        /// <summary> The ParentProjectId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ParentProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( ParentProjectIdProperty);}
            set { SetValue(ParentProjectIdProperty,value); }
        }

        /// <summary> The DateModelStart property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateModelStart
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateModelStartProperty);}
            set { SetValue(DateModelStartProperty,value); }
        }

        /// <summary> The DateModelEnd property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateModelEnd
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateModelEndProperty);}
            set { SetValue(DateModelEndProperty,value); }
        }

        /// <summary> The DatePlannedStart property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DatePlannedStart
        {
            get { return  GetValue<Nullable<System.DateTime>>( DatePlannedStartProperty);}
            set { SetValue(DatePlannedStartProperty,value); }
        }

        /// <summary> The DatePlannedEnd property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DatePlannedEnd
        {
            get { return  GetValue<Nullable<System.DateTime>>( DatePlannedEndProperty);}
            set { SetValue(DatePlannedEndProperty,value); }
        }

        /// <summary> The DateActualStart property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateActualStart
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateActualStartProperty);}
            set { SetValue(DateActualStartProperty,value); }
        }

        /// <summary> The DateActualEnd property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateActualEnd
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateActualEndProperty);}
            set { SetValue(DateActualEndProperty,value); }
        }

        /// <summary> The DateAborted property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateAborted
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateAbortedProperty);}
            set { SetValue(DateAbortedProperty,value); }
        }

        /// <summary> The ProjectPathId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Int32 ProjectPathId
        {
            get { return  GetValue<System.Int32>( ProjectPathIdProperty);}
            set { SetValue(ProjectPathIdProperty,value); }
        }

        /// <summary> The IsPredefined property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.Boolean IsPredefined
        {
            get { return  GetValue<System.Boolean>( IsPredefinedProperty);}
            set { SetValue(IsPredefinedProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionRid property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRid
        {
            get { return  GetValue<System.String>( TransactionRidProperty);}
            set { SetValue(TransactionRidProperty,value); }
        }

        /// <summary> The TimeUnit property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TimeUnit
        {
            get { return  GetValue<Nullable<System.Int32>>( TimeUnitProperty);}
            set { SetValue(TimeUnitProperty,value); }
        }

        /// <summary> The ProjectWorkflowType property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectWorkflowType
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectWorkflowTypeProperty);}
            set { SetValue(ProjectWorkflowTypeProperty,value); }
        }

        /// <summary> The ProjectTeamId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectTeamId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectTeamIdProperty);}
            set { SetValue(ProjectTeamIdProperty,value); }
        }

        /// <summary> The ProjectLeaderId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectLeaderId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectLeaderIdProperty);}
            set { SetValue(ProjectLeaderIdProperty,value); }
        }

        /// <summary> The ProjectSumaryTaskId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectSumaryTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectSumaryTaskIdProperty);}
            set { SetValue(ProjectSumaryTaskIdProperty,value); }
        }

        /// <summary> The ProjectModelBugestCost property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ProjectModelBugestCost
        {
            get { return  GetValue<Nullable<System.Double>>( ProjectModelBugestCostProperty);}
            set { SetValue(ProjectModelBugestCostProperty,value); }
        }

        /// <summary> The ProjectPlannedCost property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ProjectPlannedCost
        {
            get { return  GetValue<Nullable<System.Double>>( ProjectPlannedCostProperty);}
            set { SetValue(ProjectPlannedCostProperty,value); }
        }

        /// <summary> The ProjectActualCost property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ProjectActualCost
        {
            get { return  GetValue<Nullable<System.Double>>( ProjectActualCostProperty);}
            set { SetValue(ProjectActualCostProperty,value); }
        }

        /// <summary> The CurrencyId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CurrencyId
        {
            get { return  GetValue<Nullable<System.Int32>>( CurrencyIdProperty);}
            set { SetValue(CurrencyIdProperty,value); }
        }

        /// <summary> The CompanyId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( CompanyIdProperty);}
            set { SetValue(CompanyIdProperty,value); }
        }

        /// <summary> The IsNeedBudget property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsNeedBudget
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsNeedBudgetProperty);}
            set { SetValue(IsNeedBudgetProperty,value); }
        }

        /// <summary> The Duration property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> Duration
        {
            get { return  GetValue<Nullable<System.Double>>( DurationProperty);}
            set { SetValue(DurationProperty,value); }
        }

        /// <summary> The DisplayLayoutType property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DisplayLayoutType
        {
            get { return  GetValue<Nullable<System.Int32>>( DisplayLayoutTypeProperty);}
            set { SetValue(DisplayLayoutTypeProperty,value); }
        }

        /// <summary> The EmPrivacy property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmPrivacy
        {
            get { return  GetValue<Nullable<System.Int32>>( EmPrivacyProperty);}
            set { SetValue(EmPrivacyProperty,value); }
        }

        /// <summary> The ParticipatedDmainId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String ParticipatedDmainId
        {
            get { return  GetValue<System.String>( ParticipatedDmainIdProperty);}
            set { SetValue(ParticipatedDmainIdProperty,value); }
        }

        /// <summary> The EmCostType property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmCostType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmCostTypeProperty);}
            set { SetValue(EmCostTypeProperty,value); }
        }

        /// <summary> The CompletedPercent property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> CompletedPercent
        {
            get { return  GetValue<Nullable<System.Double>>( CompletedPercentProperty);}
            set { SetValue(CompletedPercentProperty,value); }
        }

        /// <summary> The RequireTaskCompletedPercentAsCompleProject property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> RequireTaskCompletedPercentAsCompleProject
        {
            get { return  GetValue<Nullable<System.Double>>( RequireTaskCompletedPercentAsCompleProjectProperty);}
            set { SetValue(RequireTaskCompletedPercentAsCompleProjectProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The RuntimeOriginalProjectId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> RuntimeOriginalProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( RuntimeOriginalProjectIdProperty);}
            set { SetValue(RuntimeOriginalProjectIdProperty,value); }
        }

        /// <summary> The DefaultGanttDisplayUnit property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DefaultGanttDisplayUnit
        {
            get { return  GetValue<Nullable<System.Int32>>( DefaultGanttDisplayUnitProperty);}
            set { SetValue(DefaultGanttDisplayUnitProperty,value); }
        }

        /// <summary> The IsChildProjectAllowParentTtrickleDown property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsChildProjectAllowParentTtrickleDown
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsChildProjectAllowParentTtrickleDownProperty);}
            set { SetValue(IsChildProjectAllowParentTtrickleDownProperty,value); }
        }

        /// <summary> The IsChildProjectAllowChildBubbleUpParent property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsChildProjectAllowChildBubbleUpParent
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsChildProjectAllowChildBubbleUpParentProperty);}
            set { SetValue(IsChildProjectAllowChildBubbleUpParentProperty,value); }
        }

        /// <summary> The ProjectLogoImageId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectLogoImageId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectLogoImageIdProperty);}
            set { SetValue(ProjectLogoImageIdProperty,value); }
        }

        /// <summary> The PlannedWorkHours property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PlannedWorkHours
        {
            get { return  GetValue<Nullable<System.Double>>( PlannedWorkHoursProperty);}
            set { SetValue(PlannedWorkHoursProperty,value); }
        }

        /// <summary> The ActualWorkHours property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ActualWorkHours
        {
            get { return  GetValue<Nullable<System.Double>>( ActualWorkHoursProperty);}
            set { SetValue(ActualWorkHoursProperty,value); }
        }

        /// <summary> The PlannedResourceCost property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PlannedResourceCost
        {
            get { return  GetValue<Nullable<System.Double>>( PlannedResourceCostProperty);}
            set { SetValue(PlannedResourceCostProperty,value); }
        }

        /// <summary> The ActualResourceCost property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ActualResourceCost
        {
            get { return  GetValue<Nullable<System.Double>>( ActualResourceCostProperty);}
            set { SetValue(ActualResourceCostProperty,value); }
        }

        /// <summary> The SaasApplicationId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SaasApplicationId
        {
            get { return  GetValue<Nullable<System.Int32>>( SaasApplicationIdProperty);}
            set { SetValue(SaasApplicationIdProperty,value); }
        }

        /// <summary> The TransactionGroupId property of the Entity AppProjectOrWorkFlow</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionGroupId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionGroupIdProperty);}
            set { SetValue(TransactionGroupIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

