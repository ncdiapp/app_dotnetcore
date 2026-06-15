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
    /// Convert Properties between  AppDataSetParameterEntity and  AppDataSetParameterDto
    /// </summary>
    public static partial class AppDataSetParameterConverter 
    {
         /// <summary>
        ///  Convert AppDataSetParameterEntity To  AppDataSetParameterDto
        /// </summary>
        public static AppDataSetParameterDto ConvertEntityToDto(AppDataSetParameterEntity aAppDataSetParameterEntity)
        {        
    		AppDataSetParameterDto aAppDataSetParameterDto = new AppDataSetParameterDto();
    		CopyEntityPropertyToDto( aAppDataSetParameterEntity, aAppDataSetParameterDto);          
			return aAppDataSetParameterDto;
        }
		 /// <summary>
        ///  Convert AppDataSetParameterEntity To  AppDataSetParameterExDto
        /// </summary>
        public static AppDataSetParameterExDto ConvertEntityToExDto(AppDataSetParameterEntity aAppDataSetParameterEntity)
        {        
    		AppDataSetParameterExDto aAppDataSetParameterExDto = new AppDataSetParameterExDto();
			CopyEntityPropertyToDto( aAppDataSetParameterEntity, aAppDataSetParameterExDto);
			
			
			
            return aAppDataSetParameterExDto;
        }
		
		 /// <summary>
        ///  Convert AppDataSetParameterEntity To  AppDataSetParameterDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppDataSetParameterEntity aAppDataSetParameterEntity,AppDataSetParameterDto aAppDataSetParameterDto)
        {        
    		
           // aAppDataSetParameterDto.StopChangeTracking();
 			aAppDataSetParameterDto.Id = aAppDataSetParameterEntity.ParameterId;
 			aAppDataSetParameterDto.DataSetId = aAppDataSetParameterEntity.DataSetId;
 			aAppDataSetParameterDto.ParameterName = aAppDataSetParameterEntity.ParameterName;
 			aAppDataSetParameterDto.DataType = aAppDataSetParameterEntity.DataType;
 			aAppDataSetParameterDto.DirectionInOut = aAppDataSetParameterEntity.DirectionInOut;
 			aAppDataSetParameterDto.DefautValue = aAppDataSetParameterEntity.DefautValue;
 			aAppDataSetParameterDto.AppCreatedById = aAppDataSetParameterEntity.AppCreatedById;
 			aAppDataSetParameterDto.AppCreatedDate = aAppDataSetParameterEntity.AppCreatedDate;
 			aAppDataSetParameterDto.AppModifiedDate = aAppDataSetParameterEntity.AppModifiedDate;
 			aAppDataSetParameterDto.AppModifiedById = aAppDataSetParameterEntity.AppModifiedById;
 			aAppDataSetParameterDto.AppCreatedByCompanyId = aAppDataSetParameterEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppDataSetParameterDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDataSetParameterEntity.AppCreatedDate);
                aAppDataSetParameterDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppDataSetParameterEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppDataSetParameterEntity, aAppDataSetParameterDto);
		}
		
		 /// <summary>
        ///  Copy AppDataSetParameterDto Properties to   AppDataSetParameterEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppDataSetParameterEntity aAppDataSetParameterEntity,AppDataSetParameterDto aAppDataSetParameterDto)
        {        
 
      			aAppDataSetParameterEntity.DataSetId = aAppDataSetParameterDto.DataSetId;
      			aAppDataSetParameterEntity.ParameterName = aAppDataSetParameterDto.ParameterName;
      			aAppDataSetParameterEntity.DataType = aAppDataSetParameterDto.DataType;
      			aAppDataSetParameterEntity.DirectionInOut = aAppDataSetParameterDto.DirectionInOut;
      			aAppDataSetParameterEntity.DefautValue = aAppDataSetParameterDto.DefautValue;
 
  
   
    
      			aAppDataSetParameterEntity.AppCreatedByCompanyId = aAppDataSetParameterDto.AppCreatedByCompanyId;
			
			if(aAppDataSetParameterDto.Id == null)
			{
				aAppDataSetParameterEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppDataSetParameterEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppDataSetParameterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDataSetParameterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppDataSetParameterEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppDataSetParameterEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppDataSetParameterEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppDataSetParameterEntity, aAppDataSetParameterDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppDataSetParameterEntity aAppDataSetParameterEntity,AppDataSetParameterDto aAppDataSetParameterDto);
		
		static partial void OnCopyDtoToEntityDone(AppDataSetParameterEntity aAppDataSetParameterEntity,AppDataSetParameterDto aAppDataSetParameterDto);
		
   
       
    }
}

 