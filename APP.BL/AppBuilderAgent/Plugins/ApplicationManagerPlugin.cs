using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.DbGenie;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;

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

        [AgentFunction("create_app_package",
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

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess        = true,
                    SaasApplicationId = result.Object.Value,
                    ApplicationName  = applicationName,
                    Note             = "Pass SaasApplicationId to create_entity_simple_list, create_entity_from_table, and create_application."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        [AgentFunction("delete_application",
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

        [AgentFunction("add_transaction_to_menu",
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

        [AgentFunction("add_search_to_menu",
            "Step 6 — Add an existing search (or saved search) to the application's main navigation menu. " +
            "Call this after create_search_view if the search was not automatically added to the menu.")]
        public string AddSearchToMenu(
            [AgentParam("The ID of the search or saved search to add to the menu", isRequired: true)]
            int searchId,
            [AgentParam("Display label for the menu item, e.g. 'Sales Orders'", isRequired: true)]
            string menuName,
            [AgentParam("Set true if searchId refers to a saved search; false for a regular search")]
            bool isSavedSearch = false)
        {
            try
            {
                var result = AppTreeListMenuBL.AddSearchToMainMenu(searchId, menuName, isSavedSearch);

                bool success = result?.ValidationResult?.HasErrors == false;
                var messages = result?.ValidationResult?.Items?
                    .Select(i => i.Message).ToList();

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = success,
                    Error     = success ? null : (messages != null ? string.Join("; ", messages) : "Unknown error"),
                    Info      = success ? $"'{menuName}' added to navigation menu." : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }
    }
}
