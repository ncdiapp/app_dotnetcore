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
    /// Convert Properties between  AppSecurityTransactionActionResourceEntity and  AppSecurityTransactionActionResourceDto
    /// </summary>
    public static partial class AppSecurityTransactionActionResourceConverter 
    {
         /// <summary>
        ///  Convert AppSecurityTransactionActionResourceEntity To  AppSecurityTransactionActionResourceDto
        /// </summary>
        public static AppSecurityTransactionActionResourceDto ConvertEntityToDto(AppSecurityTransactionActionResourceEntity aAppSecurityTransactionActionResourceEntity)
        {        
    		AppSecurityTransactionActionResourceDto aAppSecurityTransactionActionResourceDto = new AppSecurityTransactionActionResourceDto();
    		CopyEntityPropertyToDto( aAppSecurityTransactionActionResourceEntity, aAppSecurityTransactionActionResourceDto);          
			return aAppSecurityTransactionActionResourceDto;
        }
		 /// <summary>
        ///  Convert AppSecurityTransactionActionResourceEntity To  AppSecurityTransactionActionResourceExDto
        /// </summary>
        public static AppSecurityTransactionActionResourceExDto ConvertEntityToExDto(AppSecurityTransactionActionResourceEntity aAppSecurityTransactionActionResourceEntity)
        {        
    		AppSecurityTransactionActionResourceExDto aAppSecurityTransactionActionResourceExDto = new AppSecurityTransactionActionResourceExDto();
			CopyEntityPropertyToDto( aAppSecurityTransactionActionResourceEntity, aAppSecurityTransactionActionResourceExDto);
			
			
			
            return aAppSecurityTransactionActionResourceExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityTransactionActionResourceEntity To  AppSecurityTransactionActionResourceDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityTransactionActionResourceEntity aAppSecurityTransactionActionResourceEntity,AppSecurityTransactionActionResourceDto aAppSecurityTransactionActionResourceDto)
        {        
    		
           // aAppSecurityTransactionActionResourceDto.StopChangeTracking();
 			aAppSecurityTransactionActionResourceDto.Id = aAppSecurityTransactionActionResourceEntity.AppFormActionResourceId;
 			aAppSecurityTransactionActionResourceDto.TransactionActionId = aAppSecurityTransactionActionResourceEntity.TransactionActionId;
 			aAppSecurityTransactionActionResourceDto.GroupId = aAppSecurityTransactionActionResourceEntity.GroupId;
 			aAppSecurityTransactionActionResourceDto.UserId = aAppSecurityTransactionActionResourceEntity.UserId;
 			aAppSecurityTransactionActionResourceDto.AppCreatedById = aAppSecurityTransactionActionResourceEntity.AppCreatedById;
 			aAppSecurityTransactionActionResourceDto.AppCreatedDate = aAppSecurityTransactionActionResourceEntity.AppCreatedDate;
 			aAppSecurityTransactionActionResourceDto.AppModifiedDate = aAppSecurityTransactionActionResourceEntity.AppModifiedDate;
 			aAppSecurityTransactionActionResourceDto.AppModifiedById = aAppSecurityTransactionActionResourceEntity.AppModifiedById;
 			aAppSecurityTransactionActionResourceDto.AppCreatedByCompanyId = aAppSecurityTransactionActionResourceEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityTransactionActionResourceDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityTransactionActionResourceEntity.AppCreatedDate);
                aAppSecurityTransactionActionResourceDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityTransactionActionResourceEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityTransactionActionResourceEntity, aAppSecurityTransactionActionResourceDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityTransactionActionResourceDto Properties to   AppSecurityTransactionActionResourceEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityTransactionActionResourceEntity aAppSecurityTransactionActionResourceEntity,AppSecurityTransactionActionResourceDto aAppSecurityTransactionActionResourceDto)
        {        
 
      			aAppSecurityTransactionActionResourceEntity.TransactionActionId = aAppSecurityTransactionActionResourceDto.TransactionActionId;
      			aAppSecurityTransactionActionResourceEntity.GroupId = aAppSecurityTransactionActionResourceDto.GroupId;
      			aAppSecurityTransactionActionResourceEntity.UserId = aAppSecurityTransactionActionResourceDto.UserId;
 
  
   
    
      			aAppSecurityTransactionActionResourceEntity.AppCreatedByCompanyId = aAppSecurityTransactionActionResourceDto.AppCreatedByCompanyId;
			
			if(aAppSecurityTransactionActionResourceDto.Id == null)
			{
				aAppSecurityTransactionActionResourceEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityTransactionActionResourceEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityTransactionActionResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityTransactionActionResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityTransactionActionResourceEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityTransactionActionResourceEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityTransactionActionResourceEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityTransactionActionResourceEntity, aAppSecurityTransactionActionResourceDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityTransactionActionResourceEntity aAppSecurityTransactionActionResourceEntity,AppSecurityTransactionActionResourceDto aAppSecurityTransactionActionResourceDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityTransactionActionResourceEntity aAppSecurityTransactionActionResourceEntity,AppSecurityTransactionActionResourceDto aAppSecurityTransactionActionResourceDto);
		
   
       
    }
}

 