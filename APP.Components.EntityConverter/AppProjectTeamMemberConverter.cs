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
    /// Convert Properties between  AppProjectTeamMemberEntity and  AppProjectTeamMemberDto
    /// </summary>
    public static partial class AppProjectTeamMemberConverter 
    {
         /// <summary>
        ///  Convert AppProjectTeamMemberEntity To  AppProjectTeamMemberDto
        /// </summary>
        public static AppProjectTeamMemberDto ConvertEntityToDto(AppProjectTeamMemberEntity aAppProjectTeamMemberEntity)
        {        
    		AppProjectTeamMemberDto aAppProjectTeamMemberDto = new AppProjectTeamMemberDto();
    		CopyEntityPropertyToDto( aAppProjectTeamMemberEntity, aAppProjectTeamMemberDto);          
			return aAppProjectTeamMemberDto;
        }
		 /// <summary>
        ///  Convert AppProjectTeamMemberEntity To  AppProjectTeamMemberExDto
        /// </summary>
        public static AppProjectTeamMemberExDto ConvertEntityToExDto(AppProjectTeamMemberEntity aAppProjectTeamMemberEntity)
        {        
    		AppProjectTeamMemberExDto aAppProjectTeamMemberExDto = new AppProjectTeamMemberExDto();
			CopyEntityPropertyToDto( aAppProjectTeamMemberEntity, aAppProjectTeamMemberExDto);
			
			
			
            return aAppProjectTeamMemberExDto;
        }
		
		 /// <summary>
        ///  Convert AppProjectTeamMemberEntity To  AppProjectTeamMemberDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppProjectTeamMemberEntity aAppProjectTeamMemberEntity,AppProjectTeamMemberDto aAppProjectTeamMemberDto)
        {        
    		
           // aAppProjectTeamMemberDto.StopChangeTracking();
 			aAppProjectTeamMemberDto.Id = aAppProjectTeamMemberEntity.TeamMemberId;
 			aAppProjectTeamMemberDto.ProjectTeamId = aAppProjectTeamMemberEntity.ProjectTeamId;
 			aAppProjectTeamMemberDto.ProjectId = aAppProjectTeamMemberEntity.ProjectId;
 			aAppProjectTeamMemberDto.UserId = aAppProjectTeamMemberEntity.UserId;
 			aAppProjectTeamMemberDto.EmCostType = aAppProjectTeamMemberEntity.EmCostType;
 			aAppProjectTeamMemberDto.PersonalRate = aAppProjectTeamMemberEntity.PersonalRate;
 			aAppProjectTeamMemberDto.CurrencyId = aAppProjectTeamMemberEntity.CurrencyId;
 			aAppProjectTeamMemberDto.AppCreatedById = aAppProjectTeamMemberEntity.AppCreatedById;
 			aAppProjectTeamMemberDto.AppCreatedDate = aAppProjectTeamMemberEntity.AppCreatedDate;
 			aAppProjectTeamMemberDto.AppModifiedDate = aAppProjectTeamMemberEntity.AppModifiedDate;
 			aAppProjectTeamMemberDto.AppModifiedById = aAppProjectTeamMemberEntity.AppModifiedById;
 			aAppProjectTeamMemberDto.AppCreatedByCompanyId = aAppProjectTeamMemberEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppProjectTeamMemberDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTeamMemberEntity.AppCreatedDate);
                aAppProjectTeamMemberDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppProjectTeamMemberEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppProjectTeamMemberEntity, aAppProjectTeamMemberDto);
		}
		
		 /// <summary>
        ///  Copy AppProjectTeamMemberDto Properties to   AppProjectTeamMemberEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppProjectTeamMemberEntity aAppProjectTeamMemberEntity,AppProjectTeamMemberDto aAppProjectTeamMemberDto)
        {        
 
      			aAppProjectTeamMemberEntity.ProjectTeamId = aAppProjectTeamMemberDto.ProjectTeamId;
      			aAppProjectTeamMemberEntity.ProjectId = aAppProjectTeamMemberDto.ProjectId;
      			aAppProjectTeamMemberEntity.UserId = aAppProjectTeamMemberDto.UserId;
      			aAppProjectTeamMemberEntity.EmCostType = aAppProjectTeamMemberDto.EmCostType;
      			aAppProjectTeamMemberEntity.PersonalRate = aAppProjectTeamMemberDto.PersonalRate;
      			aAppProjectTeamMemberEntity.CurrencyId = aAppProjectTeamMemberDto.CurrencyId;
 
  
   
    
      			aAppProjectTeamMemberEntity.AppCreatedByCompanyId = aAppProjectTeamMemberDto.AppCreatedByCompanyId;
			
			if(aAppProjectTeamMemberDto.Id == null)
			{
				aAppProjectTeamMemberEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTeamMemberEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppProjectTeamMemberEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTeamMemberEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppProjectTeamMemberEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppProjectTeamMemberEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppProjectTeamMemberEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppProjectTeamMemberEntity, aAppProjectTeamMemberDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppProjectTeamMemberEntity aAppProjectTeamMemberEntity,AppProjectTeamMemberDto aAppProjectTeamMemberDto);
		
		static partial void OnCopyDtoToEntityDone(AppProjectTeamMemberEntity aAppProjectTeamMemberEntity,AppProjectTeamMemberDto aAppProjectTeamMemberDto);
		
   
       
    }
}

 