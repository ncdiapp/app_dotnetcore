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
    /// Convert Properties between  AppProjectWorkFlowConditionEntity and  AppProjectWorkFlowConditionDto
    /// </summary>
    public static partial class AppProjectWorkFlowConditionConverter 
    {
         /// <summary>
        ///  Convert AppProjectWorkFlowConditionEntity To  AppProjectWorkFlowConditionDto
        /// </summary>
        public static AppProjectWorkFlowConditionDto ConvertEntityToDto(AppProjectWorkFlowConditionEntity aAppProjectWorkFlowConditionEntity)
        {        
    		AppProjectWorkFlowConditionDto aAppProjectWorkFlowConditionDto = new AppProjectWorkFlowConditionDto();
    		CopyEntityPropertyToDto( aAppProjectWorkFlowConditionEntity, aAppProjectWorkFlowConditionDto);          
			return aAppProjectWorkFlowConditionDto;
        }
		 /// <summary>
        ///  Convert AppProjectWorkFlowConditionEntity To  AppProjectWorkFlowConditionExDto
        /// </summary>
        public static AppProjectWorkFlowConditionExDto ConvertEntityToExDto(AppProjectWorkFlowConditionEntity aAppProjectWorkFlowConditionEntity)
        {        
    		AppProjectWorkFlowConditionExDto aAppProjectWorkFlowConditionExDto = new AppProjectWorkFlowConditionExDto();
			CopyEntityPropertyToDto( aAppProjectWorkFlowConditionEntity, aAppProjectWorkFlowConditionExDto);
			
			
			
            return aAppProjectWorkFlowConditionExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectWorkFlowConditionEntity To  AppProjectWorkFlowConditionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectWorkFlowConditionEntity aAppProjectWorkFlowConditionEntity,AppProjectWorkFlowConditionDto aAppProjectWorkFlowConditionDto)
        {        
    		
           // aAppProjectWorkFlowConditionDto.StopChangeTracking();
 			aAppProjectWorkFlowConditionDto.Id = aAppProjectWorkFlowConditionEntity.WorkFlowConditionId;
 			aAppProjectWorkFlowConditionDto.ProjectWorkFlowTaskId = aAppProjectWorkFlowConditionEntity.ProjectWorkFlowTaskId;
 			aAppProjectWorkFlowConditionDto.ProjectId = aAppProjectWorkFlowConditionEntity.ProjectId;
 			aAppProjectWorkFlowConditionDto.Name = aAppProjectWorkFlowConditionEntity.Name;
 			aAppProjectWorkFlowConditionDto.Description = aAppProjectWorkFlowConditionEntity.Description;
 			aAppProjectWorkFlowConditionDto.FormulaExpression = aAppProjectWorkFlowConditionEntity.FormulaExpression;
 			aAppProjectWorkFlowConditionDto.MonitorChildUnitId = aAppProjectWorkFlowConditionEntity.MonitorChildUnitId;
 			aAppProjectWorkFlowConditionDto.ConditionTransactionFieldId = aAppProjectWorkFlowConditionEntity.ConditionTransactionFieldId;
 			aAppProjectWorkFlowConditionDto.ConditionTypeId = aAppProjectWorkFlowConditionEntity.ConditionTypeId;
 			aAppProjectWorkFlowConditionDto.ConditionPredictValue = aAppProjectWorkFlowConditionEntity.ConditionPredictValue;
 			aAppProjectWorkFlowConditionDto.RowIdentity = aAppProjectWorkFlowConditionEntity.RowIdentity;
 			aAppProjectWorkFlowConditionDto.TriggerFlowOrder = aAppProjectWorkFlowConditionEntity.TriggerFlowOrder;
 			aAppProjectWorkFlowConditionDto.AppCreatedById = aAppProjectWorkFlowConditionEntity.AppCreatedById;
 			aAppProjectWorkFlowConditionDto.AppCreatedDate = aAppProjectWorkFlowConditionEntity.AppCreatedDate;
 			aAppProjectWorkFlowConditionDto.AppModifiedDate = aAppProjectWorkFlowConditionEntity.AppModifiedDate;
 			aAppProjectWorkFlowConditionDto.AppModifiedById = aAppProjectWorkFlowConditionEntity.AppModifiedById;
 			aAppProjectWorkFlowConditionDto.AppCreatedByCompanyId = aAppProjectWorkFlowConditionEntity.AppCreatedByCompanyId;
 			aAppProjectWorkFlowConditionDto.ConditionGuid = aAppProjectWorkFlowConditionEntity.ConditionGuid;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectWorkFlowConditionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowConditionEntity.AppCreatedDate);
                aAppProjectWorkFlowConditionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectWorkFlowConditionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectWorkFlowConditionEntity, aAppProjectWorkFlowConditionDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectWorkFlowConditionDto Properties to   AppProjectWorkFlowConditionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectWorkFlowConditionEntity aAppProjectWorkFlowConditionEntity,AppProjectWorkFlowConditionDto aAppProjectWorkFlowConditionDto)
        {        
 
      			aAppProjectWorkFlowConditionEntity.ProjectWorkFlowTaskId = aAppProjectWorkFlowConditionDto.ProjectWorkFlowTaskId;
      			aAppProjectWorkFlowConditionEntity.ProjectId = aAppProjectWorkFlowConditionDto.ProjectId;
      			aAppProjectWorkFlowConditionEntity.Name = aAppProjectWorkFlowConditionDto.Name;
      			aAppProjectWorkFlowConditionEntity.Description = aAppProjectWorkFlowConditionDto.Description;
      			aAppProjectWorkFlowConditionEntity.FormulaExpression = aAppProjectWorkFlowConditionDto.FormulaExpression;
      			aAppProjectWorkFlowConditionEntity.MonitorChildUnitId = aAppProjectWorkFlowConditionDto.MonitorChildUnitId;
      			aAppProjectWorkFlowConditionEntity.ConditionTransactionFieldId = aAppProjectWorkFlowConditionDto.ConditionTransactionFieldId;
      			aAppProjectWorkFlowConditionEntity.ConditionTypeId = aAppProjectWorkFlowConditionDto.ConditionTypeId;
      			aAppProjectWorkFlowConditionEntity.ConditionPredictValue = aAppProjectWorkFlowConditionDto.ConditionPredictValue;
      			aAppProjectWorkFlowConditionEntity.RowIdentity = aAppProjectWorkFlowConditionDto.RowIdentity;
      			aAppProjectWorkFlowConditionEntity.TriggerFlowOrder = aAppProjectWorkFlowConditionDto.TriggerFlowOrder;
 
  
   
    
      			aAppProjectWorkFlowConditionEntity.AppCreatedByCompanyId = aAppProjectWorkFlowConditionDto.AppCreatedByCompanyId;
      			aAppProjectWorkFlowConditionEntity.ConditionGuid = aAppProjectWorkFlowConditionDto.ConditionGuid;
			
			if(aAppProjectWorkFlowConditionDto.Id == null)
			{
				aAppProjectWorkFlowConditionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectWorkFlowConditionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectWorkFlowConditionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectWorkFlowConditionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectWorkFlowConditionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectWorkFlowConditionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectWorkFlowConditionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectWorkFlowConditionEntity, aAppProjectWorkFlowConditionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectWorkFlowConditionEntity aAppProjectWorkFlowConditionEntity,AppProjectWorkFlowConditionDto aAppProjectWorkFlowConditionDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectWorkFlowConditionEntity aAppProjectWorkFlowConditionEntity,AppProjectWorkFlowConditionDto aAppProjectWorkFlowConditionDto);
		
   
       
    }
}

 