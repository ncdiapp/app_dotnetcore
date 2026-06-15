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
    /// Convert Properties between  AppDatabaseDiagramEntity and  AppDatabaseDiagramDto
    /// </summary>
    public static partial class AppDatabaseDiagramConverter 
    {
         /// <summary>
        ///  Convert AppDatabaseDiagramEntity To  AppDatabaseDiagramDto
        /// </summary>
        public static AppDatabaseDiagramDto ConvertEntityToDto(AppDatabaseDiagramEntity aAppDatabaseDiagramEntity)
        {        
    		AppDatabaseDiagramDto aAppDatabaseDiagramDto = new AppDatabaseDiagramDto();
    		CopyEntityPropertyToDto( aAppDatabaseDiagramEntity, aAppDatabaseDiagramDto);          
			return aAppDatabaseDiagramDto;
        }
		 /// <summary>
        ///  Convert AppDatabaseDiagramEntity To  AppDatabaseDiagramExDto
        /// </summary>
        public static AppDatabaseDiagramExDto ConvertEntityToExDto(AppDatabaseDiagramEntity aAppDatabaseDiagramEntity)
        {        
    		AppDatabaseDiagramExDto aAppDatabaseDiagramExDto = new AppDatabaseDiagramExDto();
			CopyEntityPropertyToDto( aAppDatabaseDiagramEntity, aAppDatabaseDiagramExDto);
			
			
			
            return aAppDatabaseDiagramExDto;
        }
		
		 /// <summary>
        ///  Convert AppDatabaseDiagramEntity To  AppDatabaseDiagramDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDatabaseDiagramEntity aAppDatabaseDiagramEntity,AppDatabaseDiagramDto aAppDatabaseDiagramDto)
        {        
    		
           // aAppDatabaseDiagramDto.StopChangeTracking();
 			aAppDatabaseDiagramDto.Id = aAppDatabaseDiagramEntity.DiagramId;
 			aAppDatabaseDiagramDto.DiagramName = aAppDatabaseDiagramEntity.DiagramName;
 			aAppDatabaseDiagramDto.Description = aAppDatabaseDiagramEntity.Description;
 			aAppDatabaseDiagramDto.AppCreatedById = aAppDatabaseDiagramEntity.AppCreatedById;
 			aAppDatabaseDiagramDto.AppCreatedDate = aAppDatabaseDiagramEntity.AppCreatedDate;
 			aAppDatabaseDiagramDto.AppModifiedDate = aAppDatabaseDiagramEntity.AppModifiedDate;
 			aAppDatabaseDiagramDto.AppModifiedById = aAppDatabaseDiagramEntity.AppModifiedById;
 			aAppDatabaseDiagramDto.AppCreatedByCompanyId = aAppDatabaseDiagramEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDatabaseDiagramDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDatabaseDiagramEntity.AppCreatedDate);
                aAppDatabaseDiagramDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDatabaseDiagramEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDatabaseDiagramEntity, aAppDatabaseDiagramDto);
		}
		
		 /// <summary>
        ///  Copy AppDatabaseDiagramDto Properties to   AppDatabaseDiagramEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDatabaseDiagramEntity aAppDatabaseDiagramEntity,AppDatabaseDiagramDto aAppDatabaseDiagramDto)
        {        
 
      			aAppDatabaseDiagramEntity.DiagramName = aAppDatabaseDiagramDto.DiagramName;
      			aAppDatabaseDiagramEntity.Description = aAppDatabaseDiagramDto.Description;
 
  
   
    
      			aAppDatabaseDiagramEntity.AppCreatedByCompanyId = aAppDatabaseDiagramDto.AppCreatedByCompanyId;
			
			if(aAppDatabaseDiagramDto.Id == null)
			{
				aAppDatabaseDiagramEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDatabaseDiagramEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDatabaseDiagramEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDatabaseDiagramEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDatabaseDiagramEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDatabaseDiagramEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDatabaseDiagramEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDatabaseDiagramEntity, aAppDatabaseDiagramDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDatabaseDiagramEntity aAppDatabaseDiagramEntity,AppDatabaseDiagramDto aAppDatabaseDiagramDto);
		
		static partial void OnCopyDtoToEntityDone(AppDatabaseDiagramEntity aAppDatabaseDiagramEntity,AppDatabaseDiagramDto aAppDatabaseDiagramDto);
		
   
       
    }
}

 