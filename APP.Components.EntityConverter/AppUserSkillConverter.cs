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
    /// Convert Properties between  AppUserSkillEntity and  AppUserSkillDto
    /// </summary>
    public static partial class AppUserSkillConverter 
    {
         /// <summary>
        ///  Convert AppUserSkillEntity To  AppUserSkillDto
        /// </summary>
        public static AppUserSkillDto ConvertEntityToDto(AppUserSkillEntity aAppUserSkillEntity)
        {        
    		AppUserSkillDto aAppUserSkillDto = new AppUserSkillDto();
    		CopyEntityPropertyToDto( aAppUserSkillEntity, aAppUserSkillDto);          
			return aAppUserSkillDto;
        }
		 /// <summary>
        ///  Convert AppUserSkillEntity To  AppUserSkillExDto
        /// </summary>
        public static AppUserSkillExDto ConvertEntityToExDto(AppUserSkillEntity aAppUserSkillEntity)
        {        
    		AppUserSkillExDto aAppUserSkillExDto = new AppUserSkillExDto();
			CopyEntityPropertyToDto( aAppUserSkillEntity, aAppUserSkillExDto);
			
			
			
            return aAppUserSkillExDto;
        }
		
		 /// <summary>
        ///  Convert AppUserSkillEntity To  AppUserSkillDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppUserSkillEntity aAppUserSkillEntity,AppUserSkillDto aAppUserSkillDto)
        {        
    		
           // aAppUserSkillDto.StopChangeTracking();
 			aAppUserSkillDto.Id = aAppUserSkillEntity.UserSkillId;
 			aAppUserSkillDto.SkillItemId = aAppUserSkillEntity.SkillItemId;
 			aAppUserSkillDto.UserId = aAppUserSkillEntity.UserId;
 			aAppUserSkillDto.AppCreatedById = aAppUserSkillEntity.AppCreatedById;
 			aAppUserSkillDto.AppCreatedDate = aAppUserSkillEntity.AppCreatedDate;
 			aAppUserSkillDto.AppModifiedDate = aAppUserSkillEntity.AppModifiedDate;
 			aAppUserSkillDto.AppModifiedById = aAppUserSkillEntity.AppModifiedById;
 			aAppUserSkillDto.AppCreatedByCompanyId = aAppUserSkillEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppUserSkillDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserSkillEntity.AppCreatedDate);
                aAppUserSkillDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserSkillEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppUserSkillEntity, aAppUserSkillDto);
		}
		
		 /// <summary>
        ///  Copy AppUserSkillDto Properties to   AppUserSkillEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppUserSkillEntity aAppUserSkillEntity,AppUserSkillDto aAppUserSkillDto)
        {        
 
      			aAppUserSkillEntity.SkillItemId = aAppUserSkillDto.SkillItemId;
      			aAppUserSkillEntity.UserId = aAppUserSkillDto.UserId;
 
  
   
    
      			aAppUserSkillEntity.AppCreatedByCompanyId = aAppUserSkillDto.AppCreatedByCompanyId;
			
			if(aAppUserSkillDto.Id == null)
			{
				aAppUserSkillEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserSkillEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppUserSkillEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserSkillEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserSkillEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppUserSkillEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserSkillEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppUserSkillEntity, aAppUserSkillDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppUserSkillEntity aAppUserSkillEntity,AppUserSkillDto aAppUserSkillDto);
		
		static partial void OnCopyDtoToEntityDone(AppUserSkillEntity aAppUserSkillEntity,AppUserSkillDto aAppUserSkillDto);
		
   
       
    }
}

 