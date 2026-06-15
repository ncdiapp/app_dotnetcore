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
    /// Convert Properties between  AppWorkflowEntity and  AppWorkflowDto
    /// </summary>
    public static partial class AppWorkflowConverter 
    {
         /// <summary>
        ///  Convert AppWorkflowEntity To  AppWorkflowDto
        /// </summary>
        public static AppWorkflowDto ConvertEntityToDto(AppWorkflowEntity aAppWorkflowEntity)
        {        
    		AppWorkflowDto aAppWorkflowDto = new AppWorkflowDto();
    		CopyEntityPropertyToDto( aAppWorkflowEntity, aAppWorkflowDto);          
			return aAppWorkflowDto;
        }
		 /// <summary>
        ///  Convert AppWorkflowEntity To  AppWorkflowExDto
        /// </summary>
        public static AppWorkflowExDto ConvertEntityToExDto(AppWorkflowEntity aAppWorkflowEntity)
        {        
    		AppWorkflowExDto aAppWorkflowExDto = new AppWorkflowExDto();
			CopyEntityPropertyToDto( aAppWorkflowEntity, aAppWorkflowExDto);
			
			
			
            return aAppWorkflowExDto;
        }
		
		 /// <summary>
        ///  Convert AppWorkflowEntity To  AppWorkflowDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppWorkflowEntity aAppWorkflowEntity,AppWorkflowDto aAppWorkflowDto)
        {        
    		
           // aAppWorkflowDto.StopChangeTracking();
 			aAppWorkflowDto.Id = aAppWorkflowEntity.WorkflowId;
 			aAppWorkflowDto.Name = aAppWorkflowEntity.Name;
 			aAppWorkflowDto.Description = aAppWorkflowEntity.Description;
 			aAppWorkflowDto.TriggerType = aAppWorkflowEntity.TriggerType;
 			aAppWorkflowDto.AppCreatedById = aAppWorkflowEntity.AppCreatedById;
 			aAppWorkflowDto.AppCreatedDate = aAppWorkflowEntity.AppCreatedDate;
 			aAppWorkflowDto.AppModifiedDate = aAppWorkflowEntity.AppModifiedDate;
 			aAppWorkflowDto.AppModifiedById = aAppWorkflowEntity.AppModifiedById;
 			aAppWorkflowDto.AppCreatedByCompanyId = aAppWorkflowEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppWorkflowDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWorkflowEntity.AppCreatedDate);
                aAppWorkflowDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWorkflowEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppWorkflowEntity, aAppWorkflowDto);
		}
		
		 /// <summary>
        ///  Copy AppWorkflowDto Properties to   AppWorkflowEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppWorkflowEntity aAppWorkflowEntity,AppWorkflowDto aAppWorkflowDto)
        {        
 
      			aAppWorkflowEntity.Name = aAppWorkflowDto.Name;
      			aAppWorkflowEntity.Description = aAppWorkflowDto.Description;
      			aAppWorkflowEntity.TriggerType = aAppWorkflowDto.TriggerType;
 
  
   
    
      			aAppWorkflowEntity.AppCreatedByCompanyId = aAppWorkflowDto.AppCreatedByCompanyId;
			
			if(aAppWorkflowDto.Id == null)
			{
				aAppWorkflowEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppWorkflowEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppWorkflowEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWorkflowEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppWorkflowEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppWorkflowEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWorkflowEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppWorkflowEntity, aAppWorkflowDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppWorkflowEntity aAppWorkflowEntity,AppWorkflowDto aAppWorkflowDto);
		
		static partial void OnCopyDtoToEntityDone(AppWorkflowEntity aAppWorkflowEntity,AppWorkflowDto aAppWorkflowDto);
		
   
       
    }
}

 