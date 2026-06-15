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
    /// Convert Properties between  AppCompanyUserTypeRegisterEntity and  AppCompanyUserTypeRegisterDto
    /// </summary>
    public static partial class AppCompanyUserTypeRegisterConverter 
    {
         /// <summary>
        ///  Convert AppCompanyUserTypeRegisterEntity To  AppCompanyUserTypeRegisterDto
        /// </summary>
        public static AppCompanyUserTypeRegisterDto ConvertEntityToDto(AppCompanyUserTypeRegisterEntity aAppCompanyUserTypeRegisterEntity)
        {        
    		AppCompanyUserTypeRegisterDto aAppCompanyUserTypeRegisterDto = new AppCompanyUserTypeRegisterDto();
    		CopyEntityPropertyToDto( aAppCompanyUserTypeRegisterEntity, aAppCompanyUserTypeRegisterDto);          
			return aAppCompanyUserTypeRegisterDto;
        }
		 /// <summary>
        ///  Convert AppCompanyUserTypeRegisterEntity To  AppCompanyUserTypeRegisterExDto
        /// </summary>
        public static AppCompanyUserTypeRegisterExDto ConvertEntityToExDto(AppCompanyUserTypeRegisterEntity aAppCompanyUserTypeRegisterEntity)
        {        
    		AppCompanyUserTypeRegisterExDto aAppCompanyUserTypeRegisterExDto = new AppCompanyUserTypeRegisterExDto();
			CopyEntityPropertyToDto( aAppCompanyUserTypeRegisterEntity, aAppCompanyUserTypeRegisterExDto);
			
			
			
            return aAppCompanyUserTypeRegisterExDto;
        }
		
		 /// <summary>
        ///  Convert AppCompanyUserTypeRegisterEntity To  AppCompanyUserTypeRegisterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppCompanyUserTypeRegisterEntity aAppCompanyUserTypeRegisterEntity,AppCompanyUserTypeRegisterDto aAppCompanyUserTypeRegisterDto)
        {        
    		
           // aAppCompanyUserTypeRegisterDto.StopChangeTracking();
 			aAppCompanyUserTypeRegisterDto.Id = aAppCompanyUserTypeRegisterEntity.CompanyUserTypeRegisterId;
 			aAppCompanyUserTypeRegisterDto.CompanyId = aAppCompanyUserTypeRegisterEntity.CompanyId;
 			aAppCompanyUserTypeRegisterDto.UserType = aAppCompanyUserTypeRegisterEntity.UserType;
 			aAppCompanyUserTypeRegisterDto.MappingToEntityId = aAppCompanyUserTypeRegisterEntity.MappingToEntityId;
 			aAppCompanyUserTypeRegisterDto.AppCompanyId = aAppCompanyUserTypeRegisterEntity.AppCompanyId;
 			aAppCompanyUserTypeRegisterDto.AppCreatedById = aAppCompanyUserTypeRegisterEntity.AppCreatedById;
 			aAppCompanyUserTypeRegisterDto.AppCreatedDate = aAppCompanyUserTypeRegisterEntity.AppCreatedDate;
 			aAppCompanyUserTypeRegisterDto.AppModifiedDate = aAppCompanyUserTypeRegisterEntity.AppModifiedDate;
 			aAppCompanyUserTypeRegisterDto.AppModifiedById = aAppCompanyUserTypeRegisterEntity.AppModifiedById;
 			aAppCompanyUserTypeRegisterDto.AppCreatedByCompanyId = aAppCompanyUserTypeRegisterEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppCompanyUserTypeRegisterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCompanyUserTypeRegisterEntity.AppCreatedDate);
                aAppCompanyUserTypeRegisterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppCompanyUserTypeRegisterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppCompanyUserTypeRegisterEntity, aAppCompanyUserTypeRegisterDto);
		}
		
		 /// <summary>
        ///  Copy AppCompanyUserTypeRegisterDto Properties to   AppCompanyUserTypeRegisterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppCompanyUserTypeRegisterEntity aAppCompanyUserTypeRegisterEntity,AppCompanyUserTypeRegisterDto aAppCompanyUserTypeRegisterDto)
        {        
 
      			aAppCompanyUserTypeRegisterEntity.CompanyId = aAppCompanyUserTypeRegisterDto.CompanyId;
      			aAppCompanyUserTypeRegisterEntity.UserType = aAppCompanyUserTypeRegisterDto.UserType;
      			aAppCompanyUserTypeRegisterEntity.MappingToEntityId = aAppCompanyUserTypeRegisterDto.MappingToEntityId;
      			aAppCompanyUserTypeRegisterEntity.AppCompanyId = aAppCompanyUserTypeRegisterDto.AppCompanyId;
 
  
   
    
      			aAppCompanyUserTypeRegisterEntity.AppCreatedByCompanyId = aAppCompanyUserTypeRegisterDto.AppCreatedByCompanyId;
			
			if(aAppCompanyUserTypeRegisterDto.Id == null)
			{
				aAppCompanyUserTypeRegisterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppCompanyUserTypeRegisterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppCompanyUserTypeRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCompanyUserTypeRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppCompanyUserTypeRegisterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppCompanyUserTypeRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppCompanyUserTypeRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppCompanyUserTypeRegisterEntity, aAppCompanyUserTypeRegisterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppCompanyUserTypeRegisterEntity aAppCompanyUserTypeRegisterEntity,AppCompanyUserTypeRegisterDto aAppCompanyUserTypeRegisterDto);
		
		static partial void OnCopyDtoToEntityDone(AppCompanyUserTypeRegisterEntity aAppCompanyUserTypeRegisterEntity,AppCompanyUserTypeRegisterDto aAppCompanyUserTypeRegisterDto);
		
   
       
    }
}

 