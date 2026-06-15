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
    /// Convert Properties between  AppTransactionItemEntity and  AppTransactionItemDto
    /// </summary>
    public static partial class AppTransactionItemConverter 
    {
         /// <summary>
        ///  Convert AppTransactionItemEntity To  AppTransactionItemDto
        /// </summary>
        public static AppTransactionItemDto ConvertEntityToDto(AppTransactionItemEntity aAppTransactionItemEntity)
        {        
    		AppTransactionItemDto aAppTransactionItemDto = new AppTransactionItemDto();
    		CopyEntityPropertyToDto( aAppTransactionItemEntity, aAppTransactionItemDto);          
			return aAppTransactionItemDto;
        }
		 /// <summary>
        ///  Convert AppTransactionItemEntity To  AppTransactionItemExDto
        /// </summary>
        public static AppTransactionItemExDto ConvertEntityToExDto(AppTransactionItemEntity aAppTransactionItemEntity)
        {        
    		AppTransactionItemExDto aAppTransactionItemExDto = new AppTransactionItemExDto();
			CopyEntityPropertyToDto( aAppTransactionItemEntity, aAppTransactionItemExDto);
			
			
			
            return aAppTransactionItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionItemEntity To  AppTransactionItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionItemEntity aAppTransactionItemEntity,AppTransactionItemDto aAppTransactionItemDto)
        {        
    		
           // aAppTransactionItemDto.StopChangeTracking();
 			aAppTransactionItemDto.Id = aAppTransactionItemEntity.AppTransactionItemId;
 			aAppTransactionItemDto.TransactionId = aAppTransactionItemEntity.TransactionId;
 			aAppTransactionItemDto.TransactionItemName = aAppTransactionItemEntity.TransactionItemName;
 			aAppTransactionItemDto.Description = aAppTransactionItemEntity.Description;
 			aAppTransactionItemDto.CategoryId = aAppTransactionItemEntity.CategoryId;
 			aAppTransactionItemDto.Tag = aAppTransactionItemEntity.Tag;
 			aAppTransactionItemDto.AppCreatedById = aAppTransactionItemEntity.AppCreatedById;
 			aAppTransactionItemDto.AppCreatedDate = aAppTransactionItemEntity.AppCreatedDate;
 			aAppTransactionItemDto.AppModifiedDate = aAppTransactionItemEntity.AppModifiedDate;
 			aAppTransactionItemDto.AppModifiedById = aAppTransactionItemEntity.AppModifiedById;
 			aAppTransactionItemDto.AppCreatedByCompanyId = aAppTransactionItemEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionItemEntity.AppCreatedDate);
                aAppTransactionItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionItemEntity, aAppTransactionItemDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionItemDto Properties to   AppTransactionItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionItemEntity aAppTransactionItemEntity,AppTransactionItemDto aAppTransactionItemDto)
        {        
 
      			aAppTransactionItemEntity.TransactionId = aAppTransactionItemDto.TransactionId;
      			aAppTransactionItemEntity.TransactionItemName = aAppTransactionItemDto.TransactionItemName;
      			aAppTransactionItemEntity.Description = aAppTransactionItemDto.Description;
      			aAppTransactionItemEntity.CategoryId = aAppTransactionItemDto.CategoryId;
      			aAppTransactionItemEntity.Tag = aAppTransactionItemDto.Tag;
 
  
   
    
      			aAppTransactionItemEntity.AppCreatedByCompanyId = aAppTransactionItemDto.AppCreatedByCompanyId;
			
			if(aAppTransactionItemDto.Id == null)
			{
				aAppTransactionItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionItemEntity, aAppTransactionItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionItemEntity aAppTransactionItemEntity,AppTransactionItemDto aAppTransactionItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionItemEntity aAppTransactionItemEntity,AppTransactionItemDto aAppTransactionItemDto);
		
   
       
    }
}

 