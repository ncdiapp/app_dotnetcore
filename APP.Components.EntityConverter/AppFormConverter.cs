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
    /// Convert Properties between  AppFormEntity and  AppFormDto
    /// </summary>
    public static partial class AppFormConverter 
    {
         /// <summary>
        ///  Convert AppFormEntity To  AppFormDto
        /// </summary>
        public static AppFormDto ConvertEntityToDto(AppFormEntity aAppFormEntity)
        {        
    		AppFormDto aAppFormDto = new AppFormDto();
    		CopyEntityPropertyToDto( aAppFormEntity, aAppFormDto);          
			return aAppFormDto;
        }
		 /// <summary>
        ///  Convert AppFormEntity To  AppFormExDto
        /// </summary>
        public static AppFormExDto ConvertEntityToExDto(AppFormEntity aAppFormEntity)
        {        
    		AppFormExDto aAppFormExDto = new AppFormExDto();
			CopyEntityPropertyToDto( aAppFormEntity, aAppFormExDto);
			
			
			
            return aAppFormExDto;
        }
		
		 /// <summary>
        ///  Convert AppFormEntity To  AppFormDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFormEntity aAppFormEntity,AppFormDto aAppFormDto)
        {        
    		
           // aAppFormDto.StopChangeTracking();
 			aAppFormDto.Id = aAppFormEntity.FormId;
 			aAppFormDto.Name = aAppFormEntity.Name;
 			aAppFormDto.Description = aAppFormEntity.Description;
 			aAppFormDto.LayoutType = aAppFormEntity.LayoutType;
 			aAppFormDto.FormScope = aAppFormEntity.FormScope;
 			aAppFormDto.SearchViewId = aAppFormEntity.SearchViewId;
 			aAppFormDto.SystemDefineRouteState = aAppFormEntity.SystemDefineRouteState;
 			aAppFormDto.RouteParamter1 = aAppFormEntity.RouteParamter1;
 			aAppFormDto.RouteParamter2 = aAppFormEntity.RouteParamter2;
 			aAppFormDto.RouteParamter3 = aAppFormEntity.RouteParamter3;
 			aAppFormDto.DefaultWidth = aAppFormEntity.DefaultWidth;
 			aAppFormDto.DefaultHight = aAppFormEntity.DefaultHight;
 			aAppFormDto.AppCreatedById = aAppFormEntity.AppCreatedById;
 			aAppFormDto.AppCreatedDate = aAppFormEntity.AppCreatedDate;
 			aAppFormDto.AppModifiedDate = aAppFormEntity.AppModifiedDate;
 			aAppFormDto.AppModifiedById = aAppFormEntity.AppModifiedById;
 			aAppFormDto.AppCreatedByCompanyId = aAppFormEntity.AppCreatedByCompanyId;
 			aAppFormDto.SaasApplicationId = aAppFormEntity.SaasApplicationId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFormDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormEntity.AppCreatedDate);
                aAppFormDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFormEntity, aAppFormDto);
		}
		
		 /// <summary>
        ///  Copy AppFormDto Properties to   AppFormEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFormEntity aAppFormEntity,AppFormDto aAppFormDto)
        {        
 
      			aAppFormEntity.Name = aAppFormDto.Name;
      			aAppFormEntity.Description = aAppFormDto.Description;
      			aAppFormEntity.LayoutType = aAppFormDto.LayoutType;
      			aAppFormEntity.FormScope = aAppFormDto.FormScope;
      			aAppFormEntity.SearchViewId = aAppFormDto.SearchViewId;
      			aAppFormEntity.SystemDefineRouteState = aAppFormDto.SystemDefineRouteState;
      			aAppFormEntity.RouteParamter1 = aAppFormDto.RouteParamter1;
      			aAppFormEntity.RouteParamter2 = aAppFormDto.RouteParamter2;
      			aAppFormEntity.RouteParamter3 = aAppFormDto.RouteParamter3;
      			aAppFormEntity.DefaultWidth = aAppFormDto.DefaultWidth;
      			aAppFormEntity.DefaultHight = aAppFormDto.DefaultHight;
 
  
   
    
      			aAppFormEntity.AppCreatedByCompanyId = aAppFormDto.AppCreatedByCompanyId;
      			aAppFormEntity.SaasApplicationId = aAppFormDto.SaasApplicationId;
			
			if(aAppFormDto.Id == null)
			{
				aAppFormEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFormEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFormEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFormEntity, aAppFormDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFormEntity aAppFormEntity,AppFormDto aAppFormDto);
		
		static partial void OnCopyDtoToEntityDone(AppFormEntity aAppFormEntity,AppFormDto aAppFormDto);
		
   
       
    }
}

 