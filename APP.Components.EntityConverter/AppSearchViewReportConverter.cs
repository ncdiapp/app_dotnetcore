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
    /// Convert Properties between  AppSearchViewReportEntity and  AppSearchViewReportDto
    /// </summary>
    public static partial class AppSearchViewReportConverter 
    {
         /// <summary>
        ///  Convert AppSearchViewReportEntity To  AppSearchViewReportDto
        /// </summary>
        public static AppSearchViewReportDto ConvertEntityToDto(AppSearchViewReportEntity aAppSearchViewReportEntity)
        {        
    		AppSearchViewReportDto aAppSearchViewReportDto = new AppSearchViewReportDto();
    		CopyEntityPropertyToDto( aAppSearchViewReportEntity, aAppSearchViewReportDto);          
			return aAppSearchViewReportDto;
        }
		 /// <summary>
        ///  Convert AppSearchViewReportEntity To  AppSearchViewReportExDto
        /// </summary>
        public static AppSearchViewReportExDto ConvertEntityToExDto(AppSearchViewReportEntity aAppSearchViewReportEntity)
        {        
    		AppSearchViewReportExDto aAppSearchViewReportExDto = new AppSearchViewReportExDto();
			CopyEntityPropertyToDto( aAppSearchViewReportEntity, aAppSearchViewReportExDto);
			
			
			
            return aAppSearchViewReportExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchViewReportEntity To  AppSearchViewReportDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchViewReportEntity aAppSearchViewReportEntity,AppSearchViewReportDto aAppSearchViewReportDto)
        {        
    		
           // aAppSearchViewReportDto.StopChangeTracking();
 			aAppSearchViewReportDto.Id = aAppSearchViewReportEntity.SearchViewReportId;
 			aAppSearchViewReportDto.SearchViewId = aAppSearchViewReportEntity.SearchViewId;
 			aAppSearchViewReportDto.ReportId = aAppSearchViewReportEntity.ReportId;
 			aAppSearchViewReportDto.ReportDisplayName = aAppSearchViewReportEntity.ReportDisplayName;
 			aAppSearchViewReportDto.AppCreatedById = aAppSearchViewReportEntity.AppCreatedById;
 			aAppSearchViewReportDto.AppCreatedDate = aAppSearchViewReportEntity.AppCreatedDate;
 			aAppSearchViewReportDto.AppModifiedDate = aAppSearchViewReportEntity.AppModifiedDate;
 			aAppSearchViewReportDto.AppModifiedById = aAppSearchViewReportEntity.AppModifiedById;
 			aAppSearchViewReportDto.AppCreatedByCompanyId = aAppSearchViewReportEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchViewReportDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewReportEntity.AppCreatedDate);
                aAppSearchViewReportDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewReportEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchViewReportEntity, aAppSearchViewReportDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchViewReportDto Properties to   AppSearchViewReportEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchViewReportEntity aAppSearchViewReportEntity,AppSearchViewReportDto aAppSearchViewReportDto)
        {        
 
      			aAppSearchViewReportEntity.SearchViewId = aAppSearchViewReportDto.SearchViewId;
      			aAppSearchViewReportEntity.ReportId = aAppSearchViewReportDto.ReportId;
      			aAppSearchViewReportEntity.ReportDisplayName = aAppSearchViewReportDto.ReportDisplayName;
 
  
   
    
      			aAppSearchViewReportEntity.AppCreatedByCompanyId = aAppSearchViewReportDto.AppCreatedByCompanyId;
			
			if(aAppSearchViewReportDto.Id == null)
			{
				aAppSearchViewReportEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewReportEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchViewReportEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewReportEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewReportEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchViewReportEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewReportEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchViewReportEntity, aAppSearchViewReportDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchViewReportEntity aAppSearchViewReportEntity,AppSearchViewReportDto aAppSearchViewReportDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchViewReportEntity aAppSearchViewReportEntity,AppSearchViewReportDto aAppSearchViewReportDto);
		
   
       
    }
}

 