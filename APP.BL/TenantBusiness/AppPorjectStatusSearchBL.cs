using System.Collections.Generic;
using System.Data;
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
using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using APP.Framework;
namespace App.BL
{
    public class AppPorjectStatusSearchBL
    {
        public static readonly string SearchParam_UserID = "userId";
        public static readonly string SearchParam_StatusBaseDate = "statusBaseDate";

        public static List<AppProjectWorkFlowTaskExDto> RetrieveUserProjectTaskDtos(int? userId, DateTime? statusBaseDate)
        {

            var entities = new EntityCollection<AppProjectWorkFlowTaskEntity>();

            //PrefetchPath2 path = new PrefetchPath2(EntityType.AppProjectWorkFlowTaskEntity);
            //path.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectOrWorkFlow);

            using (DataAccessAdapter adapater = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket();
                filter.Relations.Add(AppProjectWorkFlowTaskEntity.Relations.AppProjectOrWorkFlowEntityUsingProjectId);
                filter.PredicateExpression.Add(AppProjectOrWorkFlowFields.ProjectWorkflowType == (int)EmAppProjectWorkflowType.Project);

                if (userId.HasValue)
                {
                    filter.PredicateExpression.Add(AppProjectWorkFlowTaskFields.TaskOwnerId == userId.Value);
                }

                //adapater.FetchEntityCollection(entities, filter, path);
                adapater.FetchEntityCollection(entities, filter);

                List<AppProjectWorkFlowTaskExDto> taskDtoList = new List<AppProjectWorkFlowTaskExDto>();
                foreach (var folderEntity in entities)
                {
                    taskDtoList.Add(AppProjectWorkFlowTaskConverter.ConvertEntityToExDto(folderEntity));
                }


                if (!statusBaseDate.HasValue)
                {
                    statusBaseDate = DateTime.UtcNow;
                }

                foreach (var aTask in taskDtoList)
                {
                    if (aTask.DatePlannedStart.HasValue && aTask.DatePlannedEnd.HasValue)
                    {
                        if (aTask.DateActualEnd.HasValue)
                        {
                            aTask.EmAppProjectTaskStage = EmAppProjectTaskStage.Completed;
                            aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.Completed;
                        }
                        else if (aTask.DateActualStart.HasValue && !aTask.DateActualEnd.HasValue)
                        {
                            aTask.EmAppProjectTaskStage = EmAppProjectTaskStage.Started;

                            if (aTask.DatePlannedStart.Value > statusBaseDate.Value)
                            {
                                aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.OnSchedule;
                            }
                            else
                            {
                                double completePercentagePlanned = 1.0 * (statusBaseDate.Value.Ticks - aTask.DatePlannedStart.Value.Ticks)
                                    / (aTask.DatePlannedEnd.Value.Ticks - aTask.DatePlannedStart.Value.Ticks) * 100;

                                double completePercentageActual = aTask.CompletedPercent.HasValue ? aTask.CompletedPercent.Value : 0;

                                if (completePercentageActual >= completePercentagePlanned)
                                {
                                    aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.OnSchedule;
                                }
                                else
                                {
                                    aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.Late;
                                }

                            }
                        }
                        else
                        {

                            aTask.EmAppProjectTaskStage = EmAppProjectTaskStage.NotStarted;

                            if (aTask.DatePlannedStart.Value >= statusBaseDate.Value)
                            {
                                aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.OnSchedule;
                            }
                            else if (aTask.DatePlannedStart.Value < statusBaseDate.Value && aTask.DatePlannedEnd.Value > statusBaseDate.Value)
                            {
                                aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.AtRisk;
                            }
                            else if (aTask.DatePlannedEnd.Value <= statusBaseDate.Value)
                            {
                                aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.Late;
                            }
                        }
                    }
                    else
                    {
                        aTask.EmAppProjectTaskStage = EmAppProjectTaskStage.NotStarted;
                        aTask.ProjectActivityStatusId = (int)EmAppProjectTaskStatus.NotAvailable;
                    }

                }

                return taskDtoList;

            }
        }

