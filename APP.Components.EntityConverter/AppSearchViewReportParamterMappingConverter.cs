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
    /// Convert Properties between  AppSearchViewReportParamterMappingEntity and  AppSearchViewReportParamterMappingDto
    /// </summary>
    public static partial class AppSearchViewReportParamterMappingConverter 
    {
         /// <summary>
        ///  Convert AppSearchViewReportParamterMappingEntity To  AppSearchViewReportParamterMappingDto
        /// </summary>
        public static AppSearchViewReportParamterMappingDto ConvertEntityToDto(AppSearchViewReportParamterMappingEntity aAppSearchViewReportParamterMappingEntity)
        {        
    		AppSearchViewReportParamterMappingDto aAppSearchViewReportParamterMappingDto = new AppSearchViewReportParamterMappingDto();
    		CopyEntityPropertyToDto( aAppSearchViewReportParamterMappingEntity, aAppSearchViewReportParamterMappingDto);          
			return aAppSearchViewReportParamterMappingDto;
        }
		 /// <summary>
        ///  Convert AppSearchViewReportParamterMappingEntity To  AppSearchViewReportParamterMappingExDto
        /// </summary>
        public static AppSearchViewReportParamterMappingExDto ConvertEntityToExDto(AppSearchViewReportParamterMappingEntity aAppSearchViewReportParamterMappingEntity)
        {        
    		AppSearchViewReportParamterMappingExDto aAppSearchViewReportParamterMappingExDto = new AppSearchViewReportParamterMappingExDto();
			CopyEntityPropertyToDto( aAppSearchViewReportParamterMappingEntity, aAppSearchViewReportParamterMappingExDto);
			
			
			
            return aAppSearchViewReportParamterMappingExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchViewReportParamterMappingEntity To  AppSearchViewReportParamterMappingDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchViewReportParamterMappingEntity aAppSearchViewReportParamterMappingEntity,AppSearchViewReportParamterMappingDto aAppSearchViewReportParamterMappingDto)
        {        
    		
           // aAppSearchViewReportParamterMappingDto.StopChangeTracking();
 			aAppSearchViewReportParamterMappingDto.Id = aAppSearchViewReportParamterMappingEntity.ParamterMappingId;
 			aAppSearchViewReportParamterMappingDto.SearchViewReportId = aAppSearchViewReportParamterMappingEntity.SearchViewReportId;
 			aAppSearchViewReportParamterMappingDto.SearchViewFieldId = aAppSearchViewReportParamterMappingEntity.SearchViewFieldId;
 			aAppSearchViewReportParamterMappingDto.ReportParamterName = aAppSearchViewReportParamterMappingEntity.ReportParamterName;
 			aAppSearchViewReportParamterMappingDto.AppCreatedById = aAppSearchViewReportParamterMappingEntity.AppCreatedById;
 			aAppSearchViewReportParamterMappingDto.AppCreatedDate = aAppSearchViewReportParamterMappingEntity.AppCreatedDate;
 			aAppSearchViewReportParamterMappingDto.AppModifiedDate = aAppSearchViewReportParamterMappingEntity.AppModifiedDate;
 			aAppSearchViewReportParamterMappingDto.AppModifiedById = aAppSearchViewReportParamterMappingEntity.AppModifiedById;
 			aAppSearchViewReportParamterMappingDto.AppCreatedByCompanyId = aAppSearchViewReportParamterMappingEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchViewReportParamterMappingDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewReportParamterMappingEntity.AppCreatedDate);
                aAppSearchViewReportParamterMappingDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchViewReportParamterMappingEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchViewReportParamterMappingEntity, aAppSearchViewReportParamterMappingDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchViewReportParamterMappingDto Properties to   AppSearchViewReportParamterMappingEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchViewReportParamterMappingEntity aAppSearchViewReportParamterMappingEntity,AppSearchViewReportParamterMappingDto aAppSearchViewReportParamterMappingDto)
        {        
 
      			aAppSearchViewReportParamterMappingEntity.SearchViewReportId = aAppSearchViewReportParamterMappingDto.SearchViewReportId;
      			aAppSearchViewReportParamterMappingEntity.SearchViewFieldId = aAppSearchViewReportParamterMappingDto.SearchViewFieldId;
      			aAppSearchViewReportParamterMappingEntity.ReportParamterName = aAppSearchViewReportParamterMappingDto.ReportParamterName;
 
  
   
    
      			aAppSearchViewReportParamterMappingEntity.AppCreatedByCompanyId = aAppSearchViewReportParamterMappingDto.AppCreatedByCompanyId;
			
			if(aAppSearchViewReportParamterMappingDto.Id == null)
			{
				aAppSearchViewReportParamterMappingEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewReportParamterMappingEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchViewReportParamterMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewReportParamterMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchViewReportParamterMappingEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchViewReportParamterMappingEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchViewReportParamterMappingEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchViewReportParamterMappingEntity, aAppSearchViewReportParamterMappingDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchViewReportParamterMappingEntity aAppSearchViewReportParamterMappingEntity,AppSearchViewReportParamterMappingDto aAppSearchViewReportParamterMappingDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchViewReportParamterMappingEntity aAppSearchViewReportParamterMappingEntity,AppSearchViewReportParamterMappingDto aAppSearchViewReportParamterMappingDto);
		
   
       
    }
}

 