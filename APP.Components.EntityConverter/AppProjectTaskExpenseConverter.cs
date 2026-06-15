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
    /// Convert Properties between  AppProjectTaskExpenseEntity and  AppProjectTaskExpenseDto
    /// </summary>
    public static partial class AppProjectTaskExpenseConverter 
    {
         /// <summary>
        ///  Convert AppProjectTaskExpenseEntity To  AppProjectTaskExpenseDto
        /// </summary>
        public static AppProjectTaskExpenseDto ConvertEntityToDto(AppProjectTaskExpenseEntity aAppProjectTaskExpenseEntity)
        {        
    		AppProjectTaskExpenseDto aAppProjectTaskExpenseDto = new AppProjectTaskExpenseDto();
    		CopyEntityPropertyToDto( aAppProjectTaskExpenseEntity, aAppProjectTaskExpenseDto);          
			return aAppProjectTaskExpenseDto;
        }
		 /// <summary>
        ///  Convert AppProjectTaskExpenseEntity To  AppProjectTaskExpenseExDto
        /// </summary>
        public static AppProjectTaskExpenseExDto ConvertEntityToExDto(AppProjectTaskExpenseEntity aAppProjectTaskExpenseEntity)
        {        
    		AppProjectTaskExpenseExDto aAppProjectTaskExpenseExDto = new AppProjectTaskExpenseExDto();
			CopyEntityPropertyToDto( aAppProjectTaskExpenseEntity, aAppProjectTaskExpenseExDto);
			
			
			
            return aAppProjectTaskExpenseExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTaskExpenseEntity To  AppProjectTaskExpenseDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTaskExpenseEntity aAppProjectTaskExpenseEntity,AppProjectTaskExpenseDto aAppProjectTaskExpenseDto)
        {        
    		
           // aAppProjectTaskExpenseDto.StopChangeTracking();
 			aAppProjectTaskExpenseDto.Id = aAppProjectTaskExpenseEntity.TaskExpenseId;
 			aAppProjectTaskExpenseDto.ProjectTaskId = aAppProjectTaskExpenseEntity.ProjectTaskId;
 			aAppProjectTaskExpenseDto.Category = aAppProjectTaskExpenseEntity.Category;
 			aAppProjectTaskExpenseDto.Notes = aAppProjectTaskExpenseEntity.Notes;
 			aAppProjectTaskExpenseDto.ExpenseAmount = aAppProjectTaskExpenseEntity.ExpenseAmount;
 			aAppProjectTaskExpenseDto.ApprovedBy = aAppProjectTaskExpenseEntity.ApprovedBy;
 			aAppProjectTaskExpenseDto.AppCreatedById = aAppProjectTaskExpenseEntity.AppCreatedById;
 			aAppProjectTaskExpenseDto.AppCreatedDate = aAppProjectTaskExpenseEntity.AppCreatedDate;
 			aAppProjectTaskExpenseDto.AppModifiedDate = aAppProjectTaskExpenseEntity.AppModifiedDate;
 			aAppProjectTaskExpenseDto.AppModifiedById = aAppProjectTaskExpenseEntity.AppModifiedById;
 			aAppProjectTaskExpenseDto.AppCreatedByCompanyId = aAppProjectTaskExpenseEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTaskExpenseDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskExpenseEntity.AppCreatedDate);
                aAppProjectTaskExpenseDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTaskExpenseEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTaskExpenseEntity, aAppProjectTaskExpenseDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTaskExpenseDto Properties to   AppProjectTaskExpenseEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTaskExpenseEntity aAppProjectTaskExpenseEntity,AppProjectTaskExpenseDto aAppProjectTaskExpenseDto)
        {        
 
      			aAppProjectTaskExpenseEntity.ProjectTaskId = aAppProjectTaskExpenseDto.ProjectTaskId;
      			aAppProjectTaskExpenseEntity.Category = aAppProjectTaskExpenseDto.Category;
      			aAppProjectTaskExpenseEntity.Notes = aAppProjectTaskExpenseDto.Notes;
      			aAppProjectTaskExpenseEntity.ExpenseAmount = aAppProjectTaskExpenseDto.ExpenseAmount;
      			aAppProjectTaskExpenseEntity.ApprovedBy = aAppProjectTaskExpenseDto.ApprovedBy;
 
  
   
    
      			aAppProjectTaskExpenseEntity.AppCreatedByCompanyId = aAppProjectTaskExpenseDto.AppCreatedByCompanyId;
			
			if(aAppProjectTaskExpenseDto.Id == null)
			{
				aAppProjectTaskExpenseEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskExpenseEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTaskExpenseEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskExpenseEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTaskExpenseEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTaskExpenseEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTaskExpenseEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTaskExpenseEntity, aAppProjectTaskExpenseDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTaskExpenseEntity aAppProjectTaskExpenseEntity,AppProjectTaskExpenseDto aAppProjectTaskExpenseDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTaskExpenseEntity aAppProjectTaskExpenseEntity,AppProjectTaskExpenseDto aAppProjectTaskExpenseDto);
		
   
       
    }
}

 