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
    /// Convert Properties between  AppDataSetEntity and  AppDataSetDto
    /// </summary>
    public static partial class AppDataSetConverter 
    {
         /// <summary>
        ///  Convert AppDataSetEntity To  AppDataSetDto
        /// </summary>
        public static AppDataSetDto ConvertEntityToDto(AppDataSetEntity aAppDataSetEntity)
        {        
    		AppDataSetDto aAppDataSetDto = new AppDataSetDto();
    		CopyEntityPropertyToDto( aAppDataSetEntity, aAppDataSetDto);          
			return aAppDataSetDto;
        }
		 /// <summary>
        ///  Convert AppDataSetEntity To  AppDataSetExDto
        /// </summary>
        public static AppDataSetExDto ConvertEntityToExDto(AppDataSetEntity aAppDataSetEntity)
        {        
    		AppDataSetExDto aAppDataSetExDto = new AppDataSetExDto();
			CopyEntityPropertyToDto( aAppDataSetEntity, aAppDataSetExDto);
			
			
			
            return aAppDataSetExDto;
        }
		
		 /// <summary>
        ///  Convert AppDataSetEntity To  AppDataSetDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDataSetEntity aAppDataSetEntity,AppDataSetDto aAppDataSetDto)
        {        
    		
           // aAppDataSetDto.StopChangeTracking();
 			aAppDataSetDto.Id = aAppDataSetEntity.DataSetId;
 			aAppDataSetDto.DataSourceFrom = aAppDataSetEntity.DataSourceFrom;
 			aAppDataSetDto.Name = aAppDataSetEntity.Name;
 			aAppDataSetDto.Description = aAppDataSetEntity.Description;
 			aAppDataSetDto.BaseDataSetId = aAppDataSetEntity.BaseDataSetId;
 			aAppDataSetDto.QueryText = aAppDataSetEntity.QueryText;
 			aAppDataSetDto.QueryType = aAppDataSetEntity.QueryType;
 			aAppDataSetDto.AppCreatedById = aAppDataSetEntity.AppCreatedById;
 			aAppDataSetDto.AppCreatedDate = aAppDataSetEntity.AppCreatedDate;
 			aAppDataSetDto.AppModifiedDate = aAppDataSetEntity.AppModifiedDate;
 			aAppDataSetDto.AppModifiedById = aAppDataSetEntity.AppModifiedById;
 			aAppDataSetDto.AppCreatedByCompanyId = aAppDataSetEntity.AppCreatedByCompanyId;
 			aAppDataSetDto.SaasApplicationId = aAppDataSetEntity.SaasApplicationId;
 			aAppDataSetDto.UsageTypeId = aAppDataSetEntity.UsageTypeId;
 			aAppDataSetDto.BaseTableName = aAppDataSetEntity.BaseTableName;
 			aAppDataSetDto.WebApiConfigId = aAppDataSetEntity.WebApiConfigId;
 			aAppDataSetDto.RestApiHeaderKeyValue = aAppDataSetEntity.RestApiHeaderKeyValue;
 			aAppDataSetDto.RestApiQueryParameterKeyValue = aAppDataSetEntity.RestApiQueryParameterKeyValue;
 			aAppDataSetDto.HttpMethod = aAppDataSetEntity.HttpMethod;
 			aAppDataSetDto.OtherSettings = aAppDataSetEntity.OtherSettings;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDataSetDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDataSetEntity.AppCreatedDate);
                aAppDataSetDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDataSetEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDataSetEntity, aAppDataSetDto);
		}
		
		 /// <summary>
        ///  Copy AppDataSetDto Properties to   AppDataSetEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDataSetEntity aAppDataSetEntity,AppDataSetDto aAppDataSetDto)
        {        
 
      			aAppDataSetEntity.DataSourceFrom = aAppDataSetDto.DataSourceFrom;
      			aAppDataSetEntity.Name = aAppDataSetDto.Name;
      			aAppDataSetEntity.Description = aAppDataSetDto.Description;
      			aAppDataSetEntity.BaseDataSetId = aAppDataSetDto.BaseDataSetId;
      			aAppDataSetEntity.QueryText = aAppDataSetDto.QueryText;
      			aAppDataSetEntity.QueryType = aAppDataSetDto.QueryType;
 
  
   
    
      			aAppDataSetEntity.AppCreatedByCompanyId = aAppDataSetDto.AppCreatedByCompanyId;
      			aAppDataSetEntity.SaasApplicationId = aAppDataSetDto.SaasApplicationId;
      			aAppDataSetEntity.UsageTypeId = aAppDataSetDto.UsageTypeId;
      			aAppDataSetEntity.BaseTableName = aAppDataSetDto.BaseTableName;
      			aAppDataSetEntity.WebApiConfigId = aAppDataSetDto.WebApiConfigId;
      			aAppDataSetEntity.RestApiHeaderKeyValue = aAppDataSetDto.RestApiHeaderKeyValue;
      			aAppDataSetEntity.RestApiQueryParameterKeyValue = aAppDataSetDto.RestApiQueryParameterKeyValue;
      			aAppDataSetEntity.HttpMethod = aAppDataSetDto.HttpMethod;
      			aAppDataSetEntity.OtherSettings = aAppDataSetDto.OtherSettings;
			
			if(aAppDataSetDto.Id == null)
			{
				aAppDataSetEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDataSetEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDataSetEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDataSetEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDataSetEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDataSetEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDataSetEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDataSetEntity, aAppDataSetDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDataSetEntity aAppDataSetEntity,AppDataSetDto aAppDataSetDto);
		
		static partial void OnCopyDtoToEntityDone(AppDataSetEntity aAppDataSetEntity,AppDataSetDto aAppDataSetDto);
		
   
       
    }
}

 