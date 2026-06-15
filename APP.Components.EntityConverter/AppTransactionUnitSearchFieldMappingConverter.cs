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
    /// Convert Properties between  AppTransactionUnitSearchFieldMappingEntity and  AppTransactionUnitSearchFieldMappingDto
    /// </summary>
    public static partial class AppTransactionUnitSearchFieldMappingConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitSearchFieldMappingEntity To  AppTransactionUnitSearchFieldMappingDto
        /// </summary>
        public static AppTransactionUnitSearchFieldMappingDto ConvertEntityToDto(AppTransactionUnitSearchFieldMappingEntity aAppTransactionUnitSearchFieldMappingEntity)
        {        
    		AppTransactionUnitSearchFieldMappingDto aAppTransactionUnitSearchFieldMappingDto = new AppTransactionUnitSearchFieldMappingDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitSearchFieldMappingEntity, aAppTransactionUnitSearchFieldMappingDto);          
			return aAppTransactionUnitSearchFieldMappingDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitSearchFieldMappingEntity To  AppTransactionUnitSearchFieldMappingExDto
        /// </summary>
        public static AppTransactionUnitSearchFieldMappingExDto ConvertEntityToExDto(AppTransactionUnitSearchFieldMappingEntity aAppTransactionUnitSearchFieldMappingEntity)
        {        
    		AppTransactionUnitSearchFieldMappingExDto aAppTransactionUnitSearchFieldMappingExDto = new AppTransactionUnitSearchFieldMappingExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitSearchFieldMappingEntity, aAppTransactionUnitSearchFieldMappingExDto);
			
			
			
            return aAppTransactionUnitSearchFieldMappingExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitSearchFieldMappingEntity To  AppTransactionUnitSearchFieldMappingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitSearchFieldMappingEntity aAppTransactionUnitSearchFieldMappingEntity,AppTransactionUnitSearchFieldMappingDto aAppTransactionUnitSearchFieldMappingDto)
        {        
    		
           // aAppTransactionUnitSearchFieldMappingDto.StopChangeTracking();
 			aAppTransactionUnitSearchFieldMappingDto.Id = aAppTransactionUnitSearchFieldMappingEntity.TransactionUnitSearchFieldMappingId;
 			aAppTransactionUnitSearchFieldMappingDto.TransactionUnitLinkedSearchId = aAppTransactionUnitSearchFieldMappingEntity.TransactionUnitLinkedSearchId;
 			aAppTransactionUnitSearchFieldMappingDto.TransactionFieldId = aAppTransactionUnitSearchFieldMappingEntity.TransactionFieldId;
 			aAppTransactionUnitSearchFieldMappingDto.SearchFieldId = aAppTransactionUnitSearchFieldMappingEntity.SearchFieldId;
 			aAppTransactionUnitSearchFieldMappingDto.AppCreatedById = aAppTransactionUnitSearchFieldMappingEntity.AppCreatedById;
 			aAppTransactionUnitSearchFieldMappingDto.AppCreatedDate = aAppTransactionUnitSearchFieldMappingEntity.AppCreatedDate;
 			aAppTransactionUnitSearchFieldMappingDto.AppModifiedDate = aAppTransactionUnitSearchFieldMappingEntity.AppModifiedDate;
 			aAppTransactionUnitSearchFieldMappingDto.AppModifiedById = aAppTransactionUnitSearchFieldMappingEntity.AppModifiedById;
 			aAppTransactionUnitSearchFieldMappingDto.AppCreatedByCompanyId = aAppTransactionUnitSearchFieldMappingEntity.AppCreatedByCompanyId;
 			aAppTransactionUnitSearchFieldMappingDto.TargetUnitId = aAppTransactionUnitSearchFieldMappingEntity.TargetUnitId;
 			aAppTransactionUnitSearchFieldMappingDto.TargetTransactionFieldDbname = aAppTransactionUnitSearchFieldMappingEntity.TargetTransactionFieldDbname;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitSearchFieldMappingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitSearchFieldMappingEntity.AppCreatedDate);
                aAppTransactionUnitSearchFieldMappingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitSearchFieldMappingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitSearchFieldMappingEntity, aAppTransactionUnitSearchFieldMappingDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitSearchFieldMappingDto Properties to   AppTransactionUnitSearchFieldMappingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitSearchFieldMappingEntity aAppTransactionUnitSearchFieldMappingEntity,AppTransactionUnitSearchFieldMappingDto aAppTransactionUnitSearchFieldMappingDto)
        {        
 
      			aAppTransactionUnitSearchFieldMappingEntity.TransactionUnitLinkedSearchId = aAppTransactionUnitSearchFieldMappingDto.TransactionUnitLinkedSearchId;
      			aAppTransactionUnitSearchFieldMappingEntity.TransactionFieldId = aAppTransactionUnitSearchFieldMappingDto.TransactionFieldId;
      			aAppTransactionUnitSearchFieldMappingEntity.SearchFieldId = aAppTransactionUnitSearchFieldMappingDto.SearchFieldId;
 
  
   
    
      			aAppTransactionUnitSearchFieldMappingEntity.AppCreatedByCompanyId = aAppTransactionUnitSearchFieldMappingDto.AppCreatedByCompanyId;
      			aAppTransactionUnitSearchFieldMappingEntity.TargetUnitId = aAppTransactionUnitSearchFieldMappingDto.TargetUnitId;
      			aAppTransactionUnitSearchFieldMappingEntity.TargetTransactionFieldDbname = aAppTransactionUnitSearchFieldMappingDto.TargetTransactionFieldDbname;
			
			if(aAppTransactionUnitSearchFieldMappingDto.Id == null)
			{
				aAppTransactionUnitSearchFieldMappingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitSearchFieldMappingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitSearchFieldMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitSearchFieldMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitSearchFieldMappingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitSearchFieldMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitSearchFieldMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitSearchFieldMappingEntity, aAppTransactionUnitSearchFieldMappingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitSearchFieldMappingEntity aAppTransactionUnitSearchFieldMappingEntity,AppTransactionUnitSearchFieldMappingDto aAppTransactionUnitSearchFieldMappingDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitSearchFieldMappingEntity aAppTransactionUnitSearchFieldMappingEntity,AppTransactionUnitSearchFieldMappingDto aAppTransactionUnitSearchFieldMappingDto);
		
   
       
    }
}

 