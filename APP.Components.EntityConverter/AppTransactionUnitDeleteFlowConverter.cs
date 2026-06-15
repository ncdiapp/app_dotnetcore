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
    /// Convert Properties between  AppTransactionUnitDeleteFlowEntity and  AppTransactionUnitDeleteFlowDto
    /// </summary>
    public static partial class AppTransactionUnitDeleteFlowConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitDeleteFlowEntity To  AppTransactionUnitDeleteFlowDto
        /// </summary>
        public static AppTransactionUnitDeleteFlowDto ConvertEntityToDto(AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity)
        {        
    		AppTransactionUnitDeleteFlowDto aAppTransactionUnitDeleteFlowDto = new AppTransactionUnitDeleteFlowDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitDeleteFlowEntity, aAppTransactionUnitDeleteFlowDto);          
			return aAppTransactionUnitDeleteFlowDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitDeleteFlowEntity To  AppTransactionUnitDeleteFlowExDto
        /// </summary>
        public static AppTransactionUnitDeleteFlowExDto ConvertEntityToExDto(AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity)
        {        
    		AppTransactionUnitDeleteFlowExDto aAppTransactionUnitDeleteFlowExDto = new AppTransactionUnitDeleteFlowExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitDeleteFlowEntity, aAppTransactionUnitDeleteFlowExDto);
			
			
			
            return aAppTransactionUnitDeleteFlowExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitDeleteFlowEntity To  AppTransactionUnitDeleteFlowDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity,AppTransactionUnitDeleteFlowDto aAppTransactionUnitDeleteFlowDto)
        {        
    		
           // aAppTransactionUnitDeleteFlowDto.StopChangeTracking();
 			aAppTransactionUnitDeleteFlowDto.Id = aAppTransactionUnitDeleteFlowEntity.DeleteFlowId;
 			aAppTransactionUnitDeleteFlowDto.TransactionUnitId = aAppTransactionUnitDeleteFlowEntity.TransactionUnitId;
 			aAppTransactionUnitDeleteFlowDto.RelativeTableName = aAppTransactionUnitDeleteFlowEntity.RelativeTableName;
 			aAppTransactionUnitDeleteFlowDto.RelativeForeignKeyName = aAppTransactionUnitDeleteFlowEntity.RelativeForeignKeyName;
 			aAppTransactionUnitDeleteFlowDto.IsForcedDelete = aAppTransactionUnitDeleteFlowEntity.IsForcedDelete;
 			aAppTransactionUnitDeleteFlowDto.IsSetEmpty = aAppTransactionUnitDeleteFlowEntity.IsSetEmpty;
 			aAppTransactionUnitDeleteFlowDto.IsNotAllowedDeleteWithMsg = aAppTransactionUnitDeleteFlowEntity.IsNotAllowedDeleteWithMsg;
 			aAppTransactionUnitDeleteFlowDto.DeleteFlowPriority = aAppTransactionUnitDeleteFlowEntity.DeleteFlowPriority;
 			aAppTransactionUnitDeleteFlowDto.WarningMessage = aAppTransactionUnitDeleteFlowEntity.WarningMessage;
 			aAppTransactionUnitDeleteFlowDto.StoredProcedureName = aAppTransactionUnitDeleteFlowEntity.StoredProcedureName;
 			aAppTransactionUnitDeleteFlowDto.SpParameterOptions = aAppTransactionUnitDeleteFlowEntity.SpParameterOptions;
 			aAppTransactionUnitDeleteFlowDto.DeleteValidationStoredProcedureName = aAppTransactionUnitDeleteFlowEntity.DeleteValidationStoredProcedureName;
 			aAppTransactionUnitDeleteFlowDto.AppCreatedById = aAppTransactionUnitDeleteFlowEntity.AppCreatedById;
 			aAppTransactionUnitDeleteFlowDto.AppCreatedDate = aAppTransactionUnitDeleteFlowEntity.AppCreatedDate;
 			aAppTransactionUnitDeleteFlowDto.AppModifiedDate = aAppTransactionUnitDeleteFlowEntity.AppModifiedDate;
 			aAppTransactionUnitDeleteFlowDto.AppModifiedById = aAppTransactionUnitDeleteFlowEntity.AppModifiedById;
 			aAppTransactionUnitDeleteFlowDto.AppCreatedByCompanyId = aAppTransactionUnitDeleteFlowEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitDeleteFlowDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitDeleteFlowEntity.AppCreatedDate);
                aAppTransactionUnitDeleteFlowDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitDeleteFlowEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitDeleteFlowEntity, aAppTransactionUnitDeleteFlowDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitDeleteFlowDto Properties to   AppTransactionUnitDeleteFlowEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity,AppTransactionUnitDeleteFlowDto aAppTransactionUnitDeleteFlowDto)
        {        
 
      			aAppTransactionUnitDeleteFlowEntity.TransactionUnitId = aAppTransactionUnitDeleteFlowDto.TransactionUnitId;
      			aAppTransactionUnitDeleteFlowEntity.RelativeTableName = aAppTransactionUnitDeleteFlowDto.RelativeTableName;
      			aAppTransactionUnitDeleteFlowEntity.RelativeForeignKeyName = aAppTransactionUnitDeleteFlowDto.RelativeForeignKeyName;
      			aAppTransactionUnitDeleteFlowEntity.IsForcedDelete = aAppTransactionUnitDeleteFlowDto.IsForcedDelete;
      			aAppTransactionUnitDeleteFlowEntity.IsSetEmpty = aAppTransactionUnitDeleteFlowDto.IsSetEmpty;
      			aAppTransactionUnitDeleteFlowEntity.IsNotAllowedDeleteWithMsg = aAppTransactionUnitDeleteFlowDto.IsNotAllowedDeleteWithMsg;
      			aAppTransactionUnitDeleteFlowEntity.DeleteFlowPriority = aAppTransactionUnitDeleteFlowDto.DeleteFlowPriority;
      			aAppTransactionUnitDeleteFlowEntity.WarningMessage = aAppTransactionUnitDeleteFlowDto.WarningMessage;
      			aAppTransactionUnitDeleteFlowEntity.StoredProcedureName = aAppTransactionUnitDeleteFlowDto.StoredProcedureName;
      			aAppTransactionUnitDeleteFlowEntity.SpParameterOptions = aAppTransactionUnitDeleteFlowDto.SpParameterOptions;
      			aAppTransactionUnitDeleteFlowEntity.DeleteValidationStoredProcedureName = aAppTransactionUnitDeleteFlowDto.DeleteValidationStoredProcedureName;
 
  
   
    
      			aAppTransactionUnitDeleteFlowEntity.AppCreatedByCompanyId = aAppTransactionUnitDeleteFlowDto.AppCreatedByCompanyId;
			
			if(aAppTransactionUnitDeleteFlowDto.Id == null)
			{
				aAppTransactionUnitDeleteFlowEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitDeleteFlowEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitDeleteFlowEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitDeleteFlowEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitDeleteFlowEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitDeleteFlowEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitDeleteFlowEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitDeleteFlowEntity, aAppTransactionUnitDeleteFlowDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity,AppTransactionUnitDeleteFlowDto aAppTransactionUnitDeleteFlowDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitDeleteFlowEntity aAppTransactionUnitDeleteFlowEntity,AppTransactionUnitDeleteFlowDto aAppTransactionUnitDeleteFlowDto);
		
   
       
    }
}

 