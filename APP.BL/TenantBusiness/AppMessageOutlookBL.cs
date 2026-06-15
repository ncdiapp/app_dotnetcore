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

using APP.Framework;
namespace App.BL
{
    public static class AppMessageOutlookBL
    {
       

        public static IEnumerable<AppMessageDto> RetrieveCurrentUserUnReadMessages()
        {

            return RetrieveCurrentUserInComeMessages(true);

        }

        public static IEnumerable<AppMessageDto> RetrieveCurrentUserUnReadMessagesByUserId()
        {

            return RetrieveCurrentUserInComeMessages(true, null, null, null, AppSecurityUserBL.CurrentUserId);
           
        }


        public static OperationCallResult<bool> SetMessageReadState(bool isRead, IEnumerable<int> messgeUpdateIds)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>() { ValidationResult = new ValidationResult() };

            AppMessageUserReceivedEntity userMessageEntity = new AppMessageUserReceivedEntity();

            if (isRead)
            {
                userMessageEntity.ReadDate = DateTime.UtcNow;
            }
            else
            {
                userMessageEntity.ReadDate = null;
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    RelationPredicateBucket filter = new RelationPredicateBucket();

                    filter.PredicateExpression.Add(AppMessageUserReceivedFields.MessageId == messgeUpdateIds.ToArray());
                    filter.PredicateExpression.Add(AppMessageUserReceivedFields.ReceivedById == ServerContext.Instance.CurrentUid);

                    adapter.UpdateEntitiesDirectly(userMessageEntity, filter);

                    string message = StringLocalizer.Localize("Message_Update_OK", "Message Has Been Updated Successfully ");
                    operationCallResult.ValidationResult.AddItem(null, "Message_Update_OK", ValidationItemType.Message, message);
                    adapter.Commit();
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize("AppMessage_Update_Failed", " Message Update Failed" + ex.ToString());
                    operationCallResult.ValidationResult.Items.Add(new ValidationItem(null, "AppMessage_Update_Failed", ValidationItemType.Error, message));
                }
            }
            if (!operationCallResult.ValidationResult.HasErrors)
            {
                operationCallResult.Object = true;
            }

