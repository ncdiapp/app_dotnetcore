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
    /// DTO class for the entity 'AppProjectWorkFlowTask'.
    /// </summary>
    
    [DataContract(Namespace = ContractNamespaces.Dto)]
    public partial class AppProjectWorkFlowTaskDto : NotifyDataErrorDto 
    {

         #region Static Name Properties Declaration
  

        public static readonly string ProjectSectionIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.ProjectSectionId);
        public static readonly string ProjectIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.ProjectId);
        public static readonly string NameProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,System.String>(o => o.Name);
        public static readonly string DescriptionProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,System.String>(o => o.Description);
        public static readonly string DateModelStartProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.DateModelStart);
        public static readonly string DateModelEndProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.DateModelEnd);
        public static readonly string DatePlannedStartProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.DatePlannedStart);
        public static readonly string DatePlannedEndProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.DatePlannedEnd);
        public static readonly string DateActualStartProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.DateActualStart);
        public static readonly string DateActualEndProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.DateActualEnd);
        public static readonly string CompletedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.CompletedById);
        public static readonly string ActivityIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.ActivityId);
        public static readonly string PhaseIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.PhaseId);
        public static readonly string IsAutoStartProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsAutoStart);
        public static readonly string SeverityIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.SeverityId);
        public static readonly string NotesProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,System.String>(o => o.Notes);
        public static readonly string SortProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.Sort);
        public static readonly string IsFixedPlannedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsFixedPlannedDate);
        public static readonly string NbDaysProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Decimal>>(o => o.NbDays);
        public static readonly string TimingDaysProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.TimingDays);
        public static readonly string NbHoursProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.NbHours);
        public static readonly string IsAutoCompleteProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsAutoComplete);
        public static readonly string IsMilestoneProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsMilestone);
        public static readonly string ProjectActivityStatusIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.ProjectActivityStatusId);
        public static readonly string WeightProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.Weight);
        public static readonly string ToleranceDaysProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.ToleranceDays);
        public static readonly string IsDependentProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsDependent);
        public static readonly string OriginalLibProjectActivityIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.OriginalLibProjectActivityId);
        public static readonly string RowIdentityProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Guid>>(o => o.RowIdentity);
        public static readonly string IsActiveProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsActive);
        public static readonly string MainTaskIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.MainTaskId);
        public static readonly string UnitOfTimeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.UnitOfTime);
        public static readonly string AmountOfTimeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.AmountOfTime);
        public static readonly string ActrualNeedAmountHoursProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.ActrualNeedAmountHours);
        public static readonly string DiagramShapeTypeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.DiagramShapeType);
        public static readonly string StageTypeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.StageType);
        public static readonly string StageStatusFlagProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.StageStatusFlag);
        public static readonly string StageUilayoutProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,System.String>(o => o.StageUilayout);
        public static readonly string IsProjectSumaryTaskProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsProjectSumaryTask);
        public static readonly string TaskPlannedCostProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.TaskPlannedCost);
        public static readonly string TaskActualCostProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.TaskActualCost);
        public static readonly string IsBillableProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Boolean>>(o => o.IsBillable);
        public static readonly string BillRateByHourProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.BillRateByHour);
        public static readonly string CurrencyCodeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,System.String>(o => o.CurrencyCode);
        public static readonly string EmPriorityProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.EmPriority);
        public static readonly string EmTaskTypeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.EmTaskType);
        public static readonly string EmCostTypeProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.EmCostType);
        public static readonly string ProjectRoleIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.ProjectRoleId);
        public static readonly string TaskOwnerIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.TaskOwnerId);
        public static readonly string TransactionIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.TransactionId);
        public static readonly string TransactionRidProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,System.String>(o => o.TransactionRid);
        public static readonly string EmAppTaskOwnerDeliverPhaseProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.EmAppTaskOwnerDeliverPhase);
        public static readonly string RequirChildrenCompletedPercentAsTaskCompleProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.RequirChildrenCompletedPercentAsTaskComple);
        public static readonly string CompletedPercentProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.CompletedPercent);
        public static readonly string AppCreatedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.AppCreatedById);
        public static readonly string AppCreatedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.AppCreatedDate);
        public static readonly string AppModifiedDateProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.DateTime>>(o => o.AppModifiedDate);
        public static readonly string AppModifiedByIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.AppModifiedById);
        public static readonly string AppCreatedByCompanyIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.AppCreatedByCompanyId);
        public static readonly string TimeSheetEntryMethodProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.TimeSheetEntryMethod);
        public static readonly string PlannedWorkHoursProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.PlannedWorkHours);
        public static readonly string ActualWorkHoursProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.ActualWorkHours);
        public static readonly string PlannedResourceCostProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.PlannedResourceCost);
        public static readonly string ActualResourceCostProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Double>>(o => o.ActualResourceCost);
        public static readonly string ProgressIdProperty = ObjectInfoHelper.GetName<AppProjectWorkFlowTaskDto ,Nullable<System.Int32>>(o => o.ProgressId);
     
        private static readonly List<string> MandatoryProperties = new List<string>();
        private static readonly List<string> ForeignKeyProperties = new List<string>();
        private static readonly Dictionary<string, int> DictStringPropertyMaxLength = new Dictionary<string, int>();
		
        #endregion

        public AppProjectWorkFlowTaskDto()
        {        
        }
		
		static AppProjectWorkFlowTaskDto()
        {
                                                                              
			   
			ForeignKeyProperties.Add(ProjectIdProperty);                              
			ForeignKeyProperties.Add(MainTaskIdProperty);                    
			ForeignKeyProperties.Add(TransactionIdProperty);                		
                
			DictStringPropertyMaxLength.Add(NameProperty,200); 
			DictStringPropertyMaxLength.Add(DescriptionProperty,500);            
			DictStringPropertyMaxLength.Add(NotesProperty,4000);                      
			DictStringPropertyMaxLength.Add(StageUilayoutProperty,1000);      
			DictStringPropertyMaxLength.Add(CurrencyCodeProperty,10);       
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
    


        /// <summary> The ProjectSectionId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectSectionId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectSectionIdProperty);}
            set { SetValue(ProjectSectionIdProperty,value); }
        }

        /// <summary> The ProjectId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectIdProperty);}
            set { SetValue(ProjectIdProperty,value); }
        }

        /// <summary> The Name property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Name
        {
            get { return  GetValue<System.String>( NameProperty);}
            set { SetValue(NameProperty,value); }
        }

        /// <summary> The Description property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Description
        {
            get { return  GetValue<System.String>( DescriptionProperty);}
            set { SetValue(DescriptionProperty,value); }
        }

        /// <summary> The DateModelStart property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateModelStart
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateModelStartProperty);}
            set { SetValue(DateModelStartProperty,value); }
        }

        /// <summary> The DateModelEnd property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateModelEnd
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateModelEndProperty);}
            set { SetValue(DateModelEndProperty,value); }
        }

        /// <summary> The DatePlannedStart property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DatePlannedStart
        {
            get { return  GetValue<Nullable<System.DateTime>>( DatePlannedStartProperty);}
            set { SetValue(DatePlannedStartProperty,value); }
        }

        /// <summary> The DatePlannedEnd property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DatePlannedEnd
        {
            get { return  GetValue<Nullable<System.DateTime>>( DatePlannedEndProperty);}
            set { SetValue(DatePlannedEndProperty,value); }
        }

        /// <summary> The DateActualStart property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateActualStart
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateActualStartProperty);}
            set { SetValue(DateActualStartProperty,value); }
        }

        /// <summary> The DateActualEnd property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> DateActualEnd
        {
            get { return  GetValue<Nullable<System.DateTime>>( DateActualEndProperty);}
            set { SetValue(DateActualEndProperty,value); }
        }

        /// <summary> The CompletedById property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> CompletedById
        {
            get { return  GetValue<Nullable<System.Int32>>( CompletedByIdProperty);}
            set { SetValue(CompletedByIdProperty,value); }
        }

        /// <summary> The ActivityId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ActivityId
        {
            get { return  GetValue<Nullable<System.Int32>>( ActivityIdProperty);}
            set { SetValue(ActivityIdProperty,value); }
        }

        /// <summary> The PhaseId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> PhaseId
        {
            get { return  GetValue<Nullable<System.Int32>>( PhaseIdProperty);}
            set { SetValue(PhaseIdProperty,value); }
        }

        /// <summary> The IsAutoStart property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAutoStart
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAutoStartProperty);}
            set { SetValue(IsAutoStartProperty,value); }
        }

        /// <summary> The SeverityId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> SeverityId
        {
            get { return  GetValue<Nullable<System.Int32>>( SeverityIdProperty);}
            set { SetValue(SeverityIdProperty,value); }
        }

        /// <summary> The Notes property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String Notes
        {
            get { return  GetValue<System.String>( NotesProperty);}
            set { SetValue(NotesProperty,value); }
        }

        /// <summary> The Sort property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> Sort
        {
            get { return  GetValue<Nullable<System.Int32>>( SortProperty);}
            set { SetValue(SortProperty,value); }
        }

        /// <summary> The IsFixedPlannedDate property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsFixedPlannedDate
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsFixedPlannedDateProperty);}
            set { SetValue(IsFixedPlannedDateProperty,value); }
        }

        /// <summary> The NbDays property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Decimal> NbDays
        {
            get { return  GetValue<Nullable<System.Decimal>>( NbDaysProperty);}
            set { SetValue(NbDaysProperty,value); }
        }

        /// <summary> The TimingDays property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> TimingDays
        {
            get { return  GetValue<Nullable<System.Double>>( TimingDaysProperty);}
            set { SetValue(TimingDaysProperty,value); }
        }

        /// <summary> The NbHours property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> NbHours
        {
            get { return  GetValue<Nullable<System.Double>>( NbHoursProperty);}
            set { SetValue(NbHoursProperty,value); }
        }

        /// <summary> The IsAutoComplete property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsAutoComplete
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsAutoCompleteProperty);}
            set { SetValue(IsAutoCompleteProperty,value); }
        }

        /// <summary> The IsMilestone property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsMilestone
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsMilestoneProperty);}
            set { SetValue(IsMilestoneProperty,value); }
        }

        /// <summary> The ProjectActivityStatusId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectActivityStatusId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectActivityStatusIdProperty);}
            set { SetValue(ProjectActivityStatusIdProperty,value); }
        }

        /// <summary> The Weight property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> Weight
        {
            get { return  GetValue<Nullable<System.Double>>( WeightProperty);}
            set { SetValue(WeightProperty,value); }
        }

        /// <summary> The ToleranceDays property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ToleranceDays
        {
            get { return  GetValue<Nullable<System.Double>>( ToleranceDaysProperty);}
            set { SetValue(ToleranceDaysProperty,value); }
        }

        /// <summary> The IsDependent property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsDependent
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsDependentProperty);}
            set { SetValue(IsDependentProperty,value); }
        }

        /// <summary> The OriginalLibProjectActivityId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> OriginalLibProjectActivityId
        {
            get { return  GetValue<Nullable<System.Int32>>( OriginalLibProjectActivityIdProperty);}
            set { SetValue(OriginalLibProjectActivityIdProperty,value); }
        }

        /// <summary> The RowIdentity property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Guid> RowIdentity
        {
            get { return  GetValue<Nullable<System.Guid>>( RowIdentityProperty);}
            set { SetValue(RowIdentityProperty,value); }
        }

        /// <summary> The IsActive property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsActive
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsActiveProperty);}
            set { SetValue(IsActiveProperty,value); }
        }

        /// <summary> The MainTaskId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> MainTaskId
        {
            get { return  GetValue<Nullable<System.Int32>>( MainTaskIdProperty);}
            set { SetValue(MainTaskIdProperty,value); }
        }

        /// <summary> The UnitOfTime property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> UnitOfTime
        {
            get { return  GetValue<Nullable<System.Int32>>( UnitOfTimeProperty);}
            set { SetValue(UnitOfTimeProperty,value); }
        }

        /// <summary> The AmountOfTime property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> AmountOfTime
        {
            get { return  GetValue<Nullable<System.Double>>( AmountOfTimeProperty);}
            set { SetValue(AmountOfTimeProperty,value); }
        }

        /// <summary> The ActrualNeedAmountHours property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ActrualNeedAmountHours
        {
            get { return  GetValue<Nullable<System.Double>>( ActrualNeedAmountHoursProperty);}
            set { SetValue(ActrualNeedAmountHoursProperty,value); }
        }

        /// <summary> The DiagramShapeType property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> DiagramShapeType
        {
            get { return  GetValue<Nullable<System.Int32>>( DiagramShapeTypeProperty);}
            set { SetValue(DiagramShapeTypeProperty,value); }
        }

        /// <summary> The StageType property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> StageType
        {
            get { return  GetValue<Nullable<System.Int32>>( StageTypeProperty);}
            set { SetValue(StageTypeProperty,value); }
        }

        /// <summary> The StageStatusFlag property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> StageStatusFlag
        {
            get { return  GetValue<Nullable<System.Int32>>( StageStatusFlagProperty);}
            set { SetValue(StageStatusFlagProperty,value); }
        }

        /// <summary> The StageUilayout property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String StageUilayout
        {
            get { return  GetValue<System.String>( StageUilayoutProperty);}
            set { SetValue(StageUilayoutProperty,value); }
        }

        /// <summary> The IsProjectSumaryTask property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsProjectSumaryTask
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsProjectSumaryTaskProperty);}
            set { SetValue(IsProjectSumaryTaskProperty,value); }
        }

        /// <summary> The TaskPlannedCost property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> TaskPlannedCost
        {
            get { return  GetValue<Nullable<System.Double>>( TaskPlannedCostProperty);}
            set { SetValue(TaskPlannedCostProperty,value); }
        }

        /// <summary> The TaskActualCost property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> TaskActualCost
        {
            get { return  GetValue<Nullable<System.Double>>( TaskActualCostProperty);}
            set { SetValue(TaskActualCostProperty,value); }
        }

        /// <summary> The IsBillable property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Boolean> IsBillable
        {
            get { return  GetValue<Nullable<System.Boolean>>( IsBillableProperty);}
            set { SetValue(IsBillableProperty,value); }
        }

        /// <summary> The BillRateByHour property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> BillRateByHour
        {
            get { return  GetValue<Nullable<System.Double>>( BillRateByHourProperty);}
            set { SetValue(BillRateByHourProperty,value); }
        }

        /// <summary> The CurrencyCode property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String CurrencyCode
        {
            get { return  GetValue<System.String>( CurrencyCodeProperty);}
            set { SetValue(CurrencyCodeProperty,value); }
        }

        /// <summary> The EmPriority property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmPriority
        {
            get { return  GetValue<Nullable<System.Int32>>( EmPriorityProperty);}
            set { SetValue(EmPriorityProperty,value); }
        }

        /// <summary> The EmTaskType property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmTaskType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmTaskTypeProperty);}
            set { SetValue(EmTaskTypeProperty,value); }
        }

        /// <summary> The EmCostType property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmCostType
        {
            get { return  GetValue<Nullable<System.Int32>>( EmCostTypeProperty);}
            set { SetValue(EmCostTypeProperty,value); }
        }

        /// <summary> The ProjectRoleId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProjectRoleId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProjectRoleIdProperty);}
            set { SetValue(ProjectRoleIdProperty,value); }
        }

        /// <summary> The TaskOwnerId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TaskOwnerId
        {
            get { return  GetValue<Nullable<System.Int32>>( TaskOwnerIdProperty);}
            set { SetValue(TaskOwnerIdProperty,value); }
        }

        /// <summary> The TransactionId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TransactionId
        {
            get { return  GetValue<Nullable<System.Int32>>( TransactionIdProperty);}
            set { SetValue(TransactionIdProperty,value); }
        }

        /// <summary> The TransactionRid property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  System.String TransactionRid
        {
            get { return  GetValue<System.String>( TransactionRidProperty);}
            set { SetValue(TransactionRidProperty,value); }
        }

        /// <summary> The EmAppTaskOwnerDeliverPhase property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> EmAppTaskOwnerDeliverPhase
        {
            get { return  GetValue<Nullable<System.Int32>>( EmAppTaskOwnerDeliverPhaseProperty);}
            set { SetValue(EmAppTaskOwnerDeliverPhaseProperty,value); }
        }

        /// <summary> The RequirChildrenCompletedPercentAsTaskComple property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> RequirChildrenCompletedPercentAsTaskComple
        {
            get { return  GetValue<Nullable<System.Double>>( RequirChildrenCompletedPercentAsTaskCompleProperty);}
            set { SetValue(RequirChildrenCompletedPercentAsTaskCompleProperty,value); }
        }

        /// <summary> The CompletedPercent property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> CompletedPercent
        {
            get { return  GetValue<Nullable<System.Double>>( CompletedPercentProperty);}
            set { SetValue(CompletedPercentProperty,value); }
        }

        /// <summary> The AppCreatedById property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByIdProperty);}
            set { SetValue(AppCreatedByIdProperty,value); }
        }

        /// <summary> The AppCreatedDate property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppCreatedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppCreatedDateProperty);}
            set { SetValue(AppCreatedDateProperty,value); }
        }

        /// <summary> The AppModifiedDate property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.DateTime> AppModifiedDate
        {
            get { return  GetValue<Nullable<System.DateTime>>( AppModifiedDateProperty);}
            set { SetValue(AppModifiedDateProperty,value); }
        }

        /// <summary> The AppModifiedById property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppModifiedById
        {
            get { return  GetValue<Nullable<System.Int32>>( AppModifiedByIdProperty);}
            set { SetValue(AppModifiedByIdProperty,value); }
        }

        /// <summary> The AppCreatedByCompanyId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> AppCreatedByCompanyId
        {
            get { return  GetValue<Nullable<System.Int32>>( AppCreatedByCompanyIdProperty);}
            set { SetValue(AppCreatedByCompanyIdProperty,value); }
        }

        /// <summary> The TimeSheetEntryMethod property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> TimeSheetEntryMethod
        {
            get { return  GetValue<Nullable<System.Int32>>( TimeSheetEntryMethodProperty);}
            set { SetValue(TimeSheetEntryMethodProperty,value); }
        }

        /// <summary> The PlannedWorkHours property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PlannedWorkHours
        {
            get { return  GetValue<Nullable<System.Double>>( PlannedWorkHoursProperty);}
            set { SetValue(PlannedWorkHoursProperty,value); }
        }

        /// <summary> The ActualWorkHours property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ActualWorkHours
        {
            get { return  GetValue<Nullable<System.Double>>( ActualWorkHoursProperty);}
            set { SetValue(ActualWorkHoursProperty,value); }
        }

        /// <summary> The PlannedResourceCost property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> PlannedResourceCost
        {
            get { return  GetValue<Nullable<System.Double>>( PlannedResourceCostProperty);}
            set { SetValue(PlannedResourceCostProperty,value); }
        }

        /// <summary> The ActualResourceCost property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Double> ActualResourceCost
        {
            get { return  GetValue<Nullable<System.Double>>( ActualResourceCostProperty);}
            set { SetValue(ActualResourceCostProperty,value); }
        }

        /// <summary> The ProgressId property of the Entity AppProjectWorkFlowTask</summary>
        [DataMember(EmitDefaultValue=false)]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public  Nullable<System.Int32> ProgressId
        {
            get { return  GetValue<Nullable<System.Int32>>( ProgressIdProperty);}
            set { SetValue(ProgressIdProperty,value); }
        }
        
        #endregion

       
        
    }
}

