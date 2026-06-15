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
    /// Convert Properties between  AppTransactionDataTransferSettingEntity and  AppTransactionDataTransferSettingDto
    /// </summary>
    public static partial class AppTransactionDataTransferSettingConverter 
    {
         /// <summary>
        ///  Convert AppTransactionDataTransferSettingEntity To  AppTransactionDataTransferSettingDto
        /// </summary>
        public static AppTransactionDataTransferSettingDto ConvertEntityToDto(AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity)
        {        
    		AppTransactionDataTransferSettingDto aAppTransactionDataTransferSettingDto = new AppTransactionDataTransferSettingDto();
    		CopyEntityPropertyToDto( aAppTransactionDataTransferSettingEntity, aAppTransactionDataTransferSettingDto);          
			return aAppTransactionDataTransferSettingDto;
        }
		 /// <summary>
        ///  Convert AppTransactionDataTransferSettingEntity To  AppTransactionDataTransferSettingExDto
        /// </summary>
        public static AppTransactionDataTransferSettingExDto ConvertEntityToExDto(AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity)
        {        
    		AppTransactionDataTransferSettingExDto aAppTransactionDataTransferSettingExDto = new AppTransactionDataTransferSettingExDto();
			CopyEntityPropertyToDto( aAppTransactionDataTransferSettingEntity, aAppTransactionDataTransferSettingExDto);
			
			
			
            return aAppTransactionDataTransferSettingExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionDataTransferSettingEntity To  AppTransactionDataTransferSettingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity,AppTransactionDataTransferSettingDto aAppTransactionDataTransferSettingDto)
        {        
    		
           // aAppTransactionDataTransferSettingDto.StopChangeTracking();
 			aAppTransactionDataTransferSettingDto.Id = aAppTransactionDataTransferSettingEntity.DataTransferSettingId;
 			aAppTransactionDataTransferSettingDto.TransferTypeId = aAppTransactionDataTransferSettingEntity.TransferTypeId;
 			aAppTransactionDataTransferSettingDto.TransactionId = aAppTransactionDataTransferSettingEntity.TransactionId;
 			aAppTransactionDataTransferSettingDto.DestinationTransactionId = aAppTransactionDataTransferSettingEntity.DestinationTransactionId;
 			aAppTransactionDataTransferSettingDto.InternalCode = aAppTransactionDataTransferSettingEntity.InternalCode;
 			aAppTransactionDataTransferSettingDto.Description = aAppTransactionDataTransferSettingEntity.Description;
 			aAppTransactionDataTransferSettingDto.AppCreatedById = aAppTransactionDataTransferSettingEntity.AppCreatedById;
 			aAppTransactionDataTransferSettingDto.AppCreatedDate = aAppTransactionDataTransferSettingEntity.AppCreatedDate;
 			aAppTransactionDataTransferSettingDto.AppModifiedDate = aAppTransactionDataTransferSettingEntity.AppModifiedDate;
 			aAppTransactionDataTransferSettingDto.AppModifiedById = aAppTransactionDataTransferSettingEntity.AppModifiedById;
 			aAppTransactionDataTransferSettingDto.AppCreatedByCompanyId = aAppTransactionDataTransferSettingEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionDataTransferSettingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionDataTransferSettingEntity.AppCreatedDate);
                aAppTransactionDataTransferSettingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionDataTransferSettingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionDataTransferSettingEntity, aAppTransactionDataTransferSettingDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionDataTransferSettingDto Properties to   AppTransactionDataTransferSettingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity,AppTransactionDataTransferSettingDto aAppTransactionDataTransferSettingDto)
        {        
 
      			aAppTransactionDataTransferSettingEntity.TransferTypeId = aAppTransactionDataTransferSettingDto.TransferTypeId;
      			aAppTransactionDataTransferSettingEntity.TransactionId = aAppTransactionDataTransferSettingDto.TransactionId;
      			aAppTransactionDataTransferSettingEntity.DestinationTransactionId = aAppTransactionDataTransferSettingDto.DestinationTransactionId;
      			aAppTransactionDataTransferSettingEntity.InternalCode = aAppTransactionDataTransferSettingDto.InternalCode;
      			aAppTransactionDataTransferSettingEntity.Description = aAppTransactionDataTransferSettingDto.Description;
 
  
   
    
      			aAppTransactionDataTransferSettingEntity.AppCreatedByCompanyId = aAppTransactionDataTransferSettingDto.AppCreatedByCompanyId;
			
			if(aAppTransactionDataTransferSettingDto.Id == null)
			{
				aAppTransactionDataTransferSettingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionDataTransferSettingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionDataTransferSettingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionDataTransferSettingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionDataTransferSettingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionDataTransferSettingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionDataTransferSettingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionDataTransferSettingEntity, aAppTransactionDataTransferSettingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity,AppTransactionDataTransferSettingDto aAppTransactionDataTransferSettingDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionDataTransferSettingEntity aAppTransactionDataTransferSettingEntity,AppTransactionDataTransferSettingDto aAppTransactionDataTransferSettingDto);
		
   
       
    }
}

 