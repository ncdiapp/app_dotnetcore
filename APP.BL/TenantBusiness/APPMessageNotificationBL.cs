using System.Collections.Generic;
using System.Data;
using System.Linq;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
//using APP.Persistence.Common;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using System.Data.SqlClient;
using System;

using APP.Framework;
namespace App.BL
{
    public static class APPMessageNotificationBL
    {



        // aAppmessageNotificationSettingExDto with TargetValueIdList
        public static void ProcessPeriodMessageSettingWithTargetDate(AppmessageNotificationSettingExDto aAppmessageNotificationSettingExDto)
        {

            List<object> targetValueIdList = aAppmessageNotificationSettingExDto.TargetValueIdList;

            foreach (object targetValueId in targetValueIdList)
            {
                if (aAppmessageNotificationSettingExDto.NotificationQueryContentSettingId.HasValue)
                {
                    AppmessageNotificationSettingExDto contentSetting = APPMessageNotificationBL.RetrieveOneAppmessageNotificationSettingExDto(aAppmessageNotificationSettingExDto.NotificationQueryContentSettingId.Value);

                    if (contentSetting != null)
                    {
                        //AppmessageNotificationSettingExDto contentSetting = aAppmessageNotificationSettingExDto.ForeignAppmessageNotificationSettingExDto;

                        if (!string.IsNullOrWhiteSpace(contentSetting.NotificationQuery))
                        {
                            string query = contentSetting.NotificationQuery;
                            var paramterList = new List<SqlParameter>();
                            paramterList.Add(new SqlParameter("@targetValueId", targetValueId));


                            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                            {
                                var datatable = adapter.ExecuteDataTableRetrievalQuery(query, paramterList);
                                List<AppMessageDto> messageDtoList = ConvertNotificationSettingContentDataTableToMessageDto(contentSetting, datatable, targetValueId);

                                AppMessageBL.SendEmailFromAppMessageDtoList(messageDtoList);
                            }
                        }
                    }
                }
                else if (aAppmessageNotificationSettingExDto.MessageTemplateId.HasValue)
                {

                }
            }
        }

       

        public static List<AppmessageNotificationSettingExDto> GetPeriodMessageSettingWithTargetDate()
        {
            List<AppmessageNotificationSettingExDto> listExDto = new List<AppmessageNotificationSettingExDto>();

            EntityCollection<AppmessageNotificationSettingEntity> list = new EntityCollection<AppmessageNotificationSettingEntity>();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                RelationPredicateBucket fitler = new RelationPredicateBucket(AppmessageNotificationSettingFields.MessageUsageType == (int)EmAppNotificationMessageUsageType.TargetDateQuery);
                adapter.FetchEntityCollection(list, fitler);

            }

            foreach (var o in list)
            {
                AppmessageNotificationSettingExDto aDto = AppmessageNotificationSettingConverter.ConvertEntityToExDto(o);
                listExDto.Add(aDto);
            }

            return listExDto;
        }

        public static EntityCollection<AppmessageNotificationSettingEntity> RetrieveAllAppmessageNotificationSettingEntity(int? transactionId, int? projectId, int? usageType)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppmessageNotificationSettingEntity> list = new EntityCollection<AppmessageNotificationSettingEntity>();
                SortClause aSortClause = AppmessageNotificationSettingFields.SettingName | SortOperator.Ascending;

                RelationPredicateBucket fitler = new RelationPredicateBucket();

                if (transactionId.HasValue)
                {
                    fitler.PredicateExpression.AddWithAnd(AppmessageNotificationSettingFields.TranscationId == transactionId.Value);
                }

                if (projectId.HasValue)
                {
                    fitler.PredicateExpression.AddWithAnd(AppmessageNotificationSettingFields.ProejctId == projectId.Value);
                }

                if (usageType.HasValue)
                {
                    fitler.PredicateExpression.AddWithAnd(AppmessageNotificationSettingFields.MessageUsageType == usageType.Value);
                }


                adapter.FetchEntityCollection(list, fitler, 0, new SortExpression(aSortClause), null);