        public static List<StaticSearchResultRowJsonDto> RetrieveUserProjectTasks(dynamic searchDtoObj)
        {
            string sealizeString = JsonConvert.SerializeObject(searchDtoObj);
            SearchDto searchDto = JsonConvert.DeserializeObject<SearchDto>(sealizeString);


            if (searchDto != null)
            {
                DateTime? statusBaseDate = null;

                var userIdCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath == SearchParam_UserID);
                var statusBaseDateCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath == SearchParam_StatusBaseDate);

                int? userId = ControlTypeValueConverter.ConvertValueToInt(userIdCriteria.Value);

                if (!userId.HasValue)
                {
                    userId = searchDto.CurrentUserId;
                }

                if (userId.HasValue)
                {

                    if (statusBaseDateCriteria != null)
                    {
                        statusBaseDate = ControlTypeValueConverter.ConvertValueToDate(statusBaseDateCriteria.Value);
                        statusBaseDate = statusBaseDate.HasValue ? statusBaseDate : DateTime.UtcNow;
                    }

                    List<AppProjectWorkFlowTaskExDto> taskList = RetrieveUserProjectTaskDtos(userId, statusBaseDate);

                    if (searchDto.ReferenceViewDefinitionDto != null)
                    {

                        List<StaticSearchResultRowJsonDto> result = RetrieveUserProjectTasks_PrepareSearchResultFromDto((int)searchDto.ReferenceViewDefinitionDto.Id, taskList);

                        return result;
                    }

                }

            }

