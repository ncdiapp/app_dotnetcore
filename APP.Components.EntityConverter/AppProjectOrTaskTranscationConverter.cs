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
    /// Convert Properties between  AppProjectOrTaskTranscationEntity and  AppProjectOrTaskTranscationDto
    /// </summary>
    public static partial class AppProjectOrTaskTranscationConverter 
    {
         /// <summary>
        ///  Convert AppProjectOrTaskTranscationEntity To  AppProjectOrTaskTranscationDto
        /// </summary>
        public static AppProjectOrTaskTranscationDto ConvertEntityToDto(AppProjectOrTaskTranscationEntity aAppProjectOrTaskTranscationEntity)
        {        
    		AppProjectOrTaskTranscationDto aAppProjectOrTaskTranscationDto = new AppProjectOrTaskTranscationDto();
    		CopyEntityPropertyToDto( aAppProjectOrTaskTranscationEntity, aAppProjectOrTaskTranscationDto);          
			return aAppProjectOrTaskTranscationDto;
        }
		 /// <summary>
        ///  Convert AppProjectOrTaskTranscationEntity To  AppProjectOrTaskTranscationExDto
        /// </summary>
        public static AppProjectOrTaskTranscationExDto ConvertEntityToExDto(AppProjectOrTaskTranscationEntity aAppProjectOrTaskTranscationEntity)
        {        
    		AppProjectOrTaskTranscationExDto aAppProjectOrTaskTranscationExDto = new AppProjectOrTaskTranscationExDto();
			CopyEntityPropertyToDto( aAppProjectOrTaskTranscationEntity, aAppProjectOrTaskTranscationExDto);
			
			
			
            return aAppProjectOrTaskTranscationExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectOrTaskTranscationEntity To  AppProjectOrTaskTranscationDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectOrTaskTranscationEntity aAppProjectOrTaskTranscationEntity,AppProjectOrTaskTranscationDto aAppProjectOrTaskTranscationDto)
        {        
    		
           // aAppProjectOrTaskTranscationDto.StopChangeTracking();
 			aAppProjectOrTaskTranscationDto.Id = aAppProjectOrTaskTranscationEntity.ProejctTaskTranscationId;
 			aAppProjectOrTaskTranscationDto.ProejctId = aAppProjectOrTaskTranscationEntity.ProejctId;
 			aAppProjectOrTaskTranscationDto.ProjectTaskId = aAppProjectOrTaskTranscationEntity.ProjectTaskId;
 			aAppProjectOrTaskTranscationDto.TranscationId = aAppProjectOrTaskTranscationEntity.TranscationId;
 			aAppProjectOrTaskTranscationDto.TransactionRid = aAppProjectOrTaskTranscationEntity.TransactionRid;
 			aAppProjectOrTaskTranscationDto.AppCreatedById = aAppProjectOrTaskTranscationEntity.AppCreatedById;
 			aAppProjectOrTaskTranscationDto.AppCreatedDate = aAppProjectOrTaskTranscationEntity.AppCreatedDate;
 			aAppProjectOrTaskTranscationDto.AppModifiedDate = aAppProjectOrTaskTranscationEntity.AppModifiedDate;
 			aAppProjectOrTaskTranscationDto.AppModifiedById = aAppProjectOrTaskTranscationEntity.AppModifiedById;
 			aAppProjectOrTaskTranscationDto.AppCreatedByCompanyId = aAppProjectOrTaskTranscationEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectOrTaskTranscationDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrTaskTranscationEntity.AppCreatedDate);
                aAppProjectOrTaskTranscationDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectOrTaskTranscationEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectOrTaskTranscationEntity, aAppProjectOrTaskTranscationDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectOrTaskTranscationDto Properties to   AppProjectOrTaskTranscationEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectOrTaskTranscationEntity aAppProjectOrTaskTranscationEntity,AppProjectOrTaskTranscationDto aAppProjectOrTaskTranscationDto)
        {        
 
      			aAppProjectOrTaskTranscationEntity.ProejctId = aAppProjectOrTaskTranscationDto.ProejctId;
      			aAppProjectOrTaskTranscationEntity.ProjectTaskId = aAppProjectOrTaskTranscationDto.ProjectTaskId;
      			aAppProjectOrTaskTranscationEntity.TranscationId = aAppProjectOrTaskTranscationDto.TranscationId;
      			aAppProjectOrTaskTranscationEntity.TransactionRid = aAppProjectOrTaskTranscationDto.TransactionRid;
 
  
   
    
      			aAppProjectOrTaskTranscationEntity.AppCreatedByCompanyId = aAppProjectOrTaskTranscationDto.AppCreatedByCompanyId;
			
			if(aAppProjectOrTaskTranscationDto.Id == null)
			{
				aAppProjectOrTaskTranscationEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectOrTaskTranscationEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectOrTaskTranscationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectOrTaskTranscationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectOrTaskTranscationEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectOrTaskTranscationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectOrTaskTranscationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectOrTaskTranscationEntity, aAppProjectOrTaskTranscationDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectOrTaskTranscationEntity aAppProjectOrTaskTranscationEntity,AppProjectOrTaskTranscationDto aAppProjectOrTaskTranscationDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectOrTaskTranscationEntity aAppProjectOrTaskTranscationEntity,AppProjectOrTaskTranscationDto aAppProjectOrTaskTranscationDto);
		
   
       
    }
}

 