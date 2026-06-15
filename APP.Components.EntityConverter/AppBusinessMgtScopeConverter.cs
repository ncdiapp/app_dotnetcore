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
    /// Convert Properties between  AppBusinessMgtScopeEntity and  AppBusinessMgtScopeDto
    /// </summary>
    public static partial class AppBusinessMgtScopeConverter 
    {
         /// <summary>
        ///  Convert AppBusinessMgtScopeEntity To  AppBusinessMgtScopeDto
        /// </summary>
        public static AppBusinessMgtScopeDto ConvertEntityToDto(AppBusinessMgtScopeEntity aAppBusinessMgtScopeEntity)
        {        
    		AppBusinessMgtScopeDto aAppBusinessMgtScopeDto = new AppBusinessMgtScopeDto();
    		CopyEntityPropertyToDto( aAppBusinessMgtScopeEntity, aAppBusinessMgtScopeDto);          
			return aAppBusinessMgtScopeDto;
        }
		 /// <summary>
        ///  Convert AppBusinessMgtScopeEntity To  AppBusinessMgtScopeExDto
        /// </summary>
        public static AppBusinessMgtScopeExDto ConvertEntityToExDto(AppBusinessMgtScopeEntity aAppBusinessMgtScopeEntity)
        {        
    		AppBusinessMgtScopeExDto aAppBusinessMgtScopeExDto = new AppBusinessMgtScopeExDto();
			CopyEntityPropertyToDto( aAppBusinessMgtScopeEntity, aAppBusinessMgtScopeExDto);
			
			
			
            return aAppBusinessMgtScopeExDto;
        }
		
		 /// <summary>
        ///  Convert AppBusinessMgtScopeEntity To  AppBusinessMgtScopeDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppBusinessMgtScopeEntity aAppBusinessMgtScopeEntity,AppBusinessMgtScopeDto aAppBusinessMgtScopeDto)
        {        
    		
           // aAppBusinessMgtScopeDto.StopChangeTracking();
 			aAppBusinessMgtScopeDto.Id = aAppBusinessMgtScopeEntity.BusinessScopeId;
 			aAppBusinessMgtScopeDto.ScopeName = aAppBusinessMgtScopeEntity.ScopeName;
 			aAppBusinessMgtScopeDto.Description = aAppBusinessMgtScopeEntity.Description;
 			aAppBusinessMgtScopeDto.Sort = aAppBusinessMgtScopeEntity.Sort;
 			aAppBusinessMgtScopeDto.AppCreatedById = aAppBusinessMgtScopeEntity.AppCreatedById;
 			aAppBusinessMgtScopeDto.AppCreatedDate = aAppBusinessMgtScopeEntity.AppCreatedDate;
 			aAppBusinessMgtScopeDto.AppModifiedDate = aAppBusinessMgtScopeEntity.AppModifiedDate;
 			aAppBusinessMgtScopeDto.AppModifiedById = aAppBusinessMgtScopeEntity.AppModifiedById;
 			aAppBusinessMgtScopeDto.AppCreatedByCompanyId = aAppBusinessMgtScopeEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppBusinessMgtScopeDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessMgtScopeEntity.AppCreatedDate);
                aAppBusinessMgtScopeDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessMgtScopeEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppBusinessMgtScopeEntity, aAppBusinessMgtScopeDto);
		}
		
		 /// <summary>
        ///  Copy AppBusinessMgtScopeDto Properties to   AppBusinessMgtScopeEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppBusinessMgtScopeEntity aAppBusinessMgtScopeEntity,AppBusinessMgtScopeDto aAppBusinessMgtScopeDto)
        {        
 
      			aAppBusinessMgtScopeEntity.ScopeName = aAppBusinessMgtScopeDto.ScopeName;
      			aAppBusinessMgtScopeEntity.Description = aAppBusinessMgtScopeDto.Description;
      			aAppBusinessMgtScopeEntity.Sort = aAppBusinessMgtScopeDto.Sort;
 
  
   
    
      			aAppBusinessMgtScopeEntity.AppCreatedByCompanyId = aAppBusinessMgtScopeDto.AppCreatedByCompanyId;
			
			if(aAppBusinessMgtScopeDto.Id == null)
			{
				aAppBusinessMgtScopeEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessMgtScopeEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppBusinessMgtScopeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessMgtScopeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessMgtScopeEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppBusinessMgtScopeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessMgtScopeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppBusinessMgtScopeEntity, aAppBusinessMgtScopeDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppBusinessMgtScopeEntity aAppBusinessMgtScopeEntity,AppBusinessMgtScopeDto aAppBusinessMgtScopeDto);
		
		static partial void OnCopyDtoToEntityDone(AppBusinessMgtScopeEntity aAppBusinessMgtScopeEntity,AppBusinessMgtScopeDto aAppBusinessMgtScopeDto);
		
   
       
    }
}

 