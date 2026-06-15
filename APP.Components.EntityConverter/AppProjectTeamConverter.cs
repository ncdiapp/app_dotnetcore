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
    /// Convert Properties between  AppProjectTeamEntity and  AppProjectTeamDto
    /// </summary>
    public static partial class AppProjectTeamConverter 
    {
         /// <summary>
        ///  Convert AppProjectTeamEntity To  AppProjectTeamDto
        /// </summary>
        public static AppProjectTeamDto ConvertEntityToDto(AppProjectTeamEntity aAppProjectTeamEntity)
        {        
    		AppProjectTeamDto aAppProjectTeamDto = new AppProjectTeamDto();
    		CopyEntityPropertyToDto( aAppProjectTeamEntity, aAppProjectTeamDto);          
			return aAppProjectTeamDto;
        }
		 /// <summary>
        ///  Convert AppProjectTeamEntity To  AppProjectTeamExDto
        /// </summary>
        public static AppProjectTeamExDto ConvertEntityToExDto(AppProjectTeamEntity aAppProjectTeamEntity)
        {        
    		AppProjectTeamExDto aAppProjectTeamExDto = new AppProjectTeamExDto();
			CopyEntityPropertyToDto( aAppProjectTeamEntity, aAppProjectTeamExDto);
			
			
			
            return aAppProjectTeamExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTeamEntity To  AppProjectTeamDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTeamEntity aAppProjectTeamEntity,AppProjectTeamDto aAppProjectTeamDto)
        {        
    		
           // aAppProjectTeamDto.StopChangeTracking();
 			aAppProjectTeamDto.Id = aAppProjectTeamEntity.ProejctTeamId;
 			aAppProjectTeamDto.TeamName = aAppProjectTeamEntity.TeamName;
 			aAppProjectTeamDto.Description = aAppProjectTeamEntity.Description;
 			aAppProjectTeamDto.IsPrefinedTeam = aAppProjectTeamEntity.IsPrefinedTeam;
 			aAppProjectTeamDto.EmPrivacy = aAppProjectTeamEntity.EmPrivacy;
 			aAppProjectTeamDto.ParticipatedDmainId = aAppProjectTeamEntity.ParticipatedDmainId;
 			aAppProjectTeamDto.AppCreatedById = aAppProjectTeamEntity.AppCreatedById;
 			aAppProjectTeamDto.AppCreatedDate = aAppProjectTeamEntity.AppCreatedDate;
 			aAppProjectTeamDto.AppModifiedDate = aAppProjectTeamEntity.AppModifiedDate;
 			aAppProjectTeamDto.AppModifiedById = aAppProjectTeamEntity.AppModifiedById;
 			aAppProjectTeamDto.AppCreatedByCompanyId = aAppProjectTeamEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTeamDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTeamEntity.AppCreatedDate);
                aAppProjectTeamDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTeamEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTeamEntity, aAppProjectTeamDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTeamDto Properties to   AppProjectTeamEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTeamEntity aAppProjectTeamEntity,AppProjectTeamDto aAppProjectTeamDto)
        {        
 
      			aAppProjectTeamEntity.TeamName = aAppProjectTeamDto.TeamName;
      			aAppProjectTeamEntity.Description = aAppProjectTeamDto.Description;
      			aAppProjectTeamEntity.IsPrefinedTeam = aAppProjectTeamDto.IsPrefinedTeam;
      			aAppProjectTeamEntity.EmPrivacy = aAppProjectTeamDto.EmPrivacy;
      			aAppProjectTeamEntity.ParticipatedDmainId = aAppProjectTeamDto.ParticipatedDmainId;
 
  
   
    
      			aAppProjectTeamEntity.AppCreatedByCompanyId = aAppProjectTeamDto.AppCreatedByCompanyId;
			
			if(aAppProjectTeamDto.Id == null)
			{
				aAppProjectTeamEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTeamEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTeamEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTeamEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTeamEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTeamEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTeamEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTeamEntity, aAppProjectTeamDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTeamEntity aAppProjectTeamEntity,AppProjectTeamDto aAppProjectTeamDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTeamEntity aAppProjectTeamEntity,AppProjectTeamDto aAppProjectTeamDto);
		
   
       
    }
}

 