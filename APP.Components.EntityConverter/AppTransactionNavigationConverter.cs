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
    /// Convert Properties between  AppTransactionNavigationEntity and  AppTransactionNavigationDto
    /// </summary>
    public static partial class AppTransactionNavigationConverter 
    {
         /// <summary>
        ///  Convert AppTransactionNavigationEntity To  AppTransactionNavigationDto
        /// </summary>
        public static AppTransactionNavigationDto ConvertEntityToDto(AppTransactionNavigationEntity aAppTransactionNavigationEntity)
        {        
    		AppTransactionNavigationDto aAppTransactionNavigationDto = new AppTransactionNavigationDto();
    		CopyEntityPropertyToDto( aAppTransactionNavigationEntity, aAppTransactionNavigationDto);          
			return aAppTransactionNavigationDto;
        }
		 /// <summary>
        ///  Convert AppTransactionNavigationEntity To  AppTransactionNavigationExDto
        /// </summary>
        public static AppTransactionNavigationExDto ConvertEntityToExDto(AppTransactionNavigationEntity aAppTransactionNavigationEntity)
        {        
    		AppTransactionNavigationExDto aAppTransactionNavigationExDto = new AppTransactionNavigationExDto();
			CopyEntityPropertyToDto( aAppTransactionNavigationEntity, aAppTransactionNavigationExDto);
			
			
			
            return aAppTransactionNavigationExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionNavigationEntity To  AppTransactionNavigationDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionNavigationEntity aAppTransactionNavigationEntity,AppTransactionNavigationDto aAppTransactionNavigationDto)
        {        
    		
           // aAppTransactionNavigationDto.StopChangeTracking();
 			aAppTransactionNavigationDto.Id = aAppTransactionNavigationEntity.TransNavigationId;
 			aAppTransactionNavigationDto.TransactionId = aAppTransactionNavigationEntity.TransactionId;
 			aAppTransactionNavigationDto.QuickSearchId = aAppTransactionNavigationEntity.QuickSearchId;
 			aAppTransactionNavigationDto.FolderViewId = aAppTransactionNavigationEntity.FolderViewId;
 			aAppTransactionNavigationDto.IsDefaultView = aAppTransactionNavigationEntity.IsDefaultView;
 			aAppTransactionNavigationDto.AppCreatedById = aAppTransactionNavigationEntity.AppCreatedById;
 			aAppTransactionNavigationDto.AppCreatedDate = aAppTransactionNavigationEntity.AppCreatedDate;
 			aAppTransactionNavigationDto.AppModifiedDate = aAppTransactionNavigationEntity.AppModifiedDate;
 			aAppTransactionNavigationDto.AppModifiedById = aAppTransactionNavigationEntity.AppModifiedById;
 			aAppTransactionNavigationDto.AppCreatedByCompanyId = aAppTransactionNavigationEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionNavigationDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionNavigationEntity.AppCreatedDate);
                aAppTransactionNavigationDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionNavigationEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionNavigationEntity, aAppTransactionNavigationDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionNavigationDto Properties to   AppTransactionNavigationEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionNavigationEntity aAppTransactionNavigationEntity,AppTransactionNavigationDto aAppTransactionNavigationDto)
        {        
 
      			aAppTransactionNavigationEntity.TransactionId = aAppTransactionNavigationDto.TransactionId;
      			aAppTransactionNavigationEntity.QuickSearchId = aAppTransactionNavigationDto.QuickSearchId;
      			aAppTransactionNavigationEntity.FolderViewId = aAppTransactionNavigationDto.FolderViewId;
      			aAppTransactionNavigationEntity.IsDefaultView = aAppTransactionNavigationDto.IsDefaultView;
 
  
   
    
      			aAppTransactionNavigationEntity.AppCreatedByCompanyId = aAppTransactionNavigationDto.AppCreatedByCompanyId;
			
			if(aAppTransactionNavigationDto.Id == null)
			{
				aAppTransactionNavigationEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionNavigationEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionNavigationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionNavigationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionNavigationEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionNavigationEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionNavigationEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionNavigationEntity, aAppTransactionNavigationDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionNavigationEntity aAppTransactionNavigationEntity,AppTransactionNavigationDto aAppTransactionNavigationDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionNavigationEntity aAppTransactionNavigationEntity,AppTransactionNavigationDto aAppTransactionNavigationDto);
		
   
       
    }
}

 