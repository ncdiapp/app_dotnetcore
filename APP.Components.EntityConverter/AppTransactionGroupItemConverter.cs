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
    /// Convert Properties between  AppTransactionGroupItemEntity and  AppTransactionGroupItemDto
    /// </summary>
    public static partial class AppTransactionGroupItemConverter 
    {
         /// <summary>
        ///  Convert AppTransactionGroupItemEntity To  AppTransactionGroupItemDto
        /// </summary>
        public static AppTransactionGroupItemDto ConvertEntityToDto(AppTransactionGroupItemEntity aAppTransactionGroupItemEntity)
        {        
    		AppTransactionGroupItemDto aAppTransactionGroupItemDto = new AppTransactionGroupItemDto();
    		CopyEntityPropertyToDto( aAppTransactionGroupItemEntity, aAppTransactionGroupItemDto);          
			return aAppTransactionGroupItemDto;
        }
		 /// <summary>
        ///  Convert AppTransactionGroupItemEntity To  AppTransactionGroupItemExDto
        /// </summary>
        public static AppTransactionGroupItemExDto ConvertEntityToExDto(AppTransactionGroupItemEntity aAppTransactionGroupItemEntity)
        {        
    		AppTransactionGroupItemExDto aAppTransactionGroupItemExDto = new AppTransactionGroupItemExDto();
			CopyEntityPropertyToDto( aAppTransactionGroupItemEntity, aAppTransactionGroupItemExDto);
			
			
			
            return aAppTransactionGroupItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionGroupItemEntity To  AppTransactionGroupItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionGroupItemEntity aAppTransactionGroupItemEntity,AppTransactionGroupItemDto aAppTransactionGroupItemDto)
        {        
    		
           // aAppTransactionGroupItemDto.StopChangeTracking();
 			aAppTransactionGroupItemDto.Id = aAppTransactionGroupItemEntity.GroupItemId;
 			aAppTransactionGroupItemDto.TransactionGroupId = aAppTransactionGroupItemEntity.TransactionGroupId;
 			aAppTransactionGroupItemDto.TransactionId = aAppTransactionGroupItemEntity.TransactionId;
 			aAppTransactionGroupItemDto.TransactionCaculationFlowOrder = aAppTransactionGroupItemEntity.TransactionCaculationFlowOrder;
 			aAppTransactionGroupItemDto.TransactionLayoutOrder = aAppTransactionGroupItemEntity.TransactionLayoutOrder;
 			aAppTransactionGroupItemDto.IsCrossGroupSharedHeader = aAppTransactionGroupItemEntity.IsCrossGroupSharedHeader;
 			aAppTransactionGroupItemDto.IsGroupSharedHeader = aAppTransactionGroupItemEntity.IsGroupSharedHeader;
 			aAppTransactionGroupItemDto.AppCreatedById = aAppTransactionGroupItemEntity.AppCreatedById;
 			aAppTransactionGroupItemDto.AppCreatedDate = aAppTransactionGroupItemEntity.AppCreatedDate;
 			aAppTransactionGroupItemDto.AppModifiedDate = aAppTransactionGroupItemEntity.AppModifiedDate;
 			aAppTransactionGroupItemDto.AppModifiedById = aAppTransactionGroupItemEntity.AppModifiedById;
 			aAppTransactionGroupItemDto.AppCreatedByCompanyId = aAppTransactionGroupItemEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionGroupItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionGroupItemEntity.AppCreatedDate);
                aAppTransactionGroupItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionGroupItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionGroupItemEntity, aAppTransactionGroupItemDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionGroupItemDto Properties to   AppTransactionGroupItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionGroupItemEntity aAppTransactionGroupItemEntity,AppTransactionGroupItemDto aAppTransactionGroupItemDto)
        {        
 
      			aAppTransactionGroupItemEntity.TransactionGroupId = aAppTransactionGroupItemDto.TransactionGroupId;
      			aAppTransactionGroupItemEntity.TransactionId = aAppTransactionGroupItemDto.TransactionId;
      			aAppTransactionGroupItemEntity.TransactionCaculationFlowOrder = aAppTransactionGroupItemDto.TransactionCaculationFlowOrder;
      			aAppTransactionGroupItemEntity.TransactionLayoutOrder = aAppTransactionGroupItemDto.TransactionLayoutOrder;
      			aAppTransactionGroupItemEntity.IsCrossGroupSharedHeader = aAppTransactionGroupItemDto.IsCrossGroupSharedHeader;
      			aAppTransactionGroupItemEntity.IsGroupSharedHeader = aAppTransactionGroupItemDto.IsGroupSharedHeader;
 
  
   
    
      			aAppTransactionGroupItemEntity.AppCreatedByCompanyId = aAppTransactionGroupItemDto.AppCreatedByCompanyId;
			
			if(aAppTransactionGroupItemDto.Id == null)
			{
				aAppTransactionGroupItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionGroupItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionGroupItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionGroupItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionGroupItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionGroupItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionGroupItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionGroupItemEntity, aAppTransactionGroupItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionGroupItemEntity aAppTransactionGroupItemEntity,AppTransactionGroupItemDto aAppTransactionGroupItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionGroupItemEntity aAppTransactionGroupItemEntity,AppTransactionGroupItemDto aAppTransactionGroupItemDto);
		
   
       
    }
}

 