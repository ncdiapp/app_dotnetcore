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
    /// Convert Properties between  AppBusienssAssormentNavigationEntity and  AppBusienssAssormentNavigationDto
    /// </summary>
    public static partial class AppBusienssAssormentNavigationConverter 
    {
         /// <summary>
        ///  Convert AppBusienssAssormentNavigationEntity To  AppBusienssAssormentNavigationDto
        /// </summary>
        public static AppBusienssAssormentNavigationDto ConvertEntityToDto(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity)
        {        
    		AppBusienssAssormentNavigationDto aAppBusienssAssormentNavigationDto = new AppBusienssAssormentNavigationDto();
    		CopyEntityPropertyToDto( aAppBusienssAssormentNavigationEntity, aAppBusienssAssormentNavigationDto);          
			return aAppBusienssAssormentNavigationDto;
        }
		 /// <summary>
        ///  Convert AppBusienssAssormentNavigationEntity To  AppBusienssAssormentNavigationExDto
        /// </summary>
        public static AppBusienssAssormentNavigationExDto ConvertEntityToExDto(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity)
        {        
    		AppBusienssAssormentNavigationExDto aAppBusienssAssormentNavigationExDto = new AppBusienssAssormentNavigationExDto();
			CopyEntityPropertyToDto( aAppBusienssAssormentNavigationEntity, aAppBusienssAssormentNavigationExDto);
			
			
			
            return aAppBusienssAssormentNavigationExDto;
        }
		
		 /// <summary>
        ///  Convert AppBusienssAssormentNavigationEntity To  AppBusienssAssormentNavigationDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity,AppBusienssAssormentNavigationDto aAppBusienssAssormentNavigationDto)
        {        
    		
           // aAppBusienssAssormentNavigationDto.StopChangeTracking();
 			aAppBusienssAssormentNavigationDto.Id = aAppBusienssAssormentNavigationEntity.AssotmentnavigationId;
 			aAppBusienssAssormentNavigationDto.AssormentName = aAppBusienssAssormentNavigationEntity.AssormentName;
 			aAppBusienssAssormentNavigationDto.Description = aAppBusienssAssormentNavigationEntity.Description;
 			aAppBusienssAssormentNavigationDto.MainTraisactionId = aAppBusienssAssormentNavigationEntity.MainTraisactionId;
 			aAppBusienssAssormentNavigationDto.MainProjectTemplateId = aAppBusienssAssormentNavigationEntity.MainProjectTemplateId;
 			aAppBusienssAssormentNavigationDto.AppCreatedById = aAppBusienssAssormentNavigationEntity.AppCreatedById;
 			aAppBusienssAssormentNavigationDto.AppCreatedDate = aAppBusienssAssormentNavigationEntity.AppCreatedDate;
 			aAppBusienssAssormentNavigationDto.AppModifiedDate = aAppBusienssAssormentNavigationEntity.AppModifiedDate;
 			aAppBusienssAssormentNavigationDto.AppModifiedById = aAppBusienssAssormentNavigationEntity.AppModifiedById;
 			aAppBusienssAssormentNavigationDto.AppCreatedByCompanyId = aAppBusienssAssormentNavigationEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppBusienssAssormentNavigationDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusienssAssormentNavigationEntity.AppCreatedDate);
                aAppBusienssAssormentNavigationDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusienssAssormentNavigationEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppBusienssAssormentNavigationEntity, aAppBusienssAssormentNavigationDto);
		}
		
		 /// <summary>
        ///  Copy AppBusienssAssormentNavigationDto Properties to   AppBusienssAssormentNavigationEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity,AppBusienssAssormentNavigationDto aAppBusienssAssormentNavigationDto)
        {        
 
      			aAppBusienssAssormentNavigationEntity.AssormentName = aAppBusienssAssormentNavigationDto.AssormentName;
      			aAppBusienssAssormentNavigationEntity.Description = aAppBusienssAssormentNavigationDto.Description;
      			aAppBusienssAssormentNavigationEntity.MainTraisactionId = aAppBusienssAssormentNavigationDto.MainTraisactionId;
      			aAppBusienssAssormentNavigationEntity.MainProjectTemplateId = aAppBusienssAssormentNavigationDto.MainProjectTemplateId;
 
  
   
    
      			aAppBusienssAssormentNavigationEntity.AppCreatedByCompanyId = aAppBusienssAssormentNavigationDto.AppCreatedByCompanyId;
			
			if(aAppBusienssAssormentNavigationDto.Id == null)
			{
				aAppBusienssAssormentNavigationEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusienssAssormentNavigationEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppBusienssAssormentNavigationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusienssAssormentNavigationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusienssAssormentNavigationEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppBusienssAssormentNavigationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusienssAssormentNavigationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppBusienssAssormentNavigationEntity, aAppBusienssAssormentNavigationDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity,AppBusienssAssormentNavigationDto aAppBusienssAssormentNavigationDto);
		
		static partial void OnCopyDtoToEntityDone(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity,AppBusienssAssormentNavigationDto aAppBusienssAssormentNavigationDto);
		
   
       
    }
}

 