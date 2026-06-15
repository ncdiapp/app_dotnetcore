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
    /// Convert Properties between  AppEntityEnumValueEntity and  AppEntityEnumValueDto
    /// </summary>
    public static partial class AppEntityEnumValueConverter 
    {
         /// <summary>
        ///  Convert AppEntityEnumValueEntity To  AppEntityEnumValueDto
        /// </summary>
        public static AppEntityEnumValueDto ConvertEntityToDto(AppEntityEnumValueEntity aAppEntityEnumValueEntity)
        {        
    		AppEntityEnumValueDto aAppEntityEnumValueDto = new AppEntityEnumValueDto();
    		CopyEntityPropertyToDto( aAppEntityEnumValueEntity, aAppEntityEnumValueDto);          
			return aAppEntityEnumValueDto;
        }
		 /// <summary>
        ///  Convert AppEntityEnumValueEntity To  AppEntityEnumValueExDto
        /// </summary>
        public static AppEntityEnumValueExDto ConvertEntityToExDto(AppEntityEnumValueEntity aAppEntityEnumValueEntity)
        {        
    		AppEntityEnumValueExDto aAppEntityEnumValueExDto = new AppEntityEnumValueExDto();
			CopyEntityPropertyToDto( aAppEntityEnumValueEntity, aAppEntityEnumValueExDto);
			
			
			
            return aAppEntityEnumValueExDto;
        }
		
		 /// <summary>
        ///  Convert AppEntityEnumValueEntity To  AppEntityEnumValueDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEntityEnumValueEntity aAppEntityEnumValueEntity,AppEntityEnumValueDto aAppEntityEnumValueDto)
        {        
    		
           // aAppEntityEnumValueDto.StopChangeTracking();
 			aAppEntityEnumValueDto.Id = aAppEntityEnumValueEntity.EnumValueId;
 			aAppEntityEnumValueDto.EntityInfoId = aAppEntityEnumValueEntity.EntityInfoId;
 			aAppEntityEnumValueDto.EnumKey = aAppEntityEnumValueEntity.EnumKey;
 			aAppEntityEnumValueDto.EnumValue = aAppEntityEnumValueEntity.EnumValue;
 			aAppEntityEnumValueDto.AppCreatedById = aAppEntityEnumValueEntity.AppCreatedById;
 			aAppEntityEnumValueDto.AppCreatedDate = aAppEntityEnumValueEntity.AppCreatedDate;
 			aAppEntityEnumValueDto.AppModifiedDate = aAppEntityEnumValueEntity.AppModifiedDate;
 			aAppEntityEnumValueDto.AppModifiedById = aAppEntityEnumValueEntity.AppModifiedById;
 			aAppEntityEnumValueDto.AppCreatedByCompanyId = aAppEntityEnumValueEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEntityEnumValueDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEntityEnumValueEntity.AppCreatedDate);
                aAppEntityEnumValueDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEntityEnumValueEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEntityEnumValueEntity, aAppEntityEnumValueDto);
		}
		
		 /// <summary>
        ///  Copy AppEntityEnumValueDto Properties to   AppEntityEnumValueEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEntityEnumValueEntity aAppEntityEnumValueEntity,AppEntityEnumValueDto aAppEntityEnumValueDto)
        {        
 
      			aAppEntityEnumValueEntity.EntityInfoId = aAppEntityEnumValueDto.EntityInfoId;
      			aAppEntityEnumValueEntity.EnumKey = aAppEntityEnumValueDto.EnumKey;
      			aAppEntityEnumValueEntity.EnumValue = aAppEntityEnumValueDto.EnumValue;
 
  
   
    
      			aAppEntityEnumValueEntity.AppCreatedByCompanyId = aAppEntityEnumValueDto.AppCreatedByCompanyId;
			
			if(aAppEntityEnumValueDto.Id == null)
			{
				aAppEntityEnumValueEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEntityEnumValueEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEntityEnumValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEntityEnumValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEntityEnumValueEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEntityEnumValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEntityEnumValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEntityEnumValueEntity, aAppEntityEnumValueDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEntityEnumValueEntity aAppEntityEnumValueEntity,AppEntityEnumValueDto aAppEntityEnumValueDto);
		
		static partial void OnCopyDtoToEntityDone(AppEntityEnumValueEntity aAppEntityEnumValueEntity,AppEntityEnumValueDto aAppEntityEnumValueDto);
		
   
       
    }
}

 