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
    /// Convert Properties between  AppTransactionSaveAsMappingEntity and  AppTransactionSaveAsMappingDto
    /// </summary>
    public static partial class AppTransactionSaveAsMappingConverter 
    {
         /// <summary>
        ///  Convert AppTransactionSaveAsMappingEntity To  AppTransactionSaveAsMappingDto
        /// </summary>
        public static AppTransactionSaveAsMappingDto ConvertEntityToDto(AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity)
        {        
    		AppTransactionSaveAsMappingDto aAppTransactionSaveAsMappingDto = new AppTransactionSaveAsMappingDto();
    		CopyEntityPropertyToDto( aAppTransactionSaveAsMappingEntity, aAppTransactionSaveAsMappingDto);          
			return aAppTransactionSaveAsMappingDto;
        }
		 /// <summary>
        ///  Convert AppTransactionSaveAsMappingEntity To  AppTransactionSaveAsMappingExDto
        /// </summary>
        public static AppTransactionSaveAsMappingExDto ConvertEntityToExDto(AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity)
        {        
    		AppTransactionSaveAsMappingExDto aAppTransactionSaveAsMappingExDto = new AppTransactionSaveAsMappingExDto();
			CopyEntityPropertyToDto( aAppTransactionSaveAsMappingEntity, aAppTransactionSaveAsMappingExDto);
			
			
			
            return aAppTransactionSaveAsMappingExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionSaveAsMappingEntity To  AppTransactionSaveAsMappingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity,AppTransactionSaveAsMappingDto aAppTransactionSaveAsMappingDto)
        {        
    		
           // aAppTransactionSaveAsMappingDto.StopChangeTracking();
 			aAppTransactionSaveAsMappingDto.Id = aAppTransactionSaveAsMappingEntity.MappingId;
 			aAppTransactionSaveAsMappingDto.DataTransferSettingId = aAppTransactionSaveAsMappingEntity.DataTransferSettingId;
 			aAppTransactionSaveAsMappingDto.Name = aAppTransactionSaveAsMappingEntity.Name;
 			aAppTransactionSaveAsMappingDto.TransactionId = aAppTransactionSaveAsMappingEntity.TransactionId;
 			aAppTransactionSaveAsMappingDto.MappingUnitId = aAppTransactionSaveAsMappingEntity.MappingUnitId;
 			aAppTransactionSaveAsMappingDto.SourceFiledId = aAppTransactionSaveAsMappingEntity.SourceFiledId;
 			aAppTransactionSaveAsMappingDto.TargetFiledId = aAppTransactionSaveAsMappingEntity.TargetFiledId;
 			aAppTransactionSaveAsMappingDto.IsBlankTargetField = aAppTransactionSaveAsMappingEntity.IsBlankTargetField;
 			aAppTransactionSaveAsMappingDto.AppCreatedById = aAppTransactionSaveAsMappingEntity.AppCreatedById;
 			aAppTransactionSaveAsMappingDto.AppCreatedDate = aAppTransactionSaveAsMappingEntity.AppCreatedDate;
 			aAppTransactionSaveAsMappingDto.AppModifiedDate = aAppTransactionSaveAsMappingEntity.AppModifiedDate;
 			aAppTransactionSaveAsMappingDto.AppModifiedById = aAppTransactionSaveAsMappingEntity.AppModifiedById;
 			aAppTransactionSaveAsMappingDto.AppCreatedByCompanyId = aAppTransactionSaveAsMappingEntity.AppCreatedByCompanyId;
 			aAppTransactionSaveAsMappingDto.JsonPropertyPathName = aAppTransactionSaveAsMappingEntity.JsonPropertyPathName;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionSaveAsMappingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionSaveAsMappingEntity.AppCreatedDate);
                aAppTransactionSaveAsMappingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionSaveAsMappingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionSaveAsMappingEntity, aAppTransactionSaveAsMappingDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionSaveAsMappingDto Properties to   AppTransactionSaveAsMappingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity,AppTransactionSaveAsMappingDto aAppTransactionSaveAsMappingDto)
        {        
 
      			aAppTransactionSaveAsMappingEntity.DataTransferSettingId = aAppTransactionSaveAsMappingDto.DataTransferSettingId;
      			aAppTransactionSaveAsMappingEntity.Name = aAppTransactionSaveAsMappingDto.Name;
      			aAppTransactionSaveAsMappingEntity.TransactionId = aAppTransactionSaveAsMappingDto.TransactionId;
      			aAppTransactionSaveAsMappingEntity.MappingUnitId = aAppTransactionSaveAsMappingDto.MappingUnitId;
      			aAppTransactionSaveAsMappingEntity.SourceFiledId = aAppTransactionSaveAsMappingDto.SourceFiledId;
      			aAppTransactionSaveAsMappingEntity.TargetFiledId = aAppTransactionSaveAsMappingDto.TargetFiledId;
      			aAppTransactionSaveAsMappingEntity.IsBlankTargetField = aAppTransactionSaveAsMappingDto.IsBlankTargetField;
 
  
   
    
      			aAppTransactionSaveAsMappingEntity.AppCreatedByCompanyId = aAppTransactionSaveAsMappingDto.AppCreatedByCompanyId;
      			aAppTransactionSaveAsMappingEntity.JsonPropertyPathName = aAppTransactionSaveAsMappingDto.JsonPropertyPathName;
			
			if(aAppTransactionSaveAsMappingDto.Id == null)
			{
				aAppTransactionSaveAsMappingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionSaveAsMappingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionSaveAsMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionSaveAsMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionSaveAsMappingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionSaveAsMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionSaveAsMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionSaveAsMappingEntity, aAppTransactionSaveAsMappingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity,AppTransactionSaveAsMappingDto aAppTransactionSaveAsMappingDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionSaveAsMappingEntity aAppTransactionSaveAsMappingEntity,AppTransactionSaveAsMappingDto aAppTransactionSaveAsMappingDto);
		
   
       
    }
}

 