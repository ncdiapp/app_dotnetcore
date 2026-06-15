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
    /// Convert Properties between  AppTransactionUnitExtendFieldValueEntity and  AppTransactionUnitExtendFieldValueDto
    /// </summary>
    public static partial class AppTransactionUnitExtendFieldValueConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitExtendFieldValueEntity To  AppTransactionUnitExtendFieldValueDto
        /// </summary>
        public static AppTransactionUnitExtendFieldValueDto ConvertEntityToDto(AppTransactionUnitExtendFieldValueEntity aAppTransactionUnitExtendFieldValueEntity)
        {        
    		AppTransactionUnitExtendFieldValueDto aAppTransactionUnitExtendFieldValueDto = new AppTransactionUnitExtendFieldValueDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitExtendFieldValueEntity, aAppTransactionUnitExtendFieldValueDto);          
			return aAppTransactionUnitExtendFieldValueDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitExtendFieldValueEntity To  AppTransactionUnitExtendFieldValueExDto
        /// </summary>
        public static AppTransactionUnitExtendFieldValueExDto ConvertEntityToExDto(AppTransactionUnitExtendFieldValueEntity aAppTransactionUnitExtendFieldValueEntity)
        {        
    		AppTransactionUnitExtendFieldValueExDto aAppTransactionUnitExtendFieldValueExDto = new AppTransactionUnitExtendFieldValueExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitExtendFieldValueEntity, aAppTransactionUnitExtendFieldValueExDto);
			
			
			
            return aAppTransactionUnitExtendFieldValueExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitExtendFieldValueEntity To  AppTransactionUnitExtendFieldValueDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitExtendFieldValueEntity aAppTransactionUnitExtendFieldValueEntity,AppTransactionUnitExtendFieldValueDto aAppTransactionUnitExtendFieldValueDto)
        {        
    		
           // aAppTransactionUnitExtendFieldValueDto.StopChangeTracking();
 			aAppTransactionUnitExtendFieldValueDto.Id = aAppTransactionUnitExtendFieldValueEntity.UnitExtendFieldValueId;
 			aAppTransactionUnitExtendFieldValueDto.TransactionUnitId = aAppTransactionUnitExtendFieldValueEntity.TransactionUnitId;
 			aAppTransactionUnitExtendFieldValueDto.UnitExtendFiledId = aAppTransactionUnitExtendFieldValueEntity.UnitExtendFiledId;
 			aAppTransactionUnitExtendFieldValueDto.UnitPkvalue = aAppTransactionUnitExtendFieldValueEntity.UnitPkvalue;
 			aAppTransactionUnitExtendFieldValueDto.ValueText = aAppTransactionUnitExtendFieldValueEntity.ValueText;
 			aAppTransactionUnitExtendFieldValueDto.AppCreatedById = aAppTransactionUnitExtendFieldValueEntity.AppCreatedById;
 			aAppTransactionUnitExtendFieldValueDto.AppModifiedById = aAppTransactionUnitExtendFieldValueEntity.AppModifiedById;
 			aAppTransactionUnitExtendFieldValueDto.AppCreatedByCompanyId = aAppTransactionUnitExtendFieldValueEntity.AppCreatedByCompanyId;
 			aAppTransactionUnitExtendFieldValueDto.AppCreatedDate = aAppTransactionUnitExtendFieldValueEntity.AppCreatedDate;
 			aAppTransactionUnitExtendFieldValueDto.AppModifiedDate = aAppTransactionUnitExtendFieldValueEntity.AppModifiedDate;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitExtendFieldValueDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitExtendFieldValueEntity.AppCreatedDate);
                aAppTransactionUnitExtendFieldValueDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitExtendFieldValueEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitExtendFieldValueEntity, aAppTransactionUnitExtendFieldValueDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitExtendFieldValueDto Properties to   AppTransactionUnitExtendFieldValueEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitExtendFieldValueEntity aAppTransactionUnitExtendFieldValueEntity,AppTransactionUnitExtendFieldValueDto aAppTransactionUnitExtendFieldValueDto)
        {        
 
      			aAppTransactionUnitExtendFieldValueEntity.TransactionUnitId = aAppTransactionUnitExtendFieldValueDto.TransactionUnitId;
      			aAppTransactionUnitExtendFieldValueEntity.UnitExtendFiledId = aAppTransactionUnitExtendFieldValueDto.UnitExtendFiledId;
      			aAppTransactionUnitExtendFieldValueEntity.UnitPkvalue = aAppTransactionUnitExtendFieldValueDto.UnitPkvalue;
      			aAppTransactionUnitExtendFieldValueEntity.ValueText = aAppTransactionUnitExtendFieldValueDto.ValueText;
 
    
      			aAppTransactionUnitExtendFieldValueEntity.AppCreatedByCompanyId = aAppTransactionUnitExtendFieldValueDto.AppCreatedByCompanyId;
  
   
			
			if(aAppTransactionUnitExtendFieldValueDto.Id == null)
			{
				aAppTransactionUnitExtendFieldValueEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitExtendFieldValueEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitExtendFieldValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitExtendFieldValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitExtendFieldValueEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitExtendFieldValueEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitExtendFieldValueEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitExtendFieldValueEntity, aAppTransactionUnitExtendFieldValueDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitExtendFieldValueEntity aAppTransactionUnitExtendFieldValueEntity,AppTransactionUnitExtendFieldValueDto aAppTransactionUnitExtendFieldValueDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitExtendFieldValueEntity aAppTransactionUnitExtendFieldValueEntity,AppTransactionUnitExtendFieldValueDto aAppTransactionUnitExtendFieldValueDto);
		
   
       
    }
}

 