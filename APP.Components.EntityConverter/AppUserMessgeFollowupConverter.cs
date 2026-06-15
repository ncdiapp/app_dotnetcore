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
    /// Convert Properties between  AppUserMessgeFollowupEntity and  AppUserMessgeFollowupDto
    /// </summary>
    public static partial class AppUserMessgeFollowupConverter 
    {
         /// <summary>
        ///  Convert AppUserMessgeFollowupEntity To  AppUserMessgeFollowupDto
        /// </summary>
        public static AppUserMessgeFollowupDto ConvertEntityToDto(AppUserMessgeFollowupEntity aAppUserMessgeFollowupEntity)
        {        
    		AppUserMessgeFollowupDto aAppUserMessgeFollowupDto = new AppUserMessgeFollowupDto();
    		CopyEntityPropertyToDto( aAppUserMessgeFollowupEntity, aAppUserMessgeFollowupDto);          
			return aAppUserMessgeFollowupDto;
        }
		 /// <summary>
        ///  Convert AppUserMessgeFollowupEntity To  AppUserMessgeFollowupExDto
        /// </summary>
        public static AppUserMessgeFollowupExDto ConvertEntityToExDto(AppUserMessgeFollowupEntity aAppUserMessgeFollowupEntity)
        {        
    		AppUserMessgeFollowupExDto aAppUserMessgeFollowupExDto = new AppUserMessgeFollowupExDto();
			CopyEntityPropertyToDto( aAppUserMessgeFollowupEntity, aAppUserMessgeFollowupExDto);
			
			
			
            return aAppUserMessgeFollowupExDto;
        }
		
		 /// <summary>
        ///  Convert AppUserMessgeFollowupEntity To  AppUserMessgeFollowupDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppUserMessgeFollowupEntity aAppUserMessgeFollowupEntity,AppUserMessgeFollowupDto aAppUserMessgeFollowupDto)
        {        
    		
           // aAppUserMessgeFollowupDto.StopChangeTracking();
 			aAppUserMessgeFollowupDto.Id = aAppUserMessgeFollowupEntity.MessageFollowupId;
 			aAppUserMessgeFollowupDto.UserId = aAppUserMessgeFollowupEntity.UserId;
 			aAppUserMessgeFollowupDto.TransactionId = aAppUserMessgeFollowupEntity.TransactionId;
 			aAppUserMessgeFollowupDto.TransactionRootValueId = aAppUserMessgeFollowupEntity.TransactionRootValueId;
 			aAppUserMessgeFollowupDto.ProjectActivityId = aAppUserMessgeFollowupEntity.ProjectActivityId;
 			aAppUserMessgeFollowupDto.ProjectTeamId = aAppUserMessgeFollowupEntity.ProjectTeamId;
 			aAppUserMessgeFollowupDto.ProjectId = aAppUserMessgeFollowupEntity.ProjectId;
 			aAppUserMessgeFollowupDto.AppCreatedById = aAppUserMessgeFollowupEntity.AppCreatedById;
 			aAppUserMessgeFollowupDto.AppCreatedDate = aAppUserMessgeFollowupEntity.AppCreatedDate;
 			aAppUserMessgeFollowupDto.AppModifiedDate = aAppUserMessgeFollowupEntity.AppModifiedDate;
 			aAppUserMessgeFollowupDto.AppModifiedById = aAppUserMessgeFollowupEntity.AppModifiedById;
 			aAppUserMessgeFollowupDto.AppCreatedByCompanyId = aAppUserMessgeFollowupEntity.AppCreatedByCompanyId;
 			aAppUserMessgeFollowupDto.RoleId = aAppUserMessgeFollowupEntity.RoleId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppUserMessgeFollowupDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserMessgeFollowupEntity.AppCreatedDate);
                aAppUserMessgeFollowupDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserMessgeFollowupEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppUserMessgeFollowupEntity, aAppUserMessgeFollowupDto);
		}
		
		 /// <summary>
        ///  Copy AppUserMessgeFollowupDto Properties to   AppUserMessgeFollowupEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppUserMessgeFollowupEntity aAppUserMessgeFollowupEntity,AppUserMessgeFollowupDto aAppUserMessgeFollowupDto)
        {        
 
      			aAppUserMessgeFollowupEntity.UserId = aAppUserMessgeFollowupDto.UserId;
      			aAppUserMessgeFollowupEntity.TransactionId = aAppUserMessgeFollowupDto.TransactionId;
      			aAppUserMessgeFollowupEntity.TransactionRootValueId = aAppUserMessgeFollowupDto.TransactionRootValueId;
      			aAppUserMessgeFollowupEntity.ProjectActivityId = aAppUserMessgeFollowupDto.ProjectActivityId;
      			aAppUserMessgeFollowupEntity.ProjectTeamId = aAppUserMessgeFollowupDto.ProjectTeamId;
      			aAppUserMessgeFollowupEntity.ProjectId = aAppUserMessgeFollowupDto.ProjectId;
 
  
   
    
      			aAppUserMessgeFollowupEntity.AppCreatedByCompanyId = aAppUserMessgeFollowupDto.AppCreatedByCompanyId;
      			aAppUserMessgeFollowupEntity.RoleId = aAppUserMessgeFollowupDto.RoleId;
			
			if(aAppUserMessgeFollowupDto.Id == null)
			{
				aAppUserMessgeFollowupEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserMessgeFollowupEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppUserMessgeFollowupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserMessgeFollowupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserMessgeFollowupEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppUserMessgeFollowupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserMessgeFollowupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppUserMessgeFollowupEntity, aAppUserMessgeFollowupDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppUserMessgeFollowupEntity aAppUserMessgeFollowupEntity,AppUserMessgeFollowupDto aAppUserMessgeFollowupDto);
		
		static partial void OnCopyDtoToEntityDone(AppUserMessgeFollowupEntity aAppUserMessgeFollowupEntity,AppUserMessgeFollowupDto aAppUserMessgeFollowupDto);
		
   
       
    }
}

 