                return list;
            }
        }



        public static AppmessageNotificationSettingEntity RetrieveOneAppmessageNotificationSettingEntity(object Id)
        {
            int? reportId = ControlTypeValueConverter.ConvertValueToInt(Id);
            if (reportId.HasValue)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity = new AppmessageNotificationSettingEntity(reportId.Value);
                    adapter.FetchEntity(aAppmessageNotificationSettingEntity);
                    return aAppmessageNotificationSettingEntity;
                }
            }
            else
            {
                return null;
            }

        }

        public static ObservableSet<AppmessageNotificationSettingExDto> RetrieveAllAppmessageNotificationSettingEntityDto(int? transactionId, int? projectId, int? usageType)
        {
            ObservableSet<AppmessageNotificationSettingExDto> aSet = new ObservableSet<AppmessageNotificationSettingExDto>();
            EntityCollection<AppmessageNotificationSettingEntity> list = RetrieveAllAppmessageNotificationSettingEntity(transactionId, projectId, usageType);
            foreach (var o in list)
            {
                AppmessageNotificationSettingExDto aDto = AppmessageNotificationSettingConverter.ConvertEntityToExDto(o);
                aSet.Add(aDto);
            }

            return aSet;
        }

        public static AppmessageNotificationSettingExDto RetrieveOneAppmessageNotificationSettingExDto(object Id)
        {
            AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity = RetrieveOneAppmessageNotificationSettingEntity(Id);
            AppmessageNotificationSettingExDto aAppmessageNotificationSettingExDto = AppmessageNotificationSettingConverter.ConvertEntityToExDto(aAppmessageNotificationSettingEntity);



            return aAppmessageNotificationSettingExDto;
        }

        public static OperationCallResult<AppmessageNotificationSettingExDto> SaveAllAppmessageNotificationSettingEntityDto(ObservableSet<AppmessageNotificationSettingExDto> aSet, int? transactionId, int? projectId)
        {
            OperationCallResult<AppmessageNotificationSettingExDto> aOperationCallResult = new OperationCallResult<AppmessageNotificationSettingExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            foreach (var ItemDto in aSet)
            {
                validationResult.Merge(ItemDto.ValidateDto());
            }

            if (validationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = validationResult;
                return aOperationCallResult;
            }

            foreach (var newItemDto in aSet.FindNewItems())
            {
                var result = ProcessNewDto(newItemDto);
                validationResult.Merge(result);
            }

            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));
            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(DeleteOneAppmessageNotificationSettingEntity(Id)));

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_AppmessageNotificationSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.ObjectList = RetrieveAllAppmessageNotificationSettingEntityDto(transactionId, projectId, null);
            }

            return aOperationCallResult;
        }

        public static OperationCallResult<AppmessageNotificationSettingExDto> SaveOneAppmessageNotificationSettingEntityDto(AppmessageNotificationSettingExDto aAppmessageNotificationSettingExDto)
        {
            OperationCallResult<AppmessageNotificationSettingExDto> aOperationCallResult = new OperationCallResult<AppmessageNotificationSettingExDto>();

            var aValidationResult = aAppmessageNotificationSettingExDto.ValidateDto();
            if (aValidationResult.HasErrors)
            {
                aOperationCallResult.ValidationResult = aValidationResult;
                return aOperationCallResult;
            }

            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (aAppmessageNotificationSettingExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(aAppmessageNotificationSettingExDto));
            }
            else if (aAppmessageNotificationSettingExDto.IsModified)
            {
                validationResult.Merge(ProcessDirtyDto(aAppmessageNotificationSettingExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_AppmessageNotificationSettingEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                var entity = RetrieveOneAppmessageNotificationSettingEntity(aAppmessageNotificationSettingExDto.Id);
                aOperationCallResult.Object = AppmessageNotificationSettingConverter.ConvertEntityToExDto(entity);

            }

            return aOperationCallResult;
        }

        //  RetrieveAllAppmessageNotificationSettingEntityDto(aAppmessageNotificationSettingExDto.Id )

        public static OperationCallResult<object> DeleteOneAppmessageNotificationSettingEntityDto(object Id)
        {
            OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();

            ValidationResult avalidationResult = DeleteOneAppmessageNotificationSettingEntity(Id);
            aOperationCallResult.ValidationResult = avalidationResult;
            if (!aOperationCallResult.ValidationResult.HasErrors)
            {
                aOperationCallResult.Object = Id;
            }

            return aOperationCallResult;
        }

        private static ValidationResult DeleteOneAppmessageNotificationSettingEntity(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppmessageNotificationSettingEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppmessageNotificationSettingEntity), new RelationPredicateBucket(AppmessageNotificationSettingFields.NotificationSettingId == Id));
                    adapter.Commit();
                }


                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessNewDto(AppmessageNotificationSettingExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppmessageNotificationSettingEntity aParentAppmessageNotificationSettingEntity = new AppmessageNotificationSettingEntity();

            AppmessageNotificationSettingConverter.CopyDtoToEntity(aParentAppmessageNotificationSettingEntity, aDto);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppmessageNotificationSettingEntity);

                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    aDto.Id = aParentAppmessageNotificationSettingEntity.NotificationSettingId;

                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static ValidationResult ProcessDirtyDto(AppmessageNotificationSettingExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppmessageNotificationSettingEntity aAppmessageNotificationSettingEntity = RetrieveOneAppmessageNotificationSettingEntity(aDto.Id);

            AppmessageNotificationSettingConverter.CopyDtoToEntity(aAppmessageNotificationSettingEntity, aDto);

            // bug test
            // aDto.Id = 100;

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aAppmessageNotificationSettingEntity, false, true);
                    adapter.Commit();
                    //aValidationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_DataSetEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppmessageNotificationSettingExDto), "App_DataSetEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        private static List<AppMessageDto> ConvertNotificationSettingContentDataTableToMessageDto(AppmessageNotificationSettingExDto contentSetting, DataTable datatable, object targetValueId)
        {
            List<AppMessageDto> messageDtoList = new List<AppMessageDto>();

            foreach (DataRow row in datatable.Rows)
            {
                var messageDto = new AppMessageDto();
                messageDto.MessagePostType = (int)EmAppMessgaePostType.SystemNotification;
                messageDto.TransactionId = contentSetting.TranscationId;
                messageDto.ProjectId = contentSetting.ProejctId;
                messageDto.TransactionRootValueId = targetValueId != null ? targetValueId.ToString() : string.Empty;

                EmAppMessgaeScopeType? scopeType = null;

                if (messageDto.TransactionId.HasValue)
                {
                    scopeType = EmAppMessgaeScopeType.Transaction;
                    messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Transaction;
                }
                else if (messageDto.ProjectId.HasValue)
                {
                    scopeType = EmAppMessgaeScopeType.Project;
                    messageDto.MessgaeScopeType = (int)EmAppMessgaeScopeType.Project;
                }

                var followupDtoList = AppMessageBL.RetrieveAppUserMessgeFollowupDtoListByScope(scopeType, messageDto.TransactionId, messageDto.TransactionRootValueId, null, messageDto.ProjectId, null);

                messageDto.ToUserIdList = followupDtoList.Where(o => o.UserId.HasValue).Select(o => o.UserId.Value).ToList();

                if (messageDto.ToUserIdList.Count > 0)
                {
                    if (row[AppMessageDto.SubjectField] != null)
                    {
                        messageDto.Subject += row[AppMessageDto.SubjectField].ToString();
                    }
                    else
                    {
                        messageDto.Subject = "Message Notification " + DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
                    }

                    List<string> paramList = new List<string>();

                    for (int i = 1; i < datatable.Columns.Count; i++)
                    {
                        string columnName = datatable.Columns[i].ColumnName;
                        string paramValue = row[columnName] == null ? string.Empty : row[columnName].ToString();
                        paramList.Add(paramValue);
                    }


                    if (!string.IsNullOrWhiteSpace(contentSetting.MessageTemplate))
                    {
                        try
                        {
                            messageDto.Message = string.Format(contentSetting.MessageTemplate, paramList.ToArray());
                        }
                        catch (Exception ex)
                        {

                        }                        
                    }                   
                    else
                    {
                        messageDto.Message = messageDto.Subject;
                    }

                    messageDtoList.Add(messageDto);
                }
            }

            return messageDtoList;
        }

    }
}