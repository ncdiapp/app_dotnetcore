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

using APP.Framework;
namespace App.BL
{
    public static class AppProjectPerspectiveViewBL
    {
        //public static readonly string App_ProjectPerspectiveViewEntity_Save_OK = "App_ProjectPerspectiveViewEntity_Save_OK";
        //public static readonly string App_ProjectPerspectiveViewEntity_Save_Failed = "App_ProjectPerspectiveViewEntity_Save_Failed";
        //public static readonly string App_ProjectPerspectiveViewEntityUILayout_Save_OK = "App_ProjectPerspectiveViewEntityUILayout_Save_OK";
        //public static readonly string App_ProjectPerspectiveViewEntityUILayout_Save_Failed = "App_ProjectPerspectiveViewEntityUILayout_Save_Failed";
        //public static readonly string App_ProjectPerspectiveViewEntity_Delete_Ok = "App_ProjectPerspectiveViewEntity_Delete_Ok";
        //public static readonly string App_ProjectPerspectiveViewEntity_Delete_Failed = "App_ProjectPerspectiveViewEntity_Delete_Failed";


        public static UserTaskKanbanDto RetrieveCurrentUserTaskKanbanDto(TaskFilterOptionDto filterOption)
        {
            int userId = AppSecurityUserBL.CurrentUserId;

            UserTaskKanbanDto kanbanDto = new UserTaskKanbanDto();                       

            kanbanDto.KanbanColumnList = RetrieveAllAppProjectPerspectiveViewExDtoByUserId(userId);
            kanbanDto.KanbanAvailableTaskList = new List<AppProjectPerspectiveTaskExDto>();
            kanbanDto.KanbanSelectedTaskList = RetrieveAllAppProjectPerspectiveTaskExDtoByUserId(userId);

            List<int> selectedTaskIds = kanbanDto.KanbanSelectedTaskList
                //.Where(o=>o.Id != null && o.ProjectWorkFlowTaskId.HasValue && o.PerspectiveSectionId.HasValue)
                .Select(o => o.ProjectWorkFlowTaskId.Value).ToList();

           
            
            var allUserTasks = AppUserTaskListBL.RetrieveUserTaskList(filterOption, true).OrderBy(o=>o.EmAppTaskSystemDefinedCategory).ThenBy(o=>o.Name);

            foreach (var aTaskExDto in allUserTasks)
            {
                int? projectTaskId = ControlTypeValueConverter.ConvertValueToInt(aTaskExDto.Id);

                if (projectTaskId.HasValue)
                {
                    if (selectedTaskIds.Contains(projectTaskId.Value))
                    {
                        AppProjectPerspectiveTaskExDto kanbanTask = kanbanDto.KanbanSelectedTaskList
                            .FirstOrDefault(o => o.ProjectWorkFlowTaskId.HasValue && o.ProjectWorkFlowTaskId.Value == projectTaskId.Value);

                        if (kanbanTask != null)
                        {
                            kanbanTask.ForeignAppProjectWorkFlowTaskExDto = aTaskExDto;
                        }
                    }
                    else
                    {
                        AppProjectPerspectiveTaskExDto kanbanTask = new AppProjectPerspectiveTaskExDto();
                        kanbanTask.ForeignAppProjectWorkFlowTaskExDto = aTaskExDto;

                        kanbanDto.KanbanAvailableTaskList.Add(kanbanTask);
                    }
                }
               
            }

            return kanbanDto;
        }
        
        public static OperationCallResult<bool> SaveCurrentUserTaskKanbanDto(UserTaskKanbanDto kanbanDto)
        {
            int userId = AppSecurityUserBL.CurrentUserId;

            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            SaveCurrentUserTaskKanbanDto_ProcessKanbanColumns(kanbanDto, userId, validationResult);
            SaveCurrentUserTaskKanbanDto_ProcessKanbanTasks(kanbanDto, userId, validationResult);

            if (!validationResult.HasErrors)
            {
                validationResult.Items.Clear();
                validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskCheckListExDto), "App_UserTaskKanban_Save_OK", ValidationItemType.Message, "Saved Successfully"));

