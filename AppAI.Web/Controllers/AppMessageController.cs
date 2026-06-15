using System.Collections.Generic;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework;
using APP.Framework.Communication;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AppMessageController : SecureBaseController
{
    [HttpGet]
    public List<AppMessageDto> RetrieveProjectActivityMessageList(int? projectActivityId)
    {
        if (projectActivityId.HasValue)
        {
            var toRuren = AppMessageBL.RetrieveProjectActivityMessageList(projectActivityId.Value).ToList();
            return toRuren;
        }

        return null;
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserUnReadMessagesByUserId()
    {
        return AppMessageOutlookBL.RetrieveCurrentUserUnReadMessagesByUserId().ToList();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveAllPredefinedMessageTemplates()
    {
        return AppMessageBL.RetrieveAllPredefinedMessageTemplates().ToList();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveTransactionMessageTemplates(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppMessageBL.RetrieveTransactionMessageTemplates(transactionId.Value).ToList();
        }

        return null;
    }

    [HttpGet]
    public AppMessageExDto RetrieveOneAppMessageExDto(int? messageId)
    {
        if (messageId.HasValue)
        {
            var toRuren = AppMessageBL.RetrieveOneAppMessageExDto(messageId.Value);
            return toRuren;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<object> SaveOneAppMessageDto(AppMessageDto aAppMessageDto)
    {
        return AppMessageBL.SaveOneAppMessageDto(aAppMessageDto);
    }

    [HttpPost]
    public OperationCallResult<object> DeleteMessagesByIdList(List<int> messageIdList)
    {
        return AppMessageBL.DeleteMessagesByIdList(messageIdList);
    }

    [HttpPost]
    public OperationCallResult<AppMessageDto> UpdateMessageAttachedFiles(AppMessageDto aAppMessageDto)
    {
        return AppMessageBL.UpdateMessageAttachedFiles(aAppMessageDto);
    }

    [HttpGet]
    public int? GetOneTaskDefaultConversactionId(int? taskId)
    {
        //base.InitializeSecurity();

        //if (taskId.HasValue)
        //{
        //    var toRuren = AppMessageBL.GetOneTaskDefaultConversactionId(taskId.Value);
        //    return toRuren;
        //}

        return null;
    }

    [HttpGet]
    public int? GetTransactionDefaultConversactionId(int? transactionId, string transactionRId)
    {
        //base.InitializeSecurity();

        //if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
        //{
        //    var toRuren = AppMessageBL.GetTransactionDefaultConversactionId(transactionId.Value, transactionRId);
        //    return toRuren;
        //}

        return null;
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserUnReadMessages()
    {
        if (ServerContext.Instance.CurrentUid == null)
            return new List<AppMessageDto>();

        return AppMessageOutlookBL.RetrieveCurrentUserUnReadMessages().ToList();


        //return CreateFakeMessages();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserInComeMessages(int? transactionId, int? transctionRid, int? messageScopeType)
    {
        return AppMessageOutlookBL.RetrieveCurrentUserInComeMessages(false, transactionId, transctionRid, messageScopeType).ToList();

        //return CreateFakeMessages();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserOutComeMessages(int? transactionId, int? transctionRid, int? messageScopeType)
    {
        return AppMessageOutlookBL.RetrieveCurrentUserOutComeMessages(transactionId, transctionRid, messageScopeType).ToList();

        //return CreateFakeMessages();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserDeletedMessages(int? transactionId, int? transctionRid, int? messageScopeType)
    {
        return AppMessageOutlookBL.RetrieveCurrentUserDeletedMessages(transactionId, transctionRid, messageScopeType).ToList();

        //return CreateFakeMessages();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserDraftMessages(int? transactionId, int? transctionRid, int? messageScopeType)
    {
        return AppMessageOutlookBL.RetrieveCurrentUserDraftMessages(transactionId, transctionRid, messageScopeType).ToList();

        //return CreateFakeMessages();
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveTransactionFormMessages(int? transactionId, int? transctionRid)
    {
        if (transactionId.HasValue && transctionRid.HasValue)
        {
            return AppMessageOutlookBL.RetrieveTransactionFormMessages(transactionId.Value, transctionRid.Value).ToList();
        }
        return null;
    }

    [HttpGet]
    public List<AppMessageDto> RetrieveCurrentUserAllConversations()
    {
        return AppMessageOutlookBL.RetrieveCurrentUserAllConversations().ToList();
    }

    [HttpPost]
    public OperationCallResult<bool> SetMessageReadState(MessageUpdateDto messageUpdateDto)
    {
        if (messageUpdateDto != null && messageUpdateDto.MessgeIdList != null)
        {
            return AppMessageOutlookBL.SetMessageReadState(messageUpdateDto.IsRead, messageUpdateDto.MessgeIdList);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<bool> DeleteUserMessages(MessageUpdateDto messageUpdateDto)
    {
        if (messageUpdateDto != null && messageUpdateDto.MessgeIdList != null && messageUpdateDto.MessgeIdList.Count > 0)
        {
            return AppMessageOutlookBL.DeleteUserMessages(messageUpdateDto.MessgeIdList, messageUpdateDto.IsDeleteReceivedMessage);
        }

        return null;
    }

    [HttpGet]
    public AppMessageDto GetMessageReplyDto(int? messageId)
    {
        if (messageId.HasValue)
        {
            return AppMessageOutlookBL.GetMessageReplyDto(messageId.Value);
        }

        return null;
    }

    [HttpGet]
    public AppMessageDto GetMessageReplyAllDto(int? messageId)
    {
        if (messageId.HasValue)
        {
            return AppMessageOutlookBL.GetMessageReplyAllDto(messageId.Value);
        }

        return null;
    }

    [HttpGet]
    public AppMessageDto GetMessageForwardDto(int? messageId)
    {
        if (messageId.HasValue)
        {
            return AppMessageOutlookBL.GetMessageForwardDto(messageId.Value);
        }

        return null;
    }

    [HttpGet]
    public ScopeMessageGroupDto RetrieveScopeMessageGroupDto(EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId)
    {
        if (scopeType.HasValue)
        {
            var toRuren = AppMessageBL.RetrieveScopeMessageGroupDto(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId);
            return toRuren;
        }

        return null;
    }

    [HttpGet]
    public List<AppUserMessgeFollowupDto> RetrieveAppUserMessgeFollowupDtoListByScope(EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId, int? projectTeamId)
    {
        if (scopeType.HasValue)
        {
            var toRuren = AppMessageBL.RetrieveAppUserMessgeFollowupDtoListByScope(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId, projectTeamId);
            return toRuren;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppUserMessgeFollowupDto> UpdateUserMessgeFollowupByScope(UserMessgeFollowupUpdateDto updateDto)
    {
        if (updateDto != null && updateDto.ScopeType.HasValue)
        {
            List<int> followupUserIdList = updateDto.FollowupUserIdList;
            EmAppMessgaeScopeType? scopeType = updateDto.ScopeType;
            int? transactionId = updateDto.TransactionId;
            string transactionRId = updateDto.TransactionRId;
            int? taskId = updateDto.TaskId;
            int? projectOrWorkflowId = updateDto.ProjectOrWorkflowId;
            int? projectTeamId = updateDto.ProjectTeamId;

            return AppMessageBL.UpdateUserMessgeFollowupByScope(followupUserIdList, scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId, projectTeamId);
        }

        return null;
    }

    [HttpGet]
    public List<AppmessageNotificationSettingExDto> RetrieveAllAppmessageNotificationSettingEntityDto(int? transactionId, int? projectId, int? usageType)
    {
        var toRuren = APPMessageNotificationBL.RetrieveAllAppmessageNotificationSettingEntityDto(transactionId, projectId, usageType).ToList();
        return toRuren;
    }

    [HttpPost]
    public OperationCallResult<AppmessageNotificationSettingExDto> SaveAllAppmessageNotificationSettingEntityDto(AppMessageNotificationSettingSetDto settingSetDto)
    {
        if (settingSetDto != null && settingSetDto.AppMessageNotificationSettingSet != null)
        {
            if (settingSetDto.DeletedItemIds == null)
            {
                settingSetDto.DeletedItemIds = new List<object>();
            }

            settingSetDto.AppMessageNotificationSettingSet.DeletedItemIds = settingSetDto.DeletedItemIds;

            return APPMessageNotificationBL.SaveAllAppmessageNotificationSettingEntityDto(settingSetDto.AppMessageNotificationSettingSet, settingSetDto.TransactionId, settingSetDto.ProjectId);
        }

        return null;
    }

    [HttpGet]
    public AppmessageNotificationSettingExDto RetrieveOneAppmessageNotificationSettingExDto(int? settingId)
    {
        if (settingId.HasValue)
        {
            var toRuren = APPMessageNotificationBL.RetrieveOneAppmessageNotificationSettingExDto(settingId.Value);
            return toRuren;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppmessageNotificationSettingExDto> SaveOneAppmessageNotificationSettingEntityDto(AppmessageNotificationSettingExDto aAppmessageNotificationSettingExDto)
    {
        if (aAppmessageNotificationSettingExDto != null)
        {
            return APPMessageNotificationBL.SaveOneAppmessageNotificationSettingEntityDto(aAppmessageNotificationSettingExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppmessageNotificationSettingEntityDto(int? settingId)
    {
        if (settingId.HasValue)
        {
            var toRuren = APPMessageNotificationBL.DeleteOneAppmessageNotificationSettingEntityDto(settingId.Value);
            return toRuren;
        }

        return null;
    }
}
