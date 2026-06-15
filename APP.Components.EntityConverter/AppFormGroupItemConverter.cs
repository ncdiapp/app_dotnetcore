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
    /// Convert Properties between  AppFormGroupItemEntity and  AppFormGroupItemDto
    /// </summary>
    public static partial class AppFormGroupItemConverter 
    {
         /// <summary>
        ///  Convert AppFormGroupItemEntity To  AppFormGroupItemDto
        /// </summary>
        public static AppFormGroupItemDto ConvertEntityToDto(AppFormGroupItemEntity aAppFormGroupItemEntity)
        {        
    		AppFormGroupItemDto aAppFormGroupItemDto = new AppFormGroupItemDto();
    		CopyEntityPropertyToDto( aAppFormGroupItemEntity, aAppFormGroupItemDto);          
			return aAppFormGroupItemDto;
        }
		 /// <summary>
        ///  Convert AppFormGroupItemEntity To  AppFormGroupItemExDto
        /// </summary>
        public static AppFormGroupItemExDto ConvertEntityToExDto(AppFormGroupItemEntity aAppFormGroupItemEntity)
        {        
    		AppFormGroupItemExDto aAppFormGroupItemExDto = new AppFormGroupItemExDto();
			CopyEntityPropertyToDto( aAppFormGroupItemEntity, aAppFormGroupItemExDto);
			
			
			
            return aAppFormGroupItemExDto;
        }
		
		 /// <summary>
        ///  Convert AppFormGroupItemEntity To  AppFormGroupItemDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppFormGroupItemEntity aAppFormGroupItemEntity,AppFormGroupItemDto aAppFormGroupItemDto)
        {        
    		
           // aAppFormGroupItemDto.StopChangeTracking();
 			aAppFormGroupItemDto.Id = aAppFormGroupItemEntity.GroupItemId;
 			aAppFormGroupItemDto.FromGroupId = aAppFormGroupItemEntity.FromGroupId;
 			aAppFormGroupItemDto.TransactionId = aAppFormGroupItemEntity.TransactionId;
 			aAppFormGroupItemDto.FlowOrder = aAppFormGroupItemEntity.FlowOrder;
 			aAppFormGroupItemDto.AppCreatedById = aAppFormGroupItemEntity.AppCreatedById;
 			aAppFormGroupItemDto.AppCreatedDate = aAppFormGroupItemEntity.AppCreatedDate;
 			aAppFormGroupItemDto.AppModifiedDate = aAppFormGroupItemEntity.AppModifiedDate;
 			aAppFormGroupItemDto.AppModifiedById = aAppFormGroupItemEntity.AppModifiedById;
 			aAppFormGroupItemDto.AppCreatedByCompanyId = aAppFormGroupItemEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppFormGroupItemDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormGroupItemEntity.AppCreatedDate);
                aAppFormGroupItemDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppFormGroupItemEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppFormGroupItemEntity, aAppFormGroupItemDto);
		}
		
		 /// <summary>
        ///  Copy AppFormGroupItemDto Properties to   AppFormGroupItemEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppFormGroupItemEntity aAppFormGroupItemEntity,AppFormGroupItemDto aAppFormGroupItemDto)
        {        
 
      			aAppFormGroupItemEntity.FromGroupId = aAppFormGroupItemDto.FromGroupId;
      			aAppFormGroupItemEntity.TransactionId = aAppFormGroupItemDto.TransactionId;
      			aAppFormGroupItemEntity.FlowOrder = aAppFormGroupItemDto.FlowOrder;
 
  
   
    
      			aAppFormGroupItemEntity.AppCreatedByCompanyId = aAppFormGroupItemDto.AppCreatedByCompanyId;
			
			if(aAppFormGroupItemDto.Id == null)
			{
				aAppFormGroupItemEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormGroupItemEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppFormGroupItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormGroupItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppFormGroupItemEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppFormGroupItemEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppFormGroupItemEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppFormGroupItemEntity, aAppFormGroupItemDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppFormGroupItemEntity aAppFormGroupItemEntity,AppFormGroupItemDto aAppFormGroupItemDto);
		
		static partial void OnCopyDtoToEntityDone(AppFormGroupItemEntity aAppFormGroupItemEntity,AppFormGroupItemDto aAppFormGroupItemDto);
		
   
       
    }
}

 