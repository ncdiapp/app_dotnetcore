using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
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
	public static class AppListMenuUserAndDomainBL
	{
		public static ObservableSet<AppListMenuExDto> RetrieveAllMenu()
		{
			return AppListMenuBL.RetrieveAllAppListMenuEntityDto();
		}

		public static ObservableSet<AppListMenuExDto> RetrieveDomainOrUserMenu(List<int> domainIdOrUserIdOrOrganizationId, EmAppMenuRegisterType anEmMenuRegisterType)
		{
			List<int> menuLists;

			if (anEmMenuRegisterType == EmAppMenuRegisterType.User)
			{
				EntityCollection<AppSecurityUserListMenuEntity> userMeneList = new EntityCollection<AppSecurityUserListMenuEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityUserListMenuFields.UserId == domainIdOrUserIdOrOrganizationId);

					adapter.FetchEntityCollection(userMeneList, aFilter);
				}

				menuLists = userMeneList.Select(o => o.MenuId.Value).ToList();
			}
			else if (anEmMenuRegisterType == EmAppMenuRegisterType.Organization)
			{
				EntityCollection<AppSecurityRegDomainListMenuEntity> userMeneList = new EntityCollection<AppSecurityRegDomainListMenuEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.OrganizationId == domainIdOrUserIdOrOrganizationId);

					adapter.FetchEntityCollection(userMeneList, aFilter);
				}

				menuLists = userMeneList.Select(o => o.MenuId).ToList();
			}
			else
			{
				EntityCollection<AppSecurityRegDomainListMenuEntity> userMeneList = new EntityCollection<AppSecurityRegDomainListMenuEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.DomainId == domainIdOrUserIdOrOrganizationId);

					adapter.FetchEntityCollection(userMeneList, aFilter);
				}

				menuLists = userMeneList.Select(o => o.MenuId).ToList();
			}

			ObservableSet<AppListMenuExDto> aSet = new ObservableSet<AppListMenuExDto>();
			ObservableSet<AppListMenuExDto> allListMenu = AppListMenuBL.RetrieveAllAppListMenuEntityDto();

			foreach (var menuGroup in allListMenu)
			{
				if (menuLists.Contains((int)menuGroup.Id))
				{
					aSet.Add(menuGroup);
					ObservableSet<AppListMenuExDto> childMenuList = new ObservableSet<AppListMenuExDto>();
					foreach (var aChildMenu in menuGroup.AppListMenu_List)
					{
						if (menuLists.Contains((int)aChildMenu.Id))
						{
							childMenuList.Add(aChildMenu);
						}
					}
					menuGroup.AppListMenu_List = childMenuList;
				}
			}

			return aSet;
		}

		private static void BuildUserTreeMenus(ObservableSet<AppListMenuExDto> treeMenus, ObservableSet<AppListMenuExDto> userTreeMenus, List<int> userMenuIds)
		{
			foreach (var aMenu in treeMenus)
			{
				if (userMenuIds.Contains((int)aMenu.Id))
				{
					userTreeMenus.Add(aMenu);
					ObservableSet<AppListMenuExDto> userChildTreeMenus = new ObservableSet<AppListMenuExDto>();
					BuildUserTreeMenus(aMenu.AppListMenu_List, userChildTreeMenus, userMenuIds);
					aMenu.AppListMenu_List = userChildTreeMenus;
				}
			}
		}

		public static ObservableSet<AppListMenuExDto> RetrieveDomainOrUserTreeMenu(List<int> domainIdOrUserIdOrOrganizationId, EmAppMenuRegisterType anEmMenuRegisterType)
		{
			List<int> userMenuIds;

			if (anEmMenuRegisterType == EmAppMenuRegisterType.User)
			{
				EntityCollection<AppSecurityUserListMenuEntity> userMeneList = new EntityCollection<AppSecurityUserListMenuEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityUserListMenuFields.UserId == domainIdOrUserIdOrOrganizationId);

					adapter.FetchEntityCollection(userMeneList, aFilter);
				}

				userMenuIds = userMeneList.Select(o => o.MenuId.Value).ToList();
			}
            else if (anEmMenuRegisterType == EmAppMenuRegisterType.Role)
            {
                EntityCollection<AppSecurityUserListMenuEntity> userMeneList = new EntityCollection<AppSecurityUserListMenuEntity>();

                using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityUserListMenuFields.GroupId == domainIdOrUserIdOrOrganizationId);

                    adapter.FetchEntityCollection(userMeneList, aFilter);
                }

                userMenuIds = userMeneList.Select(o => o.MenuId.Value).ToList();
            }
            else if (anEmMenuRegisterType == EmAppMenuRegisterType.Organization)
			{
				EntityCollection<AppSecurityRegDomainListMenuEntity> userMeneList = new EntityCollection<AppSecurityRegDomainListMenuEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.OrganizationId == domainIdOrUserIdOrOrganizationId);

					adapter.FetchEntityCollection(userMeneList, aFilter);
				}

				userMenuIds = userMeneList.Select(o => o.MenuId).ToList();
			}
			else // finally domina
			{
				EntityCollection<AppSecurityRegDomainListMenuEntity> userMeneList = new EntityCollection<AppSecurityRegDomainListMenuEntity>();

				using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
				{
					IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.DomainId == domainIdOrUserIdOrOrganizationId);

					adapter.FetchEntityCollection(userMeneList, aFilter);
				}

				userMenuIds = userMeneList.Select(o => o.MenuId).ToList();
			}

			ObservableSet<AppListMenuExDto> userTreeMenus = new ObservableSet<AppListMenuExDto>();
			ObservableSet<AppListMenuExDto> allTreeMenus = AppTreeListMenuBL.RetrieveListMenuHairarchyDto(false, null, true);

			BuildUserTreeMenus(allTreeMenus, userTreeMenus, userMenuIds);

			return userTreeMenus;
		}

		public static List<AppListMenuExDto> RetrieveEmployeeDomainMenuTree()
		{
			List<AppListMenuExDto> employeeDomainMenuTree = new List<AppListMenuExDto>();

			var organizationDomianEntity = AppSecurityRegDomainBL.GetOrganizationDomainEntity();
			if (organizationDomianEntity != null)
			{
				employeeDomainMenuTree = RetrieveDomainOrUserTreeMenu(new List<int>() { organizationDomianEntity.DomainId }, EmAppMenuRegisterType.RegionDomain).ToList();
			}

			return employeeDomainMenuTree;
		}

		public static OperationCallResult<AppListMenuExDto> SaveDomainOrUserMenu(int domainIdOrUserId, EmAppMenuRegisterType anEmMenuRegisterType, List<int> uiSelectedMenuIdList)
		{
			OperationCallResult<AppListMenuExDto> aOperationCallResult = new OperationCallResult<AppListMenuExDto>();
			ValidationResult validationResult = new ValidationResult();
			aOperationCallResult.ValidationResult = validationResult;

			if (anEmMenuRegisterType == EmAppMenuRegisterType.User)
			{
				validationResult.Merge(SavePdmSecurityWebUserMenu(domainIdOrUserId, uiSelectedMenuIdList));
			}
            else if (anEmMenuRegisterType == EmAppMenuRegisterType.Role)
            {
                validationResult.Merge(SavePdmSecurityRoleMenu(domainIdOrUserId, uiSelectedMenuIdList));
            }
            else if (anEmMenuRegisterType == EmAppMenuRegisterType.Organization)
			{
				validationResult.Merge(SavePdmSecurityOrganizationMainMenu(domainIdOrUserId, uiSelectedMenuIdList));
			}
			else
			{
				validationResult.Merge(SavePdmSecurityRegDoMainMenu(domainIdOrUserId, uiSelectedMenuIdList));
			}

			// if no any errors, refresh all entity from DBMS server
			if (!validationResult.HasErrors)
			{
				aOperationCallResult.ObjectList = RetrieveDomainOrUserTreeMenu(new List<int>() { domainIdOrUserId }, anEmMenuRegisterType);
			}

			return aOperationCallResult;
		}

		private static bool FindNeedToSaveAppSecurityRegDomainListMenuIds(ObservableSet<AppListMenuExDto> menuTree, List<int> uiSelectedMenuIdList, List<int> allNeedToSaveMenuIds)
		{
			//
			var isHaveMenuSelected = false;

			foreach (var aMenu in menuTree)
			{
				if (uiSelectedMenuIdList.Contains((int)aMenu.Id))
				{
					allNeedToSaveMenuIds.Add((int)aMenu.Id);
					isHaveMenuSelected = true;
				}

				bool isHaveChildMenuSelected = FindNeedToSaveAppSecurityRegDomainListMenuIds(aMenu.AppListMenu_List, uiSelectedMenuIdList, allNeedToSaveMenuIds);
				if (isHaveChildMenuSelected)
				{
					isHaveMenuSelected = true;
					if (!allNeedToSaveMenuIds.Contains((int)aMenu.Id))
					{
						allNeedToSaveMenuIds.Add((int)aMenu.Id);
					}
				}
			}

			return isHaveMenuSelected;
		}

		private static ValidationResult SavePdmSecurityRegDoMainMenu(object domainIdOrUserId, List<int> uiSelectedMenuIdList)
		{

            ValidationResult aValidationResult = new ValidationResult();

            var menuTree = AppTreeListMenuBL.RetrieveListMenuHairarchyDto();
            List<int> allNeedToSaveMenuIds = new List<int>();
            FindNeedToSaveAppSecurityRegDomainListMenuIds(menuTree, uiSelectedMenuIdList, allNeedToSaveMenuIds);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.DomainId == domainIdOrUserId);
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    // put the entity operation here ------

                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityRegDomainListMenuEntity), aFilter);

                    foreach (int menuId in allNeedToSaveMenuIds)
                    {
                        AppSecurityRegDomainListMenuEntity aEntity = new AppSecurityRegDomainListMenuEntity();
                        aEntity.MenuId = menuId;
                        aEntity.DomainId = (int)domainIdOrUserId;
                        adapter.SaveEntity(aEntity);
                    }

                    adapter.Commit();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
                }

                // Database FK Exception .......
                catch (ORMQueryExecutionException ex)
                {
                    adapter.Rollback();
                    aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
                }
            }

            return aValidationResult;
        }

		private static ValidationResult SavePdmSecurityOrganizationMainMenu(object domainIdOrUserIdOrOrgId, List<int> uiSelectedMenuIdList)
		{
			ValidationResult aValidationResult = new ValidationResult();

			var menuTree = AppTreeListMenuBL.RetrieveListMenuHairarchyDto();
			List<int> allNeedToSaveMenuIds = new List<int>();
			FindNeedToSaveAppSecurityRegDomainListMenuIds(menuTree, uiSelectedMenuIdList, allNeedToSaveMenuIds);

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityRegDomainListMenuFields.OrganizationId == domainIdOrUserIdOrOrgId);
				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
					// put the entity operation here ------

					adapter.DeleteEntitiesDirectly(typeof(AppSecurityRegDomainListMenuEntity), aFilter);

					foreach (int menuId in allNeedToSaveMenuIds)
					{
						AppSecurityRegDomainListMenuEntity aEntity = new AppSecurityRegDomainListMenuEntity();
						aEntity.MenuId = menuId;
						aEntity.OrganizationId = (int)domainIdOrUserIdOrOrgId;
						adapter.SaveEntity(aEntity);
					}

					adapter.Commit();
					aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_Save_OK", ValidationItemType.Message, "Saved Successfully"));
				}

				// Database FK Exception .......
				catch (ORMQueryExecutionException ex)
				{
					adapter.Rollback();
					aValidationResult.Items.Add(new ValidationItem(typeof(AppListMenuEntity), "App_ListMenuEntity_QueryExecution_Error", ValidationItemType.Error, ex.ToString()));
				}
			}

			return aValidationResult;
		}

		private static ValidationResult SavePdmSecurityWebUserMenu(object domainIdOrUserId, List<int> uiSelectedMenuIdList)
		{
			ValidationResult aValidationResult = new ValidationResult();

			var menuTree = AppTreeListMenuBL.RetrieveListMenuHairarchyDto();
			List<int> allNeedToSaveMenuIds = new List<int>();
			FindNeedToSaveAppSecurityRegDomainListMenuIds(menuTree, uiSelectedMenuIdList, allNeedToSaveMenuIds);

			using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
			{
				IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityUserListMenuFields.UserId == domainIdOrUserId);
				try
				{
					adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
					// put the entity operation here ------
					adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserListMenuEntity), aFilter);
					foreach (var menuId in allNeedToSaveMenuIds)
					{
						AppSecurityUserListMenuEntity aEntity = new AppSecurityUserListMenuEntity();
						aEntity.MenuId = menuId;
						aEntity.UserId = (int)domainIdOrUserId;
						adapter.SaveEntity(aEntity);
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

        private static ValidationResult SavePdmSecurityRoleMenu(object domainIdOrUserId, List<int> uiSelectedMenuIdList)
        {
            ValidationResult aValidationResult = new ValidationResult();

            var menuTree = AppTreeListMenuBL.RetrieveListMenuHairarchyDto();
            List<int> allNeedToSaveMenuIds = new List<int>();
            FindNeedToSaveAppSecurityRegDomainListMenuIds(menuTree, uiSelectedMenuIdList, allNeedToSaveMenuIds);

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                IRelationPredicateBucket aFilter = new RelationPredicateBucket(AppSecurityUserListMenuFields.GroupId == domainIdOrUserId);
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    // put the entity operation here ------
                    adapter.DeleteEntitiesDirectly(typeof(AppSecurityUserListMenuEntity), aFilter);
                    foreach (var menuId in allNeedToSaveMenuIds)
                    {
                        AppSecurityUserListMenuEntity aEntity = new AppSecurityUserListMenuEntity();
                        aEntity.MenuId = menuId;
                        aEntity.GroupId = (int)domainIdOrUserId;
                        adapter.SaveEntity(aEntity);
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

        internal static ValidationResult SaveNewRoleMenusSecurity(int roleId, List<int> newMenuIdList)
        {
            ValidationResult aValidationResult = new ValidationResult();			        

            using (DataAccessAdapter adapter = AppTenantAdapterBL.GetTenantAdapter())
            {                
                try
                {
                    adapter.StartTransaction(IsolationLevel.ReadCommitted, "StartTransaction");
                    // put the entity operation here ------
                    
                    foreach (var menuId in newMenuIdList)
                    {
                        AppSecurityUserListMenuEntity aEntity = new AppSecurityUserListMenuEntity();
                        aEntity.MenuId = menuId;
                        aEntity.GroupId = roleId;
                        adapter.SaveEntity(aEntity);
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
    }
}