using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

using APP.Framework;
using APP.Framework.Collections;
using APP.LBL.EntityClasses;
using APP.Components.EntityDto;



namespace APP.Components.EntityConverter
{
    /// <summary>
    /// Convert Properties between  AppProjectWorkFlowTaskEntity and  AppProjectWorkFlowTaskDto
    /// </summary>
    public static partial class AppProjectWorkFlowTaskConverter 
    {
         /// <summary>
        ///  Convert AppProjectWorkFlowTaskEntity To  AppProjectWorkFlowTaskDto
        /// </summary>
        public static AppProjectWorkFlowTaskDto ConvertEntityToDto(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity)
        {        
    		AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto = new AppProjectWorkFlowTaskDto();
    		CopyEntityPropertyToDto( aAppProjectWorkFlowTaskEntity, aAppProjectWorkFlowTaskDto);          
			return aAppProjectWorkFlowTaskDto;
        }
		 /// <summary>
        ///  Convert AppProjectWorkFlowTaskEntity To  AppProjectWorkFlowTaskExDto
        /// </summary>
        public static AppProjectWorkFlowTaskExDto ConvertEntityToExDto(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity)
        {        
    		AppProjectWorkFlowTaskExDto aAppProjectWorkFlowTaskExDto = new AppProjectWorkFlowTaskExDto();
			CopyEntityPropertyToDto( aAppProjectWorkFlowTaskEntity, aAppProjectWorkFlowTaskExDto);
			
			
			
            return aAppProjectWorkFlowTaskExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectWorkFlowTaskEntity To  AppProjectWorkFlowTaskDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity,AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto)
        {        
    		
           // aAppProjectWorkFlowTaskDto.StopChangeTracking();
 			aAppProjectWorkFlowTaskDto.Id = aAppProjectWorkFlowTaskEntity.ProjectWorkFlowTaskId;
 			aAppProjectWorkFlowTaskDto.ProjectSectionId = aAppProjectWorkFlowTaskEntity.ProjectSectionId;
 			aAppProjectWorkFlowTaskDto.ProjectId = aAppProjectWorkFlowTaskEntity.ProjectId;
 			aAppProjectWorkFlowTaskDto.Name = aAppProjectWorkFlowTaskEntity.Name;
 			aAppProjectWorkFlowTaskDto.Description = aAppProjectWorkFlowTaskEntity.Description;
 			aAppProjectWorkFlowTaskDto.DateModelStart = aAppProjectWorkFlowTaskEntity.DateModelStart;
 			aAppProjectWorkFlowTaskDto.DateModelEnd = aAppProjectWorkFlowTaskEntity.DateModelEnd;
 			aAppProjectWorkFlowTaskDto.DatePlannedStart = aAppProjectWorkFlowTaskEntity.DatePlannedStart;
 			aAppProjectWorkFlowTaskDto.DatePlannedEnd = aAppProjectWorkFlowTaskEntity.DatePlannedEnd;
 			aAppProjectWorkFlowTaskDto.DateActualStart = aAppProjectWorkFlowTaskEntity.DateActualStart;
 			aAppProjectWorkFlowTaskDto.DateActualEnd = aAppProjectWorkFlowTaskEntity.DateActualEnd;
 			aAppProjectWorkFlowTaskDto.CompletedById = aAppProjectWorkFlowTaskEntity.CompletedById;
 			aAppProjectWorkFlowTaskDto.ActivityId = aAppProjectWorkFlowTaskEntity.ActivityId;
 			aAppProjectWorkFlowTaskDto.PhaseId = aAppProjectWorkFlowTaskEntity.PhaseId;
 			aAppProjectWorkFlowTaskDto.IsAutoStart = aAppProjectWorkFlowTaskEntity.IsAutoStart;
 			aAppProjectWorkFlowTaskDto.SeverityId = aAppProjectWorkFlowTaskEntity.SeverityId;
 			aAppProjectWorkFlowTaskDto.Notes = aAppProjectWorkFlowTaskEntity.Notes;
 			aAppProjectWorkFlowTaskDto.Sort = aAppProjectWorkFlowTaskEntity.Sort;
 			aAppProjectWorkFlowTaskDto.IsFixedPlannedDate = aAppProjectWorkFlowTaskEntity.IsFixedPlannedDate;
 			aAppProjectWorkFlowTaskDto.NbDays = aAppProjectWorkFlowTaskEntity.NbDays;
 			aAppProjectWorkFlowTaskDto.TimingDays = aAppProjectWorkFlowTaskEntity.TimingDays;
 			aAppProjectWorkFlowTaskDto.NbHours = aAppProjectWorkFlowTaskEntity.NbHours;
 			aAppProjectWorkFlowTaskDto.IsAutoComplete = aAppProjectWorkFlowTaskEntity.IsAutoComplete;
 			aAppProjectWorkFlowTaskDto.IsMilestone = aAppProjectWorkFlowTaskEntity.IsMilestone;
 			aAppProjectWorkFlowTaskDto.ProjectActivityStatusId = aAppProjectWorkFlowTaskEntity.ProjectActivityStatusId;
 			aAppProjectWorkFlowTaskDto.Weight = aAppProjectWorkFlowTaskEntity.Weight;
 			aAppProjectWorkFlowTaskDto.ToleranceDays = aAppProjectWorkFlowTaskEntity.ToleranceDays;
 			aAppProjectWorkFlowTaskDto.IsDependent = aAppProjectWorkFlowTaskEntity.IsDependent;
 			aAppProjectWorkFlowTaskDto.OriginalLibProjectActivityId = aAppProjectWorkFlowTaskEntity.OriginalLibProjectActivityId;
 			aAppProjectWorkFlowTaskDto.RowIdentity = aAppProjectWorkFlowTaskEntity.RowIdentity;
 			aAppProjectWorkFlowTaskDto.IsActive = aAppProjectWorkFlowTaskEntity.IsActive;
 			aAppProjectWorkFlowTaskDto.MainTaskId = aAppProjectWorkFlowTaskEntity.MainTaskId;
 			aAppProjectWorkFlowTaskDto.UnitOfTime = aAppProjectWorkFlowTaskEntity.UnitOfTime;
 			aAppProjectWorkFlowTaskDto.AmountOfTime = aAppProjectWorkFlowTaskEntity.AmountOfTime;
 			aAppProjectWorkFlowTaskDto.ActrualNeedAmountHours = aAppProjectWorkFlowTaskEntity.ActrualNeedAmountHours;
 			aAppProjectWorkFlowTaskDto.DiagramShapeType = aAppProjectWorkFlowTaskEntity.DiagramShapeType;
 			aAppProjectWorkFlowTaskDto.StageType = aAppProjectWorkFlowTaskEntity.StageType;
 			aAppProjectWorkFlowTaskDto.StageStatusFlag = aAppProjectWorkFlowTaskEntity.StageStatusFlag;
 			aAppProjectWorkFlowTaskDto.StageUilayout = aAppProjectWorkFlowTaskEntity.StageUilayout;
 			aAppProjectWorkFlowTaskDto.IsProjectSumaryTask = aAppProjectWorkFlowTaskEntity.IsProjectSumaryTask;
 			aAppProjectWorkFlowTaskDto.TaskPlannedCost = aAppProjectWorkFlowTaskEntity.TaskPlannedCost;
 			aAppProjectWorkFlowTaskDto.TaskActualCost = aAppProjectWorkFlowTaskEntity.TaskActualCost;
 			aAppProjectWorkFlowTaskDto.IsBillable = aAppProjectWorkFlowTaskEntity.IsBillable;
 			aAppProjectWorkFlowTaskDto.BillRateByHour = aAppProjectWorkFlowTaskEntity.BillRateByHour;
 			aAppProjectWorkFlowTaskDto.CurrencyCode = aAppProjectWorkFlowTaskEntity.CurrencyCode;
 			aAppProjectWorkFlowTaskDto.EmPriority = aAppProjectWorkFlowTaskEntity.EmPriority;
 			aAppProjectWorkFlowTaskDto.EmTaskType = aAppProjectWorkFlowTaskEntity.EmTaskType;
 			aAppProjectWorkFlowTaskDto.EmCostType = aAppProjectWorkFlowTaskEntity.EmCostType;
 			aAppProjectWorkFlowTaskDto.ProjectRoleId = aAppProjectWorkFlowTaskEntity.ProjectRoleId;
 			aAppProjectWorkFlowTaskDto.TaskOwnerId = aAppProjectWorkFlowTaskEntity.TaskOwnerId;
 			aAppProjectWorkFlowTaskDto.TransactionId = aAppProjectWorkFlowTaskEntity.TransactionId;
 			aAppProjectWorkFlowTaskDto.TransactionRid = aAppProjectWorkFlowTaskEntity.TransactionRid;
 			aAppProjectWorkFlowTaskDto.EmAppTaskOwnerDeliverPhase = aAppProjectWorkFlowTaskEntity.EmAppTaskOwnerDeliverPhase;
 			aAppProjectWorkFlowTaskDto.RequirChildrenCompletedPercentAsTaskComple = aAppProjectWorkFlowTaskEntity.RequirChildrenCompletedPercentAsTaskComple;
 			aAppProjectWorkFlowTaskDto.CompletedPercent = aAppProjectWorkFlowTaskEntity.CompletedPercent;
 			aAppProjectWorkFlowTaskDto.AppCreatedById = aAppProjectWorkFlowTaskEntity.AppCreatedById;
 			aAppProjectWorkFlowTaskDto.AppCreatedDate = aAppProjectWorkFlowTaskEntity.AppCreatedDate;
 			aAppProjectWorkFlowTaskDto.AppModifiedDate = aAppProjectWorkFlowTaskEntity.AppModifiedDate;
 			aAppProjectWorkFlowTaskDto.AppModifiedById = aAppProjectWorkFlowTaskEntity.AppModifiedById;
 			aAppProjectWorkFlowTaskDto.AppCreatedByCompanyId = aAppProjectWorkFlowTaskEntity.AppCreatedByCompanyId;
 			aAppProjectWorkFlowTaskDto.TimeSheetEntryMethod = aAppProjectWorkFlowTaskEntity.TimeSheetEntryMethod;
 			aAppProjectWorkFlowTaskDto.PlannedWorkHours = aAppProjectWorkFlowTaskEntity.PlannedWorkHours;
 			aAppProjectWorkFlowTaskDto.ActualWorkHours = aAppProjectWorkFlowTaskEntity.ActualWorkHours;
 			aAppProjectWorkFlowTaskDto.PlannedResourceCost = aAppProjectWorkFlowTaskEntity.PlannedResourceCost;
 			aAppProjectWorkFlowTaskDto.ActualResourceCost = aAppProjectWorkFlowTaskEntity.ActualResourceCost;
 			aAppProjectWorkFlowTaskDto.ProgressId = aAppProjectWorkFlowTaskEntity.ProgressId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectWorkFlowTaskDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.AppCreatedDate);
                aAppProjectWorkFlowTaskDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowTaskEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectWorkFlowTaskEntity, aAppProjectWorkFlowTaskDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectWorkFlowTaskDto Properties to   AppProjectWorkFlowTaskEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity,AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto)
        {        
 
      			aAppProjectWorkFlowTaskEntity.ProjectSectionId = aAppProjectWorkFlowTaskDto.ProjectSectionId;
      			aAppProjectWorkFlowTaskEntity.ProjectId = aAppProjectWorkFlowTaskDto.ProjectId;
      			aAppProjectWorkFlowTaskEntity.Name = aAppProjectWorkFlowTaskDto.Name;
      			aAppProjectWorkFlowTaskEntity.Description = aAppProjectWorkFlowTaskDto.Description;
      			aAppProjectWorkFlowTaskEntity.DateModelStart = aAppProjectWorkFlowTaskDto.DateModelStart;
      			aAppProjectWorkFlowTaskEntity.DateModelEnd = aAppProjectWorkFlowTaskDto.DateModelEnd;
      			aAppProjectWorkFlowTaskEntity.DatePlannedStart = aAppProjectWorkFlowTaskDto.DatePlannedStart;
      			aAppProjectWorkFlowTaskEntity.DatePlannedEnd = aAppProjectWorkFlowTaskDto.DatePlannedEnd;
      			aAppProjectWorkFlowTaskEntity.DateActualStart = aAppProjectWorkFlowTaskDto.DateActualStart;
      			aAppProjectWorkFlowTaskEntity.DateActualEnd = aAppProjectWorkFlowTaskDto.DateActualEnd;
      			aAppProjectWorkFlowTaskEntity.CompletedById = aAppProjectWorkFlowTaskDto.CompletedById;
      			aAppProjectWorkFlowTaskEntity.ActivityId = aAppProjectWorkFlowTaskDto.ActivityId;
      			aAppProjectWorkFlowTaskEntity.PhaseId = aAppProjectWorkFlowTaskDto.PhaseId;
      			aAppProjectWorkFlowTaskEntity.IsAutoStart = aAppProjectWorkFlowTaskDto.IsAutoStart;
      			aAppProjectWorkFlowTaskEntity.SeverityId = aAppProjectWorkFlowTaskDto.SeverityId;
      			aAppProjectWorkFlowTaskEntity.Notes = aAppProjectWorkFlowTaskDto.Notes;
      			aAppProjectWorkFlowTaskEntity.Sort = aAppProjectWorkFlowTaskDto.Sort;
      			aAppProjectWorkFlowTaskEntity.IsFixedPlannedDate = aAppProjectWorkFlowTaskDto.IsFixedPlannedDate;
      			aAppProjectWorkFlowTaskEntity.NbDays = aAppProjectWorkFlowTaskDto.NbDays;
      			aAppProjectWorkFlowTaskEntity.TimingDays = aAppProjectWorkFlowTaskDto.TimingDays;
      			aAppProjectWorkFlowTaskEntity.NbHours = aAppProjectWorkFlowTaskDto.NbHours;
      			aAppProjectWorkFlowTaskEntity.IsAutoComplete = aAppProjectWorkFlowTaskDto.IsAutoComplete;
      			aAppProjectWorkFlowTaskEntity.IsMilestone = aAppProjectWorkFlowTaskDto.IsMilestone;
      			aAppProjectWorkFlowTaskEntity.ProjectActivityStatusId = aAppProjectWorkFlowTaskDto.ProjectActivityStatusId;
      			aAppProjectWorkFlowTaskEntity.Weight = aAppProjectWorkFlowTaskDto.Weight;
      			aAppProjectWorkFlowTaskEntity.ToleranceDays = aAppProjectWorkFlowTaskDto.ToleranceDays;
      			aAppProjectWorkFlowTaskEntity.IsDependent = aAppProjectWorkFlowTaskDto.IsDependent;
      			aAppProjectWorkFlowTaskEntity.OriginalLibProjectActivityId = aAppProjectWorkFlowTaskDto.OriginalLibProjectActivityId;
      			aAppProjectWorkFlowTaskEntity.RowIdentity = aAppProjectWorkFlowTaskDto.RowIdentity;
      			aAppProjectWorkFlowTaskEntity.IsActive = aAppProjectWorkFlowTaskDto.IsActive;
      			aAppProjectWorkFlowTaskEntity.MainTaskId = aAppProjectWorkFlowTaskDto.MainTaskId;
      			aAppProjectWorkFlowTaskEntity.UnitOfTime = aAppProjectWorkFlowTaskDto.UnitOfTime;
      			aAppProjectWorkFlowTaskEntity.AmountOfTime = aAppProjectWorkFlowTaskDto.AmountOfTime;
      			aAppProjectWorkFlowTaskEntity.ActrualNeedAmountHours = aAppProjectWorkFlowTaskDto.ActrualNeedAmountHours;
      			aAppProjectWorkFlowTaskEntity.DiagramShapeType = aAppProjectWorkFlowTaskDto.DiagramShapeType;
      			aAppProjectWorkFlowTaskEntity.StageType = aAppProjectWorkFlowTaskDto.StageType;
      			aAppProjectWorkFlowTaskEntity.StageStatusFlag = aAppProjectWorkFlowTaskDto.StageStatusFlag;
      			aAppProjectWorkFlowTaskEntity.StageUilayout = aAppProjectWorkFlowTaskDto.StageUilayout;
      			aAppProjectWorkFlowTaskEntity.IsProjectSumaryTask = aAppProjectWorkFlowTaskDto.IsProjectSumaryTask;
      			aAppProjectWorkFlowTaskEntity.TaskPlannedCost = aAppProjectWorkFlowTaskDto.TaskPlannedCost;
      			aAppProjectWorkFlowTaskEntity.TaskActualCost = aAppProjectWorkFlowTaskDto.TaskActualCost;
      			aAppProjectWorkFlowTaskEntity.IsBillable = aAppProjectWorkFlowTaskDto.IsBillable;
      			aAppProjectWorkFlowTaskEntity.BillRateByHour = aAppProjectWorkFlowTaskDto.BillRateByHour;
      			aAppProjectWorkFlowTaskEntity.CurrencyCode = aAppProjectWorkFlowTaskDto.CurrencyCode;
      			aAppProjectWorkFlowTaskEntity.EmPriority = aAppProjectWorkFlowTaskDto.EmPriority;
      			aAppProjectWorkFlowTaskEntity.EmTaskType = aAppProjectWorkFlowTaskDto.EmTaskType;
      			aAppProjectWorkFlowTaskEntity.EmCostType = aAppProjectWorkFlowTaskDto.EmCostType;
      			aAppProjectWorkFlowTaskEntity.ProjectRoleId = aAppProjectWorkFlowTaskDto.ProjectRoleId;
      			aAppProjectWorkFlowTaskEntity.TaskOwnerId = aAppProjectWorkFlowTaskDto.TaskOwnerId;
      			aAppProjectWorkFlowTaskEntity.TransactionId = aAppProjectWorkFlowTaskDto.TransactionId;
      			aAppProjectWorkFlowTaskEntity.TransactionRid = aAppProjectWorkFlowTaskDto.TransactionRid;
      			aAppProjectWorkFlowTaskEntity.EmAppTaskOwnerDeliverPhase = aAppProjectWorkFlowTaskDto.EmAppTaskOwnerDeliverPhase;
      			aAppProjectWorkFlowTaskEntity.RequirChildrenCompletedPercentAsTaskComple = aAppProjectWorkFlowTaskDto.RequirChildrenCompletedPercentAsTaskComple;
      			aAppProjectWorkFlowTaskEntity.CompletedPercent = aAppProjectWorkFlowTaskDto.CompletedPercent;
 
  
   
    
      			aAppProjectWorkFlowTaskEntity.AppCreatedByCompanyId = aAppProjectWorkFlowTaskDto.AppCreatedByCompanyId;
      			aAppProjectWorkFlowTaskEntity.TimeSheetEntryMethod = aAppProjectWorkFlowTaskDto.TimeSheetEntryMethod;
      			aAppProjectWorkFlowTaskEntity.PlannedWorkHours = aAppProjectWorkFlowTaskDto.PlannedWorkHours;
      			aAppProjectWorkFlowTaskEntity.ActualWorkHours = aAppProjectWorkFlowTaskDto.ActualWorkHours;
      			aAppProjectWorkFlowTaskEntity.PlannedResourceCost = aAppProjectWorkFlowTaskDto.PlannedResourceCost;
      			aAppProjectWorkFlowTaskEntity.ActualResourceCost = aAppProjectWorkFlowTaskDto.ActualResourceCost;
      			aAppProjectWorkFlowTaskEntity.ProgressId = aAppProjectWorkFlowTaskDto.ProgressId;
			
			if(aAppProjectWorkFlowTaskDto.Id == null)
			{
				aAppProjectWorkFlowTaskEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectWorkFlowTaskEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectWorkFlowTaskEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectWorkFlowTaskEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectWorkFlowTaskEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectWorkFlowTaskEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectWorkFlowTaskEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectWorkFlowTaskEntity, aAppProjectWorkFlowTaskDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity,AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectWorkFlowTaskEntity aAppProjectWorkFlowTaskEntity,AppProjectWorkFlowTaskDto aAppProjectWorkFlowTaskDto);
		
   
       
    }
}

 