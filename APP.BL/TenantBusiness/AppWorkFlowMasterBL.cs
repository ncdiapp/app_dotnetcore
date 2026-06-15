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
using System.Data.SqlClient;
using System.Text.RegularExpressions;

using APP.Framework;
namespace App.BL
{
    public static class AppWorkFlowMasterBL
    {

        // Get project or workflow Folder Hierarchy
        public static List<AppProjectOrWorkFlowDto> RetrieveAllMasterWorkFlows(bool? isPredefined = null)
        {
            List<AppProjectOrWorkFlowDto> allMasterWorkFlowList = new List<AppProjectOrWorkFlowDto>();
            EntityCollection<AppProjectOrWorkFlowEntity> entities = new EntityCollection<AppProjectOrWorkFlowEntity>();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                //RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.IsPredefined == false & AppProjectOrWorkFlowFields.ProjectWorkflowType == projectWorkflowType);
                RelationPredicateBucket filter = new RelationPredicateBucket(AppProjectOrWorkFlowFields.ProjectWorkflowType == EmAppProjectWorkflowType.BusinessProcessWorkflow);


                filter.PredicateExpression.AddWithAnd(AppProjectOrWorkFlowFields.ParentProjectId == DBNull.Value);

                if (isPredefined.HasValue)
                {
                    filter.PredicateExpression.AddWithAnd(AppProjectOrWorkFlowFields.IsPredefined == isPredefined.Value);
                }

                adapter.FetchEntityCollection(entities, filter);
            }

            foreach (AppProjectOrWorkFlowEntity entity in entities)
            {
                var projectDto = AppProjectOrWorkFlowConverter.ConvertEntityToDto(entity);



                allMasterWorkFlowList.Add(projectDto);
            }
            return allMasterWorkFlowList.OrderBy(o=>o.Name).ToList();
        }


        public static AppProjectOrWorkFlowEntity RetrieveOneWorkflowEntityWithAllChildWorkflowList(object workflowId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppProjectOrWorkFlowEntity appProjectOrWorkFlowEntity = new AppProjectOrWorkFlowEntity(int.Parse(workflowId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppProjectOrWorkFlowEntity);
                rootPath.Add(AppProjectOrWorkFlowEntity.PrefetchPathAppProjectOrWorkFlow_);


                adpater.FetchEntity(appProjectOrWorkFlowEntity, rootPath);
                return appProjectOrWorkFlowEntity;
            }
        }


        public static AppProjectOrWorkFlowExDto RetrieveOneWorkflowExDtoWithAllChildWorkflowList(object workflowId)
        {
            AppProjectOrWorkFlowEntity appProjectOrWorkFlowEntity = RetrieveOneWorkflowEntityWithAllChildWorkflowList(workflowId);

            AppProjectOrWorkFlowExDto aAppProjectOrWorkFlowExDto = AppProjectOrWorkFlowConverter.ConvertEntityToExDto(appProjectOrWorkFlowEntity);

            foreach (var childWorkflowEntity in appProjectOrWorkFlowEntity.AppProjectOrWorkFlow_)
            {
                AppProjectOrWorkFlowExDto childWorkflowExDto = AppProjectOrWorkFlowConverter.ConvertEntityToExDto(childWorkflowEntity);
                aAppProjectOrWorkFlowExDto.AppProjectOrWorkFlow_List.Add(childWorkflowExDto);
            }            

            return aAppProjectOrWorkFlowExDto;
        }

