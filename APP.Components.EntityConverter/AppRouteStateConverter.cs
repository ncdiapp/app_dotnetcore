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
    /// Convert Properties between  AppRouteStateEntity and  AppRouteStateDto
    /// </summary>
    public static partial class AppRouteStateConverter 
    {
         /// <summary>
        ///  Convert AppRouteStateEntity To  AppRouteStateDto
        /// </summary>
        public static AppRouteStateDto ConvertEntityToDto(AppRouteStateEntity aAppRouteStateEntity)
        {        
    		AppRouteStateDto aAppRouteStateDto = new AppRouteStateDto();
    		CopyEntityPropertyToDto( aAppRouteStateEntity, aAppRouteStateDto);          
			return aAppRouteStateDto;
        }
		 /// <summary>
        ///  Convert AppRouteStateEntity To  AppRouteStateExDto
        /// </summary>
        public static AppRouteStateExDto ConvertEntityToExDto(AppRouteStateEntity aAppRouteStateEntity)
        {        
    		AppRouteStateExDto aAppRouteStateExDto = new AppRouteStateExDto();
			CopyEntityPropertyToDto( aAppRouteStateEntity, aAppRouteStateExDto);
			
			
			
            return aAppRouteStateExDto;
        }
		
		 /// <summary>
        ///  Convert AppRouteStateEntity To  AppRouteStateDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppRouteStateEntity aAppRouteStateEntity,AppRouteStateDto aAppRouteStateDto)
        {        
    		
           // aAppRouteStateDto.StopChangeTracking();
 			aAppRouteStateDto.Id = aAppRouteStateEntity.RouteStateId;
 			aAppRouteStateDto.StateCode = aAppRouteStateEntity.StateCode;
 			aAppRouteStateDto.PageRelativeUrl = aAppRouteStateEntity.PageRelativeUrl;
 			aAppRouteStateDto.ControllerName = aAppRouteStateEntity.ControllerName;
 			aAppRouteStateDto.TemplateUrl = aAppRouteStateEntity.TemplateUrl;
 			aAppRouteStateDto.NoSecurityControl = aAppRouteStateEntity.NoSecurityControl;
 			aAppRouteStateDto.AppCreatedById = aAppRouteStateEntity.AppCreatedById;
 			aAppRouteStateDto.AppCreatedDate = aAppRouteStateEntity.AppCreatedDate;
 			aAppRouteStateDto.AppModifiedDate = aAppRouteStateEntity.AppModifiedDate;
 			aAppRouteStateDto.AppModifiedById = aAppRouteStateEntity.AppModifiedById;
 			aAppRouteStateDto.AppCreatedByCompanyId = aAppRouteStateEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppRouteStateDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppRouteStateEntity.AppCreatedDate);
                aAppRouteStateDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppRouteStateEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppRouteStateEntity, aAppRouteStateDto);
		}
		
		 /// <summary>
        ///  Copy AppRouteStateDto Properties to   AppRouteStateEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppRouteStateEntity aAppRouteStateEntity,AppRouteStateDto aAppRouteStateDto)
        {        
 
      			aAppRouteStateEntity.StateCode = aAppRouteStateDto.StateCode;
      			aAppRouteStateEntity.PageRelativeUrl = aAppRouteStateDto.PageRelativeUrl;
      			aAppRouteStateEntity.ControllerName = aAppRouteStateDto.ControllerName;
      			aAppRouteStateEntity.TemplateUrl = aAppRouteStateDto.TemplateUrl;
      			aAppRouteStateEntity.NoSecurityControl = aAppRouteStateDto.NoSecurityControl;
 
  
   
    
      			aAppRouteStateEntity.AppCreatedByCompanyId = aAppRouteStateDto.AppCreatedByCompanyId;
			
			if(aAppRouteStateDto.Id == null)
			{
				aAppRouteStateEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppRouteStateEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppRouteStateEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppRouteStateEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppRouteStateEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppRouteStateEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppRouteStateEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppRouteStateEntity, aAppRouteStateDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppRouteStateEntity aAppRouteStateEntity,AppRouteStateDto aAppRouteStateDto);
		
		static partial void OnCopyDtoToEntityDone(AppRouteStateEntity aAppRouteStateEntity,AppRouteStateDto aAppRouteStateDto);
		
   
       
    }
}

 