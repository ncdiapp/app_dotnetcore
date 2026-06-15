using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using APP.Framework;
namespace App.BL
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks></remarks>
    public static class AppBusinessNavigationBL
    {
        public static AppBusienssAssormentNavigationExDto RetrieveOneAppBusienssNavigationExDto(int? navigationId)
        {
            var listnavigation = RetrieveAllAppBusienssAssormentNavigationDto();

            AppBusienssAssormentNavigationExDto toReturn = new AppBusienssAssormentNavigationExDto();
            if (listnavigation.Count > 0)
            {
                if (!navigationId.HasValue)
                {
                    navigationId = listnavigation[0].Id as int?;
                }

                AppBusienssAssormentNavigationExDto aAppBusienssAssormentNavigationExDto = GetOnenavitationExDto(navigationId);

                aAppBusienssAssormentNavigationExDto.AppBusienssAssormentNavigationList = listnavigation;

                toReturn = aAppBusienssAssormentNavigationExDto;
            }
            return toReturn;
        }

        private static AppBusienssAssormentNavigationExDto GetOnenavitationExDto(int? navigationId)
        {
            AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity = RetrieveOneAppBusienssAssormentNavigationEntity(navigationId);

            AppBusienssAssormentNavigationExDto aAppBusienssAssormentNavigationExDto = AppBusienssAssormentNavigationConverter.ConvertEntityToExDto(aAppBusienssAssormentNavigationEntity);

            foreach (AppTransactionGroupEntity appTransactionGroupEntity in aAppBusienssAssormentNavigationEntity.AppTransactionGroup.OrderBy(o => o.GroupSortOrder))
            {
                AppTransactionGroupExDto aAppTransactionGroupExDto = AppTransactionGroupConverter.ConvertEntityToExDto(appTransactionGroupEntity);
                aAppBusienssAssormentNavigationExDto.AppTransactionGroupList.Add(aAppTransactionGroupExDto);

                foreach (AppTransactionGroupItemEntity appTransactionGroupItem in appTransactionGroupEntity.AppTransactionGroupItem.OrderBy(o => o.TransactionLayoutOrder))
                {
                    AppTransactionGroupItemExDto aAppTransactionGroupItemExDto = AppTransactionGroupItemConverter.ConvertEntityToExDto(appTransactionGroupItem);
                    aAppTransactionGroupExDto.AppTransactionGroupItemList.Add(aAppTransactionGroupItemExDto);
                }
            }

            return aAppBusienssAssormentNavigationExDto;
        }

        public static List<AppBusienssAssormentNavigationDto> RetrieveAllAppBusienssAssormentNavigationDto()
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                EntityCollection<AppBusienssAssormentNavigationEntity> list = new EntityCollection<AppBusienssAssormentNavigationEntity>();

                adapter.FetchEntityCollection(list, null);

                var aDtoList = new List<AppBusienssAssormentNavigationDto>();
                foreach (var o in list)
                {
                    AppBusienssAssormentNavigationDto group = AppBusienssAssormentNavigationConverter.ConvertEntityToDto(o);
                    aDtoList.Add(group);
                }

                return aDtoList;
            }
        }

        public static OperationCallResult<AppBusienssAssormentNavigationExDto> SaveAppBusienssAssormentNavigationExDto(AppBusienssAssormentNavigationExDto aAppBusienssAssormentNavigationExDto)
        {
            OperationCallResult<AppBusienssAssormentNavigationExDto> aOperationCallResult = new OperationCallResult<AppBusienssAssormentNavigationExDto>();
            ValidationResult aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity;

            // prepare Data
            if (aAppBusienssAssormentNavigationExDto.IsNew)
            {
                aAppBusienssAssormentNavigationEntity = new AppBusienssAssormentNavigationEntity();
                AppBusienssAssormentNavigationConverter.CopyDtoToEntity(aAppBusienssAssormentNavigationEntity, aAppBusienssAssormentNavigationExDto);
                foreach (var appTransactionGroupExDto in aAppBusienssAssormentNavigationExDto.AppTransactionGroupList)
                {
                    ProcessNewGroupExDto(aAppBusienssAssormentNavigationEntity, appTransactionGroupExDto);
                }

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    try
                    {
                        adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                        adapter.SaveEntity(aAppBusienssAssormentNavigationEntity);

                        adapter.Commit();

                        aAppBusienssAssormentNavigationExDto.Id = aAppBusienssAssormentNavigationEntity.AssotmentnavigationId;
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppBusienssAssormentNavigationExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                    }
                    catch (ORMQueryExecutionException ex)
                    {
                        adapter.Rollback();
                        aValidationResult.Items.Add(new ValidationItem(typeof(AppBusienssAssormentNavigationExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                    }
                }
            }
            else if (aAppBusienssAssormentNavigationExDto.IsRelatedEntitiesModified())
            {
                aValidationResult.Merge(ProcessDirtyAppBusienssAssormentNavigationExDto(aAppBusienssAssormentNavigationExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!aValidationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppBusienssNavigationExDto(aAppBusienssAssormentNavigationExDto.Id as int?);
            }

            return aOperationCallResult;
        }

        private static void ProcessNewGroupExDto(AppBusienssAssormentNavigationEntity aAppBusienssAssormentNavigationEntity, AppTransactionGroupExDto appTransactionGroupExDto)
        {
            AppTransactionGroupEntity aAppTransactionGroupEntity = new AppTransactionGroupEntity();
            AppTransactionGroupConverter.CopyDtoToEntity(aAppTransactionGroupEntity, appTransactionGroupExDto);
            aAppBusienssAssormentNavigationEntity.AppTransactionGroup.Add(aAppTransactionGroupEntity);
            PorcessNewGroupItem(appTransactionGroupExDto, aAppTransactionGroupEntity);
        }

        private static void PorcessNewGroupItem(AppTransactionGroupExDto appTransactionGroupExDto, AppTransactionGroupEntity aAppTransactionGroupEntity)
        {
            foreach (var appTransactionGroupItemDto in appTransactionGroupExDto.AppTransactionGroupItemList.Where(o => o.IsNew))
            {
                AppTransactionGroupItemEntity aAppTransactionGroupItemEntity = new AppTransactionGroupItemEntity();
                AppTransactionGroupItemConverter.CopyDtoToEntity(aAppTransactionGroupItemEntity, appTransactionGroupItemDto);
                aAppTransactionGroupEntity.AppTransactionGroupItem.Add(aAppTransactionGroupItemEntity);
            }
        }

        private static ValidationResult ProcessDirtyAppBusienssAssormentNavigationExDto(AppBusienssAssormentNavigationExDto busienssAssormentNavigationExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppBusienssAssormentNavigationEntity busienssAssormentNavigationEntity = RetrieveOneAppBusienssAssormentNavigationEntity(busienssAssormentNavigationExDto.Id);

            AppBusienssAssormentNavigationConverter.CopyDtoToEntity(busienssAssormentNavigationEntity, busienssAssormentNavigationExDto);

            Dictionary<int, AppTransactionGroupEntity> dictDbAppTransactionGroup = busienssAssormentNavigationEntity.AppTransactionGroup.ToDictionary(o => o.TransactionGroupId, o => o);
            Dictionary<int, AppTransactionGroupExDto> dictDbAppTransactionGroupExdto = busienssAssormentNavigationExDto.AppTransactionGroupList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

            List<int> groupIdDbms = dictDbAppTransactionGroup.Keys.ToList();

            List<int> groupIdUi = dictDbAppTransactionGroupExdto.Keys.ToList();

            //Delete Id
            List<int> deletAppTransactionGroupIDs = groupIdDbms.Except(groupIdUi).ToList();
            List<int> deletAppTransactionGroupItemIDs = new List<int>();

            //new Entity
            busienssAssormentNavigationExDto.AppTransactionGroupList.Where(o => o.IsNew)
                .ForAll(o => ProcessNewGroupExDto(busienssAssormentNavigationEntity, o));

            //dirty
            List<int> dirtyAppTransactionGroupIds = groupIdDbms.Intersect(groupIdUi).ToList();

            //
            foreach (int updateGroupId in dirtyAppTransactionGroupIds)
            {
                AppTransactionGroupEntity appTransactionGroupEntity = dictDbAppTransactionGroup[updateGroupId];
                AppTransactionGroupExDto appTransactionGroupExdto = dictDbAppTransactionGroupExdto[updateGroupId];
                AppTransactionGroupConverter.CopyDtoToEntity(appTransactionGroupEntity, appTransactionGroupExdto);

                // new childITem
                PorcessNewGroupItem(appTransactionGroupExdto, appTransactionGroupEntity);

                // check child is new

                Dictionary<int, AppTransactionGroupItemEntity> dictDbAppTransactionGroupItem = appTransactionGroupEntity.AppTransactionGroupItem.Where(o => !o.IsNew).ToDictionary(o => o.GroupItemId, o => o);
                Dictionary<int, AppTransactionGroupItemExDto> dictDbAppTransactionGroupItemExdto = appTransactionGroupExdto.AppTransactionGroupItemList.Where(o => !o.IsNew).ToDictionary(o => int.Parse(o.Id.ToString()), o => o);

                List<int> groupItemIdDbms = dictDbAppTransactionGroupItem.Keys.ToList();
                List<int> groupItemIdUi = dictDbAppTransactionGroupItemExdto.Keys.ToList();

                //dirty
                List<int> dirtyAppTransactionGroupItemssIds = groupItemIdDbms.Intersect(groupItemIdUi).ToList();

                foreach (int updateGroupItemId in dirtyAppTransactionGroupItemssIds)
                {
                    AppTransactionGroupItemEntity dirtyAppTransactionGroupItemEntity = dictDbAppTransactionGroupItem[updateGroupItemId];
                    AppTransactionGroupItemExDto appTransactionGroupItemExdto = dictDbAppTransactionGroupItemExdto[updateGroupItemId];
                    AppTransactionGroupItemConverter.CopyDtoToEntity(dirtyAppTransactionGroupItemEntity, appTransactionGroupItemExdto);
                }

                //merger all deleteIds
                deletAppTransactionGroupItemIDs.AddRange(groupItemIdDbms.Except(groupItemIdUi));
            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(busienssAssormentNavigationEntity);

                    //if (deletAppTransactionGroupIDs.Count() > 0)
                    //{
                    adapter.DeleteEntitiesDirectly(typeof(AppTransactionGroupItemEntity), new RelationPredicateBucket(AppTransactionGroupItemFields.GroupItemId == deletAppTransactionGroupItemIDs));

                    //}

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupExDto), "App_SecurityGroupEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupExDto), "App_SecurityGroupEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

        public static OperationCallResult<bool> DeleteAppBusienssAssormentNavigation(object navigationId)
        {
            OperationCallResult<bool> aOperationCallResult = new OperationCallResult<bool>();
            var aValidationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = aValidationResult;

            EntityCollection<AppTransactionGroupEntity> groupEntityList = new EntityCollection<AppTransactionGroupEntity>();

            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adpater.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");

                    adpater.FetchEntityCollection(groupEntityList, new RelationPredicateBucket(AppTransactionGroupFields.AssotmentnavigationId == navigationId));

                    var groupIds = groupEntityList.Select(o => o.TransactionGroupId).ToList();

                    adpater.DeleteEntitiesDirectly(typeof(AppTransactionGroupItemEntity), new RelationPredicateBucket(AppTransactionGroupItemFields.TransactionGroupId == groupIds));

                    adpater.DeleteEntitiesDirectly(typeof(AppTransactionGroupEntity), new RelationPredicateBucket(AppTransactionGroupFields.AssotmentnavigationId == navigationId));
                    adpater.DeleteEntitiesDirectly(typeof(AppBusienssAssormentNavigationEntity), new RelationPredicateBucket(AppBusienssAssormentNavigationFields.AssotmentnavigationId == navigationId));

                    adpater.Commit();

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupEntity), "App_AppBusienssAssormentNavigation_Delete_OK", ValidationItemType.Message, "Delete Successful"));
                }
                catch (Exception ex)
                {
                    adpater.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppTransactionGroupEntity), "App_AppBusienssAssormentNavigation_Delete_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aOperationCallResult;
        }

        private static AppBusienssAssormentNavigationEntity RetrieveOneAppBusienssAssormentNavigationEntity(object navigationId)
        {
            using (DataAccessAdapter adpater = AppTenantAdapterBL.GetTenantAdapter())
            {
                AppBusienssAssormentNavigationEntity busienssAssormentNavigationEntity = new AppBusienssAssormentNavigationEntity(int.Parse(navigationId.ToString()));

                IPrefetchPath2 rootPath = new PrefetchPath2(EntityType.AppBusienssAssormentNavigationEntity);

                IPrefetchPathElement2 securityGroupMemberPathElement = rootPath.Add(AppBusienssAssormentNavigationEntity.PrefetchPathAppTransactionGroup);

                // optional
                securityGroupMemberPathElement.SubPath.Add(AppTransactionGroupEntity.PrefetchPathAppTransactionGroupItem);

                adpater.FetchEntity(busienssAssormentNavigationEntity, rootPath);
                return busienssAssormentNavigationEntity;
            }
        }

        public static AssortmentNavigationDto RetrieveFormAssortmentNavigation(int? navigationId, object transactionRId)
        {
            AssortmentNavigationDto toReturn = new AssortmentNavigationDto();
            toReturn.Id = navigationId;
            toReturn.TransactionRId = transactionRId;

            List<NavigationGroupDto> navigationGroupList = new List<NavigationGroupDto>();
            toReturn.NavigationGroupList = navigationGroupList;

            if (!navigationId.HasValue || transactionRId == null)
            {
                return toReturn;
            }

            AppBusienssAssormentNavigationExDto navigationExDto = GetOnenavitationExDto(navigationId);

            if (navigationExDto != null)
            {
                List<int> allTransactionIdList = new List<int>();

                navigationExDto.AppTransactionGroupList.Where(o => o.EmBuseinssScope.HasValue && o.EmBuseinssScope.Value == (int)EmAppBuseinssScope.Transaction).ForAll(o =>
                {
                    o.AppTransactionGroupItemList.Where(p => p.TransactionId.HasValue).ForAll(q => allTransactionIdList.Add(q.TransactionId.Value));
                });

                allTransactionIdList = allTransactionIdList.Distinct().ToList();

                toReturn.TransactionIdList = allTransactionIdList;

                foreach (var aTransactionGroup in navigationExDto.AppTransactionGroupList.OrderBy(o => o.GroupSortOrder))
                {
                    if (aTransactionGroup.EmBuseinssScope.HasValue)
                    {
                        NavigationGroupDto aNavigationGroupDto = new NavigationGroupDto();
                        aNavigationGroupDto.Id = aTransactionGroup.Id;
                        aNavigationGroupDto.GroupName = aTransactionGroup.GroupName;
                        aNavigationGroupDto.Description = aTransactionGroup.Description;
                        aNavigationGroupDto.EmBuseinssScope = aTransactionGroup.EmBuseinssScope;
                        aNavigationGroupDto.IsDefaultGroup = aTransactionGroup.IsDefaultGroup;

                        if (aNavigationGroupDto.EmBuseinssScope.Value == (int)EmAppBuseinssScope.Transaction)
                        {
                            SetupNavigationGroup_TransactionScope(aTransactionGroup, aNavigationGroupDto);
                        }
                        else if (aNavigationGroupDto.EmBuseinssScope.Value == (int)EmAppBuseinssScope.Project)
                        {
                            SetupNavigationGroup_ProjectOrWorkFlowScope(aTransactionGroup, aNavigationGroupDto, allTransactionIdList, transactionRId, EmAppProjectWorkflowType.Project);
                        }
                        else if (aNavigationGroupDto.EmBuseinssScope.Value == (int)EmAppBuseinssScope.Workflow)
                        {
                            SetupNavigationGroup_ProjectOrWorkFlowScope(aTransactionGroup, aNavigationGroupDto, allTransactionIdList, transactionRId, EmAppProjectWorkflowType.BusinessProcessWorkflow);
                        }
                        else if (aNavigationGroupDto.EmBuseinssScope.Value == (int)EmAppBuseinssScope.File)
                        {
                        }
                        else if (aNavigationGroupDto.EmBuseinssScope.Value == (int)EmAppBuseinssScope.SearchView)
                        {
                        }

                        navigationGroupList.Add(aNavigationGroupDto);
                    }
                }

                SetupNavigationDefaultGroupItem(toReturn, navigationExDto);
            }

            return toReturn;
        }

        private static void SetupNavigationGroup_TransactionScope(AppTransactionGroupExDto aTransactionGroup, NavigationGroupDto aNavigationGroupDto)
        {
            aNavigationGroupDto.HeaderTransactionList = new List<AppTransactionDto>();
            aNavigationGroupDto.MainTransactionList = new List<AppTransactionDto>();

            foreach (var groupItem in aTransactionGroup.AppTransactionGroupItemList.Where(o => o.TransactionId.HasValue).OrderBy(o => o.TransactionLayoutOrder))
            {
                if (groupItem.IsGroupSharedHeader.HasValue && groupItem.IsGroupSharedHeader.Value
                    || groupItem.IsCrossGroupSharedHeader.HasValue && groupItem.IsCrossGroupSharedHeader.Value)
                {
                    var transactionDto = AppTransactionConverter.ConvertEntityToDto(AppTransactionBL.RetrieveOneAppTransactionSimpleEntity(groupItem.TransactionId.Value));
                    aNavigationGroupDto.HeaderTransactionList.Add(transactionDto);
                }
                else
                {
                    var transactionDto = AppTransactionConverter.ConvertEntityToDto(AppTransactionBL.RetrieveOneAppTransactionSimpleEntity(groupItem.TransactionId.Value));
                    aNavigationGroupDto.MainTransactionList.Add(transactionDto);
                }
            }
        }

        private static void SetupNavigationGroup_ProjectOrWorkFlowScope(AppTransactionGroupExDto aTransactionGroup, NavigationGroupDto aNavigationGroupDto, List<int> allTransactionIdList, object transactionRId, EmAppProjectWorkflowType emAppProjectWorkflowType)
        {
            if (allTransactionIdList != null && allTransactionIdList.Count > 0 && transactionRId != null)
            {
                List<AppProjectOrWorkFlowDto> projectDtoList = AppProjectWorkFlowStructureBL.GetTransactionFormProjectOrWorkflowDtoList(allTransactionIdList, transactionRId, emAppProjectWorkflowType);
                aNavigationGroupDto.ProjectList = projectDtoList;
            }
        }

        private static void SetupNavigationDefaultGroupItem(AssortmentNavigationDto toReturn, AppBusienssAssormentNavigationExDto navigationExDto)
        {
            if (navigationExDto.AppTransactionGroupList.Count > 0)
            {
                var defaultGroup = navigationExDto.AppTransactionGroupList.FirstOrDefault(o => o.IsDefaultGroup.HasValue && o.IsDefaultGroup.Value);

                if (defaultGroup == null)
                {
                    defaultGroup = navigationExDto.AppTransactionGroupList.First();
                }

                toReturn.DefaultGroupId = ControlTypeValueConverter.ConvertValueToInt(defaultGroup.Id);
                toReturn.DefaultGroupType = defaultGroup.EmBuseinssScope;

                if (toReturn.DefaultGroupType.HasValue)
                {
                    if (toReturn.DefaultGroupType.Value == (int)EmAppBuseinssScope.Transaction)
                    {
                        var defaultTransaction = defaultGroup.AppTransactionGroupItemList.FirstOrDefault(o => o.TransactionId.HasValue
                        && !(o.IsGroupSharedHeader.HasValue && o.IsGroupSharedHeader.Value)
                        && !(o.IsCrossGroupSharedHeader.HasValue && o.IsCrossGroupSharedHeader.Value));

                        if (defaultTransaction != null)
                        {
                            toReturn.DefaultOpenItemId = defaultTransaction.TransactionId.Value;
                        }
                    }
                    else if (toReturn.DefaultGroupType.Value == (int)EmAppBuseinssScope.Project)
                    {
                    }
                }
            }
        }
    }
}