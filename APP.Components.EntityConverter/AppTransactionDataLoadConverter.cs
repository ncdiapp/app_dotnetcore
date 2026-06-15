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
    /// Convert Properties between  AppTransactionDataLoadEntity and  AppTransactionDataLoadDto
    /// </summary>
    public static partial class AppTransactionDataLoadConverter 
    {
         /// <summary>
        ///  Convert AppTransactionDataLoadEntity To  AppTransactionDataLoadDto
        /// </summary>
        public static AppTransactionDataLoadDto ConvertEntityToDto(AppTransactionDataLoadEntity aAppTransactionDataLoadEntity)
        {        
    		AppTransactionDataLoadDto aAppTransactionDataLoadDto = new AppTransactionDataLoadDto();
    		CopyEntityPropertyToDto( aAppTransactionDataLoadEntity, aAppTransactionDataLoadDto);          
			return aAppTransactionDataLoadDto;
        }
		 /// <summary>
        ///  Convert AppTransactionDataLoadEntity To  AppTransactionDataLoadExDto
        /// </summary>
        public static AppTransactionDataLoadExDto ConvertEntityToExDto(AppTransactionDataLoadEntity aAppTransactionDataLoadEntity)
        {        
    		AppTransactionDataLoadExDto aAppTransactionDataLoadExDto = new AppTransactionDataLoadExDto();
			CopyEntityPropertyToDto( aAppTransactionDataLoadEntity, aAppTransactionDataLoadExDto);
			
			
			
            return aAppTransactionDataLoadExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionDataLoadEntity To  AppTransactionDataLoadDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionDataLoadEntity aAppTransactionDataLoadEntity,AppTransactionDataLoadDto aAppTransactionDataLoadDto)
        {        
    		
           // aAppTransactionDataLoadDto.StopChangeTracking();
 			aAppTransactionDataLoadDto.Id = aAppTransactionDataLoadEntity.DataLoadId;
 			aAppTransactionDataLoadDto.DataSetId = aAppTransactionDataLoadEntity.DataSetId;
 			aAppTransactionDataLoadDto.TransactionUnitId = aAppTransactionDataLoadEntity.TransactionUnitId;
 			aAppTransactionDataLoadDto.TransactionId = aAppTransactionDataLoadEntity.TransactionId;
 			aAppTransactionDataLoadDto.LoadName = aAppTransactionDataLoadEntity.LoadName;
 			aAppTransactionDataLoadDto.Description = aAppTransactionDataLoadEntity.Description;
 			aAppTransactionDataLoadDto.LoadOrder = aAppTransactionDataLoadEntity.LoadOrder;
 			aAppTransactionDataLoadDto.AppCreatedById = aAppTransactionDataLoadEntity.AppCreatedById;
 			aAppTransactionDataLoadDto.AppCreatedDate = aAppTransactionDataLoadEntity.AppCreatedDate;
 			aAppTransactionDataLoadDto.AppModifiedDate = aAppTransactionDataLoadEntity.AppModifiedDate;
 			aAppTransactionDataLoadDto.AppModifiedById = aAppTransactionDataLoadEntity.AppModifiedById;
 			aAppTransactionDataLoadDto.AppCreatedByCompanyId = aAppTransactionDataLoadEntity.AppCreatedByCompanyId;
 			aAppTransactionDataLoadDto.IsAutoExcutedWhenOpenEditForm = aAppTransactionDataLoadEntity.IsAutoExcutedWhenOpenEditForm;
 			aAppTransactionDataLoadDto.IsAutoExecuteBeforeIntialCscading = aAppTransactionDataLoadEntity.IsAutoExecuteBeforeIntialCscading;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionDataLoadDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionDataLoadEntity.AppCreatedDate);
                aAppTransactionDataLoadDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionDataLoadEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionDataLoadEntity, aAppTransactionDataLoadDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionDataLoadDto Properties to   AppTransactionDataLoadEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionDataLoadEntity aAppTransactionDataLoadEntity,AppTransactionDataLoadDto aAppTransactionDataLoadDto)
        {        
 
      			aAppTransactionDataLoadEntity.DataSetId = aAppTransactionDataLoadDto.DataSetId;
      			aAppTransactionDataLoadEntity.TransactionUnitId = aAppTransactionDataLoadDto.TransactionUnitId;
      			aAppTransactionDataLoadEntity.TransactionId = aAppTransactionDataLoadDto.TransactionId;
      			aAppTransactionDataLoadEntity.LoadName = aAppTransactionDataLoadDto.LoadName;
      			aAppTransactionDataLoadEntity.Description = aAppTransactionDataLoadDto.Description;
      			aAppTransactionDataLoadEntity.LoadOrder = aAppTransactionDataLoadDto.LoadOrder;
 
  
   
    
      			aAppTransactionDataLoadEntity.AppCreatedByCompanyId = aAppTransactionDataLoadDto.AppCreatedByCompanyId;
      			aAppTransactionDataLoadEntity.IsAutoExcutedWhenOpenEditForm = aAppTransactionDataLoadDto.IsAutoExcutedWhenOpenEditForm;
      			aAppTransactionDataLoadEntity.IsAutoExecuteBeforeIntialCscading = aAppTransactionDataLoadDto.IsAutoExecuteBeforeIntialCscading;
			
			if(aAppTransactionDataLoadDto.Id == null)
			{
				aAppTransactionDataLoadEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionDataLoadEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionDataLoadEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionDataLoadEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionDataLoadEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionDataLoadEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionDataLoadEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionDataLoadEntity, aAppTransactionDataLoadDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionDataLoadEntity aAppTransactionDataLoadEntity,AppTransactionDataLoadDto aAppTransactionDataLoadDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionDataLoadEntity aAppTransactionDataLoadEntity,AppTransactionDataLoadDto aAppTransactionDataLoadDto);
		
   
       
    }
}

 