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
    /// Convert Properties between  AppLogTrackEntity and  AppLogTrackDto
    /// </summary>
    public static partial class AppLogTrackConverter 
    {
         /// <summary>
        ///  Convert AppLogTrackEntity To  AppLogTrackDto
        /// </summary>
        public static AppLogTrackDto ConvertEntityToDto(AppLogTrackEntity aAppLogTrackEntity)
        {        
    		AppLogTrackDto aAppLogTrackDto = new AppLogTrackDto();
    		CopyEntityPropertyToDto( aAppLogTrackEntity, aAppLogTrackDto);          
			return aAppLogTrackDto;
        }
		 /// <summary>
        ///  Convert AppLogTrackEntity To  AppLogTrackExDto
        /// </summary>
        public static AppLogTrackExDto ConvertEntityToExDto(AppLogTrackEntity aAppLogTrackEntity)
        {        
    		AppLogTrackExDto aAppLogTrackExDto = new AppLogTrackExDto();
			CopyEntityPropertyToDto( aAppLogTrackEntity, aAppLogTrackExDto);
			
			
			
            return aAppLogTrackExDto;
        }
		
		 /// <summary>
        ///  Convert AppLogTrackEntity To  AppLogTrackDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppLogTrackEntity aAppLogTrackEntity,AppLogTrackDto aAppLogTrackDto)
        {        
    		
           // aAppLogTrackDto.StopChangeTracking();
 			aAppLogTrackDto.Id = aAppLogTrackEntity.TrackId;
 			aAppLogTrackDto.Catalogue = aAppLogTrackEntity.Catalogue;
 			aAppLogTrackDto.Description = aAppLogTrackEntity.Description;
 			aAppLogTrackDto.StatusCode = aAppLogTrackEntity.StatusCode;
 			aAppLogTrackDto.Message = aAppLogTrackEntity.Message;
 			aAppLogTrackDto.OtherInfo = aAppLogTrackEntity.OtherInfo;
 			aAppLogTrackDto.LogDate = aAppLogTrackEntity.LogDate;
 			aAppLogTrackDto.CommandId = aAppLogTrackEntity.CommandId;
 			aAppLogTrackDto.TransactionId = aAppLogTrackEntity.TransactionId;
 			aAppLogTrackDto.TransactionRid = aAppLogTrackEntity.TransactionRid;
 			aAppLogTrackDto.BatchNumber = aAppLogTrackEntity.BatchNumber;
 			aAppLogTrackDto.TransactionName = aAppLogTrackEntity.TransactionName;
 			aAppLogTrackDto.AppCreatedById = aAppLogTrackEntity.AppCreatedById;
 			aAppLogTrackDto.AppModifiedById = aAppLogTrackEntity.AppModifiedById;
 			aAppLogTrackDto.AppCreatedByCompanyId = aAppLogTrackEntity.AppCreatedByCompanyId;
 			aAppLogTrackDto.AppCreatedDate = aAppLogTrackEntity.AppCreatedDate;
 			aAppLogTrackDto.AppModifiedDate = aAppLogTrackEntity.AppModifiedDate;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppLogTrackDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppLogTrackEntity.AppCreatedDate);
                aAppLogTrackDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppLogTrackEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppLogTrackEntity, aAppLogTrackDto);
		}
		
		 /// <summary>
        ///  Copy AppLogTrackDto Properties to   AppLogTrackEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppLogTrackEntity aAppLogTrackEntity,AppLogTrackDto aAppLogTrackDto)
        {        
 
      			aAppLogTrackEntity.Catalogue = aAppLogTrackDto.Catalogue;
      			aAppLogTrackEntity.Description = aAppLogTrackDto.Description;
      			aAppLogTrackEntity.StatusCode = aAppLogTrackDto.StatusCode;
      			aAppLogTrackEntity.Message = aAppLogTrackDto.Message;
      			aAppLogTrackEntity.OtherInfo = aAppLogTrackDto.OtherInfo;
      			aAppLogTrackEntity.LogDate = aAppLogTrackDto.LogDate;
      			aAppLogTrackEntity.CommandId = aAppLogTrackDto.CommandId;
      			aAppLogTrackEntity.TransactionId = aAppLogTrackDto.TransactionId;
      			aAppLogTrackEntity.TransactionRid = aAppLogTrackDto.TransactionRid;
      			aAppLogTrackEntity.BatchNumber = aAppLogTrackDto.BatchNumber;
      			aAppLogTrackEntity.TransactionName = aAppLogTrackDto.TransactionName;
 
    
      			aAppLogTrackEntity.AppCreatedByCompanyId = aAppLogTrackDto.AppCreatedByCompanyId;
  
   
			
			if(aAppLogTrackDto.Id == null)
			{
				aAppLogTrackEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppLogTrackEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppLogTrackEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppLogTrackEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppLogTrackEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppLogTrackEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppLogTrackEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppLogTrackEntity, aAppLogTrackDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppLogTrackEntity aAppLogTrackEntity,AppLogTrackDto aAppLogTrackDto);
		
		static partial void OnCopyDtoToEntityDone(AppLogTrackEntity aAppLogTrackEntity,AppLogTrackDto aAppLogTrackDto);
		
   
       
    }
}

 