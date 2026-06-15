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
    /// Convert Properties between  AppExternalMethodRegisterEntity and  AppExternalMethodRegisterDto
    /// </summary>
    public static partial class AppExternalMethodRegisterConverter 
    {
         /// <summary>
        ///  Convert AppExternalMethodRegisterEntity To  AppExternalMethodRegisterDto
        /// </summary>
        public static AppExternalMethodRegisterDto ConvertEntityToDto(AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity)
        {        
    		AppExternalMethodRegisterDto aAppExternalMethodRegisterDto = new AppExternalMethodRegisterDto();
    		CopyEntityPropertyToDto( aAppExternalMethodRegisterEntity, aAppExternalMethodRegisterDto);          
			return aAppExternalMethodRegisterDto;
        }
		 /// <summary>
        ///  Convert AppExternalMethodRegisterEntity To  AppExternalMethodRegisterExDto
        /// </summary>
        public static AppExternalMethodRegisterExDto ConvertEntityToExDto(AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity)
        {        
    		AppExternalMethodRegisterExDto aAppExternalMethodRegisterExDto = new AppExternalMethodRegisterExDto();
			CopyEntityPropertyToDto( aAppExternalMethodRegisterEntity, aAppExternalMethodRegisterExDto);
			
			
			
            return aAppExternalMethodRegisterExDto;
        }
		
		 /// <summary>
        ///  Convert AppExternalMethodRegisterEntity To  AppExternalMethodRegisterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity,AppExternalMethodRegisterDto aAppExternalMethodRegisterDto)
        {        
    		
           // aAppExternalMethodRegisterDto.StopChangeTracking();
 			aAppExternalMethodRegisterDto.Id = aAppExternalMethodRegisterEntity.MethodRegisterId;
 			aAppExternalMethodRegisterDto.MethodDisplayName = aAppExternalMethodRegisterEntity.MethodDisplayName;
 			aAppExternalMethodRegisterDto.AssemblyName = aAppExternalMethodRegisterEntity.AssemblyName;
 			aAppExternalMethodRegisterDto.TypeName = aAppExternalMethodRegisterEntity.TypeName;
 			aAppExternalMethodRegisterDto.MethodName = aAppExternalMethodRegisterEntity.MethodName;
 			aAppExternalMethodRegisterDto.InputParameterList = aAppExternalMethodRegisterEntity.InputParameterList;
 			aAppExternalMethodRegisterDto.AppCreatedById = aAppExternalMethodRegisterEntity.AppCreatedById;
 			aAppExternalMethodRegisterDto.AppCreatedDate = aAppExternalMethodRegisterEntity.AppCreatedDate;
 			aAppExternalMethodRegisterDto.AppModifiedDate = aAppExternalMethodRegisterEntity.AppModifiedDate;
 			aAppExternalMethodRegisterDto.AppModifiedById = aAppExternalMethodRegisterEntity.AppModifiedById;
 			aAppExternalMethodRegisterDto.AppCreatedByCompanyId = aAppExternalMethodRegisterEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppExternalMethodRegisterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppExternalMethodRegisterEntity.AppCreatedDate);
                aAppExternalMethodRegisterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppExternalMethodRegisterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppExternalMethodRegisterEntity, aAppExternalMethodRegisterDto);
		}
		
		 /// <summary>
        ///  Copy AppExternalMethodRegisterDto Properties to   AppExternalMethodRegisterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity,AppExternalMethodRegisterDto aAppExternalMethodRegisterDto)
        {        
 
      			aAppExternalMethodRegisterEntity.MethodDisplayName = aAppExternalMethodRegisterDto.MethodDisplayName;
      			aAppExternalMethodRegisterEntity.AssemblyName = aAppExternalMethodRegisterDto.AssemblyName;
      			aAppExternalMethodRegisterEntity.TypeName = aAppExternalMethodRegisterDto.TypeName;
      			aAppExternalMethodRegisterEntity.MethodName = aAppExternalMethodRegisterDto.MethodName;
      			aAppExternalMethodRegisterEntity.InputParameterList = aAppExternalMethodRegisterDto.InputParameterList;
 
  
   
    
      			aAppExternalMethodRegisterEntity.AppCreatedByCompanyId = aAppExternalMethodRegisterDto.AppCreatedByCompanyId;
			
			if(aAppExternalMethodRegisterDto.Id == null)
			{
				aAppExternalMethodRegisterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppExternalMethodRegisterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppExternalMethodRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppExternalMethodRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppExternalMethodRegisterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppExternalMethodRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppExternalMethodRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppExternalMethodRegisterEntity, aAppExternalMethodRegisterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity,AppExternalMethodRegisterDto aAppExternalMethodRegisterDto);
		
		static partial void OnCopyDtoToEntityDone(AppExternalMethodRegisterEntity aAppExternalMethodRegisterEntity,AppExternalMethodRegisterDto aAppExternalMethodRegisterDto);
		
   
       
    }
}

 