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
    /// Convert Properties between  AppEsitePagesEntity and  AppEsitePagesDto
    /// </summary>
    public static partial class AppEsitePagesConverter 
    {
         /// <summary>
        ///  Convert AppEsitePagesEntity To  AppEsitePagesDto
        /// </summary>
        public static AppEsitePagesDto ConvertEntityToDto(AppEsitePagesEntity aAppEsitePagesEntity)
        {        
    		AppEsitePagesDto aAppEsitePagesDto = new AppEsitePagesDto();
    		CopyEntityPropertyToDto( aAppEsitePagesEntity, aAppEsitePagesDto);          
			return aAppEsitePagesDto;
        }
		 /// <summary>
        ///  Convert AppEsitePagesEntity To  AppEsitePagesExDto
        /// </summary>
        public static AppEsitePagesExDto ConvertEntityToExDto(AppEsitePagesEntity aAppEsitePagesEntity)
        {        
    		AppEsitePagesExDto aAppEsitePagesExDto = new AppEsitePagesExDto();
			CopyEntityPropertyToDto( aAppEsitePagesEntity, aAppEsitePagesExDto);
			
			
			
            return aAppEsitePagesExDto;
        }
		
		 /// <summary>
        ///  Convert AppEsitePagesEntity To  AppEsitePagesDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEsitePagesEntity aAppEsitePagesEntity,AppEsitePagesDto aAppEsitePagesDto)
        {        
    		
           // aAppEsitePagesDto.StopChangeTracking();
 			aAppEsitePagesDto.Id = aAppEsitePagesEntity.PageId;
 			aAppEsitePagesDto.EsiteId = aAppEsitePagesEntity.EsiteId;
 			aAppEsitePagesDto.Title = aAppEsitePagesEntity.Title;
 			aAppEsitePagesDto.EmresourceContentType = aAppEsitePagesEntity.EmresourceContentType;
 			aAppEsitePagesDto.HtmlContent = aAppEsitePagesEntity.HtmlContent;
 			aAppEsitePagesDto.LoadOrder = aAppEsitePagesEntity.LoadOrder;
 			aAppEsitePagesDto.IsActive = aAppEsitePagesEntity.IsActive;
 			aAppEsitePagesDto.MetaDesciption = aAppEsitePagesEntity.MetaDesciption;
 			aAppEsitePagesDto.UrlAndHandle = aAppEsitePagesEntity.UrlAndHandle;
 			aAppEsitePagesDto.TransactionId = aAppEsitePagesEntity.TransactionId;
 			aAppEsitePagesDto.IsDefault = aAppEsitePagesEntity.IsDefault;
 			aAppEsitePagesDto.AppCreatedById = aAppEsitePagesEntity.AppCreatedById;
 			aAppEsitePagesDto.AppCreatedDate = aAppEsitePagesEntity.AppCreatedDate;
 			aAppEsitePagesDto.AppModifiedDate = aAppEsitePagesEntity.AppModifiedDate;
 			aAppEsitePagesDto.AppModifiedById = aAppEsitePagesEntity.AppModifiedById;
 			aAppEsitePagesDto.AppCreatedByCompanyId = aAppEsitePagesEntity.AppCreatedByCompanyId;
 			aAppEsitePagesDto.ControllerName = aAppEsitePagesEntity.ControllerName;
 			aAppEsitePagesDto.SearchId = aAppEsitePagesEntity.SearchId;
 			aAppEsitePagesDto.SearchViewId = aAppEsitePagesEntity.SearchViewId;
 			aAppEsitePagesDto.IsMasterLayoutPage = aAppEsitePagesEntity.IsMasterLayoutPage;
 			aAppEsitePagesDto.PageJsMethod = aAppEsitePagesEntity.PageJsMethod;
 			aAppEsitePagesDto.PageCssStyle = aAppEsitePagesEntity.PageCssStyle;
 			aAppEsitePagesDto.NavigationCtrlJavascript = aAppEsitePagesEntity.NavigationCtrlJavascript;
 			aAppEsitePagesDto.FileFullPath = aAppEsitePagesEntity.FileFullPath;
 			aAppEsitePagesDto.DesignLayout = aAppEsitePagesEntity.DesignLayout;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEsitePagesDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEsitePagesEntity.AppCreatedDate);
                aAppEsitePagesDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEsitePagesEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEsitePagesEntity, aAppEsitePagesDto);
		}
		
		 /// <summary>
        ///  Copy AppEsitePagesDto Properties to   AppEsitePagesEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEsitePagesEntity aAppEsitePagesEntity,AppEsitePagesDto aAppEsitePagesDto)
        {        
 
      			aAppEsitePagesEntity.EsiteId = aAppEsitePagesDto.EsiteId;
      			aAppEsitePagesEntity.Title = aAppEsitePagesDto.Title;
      			aAppEsitePagesEntity.EmresourceContentType = aAppEsitePagesDto.EmresourceContentType;
      			aAppEsitePagesEntity.HtmlContent = aAppEsitePagesDto.HtmlContent;
      			aAppEsitePagesEntity.LoadOrder = aAppEsitePagesDto.LoadOrder;
      			aAppEsitePagesEntity.IsActive = aAppEsitePagesDto.IsActive;
      			aAppEsitePagesEntity.MetaDesciption = aAppEsitePagesDto.MetaDesciption;
      			aAppEsitePagesEntity.UrlAndHandle = aAppEsitePagesDto.UrlAndHandle;
      			aAppEsitePagesEntity.TransactionId = aAppEsitePagesDto.TransactionId;
      			aAppEsitePagesEntity.IsDefault = aAppEsitePagesDto.IsDefault;
 
  
   
    
      			aAppEsitePagesEntity.AppCreatedByCompanyId = aAppEsitePagesDto.AppCreatedByCompanyId;
      			aAppEsitePagesEntity.ControllerName = aAppEsitePagesDto.ControllerName;
      			aAppEsitePagesEntity.SearchId = aAppEsitePagesDto.SearchId;
      			aAppEsitePagesEntity.SearchViewId = aAppEsitePagesDto.SearchViewId;
      			aAppEsitePagesEntity.IsMasterLayoutPage = aAppEsitePagesDto.IsMasterLayoutPage;
      			aAppEsitePagesEntity.PageJsMethod = aAppEsitePagesDto.PageJsMethod;
      			aAppEsitePagesEntity.PageCssStyle = aAppEsitePagesDto.PageCssStyle;
      			aAppEsitePagesEntity.NavigationCtrlJavascript = aAppEsitePagesDto.NavigationCtrlJavascript;
      			aAppEsitePagesEntity.FileFullPath = aAppEsitePagesDto.FileFullPath;
      			aAppEsitePagesEntity.DesignLayout = aAppEsitePagesDto.DesignLayout;
			
			if(aAppEsitePagesDto.Id == null)
			{
				aAppEsitePagesEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEsitePagesEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEsitePagesEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEsitePagesEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEsitePagesEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEsitePagesEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEsitePagesEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEsitePagesEntity, aAppEsitePagesDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEsitePagesEntity aAppEsitePagesEntity,AppEsitePagesDto aAppEsitePagesDto);
		
		static partial void OnCopyDtoToEntityDone(AppEsitePagesEntity aAppEsitePagesEntity,AppEsitePagesDto aAppEsitePagesDto);
		
   
       
    }
}

 