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
    /// Convert Properties between  AppTransactionPostProcessEntity and  AppTransactionPostProcessDto
    /// </summary>
    public static partial class AppTransactionPostProcessConverter 
    {
         /// <summary>
        ///  Convert AppTransactionPostProcessEntity To  AppTransactionPostProcessDto
        /// </summary>
        public static AppTransactionPostProcessDto ConvertEntityToDto(AppTransactionPostProcessEntity aAppTransactionPostProcessEntity)
        {        
    		AppTransactionPostProcessDto aAppTransactionPostProcessDto = new AppTransactionPostProcessDto();
    		CopyEntityPropertyToDto( aAppTransactionPostProcessEntity, aAppTransactionPostProcessDto);          
			return aAppTransactionPostProcessDto;
        }
		 /// <summary>
        ///  Convert AppTransactionPostProcessEntity To  AppTransactionPostProcessExDto
        /// </summary>
        public static AppTransactionPostProcessExDto ConvertEntityToExDto(AppTransactionPostProcessEntity aAppTransactionPostProcessEntity)
        {        
    		AppTransactionPostProcessExDto aAppTransactionPostProcessExDto = new AppTransactionPostProcessExDto();
			CopyEntityPropertyToDto( aAppTransactionPostProcessEntity, aAppTransactionPostProcessExDto);
			
			
			
            return aAppTransactionPostProcessExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionPostProcessEntity To  AppTransactionPostProcessDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionPostProcessEntity aAppTransactionPostProcessEntity,AppTransactionPostProcessDto aAppTransactionPostProcessDto)
        {        
    		
           // aAppTransactionPostProcessDto.StopChangeTracking();
 			aAppTransactionPostProcessDto.Id = aAppTransactionPostProcessEntity.PostProcessId;
 			aAppTransactionPostProcessDto.Name = aAppTransactionPostProcessEntity.Name;
 			aAppTransactionPostProcessDto.TransactionId = aAppTransactionPostProcessEntity.TransactionId;
 			aAppTransactionPostProcessDto.ProcessFlow = aAppTransactionPostProcessEntity.ProcessFlow;
 			aAppTransactionPostProcessDto.PostStoreProcedureName = aAppTransactionPostProcessEntity.PostStoreProcedureName;
 			aAppTransactionPostProcessDto.ExternalCommand = aAppTransactionPostProcessEntity.ExternalCommand;
 			aAppTransactionPostProcessDto.InternalMethod = aAppTransactionPostProcessEntity.InternalMethod;
 			aAppTransactionPostProcessDto.RootUnitId = aAppTransactionPostProcessEntity.RootUnitId;
 			aAppTransactionPostProcessDto.ParameterOptions = aAppTransactionPostProcessEntity.ParameterOptions;
 			aAppTransactionPostProcessDto.AppCreatedById = aAppTransactionPostProcessEntity.AppCreatedById;
 			aAppTransactionPostProcessDto.AppCreatedDate = aAppTransactionPostProcessEntity.AppCreatedDate;
 			aAppTransactionPostProcessDto.AppModifiedDate = aAppTransactionPostProcessEntity.AppModifiedDate;
 			aAppTransactionPostProcessDto.AppModifiedById = aAppTransactionPostProcessEntity.AppModifiedById;
 			aAppTransactionPostProcessDto.AppCreatedByCompanyId = aAppTransactionPostProcessEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionPostProcessDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionPostProcessEntity.AppCreatedDate);
                aAppTransactionPostProcessDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionPostProcessEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionPostProcessEntity, aAppTransactionPostProcessDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionPostProcessDto Properties to   AppTransactionPostProcessEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionPostProcessEntity aAppTransactionPostProcessEntity,AppTransactionPostProcessDto aAppTransactionPostProcessDto)
        {        
 
      			aAppTransactionPostProcessEntity.Name = aAppTransactionPostProcessDto.Name;
      			aAppTransactionPostProcessEntity.TransactionId = aAppTransactionPostProcessDto.TransactionId;
      			aAppTransactionPostProcessEntity.ProcessFlow = aAppTransactionPostProcessDto.ProcessFlow;
      			aAppTransactionPostProcessEntity.PostStoreProcedureName = aAppTransactionPostProcessDto.PostStoreProcedureName;
      			aAppTransactionPostProcessEntity.ExternalCommand = aAppTransactionPostProcessDto.ExternalCommand;
      			aAppTransactionPostProcessEntity.InternalMethod = aAppTransactionPostProcessDto.InternalMethod;
      			aAppTransactionPostProcessEntity.RootUnitId = aAppTransactionPostProcessDto.RootUnitId;
      			aAppTransactionPostProcessEntity.ParameterOptions = aAppTransactionPostProcessDto.ParameterOptions;
 
  
   
    
      			aAppTransactionPostProcessEntity.AppCreatedByCompanyId = aAppTransactionPostProcessDto.AppCreatedByCompanyId;
			
			if(aAppTransactionPostProcessDto.Id == null)
			{
				aAppTransactionPostProcessEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionPostProcessEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionPostProcessEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionPostProcessEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionPostProcessEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionPostProcessEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionPostProcessEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionPostProcessEntity, aAppTransactionPostProcessDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionPostProcessEntity aAppTransactionPostProcessEntity,AppTransactionPostProcessDto aAppTransactionPostProcessDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionPostProcessEntity aAppTransactionPostProcessEntity,AppTransactionPostProcessDto aAppTransactionPostProcessDto);
		
   
       
    }
}

 