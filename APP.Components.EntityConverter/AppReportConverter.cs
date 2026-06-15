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
    /// Convert Properties between  AppReportEntity and  AppReportDto
    /// </summary>
    public static partial class AppReportConverter 
    {
         /// <summary>
        ///  Convert AppReportEntity To  AppReportDto
        /// </summary>
        public static AppReportDto ConvertEntityToDto(AppReportEntity aAppReportEntity)
        {        
    		AppReportDto aAppReportDto = new AppReportDto();
    		CopyEntityPropertyToDto( aAppReportEntity, aAppReportDto);          
			return aAppReportDto;
        }
		 /// <summary>
        ///  Convert AppReportEntity To  AppReportExDto
        /// </summary>
        public static AppReportExDto ConvertEntityToExDto(AppReportEntity aAppReportEntity)
        {        
    		AppReportExDto aAppReportExDto = new AppReportExDto();
			CopyEntityPropertyToDto( aAppReportEntity, aAppReportExDto);
			
			
			
            return aAppReportExDto;
        }
		
		 /// <summary>
        ///  Convert AppReportEntity To  AppReportDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppReportEntity aAppReportEntity,AppReportDto aAppReportDto)
        {        
    		
           // aAppReportDto.StopChangeTracking();
 			aAppReportDto.Id = aAppReportEntity.ReportId;
 			aAppReportDto.DataSourceId = aAppReportEntity.DataSourceId;
 			aAppReportDto.ReportName = aAppReportEntity.ReportName;
 			aAppReportDto.Description = aAppReportEntity.Description;
 			aAppReportDto.ReportFileName = aAppReportEntity.ReportFileName;
 			aAppReportDto.IsActive = aAppReportEntity.IsActive;
 			aAppReportDto.ReportEngineType = aAppReportEntity.ReportEngineType;
 			aAppReportDto.AppCreatedById = aAppReportEntity.AppCreatedById;
 			aAppReportDto.AppCreatedDate = aAppReportEntity.AppCreatedDate;
 			aAppReportDto.AppModifiedDate = aAppReportEntity.AppModifiedDate;
 			aAppReportDto.AppModifiedById = aAppReportEntity.AppModifiedById;
 			aAppReportDto.AppCreatedByCompanyId = aAppReportEntity.AppCreatedByCompanyId;
 			aAppReportDto.SaasApplicationId = aAppReportEntity.SaasApplicationId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppReportDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppReportEntity.AppCreatedDate);
                aAppReportDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppReportEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppReportEntity, aAppReportDto);
		}
		
		 /// <summary>
        ///  Copy AppReportDto Properties to   AppReportEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppReportEntity aAppReportEntity,AppReportDto aAppReportDto)
        {        
 
      			aAppReportEntity.DataSourceId = aAppReportDto.DataSourceId;
      			aAppReportEntity.ReportName = aAppReportDto.ReportName;
      			aAppReportEntity.Description = aAppReportDto.Description;
      			aAppReportEntity.ReportFileName = aAppReportDto.ReportFileName;
      			aAppReportEntity.IsActive = aAppReportDto.IsActive;
      			aAppReportEntity.ReportEngineType = aAppReportDto.ReportEngineType;
 
  
   
    
      			aAppReportEntity.AppCreatedByCompanyId = aAppReportDto.AppCreatedByCompanyId;
      			aAppReportEntity.SaasApplicationId = aAppReportDto.SaasApplicationId;
			
			if(aAppReportDto.Id == null)
			{
				aAppReportEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppReportEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppReportEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppReportEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppReportEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppReportEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppReportEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppReportEntity, aAppReportDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppReportEntity aAppReportEntity,AppReportDto aAppReportDto);
		
		static partial void OnCopyDtoToEntityDone(AppReportEntity aAppReportEntity,AppReportDto aAppReportDto);
		
   
       
    }
}

 