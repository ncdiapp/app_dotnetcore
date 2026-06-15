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
    /// Convert Properties between  AppTransactionGroupEntity and  AppTransactionGroupDto
    /// </summary>
    public static partial class AppTransactionGroupConverter 
    {
         /// <summary>
        ///  Convert AppTransactionGroupEntity To  AppTransactionGroupDto
        /// </summary>
        public static AppTransactionGroupDto ConvertEntityToDto(AppTransactionGroupEntity aAppTransactionGroupEntity)
        {        
    		AppTransactionGroupDto aAppTransactionGroupDto = new AppTransactionGroupDto();
    		CopyEntityPropertyToDto( aAppTransactionGroupEntity, aAppTransactionGroupDto);          
			return aAppTransactionGroupDto;
        }
		 /// <summary>
        ///  Convert AppTransactionGroupEntity To  AppTransactionGroupExDto
        /// </summary>
        public static AppTransactionGroupExDto ConvertEntityToExDto(AppTransactionGroupEntity aAppTransactionGroupEntity)
        {        
    		AppTransactionGroupExDto aAppTransactionGroupExDto = new AppTransactionGroupExDto();
			CopyEntityPropertyToDto( aAppTransactionGroupEntity, aAppTransactionGroupExDto);
			
			
			
            return aAppTransactionGroupExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionGroupEntity To  AppTransactionGroupDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionGroupEntity aAppTransactionGroupEntity,AppTransactionGroupDto aAppTransactionGroupDto)
        {        
    		
           // aAppTransactionGroupDto.StopChangeTracking();
 			aAppTransactionGroupDto.Id = aAppTransactionGroupEntity.TransactionGroupId;
 			aAppTransactionGroupDto.AssotmentnavigationId = aAppTransactionGroupEntity.AssotmentnavigationId;
 			aAppTransactionGroupDto.GroupName = aAppTransactionGroupEntity.GroupName;
 			aAppTransactionGroupDto.Description = aAppTransactionGroupEntity.Description;
 			aAppTransactionGroupDto.IsDefaultGroup = aAppTransactionGroupEntity.IsDefaultGroup;
 			aAppTransactionGroupDto.GroupSortOrder = aAppTransactionGroupEntity.GroupSortOrder;
 			aAppTransactionGroupDto.EmBuseinssScope = aAppTransactionGroupEntity.EmBuseinssScope;
 			aAppTransactionGroupDto.AppCreatedById = aAppTransactionGroupEntity.AppCreatedById;
 			aAppTransactionGroupDto.AppCreatedDate = aAppTransactionGroupEntity.AppCreatedDate;
 			aAppTransactionGroupDto.AppModifiedDate = aAppTransactionGroupEntity.AppModifiedDate;
 			aAppTransactionGroupDto.AppModifiedById = aAppTransactionGroupEntity.AppModifiedById;
 			aAppTransactionGroupDto.AppCreatedByCompanyId = aAppTransactionGroupEntity.AppCreatedByCompanyId;
 			aAppTransactionGroupDto.SaasApplicationId = aAppTransactionGroupEntity.SaasApplicationId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionGroupDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionGroupEntity.AppCreatedDate);
                aAppTransactionGroupDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionGroupEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionGroupEntity, aAppTransactionGroupDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionGroupDto Properties to   AppTransactionGroupEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionGroupEntity aAppTransactionGroupEntity,AppTransactionGroupDto aAppTransactionGroupDto)
        {        
 
      			aAppTransactionGroupEntity.AssotmentnavigationId = aAppTransactionGroupDto.AssotmentnavigationId;
      			aAppTransactionGroupEntity.GroupName = aAppTransactionGroupDto.GroupName;
      			aAppTransactionGroupEntity.Description = aAppTransactionGroupDto.Description;
      			aAppTransactionGroupEntity.IsDefaultGroup = aAppTransactionGroupDto.IsDefaultGroup;
      			aAppTransactionGroupEntity.GroupSortOrder = aAppTransactionGroupDto.GroupSortOrder;
      			aAppTransactionGroupEntity.EmBuseinssScope = aAppTransactionGroupDto.EmBuseinssScope;
 
  
   
    
      			aAppTransactionGroupEntity.AppCreatedByCompanyId = aAppTransactionGroupDto.AppCreatedByCompanyId;
      			aAppTransactionGroupEntity.SaasApplicationId = aAppTransactionGroupDto.SaasApplicationId;
			
			if(aAppTransactionGroupDto.Id == null)
			{
				aAppTransactionGroupEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionGroupEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionGroupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionGroupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionGroupEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionGroupEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionGroupEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionGroupEntity, aAppTransactionGroupDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionGroupEntity aAppTransactionGroupEntity,AppTransactionGroupDto aAppTransactionGroupDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionGroupEntity aAppTransactionGroupEntity,AppTransactionGroupDto aAppTransactionGroupDto);
		
   
       
    }
}

 