                aOperationCallResult.Object = true;
            }

            return aOperationCallResult;
        }


        private static void SaveCurrentUserTaskKanbanDto_ProcessKanbanColumns(UserTaskKanbanDto kanbanDto, int userId, ValidationResult validationResult)
        {
            var perspectiveViewEntities = RetrieveAllAppProjectPerspectiveViewEntitiesUserId(userId);

            List<int> perspectiveViewIdDbms = perspectiveViewEntities.Select(o => o.PerspectiveSectionId).ToList();
            List<int> perspectiveViewIdUi = kanbanDto.KanbanColumnList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();
            List<int> deletePerspectiveViewIDs = perspectiveViewIdDbms.Except(perspectiveViewIdUi).ToList();
            Dictionary<int, AppProjectPerspectiveViewEntity> dictDbPerspectiveViewEntity = perspectiveViewEntities.ToDictionary(o => o.PerspectiveSectionId, o => o);

            foreach (AppProjectPerspectiveViewExDto perspectiveViewExDto in kanbanDto.KanbanColumnList)
            {
                if (perspectiveViewExDto.IsNew)
                {
                    perspectiveViewExDto.DisplayOrder = 1;

                    var orderedColumns = kanbanDto.KanbanColumnList.Where(o => o.DisplayOrder.HasValue);

                    if (orderedColumns.Count() > 0)
                    {
                        perspectiveViewExDto.DisplayOrder = orderedColumns.Max(o => o.DisplayOrder.Value) + 1;
                    }                   

                    AppProjectPerspectiveViewEntity entity = new AppProjectPerspectiveViewEntity();
                    AppProjectPerspectiveViewConverter.CopyDtoToEntity(entity, perspectiveViewExDto);
                    perspectiveViewEntities.Add(entity);
                }
                else // update 
                {
                    if (dictDbPerspectiveViewEntity.ContainsKey((int)perspectiveViewExDto.Id))
                    {
                        var entity = dictDbPerspectiveViewEntity[(int)perspectiveViewExDto.Id];
                        AppProjectPerspectiveViewConverter.CopyDtoToEntity(entity, perspectiveViewExDto);
                    }
                }
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntityCollection(perspectiveViewEntities);

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectPerspectiveTaskEntity), new RelationPredicateBucket(AppProjectPerspectiveTaskFields.PerspectiveSectionId == deletePerspectiveViewIDs));
                    adapter.DeleteEntitiesDirectly(typeof(AppProjectPerspectiveViewEntity), new RelationPredicateBucket(AppProjectPerspectiveViewFields.PerspectiveSectionId == deletePerspectiveViewIDs));
                    //}

                    adapter.Commit();
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskCheckListExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }


        private static void SaveCurrentUserTaskKanbanDto_ProcessKanbanTasks(UserTaskKanbanDto kanbanDto, int userId, ValidationResult validationResult)
        {
            var perspectiveTaskEntities = RetrieveAllAppProjectPerspectiveTaskEntitiesByUserId(userId);

            List<int> perspectiveTaskIdDbms = perspectiveTaskEntities.Select(o => o.PerspectiveTaskId).ToList();
            List<int> perspectiveTaskIdUi = kanbanDto.KanbanSelectedTaskList.Where(o => o.Id != null).Select(o => (int)o.Id).ToList();
            List<int> deletePerspectiveTaskIDs = perspectiveTaskIdDbms.Except(perspectiveTaskIdUi).ToList();

            Dictionary<int, AppProjectPerspectiveTaskEntity> dictDbPerspectiveTaskEntity = perspectiveTaskEntities.ToDictionary(o => o.PerspectiveTaskId, o => o);

            foreach (AppProjectPerspectiveTaskExDto perspectiveTaskExDto in kanbanDto.KanbanSelectedTaskList)
            {
                if (perspectiveTaskExDto.IsNew)
                {
                    AppProjectPerspectiveTaskEntity entity = new AppProjectPerspectiveTaskEntity();
                    AppProjectPerspectiveTaskConverter.CopyDtoToEntity(entity, perspectiveTaskExDto);
                    perspectiveTaskEntities.Add(entity);
                }
                else // update 
                {
                    if (dictDbPerspectiveTaskEntity.ContainsKey((int)perspectiveTaskExDto.Id))
                    {
                        var entity = dictDbPerspectiveTaskEntity[(int)perspectiveTaskExDto.Id];
                        AppProjectPerspectiveTaskConverter.CopyDtoToEntity(entity, perspectiveTaskExDto);
                    }
                }
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntityCollection(perspectiveTaskEntities);

                    adapter.DeleteEntitiesDirectly(typeof(AppProjectPerspectiveTaskEntity), new RelationPredicateBucket(AppProjectPerspectiveTaskFields.PerspectiveTaskId == deletePerspectiveTaskIDs));
                    //}

                    adapter.Commit();
                }

                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    validationResult.Items.Add(new ValidationItem(typeof(AppProjectTaskCheckListExDto), "App_ProjectTeamMember_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }
        }

        
        private static ObservableSet<AppProjectPerspectiveViewExDto> RetrieveAllAppProjectPerspectiveViewExDtoByUserId(int? userId = null)
        {
            EntityCollection<AppProjectPerspectiveViewEntity> list = RetrieveAllAppProjectPerspectiveViewEntitiesUserId(userId);

            var aDtoList = new ObservableSet<AppProjectPerspectiveViewExDto>();

            foreach (var o in list)
            {
                aDtoList.Add(AppProjectPerspectiveViewConverter.ConvertEntityToExDto(o));
            }

            return aDtoList;
        }

        private static EntityCollection<AppProjectPerspectiveViewEntity> RetrieveAllAppProjectPerspectiveViewEntitiesUserId(int? userId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectPerspectiveViewEntity> list = new EntityCollection<AppProjectPerspectiveViewEntity>();

                SortExpression expression = new SortExpression(AppProjectPerspectiveViewFields.DisplayOrder | SortOperator.Ascending);

                RelationPredicateBucket filter = new RelationPredicateBucket();
                if (userId.HasValue)
                {
                    filter.PredicateExpression.Add(AppProjectPerspectiveViewFields.AppCreatedById == userId.Value);
                }


                adapter.FetchEntityCollection(list, filter, 0, expression);

                return list;
            }
        }

        private static ObservableSet<AppProjectPerspectiveTaskExDto> RetrieveAllAppProjectPerspectiveTaskExDtoByUserId(int? userId = null)
        {
            EntityCollection<AppProjectPerspectiveTaskEntity> list = RetrieveAllAppProjectPerspectiveTaskEntitiesByUserId(userId);

            var aDtoList = new ObservableSet<AppProjectPerspectiveTaskExDto>();

            foreach (var perspectiveTaskEtntity in list)
            {
                var perspectiveTaskExDto = AppProjectPerspectiveTaskConverter.ConvertEntityToExDto(perspectiveTaskEtntity);

                //var taskEntity = perspectiveTaskEtntity.AppProjectWorkFlowTask;

                //if (taskEntity != null)
                //{
                //    taskEntity.IsConvertDBUtcToClient = true;

                //    perspectiveTaskExDto.ForeignAppProjectWorkFlowTaskExDto = AppProjectWorkFlowTaskConverter.ConvertEntityToExDto(taskEntity);

                //    var resourceDtoList = perspectiveTaskExDto.ForeignAppProjectWorkFlowTaskExDto.AppProjectTaskResourceList;

                //    if (taskEntity.AppProjectTaskResource != null)
                //    {
                //        foreach (var resourceEntity in taskEntity.AppProjectTaskResource)
                //        {
                //            resourceDtoList.Add(AppProjectTaskResourceConverter.ConvertEntityToExDto(resourceEntity));
                //        }
                //    }
                //}

                aDtoList.Add(perspectiveTaskExDto);
            }

            return aDtoList;
        }

        private static EntityCollection<AppProjectPerspectiveTaskEntity> RetrieveAllAppProjectPerspectiveTaskEntitiesByUserId(int? userId)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppProjectPerspectiveTaskEntity> list = new EntityCollection<AppProjectPerspectiveTaskEntity>();

                SortExpression expression = new SortExpression(AppProjectPerspectiveTaskFields.DisplayOrder | SortOperator.Ascending);

                RelationPredicateBucket filter = new RelationPredicateBucket();
                if (userId.HasValue)
                {
                    filter.PredicateExpression.Add(AppProjectPerspectiveTaskFields.AppCreatedById == userId.Value);
                }

                //IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectPerspectiveTaskEntity);
                //rootPath.Add(AppProjectPerspectiveTaskEntity.PrefetchPathAppProjectWorkFlowTask)
                //    .SubPath.Add(AppProjectWorkFlowTaskEntity.PrefetchPathAppProjectTaskResource);

                //adapter.FetchEntityCollection(list, filter, 0, expression, rootPath);

                adapter.FetchEntityCollection(list, filter, 0, expression);

                return list;
            }
        }



        //private static AppProjectPerspectiveViewExDto RetrieveOneAppProjectPerspectiveViewExDto(object PerspectiveSectionId)
        //{
        //    AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity = RetrieveOneAppProjectPerspectiveViewEntity(PerspectiveSectionId);
        //    AppProjectPerspectiveViewExDto aProjectPerspectiveViewDto = AppProjectPerspectiveViewConverter.ConvertEntityToExDto(aAppProjectPerspectiveViewEntity);



        //    foreach (var o in aAppProjectPerspectiveViewEntity.AppProjectPerspectiveTask)
        //    {
        //        AppProjectPerspectiveTaskExDto aAppProjectPerspectiveViewKeyExDto = AppProjectPerspectiveTaskConverter.ConvertEntityToExDto(o);


        //        aProjectPerspectiveViewDto.AppProjectPerspectiveTaskList.Add(aAppProjectPerspectiveViewKeyExDto);
        //    }


        //    return aProjectPerspectiveViewDto;
        //}

        //private static AppProjectPerspectiveViewEntity RetrieveOneAppProjectPerspectiveViewEntity(object PerspectiveSectionId)
        //{
        //    using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        AppProjectPerspectiveViewEntity ProjectPerspectiveViewEntity = new AppProjectPerspectiveViewEntity(int.Parse(PerspectiveSectionId.ToString()));

        //        IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectPerspectiveViewEntity);




        //        rootPath.Add(AppProjectPerspectiveViewEntity.PrefetchPathAppProjectPerspectiveTask);

        //        adpater.FetchEntity(ProjectPerspectiveViewEntity, rootPath);
        //        return ProjectPerspectiveViewEntity;
        //    }
        //}




        //public static OperationCallResult<AppProjectPerspectiveViewExDto> SaveAppProjectPerspectiveViewExDto(AppProjectPerspectiveViewExDto aAppProjectPerspectiveViewExDto)
        //{
        //    OperationCallResult<AppProjectPerspectiveViewExDto> aOperationCallResult = new OperationCallResult<AppProjectPerspectiveViewExDto>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;

        //    AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity;

        //    // prepare Data
        //    if (aAppProjectPerspectiveViewExDto.IsNew)
        //    {
        //        aAppProjectPerspectiveViewEntity = new AppProjectPerspectiveViewEntity();
        //        AppProjectPerspectiveViewConverter.CopyDtoToEntity(aAppProjectPerspectiveViewEntity, aAppProjectPerspectiveViewExDto);



        //        foreach (var templatefieldDto in aAppProjectPerspectiveViewExDto.AppProjectPerspectiveTaskList)
        //        {
        //            AppProjectPerspectiveTaskEntity aAppProjectPerspectiveTaskEntity = new AppProjectPerspectiveTaskEntity();
        //            AppProjectPerspectiveTaskConverter.CopyDtoToEntity(aAppProjectPerspectiveTaskEntity, templatefieldDto);
        //            aAppProjectPerspectiveViewEntity.AppProjectPerspectiveTask.Add(aAppProjectPerspectiveTaskEntity);
        //        }
        //        using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //        {
        //            try
        //            {//PerspectiveSectionId
        //                adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
        //                adapter.SaveEntity(aAppProjectPerspectiveViewEntity);
        //                adapter.Commit();
        //                //PerspectiveSectionId
        //                aAppProjectPerspectiveViewExDto.Id = aAppProjectPerspectiveViewEntity.PerspectiveSectionId;
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPerspectiveViewEntity), "App_ProjectPerspectiveViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
        //            }


        //            // Database FK Exeption ........
        //            catch (ORMQueryExecutionException ex)
        //            {
        //                adapter.Rollback();
        //                aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPerspectiveViewEntity), "App_ProjectPerspectiveViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
        //            }
        //        }
        //    }

        //    else if (aAppProjectPerspectiveViewExDto.IsRelatedEntitiesModified())
        //    {
        //        aValidationResult.Merge(ProcessDirtyAppProjectPerspectiveViewExDto(aAppProjectPerspectiveViewExDto));
        //    }

        //    // if no any errors, refresh all entity from DBMS server
        //    if (!aValidationResult.HasErrors)
        //    {
        //        aOperationCallResult.Object = RetrieveOneAppProjectPerspectiveViewExDto(aAppProjectPerspectiveViewExDto.Id);
        //    }

        //    return aOperationCallResult;
        //}

        //public static OperationCallResult<object> DeleteOneAppProjectPerspectiveView(object PerspectiveSectionId)
        //{
        //    OperationCallResult<object> aOperationCallResult = new OperationCallResult<object>();
        //    ValidationResult aValidationResult = new ValidationResult();
        //    aOperationCallResult.ValidationResult = aValidationResult;


        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        try
        //        {
        //            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
        //            adapter.DeleteEntitiesDirectly(typeof(AppProjectPerspectiveTaskEntity), new RelationPredicateBucket(AppProjectPerspectiveTaskFields.PerspectiveSectionId == PerspectiveSectionId));
        //            adapter.DeleteEntitiesDirectly(typeof(AppProjectPerspectiveViewEntity), new RelationPredicateBucket(AppProjectPerspectiveViewFields.PerspectiveSectionId == PerspectiveSectionId));
        //            string message = StringLocalizer.Localize(App_ProjectPerspectiveViewEntity_Delete_Ok, "ProjectPerspectiveView Delete Successful");
        //            aValidationResult.Items.Add(new ValidationItem(null, App_ProjectPerspectiveViewEntity_Delete_Ok, ValidationItemType.Message, message));

        //            adapter.Commit();
        //        }

        //        // FK Exception .......
        //        catch (ORMQueryExecutionException ex)
        //        {
        //            adapter.Rollback();
        //            string message = StringLocalizer.Localize(App_ProjectPerspectiveViewEntity_Delete_Failed, "ProjectPerspectiveView Delete Failed" + ex.ToString());
        //            aValidationResult.Items.Add(new ValidationItem(null, App_ProjectPerspectiveViewEntity_Delete_Failed, ValidationItemType.Error, message));
        //        }
        //    }

        //    // if no any errors
        //    if (!aOperationCallResult.ValidationResult.HasErrors)
        //    {
        //        aOperationCallResult.Object = PerspectiveSectionId;
        //    }

        //    return aOperationCallResult;
        //}

        //private static ValidationResult ProcessDirtyAppProjectPerspectiveViewExDto(AppProjectPerspectiveViewExDto aAppProjectPerspectiveViewExDto)
        //{
        //    ValidationResult aValidationResult = new ValidationResult();

        //    int[] dirtyFieldIds = aAppProjectPerspectiveViewExDto.AppProjectPerspectiveTaskList.FindModifiedItems().Select(o => o.Id).Cast<int>().ToArray();

        //    AppProjectPerspectiveViewEntity aAppProjectPerspectiveViewEntity = RetrieveOneAppProjectPerspectiveViewEntity(aAppProjectPerspectiveViewExDto.Id);

        //    //PerspectiveTaskId  PerspectiveTaskID
        //    Dictionary<int, AppProjectPerspectiveTaskEntity> dictAppProjectPerspectiveTaskFromDbms = aAppProjectPerspectiveViewEntity.AppProjectPerspectiveTask.ToDictionary(o => o.PerspectiveTaskId, o => o);

        //    AppProjectPerspectiveViewConverter.CopyDtoToEntity(aAppProjectPerspectiveViewEntity, aAppProjectPerspectiveViewExDto);
        //    //  aAppProjectPerspectiveViewEntity.ModifiedDate = System.DateTime.UtcNow;
        //    //  aAppProjectPerspectiveViewEntity.ModifiedBy = (int)ServerContext.Instance.CurrentUid;

        //    //------- check  AppProjectPerspectiveTask

        //    // new Items
        //    foreach (var aChildDto in aAppProjectPerspectiveViewExDto.AppProjectPerspectiveTaskList.FindNewItems())
        //    {
        //        AppProjectPerspectiveTaskEntity aNewChildEntity = new AppProjectPerspectiveTaskEntity();
        //        AppProjectPerspectiveTaskConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
        //        aAppProjectPerspectiveViewEntity.AppProjectPerspectiveTask.Add(aNewChildEntity);
        //    }

        //    // Dirty items
        //    foreach (var modifyitem in aAppProjectPerspectiveViewExDto.AppProjectPerspectiveTaskList.FindModifiedItems())
        //    {
        //        int dtoKey = int.Parse(modifyitem.Id.ToString());
        //        if (dictAppProjectPerspectiveTaskFromDbms.ContainsKey(dtoKey))
        //        {
        //            AppProjectPerspectiveTaskConverter.CopyDtoToEntity(dictAppProjectPerspectiveTaskFromDbms[dtoKey], modifyitem);
        //        }
        //    }

        //    // deletedIDs
        //    int[] deleteFieldIDs = aAppProjectPerspectiveViewExDto.AppProjectPerspectiveTaskList.FindDeletedItemIds().Cast<int>().ToArray();

        //    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
        //    {
        //        try
        //        {
        //            adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
        //            adapter.SaveEntity(aAppProjectPerspectiveViewEntity);

        //            // Need to delete AppProjectPerspectiveTaskFields
        //            if (deleteFieldIDs.Count() > 0)
        //            {
        //                adapter.DeleteEntitiesDirectly(typeof(AppProjectPerspectiveTaskEntity), new RelationPredicateBucket(AppProjectPerspectiveTaskFields.PerspectiveTaskId == deleteFieldIDs));
        //            }

        //            adapter.Commit();
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPerspectiveViewEntity), "App_ProjectPerspectiveViewEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
        //        }


        //        // Database FK Exception .......
        //        catch (ORMQueryExecutionException ex)
        //        {
        //            adapter.Rollback();
        //            aValidationResult.Items.Add(new ValidationItem(typeof(AppProjectPerspectiveViewEntity), "App_ProjectPerspectiveViewEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
        //        }
        //    }

        //    return aValidationResult;
        //}


    }
}