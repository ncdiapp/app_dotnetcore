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
    /// Convert Properties between  AppTransactionFieldAggFunctionEntity and  AppTransactionFieldAggFunctionDto
    /// </summary>
    public static partial class AppTransactionFieldAggFunctionConverter 
    {
         /// <summary>
        ///  Convert AppTransactionFieldAggFunctionEntity To  AppTransactionFieldAggFunctionDto
        /// </summary>
        public static AppTransactionFieldAggFunctionDto ConvertEntityToDto(AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity)
        {        
    		AppTransactionFieldAggFunctionDto aAppTransactionFieldAggFunctionDto = new AppTransactionFieldAggFunctionDto();
    		CopyEntityPropertyToDto( aAppTransactionFieldAggFunctionEntity, aAppTransactionFieldAggFunctionDto);          
			return aAppTransactionFieldAggFunctionDto;
        }
		 /// <summary>
        ///  Convert AppTransactionFieldAggFunctionEntity To  AppTransactionFieldAggFunctionExDto
        /// </summary>
        public static AppTransactionFieldAggFunctionExDto ConvertEntityToExDto(AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity)
        {        
    		AppTransactionFieldAggFunctionExDto aAppTransactionFieldAggFunctionExDto = new AppTransactionFieldAggFunctionExDto();
			CopyEntityPropertyToDto( aAppTransactionFieldAggFunctionEntity, aAppTransactionFieldAggFunctionExDto);
			
			
			
            return aAppTransactionFieldAggFunctionExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionFieldAggFunctionEntity To  AppTransactionFieldAggFunctionDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity,AppTransactionFieldAggFunctionDto aAppTransactionFieldAggFunctionDto)
        {        
    		
           // aAppTransactionFieldAggFunctionDto.StopChangeTracking();
 			aAppTransactionFieldAggFunctionDto.Id = aAppTransactionFieldAggFunctionEntity.FieldAggFunctionId;
 			aAppTransactionFieldAggFunctionDto.AggregationFunctionType = aAppTransactionFieldAggFunctionEntity.AggregationFunctionType;
 			aAppTransactionFieldAggFunctionDto.TransactionFieldId = aAppTransactionFieldAggFunctionEntity.TransactionFieldId;
 			aAppTransactionFieldAggFunctionDto.AppCreatedById = aAppTransactionFieldAggFunctionEntity.AppCreatedById;
 			aAppTransactionFieldAggFunctionDto.AppCreatedDate = aAppTransactionFieldAggFunctionEntity.AppCreatedDate;
 			aAppTransactionFieldAggFunctionDto.AppModifiedDate = aAppTransactionFieldAggFunctionEntity.AppModifiedDate;
 			aAppTransactionFieldAggFunctionDto.AppModifiedById = aAppTransactionFieldAggFunctionEntity.AppModifiedById;
 			aAppTransactionFieldAggFunctionDto.AppCreatedByCompanyId = aAppTransactionFieldAggFunctionEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionFieldAggFunctionDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionFieldAggFunctionEntity.AppCreatedDate);
                aAppTransactionFieldAggFunctionDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionFieldAggFunctionEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionFieldAggFunctionEntity, aAppTransactionFieldAggFunctionDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionFieldAggFunctionDto Properties to   AppTransactionFieldAggFunctionEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity,AppTransactionFieldAggFunctionDto aAppTransactionFieldAggFunctionDto)
        {        
 
      			aAppTransactionFieldAggFunctionEntity.AggregationFunctionType = aAppTransactionFieldAggFunctionDto.AggregationFunctionType;
      			aAppTransactionFieldAggFunctionEntity.TransactionFieldId = aAppTransactionFieldAggFunctionDto.TransactionFieldId;
 
  
   
    
      			aAppTransactionFieldAggFunctionEntity.AppCreatedByCompanyId = aAppTransactionFieldAggFunctionDto.AppCreatedByCompanyId;
			
			if(aAppTransactionFieldAggFunctionDto.Id == null)
			{
				aAppTransactionFieldAggFunctionEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionFieldAggFunctionEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionFieldAggFunctionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionFieldAggFunctionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionFieldAggFunctionEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionFieldAggFunctionEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionFieldAggFunctionEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionFieldAggFunctionEntity, aAppTransactionFieldAggFunctionDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity,AppTransactionFieldAggFunctionDto aAppTransactionFieldAggFunctionDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionFieldAggFunctionEntity aAppTransactionFieldAggFunctionEntity,AppTransactionFieldAggFunctionDto aAppTransactionFieldAggFunctionDto);
		
   
       
    }
}

 