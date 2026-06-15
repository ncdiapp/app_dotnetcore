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
    /// Convert Properties between  AppTranscationReportEntity and  AppTranscationReportDto
    /// </summary>
    public static partial class AppTranscationReportConverter 
    {
         /// <summary>
        ///  Convert AppTranscationReportEntity To  AppTranscationReportDto
        /// </summary>
        public static AppTranscationReportDto ConvertEntityToDto(AppTranscationReportEntity aAppTranscationReportEntity)
        {        
    		AppTranscationReportDto aAppTranscationReportDto = new AppTranscationReportDto();
    		CopyEntityPropertyToDto( aAppTranscationReportEntity, aAppTranscationReportDto);          
			return aAppTranscationReportDto;
        }
		 /// <summary>
        ///  Convert AppTranscationReportEntity To  AppTranscationReportExDto
        /// </summary>
        public static AppTranscationReportExDto ConvertEntityToExDto(AppTranscationReportEntity aAppTranscationReportEntity)
        {        
    		AppTranscationReportExDto aAppTranscationReportExDto = new AppTranscationReportExDto();
			CopyEntityPropertyToDto( aAppTranscationReportEntity, aAppTranscationReportExDto);
			
			
			
            return aAppTranscationReportExDto;
        }
		
		 /// <summary>
        ///  Convert AppTranscationReportEntity To  AppTranscationReportDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTranscationReportEntity aAppTranscationReportEntity,AppTranscationReportDto aAppTranscationReportDto)
        {        
    		
           // aAppTranscationReportDto.StopChangeTracking();
 			aAppTranscationReportDto.Id = aAppTranscationReportEntity.TransctionReportId;
 			aAppTranscationReportDto.TranscationId = aAppTranscationReportEntity.TranscationId;
 			aAppTranscationReportDto.ReportId = aAppTranscationReportEntity.ReportId;
 			aAppTranscationReportDto.ReportDisplayName = aAppTranscationReportEntity.ReportDisplayName;
 			aAppTranscationReportDto.AppCreatedById = aAppTranscationReportEntity.AppCreatedById;
 			aAppTranscationReportDto.AppCreatedDate = aAppTranscationReportEntity.AppCreatedDate;
 			aAppTranscationReportDto.AppModifiedDate = aAppTranscationReportEntity.AppModifiedDate;
 			aAppTranscationReportDto.AppModifiedById = aAppTranscationReportEntity.AppModifiedById;
 			aAppTranscationReportDto.AppCreatedByCompanyId = aAppTranscationReportEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTranscationReportDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTranscationReportEntity.AppCreatedDate);
                aAppTranscationReportDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTranscationReportEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTranscationReportEntity, aAppTranscationReportDto);
		}
		
		 /// <summary>
        ///  Copy AppTranscationReportDto Properties to   AppTranscationReportEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTranscationReportEntity aAppTranscationReportEntity,AppTranscationReportDto aAppTranscationReportDto)
        {        
 
      			aAppTranscationReportEntity.TranscationId = aAppTranscationReportDto.TranscationId;
      			aAppTranscationReportEntity.ReportId = aAppTranscationReportDto.ReportId;
      			aAppTranscationReportEntity.ReportDisplayName = aAppTranscationReportDto.ReportDisplayName;
 
  
   
    
      			aAppTranscationReportEntity.AppCreatedByCompanyId = aAppTranscationReportDto.AppCreatedByCompanyId;
			
			if(aAppTranscationReportDto.Id == null)
			{
				aAppTranscationReportEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTranscationReportEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTranscationReportEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTranscationReportEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTranscationReportEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTranscationReportEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTranscationReportEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTranscationReportEntity, aAppTranscationReportDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTranscationReportEntity aAppTranscationReportEntity,AppTranscationReportDto aAppTranscationReportDto);
		
		static partial void OnCopyDtoToEntityDone(AppTranscationReportEntity aAppTranscationReportEntity,AppTranscationReportDto aAppTranscationReportDto);
		
   
       
    }
}

 