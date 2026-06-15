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
    /// Convert Properties between  AppProjectPrivacyEntity and  AppProjectPrivacyDto
    /// </summary>
    public static partial class AppProjectPrivacyConverter 
    {
         /// <summary>
        ///  Convert AppProjectPrivacyEntity To  AppProjectPrivacyDto
        /// </summary>
        public static AppProjectPrivacyDto ConvertEntityToDto(AppProjectPrivacyEntity aAppProjectPrivacyEntity)
        {        
    		AppProjectPrivacyDto aAppProjectPrivacyDto = new AppProjectPrivacyDto();
    		CopyEntityPropertyToDto( aAppProjectPrivacyEntity, aAppProjectPrivacyDto);          
			return aAppProjectPrivacyDto;
        }
		 /// <summary>
        ///  Convert AppProjectPrivacyEntity To  AppProjectPrivacyExDto
        /// </summary>
        public static AppProjectPrivacyExDto ConvertEntityToExDto(AppProjectPrivacyEntity aAppProjectPrivacyEntity)
        {        
    		AppProjectPrivacyExDto aAppProjectPrivacyExDto = new AppProjectPrivacyExDto();
			CopyEntityPropertyToDto( aAppProjectPrivacyEntity, aAppProjectPrivacyExDto);
			
			
			
            return aAppProjectPrivacyExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectPrivacyEntity To  AppProjectPrivacyDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectPrivacyEntity aAppProjectPrivacyEntity,AppProjectPrivacyDto aAppProjectPrivacyDto)
        {        
    		
           // aAppProjectPrivacyDto.StopChangeTracking();
 			aAppProjectPrivacyDto.Id = aAppProjectPrivacyEntity.ProvacyId;
 			aAppProjectPrivacyDto.ProjectId = aAppProjectPrivacyEntity.ProjectId;
 			aAppProjectPrivacyDto.UserId = aAppProjectPrivacyEntity.UserId;
 			aAppProjectPrivacyDto.RoleId = aAppProjectPrivacyEntity.RoleId;
 			aAppProjectPrivacyDto.ProjectTeamId = aAppProjectPrivacyEntity.ProjectTeamId;
 			aAppProjectPrivacyDto.IsAllowedToEdit = aAppProjectPrivacyEntity.IsAllowedToEdit;
 			aAppProjectPrivacyDto.AppCreatedById = aAppProjectPrivacyEntity.AppCreatedById;
 			aAppProjectPrivacyDto.AppCreatedDate = aAppProjectPrivacyEntity.AppCreatedDate;
 			aAppProjectPrivacyDto.AppModifiedDate = aAppProjectPrivacyEntity.AppModifiedDate;
 			aAppProjectPrivacyDto.AppModifiedById = aAppProjectPrivacyEntity.AppModifiedById;
 			aAppProjectPrivacyDto.AppCreatedByCompanyId = aAppProjectPrivacyEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectPrivacyDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPrivacyEntity.AppCreatedDate);
                aAppProjectPrivacyDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectPrivacyEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectPrivacyEntity, aAppProjectPrivacyDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectPrivacyDto Properties to   AppProjectPrivacyEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectPrivacyEntity aAppProjectPrivacyEntity,AppProjectPrivacyDto aAppProjectPrivacyDto)
        {        
 
      			aAppProjectPrivacyEntity.ProjectId = aAppProjectPrivacyDto.ProjectId;
      			aAppProjectPrivacyEntity.UserId = aAppProjectPrivacyDto.UserId;
      			aAppProjectPrivacyEntity.RoleId = aAppProjectPrivacyDto.RoleId;
      			aAppProjectPrivacyEntity.ProjectTeamId = aAppProjectPrivacyDto.ProjectTeamId;
      			aAppProjectPrivacyEntity.IsAllowedToEdit = aAppProjectPrivacyDto.IsAllowedToEdit;
 
  
   
    
      			aAppProjectPrivacyEntity.AppCreatedByCompanyId = aAppProjectPrivacyDto.AppCreatedByCompanyId;
			
			if(aAppProjectPrivacyDto.Id == null)
			{
				aAppProjectPrivacyEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPrivacyEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectPrivacyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPrivacyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectPrivacyEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectPrivacyEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectPrivacyEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectPrivacyEntity, aAppProjectPrivacyDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectPrivacyEntity aAppProjectPrivacyEntity,AppProjectPrivacyDto aAppProjectPrivacyDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectPrivacyEntity aAppProjectPrivacyEntity,AppProjectPrivacyDto aAppProjectPrivacyDto);
		
   
       
    }
}

 