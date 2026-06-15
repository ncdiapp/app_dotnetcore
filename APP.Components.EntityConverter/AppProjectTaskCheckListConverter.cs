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
    /// Convert Properties between  AppProjectTaskCheckListEntity and  AppProjectTaskCheckListDto
    /// </summary>
    public static partial class AppProjectTaskCheckListConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskCheckListEntity To  AppProjectTaskCheckListDto
        /// </summary>
        public static AppProjectTaskCheckListDto ConvertEntityToDto(AppProjectTaskCheckListEntity aAppProjectTaskCheckListEntity)
        {        
    		AppProjectTaskCheckListDto aAppProjectTaskCheckListDto = new AppProjectTaskCheckListDto();
    		CopyEntityPropertyToDto( aAppProjectTaskCheckListEntity, aAppProjectTaskCheckListDto);          
			return aAppProjectTaskCheckListDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskCheckListEntity To  AppProjectTaskCheckListExDto
        /// </summary>
        public static AppProjectTaskCheckListExDto ConvertEntityToExDto(AppProjectTaskCheckListEntity aAppProjectTaskCheckListEntity)
        {        
    		AppProjectTaskCheckListExDto aAppProjectTaskCheckListExDto = new AppProjectTaskCheckListExDto();
			CopyEntityPropertyToDto( aAppProjectTaskCheckListEntity, aAppProjectTaskCheckListExDto);
			
			
			
            return aAppProjectTaskCheckListExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskCheckListEntity To  AppProjectTaskCheckListDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskCheckListEntity aAppProjectTaskCheckListEntity,AppProjectTaskCheckListDto aAppProjectTaskCheckListDto)
        {        
    		
           // aAppProjectTaskCheckListDto.StopChangeTracking();
 			aAppProjectTaskCheckListDto.Id = aAppProjectTaskCheckListEntity.TaskCheckListId;
 			aAppProjectTaskCheckListDto.ProjectTaskId = aAppProjectTaskCheckListEntity.ProjectTaskId;
 			aAppProjectTaskCheckListDto.CheckItemDesc = aAppProjectTaskCheckListEntity.CheckItemDesc;
 			aAppProjectTaskCheckListDto.IsPass = aAppProjectTaskCheckListEntity.IsPass;
 			aAppProjectTaskCheckListDto.Comments = aAppProjectTaskCheckListEntity.Comments;
 			aAppProjectTaskCheckListDto.AppCreatedById = aAppProjectTaskCheckListEntity.AppCreatedById;
 			aAppProjectTaskCheckListDto.AppCreatedDate = aAppProjectTaskCheckListEntity.AppCreatedDate;
 			aAppProjectTaskCheckListDto.AppModifiedDate = aAppProjectTaskCheckListEntity.AppModifiedDate;
 			aAppProjectTaskCheckListDto.AppModifiedById = aAppProjectTaskCheckListEntity.AppModifiedById;
 			aAppProjectTaskCheckListDto.AppCreatedByCompanyId = aAppProjectTaskCheckListEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskCheckListDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskCheckListEntity.AppCreatedDate);
                aAppProjectTaskCheckListDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskCheckListEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskCheckListEntity, aAppProjectTaskCheckListDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskCheckListDto Properties to   AppProjectTaskCheckListEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskCheckListEntity aAppProjectTaskCheckListEntity,AppProjectTaskCheckListDto aAppProjectTaskCheckListDto)
        {        
 
      			aAppProjectTaskCheckListEntity.ProjectTaskId = aAppProjectTaskCheckListDto.ProjectTaskId;
      			aAppProjectTaskCheckListEntity.CheckItemDesc = aAppProjectTaskCheckListDto.CheckItemDesc;
      			aAppProjectTaskCheckListEntity.IsPass = aAppProjectTaskCheckListDto.IsPass;
      			aAppProjectTaskCheckListEntity.Comments = aAppProjectTaskCheckListDto.Comments;
 
  
   
    
      			aAppProjectTaskCheckListEntity.AppCreatedByCompanyId = aAppProjectTaskCheckListDto.AppCreatedByCompanyId;
			
			if(aAppProjectTaskCheckListDto.Id == null)
			{
				aAppProjectTaskCheckListEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskCheckListEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskCheckListEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskCheckListEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskCheckListEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskCheckListEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskCheckListEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskCheckListEntity, aAppProjectTaskCheckListDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskCheckListEntity aAppProjectTaskCheckListEntity,AppProjectTaskCheckListDto aAppProjectTaskCheckListDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskCheckListEntity aAppProjectTaskCheckListEntity,AppProjectTaskCheckListDto aAppProjectTaskCheckListDto);
		
   
       
    }
}

 