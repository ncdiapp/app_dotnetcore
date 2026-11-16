using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.DbGenie;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using APP.Framework.Plugin;
using APP.Framework.Communication;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Tools for Step 1 (create application package), Step 6 (add search to navigation menu),
    /// and application deletion.
    /// </summary>
    public class ApplicationManagerPlugin
    {
        private readonly int? _dataSourceId;

        public ApplicationManagerPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("create_app_package",
            "Step 1 — Create a new named application package in the platform. " +
            "Returns the new SaasApplicationId (integer). " +
            "Call this FIRST before creating tables, entities, or transactions. " +
            "Pass the returned SaasApplicationId to all subsequent tools that require it.")]
        public string CreateAppPackage(
            [AgentParam("Display name for the new application, e.g. 'Sales Order Management'", isRequired: true)]
            string applicationName)
        {
            try
            {
                // Return existing package if one with the same name already exists.
                var existing = AppSaasUserApplicationPackageBL.GetSaasApplicationList()
                    .FirstOrDefault(a => string.Equals(a.Name, applicationName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess         = true,
                        SaasApplicationId = (int)existing.Id,
                        ApplicationName   = existing.Name,
                        Note              = "Existing application package reused — name already exists. Pass SaasApplicationId to all subsequent tools."
                    }, Formatting.Indented);

                var menuDto = new AppListMenuExDto { Name = applicationName };
                var result  = AppSaasUserApplicationPackageBL.CreateMyNewApplicationPackage(menuDto);

                if (result?.Object == null)
                {
                    var errors = result?.ValidationResult?.Items?
                        .Where(i => i.ItemType == APP.Framework.Validation.ValidationItemType.Error)
                        .Select(i => i.Message)
                        .ToList();
                    var errorMsg = errors != null && errors.Count > 0
                        ? string.Join("; ", errors)
                        : "Application package was not created";
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = errorMsg });
                }

                int newId = result.Object.Value;
                AppBuilderState.RegisterCreatedPackage(newId);

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess        = true,
                    SaasApplicationId = newId,
                    ApplicationName  = applicationName,
                    Note             = "Pass SaasApplicationId to create_entity_simple_list, create_entity_from_table, and create_application."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("delete_application",
            "Permanently delete an application and ALL of its associated data: " +
            "transactions (data models + forms), search views, entity data sources, " +
            "and optionally the physical database tables. " +
            "Find the application by name (case-insensitive). " +
            "ALWAYS call propose_plan before this — deletion is irreversible. " +
            "Returns a detailed report of everything that was deleted and any errors.")]
        public string DeleteApplication(
            [AgentParam("Name of the application to delete (case-insensitive match).", isRequired: true)]
            string applicationName,
            [AgentParam("Set true to also DROP the physical database tables. Default false — only removes AppAI configuration.")]
            bool dropDatabaseTables = false)
        {
            var report = new
            {
                ApplicationName    = applicationName,
                ApplicationId      = (int?)null,
                TransactionsDeleted = new List<string>(),
                SearchesDeleted    = new List<string>(),
                EntitiesDeleted    = new List<string>(),
                TablesDropped      = new List<string>(),
                Errors             = new List<string>()
            };

            // Use mutable local lists so we can populate them
            var transactionsDeleted = new List<string>();
            var searchesDeleted     = new List<string>();
            var entitiesDeleted     = new List<string>();
            var tablesDropped       = new List<string>();
            var errors              = new List<string>();

            try
            {
                // ── 1. Find application by name ──────────────────────────────
                var apps = AppSaasUserApplicationPackageBL.GetSaasApplicationList();
                var app  = apps.FirstOrDefault(a =>
                    string.Equals(a.Name, applicationName, StringComparison.OrdinalIgnoreCase));

                if (app == null)
                {
                    var available = string.Join(", ", apps.Select(a => a.Name));
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error     = $"Application '{applicationName}' not found. Available: {available}"
                    });
                }

                int appId = (int)app.Id;

                // ── 2. Collect table names from transactions BEFORE deleting ─
                var allTransactions = AppTransactionBL.RetrieveAllAppTransactionDto(false)
                    .Where(t => t.SaasApplicationId == appId)
                    .ToList();

                var tablesToDrop = new List<string>();
                foreach (var trans in allTransactions)
                {
                    try
                    {
                        int transId = (int)trans.Id;
                        AppCacheManagerBL.RefreshOnetHierarchyTranscation(transId);
                        var hierarchy = AppTransactionBL.GetHierarchyTranscationFromDatabase(transId);
                        if (hierarchy?.AppTransactionUnitList != null)
                        {
                            foreach (var unit in hierarchy.AppTransactionUnitList)
                            {
                                var tbl = unit.DataBaseTableName;
                                if (!string.IsNullOrWhiteSpace(tbl) && !tablesToDrop.Contains(tbl, StringComparer.OrdinalIgnoreCase))
                                    tablesToDrop.Add(tbl);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Could not read transaction {trans.Id} tables: {ex.Message}");
                    }
                }

                // ── 3. Delete searches ───────────────────────────────────────
                try
                {
                    var searches = AppSearchConfigBL.RetrieveSaasApplicationSearchList(appId);
                    foreach (var search in searches)
                    {
                        try
                        {
                            AppSearchConfigBL.DeleteAppSearch(search.Id, isDeleteDefaultView: true, isDeleteDataSet: true);
                            searchesDeleted.Add(search.Name ?? search.Id.ToString());
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Delete search '{search.Name}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"List searches: {ex.Message}");
                }

                // ── 4. Delete transactions ───────────────────────────────────
                foreach (var trans in allTransactions)
                {
                    try
                    {
                        AppTransactionBL.DeleteOneAppTransaction((int)trans.Id);
                        transactionsDeleted.Add(trans.TransactionName ?? trans.Id.ToString());
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Delete transaction '{trans.TransactionName}': {ex.Message}");
                    }
                }

                // ── 5. Delete entity data sources ────────────────────────────
                try
                {
                    var entities = AppEntityInfoBL.RetrieveAllAppEntityInfoDto()
                        .Where(e => e.SaasApplicationId == appId)
                        .ToList();
                    foreach (var entity in entities)
                    {
                        try
                        {
                            AppEntityInfoBL.DeleteOneAppEntityInfo(entity.Id);
                            entitiesDeleted.Add(entity.EntityCode ?? entity.Id.ToString());
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Delete entity '{entity.EntityCode}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"List entities: {ex.Message}");
                }

                // ── 6. DROP physical database tables ────────────────────────
                if (dropDatabaseTables && tablesToDrop.Count > 0)
                {
                    if (!_dataSourceId.HasValue)
                    {
                        errors.Add("dropDatabaseTables=true but no data source is configured — tables were NOT dropped.");
                    }
                    else
                    {
                        var fixture = AppCacheManagerBL.GetOneDatabaseFixture(_dataSourceId.Value);
                        foreach (var tbl in tablesToDrop)
                        {
                            try
                            {
                                fixture.ExecuteNonQueryResult(
                                    $"IF OBJECT_ID(N'dbo.{tbl}', N'U') IS NOT NULL DROP TABLE dbo.[{tbl}]",
                                    new List<System.Data.Common.DbParameter>());
                                tablesDropped.Add(tbl);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"DROP TABLE {tbl}: {ex.Message}");
                            }
                        }
                    }
                }

                // ── 7. Delete the application package itself ─────────────────
                try
                {
                    AppSaasUserApplicationPackageBL.DeleteOneApplicationPackage(appId);
                }
                catch (Exception ex)
                {
                    errors.Add($"Delete application package: {ex.Message}");
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess           = errors.Count == 0,
                    ApplicationId       = appId,
                    ApplicationName     = applicationName,
                    TransactionsDeleted = transactionsDeleted,
                    SearchesDeleted     = searchesDeleted,
                    EntitiesDeleted     = entitiesDeleted,
                    TablesDropped       = dropDatabaseTables ? (object)tablesDropped : "Not requested",
                    Errors              = errors.Count > 0 ? errors : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("add_transaction_to_menu",
            "Add a List or FolderList transaction (data model) to the application's main navigation menu as a clickable item. " +
            "Use this to expose a List Edit transaction in the left-side navigation. " +
            "Looks up the transaction by name (case-insensitive), then adds it under the menu group. " +
            "Only List and FolderList transaction types are supported (not MasterDetail). " +
            "To find the correct transactionName use search_platform or get_existing_transactions first.")]
        public string AddTransactionToMenu(
            [AgentParam("Name of the transaction to add (case-insensitive match against TransactionName). " +
                        "Example: 'EntityInfo_dbo_orderoo6_customers'", isRequired: true)]
            string transactionName,
            [AgentParam("Display label shown in the navigation menu. Example: 'Customers'", isRequired: true)]
            string menuName)
        {
            try
            {
                var all = AppTransactionBL.RetrieveAllAppTransactionDto(false);
                var tx  = all.FirstOrDefault(t =>
                    string.Equals(t.TransactionName, transactionName, StringComparison.OrdinalIgnoreCase));

                if (tx == null)
                {
                    var available = string.Join(", ", all.Select(t => t.TransactionName).Where(n => n != null).OrderBy(n => n));
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error     = $"Transaction '{transactionName}' not found. Available: {available}"
                    }, Formatting.Indented);
                }

                int? transactionType = tx.TransactionOrganizedType;
                if (transactionType == null ||
                    (transactionType != (int)EmTransactionOrganizedType.List &&
                     transactionType != (int)EmTransactionOrganizedType.FolderList))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error     = $"Transaction '{transactionName}' has type {transactionType} which is not supported. " +
                                    "Only List (3) and FolderList (5) can be added to the navigation menu via this tool."
                    }, Formatting.Indented);
                }

                var result = AppTreeListMenuBL.AddListTransactionToMainMenu((int)tx.Id, menuName);

                bool success = result?.ValidationResult?.HasErrors == false;
                var messages = result?.ValidationResult?.Items?
                    .Select(i => i.Message).ToList();

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess     = success,
                    TransactionId = (int)tx.Id,
                    TransactionName = tx.TransactionName,
                    MenuName      = menuName,
                    Error         = success ? null : (messages != null ? string.Join("; ", messages) : "Unknown error"),
                    Info          = success ? $"'{menuName}' added to navigation menu (transactionId={tx.Id})." : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        private static string ValidateSaasApplicationId(int saasApplicationId)
            => AppBuilderValidation.ValidateSaasApplicationId(saasApplicationId);

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("add_search_to_menu",
            "Step 6 — Add an existing search (or saved search) to the application's navigation menu. " +
            "Always pass saasApplicationId so the item appears under the correct app package in the sidebar. " +
            "Call this after create_search_view.")]
        public string AddSearchToMenu(
            [AgentParam("The ID of the search or saved search to add to the menu", isRequired: true)]
            int searchId,
            [AgentParam("Display label for the menu item, e.g. 'Ticket Overview'", isRequired: true)]
            string menuName,
            [AgentParam("The SaasApplicationId returned by create_app_package. Required to place the item under the correct app.", isRequired: true)]
            int saasApplicationId,
            [AgentParam("Set true if searchId refers to a saved search; false for a regular search")]
            bool isSavedSearch = false)
        {
            try
            {
                var validErr = ValidateSaasApplicationId(saasApplicationId);
                if (validErr != null) return validErr;

                OperationCallResult<object> result;
                result = AppTreeListMenuBL.AddSearchToApplicationMenu(searchId, saasApplicationId, menuName, isSavedSearch);

                bool success = result?.ValidationResult?.HasErrors == false;
                var messages = result?.ValidationResult?.Items?
                    .Select(i => i.Message).ToList();

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = success,
                    Error     = success ? null : (messages != null ? string.Join("; ", messages) : "Unknown error"),
                    Info      = success ? $"'{menuName}' added to navigation menu (saasApplicationId={saasApplicationId})." : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        [AgentTool("list_app_nav_menu",
            "List left-sidebar navigation menu items under one application package (SaasApplicationId). " +
            "Use before update_nav_menu_item / delete_nav_menu_item to see menuId, name, routeCode, link.")]
        public string ListAppNavMenu(
            [AgentParam("Application package id (SaasApplicationId / app root menu id).", isRequired: true)]
            int saasApplicationId)
        {
            try
            {
                var validErr = ValidateSaasApplicationId(saasApplicationId);
                if (validErr != null) return validErr;

                var items = AppTreeListMenuBL.ListApplicationNavMenuItems(saasApplicationId)
                    .OrderBy(m => m.Sort ?? int.MaxValue)
                    .Select(m =>
                    {
                        string kind = "other";
                        int? linkedSearchId = null;
                        int? linkedTransactionId = null;
                        bool? isSavedSearch = null;
                        if (m.RouteCode == ListMenus.MasterDataManagement && !string.IsNullOrWhiteSpace(m.Link))
                        {
                            kind = "search";
                            var parts = m.Link.Split('_');
                            if (int.TryParse(parts[0], out var sid)) linkedSearchId = sid;
                            isSavedSearch = parts.Length > 1 && parts[1] == "1";
                        }
                        else if (m.RouteCode == ListMenus.FormListEdit && int.TryParse(m.Link, out var tid))
                        {
                            kind = "listTransaction";
                            linkedTransactionId = tid;
                        }

                        return new
                        {
                            menuId = m.Id,
                            name = m.Name,
                            routeCode = m.RouteCode,
                            link = m.Link,
                            sort = m.Sort,
                            kind,
                            linkedSearchId,
                            isSavedSearch,
                            linkedTransactionId
                        };
                    }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    IsSuccess = true,
                    saasApplicationId,
                    items
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = ex.Message });
            }
        }

        [AgentTool("update_nav_menu_item",
            "Update one left-sidebar menu node by menuId (from list_app_nav_menu). " +
            "Pass any of name / sort / iconName. Does not change route/link target.")]
        public string UpdateNavMenuItem(
            [AgentParam("AppListMenu.MenuId to update.", isRequired: true)]
            int menuId,
            [AgentParam("New display name.")]
            string name = null,
            [AgentParam("New sort order.")]
            int? sort = null,
            [AgentParam("New icon name.")]
            string iconName = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) && !sort.HasValue && string.IsNullOrWhiteSpace(iconName))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        ok = false,
                        Error = "Provide at least one of: name, sort, iconName."
                    }, Formatting.Indented);
                }

                var dto = AppTreeListMenuBL.RetrieveOneAppListMenuExDto(menuId);
                if (dto == null || dto.Id == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        ok = false,
                        Error = $"menuId={menuId} not found."
                    }, Formatting.Indented);
                }

                if (!string.IsNullOrWhiteSpace(name)) dto.Name = name.Trim();
                if (sort.HasValue) dto.Sort = sort.Value;
                if (!string.IsNullOrWhiteSpace(iconName)) dto.IconName = iconName.Trim();

                var result = AppTreeListMenuBL.SaveOneAppListMenuTreeNode(dto);
                bool success = result?.ValidationResult?.HasErrors == false;
                var messages = result?.ValidationResult?.Items?.Select(i => i.Message).ToList();

                return JsonConvert.SerializeObject(new
                {
                    ok = success,
                    IsSuccess = success,
                    menuId,
                    name = dto.Name,
                    sort = dto.Sort,
                    iconName = dto.IconName,
                    Error = success ? null : (messages != null ? string.Join("; ", messages) : "Unknown error"),
                    Info = success ? $"Menu item {menuId} updated." : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = ex.Message });
            }
        }

        [AgentTool("delete_nav_menu_item",
            "Delete one left-sidebar menu node by menuId (from list_app_nav_menu). " +
            "Works for Search or List shortcuts — only menuId is required. " +
            "Does NOT delete the Search/transaction. Refuses if the node has children. Prefer ask_user before calling.")]
        public string DeleteNavMenuItem(
            [AgentParam("AppListMenu.MenuId to delete.", isRequired: true)]
            int menuId,
            [AgentParam("Optional SaasApplicationId — when set, verifies the item is a direct child of this app.")]
            int? saasApplicationId = null)
        {
            try
            {
                if (saasApplicationId.HasValue && saasApplicationId.Value > 0)
                {
                    var validErr = ValidateSaasApplicationId(saasApplicationId.Value);
                    if (validErr != null) return validErr;

                    var children = AppTreeListMenuBL.ListApplicationNavMenuItems(saasApplicationId.Value);
                    if (!children.Any(c => c.Id != null && Convert.ToInt32(c.Id) == menuId))
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            ok = false,
                            Error = $"menuId={menuId} is not a direct child of SaasApplicationId={saasApplicationId}."
                        }, Formatting.Indented);
                    }
                }

                var result = AppTreeListMenuBL.DeleteOneAppListMenuTreeNode(menuId);
                bool success = result?.ValidationResult?.HasErrors == false;
                var messages = result?.ValidationResult?.Items?.Select(i => i.Message).ToList();

                return JsonConvert.SerializeObject(new
                {
                    ok = success,
                    IsSuccess = success,
                    menuId,
                    Error = success ? null : (messages != null ? string.Join("; ", messages) : "Unknown error"),
                    Info = success ? $"Menu item {menuId} deleted from sidebar." : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = ex.Message });
            }
        }
    }
}
