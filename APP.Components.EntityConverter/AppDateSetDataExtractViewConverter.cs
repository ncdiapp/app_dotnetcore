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
    /// Convert Properties between  AppDateSetDataExtractViewEntity and  AppDateSetDataExtractViewDto
    /// </summary>
    public static partial class AppDateSetDataExtractViewConverter 
    {
         /// <summary>
        ///  Convert AppDateSetDataExtractViewEntity To  AppDateSetDataExtractViewDto
        /// </summary>
        public static AppDateSetDataExtractViewDto ConvertEntityToDto(AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity)
        {        
    		AppDateSetDataExtractViewDto aAppDateSetDataExtractViewDto = new AppDateSetDataExtractViewDto();
    		CopyEntityPropertyToDto( aAppDateSetDataExtractViewEntity, aAppDateSetDataExtractViewDto);          
			return aAppDateSetDataExtractViewDto;
        }
		 /// <summary>
        ///  Convert AppDateSetDataExtractViewEntity To  AppDateSetDataExtractViewExDto
        /// </summary>
        public static AppDateSetDataExtractViewExDto ConvertEntityToExDto(AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity)
        {        
    		AppDateSetDataExtractViewExDto aAppDateSetDataExtractViewExDto = new AppDateSetDataExtractViewExDto();
			CopyEntityPropertyToDto( aAppDateSetDataExtractViewEntity, aAppDateSetDataExtractViewExDto);
			
			
			
            return aAppDateSetDataExtractViewExDto;
        }
		
		 /// <summary>
        ///  Convert AppDateSetDataExtractViewEntity To  AppDateSetDataExtractViewDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity,AppDateSetDataExtractViewDto aAppDateSetDataExtractViewDto)
        {        
    		
           // aAppDateSetDataExtractViewDto.StopChangeTracking();
 			aAppDateSetDataExtractViewDto.Id = aAppDateSetDataExtractViewEntity.ExtractViewId;
 			aAppDateSetDataExtractViewDto.Description = aAppDateSetDataExtractViewEntity.Description;
 			aAppDateSetDataExtractViewDto.DataSetId = aAppDateSetDataExtractViewEntity.DataSetId;
 			aAppDateSetDataExtractViewDto.DbfiledName = aAppDateSetDataExtractViewEntity.DbfiledName;
 			aAppDateSetDataExtractViewDto.IsGroup = aAppDateSetDataExtractViewEntity.IsGroup;
 			aAppDateSetDataExtractViewDto.AggFunction = aAppDateSetDataExtractViewEntity.AggFunction;
 			aAppDateSetDataExtractViewDto.AppCreatedById = aAppDateSetDataExtractViewEntity.AppCreatedById;
 			aAppDateSetDataExtractViewDto.AppCreatedDate = aAppDateSetDataExtractViewEntity.AppCreatedDate;
 			aAppDateSetDataExtractViewDto.AppModifiedDate = aAppDateSetDataExtractViewEntity.AppModifiedDate;
 			aAppDateSetDataExtractViewDto.AppModifiedById = aAppDateSetDataExtractViewEntity.AppModifiedById;
 			aAppDateSetDataExtractViewDto.AppCreatedByCompanyId = aAppDateSetDataExtractViewEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDateSetDataExtractViewDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDateSetDataExtractViewEntity.AppCreatedDate);
                aAppDateSetDataExtractViewDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDateSetDataExtractViewEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDateSetDataExtractViewEntity, aAppDateSetDataExtractViewDto);
		}
		
		 /// <summary>
        ///  Copy AppDateSetDataExtractViewDto Properties to   AppDateSetDataExtractViewEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity,AppDateSetDataExtractViewDto aAppDateSetDataExtractViewDto)
        {        
 
      			aAppDateSetDataExtractViewEntity.Description = aAppDateSetDataExtractViewDto.Description;
      			aAppDateSetDataExtractViewEntity.DataSetId = aAppDateSetDataExtractViewDto.DataSetId;
      			aAppDateSetDataExtractViewEntity.DbfiledName = aAppDateSetDataExtractViewDto.DbfiledName;
      			aAppDateSetDataExtractViewEntity.IsGroup = aAppDateSetDataExtractViewDto.IsGroup;
      			aAppDateSetDataExtractViewEntity.AggFunction = aAppDateSetDataExtractViewDto.AggFunction;
 
  
   
    
      			aAppDateSetDataExtractViewEntity.AppCreatedByCompanyId = aAppDateSetDataExtractViewDto.AppCreatedByCompanyId;
			
			if(aAppDateSetDataExtractViewDto.Id == null)
			{
				aAppDateSetDataExtractViewEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDateSetDataExtractViewEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDateSetDataExtractViewEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDateSetDataExtractViewEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDateSetDataExtractViewEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDateSetDataExtractViewEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDateSetDataExtractViewEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDateSetDataExtractViewEntity, aAppDateSetDataExtractViewDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity,AppDateSetDataExtractViewDto aAppDateSetDataExtractViewDto);
		
		static partial void OnCopyDtoToEntityDone(AppDateSetDataExtractViewEntity aAppDateSetDataExtractViewEntity,AppDateSetDataExtractViewDto aAppDateSetDataExtractViewDto);
		
   
       
    }
}

 