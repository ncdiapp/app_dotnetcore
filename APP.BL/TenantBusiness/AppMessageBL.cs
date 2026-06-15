using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
//using Microsoft.Office.Interop.Word;
#if NETFRAMEWORK
using Microsoft.Exchange.WebServices.Data;
// TODO-PHASE4: Replace with .NET 10 equivalent
#endif
using System.Diagnostics;
using System.ComponentModel.Design;
using System.Threading.Tasks;

using APP.Framework;
namespace App.BL
{
    public static class AppMessageBL
    {
        public static readonly string UtcDateTimeTicks_Token_StringFormat = "[" + EmAppMessagePlaceHolderToken.UtcTicks.ToString() + "_{0}]";


        //ProjectID,
        //ProjectTeamID,
        //ProjectActivityID,
        //TransactionRootValueID,TransactionID


        // Link Target



        //public static IEnumerable<AppMessageDto> RetrieveAllMessageTemplates()
        //{
        //    IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.MessgaeScopeType == (int)EmAppMessgaeScopeType.MessageTemplate);


        //    return GetMessageDtoList(filter);
        //}

        public static ObservableSet<AppMessageDto> RetrieveAllPredefinedMessageTemplates()
        {
            //IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.IsPredefinedTemplate == true);
            //return GetMessageDtoList(filter);

            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.MessgaeScopeType == (int)EmAppMessgaeScopeType.MessageTemplate);
            return GetMessageDtoList(filter);
        }

        public static ObservableSet<AppMessageDto> RetrieveTransactionMessageTemplates(int transactionId)
        {
            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.MessgaeScopeType == (int)EmAppMessgaeScopeType.MessageTemplate
                & AppMessageFields.TransactionId == transactionId);

