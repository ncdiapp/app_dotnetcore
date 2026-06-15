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
    /// Convert Properties between  AppTransactionUnitSearchViewFieldMappingEntity and  AppTransactionUnitSearchViewFieldMappingDto
    /// </summary>
    public static partial class AppTransactionUnitSearchViewFieldMappingConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitSearchViewFieldMappingEntity To  AppTransactionUnitSearchViewFieldMappingDto
        /// </summary>
        public static AppTransactionUnitSearchViewFieldMappingDto ConvertEntityToDto(AppTransactionUnitSearchViewFieldMappingEntity aAppTransactionUnitSearchViewFieldMappingEntity)
        {        
    		AppTransactionUnitSearchViewFieldMappingDto aAppTransactionUnitSearchViewFieldMappingDto = new AppTransactionUnitSearchViewFieldMappingDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitSearchViewFieldMappingEntity, aAppTransactionUnitSearchViewFieldMappingDto);          
			return aAppTransactionUnitSearchViewFieldMappingDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitSearchViewFieldMappingEntity To  AppTransactionUnitSearchViewFieldMappingExDto
        /// </summary>
        public static AppTransactionUnitSearchViewFieldMappingExDto ConvertEntityToExDto(AppTransactionUnitSearchViewFieldMappingEntity aAppTransactionUnitSearchViewFieldMappingEntity)
        {        
    		AppTransactionUnitSearchViewFieldMappingExDto aAppTransactionUnitSearchViewFieldMappingExDto = new AppTransactionUnitSearchViewFieldMappingExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitSearchViewFieldMappingEntity, aAppTransactionUnitSearchViewFieldMappingExDto);
			
			
			
            return aAppTransactionUnitSearchViewFieldMappingExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitSearchViewFieldMappingEntity To  AppTransactionUnitSearchViewFieldMappingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitSearchViewFieldMappingEntity aAppTransactionUnitSearchViewFieldMappingEntity,AppTransactionUnitSearchViewFieldMappingDto aAppTransactionUnitSearchViewFieldMappingDto)
        {        
    		
           // aAppTransactionUnitSearchViewFieldMappingDto.StopChangeTracking();
 			aAppTransactionUnitSearchViewFieldMappingDto.Id = aAppTransactionUnitSearchViewFieldMappingEntity.TransactionUnitSearchViewFieldMappingId;
 			aAppTransactionUnitSearchViewFieldMappingDto.TransactionUnitLinkedSearchId = aAppTransactionUnitSearchViewFieldMappingEntity.TransactionUnitLinkedSearchId;
 			aAppTransactionUnitSearchViewFieldMappingDto.TransactionFieldId = aAppTransactionUnitSearchViewFieldMappingEntity.TransactionFieldId;
 			aAppTransactionUnitSearchViewFieldMappingDto.SearchViewFieldId = aAppTransactionUnitSearchViewFieldMappingEntity.SearchViewFieldId;
 			aAppTransactionUnitSearchViewFieldMappingDto.ExternalAppFieldMappingCode = aAppTransactionUnitSearchViewFieldMappingEntity.ExternalAppFieldMappingCode;
 			aAppTransactionUnitSearchViewFieldMappingDto.IsUnique = aAppTransactionUnitSearchViewFieldMappingEntity.IsUnique;
 			aAppTransactionUnitSearchViewFieldMappingDto.AppCreatedById = aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedById;
 			aAppTransactionUnitSearchViewFieldMappingDto.AppCreatedDate = aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedDate;
 			aAppTransactionUnitSearchViewFieldMappingDto.AppModifiedDate = aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedDate;
 			aAppTransactionUnitSearchViewFieldMappingDto.AppModifiedById = aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedById;
 			aAppTransactionUnitSearchViewFieldMappingDto.AppCreatedByCompanyId = aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedByCompanyId;
 			aAppTransactionUnitSearchViewFieldMappingDto.TargetUnitId = aAppTransactionUnitSearchViewFieldMappingEntity.TargetUnitId;
 			aAppTransactionUnitSearchViewFieldMappingDto.TargetTransactionFieldDbname = aAppTransactionUnitSearchViewFieldMappingEntity.TargetTransactionFieldDbname;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitSearchViewFieldMappingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedDate);
                aAppTransactionUnitSearchViewFieldMappingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitSearchViewFieldMappingEntity, aAppTransactionUnitSearchViewFieldMappingDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitSearchViewFieldMappingDto Properties to   AppTransactionUnitSearchViewFieldMappingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitSearchViewFieldMappingEntity aAppTransactionUnitSearchViewFieldMappingEntity,AppTransactionUnitSearchViewFieldMappingDto aAppTransactionUnitSearchViewFieldMappingDto)
        {        
 
      			aAppTransactionUnitSearchViewFieldMappingEntity.TransactionUnitLinkedSearchId = aAppTransactionUnitSearchViewFieldMappingDto.TransactionUnitLinkedSearchId;
      			aAppTransactionUnitSearchViewFieldMappingEntity.TransactionFieldId = aAppTransactionUnitSearchViewFieldMappingDto.TransactionFieldId;
      			aAppTransactionUnitSearchViewFieldMappingEntity.SearchViewFieldId = aAppTransactionUnitSearchViewFieldMappingDto.SearchViewFieldId;
      			aAppTransactionUnitSearchViewFieldMappingEntity.ExternalAppFieldMappingCode = aAppTransactionUnitSearchViewFieldMappingDto.ExternalAppFieldMappingCode;
      			aAppTransactionUnitSearchViewFieldMappingEntity.IsUnique = aAppTransactionUnitSearchViewFieldMappingDto.IsUnique;
 
  
   
    
      			aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedByCompanyId = aAppTransactionUnitSearchViewFieldMappingDto.AppCreatedByCompanyId;
      			aAppTransactionUnitSearchViewFieldMappingEntity.TargetUnitId = aAppTransactionUnitSearchViewFieldMappingDto.TargetUnitId;
      			aAppTransactionUnitSearchViewFieldMappingEntity.TargetTransactionFieldDbname = aAppTransactionUnitSearchViewFieldMappingDto.TargetTransactionFieldDbname;
			
			if(aAppTransactionUnitSearchViewFieldMappingDto.Id == null)
			{
				aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitSearchViewFieldMappingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitSearchViewFieldMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitSearchViewFieldMappingEntity, aAppTransactionUnitSearchViewFieldMappingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitSearchViewFieldMappingEntity aAppTransactionUnitSearchViewFieldMappingEntity,AppTransactionUnitSearchViewFieldMappingDto aAppTransactionUnitSearchViewFieldMappingDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitSearchViewFieldMappingEntity aAppTransactionUnitSearchViewFieldMappingEntity,AppTransactionUnitSearchViewFieldMappingDto aAppTransactionUnitSearchViewFieldMappingDto);
		
   
       
    }
}

 