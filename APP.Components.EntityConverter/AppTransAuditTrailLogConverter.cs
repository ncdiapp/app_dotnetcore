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
    /// Convert Properties between  AppTransAuditTrailLogEntity and  AppTransAuditTrailLogDto
    /// </summary>
    public static partial class AppTransAuditTrailLogConverter 
    {
         /// <summary>
        ///  Convert AppTransAuditTrailLogEntity To  AppTransAuditTrailLogDto
        /// </summary>
        public static AppTransAuditTrailLogDto ConvertEntityToDto(AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity)
        {        
    		AppTransAuditTrailLogDto aAppTransAuditTrailLogDto = new AppTransAuditTrailLogDto();
    		CopyEntityPropertyToDto( aAppTransAuditTrailLogEntity, aAppTransAuditTrailLogDto);          
			return aAppTransAuditTrailLogDto;
        }
		 /// <summary>
        ///  Convert AppTransAuditTrailLogEntity To  AppTransAuditTrailLogExDto
        /// </summary>
        public static AppTransAuditTrailLogExDto ConvertEntityToExDto(AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity)
        {        
    		AppTransAuditTrailLogExDto aAppTransAuditTrailLogExDto = new AppTransAuditTrailLogExDto();
			CopyEntityPropertyToDto( aAppTransAuditTrailLogEntity, aAppTransAuditTrailLogExDto);
			
			
			
            return aAppTransAuditTrailLogExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransAuditTrailLogEntity To  AppTransAuditTrailLogDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity,AppTransAuditTrailLogDto aAppTransAuditTrailLogDto)
        {        
    		
           // aAppTransAuditTrailLogDto.StopChangeTracking();
 			aAppTransAuditTrailLogDto.Id = aAppTransAuditTrailLogEntity.AuditTrailLogId;
 			aAppTransAuditTrailLogDto.TransactionId = aAppTransAuditTrailLogEntity.TransactionId;
 			aAppTransAuditTrailLogDto.RootValueId = aAppTransAuditTrailLogEntity.RootValueId;
 			aAppTransAuditTrailLogDto.TransactionFieldId = aAppTransAuditTrailLogEntity.TransactionFieldId;
 			aAppTransAuditTrailLogDto.RowIdentityName = aAppTransAuditTrailLogEntity.RowIdentityName;
 			aAppTransAuditTrailLogDto.TraiLogAction = aAppTransAuditTrailLogEntity.TraiLogAction;
 			aAppTransAuditTrailLogDto.ModifiedValueBefor = aAppTransAuditTrailLogEntity.ModifiedValueBefor;
 			aAppTransAuditTrailLogDto.ModifiedValueAfter = aAppTransAuditTrailLogEntity.ModifiedValueAfter;
 			aAppTransAuditTrailLogDto.BatchNoId = aAppTransAuditTrailLogEntity.BatchNoId;
 			aAppTransAuditTrailLogDto.UnitId = aAppTransAuditTrailLogEntity.UnitId;
 			aAppTransAuditTrailLogDto.ChildUnitRowValueId = aAppTransAuditTrailLogEntity.ChildUnitRowValueId;
 			aAppTransAuditTrailLogDto.GrandChildUnitRowValueId = aAppTransAuditTrailLogEntity.GrandChildUnitRowValueId;
 			aAppTransAuditTrailLogDto.AppCreatedById = aAppTransAuditTrailLogEntity.AppCreatedById;
 			aAppTransAuditTrailLogDto.AppCreatedDate = aAppTransAuditTrailLogEntity.AppCreatedDate;
 			aAppTransAuditTrailLogDto.AppModifiedDate = aAppTransAuditTrailLogEntity.AppModifiedDate;
 			aAppTransAuditTrailLogDto.AppModifiedById = aAppTransAuditTrailLogEntity.AppModifiedById;
 			aAppTransAuditTrailLogDto.AppCreatedByCompanyId = aAppTransAuditTrailLogEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransAuditTrailLogDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransAuditTrailLogEntity.AppCreatedDate);
                aAppTransAuditTrailLogDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransAuditTrailLogEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransAuditTrailLogEntity, aAppTransAuditTrailLogDto);
		}
		
		 /// <summary>
        ///  Copy AppTransAuditTrailLogDto Properties to   AppTransAuditTrailLogEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity,AppTransAuditTrailLogDto aAppTransAuditTrailLogDto)
        {        
 
      			aAppTransAuditTrailLogEntity.TransactionId = aAppTransAuditTrailLogDto.TransactionId;
      			aAppTransAuditTrailLogEntity.RootValueId = aAppTransAuditTrailLogDto.RootValueId;
      			aAppTransAuditTrailLogEntity.TransactionFieldId = aAppTransAuditTrailLogDto.TransactionFieldId;
      			aAppTransAuditTrailLogEntity.RowIdentityName = aAppTransAuditTrailLogDto.RowIdentityName;
      			aAppTransAuditTrailLogEntity.TraiLogAction = aAppTransAuditTrailLogDto.TraiLogAction;
      			aAppTransAuditTrailLogEntity.ModifiedValueBefor = aAppTransAuditTrailLogDto.ModifiedValueBefor;
      			aAppTransAuditTrailLogEntity.ModifiedValueAfter = aAppTransAuditTrailLogDto.ModifiedValueAfter;
      			aAppTransAuditTrailLogEntity.BatchNoId = aAppTransAuditTrailLogDto.BatchNoId;
      			aAppTransAuditTrailLogEntity.UnitId = aAppTransAuditTrailLogDto.UnitId;
      			aAppTransAuditTrailLogEntity.ChildUnitRowValueId = aAppTransAuditTrailLogDto.ChildUnitRowValueId;
      			aAppTransAuditTrailLogEntity.GrandChildUnitRowValueId = aAppTransAuditTrailLogDto.GrandChildUnitRowValueId;
 
  
   
    
      			aAppTransAuditTrailLogEntity.AppCreatedByCompanyId = aAppTransAuditTrailLogDto.AppCreatedByCompanyId;
			
			if(aAppTransAuditTrailLogDto.Id == null)
			{
				aAppTransAuditTrailLogEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransAuditTrailLogEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransAuditTrailLogEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransAuditTrailLogEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransAuditTrailLogEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransAuditTrailLogEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransAuditTrailLogEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransAuditTrailLogEntity, aAppTransAuditTrailLogDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity,AppTransAuditTrailLogDto aAppTransAuditTrailLogDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransAuditTrailLogEntity aAppTransAuditTrailLogEntity,AppTransAuditTrailLogDto aAppTransAuditTrailLogDto);
		
   
       
    }
}

 