import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class AppMessageService {
  
  async retrieveProjectActivityMessageList(projectActivityId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveProjectActivityMessageList?projectActivityId=${projectActivityId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve project activity messages');
    return response.json();
  }

  async retrieveAllPredefinedMessageTemplates(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveAllPredefinedMessageTemplates`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve message templates');
    return response.json();
  }

  async retrieveTransactionMessageTemplates(transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveTransactionMessageTemplates?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction message templates');
    return response.json();
  }

  async retrieveOneAppMessageExDto(messageId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveOneAppMessageExDto?messageId=${messageId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve message');
    return response.json();
  }

  async saveOneAppMessageDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/SaveOneAppMessageDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save message');
    return response.json();
  }

  /** Angular: `DeleteMessagesByIdList` — body is `number[]` (message id list). */
  async deleteMessagesByIdList(messageIdList: number[]): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/DeleteMessagesByIdList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(messageIdList ?? [])
    });
    if (!response.ok) throw new Error('Failed to delete messages');
    return response.json();
  }

  async updateMessageAttachedFiles(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/UpdateMessageAttachedFiles`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update message attachments');
    return response.json();
  }

  async getOneTaskDefaultConversationId(taskId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/GetOneTaskDefaultConversactionId?taskId=${taskId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get task conversation ID');
    return response.json();
  }

  async getTransactionDefaultConversationId(transactionId: string, transactionRId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/GetTransactionDefaultConversactionId?transactionId=${transactionId}&transactionRId=${transactionRId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get transaction conversation ID');
    return response.json();
  }

  async retrieveCurrentUserUnReadMessages(): Promise<any> {
    try {
      const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserUnReadMessages`, {
        headers: getHeaders()
      });
      if (!response.ok) throw new Error('Failed to retrieve unread messages');
      return response.json();
    } catch (error) {
      // Handle network errors (Failed to fetch, CORS, etc.)
      if (error instanceof TypeError && error.message === 'Failed to fetch') {
        // Network error - return empty array instead of throwing
        // This prevents UI from breaking when server is unreachable
        return [];
      }
      // Re-throw other errors
      throw error;
    }
  }

  async retrieveCurrentUserInComeMessages(transactionId: string, transactionRid: string, messageScopeType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserInComeMessages?transactionId=${transactionId}&transctionRid=${transactionRid}&messageScopeType=${messageScopeType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve incoming messages');
    return response.json();
  }

  async retrieveCurrentUserOutComeMessages(transactionId: string, transactionRid: string, messageScopeType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserOutComeMessages?transactionId=${transactionId}&transctionRid=${transactionRid}&messageScopeType=${messageScopeType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve outgoing messages');
    return response.json();
  }

  async retrieveCurrentUserDeletedMessages(transactionId: string, transactionRid: string, messageScopeType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserDeletedMessages?transactionId=${transactionId}&transctionRid=${transactionRid}&messageScopeType=${messageScopeType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve deleted messages');
    return response.json();
  }

  async retrieveCurrentUserDraftMessages(transactionId: string, transactionRid: string, messageScopeType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserDraftMessages?transactionId=${transactionId}&transctionRid=${transactionRid}&messageScopeType=${messageScopeType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve draft messages');
    return response.json();
  }

  async retrieveTransactionFormMessages(transactionId: string, transactionRid: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveTransactionFormMessages?transactionId=${transactionId}&transctionRid=${transactionRid}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction form messages');
    return response.json();
  }

  async retrieveCurrentUserAllConversations(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserAllConversations`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve all conversations');
    return response.json();
  }

  async setMessageReadState(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/SetMessageReadState`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to set message read state');
    return response.json();
  }

  async deleteUserMessages(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/DeleteUserMessages`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to delete user messages');
    return response.json();
  }

  async getMessageReplyDto(messageId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/GetMessageReplyDto?messageId=${messageId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get message reply data');
    return response.json();
  }

  async getMessageReplyAllDto(messageId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/GetMessageReplyAllDto?messageId=${messageId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get message reply all data');
    return response.json();
  }

  async getMessageForwardDto(messageId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/GetMessageForwardDto?messageId=${messageId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get message forward data');
    return response.json();
  }

  async retrieveScopeMessageGroupDto(scopeType: string, transactionId: string, transactionRId: string, taskId: string, projectOrWorkflowId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveScopeMessageGroupDto?scopeType=${scopeType}&transactionId=${transactionId}&transactionRId=${transactionRId}&taskId=${taskId}&projectOrWorkflowId=${projectOrWorkflowId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve scope message group');
    return response.json();
  }

  async updateUserMessgeFollowupByScope(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/UpdateUserMessgeFollowupByScope`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update message followup');
    return response.json();
  }

  async retrieveAppUserMessageFollowupDtoListByScope(scopeType: string, transactionId: string, transactionRId: string, taskId: string, projectOrWorkflowId: string, projectTeamId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveAppUserMessgeFollowupDtoListByScope?scopeType=${scopeType}&transactionId=${transactionId}&transactionRId=${transactionRId}&taskId=${taskId}&projectOrWorkflowId=${projectOrWorkflowId}&projectTeamId=${projectTeamId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve message followup list');
    return response.json();
  }

  async retrieveAllAppmessageNotificationSettingEntityDto(
    transactionId: string,
    projectId: string,
    usageType: string
  ): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppMessage/RetrieveAllAppmessageNotificationSettingEntityDto?transactionId=${transactionId || ''}&projectId=${projectId || ''}&usageType=${usageType || ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to retrieve notification settings');
    return response.json();
  }

  async retrieveOneAppmessageNotificationSettingExDto(settingId: string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppMessage/RetrieveOneAppmessageNotificationSettingExDto?settingId=${settingId || ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to retrieve notification setting');
    return response.json();
  }

  async saveOneAppmessageNotificationSettingEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/SaveOneAppmessageNotificationSettingEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save notification setting');
    return response.json();
  }

  async deleteOneAppmessageNotificationSettingEntityDto(settingId: string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/AppMessage/DeleteOneAppmessageNotificationSettingEntityDto?settingId=${settingId || ''}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to delete notification setting');
    return response.json();
  }
}

export const appMessageService = new AppMessageService(); 