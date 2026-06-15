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
    /// Convert Properties between  AppTranscationDataLoadFieldMappingEntity and  AppTranscationDataLoadFieldMappingDto
    /// </summary>
    public static partial class AppTranscationDataLoadFieldMappingConverter 
    {
         /// <summary>
        ///  Convert AppTranscationDataLoadFieldMappingEntity To  AppTranscationDataLoadFieldMappingDto
        /// </summary>
        public static AppTranscationDataLoadFieldMappingDto ConvertEntityToDto(AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity)
        {        
    		AppTranscationDataLoadFieldMappingDto aAppTranscationDataLoadFieldMappingDto = new AppTranscationDataLoadFieldMappingDto();
    		CopyEntityPropertyToDto( aAppTranscationDataLoadFieldMappingEntity, aAppTranscationDataLoadFieldMappingDto);          
			return aAppTranscationDataLoadFieldMappingDto;
        }
		 /// <summary>
        ///  Convert AppTranscationDataLoadFieldMappingEntity To  AppTranscationDataLoadFieldMappingExDto
        /// </summary>
        public static AppTranscationDataLoadFieldMappingExDto ConvertEntityToExDto(AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity)
        {        
    		AppTranscationDataLoadFieldMappingExDto aAppTranscationDataLoadFieldMappingExDto = new AppTranscationDataLoadFieldMappingExDto();
			CopyEntityPropertyToDto( aAppTranscationDataLoadFieldMappingEntity, aAppTranscationDataLoadFieldMappingExDto);
			
			
			
            return aAppTranscationDataLoadFieldMappingExDto;
        }
		
		 /// <summary>
        ///  Convert AppTranscationDataLoadFieldMappingEntity To  AppTranscationDataLoadFieldMappingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity,AppTranscationDataLoadFieldMappingDto aAppTranscationDataLoadFieldMappingDto)
        {        
    		
           // aAppTranscationDataLoadFieldMappingDto.StopChangeTracking();
 			aAppTranscationDataLoadFieldMappingDto.Id = aAppTranscationDataLoadFieldMappingEntity.FieldMappingId;
 			aAppTranscationDataLoadFieldMappingDto.DataLoadId = aAppTranscationDataLoadFieldMappingEntity.DataLoadId;
 			aAppTranscationDataLoadFieldMappingDto.TransactionFieldId = aAppTranscationDataLoadFieldMappingEntity.TransactionFieldId;
 			aAppTranscationDataLoadFieldMappingDto.DbcolumnName = aAppTranscationDataLoadFieldMappingEntity.DbcolumnName;
 			aAppTranscationDataLoadFieldMappingDto.IsConditionMapping = aAppTranscationDataLoadFieldMappingEntity.IsConditionMapping;
 			aAppTranscationDataLoadFieldMappingDto.WhereClause = aAppTranscationDataLoadFieldMappingEntity.WhereClause;
 			aAppTranscationDataLoadFieldMappingDto.AppCreatedById = aAppTranscationDataLoadFieldMappingEntity.AppCreatedById;
 			aAppTranscationDataLoadFieldMappingDto.AppCreatedDate = aAppTranscationDataLoadFieldMappingEntity.AppCreatedDate;
 			aAppTranscationDataLoadFieldMappingDto.AppModifiedDate = aAppTranscationDataLoadFieldMappingEntity.AppModifiedDate;
 			aAppTranscationDataLoadFieldMappingDto.AppModifiedById = aAppTranscationDataLoadFieldMappingEntity.AppModifiedById;
 			aAppTranscationDataLoadFieldMappingDto.AppCreatedByCompanyId = aAppTranscationDataLoadFieldMappingEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTranscationDataLoadFieldMappingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTranscationDataLoadFieldMappingEntity.AppCreatedDate);
                aAppTranscationDataLoadFieldMappingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTranscationDataLoadFieldMappingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTranscationDataLoadFieldMappingEntity, aAppTranscationDataLoadFieldMappingDto);
		}
		
		 /// <summary>
        ///  Copy AppTranscationDataLoadFieldMappingDto Properties to   AppTranscationDataLoadFieldMappingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity,AppTranscationDataLoadFieldMappingDto aAppTranscationDataLoadFieldMappingDto)
        {        
 
      			aAppTranscationDataLoadFieldMappingEntity.DataLoadId = aAppTranscationDataLoadFieldMappingDto.DataLoadId;
      			aAppTranscationDataLoadFieldMappingEntity.TransactionFieldId = aAppTranscationDataLoadFieldMappingDto.TransactionFieldId;
      			aAppTranscationDataLoadFieldMappingEntity.DbcolumnName = aAppTranscationDataLoadFieldMappingDto.DbcolumnName;
      			aAppTranscationDataLoadFieldMappingEntity.IsConditionMapping = aAppTranscationDataLoadFieldMappingDto.IsConditionMapping;
      			aAppTranscationDataLoadFieldMappingEntity.WhereClause = aAppTranscationDataLoadFieldMappingDto.WhereClause;
 
  
   
    
      			aAppTranscationDataLoadFieldMappingEntity.AppCreatedByCompanyId = aAppTranscationDataLoadFieldMappingDto.AppCreatedByCompanyId;
			
			if(aAppTranscationDataLoadFieldMappingDto.Id == null)
			{
				aAppTranscationDataLoadFieldMappingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTranscationDataLoadFieldMappingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTranscationDataLoadFieldMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTranscationDataLoadFieldMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTranscationDataLoadFieldMappingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTranscationDataLoadFieldMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTranscationDataLoadFieldMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTranscationDataLoadFieldMappingEntity, aAppTranscationDataLoadFieldMappingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity,AppTranscationDataLoadFieldMappingDto aAppTranscationDataLoadFieldMappingDto);
		
		static partial void OnCopyDtoToEntityDone(AppTranscationDataLoadFieldMappingEntity aAppTranscationDataLoadFieldMappingEntity,AppTranscationDataLoadFieldMappingDto aAppTranscationDataLoadFieldMappingDto);
		
   
       
    }
}

 