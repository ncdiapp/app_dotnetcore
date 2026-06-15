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
    /// Convert Properties between  AppPorjectWorkFlowTaskTimeSheetEntity and  AppPorjectWorkFlowTaskTimeSheetDto
    /// </summary>
    public static partial class AppPorjectWorkFlowTaskTimeSheetConverter 
    {
         /// <summary>
        ///  Convert AppPorjectWorkFlowTaskTimeSheetEntity To  AppPorjectWorkFlowTaskTimeSheetDto
        /// </summary>
        public static AppPorjectWorkFlowTaskTimeSheetDto ConvertEntityToDto(AppPorjectWorkFlowTaskTimeSheetEntity aAppPorjectWorkFlowTaskTimeSheetEntity)
        {        
    		AppPorjectWorkFlowTaskTimeSheetDto aAppPorjectWorkFlowTaskTimeSheetDto = new AppPorjectWorkFlowTaskTimeSheetDto();
    		CopyEntityPropertyToDto( aAppPorjectWorkFlowTaskTimeSheetEntity, aAppPorjectWorkFlowTaskTimeSheetDto);          
			return aAppPorjectWorkFlowTaskTimeSheetDto;
        }
		 /// <summary>
        ///  Convert AppPorjectWorkFlowTaskTimeSheetEntity To  AppPorjectWorkFlowTaskTimeSheetExDto
        /// </summary>
        public static AppPorjectWorkFlowTaskTimeSheetExDto ConvertEntityToExDto(AppPorjectWorkFlowTaskTimeSheetEntity aAppPorjectWorkFlowTaskTimeSheetEntity)
        {        
    		AppPorjectWorkFlowTaskTimeSheetExDto aAppPorjectWorkFlowTaskTimeSheetExDto = new AppPorjectWorkFlowTaskTimeSheetExDto();
			CopyEntityPropertyToDto( aAppPorjectWorkFlowTaskTimeSheetEntity, aAppPorjectWorkFlowTaskTimeSheetExDto);
			
			
			
            return aAppPorjectWorkFlowTaskTimeSheetExDto;
        }
		
		 /// <summary>
        ///  Convert AppPorjectWorkFlowTaskTimeSheetEntity To  AppPorjectWorkFlowTaskTimeSheetDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppPorjectWorkFlowTaskTimeSheetEntity aAppPorjectWorkFlowTaskTimeSheetEntity,AppPorjectWorkFlowTaskTimeSheetDto aAppPorjectWorkFlowTaskTimeSheetDto)
        {        
    		
           // aAppPorjectWorkFlowTaskTimeSheetDto.StopChangeTracking();
 			aAppPorjectWorkFlowTaskTimeSheetDto.Id = aAppPorjectWorkFlowTaskTimeSheetEntity.FlowTaskTimeSheetId;
 			aAppPorjectWorkFlowTaskTimeSheetDto.ProjectWorkFlowTaskId = aAppPorjectWorkFlowTaskTimeSheetEntity.ProjectWorkFlowTaskId;
 			aAppPorjectWorkFlowTaskTimeSheetDto.StartTime = aAppPorjectWorkFlowTaskTimeSheetEntity.StartTime;
 			aAppPorjectWorkFlowTaskTimeSheetDto.EndTime = aAppPorjectWorkFlowTaskTimeSheetEntity.EndTime;
 			aAppPorjectWorkFlowTaskTimeSheetDto.TimeSpan = aAppPorjectWorkFlowTaskTimeSheetEntity.TimeSpan;
 			aAppPorjectWorkFlowTaskTimeSheetDto.HourByRate = aAppPorjectWorkFlowTaskTimeSheetEntity.HourByRate;
 			aAppPorjectWorkFlowTaskTimeSheetDto.ApprovedById = aAppPorjectWorkFlowTaskTimeSheetEntity.ApprovedById;
 			aAppPorjectWorkFlowTaskTimeSheetDto.ApprovedByDate = aAppPorjectWorkFlowTaskTimeSheetEntity.ApprovedByDate;
 			aAppPorjectWorkFlowTaskTimeSheetDto.Comments = aAppPorjectWorkFlowTaskTimeSheetEntity.Comments;
 			aAppPorjectWorkFlowTaskTimeSheetDto.AppCreatedById = aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedById;
 			aAppPorjectWorkFlowTaskTimeSheetDto.AppCreatedDate = aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedDate;
 			aAppPorjectWorkFlowTaskTimeSheetDto.AppModifiedById = aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedById;
 			aAppPorjectWorkFlowTaskTimeSheetDto.AppModifiedDate = aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedDate;
 			aAppPorjectWorkFlowTaskTimeSheetDto.AppCreatedByCompanyId = aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedByCompanyId;
 			aAppPorjectWorkFlowTaskTimeSheetDto.DateId = aAppPorjectWorkFlowTaskTimeSheetEntity.DateId;
 			aAppPorjectWorkFlowTaskTimeSheetDto.ResourceUserId = aAppPorjectWorkFlowTaskTimeSheetEntity.ResourceUserId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppPorjectWorkFlowTaskTimeSheetDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedDate);
                aAppPorjectWorkFlowTaskTimeSheetDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppPorjectWorkFlowTaskTimeSheetEntity, aAppPorjectWorkFlowTaskTimeSheetDto);
		}
		
		 /// <summary>
        ///  Copy AppPorjectWorkFlowTaskTimeSheetDto Properties to   AppPorjectWorkFlowTaskTimeSheetEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppPorjectWorkFlowTaskTimeSheetEntity aAppPorjectWorkFlowTaskTimeSheetEntity,AppPorjectWorkFlowTaskTimeSheetDto aAppPorjectWorkFlowTaskTimeSheetDto)
        {        
 
      			aAppPorjectWorkFlowTaskTimeSheetEntity.ProjectWorkFlowTaskId = aAppPorjectWorkFlowTaskTimeSheetDto.ProjectWorkFlowTaskId;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.StartTime = aAppPorjectWorkFlowTaskTimeSheetDto.StartTime;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.EndTime = aAppPorjectWorkFlowTaskTimeSheetDto.EndTime;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.TimeSpan = aAppPorjectWorkFlowTaskTimeSheetDto.TimeSpan;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.HourByRate = aAppPorjectWorkFlowTaskTimeSheetDto.HourByRate;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.ApprovedById = aAppPorjectWorkFlowTaskTimeSheetDto.ApprovedById;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.ApprovedByDate = aAppPorjectWorkFlowTaskTimeSheetDto.ApprovedByDate;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.Comments = aAppPorjectWorkFlowTaskTimeSheetDto.Comments;
 
  
    
   
      			aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedByCompanyId = aAppPorjectWorkFlowTaskTimeSheetDto.AppCreatedByCompanyId;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.DateId = aAppPorjectWorkFlowTaskTimeSheetDto.DateId;
      			aAppPorjectWorkFlowTaskTimeSheetEntity.ResourceUserId = aAppPorjectWorkFlowTaskTimeSheetDto.ResourceUserId;
			
			if(aAppPorjectWorkFlowTaskTimeSheetDto.Id == null)
			{
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppPorjectWorkFlowTaskTimeSheetEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppPorjectWorkFlowTaskTimeSheetEntity, aAppPorjectWorkFlowTaskTimeSheetDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppPorjectWorkFlowTaskTimeSheetEntity aAppPorjectWorkFlowTaskTimeSheetEntity,AppPorjectWorkFlowTaskTimeSheetDto aAppPorjectWorkFlowTaskTimeSheetDto);
		
		static partial void OnCopyDtoToEntityDone(AppPorjectWorkFlowTaskTimeSheetEntity aAppPorjectWorkFlowTaskTimeSheetEntity,AppPorjectWorkFlowTaskTimeSheetDto aAppPorjectWorkFlowTaskTimeSheetDto);
		
   
       
    }
}

 