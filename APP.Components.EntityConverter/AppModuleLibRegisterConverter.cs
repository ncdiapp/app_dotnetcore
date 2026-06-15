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
    /// Convert Properties between  AppModuleLibRegisterEntity and  AppModuleLibRegisterDto
    /// </summary>
    public static partial class AppModuleLibRegisterConverter 
    {
         /// <summary>
        ///  Convert AppModuleLibRegisterEntity To  AppModuleLibRegisterDto
        /// </summary>
        public static AppModuleLibRegisterDto ConvertEntityToDto(AppModuleLibRegisterEntity aAppModuleLibRegisterEntity)
        {        
    		AppModuleLibRegisterDto aAppModuleLibRegisterDto = new AppModuleLibRegisterDto();
    		CopyEntityPropertyToDto( aAppModuleLibRegisterEntity, aAppModuleLibRegisterDto);          
			return aAppModuleLibRegisterDto;
        }
		 /// <summary>
        ///  Convert AppModuleLibRegisterEntity To  AppModuleLibRegisterExDto
        /// </summary>
        public static AppModuleLibRegisterExDto ConvertEntityToExDto(AppModuleLibRegisterEntity aAppModuleLibRegisterEntity)
        {        
    		AppModuleLibRegisterExDto aAppModuleLibRegisterExDto = new AppModuleLibRegisterExDto();
			CopyEntityPropertyToDto( aAppModuleLibRegisterEntity, aAppModuleLibRegisterExDto);
			
			
			
            return aAppModuleLibRegisterExDto;
        }
		
		 /// <summary>
        ///  Convert AppModuleLibRegisterEntity To  AppModuleLibRegisterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppModuleLibRegisterEntity aAppModuleLibRegisterEntity,AppModuleLibRegisterDto aAppModuleLibRegisterDto)
        {        
    		
           // aAppModuleLibRegisterDto.StopChangeTracking();
 			aAppModuleLibRegisterDto.Id = aAppModuleLibRegisterEntity.ModuleRegisterId;
 			aAppModuleLibRegisterDto.ModuleName = aAppModuleLibRegisterEntity.ModuleName;
 			aAppModuleLibRegisterDto.FeatureDescption = aAppModuleLibRegisterEntity.FeatureDescption;
 			aAppModuleLibRegisterDto.AppCreatedById = aAppModuleLibRegisterEntity.AppCreatedById;
 			aAppModuleLibRegisterDto.AppCreatedDate = aAppModuleLibRegisterEntity.AppCreatedDate;
 			aAppModuleLibRegisterDto.AppModifiedDate = aAppModuleLibRegisterEntity.AppModifiedDate;
 			aAppModuleLibRegisterDto.AppModifiedById = aAppModuleLibRegisterEntity.AppModifiedById;
 			aAppModuleLibRegisterDto.AppCreatedByCompanyId = aAppModuleLibRegisterEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppModuleLibRegisterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppModuleLibRegisterEntity.AppCreatedDate);
                aAppModuleLibRegisterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppModuleLibRegisterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppModuleLibRegisterEntity, aAppModuleLibRegisterDto);
		}
		
		 /// <summary>
        ///  Copy AppModuleLibRegisterDto Properties to   AppModuleLibRegisterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppModuleLibRegisterEntity aAppModuleLibRegisterEntity,AppModuleLibRegisterDto aAppModuleLibRegisterDto)
        {        
 
      			aAppModuleLibRegisterEntity.ModuleName = aAppModuleLibRegisterDto.ModuleName;
      			aAppModuleLibRegisterEntity.FeatureDescption = aAppModuleLibRegisterDto.FeatureDescption;
 
  
   
    
      			aAppModuleLibRegisterEntity.AppCreatedByCompanyId = aAppModuleLibRegisterDto.AppCreatedByCompanyId;
			
			if(aAppModuleLibRegisterDto.Id == null)
			{
				aAppModuleLibRegisterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppModuleLibRegisterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppModuleLibRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppModuleLibRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppModuleLibRegisterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppModuleLibRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppModuleLibRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppModuleLibRegisterEntity, aAppModuleLibRegisterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppModuleLibRegisterEntity aAppModuleLibRegisterEntity,AppModuleLibRegisterDto aAppModuleLibRegisterDto);
		
		static partial void OnCopyDtoToEntityDone(AppModuleLibRegisterEntity aAppModuleLibRegisterEntity,AppModuleLibRegisterDto aAppModuleLibRegisterDto);
		
   
       
    }
}

 