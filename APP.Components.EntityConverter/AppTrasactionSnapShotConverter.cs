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
    /// Convert Properties between  AppTrasactionSnapShotEntity and  AppTrasactionSnapShotDto
    /// </summary>
    public static partial class AppTrasactionSnapShotConverter 
    {
         /// <summary>
        ///  Convert AppTrasactionSnapShotEntity To  AppTrasactionSnapShotDto
        /// </summary>
        public static AppTrasactionSnapShotDto ConvertEntityToDto(AppTrasactionSnapShotEntity aAppTrasactionSnapShotEntity)
        {        
    		AppTrasactionSnapShotDto aAppTrasactionSnapShotDto = new AppTrasactionSnapShotDto();
    		CopyEntityPropertyToDto( aAppTrasactionSnapShotEntity, aAppTrasactionSnapShotDto);          
			return aAppTrasactionSnapShotDto;
        }
		 /// <summary>
        ///  Convert AppTrasactionSnapShotEntity To  AppTrasactionSnapShotExDto
        /// </summary>
        public static AppTrasactionSnapShotExDto ConvertEntityToExDto(AppTrasactionSnapShotEntity aAppTrasactionSnapShotEntity)
        {        
    		AppTrasactionSnapShotExDto aAppTrasactionSnapShotExDto = new AppTrasactionSnapShotExDto();
			CopyEntityPropertyToDto( aAppTrasactionSnapShotEntity, aAppTrasactionSnapShotExDto);
			
			
			
            return aAppTrasactionSnapShotExDto;
        }
		
		 /// <summary>
        ///  Convert AppTrasactionSnapShotEntity To  AppTrasactionSnapShotDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTrasactionSnapShotEntity aAppTrasactionSnapShotEntity,AppTrasactionSnapShotDto aAppTrasactionSnapShotDto)
        {        
    		
           // aAppTrasactionSnapShotDto.StopChangeTracking();
 			aAppTrasactionSnapShotDto.Id = aAppTrasactionSnapShotEntity.TrasactionSnapShotId;
 			aAppTrasactionSnapShotDto.TransactionId = aAppTrasactionSnapShotEntity.TransactionId;
 			aAppTrasactionSnapShotDto.RootValueId = aAppTrasactionSnapShotEntity.RootValueId;
 			aAppTrasactionSnapShotDto.TransactionFieldId = aAppTrasactionSnapShotEntity.TransactionFieldId;
 			aAppTrasactionSnapShotDto.BatchNoId = aAppTrasactionSnapShotEntity.BatchNoId;
 			aAppTrasactionSnapShotDto.UnitId = aAppTrasactionSnapShotEntity.UnitId;
 			aAppTrasactionSnapShotDto.ChildUnitRowValueId = aAppTrasactionSnapShotEntity.ChildUnitRowValueId;
 			aAppTrasactionSnapShotDto.GrandChildUnitRowValueId = aAppTrasactionSnapShotEntity.GrandChildUnitRowValueId;
 			aAppTrasactionSnapShotDto.SnapShotValue = aAppTrasactionSnapShotEntity.SnapShotValue;
 			aAppTrasactionSnapShotDto.AppCreatedById = aAppTrasactionSnapShotEntity.AppCreatedById;
 			aAppTrasactionSnapShotDto.AppCreatedDate = aAppTrasactionSnapShotEntity.AppCreatedDate;
 			aAppTrasactionSnapShotDto.AppModifiedDate = aAppTrasactionSnapShotEntity.AppModifiedDate;
 			aAppTrasactionSnapShotDto.AppModifiedById = aAppTrasactionSnapShotEntity.AppModifiedById;
 			aAppTrasactionSnapShotDto.AppCreatedByCompanyId = aAppTrasactionSnapShotEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTrasactionSnapShotDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTrasactionSnapShotEntity.AppCreatedDate);
                aAppTrasactionSnapShotDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTrasactionSnapShotEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTrasactionSnapShotEntity, aAppTrasactionSnapShotDto);
		}
		
		 /// <summary>
        ///  Copy AppTrasactionSnapShotDto Properties to   AppTrasactionSnapShotEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTrasactionSnapShotEntity aAppTrasactionSnapShotEntity,AppTrasactionSnapShotDto aAppTrasactionSnapShotDto)
        {        
 
      			aAppTrasactionSnapShotEntity.TransactionId = aAppTrasactionSnapShotDto.TransactionId;
      			aAppTrasactionSnapShotEntity.RootValueId = aAppTrasactionSnapShotDto.RootValueId;
      			aAppTrasactionSnapShotEntity.TransactionFieldId = aAppTrasactionSnapShotDto.TransactionFieldId;
      			aAppTrasactionSnapShotEntity.BatchNoId = aAppTrasactionSnapShotDto.BatchNoId;
      			aAppTrasactionSnapShotEntity.UnitId = aAppTrasactionSnapShotDto.UnitId;
      			aAppTrasactionSnapShotEntity.ChildUnitRowValueId = aAppTrasactionSnapShotDto.ChildUnitRowValueId;
      			aAppTrasactionSnapShotEntity.GrandChildUnitRowValueId = aAppTrasactionSnapShotDto.GrandChildUnitRowValueId;
      			aAppTrasactionSnapShotEntity.SnapShotValue = aAppTrasactionSnapShotDto.SnapShotValue;
 
  
   
    
      			aAppTrasactionSnapShotEntity.AppCreatedByCompanyId = aAppTrasactionSnapShotDto.AppCreatedByCompanyId;
			
			if(aAppTrasactionSnapShotDto.Id == null)
			{
				aAppTrasactionSnapShotEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTrasactionSnapShotEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTrasactionSnapShotEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTrasactionSnapShotEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTrasactionSnapShotEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTrasactionSnapShotEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTrasactionSnapShotEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTrasactionSnapShotEntity, aAppTrasactionSnapShotDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTrasactionSnapShotEntity aAppTrasactionSnapShotEntity,AppTrasactionSnapShotDto aAppTrasactionSnapShotDto);
		
		static partial void OnCopyDtoToEntityDone(AppTrasactionSnapShotEntity aAppTrasactionSnapShotEntity,AppTrasactionSnapShotDto aAppTrasactionSnapShotDto);
		
   
       
    }
}

 