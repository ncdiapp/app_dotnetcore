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
    /// Convert Properties between  AppSecurityGroupMemberEntity and  AppSecurityGroupMemberDto
    /// </summary>
    public static partial class AppSecurityGroupMemberConverter 
    {
         /// <summary>
        ///  Convert AppSecurityGroupMemberEntity To  AppSecurityGroupMemberDto
        /// </summary>
        public static AppSecurityGroupMemberDto ConvertEntityToDto(AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity)
        {        
    		AppSecurityGroupMemberDto aAppSecurityGroupMemberDto = new AppSecurityGroupMemberDto();
    		CopyEntityPropertyToDto( aAppSecurityGroupMemberEntity, aAppSecurityGroupMemberDto);          
			return aAppSecurityGroupMemberDto;
        }
		 /// <summary>
        ///  Convert AppSecurityGroupMemberEntity To  AppSecurityGroupMemberExDto
        /// </summary>
        public static AppSecurityGroupMemberExDto ConvertEntityToExDto(AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity)
        {        
    		AppSecurityGroupMemberExDto aAppSecurityGroupMemberExDto = new AppSecurityGroupMemberExDto();
			CopyEntityPropertyToDto( aAppSecurityGroupMemberEntity, aAppSecurityGroupMemberExDto);
			
			
			
            return aAppSecurityGroupMemberExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecurityGroupMemberEntity To  AppSecurityGroupMemberDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity,AppSecurityGroupMemberDto aAppSecurityGroupMemberDto)
        {        
    		
           // aAppSecurityGroupMemberDto.StopChangeTracking();
 			aAppSecurityGroupMemberDto.Id = aAppSecurityGroupMemberEntity.RoleMemberId;
 			aAppSecurityGroupMemberDto.GroupId = aAppSecurityGroupMemberEntity.GroupId;
 			aAppSecurityGroupMemberDto.UserId = aAppSecurityGroupMemberEntity.UserId;
 			aAppSecurityGroupMemberDto.IsDefault = aAppSecurityGroupMemberEntity.IsDefault;
 			aAppSecurityGroupMemberDto.AppCreatedById = aAppSecurityGroupMemberEntity.AppCreatedById;
 			aAppSecurityGroupMemberDto.AppCreatedDate = aAppSecurityGroupMemberEntity.AppCreatedDate;
 			aAppSecurityGroupMemberDto.AppModifiedDate = aAppSecurityGroupMemberEntity.AppModifiedDate;
 			aAppSecurityGroupMemberDto.AppModifiedById = aAppSecurityGroupMemberEntity.AppModifiedById;
 			aAppSecurityGroupMemberDto.AppCreatedByCompanyId = aAppSecurityGroupMemberEntity.AppCreatedByCompanyId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecurityGroupMemberDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityGroupMemberEntity.AppCreatedDate);
                aAppSecurityGroupMemberDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecurityGroupMemberEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecurityGroupMemberEntity, aAppSecurityGroupMemberDto);
		}
		
		 /// <summary>
        ///  Copy AppSecurityGroupMemberDto Properties to   AppSecurityGroupMemberEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity,AppSecurityGroupMemberDto aAppSecurityGroupMemberDto)
        {        
 
      			aAppSecurityGroupMemberEntity.GroupId = aAppSecurityGroupMemberDto.GroupId;
      			aAppSecurityGroupMemberEntity.UserId = aAppSecurityGroupMemberDto.UserId;
      			aAppSecurityGroupMemberEntity.IsDefault = aAppSecurityGroupMemberDto.IsDefault;
 
  
   
    
      			aAppSecurityGroupMemberEntity.AppCreatedByCompanyId = aAppSecurityGroupMemberDto.AppCreatedByCompanyId;
			
			if(aAppSecurityGroupMemberDto.Id == null)
			{
				aAppSecurityGroupMemberEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityGroupMemberEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecurityGroupMemberEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityGroupMemberEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecurityGroupMemberEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecurityGroupMemberEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecurityGroupMemberEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecurityGroupMemberEntity, aAppSecurityGroupMemberDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity,AppSecurityGroupMemberDto aAppSecurityGroupMemberDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecurityGroupMemberEntity aAppSecurityGroupMemberEntity,AppSecurityGroupMemberDto aAppSecurityGroupMemberDto);
		
   
       
    }
}

 