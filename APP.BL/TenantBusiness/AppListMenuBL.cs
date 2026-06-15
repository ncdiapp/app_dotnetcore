using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using APP.LBL;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.Framework.Validation;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Components.EntityConverter;
using System.Data;
using APP.Framework.Communication;

using APP.Framework;
namespace App.BL
{
    public static class AppListMenuBL
    {

        public static readonly string ProjectConfigurationRootMenuGuid = "9D0F4CB5-E26A-40B5-9AFB-28D5FED51F5B";
        public static readonly string ESiteConfigurationRootMenuGuid = "706026CA-27FE-4A7D-B0BF-03DC2966DB74";
        public static readonly string AppWebSiteConfigurationRootMenuGuid = "9F92CEB7-8EA9-4E2D-AD4A-00293DD06F4B";

        public static EntityCollection<AppListMenuEntity> RetrieveAllAppListMenuEntity(bool isWebPageMenu = false)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                EntityCollection<AppListMenuEntity> list = new EntityCollection<AppListMenuEntity>();
                SortClause aSortClause = AppListMenuFields.Sort | SortOperator.Ascending;

                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppListMenuEntity);
                root.Add(AppListMenuEntity.PrefetchPathAppListMenu_).Sorter.Add(aSortClause);

                IRelationPredicateBucket aFilter = new RelationPredicateBucket(new FieldCompareNullPredicate(AppListMenuFields.ParentId, null));
                if (isWebPageMenu)
                    aFilter.PredicateExpression.AddWithAnd(AppListMenuFields.LinkType == (int)EmAppListMenuLinkType.WebPage);
                else
                    aFilter.PredicateExpression.AddWithAnd(AppListMenuFields.LinkType != (int)EmAppListMenuLinkType.WebPage);
                adapter.FetchEntityCollection(list, aFilter, 0, new SortExpression(aSortClause), root);

