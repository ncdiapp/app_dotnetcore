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
    /// Convert Properties between  AppMessageEntity and  AppMessageDto
    /// </summary>
    public static partial class AppMessageConverter 
    {
         /// <summary>
        ///  Convert AppMessageEntity To  AppMessageDto
        /// </summary>
        public static AppMessageDto ConvertEntityToDto(AppMessageEntity aAppMessageEntity)
        {        
    		AppMessageDto aAppMessageDto = new AppMessageDto();
    		CopyEntityPropertyToDto( aAppMessageEntity, aAppMessageDto);          
			return aAppMessageDto;
        }
		 /// <summary>
        ///  Convert AppMessageEntity To  AppMessageExDto
        /// </summary>
        public static AppMessageExDto ConvertEntityToExDto(AppMessageEntity aAppMessageEntity)
        {        
    		AppMessageExDto aAppMessageExDto = new AppMessageExDto();
			CopyEntityPropertyToDto( aAppMessageEntity, aAppMessageExDto);
			
			
			
            return aAppMessageExDto;
        }
		
		 /// <summary>
        ///  Convert AppMessageEntity To  AppMessageDto
        /// </summary>
        private static void CopyEntityPropertyToDto(AppMessageEntity aAppMessageEntity,AppMessageDto aAppMessageDto)
        {        
    		
           // aAppMessageDto.StopChangeTracking();
 			aAppMessageDto.Id = aAppMessageEntity.MessageId;
 			aAppMessageDto.Subject = aAppMessageEntity.Subject;
 			aAppMessageDto.Message = aAppMessageEntity.Message;
 			aAppMessageDto.ReplyMsgToId = aAppMessageEntity.ReplyMsgToId;
 			aAppMessageDto.FromEmail = aAppMessageEntity.FromEmail;
 			aAppMessageDto.ToList = aAppMessageEntity.ToList;
 			aAppMessageDto.Cclist = aAppMessageEntity.Cclist;
 			aAppMessageDto.Bcclist = aAppMessageEntity.Bcclist;
 			aAppMessageDto.MessagePostType = aAppMessageEntity.MessagePostType;
 			aAppMessageDto.MessgaeScopeType = aAppMessageEntity.MessgaeScopeType;
 			aAppMessageDto.IsDraft = aAppMessageEntity.IsDraft;
 			aAppMessageDto.IsPredefinedTemplate = aAppMessageEntity.IsPredefinedTemplate;
 			aAppMessageDto.TransactionId = aAppMessageEntity.TransactionId;
 			aAppMessageDto.TransactionRootValueId = aAppMessageEntity.TransactionRootValueId;
 			aAppMessageDto.ProjectActivityId = aAppMessageEntity.ProjectActivityId;
 			aAppMessageDto.ProjectTeamId = aAppMessageEntity.ProjectTeamId;
 			aAppMessageDto.ProjectId = aAppMessageEntity.ProjectId;
 			aAppMessageDto.AttachmentFileToken = aAppMessageEntity.AttachmentFileToken;
 			aAppMessageDto.MsgUniqueId = aAppMessageEntity.MsgUniqueId;
 			aAppMessageDto.ReminderMinutes = aAppMessageEntity.ReminderMinutes;
 			aAppMessageDto.IsEnableReminder = aAppMessageEntity.IsEnableReminder;
 			aAppMessageDto.ReminderTargetDate = aAppMessageEntity.ReminderTargetDate;
 			aAppMessageDto.AppCreatedById = aAppMessageEntity.AppCreatedById;
 			aAppMessageDto.AppCreatedDate = aAppMessageEntity.AppCreatedDate;
 			aAppMessageDto.AppModifiedDate = aAppMessageEntity.AppModifiedDate;
 			aAppMessageDto.AppModifiedById = aAppMessageEntity.AppModifiedById;
 			aAppMessageDto.AppCreatedByCompanyId = aAppMessageEntity.AppCreatedByCompanyId;
 			aAppMessageDto.TransactionGroupId = aAppMessageEntity.TransactionGroupId;

			if (ClientTimeZoneHelper.IsClientUsingTimeZone)
            {
                aAppMessageDto.AppCreatedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppMessageEntity.AppCreatedDate);
                aAppMessageDto.AppModifiedDate = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aAppMessageEntity.AppModifiedDate);
            }
            OnCopyEntityToDtoDone(aAppMessageEntity, aAppMessageDto);
		}
		
		 /// <summary>
        ///  Copy AppMessageDto Properties to   AppMessageEntity Without object Id 
        /// </summary>
        public static void CopyDtoToEntity(AppMessageEntity aAppMessageEntity,AppMessageDto aAppMessageDto)
        {        
 
      			aAppMessageEntity.Subject = aAppMessageDto.Subject;
      			aAppMessageEntity.Message = aAppMessageDto.Message;
      			aAppMessageEntity.ReplyMsgToId = aAppMessageDto.ReplyMsgToId;
      			aAppMessageEntity.FromEmail = aAppMessageDto.FromEmail;
      			aAppMessageEntity.ToList = aAppMessageDto.ToList;
      			aAppMessageEntity.Cclist = aAppMessageDto.Cclist;
      			aAppMessageEntity.Bcclist = aAppMessageDto.Bcclist;
      			aAppMessageEntity.MessagePostType = aAppMessageDto.MessagePostType;
      			aAppMessageEntity.MessgaeScopeType = aAppMessageDto.MessgaeScopeType;
      			aAppMessageEntity.IsDraft = aAppMessageDto.IsDraft;
      			aAppMessageEntity.IsPredefinedTemplate = aAppMessageDto.IsPredefinedTemplate;
      			aAppMessageEntity.TransactionId = aAppMessageDto.TransactionId;
      			aAppMessageEntity.TransactionRootValueId = aAppMessageDto.TransactionRootValueId;
      			aAppMessageEntity.ProjectActivityId = aAppMessageDto.ProjectActivityId;
      			aAppMessageEntity.ProjectTeamId = aAppMessageDto.ProjectTeamId;
      			aAppMessageEntity.ProjectId = aAppMessageDto.ProjectId;
      			aAppMessageEntity.AttachmentFileToken = aAppMessageDto.AttachmentFileToken;
      			aAppMessageEntity.MsgUniqueId = aAppMessageDto.MsgUniqueId;
      			aAppMessageEntity.ReminderMinutes = aAppMessageDto.ReminderMinutes;
      			aAppMessageEntity.IsEnableReminder = aAppMessageDto.IsEnableReminder;
      			aAppMessageEntity.ReminderTargetDate = aAppMessageDto.ReminderTargetDate;
 
  
   
    
      			aAppMessageEntity.AppCreatedByCompanyId = aAppMessageDto.AppCreatedByCompanyId;
      			aAppMessageEntity.TransactionGroupId = aAppMessageDto.TransactionGroupId;
			
			if(aAppMessageDto.Id == null)
			{
				aAppMessageEntity.AppCreatedById = (int)ServerContext.Instance.CurrentUid;
				aAppMessageEntity.AppCreatedDate = System.DateTime.UtcNow; 
				aAppMessageEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppMessageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
				aAppMessageEntity.AppCreatedByCompanyId = (int)ServerContext.Instance.CurrentCompanyId;
			}
			else
			{
				aAppMessageEntity.AppModifiedDate = System.DateTime.UtcNow;
				aAppMessageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;

			}
			
			 OnCopyDtoToEntityDone(aAppMessageEntity, aAppMessageDto)	;
        }
		
		static partial void OnCopyEntityToDtoDone(AppMessageEntity aAppMessageEntity,AppMessageDto aAppMessageDto);
		
		static partial void OnCopyDtoToEntityDone(AppMessageEntity aAppMessageEntity,AppMessageDto aAppMessageDto);
		
   
       
    }
}

 