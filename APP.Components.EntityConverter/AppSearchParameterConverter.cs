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
    /// Convert Properties between  AppSearchParameterEntity and  AppSearchParameterDto
    /// </summary>
    public static partial class AppSearchParameterConverter 
    {
         /// <summary>
        ///  Convert AppSearchParameterEntity To  AppSearchParameterDto
        /// </summary>
        public static AppSearchParameterDto ConvertEntityToDto(AppSearchParameterEntity aAppSearchParameterEntity)
        {        
    		AppSearchParameterDto aAppSearchParameterDto = new AppSearchParameterDto();
    		CopyEntityPropertyToDto( aAppSearchParameterEntity, aAppSearchParameterDto);          
			return aAppSearchParameterDto;
        }
		 /// <summary>
        ///  Convert AppSearchParameterEntity To  AppSearchParameterExDto
        /// </summary>
        public static AppSearchParameterExDto ConvertEntityToExDto(AppSearchParameterEntity aAppSearchParameterEntity)
        {        
    		AppSearchParameterExDto aAppSearchParameterExDto = new AppSearchParameterExDto();
			CopyEntityPropertyToDto( aAppSearchParameterEntity, aAppSearchParameterExDto);
			
			
			
            return aAppSearchParameterExDto;
        }
		
		 /// <summary>
        ///  Convert AppSearchParameterEntity To  AppSearchParameterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSearchParameterEntity aAppSearchParameterEntity,AppSearchParameterDto aAppSearchParameterDto)
        {        
    		
           // aAppSearchParameterDto.StopChangeTracking();
 			aAppSearchParameterDto.Id = aAppSearchParameterEntity.SearchparameterId;
 			aAppSearchParameterDto.ParameterId = aAppSearchParameterEntity.ParameterId;
 			aAppSearchParameterDto.SearchFieldId = aAppSearchParameterEntity.SearchFieldId;
 			aAppSearchParameterDto.DefaultValue = aAppSearchParameterEntity.DefaultValue;
 			aAppSearchParameterDto.SearchId = aAppSearchParameterEntity.SearchId;
 			aAppSearchParameterDto.AppCreatedById = aAppSearchParameterEntity.AppCreatedById;
 			aAppSearchParameterDto.AppCreatedDate = aAppSearchParameterEntity.AppCreatedDate;
 			aAppSearchParameterDto.AppModifiedDate = aAppSearchParameterEntity.AppModifiedDate;
 			aAppSearchParameterDto.AppModifiedById = aAppSearchParameterEntity.AppModifiedById;
 			aAppSearchParameterDto.AppCreatedByCompanyId = aAppSearchParameterEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSearchParameterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchParameterEntity.AppCreatedDate);
                aAppSearchParameterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSearchParameterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSearchParameterEntity, aAppSearchParameterDto);
		}
		
		 /// <summary>
        ///  Copy AppSearchParameterDto Properties to   AppSearchParameterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSearchParameterEntity aAppSearchParameterEntity,AppSearchParameterDto aAppSearchParameterDto)
        {        
 
      			aAppSearchParameterEntity.ParameterId = aAppSearchParameterDto.ParameterId;
      			aAppSearchParameterEntity.SearchFieldId = aAppSearchParameterDto.SearchFieldId;
      			aAppSearchParameterEntity.DefaultValue = aAppSearchParameterDto.DefaultValue;
      			aAppSearchParameterEntity.SearchId = aAppSearchParameterDto.SearchId;
 
  
   
    
      			aAppSearchParameterEntity.AppCreatedByCompanyId = aAppSearchParameterDto.AppCreatedByCompanyId;
			
			if(aAppSearchParameterDto.Id == null)
			{
				aAppSearchParameterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchParameterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSearchParameterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchParameterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSearchParameterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSearchParameterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSearchParameterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSearchParameterEntity, aAppSearchParameterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSearchParameterEntity aAppSearchParameterEntity,AppSearchParameterDto aAppSearchParameterDto);
		
		static partial void OnCopyDtoToEntityDone(AppSearchParameterEntity aAppSearchParameterEntity,AppSearchParameterDto aAppSearchParameterDto);
		
   
       
    }
}

 