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
    /// Convert Properties between  AppTransactionUnitFormulaEntity and  AppTransactionUnitFormulaDto
    /// </summary>
    public static partial class AppTransactionUnitFormulaConverter 
    {
         /// <summary>
        ///  Convert AppTransactionUnitFormulaEntity To  AppTransactionUnitFormulaDto
        /// </summary>
        public static AppTransactionUnitFormulaDto ConvertEntityToDto(AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity)
        {        
    		AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto = new AppTransactionUnitFormulaDto();
    		CopyEntityPropertyToDto( aAppTransactionUnitFormulaEntity, aAppTransactionUnitFormulaDto);          
			return aAppTransactionUnitFormulaDto;
        }
		 /// <summary>
        ///  Convert AppTransactionUnitFormulaEntity To  AppTransactionUnitFormulaExDto
        /// </summary>
        public static AppTransactionUnitFormulaExDto ConvertEntityToExDto(AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity)
        {        
    		AppTransactionUnitFormulaExDto aAppTransactionUnitFormulaExDto = new AppTransactionUnitFormulaExDto();
			CopyEntityPropertyToDto( aAppTransactionUnitFormulaEntity, aAppTransactionUnitFormulaExDto);
			
			
			
            return aAppTransactionUnitFormulaExDto;
        }
		
		 /// <summary>
        ///  Convert AppTransactionUnitFormulaEntity To  AppTransactionUnitFormulaDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity,AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto)
        {        
    		
           // aAppTransactionUnitFormulaDto.StopChangeTracking();
 			aAppTransactionUnitFormulaDto.Id = aAppTransactionUnitFormulaEntity.TransactionUnitFormulaId;
 			aAppTransactionUnitFormulaDto.TransactionUnitId = aAppTransactionUnitFormulaEntity.TransactionUnitId;
 			aAppTransactionUnitFormulaDto.CaculationFlowSort = aAppTransactionUnitFormulaEntity.CaculationFlowSort;
 			aAppTransactionUnitFormulaDto.FormulaExpression = aAppTransactionUnitFormulaEntity.FormulaExpression;
 			aAppTransactionUnitFormulaDto.WarningMessage = aAppTransactionUnitFormulaEntity.WarningMessage;
 			aAppTransactionUnitFormulaDto.FunctionType = aAppTransactionUnitFormulaEntity.FunctionType;
 			aAppTransactionUnitFormulaDto.OperationType = aAppTransactionUnitFormulaEntity.OperationType;
 			aAppTransactionUnitFormulaDto.ConditionFieldId = aAppTransactionUnitFormulaEntity.ConditionFieldId;
 			aAppTransactionUnitFormulaDto.SwitchTrueFalseType = aAppTransactionUnitFormulaEntity.SwitchTrueFalseType;
 			aAppTransactionUnitFormulaDto.ChildTransactionUnitId = aAppTransactionUnitFormulaEntity.ChildTransactionUnitId;
 			aAppTransactionUnitFormulaDto.SystemTimeStamp = aAppTransactionUnitFormulaEntity.SystemTimeStamp;
 			aAppTransactionUnitFormulaDto.AppCreatedById = aAppTransactionUnitFormulaEntity.AppCreatedById;
 			aAppTransactionUnitFormulaDto.AppCreatedDate = aAppTransactionUnitFormulaEntity.AppCreatedDate;
 			aAppTransactionUnitFormulaDto.AppModifiedDate = aAppTransactionUnitFormulaEntity.AppModifiedDate;
 			aAppTransactionUnitFormulaDto.AppModifiedById = aAppTransactionUnitFormulaEntity.AppModifiedById;
 			aAppTransactionUnitFormulaDto.AppCreatedByCompanyId = aAppTransactionUnitFormulaEntity.AppCreatedByCompanyId;
 			aAppTransactionUnitFormulaDto.WarningHighlightTransFieldId = aAppTransactionUnitFormulaEntity.WarningHighlightTransFieldId;
 			aAppTransactionUnitFormulaDto.WarningHighlightStyleId = aAppTransactionUnitFormulaEntity.WarningHighlightStyleId;
 			aAppTransactionUnitFormulaDto.FormulaName = aAppTransactionUnitFormulaEntity.FormulaName;
 			aAppTransactionUnitFormulaDto.ApplyToScope = aAppTransactionUnitFormulaEntity.ApplyToScope;
 			aAppTransactionUnitFormulaDto.SearchViewId = aAppTransactionUnitFormulaEntity.SearchViewId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppTransactionUnitFormulaDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitFormulaEntity.AppCreatedDate);
                aAppTransactionUnitFormulaDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppTransactionUnitFormulaEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppTransactionUnitFormulaEntity, aAppTransactionUnitFormulaDto);
		}
		
		 /// <summary>
        ///  Copy AppTransactionUnitFormulaDto Properties to   AppTransactionUnitFormulaEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity,AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto)
        {        
 
      			aAppTransactionUnitFormulaEntity.TransactionUnitId = aAppTransactionUnitFormulaDto.TransactionUnitId;
      			aAppTransactionUnitFormulaEntity.CaculationFlowSort = aAppTransactionUnitFormulaDto.CaculationFlowSort;
      			aAppTransactionUnitFormulaEntity.FormulaExpression = aAppTransactionUnitFormulaDto.FormulaExpression;
      			aAppTransactionUnitFormulaEntity.WarningMessage = aAppTransactionUnitFormulaDto.WarningMessage;
      			aAppTransactionUnitFormulaEntity.FunctionType = aAppTransactionUnitFormulaDto.FunctionType;
      			aAppTransactionUnitFormulaEntity.OperationType = aAppTransactionUnitFormulaDto.OperationType;
      			aAppTransactionUnitFormulaEntity.ConditionFieldId = aAppTransactionUnitFormulaDto.ConditionFieldId;
      			aAppTransactionUnitFormulaEntity.SwitchTrueFalseType = aAppTransactionUnitFormulaDto.SwitchTrueFalseType;
      			aAppTransactionUnitFormulaEntity.ChildTransactionUnitId = aAppTransactionUnitFormulaDto.ChildTransactionUnitId;
 
 
  
   
    
      			aAppTransactionUnitFormulaEntity.AppCreatedByCompanyId = aAppTransactionUnitFormulaDto.AppCreatedByCompanyId;
      			aAppTransactionUnitFormulaEntity.WarningHighlightTransFieldId = aAppTransactionUnitFormulaDto.WarningHighlightTransFieldId;
      			aAppTransactionUnitFormulaEntity.WarningHighlightStyleId = aAppTransactionUnitFormulaDto.WarningHighlightStyleId;
      			aAppTransactionUnitFormulaEntity.FormulaName = aAppTransactionUnitFormulaDto.FormulaName;
      			aAppTransactionUnitFormulaEntity.ApplyToScope = aAppTransactionUnitFormulaDto.ApplyToScope;
      			aAppTransactionUnitFormulaEntity.SearchViewId = aAppTransactionUnitFormulaDto.SearchViewId;
			
			if(aAppTransactionUnitFormulaDto.Id == null)
			{
				aAppTransactionUnitFormulaEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitFormulaEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppTransactionUnitFormulaEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitFormulaEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppTransactionUnitFormulaEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppTransactionUnitFormulaEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppTransactionUnitFormulaEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppTransactionUnitFormulaEntity, aAppTransactionUnitFormulaDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity,AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto);
		
		static partial void OnCopyDtoToEntityDone(AppTransactionUnitFormulaEntity aAppTransactionUnitFormulaEntity,AppTransactionUnitFormulaDto aAppTransactionUnitFormulaDto);
		
   
       
    }
}

 