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
    /// Convert Properties between  AppWebApiParamsHeaderSettigEntity and  AppWebApiParamsHeaderSettigDto
    /// </summary>
    public static partial class AppWebApiParamsHeaderSettigConverter 
    {
         /// <summary>
        ///  Convert AppWebApiParamsHeaderSettigEntity To  AppWebApiParamsHeaderSettigDto
        /// </summary>
        public static AppWebApiParamsHeaderSettigDto ConvertEntityToDto(AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity)
        {        
    		AppWebApiParamsHeaderSettigDto aAppWebApiParamsHeaderSettigDto = new AppWebApiParamsHeaderSettigDto();
    		CopyEntityPropertyToDto( aAppWebApiParamsHeaderSettigEntity, aAppWebApiParamsHeaderSettigDto);          
			return aAppWebApiParamsHeaderSettigDto;
        }
		 /// <summary>
        ///  Convert AppWebApiParamsHeaderSettigEntity To  AppWebApiParamsHeaderSettigExDto
        /// </summary>
        public static AppWebApiParamsHeaderSettigExDto ConvertEntityToExDto(AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity)
        {        
    		AppWebApiParamsHeaderSettigExDto aAppWebApiParamsHeaderSettigExDto = new AppWebApiParamsHeaderSettigExDto();
			CopyEntityPropertyToDto( aAppWebApiParamsHeaderSettigEntity, aAppWebApiParamsHeaderSettigExDto);
			
			
			
            return aAppWebApiParamsHeaderSettigExDto;
        }
		
		 /// <summary>
        ///  Convert AppWebApiParamsHeaderSettigEntity To  AppWebApiParamsHeaderSettigDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity,AppWebApiParamsHeaderSettigDto aAppWebApiParamsHeaderSettigDto)
        {        
    		
           // aAppWebApiParamsHeaderSettigDto.StopChangeTracking();
 			aAppWebApiParamsHeaderSettigDto.Id = aAppWebApiParamsHeaderSettigEntity.ParamHeaderId;
 			aAppWebApiParamsHeaderSettigDto.WebApiConfigId = aAppWebApiParamsHeaderSettigEntity.WebApiConfigId;
 			aAppWebApiParamsHeaderSettigDto.KeyValueType = aAppWebApiParamsHeaderSettigEntity.KeyValueType;
 			aAppWebApiParamsHeaderSettigDto.KeyName = aAppWebApiParamsHeaderSettigEntity.KeyName;
 			aAppWebApiParamsHeaderSettigDto.Value = aAppWebApiParamsHeaderSettigEntity.Value;
 			aAppWebApiParamsHeaderSettigDto.AppCreatedById = aAppWebApiParamsHeaderSettigEntity.AppCreatedById;
 			aAppWebApiParamsHeaderSettigDto.AppCreatedDate = aAppWebApiParamsHeaderSettigEntity.AppCreatedDate;
 			aAppWebApiParamsHeaderSettigDto.AppModifiedDate = aAppWebApiParamsHeaderSettigEntity.AppModifiedDate;
 			aAppWebApiParamsHeaderSettigDto.AppModifiedById = aAppWebApiParamsHeaderSettigEntity.AppModifiedById;
 			aAppWebApiParamsHeaderSettigDto.AppCreatedByCompanyId = aAppWebApiParamsHeaderSettigEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppWebApiParamsHeaderSettigDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWebApiParamsHeaderSettigEntity.AppCreatedDate);
                aAppWebApiParamsHeaderSettigDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppWebApiParamsHeaderSettigEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppWebApiParamsHeaderSettigEntity, aAppWebApiParamsHeaderSettigDto);
		}
		
		 /// <summary>
        ///  Copy AppWebApiParamsHeaderSettigDto Properties to   AppWebApiParamsHeaderSettigEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity,AppWebApiParamsHeaderSettigDto aAppWebApiParamsHeaderSettigDto)
        {        
 
      			aAppWebApiParamsHeaderSettigEntity.WebApiConfigId = aAppWebApiParamsHeaderSettigDto.WebApiConfigId;
      			aAppWebApiParamsHeaderSettigEntity.KeyValueType = aAppWebApiParamsHeaderSettigDto.KeyValueType;
      			aAppWebApiParamsHeaderSettigEntity.KeyName = aAppWebApiParamsHeaderSettigDto.KeyName;
      			aAppWebApiParamsHeaderSettigEntity.Value = aAppWebApiParamsHeaderSettigDto.Value;
 
  
   
    
      			aAppWebApiParamsHeaderSettigEntity.AppCreatedByCompanyId = aAppWebApiParamsHeaderSettigDto.AppCreatedByCompanyId;
			
			if(aAppWebApiParamsHeaderSettigDto.Id == null)
			{
				aAppWebApiParamsHeaderSettigEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppWebApiParamsHeaderSettigEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppWebApiParamsHeaderSettigEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWebApiParamsHeaderSettigEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppWebApiParamsHeaderSettigEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppWebApiParamsHeaderSettigEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppWebApiParamsHeaderSettigEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppWebApiParamsHeaderSettigEntity, aAppWebApiParamsHeaderSettigDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity,AppWebApiParamsHeaderSettigDto aAppWebApiParamsHeaderSettigDto);
		
		static partial void OnCopyDtoToEntityDone(AppWebApiParamsHeaderSettigEntity aAppWebApiParamsHeaderSettigEntity,AppWebApiParamsHeaderSettigDto aAppWebApiParamsHeaderSettigDto);
		
   
       
    }
}

 