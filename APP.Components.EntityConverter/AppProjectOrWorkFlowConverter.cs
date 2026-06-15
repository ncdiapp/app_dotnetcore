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
    /// Convert Properties between  AppProjectOrWorkFlowEntity and  AppProjectOrWorkFlowDto
    /// </summary>
    public static partial class AppProjectOrWorkFlowConverter 
    {
         /// <summary>
        ///  Convert AppProjectOrWorkFlowEntity To  AppProjectOrWorkFlowDto
        /// </summary>
        public static AppProjectOrWorkFlowDto ConvertEntityToDto(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity)
        {        
    		AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto = new AppProjectOrWorkFlowDto();
    		CopyEntityPropertyToDto( aAppProjectOrWorkFlowEntity, aAppProjectOrWorkFlowDto);          
			return aAppProjectOrWorkFlowDto;
        }
		 /// <summary>
        ///  Convert AppProjectOrWorkFlowEntity To  AppProjectOrWorkFlowExDto
        /// </summary>
        public static AppProjectOrWorkFlowExDto ConvertEntityToExDto(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity)
        {        
    		AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = new AppProjectOrWorkFlowExDto();
			CopyEntityPropertyToDto( aAppProjectOrWorkFlowEntity, aAppProjectOrWorkFlowExDto);
			
			
			
            return aAppProjectOrWorkFlowExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectOrWorkFlowEntity To  AppProjectOrWorkFlowDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity,AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto)
        {        
    		
           // aAppProjectOrWorkFlowDto.StopChangeTracking();
 			aAppProjectOrWorkFlowDto.Id = aAppProjectOrWorkFlowEntity.ProjectId;
 			aAppProjectOrWorkFlowDto.Name = aAppProjectOrWorkFlowEntity.Name;
 			aAppProjectOrWorkFlowDto.Description = aAppProjectOrWorkFlowEntity.Description;
 			aAppProjectOrWorkFlowDto.ProjectDirectionId = aAppProjectOrWorkFlowEntity.ProjectDirectionId;
 			aAppProjectOrWorkFlowDto.ParentProjectId = aAppProjectOrWorkFlowEntity.ParentProjectId;
 			aAppProjectOrWorkFlowDto.DateModelStart = aAppProjectOrWorkFlowEntity.DateModelStart;
 			aAppProjectOrWorkFlowDto.DateModelEnd = aAppProjectOrWorkFlowEntity.DateModelEnd;
 			aAppProjectOrWorkFlowDto.DatePlannedStart = aAppProjectOrWorkFlowEntity.DatePlannedStart;
 			aAppProjectOrWorkFlowDto.DatePlannedEnd = aAppProjectOrWorkFlowEntity.DatePlannedEnd;
 			aAppProjectOrWorkFlowDto.DateActualStart = aAppProjectOrWorkFlowEntity.DateActualStart;
 			aAppProjectOrWorkFlowDto.DateActualEnd = aAppProjectOrWorkFlowEntity.DateActualEnd;
 			aAppProjectOrWorkFlowDto.DateAborted = aAppProjectOrWorkFlowEntity.DateAborted;
 			aAppProjectOrWorkFlowDto.ProjectPathId = aAppProjectOrWorkFlowEntity.ProjectPathId;
 			aAppProjectOrWorkFlowDto.IsPredefined = aAppProjectOrWorkFlowEntity.IsPredefined;
 			aAppProjectOrWorkFlowDto.IsActive = aAppProjectOrWorkFlowEntity.IsActive;
 			aAppProjectOrWorkFlowDto.TransactionId = aAppProjectOrWorkFlowEntity.TransactionId;
 			aAppProjectOrWorkFlowDto.TransactionRid = aAppProjectOrWorkFlowEntity.TransactionRid;
 			aAppProjectOrWorkFlowDto.TimeUnit = aAppProjectOrWorkFlowEntity.TimeUnit;
 			aAppProjectOrWorkFlowDto.ProjectWorkflowType = aAppProjectOrWorkFlowEntity.ProjectWorkflowType;
 			aAppProjectOrWorkFlowDto.ProjectTeamId = aAppProjectOrWorkFlowEntity.ProjectTeamId;
 			aAppProjectOrWorkFlowDto.ProjectLeaderId = aAppProjectOrWorkFlowEntity.ProjectLeaderId;
 			aAppProjectOrWorkFlowDto.ProjectSumaryTaskId = aAppProjectOrWorkFlowEntity.ProjectSumaryTaskId;
 			aAppProjectOrWorkFlowDto.ProjectModelBugestCost = aAppProjectOrWorkFlowEntity.ProjectModelBugestCost;
 			aAppProjectOrWorkFlowDto.ProjectPlannedCost = aAppProjectOrWorkFlowEntity.ProjectPlannedCost;
 			aAppProjectOrWorkFlowDto.ProjectActualCost = aAppProjectOrWorkFlowEntity.ProjectActualCost;
 			aAppProjectOrWorkFlowDto.CurrencyId = aAppProjectOrWorkFlowEntity.CurrencyId;
 			aAppProjectOrWorkFlowDto.CompanyId = aAppProjectOrWorkFlowEntity.CompanyId;
 			aAppProjectOrWorkFlowDto.IsNeedBudget = aAppProjectOrWorkFlowEntity.IsNeedBudget;
 			aAppProjectOrWorkFlowDto.Duration = aAppProjectOrWorkFlowEntity.Duration;
 			aAppProjectOrWorkFlowDto.DisplayLayoutType = aAppProjectOrWorkFlowEntity.DisplayLayoutType;
 			aAppProjectOrWorkFlowDto.EmPrivacy = aAppProjectOrWorkFlowEntity.EmPrivacy;
 			aAppProjectOrWorkFlowDto.ParticipatedDmainId = aAppProjectOrWorkFlowEntity.ParticipatedDmainId;
 			aAppProjectOrWorkFlowDto.EmCostType = aAppProjectOrWorkFlowEntity.EmCostType;
 			aAppProjectOrWorkFlowDto.CompletedPercent = aAppProjectOrWorkFlowEntity.CompletedPercent;
 			aAppProjectOrWorkFlowDto.RequireTaskCompletedPercentAsCompleProject = aAppProjectOrWorkFlowEntity.RequireTaskCompletedPercentAsCompleProject;
 			aAppProjectOrWorkFlowDto.AppCreatedById = aAppProjectOrWorkFlowEntity.AppCreatedById;
 			aAppProjectOrWorkFlowDto.AppCreatedDate = aAppProjectOrWorkFlowEntity.AppCreatedDate;
 			aAppProjectOrWorkFlowDto.AppModifiedDate = aAppProjectOrWorkFlowEntity.AppModifiedDate;
 			aAppProjectOrWorkFlowDto.AppModifiedById = aAppProjectOrWorkFlowEntity.AppModifiedById;
 			aAppProjectOrWorkFlowDto.AppCreatedByCompanyId = aAppProjectOrWorkFlowEntity.AppCreatedByCompanyId;
 			aAppProjectOrWorkFlowDto.RuntimeOriginalProjectId = aAppProjectOrWorkFlowEntity.RuntimeOriginalProjectId;
 			aAppProjectOrWorkFlowDto.DefaultGanttDisplayUnit = aAppProjectOrWorkFlowEntity.DefaultGanttDisplayUnit;
 			aAppProjectOrWorkFlowDto.IsChildProjectAllowParentTtrickleDown = aAppProjectOrWorkFlowEntity.IsChildProjectAllowParentTtrickleDown;
 			aAppProjectOrWorkFlowDto.IsChildProjectAllowChildBubbleUpParent = aAppProjectOrWorkFlowEntity.IsChildProjectAllowChildBubbleUpParent;
 			aAppProjectOrWorkFlowDto.ProjectLogoImageId = aAppProjectOrWorkFlowEntity.ProjectLogoImageId;
 			aAppProjectOrWorkFlowDto.PlannedWorkHours = aAppProjectOrWorkFlowEntity.PlannedWorkHours;
 			aAppProjectOrWorkFlowDto.ActualWorkHours = aAppProjectOrWorkFlowEntity.ActualWorkHours;
 			aAppProjectOrWorkFlowDto.PlannedResourceCost = aAppProjectOrWorkFlowEntity.PlannedResourceCost;
 			aAppProjectOrWorkFlowDto.ActualResourceCost = aAppProjectOrWorkFlowEntity.ActualResourceCost;
 			aAppProjectOrWorkFlowDto.SaasApplicationId = aAppProjectOrWorkFlowEntity.SaasApplicationId;
 			aAppProjectOrWorkFlowDto.TransactionGroupId = aAppProjectOrWorkFlowEntity.TransactionGroupId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectOrWorkFlowDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.AppCreatedDate);
                aAppProjectOrWorkFlowDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrWorkFlowEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectOrWorkFlowEntity, aAppProjectOrWorkFlowDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectOrWorkFlowDto Properties to   AppProjectOrWorkFlowEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity,AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto)
        {        
 
      			aAppProjectOrWorkFlowEntity.Name = aAppProjectOrWorkFlowDto.Name;
      			aAppProjectOrWorkFlowEntity.Description = aAppProjectOrWorkFlowDto.Description;
      			aAppProjectOrWorkFlowEntity.ProjectDirectionId = aAppProjectOrWorkFlowDto.ProjectDirectionId;
      			aAppProjectOrWorkFlowEntity.ParentProjectId = aAppProjectOrWorkFlowDto.ParentProjectId;
      			aAppProjectOrWorkFlowEntity.DateModelStart = aAppProjectOrWorkFlowDto.DateModelStart;
      			aAppProjectOrWorkFlowEntity.DateModelEnd = aAppProjectOrWorkFlowDto.DateModelEnd;
      			aAppProjectOrWorkFlowEntity.DatePlannedStart = aAppProjectOrWorkFlowDto.DatePlannedStart;
      			aAppProjectOrWorkFlowEntity.DatePlannedEnd = aAppProjectOrWorkFlowDto.DatePlannedEnd;
      			aAppProjectOrWorkFlowEntity.DateActualStart = aAppProjectOrWorkFlowDto.DateActualStart;
      			aAppProjectOrWorkFlowEntity.DateActualEnd = aAppProjectOrWorkFlowDto.DateActualEnd;
      			aAppProjectOrWorkFlowEntity.DateAborted = aAppProjectOrWorkFlowDto.DateAborted;
      			aAppProjectOrWorkFlowEntity.ProjectPathId = aAppProjectOrWorkFlowDto.ProjectPathId;
      			aAppProjectOrWorkFlowEntity.IsPredefined = aAppProjectOrWorkFlowDto.IsPredefined;
      			aAppProjectOrWorkFlowEntity.IsActive = aAppProjectOrWorkFlowDto.IsActive;
      			aAppProjectOrWorkFlowEntity.TransactionId = aAppProjectOrWorkFlowDto.TransactionId;
      			aAppProjectOrWorkFlowEntity.TransactionRid = aAppProjectOrWorkFlowDto.TransactionRid;
      			aAppProjectOrWorkFlowEntity.TimeUnit = aAppProjectOrWorkFlowDto.TimeUnit;
      			aAppProjectOrWorkFlowEntity.ProjectWorkflowType = aAppProjectOrWorkFlowDto.ProjectWorkflowType;
      			aAppProjectOrWorkFlowEntity.ProjectTeamId = aAppProjectOrWorkFlowDto.ProjectTeamId;
      			aAppProjectOrWorkFlowEntity.ProjectLeaderId = aAppProjectOrWorkFlowDto.ProjectLeaderId;
      			aAppProjectOrWorkFlowEntity.ProjectSumaryTaskId = aAppProjectOrWorkFlowDto.ProjectSumaryTaskId;
      			aAppProjectOrWorkFlowEntity.ProjectModelBugestCost = aAppProjectOrWorkFlowDto.ProjectModelBugestCost;
      			aAppProjectOrWorkFlowEntity.ProjectPlannedCost = aAppProjectOrWorkFlowDto.ProjectPlannedCost;
      			aAppProjectOrWorkFlowEntity.ProjectActualCost = aAppProjectOrWorkFlowDto.ProjectActualCost;
      			aAppProjectOrWorkFlowEntity.CurrencyId = aAppProjectOrWorkFlowDto.CurrencyId;
      			aAppProjectOrWorkFlowEntity.CompanyId = aAppProjectOrWorkFlowDto.CompanyId;
      			aAppProjectOrWorkFlowEntity.IsNeedBudget = aAppProjectOrWorkFlowDto.IsNeedBudget;
      			aAppProjectOrWorkFlowEntity.Duration = aAppProjectOrWorkFlowDto.Duration;
      			aAppProjectOrWorkFlowEntity.DisplayLayoutType = aAppProjectOrWorkFlowDto.DisplayLayoutType;
      			aAppProjectOrWorkFlowEntity.EmPrivacy = aAppProjectOrWorkFlowDto.EmPrivacy;
      			aAppProjectOrWorkFlowEntity.ParticipatedDmainId = aAppProjectOrWorkFlowDto.ParticipatedDmainId;
      			aAppProjectOrWorkFlowEntity.EmCostType = aAppProjectOrWorkFlowDto.EmCostType;
      			aAppProjectOrWorkFlowEntity.CompletedPercent = aAppProjectOrWorkFlowDto.CompletedPercent;
      			aAppProjectOrWorkFlowEntity.RequireTaskCompletedPercentAsCompleProject = aAppProjectOrWorkFlowDto.RequireTaskCompletedPercentAsCompleProject;
 
  
   
    
      			aAppProjectOrWorkFlowEntity.AppCreatedByCompanyId = aAppProjectOrWorkFlowDto.AppCreatedByCompanyId;
      			aAppProjectOrWorkFlowEntity.RuntimeOriginalProjectId = aAppProjectOrWorkFlowDto.RuntimeOriginalProjectId;
      			aAppProjectOrWorkFlowEntity.DefaultGanttDisplayUnit = aAppProjectOrWorkFlowDto.DefaultGanttDisplayUnit;
      			aAppProjectOrWorkFlowEntity.IsChildProjectAllowParentTtrickleDown = aAppProjectOrWorkFlowDto.IsChildProjectAllowParentTtrickleDown;
      			aAppProjectOrWorkFlowEntity.IsChildProjectAllowChildBubbleUpParent = aAppProjectOrWorkFlowDto.IsChildProjectAllowChildBubbleUpParent;
      			aAppProjectOrWorkFlowEntity.ProjectLogoImageId = aAppProjectOrWorkFlowDto.ProjectLogoImageId;
      			aAppProjectOrWorkFlowEntity.PlannedWorkHours = aAppProjectOrWorkFlowDto.PlannedWorkHours;
      			aAppProjectOrWorkFlowEntity.ActualWorkHours = aAppProjectOrWorkFlowDto.ActualWorkHours;
      			aAppProjectOrWorkFlowEntity.PlannedResourceCost = aAppProjectOrWorkFlowDto.PlannedResourceCost;
      			aAppProjectOrWorkFlowEntity.ActualResourceCost = aAppProjectOrWorkFlowDto.ActualResourceCost;
      			aAppProjectOrWorkFlowEntity.SaasApplicationId = aAppProjectOrWorkFlowDto.SaasApplicationId;
      			aAppProjectOrWorkFlowEntity.TransactionGroupId = aAppProjectOrWorkFlowDto.TransactionGroupId;
			
			if(aAppProjectOrWorkFlowDto.Id == null)
			{
				aAppProjectOrWorkFlowEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectOrWorkFlowEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectOrWorkFlowEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectOrWorkFlowEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectOrWorkFlowEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectOrWorkFlowEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectOrWorkFlowEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectOrWorkFlowEntity, aAppProjectOrWorkFlowDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity,AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectOrWorkFlowEntity aAppProjectOrWorkFlowEntity,AppProjectOrWorkFlowDto aAppProjectOrWorkFlowDto);
		
   
       
    }
}

 