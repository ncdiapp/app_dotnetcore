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
    /// Convert Properties between  AppProjectTaskTimeLogEntity and  AppProjectTaskTimeLogDto
    /// </summary>
    public static partial class AppProjectTaskTimeLogConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskTimeLogEntity To  AppProjectTaskTimeLogDto
        /// </summary>
        public static AppProjectTaskTimeLogDto ConvertEntityToDto(AppProjectTaskTimeLogEntity aAppProjectTaskTimeLogEntity)
        {        
    		AppProjectTaskTimeLogDto aAppProjectTaskTimeLogDto = new AppProjectTaskTimeLogDto();
    		CopyEntityPropertyToDto( aAppProjectTaskTimeLogEntity, aAppProjectTaskTimeLogDto);          
			return aAppProjectTaskTimeLogDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskTimeLogEntity To  AppProjectTaskTimeLogExDto
        /// </summary>
        public static AppProjectTaskTimeLogExDto ConvertEntityToExDto(AppProjectTaskTimeLogEntity aAppProjectTaskTimeLogEntity)
        {        
    		AppProjectTaskTimeLogExDto aAppProjectTaskTimeLogExDto = new AppProjectTaskTimeLogExDto();
			CopyEntityPropertyToDto( aAppProjectTaskTimeLogEntity, aAppProjectTaskTimeLogExDto);
			
			
			
            return aAppProjectTaskTimeLogExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskTimeLogEntity To  AppProjectTaskTimeLogDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskTimeLogEntity aAppProjectTaskTimeLogEntity,AppProjectTaskTimeLogDto aAppProjectTaskTimeLogDto)
        {        
    		
           // aAppProjectTaskTimeLogDto.StopChangeTracking();
 			aAppProjectTaskTimeLogDto.Id = aAppProjectTaskTimeLogEntity.TaksTimeLogId;
 			aAppProjectTaskTimeLogDto.ProjectTaskId = aAppProjectTaskTimeLogEntity.ProjectTaskId;
 			aAppProjectTaskTimeLogDto.TeamMemberId = aAppProjectTaskTimeLogEntity.TeamMemberId;
 			aAppProjectTaskTimeLogDto.TiemSpanHours = aAppProjectTaskTimeLogEntity.TiemSpanHours;
 			aAppProjectTaskTimeLogDto.RateByHour = aAppProjectTaskTimeLogEntity.RateByHour;
 			aAppProjectTaskTimeLogDto.ApprovedBy = aAppProjectTaskTimeLogEntity.ApprovedBy;
 			aAppProjectTaskTimeLogDto.AppCreatedById = aAppProjectTaskTimeLogEntity.AppCreatedById;
 			aAppProjectTaskTimeLogDto.AppCreatedDate = aAppProjectTaskTimeLogEntity.AppCreatedDate;
 			aAppProjectTaskTimeLogDto.AppModifiedDate = aAppProjectTaskTimeLogEntity.AppModifiedDate;
 			aAppProjectTaskTimeLogDto.AppModifiedById = aAppProjectTaskTimeLogEntity.AppModifiedById;
 			aAppProjectTaskTimeLogDto.AppCreatedByCompanyId = aAppProjectTaskTimeLogEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskTimeLogDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskTimeLogEntity.AppCreatedDate);
                aAppProjectTaskTimeLogDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskTimeLogEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskTimeLogEntity, aAppProjectTaskTimeLogDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskTimeLogDto Properties to   AppProjectTaskTimeLogEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskTimeLogEntity aAppProjectTaskTimeLogEntity,AppProjectTaskTimeLogDto aAppProjectTaskTimeLogDto)
        {        
 
      			aAppProjectTaskTimeLogEntity.ProjectTaskId = aAppProjectTaskTimeLogDto.ProjectTaskId;
      			aAppProjectTaskTimeLogEntity.TeamMemberId = aAppProjectTaskTimeLogDto.TeamMemberId;
      			aAppProjectTaskTimeLogEntity.TiemSpanHours = aAppProjectTaskTimeLogDto.TiemSpanHours;
      			aAppProjectTaskTimeLogEntity.RateByHour = aAppProjectTaskTimeLogDto.RateByHour;
      			aAppProjectTaskTimeLogEntity.ApprovedBy = aAppProjectTaskTimeLogDto.ApprovedBy;
 
  
   
    
      			aAppProjectTaskTimeLogEntity.AppCreatedByCompanyId = aAppProjectTaskTimeLogDto.AppCreatedByCompanyId;
			
			if(aAppProjectTaskTimeLogDto.Id == null)
			{
				aAppProjectTaskTimeLogEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskTimeLogEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskTimeLogEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskTimeLogEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskTimeLogEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskTimeLogEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskTimeLogEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskTimeLogEntity, aAppProjectTaskTimeLogDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskTimeLogEntity aAppProjectTaskTimeLogEntity,AppProjectTaskTimeLogDto aAppProjectTaskTimeLogDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskTimeLogEntity aAppProjectTaskTimeLogEntity,AppProjectTaskTimeLogDto aAppProjectTaskTimeLogDto);
		
   
       
    }
}

 