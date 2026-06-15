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
    /// Convert Properties between  AppItemBaseEntity and  AppItemBaseDto
    /// </summary>
    public static partial class AppItemBaseConverter 
    {
         /// <summary>
        ///  Convert AppItemBaseEntity To  AppItemBaseDto
        /// </summary>
        public static AppItemBaseDto ConvertEntityToDto(AppItemBaseEntity aAppItemBaseEntity)
        {        
    		AppItemBaseDto aAppItemBaseDto = new AppItemBaseDto();
    		CopyEntityPropertyToDto( aAppItemBaseEntity, aAppItemBaseDto);          
			return aAppItemBaseDto;
        }
		 /// <summary>
        ///  Convert AppItemBaseEntity To  AppItemBaseExDto
        /// </summary>
        public static AppItemBaseExDto ConvertEntityToExDto(AppItemBaseEntity aAppItemBaseEntity)
        {        
    		AppItemBaseExDto aAppItemBaseExDto = new AppItemBaseExDto();
			CopyEntityPropertyToDto( aAppItemBaseEntity, aAppItemBaseExDto);
			
			
			
            return aAppItemBaseExDto;
        }
		
		 /// <summary>
        ///  Convert AppItemBaseEntity To  AppItemBaseDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppItemBaseEntity aAppItemBaseEntity,AppItemBaseDto aAppItemBaseDto)
        {        
    		
           // aAppItemBaseDto.StopChangeTracking();
 			aAppItemBaseDto.Id = aAppItemBaseEntity.ItemBaseId;
 			aAppItemBaseDto.Code = aAppItemBaseEntity.Code;
 			aAppItemBaseDto.Desc1 = aAppItemBaseEntity.Desc1;
 			aAppItemBaseDto.Desc2 = aAppItemBaseEntity.Desc2;
 			aAppItemBaseDto.ParentId = aAppItemBaseEntity.ParentId;
 			aAppItemBaseDto.CopyFromId = aAppItemBaseEntity.CopyFromId;
 			aAppItemBaseDto.AppCreatedById = aAppItemBaseEntity.AppCreatedById;
 			aAppItemBaseDto.AppCreatedDate = aAppItemBaseEntity.AppCreatedDate;
 			aAppItemBaseDto.AppModifiedDate = aAppItemBaseEntity.AppModifiedDate;
 			aAppItemBaseDto.AppModifiedById = aAppItemBaseEntity.AppModifiedById;
 			aAppItemBaseDto.CustomerId = aAppItemBaseEntity.CustomerId;
 			aAppItemBaseDto.SupplierId = aAppItemBaseEntity.SupplierId;
 			aAppItemBaseDto.AppCreatedByCompanyId = aAppItemBaseEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppItemBaseDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppItemBaseEntity.AppCreatedDate);
                aAppItemBaseDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppItemBaseEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppItemBaseEntity, aAppItemBaseDto);
		}
		
		 /// <summary>
        ///  Copy AppItemBaseDto Properties to   AppItemBaseEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppItemBaseEntity aAppItemBaseEntity,AppItemBaseDto aAppItemBaseDto)
        {        
 
      			aAppItemBaseEntity.Code = aAppItemBaseDto.Code;
      			aAppItemBaseEntity.Desc1 = aAppItemBaseDto.Desc1;
      			aAppItemBaseEntity.Desc2 = aAppItemBaseDto.Desc2;
      			aAppItemBaseEntity.ParentId = aAppItemBaseDto.ParentId;
      			aAppItemBaseEntity.CopyFromId = aAppItemBaseDto.CopyFromId;
 
  
   
    
      			aAppItemBaseEntity.CustomerId = aAppItemBaseDto.CustomerId;
      			aAppItemBaseEntity.SupplierId = aAppItemBaseDto.SupplierId;
      			aAppItemBaseEntity.AppCreatedByCompanyId = aAppItemBaseDto.AppCreatedByCompanyId;
			
			if(aAppItemBaseDto.Id == null)
			{
				aAppItemBaseEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppItemBaseEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppItemBaseEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppItemBaseEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppItemBaseEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppItemBaseEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppItemBaseEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppItemBaseEntity, aAppItemBaseDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppItemBaseEntity aAppItemBaseEntity,AppItemBaseDto aAppItemBaseDto);
		
		static partial void OnCopyDtoToEntityDone(AppItemBaseEntity aAppItemBaseEntity,AppItemBaseDto aAppItemBaseDto);
		
   
       
    }
}

 