            return null;
        }

        public static List<StaticSearchResultRowJsonDto> RetrieveUserProjectTaskCountByStatus(dynamic searchDtoObj)
        {
            string sealizeString = JsonConvert.SerializeObject(searchDtoObj);
            SearchDto searchDto = JsonConvert.DeserializeObject<SearchDto>(sealizeString);


            if (searchDto != null)
            {
                DateTime? statusBaseDate = null;

                var userIdCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath == SearchParam_UserID);
                var statusBaseDateCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath == SearchParam_StatusBaseDate);

                int? userId = ControlTypeValueConverter.ConvertValueToInt(userIdCriteria.Value);

                if (!userId.HasValue)
                {
                    userId = searchDto.CurrentUserId;
                }

                if (userId.HasValue)
                {

                    if (statusBaseDateCriteria != null)
                    {
                        statusBaseDate = ControlTypeValueConverter.ConvertValueToDate(statusBaseDateCriteria.Value);
                        statusBaseDate = statusBaseDate.HasValue ? statusBaseDate : DateTime.UtcNow;
                    }

                    List<AppProjectWorkFlowTaskExDto> taskList = RetrieveUserProjectTaskDtos(userId, statusBaseDate);



                    if (searchDto.ReferenceViewDefinitionDto != null)
                    {

                        List<StaticSearchResultRowJsonDto> result = RetrieveUserProjectTaskCountByStatus_PrepareSearchResultFromDto((int)searchDto.ReferenceViewDefinitionDto.Id, taskList);

                        return result;
                    }

                }

            }

            return null;
        }

        public static List<StaticSearchResultRowJsonDto> RetrieveUserProjectTaskCountByStage(dynamic searchDtoObj)
        {
            string sealizeString = JsonConvert.SerializeObject(searchDtoObj);
            SearchDto searchDto = JsonConvert.DeserializeObject<SearchDto>(sealizeString);


            if (searchDto != null)
            {
                DateTime? statusBaseDate = null;

                var userIdCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath == SearchParam_UserID);
                var statusBaseDateCriteria = searchDto.Criterias.FirstOrDefault(o => o.SysTableFiledPath == SearchParam_StatusBaseDate);

                int? userId = ControlTypeValueConverter.ConvertValueToInt(userIdCriteria.Value);

                if (!userId.HasValue)
                {
                    userId = searchDto.CurrentUserId;
                }

                if (userId.HasValue)
                {

                    if (statusBaseDateCriteria != null)
                    {
                        statusBaseDate = ControlTypeValueConverter.ConvertValueToDate(statusBaseDateCriteria.Value);
                        statusBaseDate = statusBaseDate.HasValue ? statusBaseDate : DateTime.UtcNow;
                    }

                    List<AppProjectWorkFlowTaskExDto> taskList = RetrieveUserProjectTaskDtos(userId, statusBaseDate);



                    if (searchDto.ReferenceViewDefinitionDto != null)
                    {

                        List<StaticSearchResultRowJsonDto> result = RetrieveUserProjectTaskCountByStage_PrepareSearchResultFromDto((int)searchDto.ReferenceViewDefinitionDto.Id, taskList);

                        return result;
                    }

                }

            }

            return null;
        }



        private static List<StaticSearchResultRowJsonDto> RetrieveUserProjectTasks_PrepareSearchResultFromDto(int? viewId, List<AppProjectWorkFlowTaskExDto> taskList)
        {
            List<StaticSearchResultRowJsonDto> searchResult = new List<StaticSearchResultRowJsonDto>();

            if (taskList != null)
            {
                if (viewId.HasValue)
                {
                    var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(viewId.Value);
                    if (searchViewDto != null && searchViewDto.AppSearchViewFieldList != null)
                    {

                        var viewField_TaskId = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.IdProperty);
                        var viewField_Name = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.NameProperty);
                        var viewField_Description = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.DescriptionProperty);
                        var viewField_ProjectId = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.ProjectIdProperty);
                        var viewField_DatePlannedStart = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.DatePlannedStartProperty);
                        var viewField_DatePlannedEnd = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.DatePlannedEndProperty);
                        var viewField_DateActualStart = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.DateActualStartProperty);
                        var viewField_DateActualEnd = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.DateActualEndProperty);
                        var viewField_AmountOfTime = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.AmountOfTimeProperty);
                        var viewField_CompletedPercent = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.CompletedPercentProperty);
                        var viewField_TaskOwnerId = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.TaskOwnerIdProperty);
                        var viewField_ProjectActivityStatusId = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.ProjectActivityStatusIdProperty);
                        var viewField_TransactionId = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.TransactionIdProperty);
                        var viewField_TransactionRid = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.TransactionRidProperty);
                        var viewField_EmAppProjectTaskStage = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == "EmAppProjectTaskStage");


                        foreach (AppProjectWorkFlowTaskExDto aTask in taskList)
                        {
                            StaticSearchResultRowJsonDto aRow = new StaticSearchResultRowJsonDto();
                            aRow.DictViewColumnIDKeyValue = new Dictionary<int, object>();

                            if (viewField_TaskId != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_TaskId.Id] = aTask.Id;
                            }

                            if (viewField_Name != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_Name.Id] = aTask.Name;
                            }

                            if (viewField_Description != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_Description.Id] = aTask.Description;
                            }

                            if (viewField_ProjectId != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_ProjectId.Id] = aTask.ProjectId;
                            }

                            if (viewField_DatePlannedStart != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_DatePlannedStart.Id] = aTask.DatePlannedStart;
                            }

                            if (viewField_DatePlannedEnd != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_DatePlannedEnd.Id] = aTask.DatePlannedEnd;
                            }

                            if (viewField_DateActualStart != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_DateActualStart.Id] = aTask.DateActualStart;
                            }

                            if (viewField_DateActualEnd != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_DateActualEnd.Id] = aTask.DateActualEnd;
                            }

                            if (viewField_AmountOfTime != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_AmountOfTime.Id] = aTask.AmountOfTime;
                            }

                            if (viewField_CompletedPercent != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_CompletedPercent.Id] = aTask.CompletedPercent;
                            }

                            if (viewField_TaskOwnerId != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_TaskOwnerId.Id] = aTask.TaskOwnerId;
                            }

                            if (viewField_ProjectActivityStatusId != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_ProjectActivityStatusId.Id] = aTask.ProjectActivityStatusId;
                            }

                            if (viewField_TransactionId != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_TransactionId.Id] = aTask.TransactionId;
                            }

                            if (viewField_TransactionRid != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_TransactionRid.Id] = aTask.TransactionRid;
                            }

                            if (viewField_EmAppProjectTaskStage != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_EmAppProjectTaskStage.Id] = aTask.EmAppProjectTaskStage;
                            }


                            searchResult.Add(aRow);

                        }

                    }
                }
            }
            return searchResult;
        }

        private static List<StaticSearchResultRowJsonDto> RetrieveUserProjectTaskCountByStatus_PrepareSearchResultFromDto(int? viewId, List<AppProjectWorkFlowTaskExDto> taskList)
        {

            List<StaticSearchResultRowJsonDto> searchResult = new List<StaticSearchResultRowJsonDto>();

            if (taskList != null)
            {
                if (viewId.HasValue)
                {
                    Dictionary<string, int> dictTaskStatusAndCount = taskList.Where(o => o.ProjectActivityStatusId.HasValue).GroupBy(o => o.ProjectActivityStatusId.Value).ToDictionary(g => ((EmAppProjectTaskStatus)g.Key).ToString(), g => g.Count());


                    var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(viewId.Value);
                    if (searchViewDto != null && searchViewDto.AppSearchViewFieldList != null)
                    {
                        var viewField_ProjectActivityStatusId = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == AppProjectWorkFlowTaskDto.ProjectActivityStatusIdProperty);
                        var viewField_TaskCount = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == "TaskCount");

                        foreach (string key in dictTaskStatusAndCount.Keys)
                        {
                            StaticSearchResultRowJsonDto aRow = new StaticSearchResultRowJsonDto();
                            aRow.DictViewColumnIDKeyValue = new Dictionary<int, object>();

                            if (viewField_ProjectActivityStatusId != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_ProjectActivityStatusId.Id] = key;
                            }

                            if (viewField_TaskCount != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_TaskCount.Id] = dictTaskStatusAndCount[key];
                            }

                            searchResult.Add(aRow);

                        }

                    }
                }
            }
            return searchResult;
        }

        private static List<StaticSearchResultRowJsonDto> RetrieveUserProjectTaskCountByStage_PrepareSearchResultFromDto(int? viewId, List<AppProjectWorkFlowTaskExDto> taskList)
        {
            List<StaticSearchResultRowJsonDto> searchResult = new List<StaticSearchResultRowJsonDto>();

            if (taskList != null)
            {
                if (viewId.HasValue)
                {
                    Dictionary<string, int> dictTaskStageAndCount = taskList.Where(o => o.EmAppProjectTaskStage.HasValue).GroupBy(o => o.EmAppProjectTaskStage.Value).ToDictionary(g => g.Key.ToString(), g => g.Count());

                    var searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(viewId.Value);
                    if (searchViewDto != null && searchViewDto.AppSearchViewFieldList != null)
                    {
                        var viewField_EmAppProjectTaskStage = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == "EmAppProjectTaskStage");
                        var viewField_TaskCount = searchViewDto.AppSearchViewFieldList.FirstOrDefault(o => o.SysTableFiledPath == "TaskCount");

                        foreach (string key in dictTaskStageAndCount.Keys)
                        {
                            StaticSearchResultRowJsonDto aRow = new StaticSearchResultRowJsonDto();
                            aRow.DictViewColumnIDKeyValue = new Dictionary<int, object>();

                            if (viewField_EmAppProjectTaskStage != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_EmAppProjectTaskStage.Id] = key;
                            }

                            if (viewField_TaskCount != null)
                            {
                                aRow.DictViewColumnIDKeyValue[(int)viewField_TaskCount.Id] = dictTaskStageAndCount[key];
                            }

                            searchResult.Add(aRow);

                        }

                    }
                }
            }
            return searchResult;
        }

    }

}