                return list;
            }

        }

        public static EntityCollection<AppListMenuEntity> RetrieveAppListMenuEntityWithIntalCode(string internalCodes)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                EntityCollection<AppListMenuEntity> list = new EntityCollection<AppListMenuEntity>();
                SortClause aSortClause = AppListMenuFields.Sort | SortOperator.Ascending;// new SortClause(AppListMenuFields.Sort | SortOperator.Ascending);


                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppListMenuEntity);
                root.Add(AppListMenuEntity.PrefetchPathAppListMenu_).Sorter.Add(aSortClause);

                IRelationPredicateBucket aFilter = new RelationPredicateBucket(new FieldCompareNullPredicate(AppListMenuFields.ParentId, null));
                aFilter.PredicateExpression.AddWithAnd(AppListMenuFields.RouteCode == internalCodes);

                adapter.FetchEntityCollection(list, aFilter, 0, new SortExpression(aSortClause), root);

                return list;



            }

        }


        public static ObservableSet<AppListMenuExDto> RetrieveAppListMenuExdtoWithIntalCode(string internalCodes)
        {
            return ConvertListMenuEntityListToDto(RetrieveAppListMenuEntityWithIntalCode(internalCodes)); ;

        }

        public static AppListMenuEntity RetrieveOneAppListMenuEntity(object Id)
        {
            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {

                AppListMenuEntity aAppListMenuEntity = new AppListMenuEntity(int.Parse(Id.ToString()));
                SortClause aSort = AppListMenuFields.Sort | SortOperator.Ascending;// new SortClause(AppListMenuFields.Sort | SortOperator.Ascending);
                IPrefetchPath2 root = new PrefetchPath2(EntityType.AppListMenuEntity);
                root.Add(AppListMenuEntity.PrefetchPathAppListMenu_).Sorter.Add(aSort);
                adapter.FetchEntity(aAppListMenuEntity, root);

                return aAppListMenuEntity;


            }


        }

        public static AppListMenuExDto RetrieveOneAppListMenuExDto(object Id)
        {
            if (Id != null)
            {
                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    AppListMenuEntity aAppListMenuEntity = new AppListMenuEntity(int.Parse(Id.ToString()));
                    IPrefetchPath2 root = new PrefetchPath2(EntityType.AppListMenuEntity);
                    adapter.FetchEntity(aAppListMenuEntity, root);
                    AppListMenuExDto aDto = AppListMenuConverter.ConvertEntityToExDto(aAppListMenuEntity);

                    return aDto;
                }
            }
            else
            {
                return null;
            }
        }

        public static OperationCallResult<AppListMenuExDto> SaveOneAppListMenuExDto(AppListMenuExDto appListMenuExDto)
        {
            OperationCallResult<AppListMenuExDto> aOperationCallResult = new OperationCallResult<AppListMenuExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;

            if (appListMenuExDto.IsNew)
            {
                validationResult.Merge(ProcessNewDto(appListMenuExDto));
            }
            else
            {
                validationResult.Merge(ProcessDirtyDto(appListMenuExDto));
            }

            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.Object = RetrieveOneAppListMenuExDto(appListMenuExDto.Id);
            }

            return aOperationCallResult;
        }

        public static ObservableSet<AppListMenuExDto> RetrieveAllAppListMenuEntityDto(bool isWebPageMenu = false)
        {


            EntityCollection<AppListMenuEntity> list = RetrieveAllAppListMenuEntity(isWebPageMenu);
            ObservableSet<AppListMenuExDto> aSet = ConvertListMenuEntityListToDto(list);

            return aSet;


        }




        private static ObservableSet<AppListMenuExDto> ConvertListMenuEntityListToDto(EntityCollection<AppListMenuEntity> list)
        {
            ObservableSet<AppListMenuExDto> aSet = new ObservableSet<AppListMenuExDto>();
            foreach (var entity in list)
            {
                AppListMenuExDto aDto = AppListMenuConverter.ConvertEntityToExDto(entity);


                aSet.Add(aDto);
                foreach (var aChild in entity.AppListMenu_)
                {
                    var childDto = AppListMenuConverter.ConvertEntityToExDto(aChild);

                    if (!string.IsNullOrEmpty(childDto.IconName))
                    {

                        childDto.ImageUrl = childDto.IconName;

                    }

                    aDto.AppListMenu_List.Add(childDto);


                }

            }
            return aSet;
        }

        public static OperationCallResult<AppListMenuExDto> SaveAllAppListMenuEntityDto(ObservableSet<AppListMenuExDto> aSet, bool isWebPageMenu = false)
        {
            OperationCallResult<AppListMenuExDto> aOperationCallResult = new OperationCallResult<AppListMenuExDto>();
            ValidationResult validationResult = new ValidationResult();
            aOperationCallResult.ValidationResult = validationResult;


            aSet.FindModifiedItems().Where(o => o.IsNew == false).ForAll(o => validationResult.Merge(ProcessDirtyDto(o)));

            aSet.FindNewItems().ForAll(o => validationResult.Merge(ProcessNewDto(o)));

            aSet.FindDeletedItemIds().ForAll(Id => validationResult.Merge(ProcessDeleteParent(Id)));



            // if no any errors, refresh all entity from DBMS server
            if (!validationResult.HasErrors)
            {
                aOperationCallResult.ObjectList = RetrieveAllAppListMenuEntityDto(isWebPageMenu);
            }

            return aOperationCallResult;


        }


        private static ValidationResult ProcessDeleteParent(object Id)
        {
            ValidationResult aValidationResult = new ValidationResult();

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "AppListMenuEntity");
                    adapter.DeleteEntitiesDirectly(typeof(AppListMenuEntity), new RelationPredicateBucket(AppListMenuFields.ParentId == Id));
                    adapter.DeleteEntitiesDirectly(typeof(AppListMenuEntity), new RelationPredicateBucket(AppListMenuFields.MenuId == Id));

                    adapter.Commit();

                }



                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));

                    adapter.Rollback();
                }
            }

            return aValidationResult;
        }
        private static ValidationResult ProcessNewDto(AppListMenuExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppListMenuEntity aParentAppListMenuEntity = new AppListMenuEntity();
            AppListMenuConverter.CopyDtoToEntity(aParentAppListMenuEntity, aDto);

            foreach (AppListMenuExDto aChildDto in aDto.AppListMenu_List)
            {
                AppListMenuEntity aChildAppListMenuEntity = new AppListMenuEntity();
                AppListMenuConverter.CopyDtoToEntity(aChildAppListMenuEntity, aChildDto);
                aParentAppListMenuEntity.AppListMenu_.Add(aChildAppListMenuEntity);

            }

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppListMenuEntity);

                    adapter.Commit();
                    aDto.Id = aParentAppListMenuEntity.MenuId;

                    AppListMenuUserAndDomainBL.SaveNewRoleMenusSecurity((int)EmAppBuiltInUserGroup.CompanyAdmin, new List<int>() { aParentAppListMenuEntity.MenuId });

                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }




            }

            return aValidationResult;
        }
        private static ValidationResult ProcessDirtyDto(AppListMenuExDto aDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            AppListMenuEntity aParentAppListMenuEntity = RetrieveOneAppListMenuEntity(aDto.Id);

            AppListMenuConverter.CopyDtoToEntity(aParentAppListMenuEntity, aDto);

            // from DBMS entity
            Dictionary<int, AppListMenuEntity> dictChildListMenuFromDbms = aParentAppListMenuEntity.AppListMenu_.ToDictionary(o => o.MenuId, o => o);


            foreach (AppListMenuExDto aChildDto in aDto.AppListMenu_List.FindNewItems())
            {

                AppListMenuEntity aNewChildEntity = new AppListMenuEntity();
                AppListMenuConverter.CopyDtoToEntity(aNewChildEntity, aChildDto);
                aParentAppListMenuEntity.AppListMenu_.Add(aNewChildEntity);


            }

            foreach (var modifyitem in aDto.AppListMenu_List.FindModifiedItems())
            {


                int dtoKey = int.Parse(modifyitem.Id.ToString());
                if (dictChildListMenuFromDbms.ContainsKey(dtoKey))
                {

                    AppListMenuConverter.CopyDtoToEntity(dictChildListMenuFromDbms[dtoKey], modifyitem);


                }


            }


            // Must use int [] type for in clause collection !, Linq type doest not work for the in clause deletion                         
            int[] deleteChildsIDs = aDto.AppListMenu_List.FindDeletedItemIds().Cast<int>().ToArray();


            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    adapter.SaveEntity(aParentAppListMenuEntity, false, true);
                    // using batch Delete
                    if (deleteChildsIDs.Count() > 0)
                    {
                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityRegDomainListMenuEntity), new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.MenuId == deleteChildsIDs));
                        adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserListMenuEntity), new RelationPredicateBucket(AppSecurityUserListMenuFields.MenuId == deleteChildsIDs));
                        adapter.DeleteEntitiesDirectly(typeof(AppListMenuEntity), new RelationPredicateBucket(AppListMenuFields.MenuId == deleteChildsIDs));
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));


                }


                catch (ORMQueryExecutionException ex)
                {

                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));


                }
            }

            return aValidationResult;
        }




        public static ValidationResult SynchronizeOneEsitePageMenus(AppEsitePagesExDto appEsitePagesExDto)
        {
            ValidationResult aValidationResult = new ValidationResult();

            if (appEsitePagesExDto != null)
            {
                if (appEsitePagesExDto.EsiteId.HasValue && appEsitePagesExDto.SearchId.HasValue
                    && !string.IsNullOrWhiteSpace(appEsitePagesExDto.MetaDesciption))
                {
                    using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                    {
                        try
                        {                            
                            AppListMenuEntity updateEntity = new AppListMenuEntity();
                            updateEntity.Link = appEsitePagesExDto.SearchId.Value.ToString();
                                                       
                            adapter.UpdateEntitiesDirectly(updateEntity, new RelationPredicateBucket(AppListMenuFields.EsiteId == appEsitePagesExDto.EsiteId.Value & AppListMenuFields.RouteCode == appEsitePagesExDto.MetaDesciption));

                            adapter.Commit();
                          
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                        }

                        // Entity Logical Validation Exception
                        catch (ORMEntityValidationException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppListMenuEntity_BLValidation_Error", ValidationItemType.Error, ex.ToString()));
                        }

                        // Database FK Exeption ........
                        catch (ORMQueryExecutionException ex)
                        {
                            adapter.Rollback();
                            aValidationResult.Items.Add(new ValidationItem(typeof(AppEsitePagesEntity), "App_AppListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                        }

                    }
                }
            }            

            return aValidationResult;
        }

    }
}
