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
    /// Convert Properties between  AppProjectSnapshotEntity and  AppProjectSnapshotDto
    /// </summary>
    public static partial class AppProjectSnapshotConverter 
    {
         /// <summary>
        ///  Convert AppProjectSnapshotEntity To  AppProjectSnapshotDto
        /// </summary>
        public static AppProjectSnapshotDto ConvertEntityToDto(AppProjectSnapshotEntity aAppProjectSnapshotEntity)
        {        
    		AppProjectSnapshotDto aAppProjectSnapshotDto = new AppProjectSnapshotDto();
    		CopyEntityPropertyToDto( aAppProjectSnapshotEntity, aAppProjectSnapshotDto);          
			return aAppProjectSnapshotDto;
        }
		 /// <summary>
        ///  Convert AppProjectSnapshotEntity To  AppProjectSnapshotExDto
        /// </summary>
        public static AppProjectSnapshotExDto ConvertEntityToExDto(AppProjectSnapshotEntity aAppProjectSnapshotEntity)
        {        
    		AppProjectSnapshotExDto aAppProjectSnapshotExDto = new AppProjectSnapshotExDto();
			CopyEntityPropertyToDto( aAppProjectSnapshotEntity, aAppProjectSnapshotExDto);
			
			
			
            return aAppProjectSnapshotExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectSnapshotEntity To  AppProjectSnapshotDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectSnapshotEntity aAppProjectSnapshotEntity,AppProjectSnapshotDto aAppProjectSnapshotDto)
        {        
    		
           // aAppProjectSnapshotDto.StopChangeTracking();
 			aAppProjectSnapshotDto.Id = aAppProjectSnapshotEntity.ProjectSnapshotId;
 			aAppProjectSnapshotDto.ManProejctId = aAppProjectSnapshotEntity.ManProejctId;
 			aAppProjectSnapshotDto.SnapshotName = aAppProjectSnapshotEntity.SnapshotName;
 			aAppProjectSnapshotDto.CopyProejctId = aAppProjectSnapshotEntity.CopyProejctId;
 			aAppProjectSnapshotDto.AppCreatedById = aAppProjectSnapshotEntity.AppCreatedById;
 			aAppProjectSnapshotDto.AppCreatedDate = aAppProjectSnapshotEntity.AppCreatedDate;
 			aAppProjectSnapshotDto.AppModifiedDate = aAppProjectSnapshotEntity.AppModifiedDate;
 			aAppProjectSnapshotDto.AppModifiedById = aAppProjectSnapshotEntity.AppModifiedById;
 			aAppProjectSnapshotDto.AppCreatedByCompanyId = aAppProjectSnapshotEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectSnapshotDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectSnapshotEntity.AppCreatedDate);
                aAppProjectSnapshotDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectSnapshotEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectSnapshotEntity, aAppProjectSnapshotDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectSnapshotDto Properties to   AppProjectSnapshotEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectSnapshotEntity aAppProjectSnapshotEntity,AppProjectSnapshotDto aAppProjectSnapshotDto)
        {        
 
      			aAppProjectSnapshotEntity.ManProejctId = aAppProjectSnapshotDto.ManProejctId;
      			aAppProjectSnapshotEntity.SnapshotName = aAppProjectSnapshotDto.SnapshotName;
      			aAppProjectSnapshotEntity.CopyProejctId = aAppProjectSnapshotDto.CopyProejctId;
 
  
   
    
      			aAppProjectSnapshotEntity.AppCreatedByCompanyId = aAppProjectSnapshotDto.AppCreatedByCompanyId;
			
			if(aAppProjectSnapshotDto.Id == null)
			{
				aAppProjectSnapshotEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectSnapshotEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectSnapshotEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectSnapshotEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectSnapshotEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectSnapshotEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectSnapshotEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectSnapshotEntity, aAppProjectSnapshotDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectSnapshotEntity aAppProjectSnapshotEntity,AppProjectSnapshotDto aAppProjectSnapshotDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectSnapshotEntity aAppProjectSnapshotEntity,AppProjectSnapshotDto aAppProjectSnapshotDto);
		
   
       
    }
}

 