        public static OperationCallResult<AppProjectOrWorkFlowExDto> SaveOneWorkflowAllChildWorkflowList(AppProjectOrWorkFlowExDto masterWorkflowExDto)
        {
            OperationCallResult<AppProjectOrWorkFlowExDto> operationCallResult = new OperationCallResult<AppProjectOrWorkFlowExDto>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            if (masterWorkflowExDto != null && masterWorkflowExDto.Id != null)
            {
                AppProjectOrWorkFlowEntity appProjectOrWorkFlowEntity = RetrieveOneWorkflowEntityWithAllChildWorkflowList(int.Parse(masterWorkflowExDto.Id.ToString()));

                appProjectOrWorkFlowEntity.Name = masterWorkflowExDto.Name;
                appProjectOrWorkFlowEntity.Description = masterWorkflowExDto.Description;
                appProjectOrWorkFlowEntity.TransactionId = masterWorkflowExDto.TransactionId;
                appProjectOrWorkFlowEntity.ParentProjectId = masterWorkflowExDto.ParentProjectId;              
              



                Dictionary<int, AppProjectOrWorkFlowEntity> dictDbChildWorkFlowEntity = appProjectOrWorkFlowEntity.AppProjectOrWorkFlow_.Where(o => !o.IsNew).ToDictionary(o => o.ProjectId, o => o);
                Dictionary<int, AppProjectOrWorkFlowExDto> dictChildWorkFlowExdto = masterWorkflowExDto.AppProjectOrWorkFlow_List.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

                List<int> childWorkflowIdDbms = dictDbChildWorkFlowEntity.Keys.ToList();
                List<int> childWorkflowIdUi = dictChildWorkFlowExdto.Keys.ToList();


                //Delete Id
                List<int> deletChildWorkflowIds = childWorkflowIdDbms.Except(childWorkflowIdUi).ToList();

                //new Entity
                masterWorkflowExDto.AppProjectOrWorkFlow_List.Where(o => o.IsNew).ForAll(o =>
                {
                    AppProjectOrWorkFlowEntity childWorkFlowEntity = new AppProjectOrWorkFlowEntity();
                    AppProjectOrWorkFlowConverter.CopyDtoToEntity(childWorkFlowEntity, o);
                    appProjectOrWorkFlowEntity.AppProjectOrWorkFlow_.Add(childWorkFlowEntity);
                });


                //dirty
                List<int> dirtyChildWorkflowIds = childWorkflowIdDbms.Intersect(childWorkflowIdUi).ToList();

                foreach (int childWorkflowId in dirtyChildWorkflowIds)
                {
                    AppProjectOrWorkFlowEntity childWorkflowEntity = dictDbChildWorkFlowEntity[childWorkflowId];
                    AppProjectOrWorkFlowExDto childWorkflowExdto = dictChildWorkFlowExdto[childWorkflowId];
                    //AppProjectOrWorkFlowConverter.CopyDtoToEntity(childWorkflowEntity, childWorkflowExdto);

                    childWorkflowEntity.Name = childWorkflowExdto.Name;
                    childWorkflowEntity.Description = childWorkflowExdto.Description;
                    childWorkflowEntity.TransactionId = childWorkflowExdto.TransactionId;
                    childWorkflowEntity.ParentProjectId = childWorkflowExdto.ParentProjectId;
                    childWorkflowEntity.ProjectDirectionId = childWorkflowExdto.ProjectDirectionId;
                }


                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(appProjectOrWorkFlowEntity);

                        adapter.Commit();
                        validationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ChildWorkflow_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }

                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        validationResult.Items.Add(new ValidationItem(typeof(AppProjectTeamMemberExDto), "App_ChildWorkflow_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }

                if (deletChildWorkflowIds != null && deletChildWorkflowIds.Count > 0)
                {
                    foreach (var deleteId in deletChildWorkflowIds)
                    {
                        var deleteResult = AppProjectWorkFlowStructureBL.DeleteProjectWorkFlow(deleteId as int?);
                        if (!deleteResult.IsSuccessful)
                        {
                            validationResult.Merge(deleteResult.ValidationResult);
                        }
                    }
                }

               


            }

            return operationCallResult;
        }



        public static OperationCallResult<bool> UpdateWorkflowParent(int workflowId, int? parentId) {

            OperationCallResult<bool> operationCallResult = new OperationCallResult<bool>();
            ValidationResult validationResult = new ValidationResult();
            operationCallResult.ValidationResult = validationResult;

            // To Do: Need to check Task complete dependency. Give warning message if the task is not allowed to complete now.

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    AppProjectOrWorkFlowEntity updateEntity = new AppProjectOrWorkFlowEntity();
                    updateEntity.ParentProjectId = parentId;

                    adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppProjectOrWorkFlowFields.ProjectId == workflowId));
                    adapter.Commit();
                    operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowEntity), "App_AppProjectOrWorkFlowEntity_Update_OK", ValidationItemType.Message, "Task Update Successfully"));
                    operationCallResult.Object = true;
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    operationCallResult.ValidationResult.Items.Add(new ValidationItem(typeof(AppProjectOrWorkFlowEntity), "App_AppProjectOrWorkFlowEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return operationCallResult;
        }


    }


}


