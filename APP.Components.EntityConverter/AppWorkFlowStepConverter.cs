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
    /// Convert Properties between  AppWorkFlowStepEntity and  AppWorkFlowStepDto
    /// </summary>
    public static partial class AppWorkFlowStepConverter 
    {
         /// <summary>
        ///  Convert AppWorkFlowStepEntity To  AppWorkFlowStepDto
        /// </summary>
        public static AppWorkFlowStepDto ConvertEntityToDto(AppWorkFlowStepEntity aAppWorkFlowStepEntity)
        {        
    		AppWorkFlowStepDto aAppWorkFlowStepDto = new AppWorkFlowStepDto();
    		CopyEntityPropertyToDto( aAppWorkFlowStepEntity, aAppWorkFlowStepDto);          
			return aAppWorkFlowStepDto;
        }
		 /// <summary>
        ///  Convert AppWorkFlowStepEntity To  AppWorkFlowStepExDto
        /// </summary>
        public static AppWorkFlowStepExDto ConvertEntityToExDto(AppWorkFlowStepEntity aAppWorkFlowStepEntity)
        {        
    		AppWorkFlowStepExDto aAppWorkFlowStepExDto = new AppWorkFlowStepExDto();
			CopyEntityPropertyToDto( aAppWorkFlowStepEntity, aAppWorkFlowStepExDto);
			
			
			
            return aAppWorkFlowStepExDto;
        }
		
		 /// <summary>
        ///  Convert AppWorkFlowStepEntity To  AppWorkFlowStepDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppWorkFlowStepEntity aAppWorkFlowStepEntity,AppWorkFlowStepDto aAppWorkFlowStepDto)
        {        
    		
           // aAppWorkFlowStepDto.StopChangeTracking();
 			aAppWorkFlowStepDto.Id = aAppWorkFlowStepEntity.WorkflowStepId;
 			aAppWorkFlowStepDto.WorkflowId = aAppWorkFlowStepEntity.WorkflowId;
 			aAppWorkFlowStepDto.StepCode = aAppWorkFlowStepEntity.StepCode;
 			aAppWorkFlowStepDto.Description = aAppWorkFlowStepEntity.Description;
 			aAppWorkFlowStepDto.DataModelTransacionId = aAppWorkFlowStepEntity.DataModelTransacionId;
 			aAppWorkFlowStepDto.BooleanFormulaExpression = aAppWorkFlowStepEntity.BooleanFormulaExpression;
 			aAppWorkFlowStepDto.SuccessiveStepActionId = aAppWorkFlowStepEntity.SuccessiveStepActionId;
 			aAppWorkFlowStepDto.IsDecisionStep = aAppWorkFlowStepEntity.IsDecisionStep;
 			aAppWorkFlowStepDto.DecisionStepPredictValue = aAppWorkFlowStepEntity.DecisionStepPredictValue;
 			aAppWorkFlowStepDto.PathUilayout = aAppWorkFlowStepEntity.PathUilayout;
 			aAppWorkFlowStepDto.RowIdentity = aAppWorkFlowStepEntity.RowIdentity;
 			aAppWorkFlowStepDto.AppCreatedById = aAppWorkFlowStepEntity.AppCreatedById;
 			aAppWorkFlowStepDto.AppCreatedDate = aAppWorkFlowStepEntity.AppCreatedDate;
 			aAppWorkFlowStepDto.AppModifiedDate = aAppWorkFlowStepEntity.AppModifiedDate;
 			aAppWorkFlowStepDto.AppModifiedById = aAppWorkFlowStepEntity.AppModifiedById;
 			aAppWorkFlowStepDto.AppCreatedByCompanyId = aAppWorkFlowStepEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppWorkFlowStepDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWorkFlowStepEntity.AppCreatedDate);
                aAppWorkFlowStepDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWorkFlowStepEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppWorkFlowStepEntity, aAppWorkFlowStepDto);
		}
		
		 /// <summary>
        ///  Copy AppWorkFlowStepDto Properties to   AppWorkFlowStepEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppWorkFlowStepEntity aAppWorkFlowStepEntity,AppWorkFlowStepDto aAppWorkFlowStepDto)
        {        
 
      			aAppWorkFlowStepEntity.WorkflowId = aAppWorkFlowStepDto.WorkflowId;
      			aAppWorkFlowStepEntity.StepCode = aAppWorkFlowStepDto.StepCode;
      			aAppWorkFlowStepEntity.Description = aAppWorkFlowStepDto.Description;
      			aAppWorkFlowStepEntity.DataModelTransacionId = aAppWorkFlowStepDto.DataModelTransacionId;
      			aAppWorkFlowStepEntity.BooleanFormulaExpression = aAppWorkFlowStepDto.BooleanFormulaExpression;
      			aAppWorkFlowStepEntity.SuccessiveStepActionId = aAppWorkFlowStepDto.SuccessiveStepActionId;
      			aAppWorkFlowStepEntity.IsDecisionStep = aAppWorkFlowStepDto.IsDecisionStep;
      			aAppWorkFlowStepEntity.DecisionStepPredictValue = aAppWorkFlowStepDto.DecisionStepPredictValue;
      			aAppWorkFlowStepEntity.PathUilayout = aAppWorkFlowStepDto.PathUilayout;
      			aAppWorkFlowStepEntity.RowIdentity = aAppWorkFlowStepDto.RowIdentity;
 
  
   
    
      			aAppWorkFlowStepEntity.AppCreatedByCompanyId = aAppWorkFlowStepDto.AppCreatedByCompanyId;
			
			if(aAppWorkFlowStepDto.Id == null)
			{
				aAppWorkFlowStepEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppWorkFlowStepEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppWorkFlowStepEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWorkFlowStepEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppWorkFlowStepEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppWorkFlowStepEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWorkFlowStepEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppWorkFlowStepEntity, aAppWorkFlowStepDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppWorkFlowStepEntity aAppWorkFlowStepEntity,AppWorkFlowStepDto aAppWorkFlowStepDto);
		
		static partial void OnCopyDtoToEntityDone(AppWorkFlowStepEntity aAppWorkFlowStepEntity,AppWorkFlowStepDto aAppWorkFlowStepDto);
		
   
       
    }
}

 