            return GetMessageDtoList(filter);
        }



        public static ObservableSet<AppMessageDto> RetrieveOneTransactionMessageList(int transactionId, object transactionRootValueId)
        {

            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.TransactionId == transactionId);
            filter.PredicateExpression.AddWithAnd(AppMessageFields.TransactionRootValueId == transactionRootValueId);
            return GetMessageDtoList(filter);
        }

        public static ObservableSet<AppMessageDto> RetrieveProjectTeamMessageList(int ProjectTeamId)
        {

            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.ProjectTeamId == ProjectTeamId);

            return GetMessageDtoList(filter);
        }

        public static ObservableSet<AppMessageDto> RetrieveProjectActivityMessageList(int projectActivityId)
        {

            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.ProjectActivityId == projectActivityId);

            return GetMessageDtoList(filter);
        }

        public static ObservableSet<AppMessageDto> RetrieveProjectOrWorkflowMessageList(int projectId)
        {
            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.ProjectId == projectId &
                    (AppMessageFields.ProjectActivityId == System.DBNull.Value | AppMessageFields.MessagePostType == (int)EmAppMessgaePostType.SystemNotification)
                );
            return GetMessageDtoList(filter);
        }

        public static ObservableSet<AppMessageDto> RetrieveGlobalMessageList()
        {

            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.MessgaeScopeType == (int)EmAppMessgaeScopeType.Global);


            return GetMessageDtoList(filter);
        }



        public static OperationCallResult<object> DeleteMessagesByIdList(List<int> messageIdList)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = DeleteBatchMessageDto(messageIdList.ToArray());

            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }


        //public static ObservableSet<AppMessageDto> RetrieveTransactionFormMessageList(int transactionId, int rootPrimaryKey, int? messageType = null)
        //{
        //    if (messageType.HasValue)
        //    {
        //        IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.TransactionId == transactionId
        //            & AppMessageFields.TransactionRootValueId == rootPrimaryKey & AppMessageFields.MessagePostType == messageType.Value);
        //        return GetMessageDtoList(filter);
        //    }
        //    else
        //    {
        //        IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.TransactionId == transactionId & AppMessageFields.TransactionRootValueId == rootPrimaryKey);
        //        return GetMessageDtoList(filter);
        //    }


        //}

        public static string ConvertMessageAllUtcTicksTokenToClientDateTimeString(string messageText, string clientTimeZoneKey = "")
        {
            while (messageText.Contains("[" + EmAppMessagePlaceHolderToken.UtcTicks.ToString()))
            {
                int startIndex = messageText.IndexOf("[" + EmAppMessagePlaceHolderToken.UtcTicks.ToString());
                int endIndex = messageText.IndexOf("]", startIndex);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    int timeTicksLength = endIndex - startIndex - 10;
                    if (timeTicksLength > 0)
                    {
                        string needToReplaceString = messageText.Substring(startIndex, endIndex - startIndex + 1);

                        string timeTicksString = messageText.Substring(startIndex + 10, timeTicksLength);

                        long? timeTicks = ControlTypeValueConverter.ConvertValueToLong(timeTicksString);

                        string replaceToString = string.Empty;

                        if (timeTicks.HasValue)
                        {
                            DateTime utcTime = new DateTime(timeTicks.Value, DateTimeKind.Utc);

                            if (string.IsNullOrWhiteSpace(clientTimeZoneKey))
                            {
                                clientTimeZoneKey = ServerContext.Instance.CurrentUserTimeZoneKey;
                            }

                            DateTime clientTime = TimeZoneHelper.ConvertUTCToClientDateTime(utcTime, clientTimeZoneKey);
                            replaceToString = clientTime.ToString();
                        }

                        messageText = messageText.Replace(needToReplaceString, replaceToString);
                    }
                }
            }
            return messageText;
        }



        private static ObservableSet<AppMessageDto> GetMessageDtoList(IRelationPredicateBucket filter)
        {
            List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
            Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

            SortClause aSortClause = AppMessageFields.AppCreatedDate | SortOperator.Ascending;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppMessageEntity> entityList = new EntityCollection<AppMessageEntity>();
                adapter.FetchEntityCollection(entityList, filter, 0, new SortExpression(aSortClause));


                var aDtoList = new ObservableSet<AppMessageDto>();
                foreach (var AppMessageEntity in entityList)
                {
                    AppMessageDto dto = AppMessageConverter.ConvertEntityToDto(AppMessageEntity);
                    dto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(dto.Subject);
                    dto.Message = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(dto.Message);

                    if (dto.AppCreatedDate != null)
                    {
                        //dto.CreateDateString = dto.AppCreatedDate.ToString();
                    }

                    dto.CreateByUserName = "Unknown User";
                    if (dto.AppCreatedById.HasValue)
                    {
                        if (dictUserIdName.ContainsKey(dto.AppCreatedById.Value))
                        {
                            dto.CreateByUserName = dictUserIdName[dto.AppCreatedById.Value];
                        }
                    }

                    aDtoList.Add(dto);
                }
                return aDtoList;
            }
        }

        public static AppMessageEntity RetrieveOneAppMessageEntity(object id)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppMessageEntity aAppMessageEntity = new AppMessageEntity(int.Parse(id.ToString()));
                adpater.FetchEntity(aAppMessageEntity);
                return aAppMessageEntity;
            }
        }

        public static AppMessageExDto GetTransactionFormPrintLayoutFromMessageTemplate(int? messageTemplateId, int? transactionId, object rootKeyValue)
        {
            if (messageTemplateId.HasValue && transactionId.HasValue && rootKeyValue != null)
            {
                AppMessageExDto messageTemplateDto = RetrieveOneAppMessageExDto(messageTemplateId.Value);

                if (messageTemplateDto != null)
                {
                    var appformDataDto = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, rootKeyValue);
                    var appTransactionExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);

                    Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(appformDataDto, appTransactionExDto);

                    string currentDatetime = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                    string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                    string workflowName = string.Empty;
                    string taskName = string.Empty;
                    string taskStatus = string.Empty;
                    string toList = string.Empty;

                    string messageTempalte = messageTemplateDto.Message;
                    messageTempalte = GetTransactionFormPrintLayoutFromMessageTemplate_ProcessChildUnit(messageTemplateId, rootKeyValue, appformDataDto, appTransactionExDto, allFreshRootValue, currentDatetime, currentUserName, workflowName, taskName, taskStatus, messageTempalte);

                    string subject = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, "", currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, null, messageTemplateId.Value, true);
                    string messageBody = AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, messageTempalte, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, null, null, false);


                    messageTemplateDto.Subject = subject;
                    messageTemplateDto.Message = messageBody;

                    return messageTemplateDto;
                }

            }

            return null;
        }

      
        public static AppMessageExDto RetrieveOneAppMessageExDto(object id)
        {
            AppMessageEntity aAppMessageEntity = RetrieveOneAppMessageEntity(id);
            AppMessageExDto dto = AppMessageConverter.ConvertEntityToExDto(aAppMessageEntity);
            dto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(dto.Subject);
            dto.Message = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(dto.Message);

            if (dto.AppCreatedDate != null)
            {
                //dto.CreateDateString = dto.AppCreatedDate.ToString();
            }

            var userEntityList = AppSecurityUserBL.RetrieveAllAppSecurityUserEntity();
            string[] toEmails = (dto.ToList).Split(";".ToArray());
            List<string> toEmailList = new List<string>();

            foreach (var anEmail in toEmails)
            {
                if (!string.IsNullOrEmpty(anEmail.Trim()))
                {
                    string emailaddress = anEmail.Trim().ToLower();

                    int paramStart = emailaddress.IndexOf("[");
                    int paramEnd = emailaddress.IndexOf("]");

                    if (paramStart >= 0 && paramEnd > paramStart)
                    {
                        emailaddress = emailaddress.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    }

                    if (!toEmailList.Contains(emailaddress))
                    {
                        toEmailList.Add(emailaddress);
                    }
                }
            }

            dto.ToUserIdList = new List<int>();

            foreach (string toEmail in toEmailList)
            {
                var foundUser = userEntityList.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.Email) && o.Email.ToLower().Trim() == toEmail.ToLower().Trim());
                if (foundUser != null)
                {
                    if (!dto.ToUserIdList.Contains(foundUser.UserId))
                    {
                        dto.ToUserIdList.Add(foundUser.UserId);
                    }
                }
            }

            List<LookupItemDto> userLookupItems = AppEntityInfoBL.GetLookupItemListByCode(EmAppEntityLookupInfoCode.AppSecurityUser.ToString(), "");
            Dictionary<int, string> dictUserIdName = userLookupItems.ToDictionary(o => (int)o.Id, o => o.Display);

            dto.CreateByUserName = "Unknown User";
            if (dto.AppCreatedById.HasValue)
            {


                if (dictUserIdName.ContainsKey(dto.AppCreatedById.Value))
                {
                    dto.CreateByUserName = dictUserIdName[dto.AppCreatedById.Value];
                }
            }

            dto.AttachmentFileDtoList = GetFileDtoListFromFileIdToken(dto);

            dto.DictAttachmentFileIdAndDisplay = dto.AttachmentFileDtoList.ToDictionary(o => (int)o.Id, o => o.FileCode);

            //if (dto.MessagePostType.HasValue && dto.MessagePostType.Value == (int)EmAppMessgaePostType.Conversaction)
            //{
            //    dto.ConversationMessageList = new List<AppMessageDto>();
            //    if (!string.IsNullOrEmpty(dto.Message))
            //    {
            //        string[] conversationMessages = (dto.Message).Split(AppMessageDto.ConversationMessageSeperateToken.ToArray());

            //        foreach (string aConversationMessageString in conversationMessages)
            //        {
            //            if (!string.IsNullOrEmpty(aConversationMessageString))
            //            {
            //                string[] messagePropertys = aConversationMessageString.Split(AppMessageDto.ConversationMessagePropertySeperateToken.ToArray());
            //                if (messagePropertys.Length >= 4)
            //                {
            //                    AppMessageDto aConversationMessageDto = new AppMessageDto();

            //                    aConversationMessageDto.Subject = messagePropertys[0];
            //                    aConversationMessageDto.Message = messagePropertys[1];

            //                    aConversationMessageDto.AppCreatedDate = ControlTypeValueConverter.ConvertValueToDate(messagePropertys[2]);
            //                    if (aConversationMessageDto.AppCreatedDate.HasValue)
            //                    {
            //                        aConversationMessageDto.CreateDateString = ClientTimeZoneHelper.ConvertUTCToClientDateTime(aConversationMessageDto.AppCreatedDate.Value).ToString();
            //                    }

            //                    aConversationMessageDto.AppCreatedById = ControlTypeValueConverter.ConvertValueToInt(messagePropertys[3]);
            //                    if (aConversationMessageDto.AppCreatedById.HasValue && dictUserIdName.ContainsKey(aConversationMessageDto.AppCreatedById.Value))
            //                    {
            //                        aConversationMessageDto.CreateByUserName = dictUserIdName[aConversationMessageDto.AppCreatedById.Value];
            //                    }

            //                    if (messagePropertys.Length >= 5)
            //                    {
            //                        aConversationMessageDto.MessagePostType = ControlTypeValueConverter.ConvertValueToInt(messagePropertys[4]);
            //                    }

            //                    dto.ConversationMessageList.Add(aConversationMessageDto);
            //                }
            //            }
            //        }
            //    }
            //}

            if (dto.MessgaeScopeType == (int)EmAppMessgaeScopeType.Task)
            {
                if (dto.ProjectActivityId.HasValue)
                {
                    AppProjectWorkFlowTaskExDto taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(dto.ProjectActivityId.Value);
                    if (taskDto != null && taskDto.EmAppTaskSystemDefinedCategory == (int)EmAppTaskSystemDefinedCategory.ProjectTask && taskDto.ProjectId.HasValue)
                    {
                        dto.LinkToProjectId = taskDto.ProjectId;
                    }
                }
            }
            else if (dto.MessgaeScopeType == (int)EmAppMessgaeScopeType.Project)
            {
                dto.LinkToProjectId = dto.ProjectId;
            }

            return dto;
        }



        public static List<AppFileExDto> GetFileDtoListFromFileIdToken(AppMessageDto messageDto)
        {
            string attachmentFileToken = messageDto.AttachmentFileToken;

            List<AppFileExDto> fileDtoList = new List<AppFileExDto>();

            fileDtoList = AppFileBL.RetrieveMultipleFileExDtoByIds(messageDto.AttachmentFieldIds);



            return fileDtoList;
        }

        public static OperationCallResult<object> SaveOneAppMessageDto(AppMessageDto aAppMessageDto)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            bool isNeedToCalculateToUserList = false;

            if (aAppMessageDto.MessagePostType.HasValue && aAppMessageDto.MessagePostType.Value == (int)EmAppMessgaePostType.Conversaction
                && aAppMessageDto.TransactionId.HasValue && !string.IsNullOrWhiteSpace(aAppMessageDto.TransactionRootValueId))
            {
                var transactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(aAppMessageDto.TransactionId.Value);

                if (transactionDto.OtherOptions != null)
                {
                    if (transactionDto.OtherOptions.CommunicationGroupByType.HasValue)
                    {
                        int groupBy = transactionDto.OtherOptions.CommunicationGroupByType.Value;

                        int currentUserDomainId = AppSecurityUserBL.CurrentUserEntity.DomainId;

                        if (currentUserDomainId == (int)EmAppUserType.Customer
                                    && (groupBy == (int)EmAppConversationGroupByType.GroupByCustomer || groupBy == (int)EmAppConversationGroupByType.GroupByCustomerUser))
                        {
                            isNeedToCalculateToUserList = true;
                            aAppMessageDto.ToUserIdList = new List<int>();

                        }
                        else if (currentUserDomainId == (int)EmAppUserType.Supplier
                            && (groupBy == (int)EmAppConversationGroupByType.GroupBySupplier || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierUser))
                        {
                            isNeedToCalculateToUserList = true;
                            aAppMessageDto.ToUserIdList = new List<int>();
                        }
                        else if (currentUserDomainId == (int)EmAppUserType.ClientAgent
                           && (groupBy == (int)EmAppConversationGroupByType.GroupByClientAgent || groupBy == (int)EmAppConversationGroupByType.GroupByClientAgentUser))
                        {
                            isNeedToCalculateToUserList = true;
                            aAppMessageDto.ToUserIdList = new List<int>();
                        }
                        else if (currentUserDomainId == (int)EmAppUserType.SupplierAgent
                           && (groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgent || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgentUser))
                        {
                            isNeedToCalculateToUserList = true;
                            aAppMessageDto.ToUserIdList = new List<int>();
                        }
                        else
                        {
                            int? subgroupId = ControlTypeValueConverter.ConvertValueToInt(aAppMessageDto.SubGroupId);
                            if (subgroupId.HasValue)
                            {


                                isNeedToCalculateToUserList = true;
                                aAppMessageDto.ToUserIdList = new List<int>();

                                if (groupBy == (int)EmAppConversationGroupByType.GroupByCustomer
                                    || groupBy == (int)EmAppConversationGroupByType.GroupBySupplier
                                    || groupBy == (int)EmAppConversationGroupByType.GroupByClientAgent
                                    || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgent)
                                {
                                    int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
                                    if (companyId.HasValue)
                                    {
                                        var dictUserIdPartnerId = AppCacheManagerBL.GetOneCompanyUserIdAndPartnerIdDictionary(companyId.Value);

                                        foreach (int userId in dictUserIdPartnerId.Keys)
                                        {
                                            int partnerId = dictUserIdPartnerId[userId];
                                            if (partnerId == subgroupId.Value)
                                            {
                                                aAppMessageDto.ToUserIdList.Add(userId);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    aAppMessageDto.ToUserIdList.Add(subgroupId.Value);
                                }

                            }
                        }
                    }
                }
            }


            var dictUserDto = AppSecurityUserBL.DictAllUserDto;

            //string messageLinks = BuildMessageLinkString(aAppMessageDto);

            //if (!string.IsNullOrWhiteSpace(messageLinks))
            //{
            //    aAppMessageDto.Message += "<br >" + messageLinks;
            //}

            List<int> userIdList = new List<int>();

            if (aAppMessageDto.ToUserIdList != null && aAppMessageDto.ToUserIdList.Count > 0)
            {
                foreach (int userId in aAppMessageDto.ToUserIdList.Distinct())
                {
                    if (dictUserDto.ContainsKey(userId))
                    {
                        var userDto = dictUserDto[userId];

                        if (!string.IsNullOrEmpty(userDto.Email.Trim()))
                        {
                            aAppMessageDto.ToList += userDto.Email.Trim() + ";";
                        }

                        userIdList.Add(userId);
                    }
                }
            }

            if (aAppMessageDto.ProjectActivityId.HasValue)
            {
                AddTaskResourceUserEmailsToMessageToList(aAppMessageDto);
            }
            List<string> sendToEmailList = new List<string>();

            if (!(aAppMessageDto.MessgaeScopeType.HasValue
                && aAppMessageDto.MessgaeScopeType.Value == (int)EmAppMessgaeScopeType.MessageTemplate))
            {
                sendToEmailList = RebuildMessageDtoEmailList(aAppMessageDto);
            }


            AppMessageEntity aAppMessageEntity;
            if (aAppMessageDto.IsNew)
            {
                aAppMessageEntity = new AppMessageEntity();
            }
            else
            {
                aAppMessageEntity = RetrieveOneAppMessageEntity(aAppMessageDto.Id);
            }

            if (AppSecurityUserBL.CurrentUserEntity != null && AppSecurityUserBL.CurrentUserEntity.Email.IsEmpty())
            {
                aAppMessageDto.FromEmail = AppSecurityUserBL.CurrentUserEntity.Email;
            }
            else
            {

            }

            // From URl
            //AttachmentFileToken: "16|17|18"


            if (aAppMessageDto.TransactionId.HasValue && !string.IsNullOrWhiteSpace(aAppMessageDto.TransactionRootValueId))
            {
                //TODO , need to gerrate print doc and save to file mangmnet sa pdf file

                List<int> tranFileIdList = new List<int>();
                if (aAppMessageDto.IsAttachFormPrintDoc)
                {
                    // Call Pdf Genrate to create a file 

                    var pdfFields = AppTransactionReportBLBL.GenerateReportPdfFile(aAppMessageDto.TransactionId, aAppMessageDto.TransactionRootValueId);

                    tranFileIdList.AddRange(pdfFields);
                }


                if (aAppMessageDto.IsAttachAllFormFiles)
                {
                    List<int> formControlFileIdList = AppMasterDetailFormPrintBL.GetTransactionFormFileIDList(aAppMessageDto.TransactionId.Value, aAppMessageDto.TransactionRootValueId);

                    tranFileIdList.AddRange(formControlFileIdList);

                    if (tranFileIdList != null)
                    {
                        tranFileIdList.AddRange(aAppMessageDto.AttachmentFieldIds);

                        tranFileIdList = tranFileIdList.Distinct().ToList();

                        aAppMessageDto.AttachmentFileToken = string.Empty;

                        foreach (int fileId in tranFileIdList)
                        {
                            if (!string.IsNullOrWhiteSpace(aAppMessageDto.AttachmentFileToken))
                            {
                                aAppMessageDto.AttachmentFileToken += "|" + fileId.ToString();
                            }
                            else
                            {
                                aAppMessageDto.AttachmentFileToken = fileId.ToString();
                            }
                        }
                    }

                }

            }




            var allUsers = AppSecurityUserBL.DictAllUserDto.Values;


            AppMessageConverter.CopyDtoToEntity(aAppMessageEntity, aAppMessageDto);

            if (!(aAppMessageDto.MessgaeScopeType.HasValue && aAppMessageDto.MessgaeScopeType.Value == (int)EmAppMessgaeScopeType.MessageTemplate) && !(aAppMessageDto.IsDraft.HasValue && aAppMessageDto.IsDraft.Value))
            {
                foreach (string anEmail in sendToEmailList)
                {
                    int? receiveUserId = null;
                    var matchedUser = allUsers.FirstOrDefault(o => !string.IsNullOrEmpty(o.Email) && o.Email.Trim().ToLower() == anEmail);

                    // for extern user email adress, there is no need to save  appMessageUserReceived Box
                    if (matchedUser != null)
                    {
                        receiveUserId = ControlTypeValueConverter.ConvertValueToInt(matchedUser.Id);

                        AppMessageUserReceivedExDto appMessageUserReceivedExDto = new AppMessageUserReceivedExDto() { ReceivedEmail = matchedUser.Email.Trim().ToLower(), ReceivedById = receiveUserId };
                        AppMessageUserReceivedEntity appMessageUserReceivedEntity = new AppMessageUserReceivedEntity();
                        AppMessageUserReceivedConverter.CopyDtoToEntity(appMessageUserReceivedEntity, appMessageUserReceivedExDto);
                        aAppMessageEntity.AppMessageUserReceived.Add(appMessageUserReceivedEntity);


                        if (receiveUserId.HasValue && !userIdList.Contains(receiveUserId.Value))
                        {
                            userIdList.Add(receiveUserId.Value);
                        }
                    }

                }
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppMessageEntity, false, true);

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    aOperationCallResult.Object = aAppMessageEntity.MessageId;
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }


            if (!aValidationResult.HasErrors)
            {
                if (!string.IsNullOrWhiteSpace(aAppMessageDto.ToList)
                    && !(aAppMessageDto.MessgaeScopeType.HasValue && aAppMessageDto.MessgaeScopeType.Value == (int)EmAppMessgaeScopeType.MessageTemplate)
                    && !(aAppMessageDto.IsDraft.HasValue && aAppMessageDto.IsDraft.Value))
                {


                    aAppMessageDto.Id = aAppMessageEntity.MessageId;

                    bool isSendEmailSuccess = SendEmailFromAppMessageDto(aAppMessageDto);

                    if (!isSendEmailSuccess)
                    {
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_SendEmailFailed", ValidationItemType.Error, "Send email failed."));
                    }
                }
            }


            return aOperationCallResult;
        }


        private static string BuildFormHtmlPrintLayout(int? transactionId, string transactionRId)
        {
            if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
            {
                string domainAndApplcationpath = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);

                //string sessionId = ServerContext.Instance.CurrentSessionId as string;

                //string compnayId = sessionId;

                //string url = domainAndApplcationpath + "/FormMgt/FormSimplePrint?transactionId=" + transactionId.Value.ToString() + "&rootPrimaryKeyValue=" + transactionRId;

                string url = domainAndApplcationpath + "/FormMgt/FormMasterDetailPrint?transactionId=" + transactionId.Value.ToString() + "&rootPrimaryKeyValue=" + transactionRId;


                AppClientIdentity aAppClientIdentity = (AppClientIdentity)ServerContext.Instance.CurrnetClientIdentity;

                string fromPrintLayoutString = DocumentHelper.GetHttpUrlContentString(url, aAppClientIdentity);

                return fromPrintLayoutString;

            }

            return string.Empty;
        }


        private static string BuildMessageLinkString(AppMessageDto aAppMessageDto)
        {
            string links = string.Empty; //< br /><div></div>



            string domainAndApplcationpath = AppSystemSettingBL.GetStringValue(EmSystemSettings.ApplicationURL);


            int? transactionId = null;
            string transactionRid = string.Empty;
            int? projectId = null;
            int? workflowId = null;
            int? taskId = aAppMessageDto.ProjectActivityId;

            if (aAppMessageDto.MessgaeScopeType == (int)EmAppMessgaeScopeType.Task)
            {
                if (aAppMessageDto.ProjectActivityId.HasValue)
                {
                    AppProjectWorkFlowTaskExDto taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(aAppMessageDto.ProjectActivityId.Value);

                    if (taskDto != null)
                    {
                        if (taskDto.EmAppTaskSystemDefinedCategory == (int)EmAppTaskSystemDefinedCategory.WorkflowTask)
                        {
                            if (taskDto.ProjectId.HasValue)
                            {
                                workflowId = taskDto.ProjectId;
                                var workflowEntity = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectOrWorkFlowSimpleEntity(taskDto.ProjectId.Value);
                                if (workflowEntity != null && workflowEntity.TransactionId.HasValue && !string.IsNullOrEmpty(workflowEntity.TransactionRid))
                                {
                                    transactionId = workflowEntity.TransactionId;
                                    transactionRid = workflowEntity.TransactionRid;
                                }
                            }
                        }
                        else if (taskDto.EmAppTaskSystemDefinedCategory == (int)EmAppTaskSystemDefinedCategory.ProjectTask)
                        {
                            if (taskDto.ProjectId.HasValue)
                            {
                                projectId = taskDto.ProjectId;
                            }
                        }
                        else if (taskDto.EmAppTaskSystemDefinedCategory == (int)EmAppTaskSystemDefinedCategory.SimpleFormTask)
                        {
                            if (taskDto.TransactionId.HasValue && !string.IsNullOrEmpty(taskDto.TransactionRid))
                            {
                                transactionId = taskDto.TransactionId;
                                transactionRid = taskDto.TransactionRid;
                            }
                        }
                        else if (taskDto.EmAppTaskSystemDefinedCategory == (int)EmAppTaskSystemDefinedCategory.UserDefinedFreeTask)
                        {

                        }
                    }
                }
            }
            else if (aAppMessageDto.MessgaeScopeType == (int)EmAppMessgaeScopeType.Project)
            {
                projectId = aAppMessageDto.ProjectId;
            }
            else if (aAppMessageDto.MessgaeScopeType == (int)EmAppMessgaeScopeType.Workflow)
            {
                workflowId = aAppMessageDto.ProjectId;
            }
            else if (aAppMessageDto.MessgaeScopeType == (int)EmAppMessgaeScopeType.Transaction)
            {
                transactionId = aAppMessageDto.TransactionId;
                transactionRid = aAppMessageDto.TransactionRootValueId;
            }

            if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRid))
            {
                string url = domainAndApplcationpath + "/Home/Mgt/#/main/FormMasterDetail?Id=" + transactionId.Value + "&amp;" + "param1=" + transactionRid;

                //links += "<a target='_blank' href='" + url + "' style='color:blue;font-size:10px;padding:2px 5px;'>Click To View Details</a>";
            }

            if (projectId.HasValue)
            {
                string url = domainAndApplcationpath + "/Home/Mgt/#/main/ProjectMgt?Id=" + projectId.Value;

                links += "<a target='_blank' href='" + url + "' style='color:blue;font-size:10px;padding:2px 5px;'>Click To View Project Details</a>";
            }


            return links;
        }

        //public static OperationCallResult<object> SaveOneConversationExDto(AppMessageDto aAppMessageDto)
        //{


        //    if (aAppMessageDto.MessagePostType.HasValue && aAppMessageDto.MessagePostType.Value == (int)EmAppMessgaePostType.Conversaction
        //        && aAppMessageDto.NewConversationMessage != null)
        //    {
        //        OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
        //        ValidationResult aValidationResult = new ValidationResult();
        //        aOperationCallResult.ValidationResult = aValidationResult;


        //        var allUsers = AppSecurityUserBL.DictAllUserDto.Values;


        //        //AppMessageDto emailMessageDto = new AppMessageDto();
        //        //emailMessageDto.Subject = aAppMessageDto.Subject;
        //        //emailMessageDto.Message = aAppMessageDto.Message;
        //        //emailMessageDto.ToList = aAppMessageDto.ToList;
        //        //emailMessageDto.Cclist = string.Empty;
        //        //emailMessageDto.Bcclist = string.Empty;
        //        //emailMessageDto.AttachmentFileToken = aAppMessageDto.AttachmentFileToken;


        //        AppMessageDto emailMessageDto = aAppMessageDto.DeepCopy();
        //        emailMessageDto.Id = null;

        //        emailMessageDto.MessagePostType = (int)EmAppMessgaePostType.SystemNotification;

        //        emailMessageDto.Message = string.Empty;

        //        if (emailMessageDto.MessgaeScopeType.HasValue)
        //        {
        //            if (emailMessageDto.MessgaeScopeType.Value == (int)EmAppMessgaeScopeType.Task)
        //            {
        //                if (emailMessageDto.ProjectActivityId.HasValue)
        //                {
        //                    var taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(emailMessageDto.ProjectActivityId.Value);
        //                    emailMessageDto.Message = "You have a new task message from task: " + taskDto.Name + "(" + taskDto.Id.ToString() + ")";
        //                }
        //            }
        //        }




        //        AppMessageEntity aAppMessageEntity;
        //        if (aAppMessageDto.IsNew)
        //        {
        //            aAppMessageEntity = new AppMessageEntity();

        //            AppMessageConverter.CopyDtoToEntity(aAppMessageEntity, aAppMessageDto);

        //            aAppMessageDto.FromEmail = AppSecurityUserBL.CurrentUserEntity.Email;

        //            aAppMessageEntity.Message = string.Empty;

        //            string createdDateString = aAppMessageEntity.AppCreatedDate.ToString();
        //            string createdByIdString = aAppMessageEntity.AppCreatedById.ToString();

        //            string postTypeString = ((int)EmAppMessgaePostType.UserNotification).ToString();

        //            //Subject₸Message₸AppCreatedDate₸AppCreatedById
        //            if (!string.IsNullOrWhiteSpace(aAppMessageDto.Message))
        //            {
        //                aAppMessageEntity.Message =
        //                    aAppMessageDto.Subject + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                    aAppMessageDto.Message + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                    createdDateString + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                    createdByIdString + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                    postTypeString;


        //                emailMessageDto.Message = aAppMessageDto.Message;


        //            }
        //        }
        //        else
        //        {
        //            //aAppMessageDto.NewConversationMessage.FromEmail = AppSecurityUserBL.CurrentUserEntity.Email;
        //            aAppMessageEntity = RetrieveOneAppMessageEntity(aAppMessageDto.Id);

        //            AppMessageEntity newConversationMessageEntity = new AppMessageEntity();
        //            AppMessageConverter.CopyDtoToEntity(newConversationMessageEntity, aAppMessageDto.NewConversationMessage);

        //            aAppMessageEntity.FromEmail = aAppMessageDto.FromEmail;
        //            aAppMessageEntity.ToList = aAppMessageDto.ToList;

        //            aAppMessageEntity.AppModifiedById = newConversationMessageEntity.AppCreatedById;
        //            aAppMessageEntity.AppModifiedDate = newConversationMessageEntity.AppCreatedDate;

        //            string createdDateString = newConversationMessageEntity.AppCreatedDate.ToString();
        //            string createdByIdString = newConversationMessageEntity.AppCreatedById.ToString();

        //            string postTypeString = ((int)EmAppMessgaePostType.UserNotification).ToString();
        //            if (newConversationMessageEntity.MessagePostType.HasValue)
        //            {
        //                postTypeString = newConversationMessageEntity.MessagePostType.Value.ToString();
        //            }

        //            //║Subject₸Message₸AppCreatedDate₸AppCreatedById
        //            aAppMessageEntity.Message +=
        //                AppMessageDto.ConversationMessageSeperateToken +
        //                aAppMessageDto.NewConversationMessage.Subject + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                aAppMessageDto.NewConversationMessage.Message + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                createdDateString + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                createdByIdString + AppMessageDto.ConversationMessagePropertySeperateToken +
        //                postTypeString;

        //            aAppMessageEntity.AttachmentFileToken = aAppMessageDto.AttachmentFileToken;



        //            emailMessageDto.Message = aAppMessageDto.NewConversationMessage.Message;
        //        }


        //        if (aAppMessageDto.IsNew || aAppMessageDto.IsConversationMemberChanged)
        //        {
        //            List<string> sendToEmailList = RebuildMessageDtoEmailList(aAppMessageDto);

        //            foreach (string anEmail in sendToEmailList)
        //            {
        //                int? receiveUserId = null;
        //                var matchedUser = allUsers.FirstOrDefault(o => !string.IsNullOrEmpty(o.Email) && o.Email.Trim().ToLower() == anEmail);

        //                // for extern user email adress, there is no need to save  appMessageUserReceived Box
        //                if (matchedUser != null)
        //                {
        //                    receiveUserId = ControlTypeValueConverter.ConvertValueToInt(matchedUser.Id);

        //                    AppMessageUserReceivedExDto appMessageUserReceivedExDto = new AppMessageUserReceivedExDto() { ReceivedEmail = matchedUser.Email.Trim().ToLower(), ReceivedById = receiveUserId };
        //                    AppMessageUserReceivedEntity appMessageUserReceivedEntity = new AppMessageUserReceivedEntity();
        //                    AppMessageUserReceivedConverter.CopyDtoToEntity(appMessageUserReceivedEntity, appMessageUserReceivedExDto);
        //                    aAppMessageEntity.AppMessageUserReceived.Add(appMessageUserReceivedEntity);
        //                }
        //            }
        //        }


        //        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //        {
        //            try
        //            {
        //                adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

        //                if (aAppMessageDto.Id != null && aAppMessageDto.IsConversationMemberChanged)
        //                {
        //                    adapter.DeleteEntitiesDirectly(typeof(AppMessageUserReceivedEntity), new RelationPredicateBucket(AppMessageUserReceivedFields.MessageId == (int)aAppMessageDto.Id));
        //                }


        //                adapter.SaveEntity(aAppMessageEntity, false, true);

        //                adapter.Commit();
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

        //                aOperationCallResult.Object = aAppMessageEntity.MessageId;

        //                if (emailMessageDto.ToList.Length > 0 && !string.IsNullOrWhiteSpace(emailMessageDto.Message))
        //                {
        //                    SaveOneAppMessageDto(emailMessageDto);
        //                }

        //            }


        //            // Database FK Exception .......
        //            catch (ORMQueryExecutionException ex)
        //            {
        //                adapter.Rollback();
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
        //            }
        //        }


        //        return aOperationCallResult;
        //    }

        //    return null;




        //}




        //public static OperationCallResult<object> SaveOneConversationExDto(AppMessageDto aAppMessageDto)
        //{


        //    if (aAppMessageDto.MessagePostType.HasValue && aAppMessageDto.MessagePostType.Value == (int)EmAppMessgaePostType.Conversaction)
        //    {
        //        OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
        //        ValidationResult aValidationResult = new ValidationResult();
        //        aOperationCallResult.ValidationResult = aValidationResult;


        //        var allUsers = AppSecurityUserBL.DictAllUserDto.Values;


        //        //AppMessageDto emailMessageDto = aAppMessageDto.DeepCopy();
        //        //emailMessageDto.Id = null;
        //        //emailMessageDto.MessagePostType = (int)EmAppMessgaePostType.SystemNotification;


        //        //if (emailMessageDto.MessgaeScopeType.HasValue)
        //        //{
        //        //    if (emailMessageDto.MessgaeScopeType.Value == (int)EmAppMessgaeScopeType.Task)
        //        //    {
        //        //        if (emailMessageDto.ProjectActivityId.HasValue)
        //        //        {
        //        //            var taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(emailMessageDto.ProjectActivityId.Value);
        //        //            emailMessageDto.Message = "You have a new message from task: " + taskDto.Name + "(" + taskDto.Id.ToString() + ")";
        //        //        }
        //        //    }
        //        //}

        //        AppMessageEntity aAppMessageEntity = new AppMessageEntity();

        //        AppMessageConverter.CopyDtoToEntity(aAppMessageEntity, aAppMessageDto);

        //        aAppMessageDto.FromEmail = AppSecurityUserBL.CurrentUserEntity.Email;

        //        aAppMessageEntity.Message = string.Empty;

        //        string createdDateString = aAppMessageEntity.AppCreatedDate.ToString();
        //        string createdByIdString = aAppMessageEntity.AppCreatedById.ToString();

        //        string postTypeString = ((int)EmAppMessgaePostType.UserNotification).ToString();







        //        if (aAppMessageDto.IsNew || aAppMessageDto.IsConversationMemberChanged)
        //        {
        //            List<string> sendToEmailList = RebuildMessageDtoEmailList(aAppMessageDto);

        //            foreach (string anEmail in sendToEmailList)
        //            {
        //                int? receiveUserId = null;
        //                var matchedUser = allUsers.FirstOrDefault(o => !string.IsNullOrEmpty(o.Email) && o.Email.Trim().ToLower() == anEmail);

        //                // for extern user email adress, there is no need to save  appMessageUserReceived Box
        //                if (matchedUser != null)
        //                {
        //                    receiveUserId = ControlTypeValueConverter.ConvertValueToInt(matchedUser.Id);

        //                    AppMessageUserReceivedExDto appMessageUserReceivedExDto = new AppMessageUserReceivedExDto() { ReceivedEmail = matchedUser.Email.Trim().ToLower(), ReceivedById = receiveUserId };
        //                    AppMessageUserReceivedEntity appMessageUserReceivedEntity = new AppMessageUserReceivedEntity();
        //                    AppMessageUserReceivedConverter.CopyDtoToEntity(appMessageUserReceivedEntity, appMessageUserReceivedExDto);
        //                    aAppMessageEntity.AppMessageUserReceived.Add(appMessageUserReceivedEntity);
        //                }
        //            }
        //        }


        //        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //        {
        //            try
        //            {
        //                adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

        //                if (aAppMessageDto.Id != null && aAppMessageDto.IsConversationMemberChanged)
        //                {
        //                    adapter.DeleteEntitiesDirectly(typeof(AppMessageUserReceivedEntity), new RelationPredicateBucket(AppMessageUserReceivedFields.MessageId == (int)aAppMessageDto.Id));
        //                }


        //                adapter.SaveEntity(aAppMessageEntity, false, true);

        //                adapter.Commit();
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

        //                aOperationCallResult.Object = aAppMessageEntity.MessageId;

        //                //if (emailMessageDto.ToList.Length > 0 && !string.IsNullOrWhiteSpace(emailMessageDto.Message))
        //                //{
        //                //    SaveOneAppMessageDto(emailMessageDto);
        //                //}

        //            }


        //            // Database FK Exception .......
        //            catch (ORMQueryExecutionException ex)
        //            {
        //                adapter.Rollback();
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
        //            }
        //        }


        //        return aOperationCallResult;
        //    }

        //    return null;




        //}

        public static OperationCallResult<AppMessageDto> UpdateMessageAttachedFiles(AppMessageDto aAppMessageDto)
        {
            if (aAppMessageDto != null)
            {
                OperationCallResult<AppMessageDto> aOperationCallResult = new OperationCallResult<AppMessageDto>();
                ValidationResult aValidationResult = new ValidationResult();
                aOperationCallResult.ValidationResult = aValidationResult;

                UpdateAppMessageAttachmentToken(aAppMessageDto);

                if (!aAppMessageDto.IsNew && aAppMessageDto.Id != null)
                {
                    AppMessageEntity aAppMessageEntity = RetrieveOneAppMessageEntity(aAppMessageDto.Id);
                    aAppMessageEntity.AppModifiedById = (int)ServerContext.Instance.CurrentUid;
                    aAppMessageEntity.AppModifiedDate = System.DateTime.UtcNow;

                    aAppMessageEntity.AttachmentFileToken = aAppMessageDto.AttachmentFileToken;


                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {
                            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                            adapter.SaveEntity(aAppMessageEntity, false, true);
                            adapter.Commit();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                            aOperationCallResult.Object = aAppMessageDto;
                        }

                        // Database FK Exception .......
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }
                    }
                }
                else
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_Update_OK", ValidationItemType.Message, "Update Successfully"));
                    aOperationCallResult.Object = aAppMessageDto;
                }

                return aOperationCallResult;
            }

            return null;
        }

        private static void UpdateAppMessageAttachmentToken(AppMessageDto aAppMessageDto)
        {
            aAppMessageDto.AttachmentFileToken = string.Empty;

            if (aAppMessageDto.DictAttachmentFileIdAndDisplay.Count > 0)
            {
                List<int> fileIdList = aAppMessageDto.DictAttachmentFileIdAndDisplay.Keys.ToList();

                Dictionary<int, string> dictInitialFileIdAndCode = new Dictionary<int, string>();
                aAppMessageDto.DictAttachmentFileIdAndDisplay = dictInitialFileIdAndCode;

                if (fileIdList.Count > 0)
                {
                    EntityCollection<AppFileEntity> fileEntityList = AppFileBL.RetrieveMultipleFileEntityByIds(fileIdList, true);
                    foreach (AppFileEntity fileEntity in fileEntityList)
                    {
                        if (fileEntity.InitialFileId.HasValue)
                        {
                            if (!dictInitialFileIdAndCode.ContainsKey(fileEntity.InitialFileId.Value))
                            {
                                dictInitialFileIdAndCode.Add(fileEntity.InitialFileId.Value, fileEntity.FileCode);
                            }
                        }
                        else
                        {
                            if (!dictInitialFileIdAndCode.ContainsKey(fileEntity.FileId))
                            {
                                dictInitialFileIdAndCode.Add(fileEntity.FileId, fileEntity.FileCode);
                            }
                        }
                    }
                }

                foreach (int fileId in dictInitialFileIdAndCode.Keys)
                {
                    aAppMessageDto.AttachmentFileToken += fileId.ToString() + "|";
                }

                if (aAppMessageDto.AttachmentFileToken.EndsWith("|"))
                {
                    aAppMessageDto.AttachmentFileToken = aAppMessageDto.AttachmentFileToken.Substring(0, aAppMessageDto.AttachmentFileToken.Length - 1);
                }
            }
        }

        //public static int? GetOneTaskDefaultConversactionId(int? taskId)
        //{
        //    if (taskId.HasValue)
        //    {
        //        var taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(taskId.Value);

        //        if (taskDto != null)
        //        {
        //            IRelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.ProjectActivityId == taskId.Value);
        //            var messageDtoList = GetMessageDtoList(filter).ToList();

        //            if (messageDtoList.Count > 0)
        //            {
        //                return (int)messageDtoList.First().Id;
        //            }
        //            else
        //            {
        //                AppMessageDto aAppMessageDto = new AppMessageDto();

        //                aAppMessageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Task;
        //                aAppMessageDto.MessagePostType = (int)EmAppMessgaePostType.Conversaction;

        //                aAppMessageDto.ProjectActivityId = taskId.Value;
        //                aAppMessageDto.Subject = "Message From Task: taskDto (#" + taskId.Value.ToString() + ")";

        //                aAppMessageDto.FromEmail = AppSecurityUserBL.CurrentUserEntity.Email;
        //                aAppMessageDto.ToList = AppSecurityUserBL.CurrentUserEntity.UserName + " [" + AppSecurityUserBL.CurrentUserEntity.Email + "];";
        //                aAppMessageDto.NewConversationMessage = new AppMessageDto();

        //                OperationCallResult<object> saveResult = SaveOneConversationExDto(aAppMessageDto);

        //                if (saveResult.IsSuccessfulWithResult)
        //                {
        //                    return ControlTypeValueConverter.ConvertValueToInt(saveResult.Object);
        //                }

        //            }
        //        }
        //    }

        //    return null;
        //}


        //public static int? GetTransactionDefaultConversactionId(int? transactionId, string transactionRId)
        //{
        //    if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
        //    {
        //        IRelationPredicateBucket filter = new RelationPredicateBucket(
        //            AppMessageFields.TransactionId == transactionId.Value
        //            & AppMessageFields.TransactionRootValueId == transactionRId
        //            & AppMessageFields.MessagePostType == (int)EmAppMessgaePostType.Conversaction);
        //        var messageDtoList = GetMessageDtoList(filter).ToList();

        //        if (messageDtoList.Count > 0)
        //        {
        //            return (int)messageDtoList.First().Id;
        //        }
        //        else
        //        {
        //            AppMessageDto aAppMessageDto = new AppMessageDto();

        //            aAppMessageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Transaction;
        //            aAppMessageDto.MessagePostType = (int)EmAppMessgaePostType.Conversaction;

        //            aAppMessageDto.TransactionId = transactionId.Value;
        //            aAppMessageDto.TransactionRootValueId = transactionRId;
        //            aAppMessageDto.Subject = "Transaction Conversation " + transactionId.Value.ToString() + " " + transactionRId; ;

        //            aAppMessageDto.FromEmail = AppSecurityUserBL.CurrentUserEntity.Email;
        //            aAppMessageDto.ToList = AppSecurityUserBL.CurrentUserEntity.UserName + " [" + AppSecurityUserBL.CurrentUserEntity.Email + "];";
        //            aAppMessageDto.NewConversationMessage = new AppMessageDto();

        //            OperationCallResult<object> saveResult = SaveOneConversationExDto(aAppMessageDto);

        //            if (saveResult.IsSuccessfulWithResult)
        //            {
        //                return ControlTypeValueConverter.ConvertValueToInt(saveResult.Object);
        //            }

        //        }
        //    }

        //    return null;
        //}


        //public static void AppendNewConversationMessage(int? conversationMessageId, string newMessageText, bool isSystemDefined)
        //{
        //    if (conversationMessageId.HasValue && !string.IsNullOrWhiteSpace(newMessageText))
        //    {
        //        var conversationDto = AppMessageBL.RetrieveOneAppMessageExDto(conversationMessageId.Value);
        //        conversationDto.NewConversationMessage = new AppMessageDto();
        //        conversationDto.NewConversationMessage.MessgaeScopeType = conversationDto.MessgaeScopeType;
        //        conversationDto.NewConversationMessage.Message = newMessageText;

        //        conversationDto.NewConversationMessage.MessagePostType = conversationDto.MessagePostType;
        //        if (isSystemDefined)
        //        {
        //            conversationDto.NewConversationMessage.MessagePostType = (int)EmAppMessgaePostType.SystemNotification;
        //        }

        //        AppMessageBL.SaveOneConversationExDto(conversationDto);
        //    }
        //}


        private static List<string> RebuildMessageDtoEmailList(AppMessageDto aAppMessageDto)
        {


            List<string> sendToEmailList = new List<string>();

            if (aAppMessageDto.ToList == null)
            {
                aAppMessageDto.ToList = string.Empty;
            }

            if (aAppMessageDto.Cclist == null)
            {
                aAppMessageDto.Cclist = string.Empty;
            }

            if (aAppMessageDto.Bcclist == null)
            {
                aAppMessageDto.Bcclist = string.Empty;
            }

            string[] toEmails = (aAppMessageDto.ToList).Split(";".ToArray());
            string[] ccEmails = (aAppMessageDto.Cclist).Split(";".ToArray());
            string[] bccEmails = (aAppMessageDto.Bcclist).Split(";".ToArray());

            aAppMessageDto.ToList = string.Empty;
            foreach (var anEmail in toEmails)
            {
                if (!string.IsNullOrEmpty(anEmail.Trim()))
                {
                    string emailToAdd = anEmail.Trim().ToLower();

                    int paramStart = emailToAdd.IndexOf("[");
                    int paramEnd = emailToAdd.IndexOf("]");

                    if (paramStart >= 0 && paramEnd > paramStart)
                    {
                        emailToAdd = emailToAdd.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    }

                    aAppMessageDto.ToList += emailToAdd + ";";

                    if (!sendToEmailList.Contains(emailToAdd))
                    {
                        sendToEmailList.Add(emailToAdd);
                    }
                }
            }

            aAppMessageDto.Cclist = string.Empty;
            foreach (var anEmail in ccEmails)
            {
                if (!string.IsNullOrEmpty(anEmail.Trim()))
                {
                    string emailToAdd = anEmail.Trim().ToLower();

                    int paramStart = emailToAdd.IndexOf("[");
                    int paramEnd = emailToAdd.IndexOf("]");

                    if (paramStart >= 0 && paramEnd > paramStart)
                    {
                        emailToAdd = emailToAdd.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    }

                    aAppMessageDto.Cclist += emailToAdd + ";";

                    if (!sendToEmailList.Contains(emailToAdd))
                    {
                        sendToEmailList.Add(emailToAdd);
                    }
                }
            }

            aAppMessageDto.Bcclist = string.Empty;
            foreach (var anEmail in bccEmails)
            {
                if (!string.IsNullOrEmpty(anEmail.Trim()))
                {
                    string emailToAdd = anEmail.Trim().ToLower();

                    int paramStart = emailToAdd.IndexOf("[");
                    int paramEnd = emailToAdd.IndexOf("]");

                    if (paramStart >= 0 && paramEnd > paramStart)
                    {
                        emailToAdd = emailToAdd.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    }

                    aAppMessageDto.Bcclist += emailToAdd + ";";

                    if (!sendToEmailList.Contains(emailToAdd))
                    {
                        sendToEmailList.Add(emailToAdd);
                    }
                }
            }

            return sendToEmailList;
        }

        private static AppMessageDto BuildWorkflowTaskUserMessageWithTaskDetail(AppMessageDto orgMessageDto)
        {
            if (orgMessageDto.ProjectActivityId.HasValue)
            {
                var taskDto = AppProjectWorkFlowStructureBL.RetrieveOneAppProjectWorkFlowTask(orgMessageDto.ProjectActivityId.Value);

                if (taskDto.Notes != null)
                {
                    taskDto.Notes = taskDto.Notes.Replace("\r\n", "<br />");
                }
                else
                {
                    taskDto.Notes = string.Empty;
                }

                // List<AppMessageDto> messageList = RetrieveProjectActivityMessageList(orgMessageDto.ProjectActivityId.Value).ToList();

                string currentUserName = AppSecurityUserBL.CurrentUserEntity.UserName;
                var dictAllUserDto = AppSecurityUserBL.DictAllUserDto;
                //myString.Replace(System.Environment.NewLine, "replacement text")

                AppMessageDto detailMessageDto = new AppMessageDto();
                detailMessageDto.ToList = orgMessageDto.ToList;
                detailMessageDto.Subject = currentUserName + " Posted A New Task Message";
                detailMessageDto.Message = string.Empty;

                //  {0}:UserName,  {1}:Time,  {2}:Subject {3}: Message
                detailMessageDto.Message += string.Format(EmailTemplate_WorkFlowTaskNewCommentAdded,
                    currentUserName,
                    orgMessageDto.AppCreatedDate.ToString(),
                    orgMessageDto.Subject,
                    orgMessageDto.Message);



                //  {0}:Task Name,  {1}:Task Description, {2}:Task Notes,  {3}:Resources String, {4}:Workflow Name,
                detailMessageDto.Message += string.Format(EmailTemplate_WorkFlowTaskDetail,
                    taskDto.Name,
                    taskDto.Description,
                    taskDto.Notes,
                    detailMessageDto.ToList,
                    taskDto.ProjectName);

                return detailMessageDto;
            }

            else
            {
                return orgMessageDto;
            }
        }

        private static void AddTaskResourceUserEmailsToMessageToList(AppMessageDto aAppMessageDto)
        {
            if (aAppMessageDto.ToList == null)
            {
                aAppMessageDto.ToList = string.Empty;
            }

            if (aAppMessageDto.ToList.Length > 0 && !aAppMessageDto.ToList.EndsWith(";"))
            {
                aAppMessageDto.ToList = aAppMessageDto.ToList + ";";
            }

            if (aAppMessageDto.ProjectActivityId.HasValue)
            {
                List<AppProjectTaskResourceDto> taskResourceList = AppProjectWorkFlowStructureBL.RetrieveWorkFlowTaskResources(aAppMessageDto.ProjectActivityId.Value);
                var dictUserDto = AppSecurityUserBL.DictAllUserDto;

                foreach (var resource in taskResourceList)
                {
                    if (resource.UserId.HasValue && dictUserDto.ContainsKey(resource.UserId.Value))
                    {
                        var userDto = dictUserDto[resource.UserId.Value];

                        if (!string.IsNullOrEmpty(userDto.Email.Trim()))
                        {
                            aAppMessageDto.ToList += userDto.Email.Trim() + ";";
                        }
                    }
                }
            }
        }


        private static ValidationResult DeleteBatchMessageDto(int[] AppMessageId)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppMessageEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppMessageEntity), new RelationPredicateBucket(AppMessageFields.MessageId == AppMessageId));
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    adapter.Commit();
                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppMessageEntity), "App_MessageEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        public static bool SendEmailFromAppMessageDto(AppMessageDto aAppMessageDto)
        {
            if (aAppMessageDto != null && !string.IsNullOrEmpty(aAppMessageDto.Subject) && !string.IsNullOrEmpty(aAppMessageDto.ToList))
            {
                System.Net.Mail.Attachment reservationAttachment = null;

                //if (aAppMessageDto.IsNeedToSendFullDetail)
                //{
                //    if (aAppMessageDto.ProjectActivityId.HasValue)
                //    {
                //        aAppMessageDto = BuildWorkflowTaskUserMessageWithTaskDetail(aAppMessageDto);
                //    }
                //}

                string messageLinks = BuildMessageLinkString(aAppMessageDto);

                if (!string.IsNullOrWhiteSpace(messageLinks))
                {
                    aAppMessageDto.Message += "<br >" + messageLinks;
                }

                if (aAppMessageDto.TransactionId.HasValue && !string.IsNullOrWhiteSpace(aAppMessageDto.TransactionRootValueId))
                {
                    aAppMessageDto.Message += "<br />" + BuildFormHtmlPrintLayout(aAppMessageDto.TransactionId, aAppMessageDto.TransactionRootValueId);

                    //List<int> printFileIdList = AppMasterDetailFormPrintBL.GetTransactionFormFileIDList(aAppMessageDto.TransactionId.Value, aAppMessageDto.TransactionRootValueId);
                    //if (printFileIdList != null)
                    //{
                    //    foreach (int fileId in printFileIdList)
                    //    {
                    //        if (!string.IsNullOrWhiteSpace(aAppMessageDto.AttachmentFileToken))
                    //        {
                    //            aAppMessageDto.AttachmentFileToken += "|" + fileId.ToString();
                    //        }
                    //        else
                    //        {
                    //            aAppMessageDto.AttachmentFileToken = fileId.ToString();
                    //        }
                    //    }
                    //}

                    reservationAttachment = EmailHelper.GenerateICSAttahment(aAppMessageDto.TransactionId.Value, aAppMessageDto.TransactionRootValueId);

                }

                return EmailHelper.SmtpEamilSend(aAppMessageDto.ToList, aAppMessageDto.Subject, aAppMessageDto.Message, aAppMessageDto.AttachmentFieldIds, reservationAttachment);
            }

            return false;
        }

        public static void SendEmailFromAppMessageDtoList(List<AppMessageDto> messageList)
        {
            if (messageList != null)
            {
                foreach (AppMessageDto message in messageList)
                {
                    //SendEmailFromAppMessageDto(message);
                    SaveOneAppMessageDto(message);
                }
            }
        }

        public static void ConvertMessageToListUserIdsToEmails(List<AppMessageDto> messageList)
        {
            //Dictionary<int, string> dictUserIdEmail = AppSecurityUserBL.DictAllUserDto.Values.Where(o => !string.IsNullOrEmpty(o.Email)).ToDictionary(p => (int)p.Id, p => p.Email);

            foreach (AppMessageDto message in messageList)
            {
                string[] userIdStringList = message.ToList.Split('|');
                message.ToList = string.Empty;

                foreach (string userIdString in userIdStringList)
                {
                    int? userId = ControlTypeValueConverter.ConvertValueToInt(userIdString);

                    //if (userId.HasValue && dictUserIdEmail.ContainsKey(userId.Value))
                    //{
                    //    message.ToList += dictUserIdEmail[userId.Value] + ";";
                    //}

                    if (userId.HasValue)
                    {
                        AppSecurityUserEntity userEntity = AppCacheManagerBL.GetOneAppSecurityUserEntityFromCache(userId.Value);

                        message.ToList += userEntity.Email + ";";
                    }

                }
            }


        }


        public static ScopeMessageGroupDto RetrieveScopeMessageGroupDto(EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId)
        {
            ScopeMessageGroupDto scopeMessageGroupDto = new ScopeMessageGroupDto();

            scopeMessageGroupDto.MessageDtoList = RetrieveAppMessageDtoListByScope(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId);


            if (transactionId.HasValue)
            {
                var transactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId.Value);

                if (transactionDto.OtherOptions != null)
                {
                    if (transactionDto.OtherOptions.CommunicationGroupByType.HasValue)
                    {
                        int groupBy = transactionDto.OtherOptions.CommunicationGroupByType.Value;

                        int currentUserDomainId = AppSecurityUserBL.CurrentUserEntity.DomainId;

                        if (currentUserDomainId == (int)EmAppUserType.Customer
                                    && (groupBy == (int)EmAppConversationGroupByType.GroupByCustomer || groupBy == (int)EmAppConversationGroupByType.GroupByCustomerUser))
                        {

                        }
                        else if (currentUserDomainId == (int)EmAppUserType.Supplier
                            && (groupBy == (int)EmAppConversationGroupByType.GroupBySupplier || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierUser))
                        {

                        }
                        else if (currentUserDomainId == (int)EmAppUserType.ClientAgent
                            && (groupBy == (int)EmAppConversationGroupByType.GroupByClientAgent || groupBy == (int)EmAppConversationGroupByType.GroupByClientAgentUser))
                        {

                        }
                        else if (currentUserDomainId == (int)EmAppUserType.SupplierAgent
                           && (groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgent || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgentUser))
                        {

                        }
                        else
                        {
                            scopeMessageGroupDto.GroupByType = groupBy;
                        }
                    }

                    if (transactionDto.OtherOptions.CommunicationToUserIdTransField.HasValue)
                    {
                        scopeMessageGroupDto.IsFollowUpUserFromTransaction = true;
                    }
                }
            }


            scopeMessageGroupDto.FollowupDtoList = RetrieveAppUserMessgeFollowupDtoListByScope(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId, null);
            scopeMessageGroupDto.FollowupUserIdList = scopeMessageGroupDto.FollowupDtoList.Where(o => o.UserId.HasValue).Select(o => o.UserId.Value).Distinct().ToList();
            scopeMessageGroupDto.DictAttachmentFileIdAndDisplay = new Dictionary<int, string>();


            List<int> allFileIds = new List<int>();

            foreach (var aMessageDto in scopeMessageGroupDto.MessageDtoList)
            {
                foreach (int fileId in aMessageDto.AttachmentFieldIds)
                {
                    if (!allFileIds.Contains(fileId))
                    {
                        allFileIds.Add(fileId);
                    }
                }


            }

            if (allFileIds.Count > 0)
            {
                var fileDtoList = AppFileBL.RetrieveMultipleFileSimpleDtoByIds(allFileIds);
                scopeMessageGroupDto.DictAttachmentFileIdAndDisplay = fileDtoList.ToDictionary(o => (int)o.Id, o => o.FileCode);
            }


            scopeMessageGroupDto.CurrentUserId = AppSecurityUserBL.CurrentUserId;

            return scopeMessageGroupDto;
        }

        public static List<AppMessageDto> RetrieveAppMessageDtoListByScope(EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId)
        {
            List<AppMessageDto> messageDtoList = new List<AppMessageDto>();

            if (scopeType.HasValue)
            {
                if (scopeType.Value == EmAppMessgaeScopeType.Transaction)
                {
                    if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
                    {
                        messageDtoList = RetrieveOneTransactionMessageList(transactionId.Value, transactionRId).ToList();

                        AppProjectOrWorkFlowExDto formDefaultWorkflow = AppProjectWorkFlowProcessBL.GetTransactionRunningProjectWorkflow(transactionId.Value, transactionRId);

                        if (formDefaultWorkflow != null)
                        {
                            messageDtoList.AddRange(RetrieveProjectOrWorkflowMessageList((int)formDefaultWorkflow.Id).ToList());
                        }


                        var transactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId.Value);
                        if (transactionDto.OtherOptions != null && transactionDto.OtherOptions.CommunicationGroupByType.HasValue)
                        {
                            int groupBy = transactionDto.OtherOptions.CommunicationGroupByType.Value;


                            messageDtoList = BuildGroupedMessageList(messageDtoList, groupBy);




                            return messageDtoList;
                        }
                        else
                        {
                            List<AppMessageDto> toReturn = SortMessageListById(messageDtoList);
                            return toReturn;
                        }
                    }
                }
                else
                {


                    if (scopeType.Value == EmAppMessgaeScopeType.Global)
                    {
                        messageDtoList = RetrieveGlobalMessageList().ToList();
                    }
                    else if (scopeType.Value == EmAppMessgaeScopeType.Project)
                    {
                        if (projectOrWorkflowId.HasValue)
                        {
                            messageDtoList = RetrieveProjectOrWorkflowMessageList(projectOrWorkflowId.Value).ToList();
                        }
                    }
                    else if (scopeType.Value == EmAppMessgaeScopeType.Workflow)
                    {
                        if (projectOrWorkflowId.HasValue)
                        {
                            messageDtoList = RetrieveProjectOrWorkflowMessageList(projectOrWorkflowId.Value).ToList();
                        }
                    }
                    else if (scopeType.Value == EmAppMessgaeScopeType.Task)
                    {
                        if (taskId.HasValue)
                        {
                            messageDtoList = RetrieveProjectActivityMessageList(taskId.Value).ToList();
                        }
                    }

                    List<AppMessageDto> toReturn = SortMessageListById(messageDtoList);
                    return toReturn;
                }
            }


            return messageDtoList;


        }


        public static List<AppUserMessgeFollowupDto> RetrieveAppUserMessgeFollowupDtoListByScope(EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId, int? projectTeamId)
        {
            List<AppUserMessgeFollowupDto> followupDtoList = new List<AppUserMessgeFollowupDto>();

            if (scopeType.HasValue)
            {
                if (scopeType.Value == EmAppMessgaeScopeType.Global)
                {
                    IRelationPredicateBucket filter = new RelationPredicateBucket(
                            AppUserMessgeFollowupFields.TransactionId == System.DBNull.Value
                            & AppUserMessgeFollowupFields.TransactionRootValueId == System.DBNull.Value
                            & AppUserMessgeFollowupFields.ProjectId == System.DBNull.Value
                            & AppUserMessgeFollowupFields.ProjectActivityId == System.DBNull.Value
                            & AppUserMessgeFollowupFields.ProjectTeamId == System.DBNull.Value);

                    followupDtoList = GetAppUserMessgeFollowupDtoList(filter);
                }
                else if (scopeType.Value == EmAppMessgaeScopeType.Project)
                {
                    if (projectOrWorkflowId.HasValue)
                    {

                        IRelationPredicateBucket filter = new RelationPredicateBucket(AppUserMessgeFollowupFields.ProjectId == projectOrWorkflowId.Value);
                        followupDtoList = GetAppUserMessgeFollowupDtoList(filter);

                        // To do: also retrieve all project tasks system notification messages
                    }
                }
                else if (scopeType.Value == EmAppMessgaeScopeType.Workflow)
                {
                    if (projectOrWorkflowId.HasValue)
                    {
                        IRelationPredicateBucket filter = new RelationPredicateBucket(AppUserMessgeFollowupFields.ProjectId == projectOrWorkflowId.Value);
                        followupDtoList = GetAppUserMessgeFollowupDtoList(filter);

                        // To do: also retrieve all workflow tasks system notification messages
                    }
                }
                else if (scopeType.Value == EmAppMessgaeScopeType.Task)
                {
                    if (taskId.HasValue)
                    {
                        IRelationPredicateBucket filter = new RelationPredicateBucket(AppUserMessgeFollowupFields.ProjectActivityId == taskId.Value);
                        followupDtoList = GetAppUserMessgeFollowupDtoList(filter);
                    }
                }
                else if (scopeType.Value == EmAppMessgaeScopeType.Transaction)
                {
                    if (transactionId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
                    {
                        var transactionDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId.Value);

                        if (transactionDto.OtherOptions != null && transactionDto.OtherOptions.CommunicationToUserIdTransField.HasValue)
                        {

                            followupDtoList = GetFolowUpDtoListFromTransactionFormField(transactionId, transactionRId, transactionDto);
                        }
                        else
                        {
                            IRelationPredicateBucket filter = new RelationPredicateBucket(
                                AppUserMessgeFollowupFields.TransactionId == transactionId.Value
                                & AppUserMessgeFollowupFields.TransactionRootValueId == transactionRId);

                            followupDtoList = GetAppUserMessgeFollowupDtoList(filter);
                        }



                    }
                }
            }


            return followupDtoList;
        }


        public static OperationCallResult<AppUserMessgeFollowupDto> UpdateUserMessgeFollowupByScope(List<int> followupUserIdList, EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId, int? projectTeamId)
        {
            OperationCallResult<AppUserMessgeFollowupDto> aOperationCallResult = new OperationCallResult<AppUserMessgeFollowupDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (followupUserIdList != null)
            {
                List<AppUserMessgeFollowupDto> orgDtoList = RetrieveAppUserMessgeFollowupDtoListByScope(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId, projectTeamId);
                List<int> orgUserIdList = orgDtoList.Where(o => o.UserId.HasValue).Select(o => o.UserId.Value).Distinct().ToList();

                List<int> needToDeleteIdList = orgDtoList.Where(o => o.UserId.HasValue && !followupUserIdList.Contains(o.UserId.Value)).Select(o => (int)o.Id).ToList();
                List<int> needToAddUserIdList = followupUserIdList.Where(o => !orgUserIdList.Contains(o)).ToList();


                EntityCollection<AppUserMessgeFollowupEntity> newEntities = new EntityCollection<AppUserMessgeFollowupEntity>();

                //new Entity
                foreach (var userId in needToAddUserIdList)
                {
                    AppUserMessgeFollowupEntity aEntity = new AppUserMessgeFollowupEntity();
                    aEntity.UserId = userId;
                    aEntity.TransactionId = transactionId;
                    aEntity.TransactionRootValueId = transactionRId;
                    aEntity.ProjectActivityId = taskId;
                    aEntity.ProjectId = projectOrWorkflowId;
                    aEntity.ProjectTeamId = projectTeamId;

                    newEntities.Add(aEntity);
                }


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {

                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntityCollection(newEntities);

                        adapter.DeleteEntitiesDirectly(typeof(AppUserMessgeFollowupEntity), new RelationPredicateBucket(AppUserMessgeFollowupFields.MessageFollowupId == needToDeleteIdList));
                        //}

                        adapter.Commit();
                        validationResult.Items.Add(new ValidationItem(typeof(AppUserMessgeFollowupEntity), "App_UserMessgeFollowup_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                    }


                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        validationResult.Items.Add(new ValidationItem(typeof(AppUserMessgeFollowupEntity), "App_UserMessgeFollowup_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }

                if (!validationResult.HasErrors)
                {
                    aOperationCallResult.ObjectList = RetrieveAppUserMessgeFollowupDtoListByScope(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId, projectTeamId);
                }
            }

            return aOperationCallResult;
        }


        public static OperationCallResult<object> SendNewAppMessageByScope(EmAppMessgaeScopeType? scopeType, int? transactionId, string transactionRId, int? taskId, int? projectOrWorkflowId, int? projectTeamId,
                                                                                               EmAppMessgaePostType? postType, string subject, string messageText, List<int> mandatoryUserIdList, bool isAttachAllFormFiles = false, string toList = "")
        {
            if (scopeType.HasValue && postType.HasValue)
            {
                AppMessageDto messageDto = new AppMessageDto();

                messageDto.MessgaeScopeType = (int)scopeType.Value;
                messageDto.MessagePostType = (int)postType.Value;

                messageDto.TransactionId = transactionId;
                messageDto.TransactionRootValueId = transactionRId;
                messageDto.ProjectActivityId = taskId;
                messageDto.ProjectId = projectOrWorkflowId;
                messageDto.ToList = toList;

                messageDto.Subject = subject;
                messageDto.Message = messageText;
                messageDto.IsAttachAllFormFiles = isAttachAllFormFiles;

                List<int> userIdList = new List<int>();

                if (postType.HasValue && postType.Value == EmAppMessgaePostType.Conversaction)
                {
                    userIdList = AppMessageBL.RetrieveAppUserMessgeFollowupDtoListByScope(scopeType, transactionId, transactionRId, taskId, projectOrWorkflowId, projectTeamId).Select(o => o.UserId.Value).Distinct().ToList();
                }

                if (mandatoryUserIdList != null && mandatoryUserIdList.Count > 0)
                {
                    userIdList.AddRange(mandatoryUserIdList);
                }

                userIdList = userIdList.Distinct().ToList();

                var dictUserDto = AppSecurityUserBL.DictAllUserDto;

                foreach (int userId in userIdList.Distinct())
                {
                    if (dictUserDto.ContainsKey(userId))
                    {
                        var userDto = dictUserDto[userId];

                        if (!string.IsNullOrEmpty(userDto.Email.Trim()))
                        {
                            messageDto.ToList += userDto.Email.Trim() + ";";
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(messageDto.ToList))
                {
                    return AppMessageBL.SaveOneAppMessageDto(messageDto);
                }
            }

            return null;
        }

        private static List<AppUserMessgeFollowupDto> GetFolowUpDtoListFromTransactionFormField(int? transactionId, string transactionRId, AppTransactionExDto transactionDto)
        {
            var aDtoList = new List<AppUserMessgeFollowupDto>();
            AppMasterDetailDto fromData = AppMasterDetailFormDataLoadBL.GetMasterDetailFormData(transactionId.Value, transactionRId);

            if (fromData != null)
            {
                Dictionary<int, object> allFreshRootValue = AppTransactionFormulaBL.MergeSiblingUnitValueToRootUnit(fromData, transactionDto);


                List<int> toUserIds = new List<int>();
                int userIdFieldId = transactionDto.OtherOptions.CommunicationToUserIdTransField.Value;

                if (allFreshRootValue.ContainsKey(userIdFieldId))
                {
                    int? toUserId = ControlTypeValueConverter.ConvertValueToInt(allFreshRootValue[userIdFieldId]);

                    if (toUserId.HasValue)
                    {
                        toUserIds.Add(toUserId.Value);
                    }
                }
                else
                {
                    if (transactionDto.DictAllTransactionField.ContainsKey(userIdFieldId))
                    {
                        var fieldDto = transactionDto.DictAllTransactionField[userIdFieldId];
                        int unitId = fieldDto.TransactionUnitId;

                        if (fromData.DictOneToManyFields.ContainsKey(unitId.ToString()))
                        {
                            foreach (var rowDto in fromData.DictOneToManyFields[unitId.ToString()])
                            {
                                if (rowDto.DictOneToOneFields.ContainsKey(fieldDto.DataBaseFieldName))
                                {
                                    int? toUserId = ControlTypeValueConverter.ConvertValueToInt(rowDto.DictOneToOneFields[fieldDto.DataBaseFieldName]);

                                    if (toUserId.HasValue)
                                    {
                                        toUserIds.Add(toUserId.Value);
                                    }
                                }
                            }
                        }
                    }
                }

                toUserIds = toUserIds.Distinct().ToList();

                var dictUserDto = AppSecurityUserBL.DictAllUserDto;


                foreach (int userId in toUserIds)
                {
                    AppUserMessgeFollowupDto followupDto = new AppUserMessgeFollowupDto();
                    followupDto.UserId = userId;
                    followupDto.TransactionId = transactionId;
                    followupDto.TransactionRootValueId = transactionRId;

                    if (followupDto.UserId.HasValue && dictUserDto.ContainsKey(followupDto.UserId.Value))
                    {
                        var userDto = dictUserDto[followupDto.UserId.Value];
                        followupDto.UserName = userDto.UserName;
                        followupDto.UserEmail = userDto.Email;
                    }

                    aDtoList.Add(followupDto);
                }
            }

            return aDtoList;
        }

        private static List<AppUserMessgeFollowupDto> GetAppUserMessgeFollowupDtoList(IRelationPredicateBucket filter)
        {
            var dictUserDto = AppSecurityUserBL.DictAllUserDto;

            SortClause aSortClause = AppUserMessgeFollowupFields.AppCreatedDate | SortOperator.Ascending;
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppUserMessgeFollowupEntity> entityList = new EntityCollection<AppUserMessgeFollowupEntity>();
                adapter.FetchEntityCollection(entityList, filter, 0, new SortExpression(aSortClause));

                var aDtoList = new List<AppUserMessgeFollowupDto>();
                foreach (var aFollowupEntity in entityList)
                {
                    AppUserMessgeFollowupDto followupDto = AppUserMessgeFollowupConverter.ConvertEntityToDto(aFollowupEntity);

                    if (followupDto.UserId.HasValue && dictUserDto.ContainsKey(followupDto.UserId.Value))
                    {
                        var userDto = dictUserDto[followupDto.UserId.Value];
                        followupDto.UserName = userDto.UserName;
                        followupDto.UserEmail = userDto.Email;
                    }

                    aDtoList.Add(followupDto);
                }

                return aDtoList;
            }
        }




        private static List<AppMessageDto> BuildMessageSubGroupsByUserId(Dictionary<int, List<AppMessageDto>> disctUserIdAndMessages, Dictionary<int, AppSecurityUserDto> dictUserDto)
        {
            List<AppMessageDto> toReturn = new List<AppMessageDto>();

            int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);
            Dictionary<int, int> dictUserIdAndPartnerId = new Dictionary<int, int>();
            if (companyId.HasValue)
            {
                dictUserIdAndPartnerId = AppCacheManagerBL.GetOneCompanyUserIdAndPartnerIdDictionary(companyId.Value);
            }

            Dictionary<int, AppBusinessPartnerDto> dictPartnerDto = AppBusinessPartnerBL.RetrieveCurrentCompanyPartnerDtoList().ToDictionary(o => (int)o.Id, o => o);


            foreach (int groupById in disctUserIdAndMessages.Keys)
            {
                var userDto = dictUserDto[groupById];

                AppMessageDto groupingMessageDto = new AppMessageDto();
                groupingMessageDto.SubGroupId = groupById.ToString();
                groupingMessageDto.SubGroupName = userDto.UserName;

                if (userDto.DomainId == (int)EmAppUserType.Supplier
                    || userDto.DomainId == (int)EmAppUserType.Customer
                    || userDto.DomainId == (int)EmAppUserType.ClientAgent
                    || userDto.DomainId == (int)EmAppUserType.SupplierAgent)
                {
                    if (dictUserIdAndPartnerId.ContainsKey((int)userDto.Id))
                    {
                        int partnerId = dictUserIdAndPartnerId[(int)userDto.Id];

                        if (dictPartnerDto.ContainsKey(partnerId))
                        {
                            AppBusinessPartnerDto partnerDto = dictPartnerDto[partnerId];

                            if (!string.IsNullOrWhiteSpace(partnerDto.DisplayName))
                            {
                                groupingMessageDto.SubGroupName += " - " + partnerDto.DisplayName;
                            }
                        }
                    }
                }




                List<AppMessageDto> subMessageList = new List<AppMessageDto>();
                List<int> subMessageIdList = new List<int>();
                int subSort = 1;

                foreach (AppMessageDto subMessageDto in disctUserIdAndMessages[groupById].OrderBy(o => o.Id))
                {
                    if (!subMessageIdList.Contains((int)subMessageDto.Id))
                    {
                        subMessageDto.Sort = subSort;
                        subSort++;
                        subMessageIdList.Add((int)subMessageDto.Id);
                        subMessageList.Add(subMessageDto);
                    }
                }

                groupingMessageDto.ConversationMessageList = subMessageList;
                toReturn.Add(groupingMessageDto);
            }

            return toReturn;
        }

        private static List<AppMessageDto> BuildMessageSubGroupsByPartnerId(Dictionary<int, List<AppMessageDto>> dictPartnerIdAndMessages)
        {
            List<AppMessageDto> toReturn = new List<AppMessageDto>();


            Dictionary<int, AppBusinessPartnerDto> dictPartnerDto = AppBusinessPartnerBL.RetrieveCurrentCompanyPartnerDtoList().ToDictionary(o => (int)o.Id, o => o);

            foreach (int groupById in dictPartnerIdAndMessages.Keys)
            {
                var partnerDto = dictPartnerDto[groupById];

                AppMessageDto groupingMessageDto = new AppMessageDto();
                groupingMessageDto.SubGroupId = groupById.ToString();
                groupingMessageDto.SubGroupName = partnerDto.FullName;

                List<AppMessageDto> subMessageList = new List<AppMessageDto>();
                List<int> subMessageIdList = new List<int>();
                int subSort = 1;

                foreach (AppMessageDto subMessageDto in dictPartnerIdAndMessages[groupById].OrderBy(o => o.Id))
                {
                    if (!subMessageIdList.Contains((int)subMessageDto.Id))
                    {
                        subMessageDto.Sort = subSort;
                        subSort++;
                        subMessageIdList.Add((int)subMessageDto.Id);
                        subMessageList.Add(subMessageDto);
                    }
                }

                groupingMessageDto.ConversationMessageList = subMessageList;
                toReturn.Add(groupingMessageDto);
            }


            return toReturn;
        }

        private static void AddMessageToSubGroupByPartnerId(Dictionary<int, AppSecurityUserDto> dictUserDto, int? domainId, Dictionary<int, List<AppMessageDto>> dictPartnerIdAndMessages, AppMessageDto messageDto, int userId)
        {
            if (dictUserDto.ContainsKey(userId))
            {
                var userDto = dictUserDto[userId];

                if (domainId.HasValue && userDto.DomainId == domainId.Value)
                {
                    int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

                    if (companyId.HasValue)
                    {
                        Dictionary<int, int> dictUserIdAndPartnerId = AppCacheManagerBL.GetOneCompanyUserIdAndPartnerIdDictionary(companyId.Value);

                        if (dictUserIdAndPartnerId.ContainsKey(userId))
                        {

                            int partnerId = dictUserIdAndPartnerId[userId];

                            if (!dictPartnerIdAndMessages.ContainsKey(partnerId))
                            {
                                dictPartnerIdAndMessages.Add(partnerId, new List<AppMessageDto>());
                            }

                            if (dictPartnerIdAndMessages[partnerId].FirstOrDefault(o => (int)o.Id == (int)messageDto.Id) == null)
                            {
                                dictPartnerIdAndMessages[partnerId].Add(messageDto);
                            }
                        }


                    }





                }
            }
        }

        private static void AddMessageToSubGroupByUserId(Dictionary<int, AppSecurityUserDto> dictUserDto, int? domainId, Dictionary<int, List<AppMessageDto>> disctUserIdAndMessages, AppMessageDto messageDto, int userId)
        {
            if (dictUserDto.ContainsKey(userId))
            {

                var userDto = dictUserDto[userId];

                if (domainId.HasValue)
                {
                    if (userDto.DomainId != domainId.Value)
                    {
                        return;
                    }
                }

                if (!disctUserIdAndMessages.ContainsKey((int)userDto.Id))
                {
                    disctUserIdAndMessages.Add((int)userDto.Id, new List<AppMessageDto>());
                }

                if (disctUserIdAndMessages[(int)userDto.Id].FirstOrDefault(o => (int)o.Id == (int)messageDto.Id) == null)
                {
                    disctUserIdAndMessages[(int)userDto.Id].Add(messageDto);
                }

            }
        }

        private static void PrepareMessageToUserIdList(Dictionary<int, AppSecurityUserDto> dictUserDto, AppMessageDto messageDto)
        {
            messageDto.ToUserIdList = new List<int>();

            string[] toEmails = (messageDto.ToList).Split(";".ToArray());
            List<string> toEmailList = new List<string>();

            foreach (var anEmail in toEmails)
            {
                if (!string.IsNullOrEmpty(anEmail.Trim()))
                {
                    string emailaddress = anEmail.Trim().ToLower();

                    int paramStart = emailaddress.IndexOf("[");
                    int paramEnd = emailaddress.IndexOf("]");

                    if (paramStart >= 0 && paramEnd > paramStart)
                    {
                        emailaddress = emailaddress.Substring(paramStart + 1, paramEnd - paramStart - 1).Trim();
                    }

                    if (!toEmailList.Contains(emailaddress))
                    {
                        toEmailList.Add(emailaddress);
                    }
                }
            }

            foreach (string toEmail in toEmailList)
            {
                var foundUser = dictUserDto.Values.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.Email) && o.Email.ToLower().Trim() == toEmail.ToLower().Trim());
                if (foundUser != null)
                {
                    if (!messageDto.ToUserIdList.Contains((int)foundUser.Id))
                    {
                        messageDto.ToUserIdList.Add((int)foundUser.Id);
                    }
                }
            }
        }

        private static List<AppMessageDto> SortMessageListById(List<AppMessageDto> messageDtoList)
        {
            List<AppMessageDto> toReturn = new List<AppMessageDto>();
            List<int> messageIdList = new List<int>();
            int sort = 1;

            foreach (AppMessageDto messageDto in messageDtoList.OrderBy(o => o.Id))
            {
                if (!messageIdList.Contains((int)messageDto.Id))
                {
                    messageDto.Sort = sort;
                    sort++;
                    messageIdList.Add((int)messageDto.Id);
                    toReturn.Add(messageDto);

                }
            }

            return toReturn;
        }

        private static string GetTransactionFormPrintLayoutFromMessageTemplate_ProcessChildUnit(int? messageTemplateId, object rootKeyValue, AppMasterDetailDto appformDataDto, AppTransactionExDto appTransactionExDto, Dictionary<int, object> allFreshRootValue, string currentDatetime, string currentUserName, string workflowName, string taskName, string taskStatus, string messageTempalte)
        {
            int loopCount = 0;
            while (messageTempalte.Contains("<childunit ") && loopCount < 100)
            {
                loopCount++;
                int startTokenIndex = messageTempalte.IndexOf("<childunit unitid=");
                int endTokenIndex = messageTempalte.IndexOf("</childunit>", startTokenIndex);
                int startToken_EndIndex = messageTempalte.IndexOf(">", startTokenIndex);

                if (startTokenIndex >= 0 && endTokenIndex >= 0)
                {



                    string needToReplaceString = messageTempalte.Substring(startTokenIndex, endTokenIndex - startTokenIndex + 12);
                    string contentString = messageTempalte.Substring(startToken_EndIndex + 1, endTokenIndex - (startToken_EndIndex + 1));

                    string replaceToString = "";
                    int unitIdStartIndex = startTokenIndex + 19;
                    string unitIdString = messageTempalte.Substring(unitIdStartIndex, messageTempalte.IndexOf("\"", unitIdStartIndex) - unitIdStartIndex);

                    int? childUnitId = ControlTypeValueConverter.ConvertValueToInt(unitIdString);

                    if (childUnitId.HasValue)
                    {
                        if (appformDataDto.DictOneToManyFields.ContainsKey(childUnitId.Value.ToString()))
                        {
                            foreach (var rowData in appformDataDto.DictOneToManyFields[childUnitId.Value.ToString()])
                            {
                                replaceToString += AppProjectWorkFlowDataFormSynchBL.ReplaceMessageTemplatePlaceHolderWithActualValue(rootKeyValue, allFreshRootValue, contentString, currentDatetime, currentUserName, workflowName, taskName, taskStatus, appTransactionExDto, rowData, null, false);
                            }
                        }


                        
                    }



                    messageTempalte = messageTempalte.Replace(needToReplaceString, replaceToString);

                }
            }

            return messageTempalte;
        }


        private static List<AppMessageDto> BuildGroupedMessageList(List<AppMessageDto> messageDtoList, int groupBy)
        {
            if (groupBy == (int)EmAppConversationGroupByType.GroupByCustomerUser
                                            || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierUser
                                            || groupBy == (int)EmAppConversationGroupByType.GroupByEmployeeUser
                                            || groupBy == (int)EmAppConversationGroupByType.GroupByClientAgentUser
                                            || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgentUser
                                            || groupBy == (int)EmAppConversationGroupByType.GroupByAllUser)
            {
                var dictUserDto = AppSecurityUserBL.DictAllUserDto;

                int? domainId = null;

                if (groupBy == (int)EmAppConversationGroupByType.GroupByCustomerUser)
                {
                    domainId = (int)EmAppUserType.Customer;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupBySupplierUser)
                {
                    domainId = (int)EmAppUserType.Supplier;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupByClientAgentUser)
                {
                    domainId = (int)EmAppUserType.ClientAgent;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgentUser)
                {
                    domainId = (int)EmAppUserType.SupplierAgent;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupByEmployeeUser)
                {
                    domainId = (int)EmAppUserType.Employee;
                }

                Dictionary<int, List<AppMessageDto>> disctUserIdAndMessages = new Dictionary<int, List<AppMessageDto>>();

                foreach (var messageDto in messageDtoList)
                {
                    if (messageDto.AppCreatedById.HasValue)
                    {
                        int fromUserId = messageDto.AppCreatedById.Value;
                        AddMessageToSubGroupByUserId(dictUserDto, domainId, disctUserIdAndMessages, messageDto, fromUserId);
                    }

                    PrepareMessageToUserIdList(dictUserDto, messageDto);

                    foreach (int toUserId in messageDto.ToUserIdList.Distinct())
                    {
                        AddMessageToSubGroupByUserId(dictUserDto, domainId, disctUserIdAndMessages, messageDto, toUserId);
                    }
                }

                messageDtoList = new List<AppMessageDto>();

                int currentUserDomainId = AppSecurityUserBL.CurrentUserEntity.DomainId;
                int currentUserId = AppSecurityUserBL.CurrentUserId;

                if ((currentUserDomainId == (int)EmAppUserType.Customer && groupBy == (int)EmAppConversationGroupByType.GroupByCustomerUser)
                    || (currentUserDomainId == (int)EmAppUserType.Supplier && groupBy == (int)EmAppConversationGroupByType.GroupBySupplierUser)
                    || (currentUserDomainId == (int)EmAppUserType.ClientAgent && groupBy == (int)EmAppConversationGroupByType.GroupByClientAgentUser)
                    || (currentUserDomainId == (int)EmAppUserType.SupplierAgent && groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgentUser)
                    )
                {
                    if (disctUserIdAndMessages.ContainsKey(currentUserId))
                    {
                        messageDtoList = disctUserIdAndMessages[currentUserId];
                    }
                }
                else
                {
                    messageDtoList = BuildMessageSubGroupsByUserId(disctUserIdAndMessages, dictUserDto);
                }

            }
            else if (groupBy == (int)EmAppConversationGroupByType.GroupByCustomer
                || groupBy == (int)EmAppConversationGroupByType.GroupBySupplier
                || groupBy == (int)EmAppConversationGroupByType.GroupByClientAgent
                || groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgent)
            {
                var dictUserDto = AppSecurityUserBL.DictAllUserDto;

                int? domainId = null;

                if (groupBy == (int)EmAppConversationGroupByType.GroupByCustomer)
                {
                    domainId = (int)EmAppUserType.Customer;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupBySupplier)
                {
                    domainId = (int)EmAppUserType.Supplier;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupByClientAgent)
                {
                    domainId = (int)EmAppUserType.ClientAgent;
                }
                else if (groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgent)
                {
                    domainId = (int)EmAppUserType.SupplierAgent;
                }

                Dictionary<int, List<AppMessageDto>> dictPartnerIdAndMessages = new Dictionary<int, List<AppMessageDto>>();

                foreach (var messageDto in messageDtoList)
                {
                    if (messageDto.AppCreatedById.HasValue)
                    {
                        int fromUserId = messageDto.AppCreatedById.Value;
                        AddMessageToSubGroupByPartnerId(dictUserDto, domainId, dictPartnerIdAndMessages, messageDto, fromUserId);
                    }

                    PrepareMessageToUserIdList(dictUserDto, messageDto);

                    foreach (int toUserId in messageDto.ToUserIdList.Distinct())
                    {
                        AddMessageToSubGroupByPartnerId(dictUserDto, domainId, dictPartnerIdAndMessages, messageDto, toUserId);
                    }
                }


                messageDtoList = new List<AppMessageDto>();

                int currentUserDomainId = AppSecurityUserBL.CurrentUserEntity.DomainId;
                int currentUserId = AppSecurityUserBL.CurrentUserId;
                int? companyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

                if ((currentUserDomainId == (int)EmAppUserType.Customer && groupBy == (int)EmAppConversationGroupByType.GroupByCustomerUser)
                    || (currentUserDomainId == (int)EmAppUserType.Supplier && groupBy == (int)EmAppConversationGroupByType.GroupBySupplierUser)
                    || (currentUserDomainId == (int)EmAppUserType.ClientAgent && groupBy == (int)EmAppConversationGroupByType.GroupByClientAgentUser)
                    || (currentUserDomainId == (int)EmAppUserType.SupplierAgent && groupBy == (int)EmAppConversationGroupByType.GroupBySupplierAgentUser)
                    )
                {
                    Dictionary<int, int> dictUserIdAndPartnerId = AppCacheManagerBL.GetOneCompanyUserIdAndPartnerIdDictionary(companyId.Value);

                    if (dictUserIdAndPartnerId.ContainsKey(currentUserId))
                    {
                        int partnerId = dictUserIdAndPartnerId[currentUserId];

                        if (dictPartnerIdAndMessages.ContainsKey(partnerId))
                        {
                            messageDtoList = dictPartnerIdAndMessages[partnerId];
                        }
                    }
                }
                else
                {
                    messageDtoList = BuildMessageSubGroupsByPartnerId(dictPartnerIdAndMessages);
                }


            }

            return messageDtoList;
        }


        //  {0}:UserName,  {1}:Time,  {2}:Subject {3}: Message
        public static string EmailTemplate_WorkFlowTaskNewCommentAdded = "<div style='padding:4px;'>" +
"        <div style='padding:5px;'>" +
"            <div style='color:gray;'>" +
"                <div style='font-size:16px;padding:5px;'>" +
"                    {0} posted a new tast message at {1} (UTC)" +
"                </div>" +
"                <div style='font-size:14px;padding:5px;border:solid 1px;'>" +
"                    <div style='padding:5px;font-weight:700'>{2}</div>" +
"                    <div style='padding:5px;'>{3}</div>" +
"                </div>                " +
"            </div>            " +
"        </div>        " +
"    </div>";





        //  {0}:Task Name,  {1}:Task Description, {2}:Task Notes,  {3}:Resources String, {4}:Workflow Name,

        public static string EmailTemplate_WorkFlowTaskDetail = "<div style='padding:20px 4px 4px 4px;'>" +
"        <div style='padding:5px;'>" +
"            <div style='color:gray;padding:5px;'>" +
"                <div style='font-size:16px;'><span style='width:100px;display:inline-block;'>Task:</span>{0}</div>" +
"            " +
"                <div style='font-size:14px;'><span style='width:100px;display:inline-block;'>Description:</span>{1}</div>" +
"            </div>" +
"            <div style='color:darkgrey;padding:5px;font-size:12px;'>" +
"                <div>" +
"                    {2}" +
"                </div>                " +
"            </div>" +
"        </div>" +
"        <div style='padding:10px;color:gray;padding:5px;'>" +
"            <div style='padding:5px;border:solid 1px;'>" +
"                <div style='font-size:16px;padding:5px;'>Task Details</div>" +
"                <table style='padding:5px;font-size:14px;'>" +
"                    <tr>" +
"                        <td>Assignee:</td>" +
"                        <td style='color:dodgerblue;'>{3}</td>" +
"                    </tr>                    " +
"                    <tr>" +
"                        <td>Project:</td>" +
"                        <td style='color:dodgerblue;'>{4}</td>" +
"                    </tr>" +
"                    " +
"                </table>" +
"            </div>" +
"        </div>" +
"    </div>";







    }
}