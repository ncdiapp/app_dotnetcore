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
    /// Convert Properties between  AppFormGroupEntity and  AppFormGroupDto
    /// </summary>
    public static partial class AppFormGroupConverter 
    {
         /// <summary>
        ///  Convert AppFormGroupEntity To  AppFormGroupDto
        /// </summary>
        public static AppFormGroupDto ConvertEntityToDto(AppFormGroupEntity aAppFormGroupEntity)
        {        
    		AppFormGroupDto aAppFormGroupDto = new AppFormGroupDto();
    		CopyEntityPropertyToDto( aAppFormGroupEntity, aAppFormGroupDto);          
			return aAppFormGroupDto;
        }
		 /// <summary>
        ///  Convert AppFormGroupEntity To  AppFormGroupExDto
        /// </summary>
        public static AppFormGroupExDto ConvertEntityToExDto(AppFormGroupEntity aAppFormGroupEntity)
        {        
    		AppFormGroupExDto aAppFormGroupExDto = new AppFormGroupExDto();
			CopyEntityPropertyToDto( aAppFormGroupEntity, aAppFormGroupExDto);
			
			
			
            return aAppFormGroupExDto;
        }
		
		 /// <summary>
        ///  Convert AppFormGroupEntity To  AppFormGroupDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFormGroupEntity aAppFormGroupEntity,AppFormGroupDto aAppFormGroupDto)
        {        
    		
           // aAppFormGroupDto.StopChangeTracking();
 			aAppFormGroupDto.Id = aAppFormGroupEntity.FormGroupId;
 			aAppFormGroupDto.GroupName = aAppFormGroupEntity.GroupName;
 			aAppFormGroupDto.Description = aAppFormGroupEntity.Description;
 			aAppFormGroupDto.AppCreatedById = aAppFormGroupEntity.AppCreatedById;
 			aAppFormGroupDto.AppCreatedDate = aAppFormGroupEntity.AppCreatedDate;
 			aAppFormGroupDto.AppModifiedDate = aAppFormGroupEntity.AppModifiedDate;
 			aAppFormGroupDto.AppModifiedById = aAppFormGroupEntity.AppModifiedById;
 			aAppFormGroupDto.AppCreatedByCompanyId = aAppFormGroupEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFormGroupDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormGroupEntity.AppCreatedDate);
                aAppFormGroupDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormGroupEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFormGroupEntity, aAppFormGroupDto);
		}
		
		 /// <summary>
        ///  Copy AppFormGroupDto Properties to   AppFormGroupEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFormGroupEntity aAppFormGroupEntity,AppFormGroupDto aAppFormGroupDto)
        {        
 
      			aAppFormGroupEntity.GroupName = aAppFormGroupDto.GroupName;
      			aAppFormGroupEntity.Description = aAppFormGroupDto.Description;
 
  
   
    
      			aAppFormGroupEntity.AppCreatedByCompanyId = aAppFormGroupDto.AppCreatedByCompanyId;
			
			if(aAppFormGroupDto.Id == null)
			{
				aAppFormGroupEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormGroupEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFormGroupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormGroupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormGroupEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFormGroupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormGroupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFormGroupEntity, aAppFormGroupDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFormGroupEntity aAppFormGroupEntity,AppFormGroupDto aAppFormGroupDto);
		
		static partial void OnCopyDtoToEntityDone(AppFormGroupEntity aAppFormGroupEntity,AppFormGroupDto aAppFormGroupDto);
		
   
       
    }
}

 