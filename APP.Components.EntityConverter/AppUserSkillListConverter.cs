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
    /// Convert Properties between  AppUserSkillListEntity and  AppUserSkillListDto
    /// </summary>
    public static partial class AppUserSkillListConverter 
    {
         /// <summary>
        ///  Convert AppUserSkillListEntity To  AppUserSkillListDto
        /// </summary>
        public static AppUserSkillListDto ConvertEntityToDto(AppUserSkillListEntity aAppUserSkillListEntity)
        {        
    		AppUserSkillListDto aAppUserSkillListDto = new AppUserSkillListDto();
    		CopyEntityPropertyToDto( aAppUserSkillListEntity, aAppUserSkillListDto);          
			return aAppUserSkillListDto;
        }
		 /// <summary>
        ///  Convert AppUserSkillListEntity To  AppUserSkillListExDto
        /// </summary>
        public static AppUserSkillListExDto ConvertEntityToExDto(AppUserSkillListEntity aAppUserSkillListEntity)
        {        
    		AppUserSkillListExDto aAppUserSkillListExDto = new AppUserSkillListExDto();
			CopyEntityPropertyToDto( aAppUserSkillListEntity, aAppUserSkillListExDto);
			
			
			
            return aAppUserSkillListExDto;
        }
		
		 /// <summary>
        ///  Convert AppUserSkillListEntity To  AppUserSkillListDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppUserSkillListEntity aAppUserSkillListEntity,AppUserSkillListDto aAppUserSkillListDto)
        {        
    		
           // aAppUserSkillListDto.StopChangeTracking();
 			aAppUserSkillListDto.Id = aAppUserSkillListEntity.SkillItemId;
 			aAppUserSkillListDto.SkillName = aAppUserSkillListEntity.SkillName;
 			aAppUserSkillListDto.Description = aAppUserSkillListEntity.Description;
 			aAppUserSkillListDto.SkillType = aAppUserSkillListEntity.SkillType;
 			aAppUserSkillListDto.SkillLevel = aAppUserSkillListEntity.SkillLevel;
 			aAppUserSkillListDto.AppCreatedById = aAppUserSkillListEntity.AppCreatedById;
 			aAppUserSkillListDto.AppCreatedDate = aAppUserSkillListEntity.AppCreatedDate;
 			aAppUserSkillListDto.AppModifiedDate = aAppUserSkillListEntity.AppModifiedDate;
 			aAppUserSkillListDto.AppModifiedById = aAppUserSkillListEntity.AppModifiedById;
 			aAppUserSkillListDto.AppCreatedByCompanyId = aAppUserSkillListEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppUserSkillListDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserSkillListEntity.AppCreatedDate);
                aAppUserSkillListDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppUserSkillListEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppUserSkillListEntity, aAppUserSkillListDto);
		}
		
		 /// <summary>
        ///  Copy AppUserSkillListDto Properties to   AppUserSkillListEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppUserSkillListEntity aAppUserSkillListEntity,AppUserSkillListDto aAppUserSkillListDto)
        {        
 
      			aAppUserSkillListEntity.SkillName = aAppUserSkillListDto.SkillName;
      			aAppUserSkillListEntity.Description = aAppUserSkillListDto.Description;
      			aAppUserSkillListEntity.SkillType = aAppUserSkillListDto.SkillType;
      			aAppUserSkillListEntity.SkillLevel = aAppUserSkillListDto.SkillLevel;
 
  
   
    
      			aAppUserSkillListEntity.AppCreatedByCompanyId = aAppUserSkillListDto.AppCreatedByCompanyId;
			
			if(aAppUserSkillListDto.Id == null)
			{
				aAppUserSkillListEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserSkillListEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppUserSkillListEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserSkillListEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppUserSkillListEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppUserSkillListEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppUserSkillListEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppUserSkillListEntity, aAppUserSkillListDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppUserSkillListEntity aAppUserSkillListEntity,AppUserSkillListDto aAppUserSkillListDto);
		
		static partial void OnCopyDtoToEntityDone(AppUserSkillListEntity aAppUserSkillListEntity,AppUserSkillListDto aAppUserSkillListDto);
		
   
       
    }
}

 