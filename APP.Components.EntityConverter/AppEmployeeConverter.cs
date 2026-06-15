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
    /// Convert Properties between  AppEmployeeEntity and  AppEmployeeDto
    /// </summary>
    public static partial class AppEmployeeConverter 
    {
         /// <summary>
        ///  Convert AppEmployeeEntity To  AppEmployeeDto
        /// </summary>
        public static AppEmployeeDto ConvertEntityToDto(AppEmployeeEntity aAppEmployeeEntity)
        {        
    		AppEmployeeDto aAppEmployeeDto = new AppEmployeeDto();
    		CopyEntityPropertyToDto( aAppEmployeeEntity, aAppEmployeeDto);          
			return aAppEmployeeDto;
        }
		 /// <summary>
        ///  Convert AppEmployeeEntity To  AppEmployeeExDto
        /// </summary>
        public static AppEmployeeExDto ConvertEntityToExDto(AppEmployeeEntity aAppEmployeeEntity)
        {        
    		AppEmployeeExDto aAppEmployeeExDto = new AppEmployeeExDto();
			CopyEntityPropertyToDto( aAppEmployeeEntity, aAppEmployeeExDto);
			
			
			
            return aAppEmployeeExDto;
        }
		
		 /// <summary>
        ///  Convert AppEmployeeEntity To  AppEmployeeDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppEmployeeEntity aAppEmployeeEntity,AppEmployeeDto aAppEmployeeDto)
        {        
    		
           // aAppEmployeeDto.StopChangeTracking();
 			aAppEmployeeDto.Id = aAppEmployeeEntity.UserId;
 			aAppEmployeeDto.HireDate = aAppEmployeeEntity.HireDate;
 			aAppEmployeeDto.VacationHours = aAppEmployeeEntity.VacationHours;
 			aAppEmployeeDto.Gender = aAppEmployeeEntity.Gender;
 			aAppEmployeeDto.SalariedFlag = aAppEmployeeEntity.SalariedFlag;
 			aAppEmployeeDto.SickLeaveHours = aAppEmployeeEntity.SickLeaveHours;
 			aAppEmployeeDto.CurrentFlag = aAppEmployeeEntity.CurrentFlag;
 			aAppEmployeeDto.JobTitle = aAppEmployeeEntity.JobTitle;
 			aAppEmployeeDto.BirthDate = aAppEmployeeEntity.BirthDate;
 			aAppEmployeeDto.MaritalStatus = aAppEmployeeEntity.MaritalStatus;
 			aAppEmployeeDto.NationalIdnumber = aAppEmployeeEntity.NationalIdnumber;
 			aAppEmployeeDto.AppCreatedById = aAppEmployeeEntity.AppCreatedById;
 			aAppEmployeeDto.AppCreatedDate = aAppEmployeeEntity.AppCreatedDate;
 			aAppEmployeeDto.AppModifiedDate = aAppEmployeeEntity.AppModifiedDate;
 			aAppEmployeeDto.AppModifiedById = aAppEmployeeEntity.AppModifiedById;
 			aAppEmployeeDto.AppCreatedByCompanyId = aAppEmployeeEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppEmployeeDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEmployeeEntity.AppCreatedDate);
                aAppEmployeeDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppEmployeeEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppEmployeeEntity, aAppEmployeeDto);
		}
		
		 /// <summary>
        ///  Copy AppEmployeeDto Properties to   AppEmployeeEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppEmployeeEntity aAppEmployeeEntity,AppEmployeeDto aAppEmployeeDto)
        {        
 
      			aAppEmployeeEntity.HireDate = aAppEmployeeDto.HireDate;
      			aAppEmployeeEntity.VacationHours = aAppEmployeeDto.VacationHours;
      			aAppEmployeeEntity.Gender = aAppEmployeeDto.Gender;
      			aAppEmployeeEntity.SalariedFlag = aAppEmployeeDto.SalariedFlag;
      			aAppEmployeeEntity.SickLeaveHours = aAppEmployeeDto.SickLeaveHours;
      			aAppEmployeeEntity.CurrentFlag = aAppEmployeeDto.CurrentFlag;
      			aAppEmployeeEntity.JobTitle = aAppEmployeeDto.JobTitle;
      			aAppEmployeeEntity.BirthDate = aAppEmployeeDto.BirthDate;
      			aAppEmployeeEntity.MaritalStatus = aAppEmployeeDto.MaritalStatus;
      			aAppEmployeeEntity.NationalIdnumber = aAppEmployeeDto.NationalIdnumber;
 
  
   
    
      			aAppEmployeeEntity.AppCreatedByCompanyId = aAppEmployeeDto.AppCreatedByCompanyId;
			
			if(aAppEmployeeDto.Id == null)
			{
				aAppEmployeeEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppEmployeeEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppEmployeeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEmployeeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppEmployeeEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppEmployeeEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppEmployeeEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppEmployeeEntity, aAppEmployeeDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppEmployeeEntity aAppEmployeeEntity,AppEmployeeDto aAppEmployeeDto);
		
		static partial void OnCopyDtoToEntityDone(AppEmployeeEntity aAppEmployeeEntity,AppEmployeeDto aAppEmployeeDto);
		
   
       
    }
}

 