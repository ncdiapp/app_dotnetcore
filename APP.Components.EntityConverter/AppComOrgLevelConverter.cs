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
    /// Convert Properties between  AppComOrgLevelEntity and  AppComOrgLevelDto
    /// </summary>
    public static partial class AppComOrgLevelConverter 
    {
         /// <summary>
        ///  Convert AppComOrgLevelEntity To  AppComOrgLevelDto
        /// </summary>
        public static AppComOrgLevelDto ConvertEntityToDto(AppComOrgLevelEntity aAppComOrgLevelEntity)
        {        
    		AppComOrgLevelDto aAppComOrgLevelDto = new AppComOrgLevelDto();
    		CopyEntityPropertyToDto( aAppComOrgLevelEntity, aAppComOrgLevelDto);          
			return aAppComOrgLevelDto;
        }
		 /// <summary>
        ///  Convert AppComOrgLevelEntity To  AppComOrgLevelExDto
        /// </summary>
        public static AppComOrgLevelExDto ConvertEntityToExDto(AppComOrgLevelEntity aAppComOrgLevelEntity)
        {        
    		AppComOrgLevelExDto aAppComOrgLevelExDto = new AppComOrgLevelExDto();
			CopyEntityPropertyToDto( aAppComOrgLevelEntity, aAppComOrgLevelExDto);
			
			
			
            return aAppComOrgLevelExDto;
        }
		
		 /// <summary>
        ///  Convert AppComOrgLevelEntity To  AppComOrgLevelDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppComOrgLevelEntity aAppComOrgLevelEntity,AppComOrgLevelDto aAppComOrgLevelDto)
        {        
    		
           // aAppComOrgLevelDto.StopChangeTracking();
 			aAppComOrgLevelDto.Id = aAppComOrgLevelEntity.OrgLevelId;
 			aAppComOrgLevelDto.AppCompanyId = aAppComOrgLevelEntity.AppCompanyId;
 			aAppComOrgLevelDto.ClassificationLevel = aAppComOrgLevelEntity.ClassificationLevel;
 			aAppComOrgLevelDto.CodeNum = aAppComOrgLevelEntity.CodeNum;
 			aAppComOrgLevelDto.LevelName = aAppComOrgLevelEntity.LevelName;
 			aAppComOrgLevelDto.FullName = aAppComOrgLevelEntity.FullName;
 			aAppComOrgLevelDto.AppCreatedById = aAppComOrgLevelEntity.AppCreatedById;
 			aAppComOrgLevelDto.AppCreatedDate = aAppComOrgLevelEntity.AppCreatedDate;
 			aAppComOrgLevelDto.AppModifiedDate = aAppComOrgLevelEntity.AppModifiedDate;
 			aAppComOrgLevelDto.AppModifiedById = aAppComOrgLevelEntity.AppModifiedById;
 			aAppComOrgLevelDto.AppCreatedByCompanyId = aAppComOrgLevelEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppComOrgLevelDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppComOrgLevelEntity.AppCreatedDate);
                aAppComOrgLevelDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppComOrgLevelEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppComOrgLevelEntity, aAppComOrgLevelDto);
		}
		
		 /// <summary>
        ///  Copy AppComOrgLevelDto Properties to   AppComOrgLevelEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppComOrgLevelEntity aAppComOrgLevelEntity,AppComOrgLevelDto aAppComOrgLevelDto)
        {        
 
      			aAppComOrgLevelEntity.AppCompanyId = aAppComOrgLevelDto.AppCompanyId;
      			aAppComOrgLevelEntity.ClassificationLevel = aAppComOrgLevelDto.ClassificationLevel;
      			aAppComOrgLevelEntity.CodeNum = aAppComOrgLevelDto.CodeNum;
      			aAppComOrgLevelEntity.LevelName = aAppComOrgLevelDto.LevelName;
      			aAppComOrgLevelEntity.FullName = aAppComOrgLevelDto.FullName;
 
  
   
    
      			aAppComOrgLevelEntity.AppCreatedByCompanyId = aAppComOrgLevelDto.AppCreatedByCompanyId;
			
			if(aAppComOrgLevelDto.Id == null)
			{
				aAppComOrgLevelEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppComOrgLevelEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppComOrgLevelEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppComOrgLevelEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppComOrgLevelEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppComOrgLevelEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppComOrgLevelEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppComOrgLevelEntity, aAppComOrgLevelDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppComOrgLevelEntity aAppComOrgLevelEntity,AppComOrgLevelDto aAppComOrgLevelDto);
		
		static partial void OnCopyDtoToEntityDone(AppComOrgLevelEntity aAppComOrgLevelEntity,AppComOrgLevelDto aAppComOrgLevelDto);
		
   
       
    }
}

 