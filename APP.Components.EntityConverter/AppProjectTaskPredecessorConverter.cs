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
    /// Convert Properties between  AppProjectTaskPredecessorEntity and  AppProjectTaskPredecessorDto
    /// </summary>
    public static partial class AppProjectTaskPredecessorConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskPredecessorEntity To  AppProjectTaskPredecessorDto
        /// </summary>
        public static AppProjectTaskPredecessorDto ConvertEntityToDto(AppProjectTaskPredecessorEntity aAppProjectTaskPredecessorEntity)
        {        
    		AppProjectTaskPredecessorDto aAppProjectTaskPredecessorDto = new AppProjectTaskPredecessorDto();
    		CopyEntityPropertyToDto( aAppProjectTaskPredecessorEntity, aAppProjectTaskPredecessorDto);          
			return aAppProjectTaskPredecessorDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskPredecessorEntity To  AppProjectTaskPredecessorExDto
        /// </summary>
        public static AppProjectTaskPredecessorExDto ConvertEntityToExDto(AppProjectTaskPredecessorEntity aAppProjectTaskPredecessorEntity)
        {        
    		AppProjectTaskPredecessorExDto aAppProjectTaskPredecessorExDto = new AppProjectTaskPredecessorExDto();
			CopyEntityPropertyToDto( aAppProjectTaskPredecessorEntity, aAppProjectTaskPredecessorExDto);
			
			
			
            return aAppProjectTaskPredecessorExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskPredecessorEntity To  AppProjectTaskPredecessorDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskPredecessorEntity aAppProjectTaskPredecessorEntity,AppProjectTaskPredecessorDto aAppProjectTaskPredecessorDto)
        {        
    		
           // aAppProjectTaskPredecessorDto.StopChangeTracking();
 			aAppProjectTaskPredecessorDto.Id = aAppProjectTaskPredecessorEntity.ProjectActivityPredecessorId;
 			aAppProjectTaskPredecessorDto.ProjectWorkFlowTaskId = aAppProjectTaskPredecessorEntity.ProjectWorkFlowTaskId;
 			aAppProjectTaskPredecessorDto.PredecessorId = aAppProjectTaskPredecessorEntity.PredecessorId;
 			aAppProjectTaskPredecessorDto.PathUilayout = aAppProjectTaskPredecessorEntity.PathUilayout;
 			aAppProjectTaskPredecessorDto.AppCreatedById = aAppProjectTaskPredecessorEntity.AppCreatedById;
 			aAppProjectTaskPredecessorDto.AppCreatedDate = aAppProjectTaskPredecessorEntity.AppCreatedDate;
 			aAppProjectTaskPredecessorDto.AppModifiedDate = aAppProjectTaskPredecessorEntity.AppModifiedDate;
 			aAppProjectTaskPredecessorDto.AppModifiedById = aAppProjectTaskPredecessorEntity.AppModifiedById;
 			aAppProjectTaskPredecessorDto.AppCreatedByCompanyId = aAppProjectTaskPredecessorEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskPredecessorDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskPredecessorEntity.AppCreatedDate);
                aAppProjectTaskPredecessorDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskPredecessorEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskPredecessorEntity, aAppProjectTaskPredecessorDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskPredecessorDto Properties to   AppProjectTaskPredecessorEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskPredecessorEntity aAppProjectTaskPredecessorEntity,AppProjectTaskPredecessorDto aAppProjectTaskPredecessorDto)
        {        
 
      			aAppProjectTaskPredecessorEntity.ProjectWorkFlowTaskId = aAppProjectTaskPredecessorDto.ProjectWorkFlowTaskId;
      			aAppProjectTaskPredecessorEntity.PredecessorId = aAppProjectTaskPredecessorDto.PredecessorId;
      			aAppProjectTaskPredecessorEntity.PathUilayout = aAppProjectTaskPredecessorDto.PathUilayout;
 
  
   
    
      			aAppProjectTaskPredecessorEntity.AppCreatedByCompanyId = aAppProjectTaskPredecessorDto.AppCreatedByCompanyId;
			
			if(aAppProjectTaskPredecessorDto.Id == null)
			{
				aAppProjectTaskPredecessorEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskPredecessorEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskPredecessorEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskPredecessorEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskPredecessorEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskPredecessorEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskPredecessorEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskPredecessorEntity, aAppProjectTaskPredecessorDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskPredecessorEntity aAppProjectTaskPredecessorEntity,AppProjectTaskPredecessorDto aAppProjectTaskPredecessorDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskPredecessorEntity aAppProjectTaskPredecessorEntity,AppProjectTaskPredecessorDto aAppProjectTaskPredecessorDto);
		
   
       
    }
}

 