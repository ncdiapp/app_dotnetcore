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
    /// Convert Properties between  AppBusinessScopeTagEntity and  AppBusinessScopeTagDto
    /// </summary>
    public static partial class AppBusinessScopeTagConverter 
    {
         /// <summary>
        ///  Convert AppBusinessScopeTagEntity To  AppBusinessScopeTagDto
        /// </summary>
        public static AppBusinessScopeTagDto ConvertEntityToDto(AppBusinessScopeTagEntity aAppBusinessScopeTagEntity)
        {        
    		AppBusinessScopeTagDto aAppBusinessScopeTagDto = new AppBusinessScopeTagDto();
    		CopyEntityPropertyToDto( aAppBusinessScopeTagEntity, aAppBusinessScopeTagDto);          
			return aAppBusinessScopeTagDto;
        }
		 /// <summary>
        ///  Convert AppBusinessScopeTagEntity To  AppBusinessScopeTagExDto
        /// </summary>
        public static AppBusinessScopeTagExDto ConvertEntityToExDto(AppBusinessScopeTagEntity aAppBusinessScopeTagEntity)
        {        
    		AppBusinessScopeTagExDto aAppBusinessScopeTagExDto = new AppBusinessScopeTagExDto();
			CopyEntityPropertyToDto( aAppBusinessScopeTagEntity, aAppBusinessScopeTagExDto);
			
			
			
            return aAppBusinessScopeTagExDto;
        }
		
		 /// <summary>
        ///  Convert AppBusinessScopeTagEntity To  AppBusinessScopeTagDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppBusinessScopeTagEntity aAppBusinessScopeTagEntity,AppBusinessScopeTagDto aAppBusinessScopeTagDto)
        {        
    		
           // aAppBusinessScopeTagDto.StopChangeTracking();
 			aAppBusinessScopeTagDto.Id = aAppBusinessScopeTagEntity.ScopeTagId;
 			aAppBusinessScopeTagDto.TagName = aAppBusinessScopeTagEntity.TagName;
 			aAppBusinessScopeTagDto.TagIconStyle = aAppBusinessScopeTagEntity.TagIconStyle;
 			aAppBusinessScopeTagDto.EmTagBusienssScope = aAppBusinessScopeTagEntity.EmTagBusienssScope;
 			aAppBusinessScopeTagDto.AppCreatedById = aAppBusinessScopeTagEntity.AppCreatedById;
 			aAppBusinessScopeTagDto.AppCreatedDate = aAppBusinessScopeTagEntity.AppCreatedDate;
 			aAppBusinessScopeTagDto.AppModifiedDate = aAppBusinessScopeTagEntity.AppModifiedDate;
 			aAppBusinessScopeTagDto.AppModifiedById = aAppBusinessScopeTagEntity.AppModifiedById;
 			aAppBusinessScopeTagDto.AppCreatedByCompanyId = aAppBusinessScopeTagEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppBusinessScopeTagDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessScopeTagEntity.AppCreatedDate);
                aAppBusinessScopeTagDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBusinessScopeTagEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppBusinessScopeTagEntity, aAppBusinessScopeTagDto);
		}
		
		 /// <summary>
        ///  Copy AppBusinessScopeTagDto Properties to   AppBusinessScopeTagEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppBusinessScopeTagEntity aAppBusinessScopeTagEntity,AppBusinessScopeTagDto aAppBusinessScopeTagDto)
        {        
 
      			aAppBusinessScopeTagEntity.TagName = aAppBusinessScopeTagDto.TagName;
      			aAppBusinessScopeTagEntity.TagIconStyle = aAppBusinessScopeTagDto.TagIconStyle;
      			aAppBusinessScopeTagEntity.EmTagBusienssScope = aAppBusinessScopeTagDto.EmTagBusienssScope;
 
  
   
    
      			aAppBusinessScopeTagEntity.AppCreatedByCompanyId = aAppBusinessScopeTagDto.AppCreatedByCompanyId;
			
			if(aAppBusinessScopeTagDto.Id == null)
			{
				aAppBusinessScopeTagEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessScopeTagEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppBusinessScopeTagEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessScopeTagEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppBusinessScopeTagEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppBusinessScopeTagEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBusinessScopeTagEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppBusinessScopeTagEntity, aAppBusinessScopeTagDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppBusinessScopeTagEntity aAppBusinessScopeTagEntity,AppBusinessScopeTagDto aAppBusinessScopeTagDto);
		
		static partial void OnCopyDtoToEntityDone(AppBusinessScopeTagEntity aAppBusinessScopeTagEntity,AppBusinessScopeTagDto aAppBusinessScopeTagDto);
		
   
       
    }
}

 