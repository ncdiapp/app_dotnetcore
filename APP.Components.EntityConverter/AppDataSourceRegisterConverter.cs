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
    /// Convert Properties between  AppDataSourceRegisterEntity and  AppDataSourceRegisterDto
    /// </summary>
    public static partial class AppDataSourceRegisterConverter 
    {
         /// <summary>
        ///  Convert AppDataSourceRegisterEntity To  AppDataSourceRegisterDto
        /// </summary>
        public static AppDataSourceRegisterDto ConvertEntityToDto(AppDataSourceRegisterEntity aAppDataSourceRegisterEntity)
        {        
    		AppDataSourceRegisterDto aAppDataSourceRegisterDto = new AppDataSourceRegisterDto();
    		CopyEntityPropertyToDto( aAppDataSourceRegisterEntity, aAppDataSourceRegisterDto);          
			return aAppDataSourceRegisterDto;
        }
		 /// <summary>
        ///  Convert AppDataSourceRegisterEntity To  AppDataSourceRegisterExDto
        /// </summary>
        public static AppDataSourceRegisterExDto ConvertEntityToExDto(AppDataSourceRegisterEntity aAppDataSourceRegisterEntity)
        {        
    		AppDataSourceRegisterExDto aAppDataSourceRegisterExDto = new AppDataSourceRegisterExDto();
			CopyEntityPropertyToDto( aAppDataSourceRegisterEntity, aAppDataSourceRegisterExDto);
			
			
			
            return aAppDataSourceRegisterExDto;
        }
		
		 /// <summary>
        ///  Convert AppDataSourceRegisterEntity To  AppDataSourceRegisterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDataSourceRegisterEntity aAppDataSourceRegisterEntity,AppDataSourceRegisterDto aAppDataSourceRegisterDto)
        {        
    		
           // aAppDataSourceRegisterDto.StopChangeTracking();
 			aAppDataSourceRegisterDto.Id = aAppDataSourceRegisterEntity.DataSourceId;
 			aAppDataSourceRegisterDto.DataSourceName = aAppDataSourceRegisterEntity.DataSourceName;
 			aAppDataSourceRegisterDto.Description = aAppDataSourceRegisterEntity.Description;
 			aAppDataSourceRegisterDto.DataSourceType = aAppDataSourceRegisterEntity.DataSourceType;
 			aAppDataSourceRegisterDto.ConnectionString = aAppDataSourceRegisterEntity.ConnectionString;
 			aAppDataSourceRegisterDto.AppCreatedById = aAppDataSourceRegisterEntity.AppCreatedById;
 			aAppDataSourceRegisterDto.AppCreatedDate = aAppDataSourceRegisterEntity.AppCreatedDate;
 			aAppDataSourceRegisterDto.AppModifiedDate = aAppDataSourceRegisterEntity.AppModifiedDate;
 			aAppDataSourceRegisterDto.AppModifiedById = aAppDataSourceRegisterEntity.AppModifiedById;
 			aAppDataSourceRegisterDto.DataSourceOwnerCompanyId = aAppDataSourceRegisterEntity.DataSourceOwnerCompanyId;
 			aAppDataSourceRegisterDto.AppCreatedByCompanyId = aAppDataSourceRegisterEntity.AppCreatedByCompanyId;
 			aAppDataSourceRegisterDto.DatabaseName = aAppDataSourceRegisterEntity.DatabaseName;
 			aAppDataSourceRegisterDto.IsCompanyMasterDb = aAppDataSourceRegisterEntity.IsCompanyMasterDb;
 			aAppDataSourceRegisterDto.CustomDomain = aAppDataSourceRegisterEntity.CustomDomain;
 			aAppDataSourceRegisterDto.DomainToken = aAppDataSourceRegisterEntity.DomainToken;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDataSourceRegisterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDataSourceRegisterEntity.AppCreatedDate);
                aAppDataSourceRegisterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDataSourceRegisterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDataSourceRegisterEntity, aAppDataSourceRegisterDto);
		}
		
		 /// <summary>
        ///  Copy AppDataSourceRegisterDto Properties to   AppDataSourceRegisterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDataSourceRegisterEntity aAppDataSourceRegisterEntity,AppDataSourceRegisterDto aAppDataSourceRegisterDto)
        {        
 
      			aAppDataSourceRegisterEntity.DataSourceName = aAppDataSourceRegisterDto.DataSourceName;
      			aAppDataSourceRegisterEntity.Description = aAppDataSourceRegisterDto.Description;
      			aAppDataSourceRegisterEntity.DataSourceType = aAppDataSourceRegisterDto.DataSourceType;
      			aAppDataSourceRegisterEntity.ConnectionString = aAppDataSourceRegisterDto.ConnectionString;
 
  
   
    
      			aAppDataSourceRegisterEntity.DataSourceOwnerCompanyId = aAppDataSourceRegisterDto.DataSourceOwnerCompanyId;
      			aAppDataSourceRegisterEntity.AppCreatedByCompanyId = aAppDataSourceRegisterDto.AppCreatedByCompanyId;
      			aAppDataSourceRegisterEntity.DatabaseName = aAppDataSourceRegisterDto.DatabaseName;
      			aAppDataSourceRegisterEntity.IsCompanyMasterDb = aAppDataSourceRegisterDto.IsCompanyMasterDb;
      			aAppDataSourceRegisterEntity.CustomDomain = aAppDataSourceRegisterDto.CustomDomain;
      			aAppDataSourceRegisterEntity.DomainToken = aAppDataSourceRegisterDto.DomainToken;
			
			if(aAppDataSourceRegisterDto.Id == null)
			{
				aAppDataSourceRegisterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDataSourceRegisterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDataSourceRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDataSourceRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDataSourceRegisterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDataSourceRegisterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDataSourceRegisterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDataSourceRegisterEntity, aAppDataSourceRegisterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDataSourceRegisterEntity aAppDataSourceRegisterEntity,AppDataSourceRegisterDto aAppDataSourceRegisterDto);
		
		static partial void OnCopyDtoToEntityDone(AppDataSourceRegisterEntity aAppDataSourceRegisterEntity,AppDataSourceRegisterDto aAppDataSourceRegisterDto);
		
   
       
    }
}

 