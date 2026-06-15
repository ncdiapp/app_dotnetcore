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
    /// Convert Properties between  AppProjectTaskResourcePlannedHoursEntity and  AppProjectTaskResourcePlannedHoursDto
    /// </summary>
    public static partial class AppProjectTaskResourcePlannedHoursConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskResourcePlannedHoursEntity To  AppProjectTaskResourcePlannedHoursDto
        /// </summary>
        public static AppProjectTaskResourcePlannedHoursDto ConvertEntityToDto(AppProjectTaskResourcePlannedHoursEntity aAppProjectTaskResourcePlannedHoursEntity)
        {        
    		AppProjectTaskResourcePlannedHoursDto aAppProjectTaskResourcePlannedHoursDto = new AppProjectTaskResourcePlannedHoursDto();
    		CopyEntityPropertyToDto( aAppProjectTaskResourcePlannedHoursEntity, aAppProjectTaskResourcePlannedHoursDto);          
			return aAppProjectTaskResourcePlannedHoursDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskResourcePlannedHoursEntity To  AppProjectTaskResourcePlannedHoursExDto
        /// </summary>
        public static AppProjectTaskResourcePlannedHoursExDto ConvertEntityToExDto(AppProjectTaskResourcePlannedHoursEntity aAppProjectTaskResourcePlannedHoursEntity)
        {        
    		AppProjectTaskResourcePlannedHoursExDto aAppProjectTaskResourcePlannedHoursExDto = new AppProjectTaskResourcePlannedHoursExDto();
			CopyEntityPropertyToDto( aAppProjectTaskResourcePlannedHoursEntity, aAppProjectTaskResourcePlannedHoursExDto);
			
			
			
            return aAppProjectTaskResourcePlannedHoursExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskResourcePlannedHoursEntity To  AppProjectTaskResourcePlannedHoursDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskResourcePlannedHoursEntity aAppProjectTaskResourcePlannedHoursEntity,AppProjectTaskResourcePlannedHoursDto aAppProjectTaskResourcePlannedHoursDto)
        {        
    		
           // aAppProjectTaskResourcePlannedHoursDto.StopChangeTracking();
 			aAppProjectTaskResourcePlannedHoursDto.Id = aAppProjectTaskResourcePlannedHoursEntity.PlannedWorkHourId;
 			aAppProjectTaskResourcePlannedHoursDto.TaskResourceId = aAppProjectTaskResourcePlannedHoursEntity.TaskResourceId;
 			aAppProjectTaskResourcePlannedHoursDto.DateId = aAppProjectTaskResourcePlannedHoursEntity.DateId;
 			aAppProjectTaskResourcePlannedHoursDto.PlannedWorkHours = aAppProjectTaskResourcePlannedHoursEntity.PlannedWorkHours;
 			aAppProjectTaskResourcePlannedHoursDto.StartTime = aAppProjectTaskResourcePlannedHoursEntity.StartTime;
 			aAppProjectTaskResourcePlannedHoursDto.EndTime = aAppProjectTaskResourcePlannedHoursEntity.EndTime;
 			aAppProjectTaskResourcePlannedHoursDto.AppCreatedById = aAppProjectTaskResourcePlannedHoursEntity.AppCreatedById;
 			aAppProjectTaskResourcePlannedHoursDto.AppCreatedDate = aAppProjectTaskResourcePlannedHoursEntity.AppCreatedDate;
 			aAppProjectTaskResourcePlannedHoursDto.AppModifiedDate = aAppProjectTaskResourcePlannedHoursEntity.AppModifiedDate;
 			aAppProjectTaskResourcePlannedHoursDto.AppModifiedById = aAppProjectTaskResourcePlannedHoursEntity.AppModifiedById;
 			aAppProjectTaskResourcePlannedHoursDto.AppCreatedByCompanyId = aAppProjectTaskResourcePlannedHoursEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskResourcePlannedHoursDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskResourcePlannedHoursEntity.AppCreatedDate);
                aAppProjectTaskResourcePlannedHoursDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskResourcePlannedHoursEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskResourcePlannedHoursEntity, aAppProjectTaskResourcePlannedHoursDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskResourcePlannedHoursDto Properties to   AppProjectTaskResourcePlannedHoursEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskResourcePlannedHoursEntity aAppProjectTaskResourcePlannedHoursEntity,AppProjectTaskResourcePlannedHoursDto aAppProjectTaskResourcePlannedHoursDto)
        {        
 
      			aAppProjectTaskResourcePlannedHoursEntity.TaskResourceId = aAppProjectTaskResourcePlannedHoursDto.TaskResourceId;
      			aAppProjectTaskResourcePlannedHoursEntity.DateId = aAppProjectTaskResourcePlannedHoursDto.DateId;
      			aAppProjectTaskResourcePlannedHoursEntity.PlannedWorkHours = aAppProjectTaskResourcePlannedHoursDto.PlannedWorkHours;
      			aAppProjectTaskResourcePlannedHoursEntity.StartTime = aAppProjectTaskResourcePlannedHoursDto.StartTime;
      			aAppProjectTaskResourcePlannedHoursEntity.EndTime = aAppProjectTaskResourcePlannedHoursDto.EndTime;
 
  
   
    
      			aAppProjectTaskResourcePlannedHoursEntity.AppCreatedByCompanyId = aAppProjectTaskResourcePlannedHoursDto.AppCreatedByCompanyId;
			
			if(aAppProjectTaskResourcePlannedHoursDto.Id == null)
			{
				aAppProjectTaskResourcePlannedHoursEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskResourcePlannedHoursEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskResourcePlannedHoursEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskResourcePlannedHoursEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskResourcePlannedHoursEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskResourcePlannedHoursEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskResourcePlannedHoursEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskResourcePlannedHoursEntity, aAppProjectTaskResourcePlannedHoursDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskResourcePlannedHoursEntity aAppProjectTaskResourcePlannedHoursEntity,AppProjectTaskResourcePlannedHoursDto aAppProjectTaskResourcePlannedHoursDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskResourcePlannedHoursEntity aAppProjectTaskResourcePlannedHoursEntity,AppProjectTaskResourcePlannedHoursDto aAppProjectTaskResourcePlannedHoursDto);
		
   
       
    }
}

 