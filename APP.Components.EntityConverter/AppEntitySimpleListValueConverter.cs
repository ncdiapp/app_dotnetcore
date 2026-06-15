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
    /// Convert Properties between  AppEntitySimpleListValueEntity and  AppEntitySimpleListValueDto
    /// </summary>
    public static partial class AppEntitySimpleListValueConverter 
    {
         /// <summary>
        ///  Convert AppEntitySimpleListValueEntity To  AppEntitySimpleListValueDto
        /// </summary>
        public static AppEntitySimpleListValueDto ConvertEntityToDto(AppEntitySimpleListValueEntity aAppEntitySimpleListValueEntity)
        {        
    		AppEntitySimpleListValueDto aAppEntitySimpleListValueDto = new AppEntitySimpleListValueDto();
    		CopyEntityPropertyToDto( aAppEntitySimpleListValueEntity, aAppEntitySimpleListValueDto);          
			return aAppEntitySimpleListValueDto;
        }
		 /// <summary>
        ///  Convert AppEntitySimpleListValueEntity To  AppEntitySimpleListValueExDto
        /// </summary>
        public static AppEntitySimpleListValueExDto ConvertEntityToExDto(AppEntitySimpleListValueEntity aAppEntitySimpleListValueEntity)
        {        
    		AppEntitySimpleListValueExDto aAppEntitySimpleListValueExDto = new AppEntitySimpleListValueExDto();
			CopyEntityPropertyToDto( aAppEntitySimpleListValueEntity, aAppEntitySimpleListValueExDto);
			
			
			
            return aAppEntitySimpleListValueExDto;
        }
		
		 /// <summary>
        ///  Convert AppEntitySimpleListValueEntity To  AppEntitySimpleListValueDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEntitySimpleListValueEntity aAppEntitySimpleListValueEntity,AppEntitySimpleListValueDto aAppEntitySimpleListValueDto)
        {        
    		
           // aAppEntitySimpleListValueDto.StopChangeTracking();
 			aAppEntitySimpleListValueDto.Id = aAppEntitySimpleListValueEntity.SimpleListValueId;
 			aAppEntitySimpleListValueDto.EntityInfoId = aAppEntitySimpleListValueEntity.EntityInfoId;
 			aAppEntitySimpleListValueDto.Sort = aAppEntitySimpleListValueEntity.Sort;
 			aAppEntitySimpleListValueDto.Code = aAppEntitySimpleListValueEntity.Code;
 			aAppEntitySimpleListValueDto.Description = aAppEntitySimpleListValueEntity.Description;
 			aAppEntitySimpleListValueDto.AppCreatedById = aAppEntitySimpleListValueEntity.AppCreatedById;
 			aAppEntitySimpleListValueDto.AppCreatedDate = aAppEntitySimpleListValueEntity.AppCreatedDate;
 			aAppEntitySimpleListValueDto.AppModifiedDate = aAppEntitySimpleListValueEntity.AppModifiedDate;
 			aAppEntitySimpleListValueDto.AppModifiedById = aAppEntitySimpleListValueEntity.AppModifiedById;
 			aAppEntitySimpleListValueDto.AppCreatedByCompanyId = aAppEntitySimpleListValueEntity.AppCreatedByCompanyId;
 			aAppEntitySimpleListValueDto.InternalKey = aAppEntitySimpleListValueEntity.InternalKey;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEntitySimpleListValueDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEntitySimpleListValueEntity.AppCreatedDate);
                aAppEntitySimpleListValueDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEntitySimpleListValueEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEntitySimpleListValueEntity, aAppEntitySimpleListValueDto);
		}
		
		 /// <summary>
        ///  Copy AppEntitySimpleListValueDto Properties to   AppEntitySimpleListValueEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEntitySimpleListValueEntity aAppEntitySimpleListValueEntity,AppEntitySimpleListValueDto aAppEntitySimpleListValueDto)
        {        
 
      			aAppEntitySimpleListValueEntity.EntityInfoId = aAppEntitySimpleListValueDto.EntityInfoId;
      			aAppEntitySimpleListValueEntity.Sort = aAppEntitySimpleListValueDto.Sort;
      			aAppEntitySimpleListValueEntity.Code = aAppEntitySimpleListValueDto.Code;
      			aAppEntitySimpleListValueEntity.Description = aAppEntitySimpleListValueDto.Description;
 
  
   
    
      			aAppEntitySimpleListValueEntity.AppCreatedByCompanyId = aAppEntitySimpleListValueDto.AppCreatedByCompanyId;
      			aAppEntitySimpleListValueEntity.InternalKey = aAppEntitySimpleListValueDto.InternalKey;
			
			if(aAppEntitySimpleListValueDto.Id == null)
			{
				aAppEntitySimpleListValueEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEntitySimpleListValueEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEntitySimpleListValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEntitySimpleListValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEntitySimpleListValueEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEntitySimpleListValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEntitySimpleListValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEntitySimpleListValueEntity, aAppEntitySimpleListValueDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEntitySimpleListValueEntity aAppEntitySimpleListValueEntity,AppEntitySimpleListValueDto aAppEntitySimpleListValueDto);
		
		static partial void OnCopyDtoToEntityDone(AppEntitySimpleListValueEntity aAppEntitySimpleListValueEntity,AppEntitySimpleListValueDto aAppEntitySimpleListValueDto);
		
   
       
    }
}

 