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
    /// Convert Properties between  AppSecuritySysObjGroupUserEntity and  AppSecuritySysObjGroupUserDto
    /// </summary>
    public static partial class AppSecuritySysObjGroupUserConverter 
    {
         /// <summary>
        ///  Convert AppSecuritySysObjGroupUserEntity To  AppSecuritySysObjGroupUserDto
        /// </summary>
        public static AppSecuritySysObjGroupUserDto ConvertEntityToDto(AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity)
        {        
    		AppSecuritySysObjGroupUserDto aAppSecuritySysObjGroupUserDto = new AppSecuritySysObjGroupUserDto();
    		CopyEntityPropertyToDto( aAppSecuritySysObjGroupUserEntity, aAppSecuritySysObjGroupUserDto);          
			return aAppSecuritySysObjGroupUserDto;
        }
		 /// <summary>
        ///  Convert AppSecuritySysObjGroupUserEntity To  AppSecuritySysObjGroupUserExDto
        /// </summary>
        public static AppSecuritySysObjGroupUserExDto ConvertEntityToExDto(AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity)
        {        
    		AppSecuritySysObjGroupUserExDto aAppSecuritySysObjGroupUserExDto = new AppSecuritySysObjGroupUserExDto();
			CopyEntityPropertyToDto( aAppSecuritySysObjGroupUserEntity, aAppSecuritySysObjGroupUserExDto);
			
			
			
            return aAppSecuritySysObjGroupUserExDto;
        }
		
		 /// <summary>
        ///  Convert AppSecuritySysObjGroupUserEntity To  AppSecuritySysObjGroupUserDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity,AppSecuritySysObjGroupUserDto aAppSecuritySysObjGroupUserDto)
        {        
    		
           // aAppSecuritySysObjGroupUserDto.StopChangeTracking();
 			aAppSecuritySysObjGroupUserDto.Id = aAppSecuritySysObjGroupUserEntity.SecurityRightId;
 			aAppSecuritySysObjGroupUserDto.GroupId = aAppSecuritySysObjGroupUserEntity.GroupId;
 			aAppSecuritySysObjGroupUserDto.UserId = aAppSecuritySysObjGroupUserEntity.UserId;
 			aAppSecuritySysObjGroupUserDto.OrganizationId = aAppSecuritySysObjGroupUserEntity.OrganizationId;
 			aAppSecuritySysObjGroupUserDto.TransactionId = aAppSecuritySysObjGroupUserEntity.TransactionId;
 			aAppSecuritySysObjGroupUserDto.TransactionUnitId = aAppSecuritySysObjGroupUserEntity.TransactionUnitId;
 			aAppSecuritySysObjGroupUserDto.TransactionFieldId = aAppSecuritySysObjGroupUserEntity.TransactionFieldId;
 			aAppSecuritySysObjGroupUserDto.SearchId = aAppSecuritySysObjGroupUserEntity.SearchId;
 			aAppSecuritySysObjGroupUserDto.SearchViewId = aAppSecuritySysObjGroupUserEntity.SearchViewId;
 			aAppSecuritySysObjGroupUserDto.RouteStateId = aAppSecuritySysObjGroupUserEntity.RouteStateId;
 			aAppSecuritySysObjGroupUserDto.DesktopId = aAppSecuritySysObjGroupUserEntity.DesktopId;
 			aAppSecuritySysObjGroupUserDto.TransactionUnitLinkedSearchId = aAppSecuritySysObjGroupUserEntity.TransactionUnitLinkedSearchId;
 			aAppSecuritySysObjGroupUserDto.UserActionTransactionId = aAppSecuritySysObjGroupUserEntity.UserActionTransactionId;
 			aAppSecuritySysObjGroupUserDto.UserActionTransactionCode = aAppSecuritySysObjGroupUserEntity.UserActionTransactionCode;
 			aAppSecuritySysObjGroupUserDto.UserActionTransactionUnitId = aAppSecuritySysObjGroupUserEntity.UserActionTransactionUnitId;
 			aAppSecuritySysObjGroupUserDto.UserActionTransactionUnitCode = aAppSecuritySysObjGroupUserEntity.UserActionTransactionUnitCode;
 			aAppSecuritySysObjGroupUserDto.FormLinkTargetId = aAppSecuritySysObjGroupUserEntity.FormLinkTargetId;
 			aAppSecuritySysObjGroupUserDto.ReportId = aAppSecuritySysObjGroupUserEntity.ReportId;
 			aAppSecuritySysObjGroupUserDto.EmUserType = aAppSecuritySysObjGroupUserEntity.EmUserType;
 			aAppSecuritySysObjGroupUserDto.IsInVisible = aAppSecuritySysObjGroupUserEntity.IsInVisible;
 			aAppSecuritySysObjGroupUserDto.IsUnSaveAble = aAppSecuritySysObjGroupUserEntity.IsUnSaveAble;
 			aAppSecuritySysObjGroupUserDto.IsSpecialPermission = aAppSecuritySysObjGroupUserEntity.IsSpecialPermission;
 			aAppSecuritySysObjGroupUserDto.AppCreatedById = aAppSecuritySysObjGroupUserEntity.AppCreatedById;
 			aAppSecuritySysObjGroupUserDto.AppCreatedDate = aAppSecuritySysObjGroupUserEntity.AppCreatedDate;
 			aAppSecuritySysObjGroupUserDto.AppModifiedDate = aAppSecuritySysObjGroupUserEntity.AppModifiedDate;
 			aAppSecuritySysObjGroupUserDto.AppModifiedById = aAppSecuritySysObjGroupUserEntity.AppModifiedById;
 			aAppSecuritySysObjGroupUserDto.AppCreatedByCompanyId = aAppSecuritySysObjGroupUserEntity.AppCreatedByCompanyId;
 			aAppSecuritySysObjGroupUserDto.IsIgnoreFilterBy = aAppSecuritySysObjGroupUserEntity.IsIgnoreFilterBy;
 			aAppSecuritySysObjGroupUserDto.IsDefault = aAppSecuritySysObjGroupUserEntity.IsDefault;
 			aAppSecuritySysObjGroupUserDto.IsNeedSpecailEditPrivilege = aAppSecuritySysObjGroupUserEntity.IsNeedSpecailEditPrivilege;
 			aAppSecuritySysObjGroupUserDto.CommandId = aAppSecuritySysObjGroupUserEntity.CommandId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppSecuritySysObjGroupUserDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecuritySysObjGroupUserEntity.AppCreatedDate);
                aAppSecuritySysObjGroupUserDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppSecuritySysObjGroupUserEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppSecuritySysObjGroupUserEntity, aAppSecuritySysObjGroupUserDto);
		}
		
		 /// <summary>
        ///  Copy AppSecuritySysObjGroupUserDto Properties to   AppSecuritySysObjGroupUserEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity,AppSecuritySysObjGroupUserDto aAppSecuritySysObjGroupUserDto)
        {        
 
      			aAppSecuritySysObjGroupUserEntity.GroupId = aAppSecuritySysObjGroupUserDto.GroupId;
      			aAppSecuritySysObjGroupUserEntity.UserId = aAppSecuritySysObjGroupUserDto.UserId;
      			aAppSecuritySysObjGroupUserEntity.OrganizationId = aAppSecuritySysObjGroupUserDto.OrganizationId;
      			aAppSecuritySysObjGroupUserEntity.TransactionId = aAppSecuritySysObjGroupUserDto.TransactionId;
      			aAppSecuritySysObjGroupUserEntity.TransactionUnitId = aAppSecuritySysObjGroupUserDto.TransactionUnitId;
      			aAppSecuritySysObjGroupUserEntity.TransactionFieldId = aAppSecuritySysObjGroupUserDto.TransactionFieldId;
      			aAppSecuritySysObjGroupUserEntity.SearchId = aAppSecuritySysObjGroupUserDto.SearchId;
      			aAppSecuritySysObjGroupUserEntity.SearchViewId = aAppSecuritySysObjGroupUserDto.SearchViewId;
      			aAppSecuritySysObjGroupUserEntity.RouteStateId = aAppSecuritySysObjGroupUserDto.RouteStateId;
      			aAppSecuritySysObjGroupUserEntity.DesktopId = aAppSecuritySysObjGroupUserDto.DesktopId;
      			aAppSecuritySysObjGroupUserEntity.TransactionUnitLinkedSearchId = aAppSecuritySysObjGroupUserDto.TransactionUnitLinkedSearchId;
      			aAppSecuritySysObjGroupUserEntity.UserActionTransactionId = aAppSecuritySysObjGroupUserDto.UserActionTransactionId;
      			aAppSecuritySysObjGroupUserEntity.UserActionTransactionCode = aAppSecuritySysObjGroupUserDto.UserActionTransactionCode;
      			aAppSecuritySysObjGroupUserEntity.UserActionTransactionUnitId = aAppSecuritySysObjGroupUserDto.UserActionTransactionUnitId;
      			aAppSecuritySysObjGroupUserEntity.UserActionTransactionUnitCode = aAppSecuritySysObjGroupUserDto.UserActionTransactionUnitCode;
      			aAppSecuritySysObjGroupUserEntity.FormLinkTargetId = aAppSecuritySysObjGroupUserDto.FormLinkTargetId;
      			aAppSecuritySysObjGroupUserEntity.ReportId = aAppSecuritySysObjGroupUserDto.ReportId;
      			aAppSecuritySysObjGroupUserEntity.EmUserType = aAppSecuritySysObjGroupUserDto.EmUserType;
      			aAppSecuritySysObjGroupUserEntity.IsInVisible = aAppSecuritySysObjGroupUserDto.IsInVisible;
      			aAppSecuritySysObjGroupUserEntity.IsUnSaveAble = aAppSecuritySysObjGroupUserDto.IsUnSaveAble;
      			aAppSecuritySysObjGroupUserEntity.IsSpecialPermission = aAppSecuritySysObjGroupUserDto.IsSpecialPermission;
 
  
   
    
      			aAppSecuritySysObjGroupUserEntity.AppCreatedByCompanyId = aAppSecuritySysObjGroupUserDto.AppCreatedByCompanyId;
      			aAppSecuritySysObjGroupUserEntity.IsIgnoreFilterBy = aAppSecuritySysObjGroupUserDto.IsIgnoreFilterBy;
      			aAppSecuritySysObjGroupUserEntity.IsDefault = aAppSecuritySysObjGroupUserDto.IsDefault;
      			aAppSecuritySysObjGroupUserEntity.IsNeedSpecailEditPrivilege = aAppSecuritySysObjGroupUserDto.IsNeedSpecailEditPrivilege;
      			aAppSecuritySysObjGroupUserEntity.CommandId = aAppSecuritySysObjGroupUserDto.CommandId;
			
			if(aAppSecuritySysObjGroupUserDto.Id == null)
			{
				aAppSecuritySysObjGroupUserEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecuritySysObjGroupUserEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppSecuritySysObjGroupUserEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecuritySysObjGroupUserEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppSecuritySysObjGroupUserEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppSecuritySysObjGroupUserEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppSecuritySysObjGroupUserEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppSecuritySysObjGroupUserEntity, aAppSecuritySysObjGroupUserDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity,AppSecuritySysObjGroupUserDto aAppSecuritySysObjGroupUserDto);
		
		static partial void OnCopyDtoToEntityDone(AppSecuritySysObjGroupUserEntity aAppSecuritySysObjGroupUserEntity,AppSecuritySysObjGroupUserDto aAppSecuritySysObjGroupUserDto);
		
   
       
    }
}

 