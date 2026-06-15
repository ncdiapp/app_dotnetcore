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
    /// Convert Properties between  AppTransactionGroupSessionEntity and  AppTransactionGroupSessionDto
    /// </summary>
    public static partial class AppTransactionGroupSessionConverter 
    {
         /// <summary>
        ///  Convert AppTransactionGroupSessionEntity To  AppTransactionGroupSessionDto
        /// </summary>
        public static AppTransactionGroupSessionDto ConvertEntityToDto(AppTransactionGroupSessionEntity aAppTransactionGroupSessionEntity)
        {        
    		AppTransactionGroupSessionDto aAppTransactionGroupSessionDto = new AppTransactionGroupSessionDto();
    		CopyEntityPropertyToDto( aAppTransactionGroupSessionEntity, aAppTransactionGroupSessionDto);          
			return aAppTransactionGroupSessionDto;
        }
		 /// <summary>
        ///  Convert AppTransactionGroupSessionEntity To  AppTransactionGroupSessionExDto
        /// </summary>
        public static AppTransactionGroupSessionExDto ConvertEntityToExDto(AppTransactionGroupSessionEntity aAppTransactionGroupSessionEntity)
        {        
    		AppTransactionGroupSessionExDto aAppTransactionGroupSessionExDto = new AppTransactionGroupSessionExDto();
			CopyEntityPropertyToDto( aAppTransactionGroupSessionEntity, aAppTransactionGroupSessionExDto);
			
			
			
            return aAppTransactionGroupSessionExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionGroupSessionEntity To  AppTransactionGroupSessionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionGroupSessionEntity aAppTransactionGroupSessionEntity,AppTransactionGroupSessionDto aAppTransactionGroupSessionDto)
        {        
    		
           // aAppTransactionGroupSessionDto.StopChangeTracking();
 			aAppTransactionGroupSessionDto.Id = aAppTransactionGroupSessionEntity.TransactionGroupSessionId;
 			aAppTransactionGroupSessionDto.TransactionGroupId = aAppTransactionGroupSessionEntity.TransactionGroupId;
 			aAppTransactionGroupSessionDto.SessionGroupName = aAppTransactionGroupSessionEntity.SessionGroupName;
 			aAppTransactionGroupSessionDto.Description = aAppTransactionGroupSessionEntity.Description;
 			aAppTransactionGroupSessionDto.AppCreatedById = aAppTransactionGroupSessionEntity.AppCreatedById;
 			aAppTransactionGroupSessionDto.AppCreatedDate = aAppTransactionGroupSessionEntity.AppCreatedDate;
 			aAppTransactionGroupSessionDto.AppModifiedDate = aAppTransactionGroupSessionEntity.AppModifiedDate;
 			aAppTransactionGroupSessionDto.AppModifiedById = aAppTransactionGroupSessionEntity.AppModifiedById;
 			aAppTransactionGroupSessionDto.AppCreatedByCompanyId = aAppTransactionGroupSessionEntity.AppCreatedByCompanyId;
 			aAppTransactionGroupSessionDto.SaasApplicationId = aAppTransactionGroupSessionEntity.SaasApplicationId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionGroupSessionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionGroupSessionEntity.AppCreatedDate);
                aAppTransactionGroupSessionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionGroupSessionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionGroupSessionEntity, aAppTransactionGroupSessionDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionGroupSessionDto Properties to   AppTransactionGroupSessionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionGroupSessionEntity aAppTransactionGroupSessionEntity,AppTransactionGroupSessionDto aAppTransactionGroupSessionDto)
        {        
 
      			aAppTransactionGroupSessionEntity.TransactionGroupId = aAppTransactionGroupSessionDto.TransactionGroupId;
      			aAppTransactionGroupSessionEntity.SessionGroupName = aAppTransactionGroupSessionDto.SessionGroupName;
      			aAppTransactionGroupSessionEntity.Description = aAppTransactionGroupSessionDto.Description;
 
  
   
    
      			aAppTransactionGroupSessionEntity.AppCreatedByCompanyId = aAppTransactionGroupSessionDto.AppCreatedByCompanyId;
      			aAppTransactionGroupSessionEntity.SaasApplicationId = aAppTransactionGroupSessionDto.SaasApplicationId;
			
			if(aAppTransactionGroupSessionDto.Id == null)
			{
				aAppTransactionGroupSessionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionGroupSessionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionGroupSessionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionGroupSessionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionGroupSessionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionGroupSessionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionGroupSessionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionGroupSessionEntity, aAppTransactionGroupSessionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionGroupSessionEntity aAppTransactionGroupSessionEntity,AppTransactionGroupSessionDto aAppTransactionGroupSessionDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionGroupSessionEntity aAppTransactionGroupSessionEntity,AppTransactionGroupSessionDto aAppTransactionGroupSessionDto);
		
   
       
    }
}

 