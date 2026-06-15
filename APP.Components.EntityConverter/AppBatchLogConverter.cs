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
    /// Convert Properties between  AppBatchLogEntity and  AppBatchLogDto
    /// </summary>
    public static partial class AppBatchLogConverter 
    {
         /// <summary>
        ///  Convert AppBatchLogEntity To  AppBatchLogDto
        /// </summary>
        public static AppBatchLogDto ConvertEntityToDto(AppBatchLogEntity aAppBatchLogEntity)
        {        
    		AppBatchLogDto aAppBatchLogDto = new AppBatchLogDto();
    		CopyEntityPropertyToDto( aAppBatchLogEntity, aAppBatchLogDto);          
			return aAppBatchLogDto;
        }
		 /// <summary>
        ///  Convert AppBatchLogEntity To  AppBatchLogExDto
        /// </summary>
        public static AppBatchLogExDto ConvertEntityToExDto(AppBatchLogEntity aAppBatchLogEntity)
        {        
    		AppBatchLogExDto aAppBatchLogExDto = new AppBatchLogExDto();
			CopyEntityPropertyToDto( aAppBatchLogEntity, aAppBatchLogExDto);
			
			
			
            return aAppBatchLogExDto;
        }
		
		 /// <summary>
        ///  Convert AppBatchLogEntity To  AppBatchLogDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppBatchLogEntity aAppBatchLogEntity,AppBatchLogDto aAppBatchLogDto)
        {        
    		
           // aAppBatchLogDto.StopChangeTracking();
 			aAppBatchLogDto.Id = aAppBatchLogEntity.BatchLogId;
 			aAppBatchLogDto.BatchNumber = aAppBatchLogEntity.BatchNumber;
 			aAppBatchLogDto.Description = aAppBatchLogEntity.Description;
 			aAppBatchLogDto.TransactionId = aAppBatchLogEntity.TransactionId;
 			aAppBatchLogDto.TransactionRid = aAppBatchLogEntity.TransactionRid;
 			aAppBatchLogDto.SequenceNumber = aAppBatchLogEntity.SequenceNumber;
 			aAppBatchLogDto.StartTime = aAppBatchLogEntity.StartTime;
 			aAppBatchLogDto.EndTime = aAppBatchLogEntity.EndTime;
 			aAppBatchLogDto.AppCreatedById = aAppBatchLogEntity.AppCreatedById;
 			aAppBatchLogDto.AppCreatedDate = aAppBatchLogEntity.AppCreatedDate;
 			aAppBatchLogDto.AppModifiedDate = aAppBatchLogEntity.AppModifiedDate;
 			aAppBatchLogDto.AppModifiedById = aAppBatchLogEntity.AppModifiedById;
 			aAppBatchLogDto.AppCreatedByCompanyId = aAppBatchLogEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppBatchLogDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBatchLogEntity.AppCreatedDate);
                aAppBatchLogDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppBatchLogEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppBatchLogEntity, aAppBatchLogDto);
		}
		
		 /// <summary>
        ///  Copy AppBatchLogDto Properties to   AppBatchLogEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppBatchLogEntity aAppBatchLogEntity,AppBatchLogDto aAppBatchLogDto)
        {        
 
      			aAppBatchLogEntity.BatchNumber = aAppBatchLogDto.BatchNumber;
      			aAppBatchLogEntity.Description = aAppBatchLogDto.Description;
      			aAppBatchLogEntity.TransactionId = aAppBatchLogDto.TransactionId;
      			aAppBatchLogEntity.TransactionRid = aAppBatchLogDto.TransactionRid;
      			aAppBatchLogEntity.SequenceNumber = aAppBatchLogDto.SequenceNumber;
      			aAppBatchLogEntity.StartTime = aAppBatchLogDto.StartTime;
      			aAppBatchLogEntity.EndTime = aAppBatchLogDto.EndTime;
 
  
   
    
      			aAppBatchLogEntity.AppCreatedByCompanyId = aAppBatchLogDto.AppCreatedByCompanyId;
			
			if(aAppBatchLogDto.Id == null)
			{
				aAppBatchLogEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppBatchLogEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppBatchLogEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBatchLogEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppBatchLogEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppBatchLogEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppBatchLogEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppBatchLogEntity, aAppBatchLogDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppBatchLogEntity aAppBatchLogEntity,AppBatchLogDto aAppBatchLogDto);
		
		static partial void OnCopyDtoToEntityDone(AppBatchLogEntity aAppBatchLogEntity,AppBatchLogDto aAppBatchLogDto);
		
   
       
    }
}

 