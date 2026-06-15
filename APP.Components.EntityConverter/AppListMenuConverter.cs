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
    /// Convert Properties between  AppListMenuEntity and  AppListMenuDto
    /// </summary>
    public static partial class AppListMenuConverter 
    {
         /// <summary>
        ///  Convert AppListMenuEntity To  AppListMenuDto
        /// </summary>
        public static AppListMenuDto ConvertEntityToDto(AppListMenuEntity aAppListMenuEntity)
        {        
    		AppListMenuDto aAppListMenuDto = new AppListMenuDto();
    		CopyEntityPropertyToDto( aAppListMenuEntity, aAppListMenuDto);          
			return aAppListMenuDto;
        }
		 /// <summary>
        ///  Convert AppListMenuEntity To  AppListMenuExDto
        /// </summary>
        public static AppListMenuExDto ConvertEntityToExDto(AppListMenuEntity aAppListMenuEntity)
        {        
    		AppListMenuExDto aAppListMenuExDto = new AppListMenuExDto();
			CopyEntityPropertyToDto( aAppListMenuEntity, aAppListMenuExDto);
			
			
			
            return aAppListMenuExDto;
        }
		
		 /// <summary>
        ///  Convert AppListMenuEntity To  AppListMenuDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppListMenuEntity aAppListMenuEntity,AppListMenuDto aAppListMenuDto)
        {        
    		
           // aAppListMenuDto.StopChangeTracking();
 			aAppListMenuDto.Id = aAppListMenuEntity.MenuId;
 			aAppListMenuDto.ParentId = aAppListMenuEntity.ParentId;
 			aAppListMenuDto.Name = aAppListMenuEntity.Name;
 			aAppListMenuDto.Description = aAppListMenuEntity.Description;
 			aAppListMenuDto.IconName = aAppListMenuEntity.IconName;
 			aAppListMenuDto.RouteCode = aAppListMenuEntity.RouteCode;
 			aAppListMenuDto.Link = aAppListMenuEntity.Link;
 			aAppListMenuDto.Sort = aAppListMenuEntity.Sort;
 			aAppListMenuDto.LinkType = aAppListMenuEntity.LinkType;
 			aAppListMenuDto.IsSharedbyMutipleCompany = aAppListMenuEntity.IsSharedbyMutipleCompany;
 			aAppListMenuDto.EmDeviceMenuShowMode = aAppListMenuEntity.EmDeviceMenuShowMode;
 			aAppListMenuDto.AppCreatedById = aAppListMenuEntity.AppCreatedById;
 			aAppListMenuDto.AppCreatedDate = aAppListMenuEntity.AppCreatedDate;
 			aAppListMenuDto.AppModifiedDate = aAppListMenuEntity.AppModifiedDate;
 			aAppListMenuDto.AppModifiedById = aAppListMenuEntity.AppModifiedById;
 			aAppListMenuDto.AppCreatedByCompanyId = aAppListMenuEntity.AppCreatedByCompanyId;
 			aAppListMenuDto.GlobalGuid = aAppListMenuEntity.GlobalGuid;
 			aAppListMenuDto.ModuleRegisterId = aAppListMenuEntity.ModuleRegisterId;
 			aAppListMenuDto.DisplayModeMenuOrTab = aAppListMenuEntity.DisplayModeMenuOrTab;
 			aAppListMenuDto.IconName2 = aAppListMenuEntity.IconName2;
 			aAppListMenuDto.EsiteId = aAppListMenuEntity.EsiteId;
 			aAppListMenuDto.EmAppMenuItemCategory = aAppListMenuEntity.EmAppMenuItemCategory;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppListMenuDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppListMenuEntity.AppCreatedDate);
                aAppListMenuDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppListMenuEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppListMenuEntity, aAppListMenuDto);
		}
		
		 /// <summary>
        ///  Copy AppListMenuDto Properties to   AppListMenuEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppListMenuEntity aAppListMenuEntity,AppListMenuDto aAppListMenuDto)
        {        
 
      			aAppListMenuEntity.ParentId = aAppListMenuDto.ParentId;
      			aAppListMenuEntity.Name = aAppListMenuDto.Name;
      			aAppListMenuEntity.Description = aAppListMenuDto.Description;
      			aAppListMenuEntity.IconName = aAppListMenuDto.IconName;
      			aAppListMenuEntity.RouteCode = aAppListMenuDto.RouteCode;
      			aAppListMenuEntity.Link = aAppListMenuDto.Link;
      			aAppListMenuEntity.Sort = aAppListMenuDto.Sort;
      			aAppListMenuEntity.LinkType = aAppListMenuDto.LinkType;
      			aAppListMenuEntity.IsSharedbyMutipleCompany = aAppListMenuDto.IsSharedbyMutipleCompany;
      			aAppListMenuEntity.EmDeviceMenuShowMode = aAppListMenuDto.EmDeviceMenuShowMode;
 
  
   
    
      			aAppListMenuEntity.AppCreatedByCompanyId = aAppListMenuDto.AppCreatedByCompanyId;
      			aAppListMenuEntity.GlobalGuid = aAppListMenuDto.GlobalGuid;
      			aAppListMenuEntity.ModuleRegisterId = aAppListMenuDto.ModuleRegisterId;
      			aAppListMenuEntity.DisplayModeMenuOrTab = aAppListMenuDto.DisplayModeMenuOrTab;
      			aAppListMenuEntity.IconName2 = aAppListMenuDto.IconName2;
      			aAppListMenuEntity.EsiteId = aAppListMenuDto.EsiteId;
      			aAppListMenuEntity.EmAppMenuItemCategory = aAppListMenuDto.EmAppMenuItemCategory;
			
			if(aAppListMenuDto.Id == null)
			{
				aAppListMenuEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppListMenuEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppListMenuEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppListMenuEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppListMenuEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppListMenuEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppListMenuEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppListMenuEntity, aAppListMenuDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppListMenuEntity aAppListMenuEntity,AppListMenuDto aAppListMenuDto);
		
		static partial void OnCopyDtoToEntityDone(AppListMenuEntity aAppListMenuEntity,AppListMenuDto aAppListMenuDto);
		
   
       
    }
}

 