            return operationCallResult;
        }






        public static AppMessageDto GetMessageReplyDto(int? messageId)
        {
            if (messageId.HasValue)
            {
                AppMessageDto orgMessageDto = AppMessageBL.RetrieveOneAppMessageExDto(messageId.Value);

                if (orgMessageDto != null)
                {
                    AppMessageDto newMessageDto = new AppMessageDto();
                    newMessageDto.TransactionId = orgMessageDto.TransactionId;
                    newMessageDto.TransactionRootValueId = orgMessageDto.TransactionRootValueId;
                    newMessageDto.ProjectId = orgMessageDto.ProjectId;
                    newMessageDto.ProjectActivityId = orgMessageDto.ProjectActivityId;
                    newMessageDto.Subject = "RE: " + orgMessageDto.Subject;
                    newMessageDto.ToList = orgMessageDto.FromEmail;
                    newMessageDto.Cclist = string.Empty;
                    newMessageDto.Bcclist = string.Empty;
                    newMessageDto.MessgaeScopeType = orgMessageDto.MessgaeScopeType;
                    newMessageDto.Message = "<br /><br /><br />  <hr>  <br />" + orgMessageDto.Message;

                    return newMessageDto;
                }
            }

            return null;
        }

        public static AppMessageDto GetMessageReplyAllDto(int? messageId)
        {
            if (messageId.HasValue)
            {
                AppMessageDto orgMessageDto = AppMessageBL.RetrieveOneAppMessageExDto(messageId.Value);

                if (orgMessageDto != null)
                {
                    AppMessageDto newMessageDto = new AppMessageDto();

                    newMessageDto.TransactionId = orgMessageDto.TransactionId;
                    newMessageDto.TransactionRootValueId = orgMessageDto.TransactionRootValueId;
                    newMessageDto.ProjectId = orgMessageDto.ProjectId;
                    newMessageDto.ProjectActivityId = orgMessageDto.ProjectActivityId;

                    newMessageDto.Subject = "RE: " + orgMessageDto.Subject;
                    newMessageDto.ToList = orgMessageDto.FromEmail;
                    newMessageDto.Cclist = orgMessageDto.ToList + orgMessageDto.Cclist;
                    newMessageDto.Bcclist = orgMessageDto.Bcclist;
                    newMessageDto.MessgaeScopeType = orgMessageDto.MessgaeScopeType;
                    newMessageDto.Message = "<br /><br /><br />  <hr>  <br />" + orgMessageDto.Message;

                    return newMessageDto;
                }
            }

            return null;
        }

        public static AppMessageDto GetMessageForwardDto(int? messageId)
        {
            if (messageId.HasValue)
            {
                AppMessageDto orgMessageDto = AppMessageBL.RetrieveOneAppMessageExDto(messageId.Value);

                if (orgMessageDto != null)
                {
                    AppMessageDto newMessageDto = new AppMessageDto();
                    newMessageDto.TransactionId = orgMessageDto.TransactionId;
                    newMessageDto.TransactionRootValueId = orgMessageDto.TransactionRootValueId;
                    newMessageDto.ProjectId = orgMessageDto.ProjectId;
                    newMessageDto.ProjectActivityId = orgMessageDto.ProjectActivityId;
                    newMessageDto.Subject = "FW: " + orgMessageDto.Subject;
                    newMessageDto.ToList = string.Empty;
                    newMessageDto.Cclist = string.Empty;
                    newMessageDto.Bcclist = string.Empty;
                    newMessageDto.MessgaeScopeType = orgMessageDto.MessgaeScopeType;
                    newMessageDto.Message = "<br /><br /><br />  <hr>  <br />" + orgMessageDto.Message;
                    newMessageDto.AttachmentFileToken = orgMessageDto.AttachmentFileToken;
                    //newMessageDto.AttachmentFileDtoList = orgMessageDto.AttachmentFileDtoList;
                    newMessageDto.DictAttachmentFileIdAndDisplay = orgMessageDto.DictAttachmentFileIdAndDisplay;
                    return newMessageDto;
                }
            }

            return null;
        }


        public static OperationCallResult<bool> DeleteUserMessages(List<int> needToDeleteMessageIds, bool isDeleteReceivedMessage = true)
        {
            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>() { ValidationResult = new ValidationResult() };


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    foreach (int messageId in needToDeleteMessageIds)
                    {
                        AppMessageDeletedEntity messageDeletedEntity = new AppMessageDeletedEntity();

                        AppMessageDeletedDto messageDeletedDto = new AppMessageDeletedDto();
                        messageDeletedDto.MessageId = messageId;

                        if (isDeleteReceivedMessage)
                        {
                            messageDeletedDto.ReceivedEmail = AppSecurityUserBL.CurrentUserEntity.Email;
                        }
                        else
                        {
                            messageDeletedDto.SenderEmail = AppSecurityUserBL.CurrentUserEntity.Email;
                        }


                        AppMessageDeletedConverter.CopyDtoToEntity(messageDeletedEntity, messageDeletedDto);
                        adapter.SaveEntity(messageDeletedEntity);

                    }

                    adapter.DeleteEntitiesDirectly(typeof(AppMessageUserReceivedEntity), new RelationPredicateBucket(AppMessageUserReceivedFields.MessageId == needToDeleteMessageIds & AppMessageUserReceivedFields.ReceivedById == ServerContext.Instance.CurrentUid));

                    adapter.Commit();
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    string message = StringLocalizer.Localize("AppMessage_Delete_Failed", " Message Delete Failed" + ex.ToString());
                    operationCallResult.ValidationResult.Items.Add(new ValidationItem(null, "AppMessage_Delete_Failed", ValidationItemType.Error, message));
                }
            }
            if (!operationCallResult.ValidationResult.HasErrors)
            {
                operationCallResult.Object = true;
            }

            return operationCallResult;
        }

        public static IEnumerable<AppMessageDto> RetrieveCurrentUserInComeMessages(bool isOnlyFetchUnRead = false, int? transactionId = null, int? transctionRid = null, int? messgaeScopeType = null, int? userId = null)
        {
            if (messgaeScopeType.HasValue && messgaeScopeType.Value == (int)EmAppMessgaeScopeType.CompanyPublicMessage)
            {
                return RetrieveCurrentCompanyPublicMessages();
            }

            AppSecurityUserEntity userEntity = null;

            if (userId.HasValue)
            {
                userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(userId.Value);
            }
            else
            {
                userEntity = AppSecurityUserBL.CurrentUserEntity;
            }

            if (userEntity == null) return Enumerable.Empty<AppMessageDto>();

            EntityCollection<AppMessageUserReceivedEntity> list = new EntityCollection<AppMessageUserReceivedEntity>();


            ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
            excludeMessageFieldsList.Add(AppMessageFields.Message);


            string currnetUserEmailAdres = userEntity.Email;
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppMessageUserReceivedEntity);
            rootPath.Add(AppMessageUserReceivedEntity.PrefetchPathAppMessage, excludeMessageFieldsList);


            //RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageUserReceivedFields.ReceivedEmail == currnetUserEmailAdres);
            RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageUserReceivedFields.ReceivedById == userEntity.UserId);
            filter.Relations.Add(AppMessageEntity.Relations.AppMessageUserReceivedEntityUsingMessageId);

            filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == System.DBNull.Value | AppMessageFields.MessgaeScopeType != (int)EmAppMessgaeScopeType.MessageTemplate);

            if (isOnlyFetchUnRead)
            {


                filter.PredicateExpression.Add(AppMessageUserReceivedFields.ReadDate == DBNull.Value);
            }

            if (transactionId.HasValue && transctionRid.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.TransactionId == transactionId.Value & AppMessageFields.TransactionRootValueId == transctionRid.Value);
            }
            //else {
            //    filter.PredicateExpression.Add(AppMessageFields.MessagePostType != (int)EmAppMessgaePostType.Conversaction);
            //}


            if (messgaeScopeType.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == messgaeScopeType.Value);
            }


            var notInDeleteCollection = new FieldCompareSetPredicate
             (
              AppMessageUserReceivedFields.MessageId,
               null,
                 AppMessageDeletedFields.MessageId,
               null,

               SetOperator.In,
               new PredicateExpression(AppMessageDeletedFields.ReceivedEmail == currnetUserEmailAdres),
               true

              );

            // negate:( not ,false

            filter.PredicateExpression.Add(notInDeleteCollection);


            SortExpression sorter = new SortExpression();
            sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);


            List<AppMessageDto> messageDtos = new List<AppMessageDto>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter, rootPath);
            }


            foreach (var userMessageEntity in list)
            {
                var messageEntity = userMessageEntity.AppMessage;

                // DsiaplyName: Guid


                AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);


                string attachmentFile = messageEntity.AttachmentFileToken;
                if (!string.IsNullOrWhiteSpace(attachmentFile))
                {
                    List<KeyValuePair<string, string>> fileLinkList = new List<KeyValuePair<string, string>>();

                    string[] fileTokeList = attachmentFile.Split("|".ToArray());

                    foreach (string fileLink in fileTokeList)
                    {



                    }



                }

                //  newMessageDto.AttachmentDisplayAndLink = new List<string>();

                if (userMessageEntity.ReadDate != null)
                {
                    newMessageDto.IsRead = true;
                }


                messageDtos.Add(newMessageDto);
            }

            return messageDtos;
        }


        public static IEnumerable<AppMessageDto> RetrieveCurrentUserOutComeMessages(int? transactionId = null, int? transctionRid = null, int? messgaeScopeType = null)
        {

            string currnetUserEmailAdres = AppSecurityUserBL.CurrentUserEntity.Email;

            EntityCollection<AppMessageEntity> list = new EntityCollection<AppMessageEntity>();
            ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
            excludeMessageFieldsList.Add(AppMessageFields.Message);


            //RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageUserReceivedFields.ReceivedEmail == currnetUserEmailAdres);
            RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.AppCreatedById == ServerContext.Instance.CurrentUid);

            if (transactionId.HasValue && transctionRid.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.TransactionId == transactionId.Value & AppMessageFields.TransactionRootValueId == transctionRid.Value);
            }
            //else
            //{
            //    filter.PredicateExpression.Add(AppMessageFields.MessagePostType != (int)EmAppMessgaePostType.Conversaction);
            //}

            if (messgaeScopeType.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == messgaeScopeType.Value);
            }


            var notInDeleteCollection = new FieldCompareSetPredicate
             (
              AppMessageFields.MessageId,
               null,
                 AppMessageDeletedFields.MessageId,
               null,

               SetOperator.In,
               new PredicateExpression(AppMessageDeletedFields.SenderEmail == currnetUserEmailAdres),
               true

              );

            // negate:( not ,false

            filter.PredicateExpression.Add(notInDeleteCollection);


            SortExpression sorter = new SortExpression();
            sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);


            List<AppMessageDto> messageDtos = new List<AppMessageDto>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter);
            }


            foreach (var messageEntity in list)
            {

                AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);

                //  newMessageDto.AttachmentDisplayAndLink = new List<string>();

                messageDtos.Add(newMessageDto);
            }

            return messageDtos;
        }


        public static IEnumerable<AppMessageDto> RetrieveCurrentUserDeletedMessages(int? transactionId = null, int? transctionRid = null, int? messageType = null)
        {


            EntityCollection<AppMessageDeletedEntity> list = new EntityCollection<AppMessageDeletedEntity>();


            ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
            excludeMessageFieldsList.Add(AppMessageFields.Message);


            string currnetUserEmailAdres = AppSecurityUserBL.CurrentUserEntity.Email;
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppMessageDeletedEntity);
            rootPath.Add(AppMessageDeletedEntity.PrefetchPathAppMessage, excludeMessageFieldsList);


            //RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageDeletedFields.ReceivedEmail == currnetUserEmailAdres);
            RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageDeletedFields.ReceivedEmail == currnetUserEmailAdres | AppMessageDeletedFields.SenderEmail == currnetUserEmailAdres);
            filter.Relations.Add(AppMessageEntity.Relations.AppMessageDeletedEntityUsingMessageId);


            if (transactionId.HasValue && transctionRid.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.TransactionId == transactionId.Value & AppMessageFields.TransactionRootValueId == transctionRid.Value);
            }
            //else
            //{
            //    filter.PredicateExpression.Add(AppMessageFields.MessagePostType != (int)EmAppMessgaePostType.Conversaction);
            //}

            if (messageType.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == messageType.Value);
            }


            SortExpression sorter = new SortExpression();
            sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);


            List<AppMessageDto> messageDtos = new List<AppMessageDto>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter, rootPath);
            }


            foreach (var userMessageEntity in list)
            {
                var messageEntity = userMessageEntity.AppMessage;

                AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);

                messageDtos.Add(newMessageDto);
            }

            return messageDtos;

        }


        public static IEnumerable<AppMessageDto> RetrieveCurrentUserDraftMessages(int? transactionId = null, int? transctionRid = null, int? messgaeScopeType = null)
        {

            string currnetUserEmailAdres = AppSecurityUserBL.CurrentUserEntity.Email;

            EntityCollection<AppMessageEntity> list = new EntityCollection<AppMessageEntity>();
            ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
            excludeMessageFieldsList.Add(AppMessageFields.Message);


            //RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageUserReceivedFields.ReceivedEmail == currnetUserEmailAdres);
            RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.AppCreatedById == ServerContext.Instance.CurrentUid);
            filter.PredicateExpression.Add(AppMessageFields.IsDraft == true);

            if (transactionId.HasValue && transctionRid.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.TransactionId == transactionId.Value & AppMessageFields.TransactionRootValueId == transctionRid.Value);
            }
            //else
            //{
            //    filter.PredicateExpression.Add(AppMessageFields.MessagePostType != (int)EmAppMessgaePostType.Conversaction);
            //}

            if (messgaeScopeType.HasValue)
            {
                filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == messgaeScopeType.Value);
            }


            var notInDeleteCollection = new FieldCompareSetPredicate
             (
              AppMessageFields.MessageId,
               null,
                 AppMessageDeletedFields.MessageId,
               null,

               SetOperator.In,
               new PredicateExpression(AppMessageDeletedFields.SenderEmail == currnetUserEmailAdres),
               true

              );

            // negate:( not ,false

            filter.PredicateExpression.Add(notInDeleteCollection);


            SortExpression sorter = new SortExpression();
            sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);


            List<AppMessageDto> messageDtos = new List<AppMessageDto>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter);
            }


            foreach (var messageEntity in list)
            {

                AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);
                //  newMessageDto.AttachmentDisplayAndLink = new List<string>();

                messageDtos.Add(newMessageDto);
            }

            return messageDtos;
        }

        public static IEnumerable<AppMessageDto> RetrieveTransactionFormMessages(int transactionId, int transctionRid)
        {
            string currnetUserEmailAdres = AppSecurityUserBL.CurrentUserEntity.Email;

            EntityCollection<AppMessageEntity> list = new EntityCollection<AppMessageEntity>();
            ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
            excludeMessageFieldsList.Add(AppMessageFields.Message);

            var currentUserEntity = AppSecurityUserBL.CurrentUserEntity;

            if (currentUserEntity.IsInSysAdminDomain)
            {
                RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.TransactionId == transactionId
                    & AppMessageFields.TransactionRootValueId == transctionRid
                    & AppMessageFields.MessgaeScopeType == (int)EmAppMessgaeScopeType.Transaction
                    //& AppMessageFields.MessagePostType != (int)EmAppMessgaePostType.Conversaction
                    );

                SortExpression sorter = new SortExpression();
                sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);

                List<AppMessageDto> messageDtos = new List<AppMessageDto>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter);
                }

                foreach (var messageEntity in list)
                {
                    AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                    newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);
                    messageDtos.Add(newMessageDto);
                }
                messageDtos.Where(o => o.IsDraft.HasValue && o.IsDraft.Value).ForAll(o => o.MessagePostType = (int)EmAppMessgaePostType.Drat);
                return messageDtos;
            }
            else
            {
                List<AppMessageDto> messageDtos = RetrieveCurrentUserInComeMessages(false, transactionId, transctionRid, (int)EmAppMessgaeScopeType.Transaction).ToList();
                List<AppMessageDto> draftMessagetos = RetrieveCurrentUserDraftMessages(transactionId, transctionRid, (int)EmAppMessgaeScopeType.Transaction).ToList();


                messageDtos.AddRange(draftMessagetos);

                messageDtos.Where(o => o.IsDraft.HasValue && o.IsDraft.Value).ForAll(o => o.MessagePostType = (int)EmAppMessgaePostType.Drat);

                return messageDtos;
            }

        }

        public static IEnumerable<AppMessageDto> RetrieveCurrentUserAllConversations(int? transactionId = null, int? transctionRid = null, int? messgaeScopeType = null)
        {
            var currentUserEntity = AppSecurityUserBL.CurrentUserEntity;

            if (currentUserEntity.IsInSysAdminDomain)
            {
                string currnetUserEmailAdres = AppSecurityUserBL.CurrentUserEntity.Email;

                EntityCollection<AppMessageEntity> list = new EntityCollection<AppMessageEntity>();
                ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
                excludeMessageFieldsList.Add(AppMessageFields.Message);

                RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.MessagePostType == (int)EmAppMessgaePostType.Conversaction);

                if (transactionId.HasValue && transctionRid.HasValue)
                {
                    filter.PredicateExpression.Add(AppMessageFields.TransactionId == transactionId.Value & AppMessageFields.TransactionRootValueId == transctionRid.Value);
                }

                if (messgaeScopeType.HasValue)
                {
                    filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == messgaeScopeType.Value);
                }


                SortExpression sorter = new SortExpression();
                sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);

                List<AppMessageDto> messageDtos = new List<AppMessageDto>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter);
                }

                foreach (var messageEntity in list)
                {
                    AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                    newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);
                    messageDtos.Add(newMessageDto);
                }

                return messageDtos;
            }
            else
            {
                EntityCollection<AppMessageUserReceivedEntity> list = new EntityCollection<AppMessageUserReceivedEntity>();

                ExcludeFieldsList excludeMessageFieldsList = new ExcludeFieldsList();
                excludeMessageFieldsList.Add(AppMessageFields.Message);

                string currnetUserEmailAdres = AppSecurityUserBL.CurrentUserEntity.Email;
                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppMessageUserReceivedEntity);
                rootPath.Add(AppMessageUserReceivedEntity.PrefetchPathAppMessage, excludeMessageFieldsList);

                RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageUserReceivedFields.ReceivedById == ServerContext.Instance.CurrentUid);
                filter.Relations.Add(AppMessageEntity.Relations.AppMessageUserReceivedEntityUsingMessageId);
                filter.PredicateExpression.Add(AppMessageFields.MessagePostType == (int)EmAppMessgaePostType.Conversaction);

                if (transactionId.HasValue && transctionRid.HasValue)
                {
                    filter.PredicateExpression.Add(AppMessageFields.TransactionId == transactionId.Value & AppMessageFields.TransactionRootValueId == transctionRid.Value);
                }

                if (messgaeScopeType.HasValue)
                {
                    filter.PredicateExpression.Add(AppMessageFields.MessgaeScopeType == messgaeScopeType.Value);
                }

                var notInDeleteCollection = new FieldCompareSetPredicate
                 (
                  AppMessageUserReceivedFields.MessageId,
                   null,
                     AppMessageDeletedFields.MessageId,
                   null,

                   SetOperator.In,
                   new PredicateExpression(AppMessageDeletedFields.ReceivedEmail == currnetUserEmailAdres),
                   true

                  );


                filter.PredicateExpression.Add(notInDeleteCollection);

                SortExpression sorter = new SortExpression();
                sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);

                List<AppMessageDto> messageDtos = new List<AppMessageDto>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter, rootPath);
                }

                foreach (var userMessageEntity in list)
                {
                    var messageEntity = userMessageEntity.AppMessage;

                    AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                    newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);

                    messageDtos.Add(newMessageDto);
                }

                return messageDtos;
            }
        }


        //public static List<AppSecurityUserDto> GetTransactionFormSubscribedUsers(int transactionId, int transactionRootValueId)
        //{
        //    List<AppSecurityUserDto> userList = new List<AppSecurityUserDto>();

        //    List<AppProjectOrWorkFlowExDto> workflowList = AppProjectWorkFlowProcessBL.GetTransactionRunningProjectWorkflowList(transactionId, transactionRootValueId);

        //    if (workflowList.Count > 0)
        //    {
        //        int? projectId = ControlTypeValueConverter.ConvertValueToInt(workflowList.First().Id);

        //        if (projectId.HasValue)
        //        {
        //            userList = AppProjectWorkFlowStructureBL.RetrieveOneWorkFlowAllTaskResourceUsers(projectId);
        //        }
        //    }

        //    return userList;
        //}

        private static IEnumerable<AppMessageDto> RetrieveCurrentCompanyPublicMessages()
        {


            EntityCollection<AppMessageEntity> list = new EntityCollection<AppMessageEntity>();
            IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppMessageEntity);           
            
            RelationPredicateBucket filter = new RelationPredicateBucket(AppMessageFields.MessgaeScopeType == (int)EmAppMessgaeScopeType.CompanyPublicMessage);
            
            SortExpression sorter = new SortExpression();
            sorter.Add(AppMessageFields.AppCreatedDate | SortOperator.Descending);


            List<AppMessageDto> messageDtos = new List<AppMessageDto>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.FetchEntityCollection(list, filter, int.MaxValue, sorter, rootPath);
            }


            foreach (var messageEntity in list)
            {
              
                AppMessageDto newMessageDto = AppMessageConverter.ConvertEntityToDto(messageEntity);
                newMessageDto.Subject = AppMessageBL.ConvertMessageAllUtcTicksTokenToClientDateTimeString(newMessageDto.Subject);

                string attachmentFile = messageEntity.AttachmentFileToken;
                if (!string.IsNullOrWhiteSpace(attachmentFile))
                {
                    List<KeyValuePair<string, string>> fileLinkList = new List<KeyValuePair<string, string>>();

                    string[] fileTokeList = attachmentFile.Split("|".ToArray());

                    foreach (string fileLink in fileTokeList)
                    {

                    }
                }               

                messageDtos.Add(newMessageDto);
            }

            return messageDtos;
        }

    }
}