using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    public class PlatformExplorerPlugin
    {
        private readonly int? _dataSourceId;

        public PlatformExplorerPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        [AgentFunction("explore_platform",
            "Get a combined overview of existing application packages, transactions (data models), " +
            "entity data sources, and database tables. " +
            "Call this FIRST to understand what already exists before building anything.")]
        public string ExplorePlatform()
        {
            try
            {
                var applications = AppSaasUserApplicationPackageBL.GetSaasApplicationList()
                    .Select(a => new { a.Id, a.Name })
                    .ToList();

                var transactions = AppTransactionBL.RetrieveAllAppTransactionDto(false)
                    .Select(t => new { t.Id, Name = t.TransactionName, t.SaasApplicationId })
                    .ToList();

                var entities = AppEntityInfoBL.RetrieveAllAppEntityInfoDto()
                    .Select(e => new
                    {
                        e.Id,
                        e.EntityCode,
                        EntityType        = e.EntityType == 4 ? "SimpleList" : e.EntityType == 1 ? "DatabaseTable" : $"Type{e.EntityType}",
                        e.TableName,
                        e.SaasApplicationId
                    })
                    .ToList();

                var tables = AppMetaDataBL.GetSaasDataSourceTableAndViewList(_dataSourceId, null, null)
                    .OrderBy(t => t.SchemaOwner).ThenBy(t => t.Name)
                    .Select(t => new { t.Name, t.SchemaOwner })
                    .Take(200)
                    .ToList();

                // Return compact (no indentation) — detailed search is available via search_platform().
                return JsonConvert.SerializeObject(new
                {
                    ExistingApplications   = applications,
                    ApplicationCount       = applications.Count,
                    ExistingTransactions   = transactions,
                    TransactionCount       = transactions.Count,
                    ExistingEntitySources  = entities,
                    EntitySourceCount      = entities.Count,
                    ExistingDatabaseTables = tables,
                    TableCount             = tables.Count
                }, Formatting.None);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }

        [AgentFunction("search_platform",
            "Search existing platform items by keyword: applications, transactions, entity data sources, " +
            "and database tables whose name contains the query string (case-insensitive). " +
            "Use this instead of explore_platform when you only need to find a specific item by name. " +
            "Returns up to 20 matches per category.")]
        public string SearchPlatform(
            [AgentParam("Keyword to search for — matched against names of apps, transactions, entities, and tables.", true)]
            string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return JsonConvert.SerializeObject(new { Error = "query is required" });

                var q = query.Trim();

                var apps = AppSaasUserApplicationPackageBL.GetSaasApplicationList()
                    .Where(a => a.Name != null && a.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(20)
                    .Select(a => new { a.Id, a.Name })
                    .ToList<object>();

                var transactions = AppTransactionBL.RetrieveAllAppTransactionDto(false)
                    .Where(t => t.TransactionName != null && t.TransactionName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(20)
                    .Select(t => new { t.Id, Name = t.TransactionName, t.SaasApplicationId })
                    .ToList<object>();

                var entities = AppEntityInfoBL.RetrieveAllAppEntityInfoDto()
                    .Where(e => (e.EntityCode != null && e.EntityCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                             || (e.TableName  != null && e.TableName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Take(20)
                    .Select(e => new { e.Id, e.EntityCode, e.TableName, e.SaasApplicationId })
                    .ToList<object>();

                var tables = AppMetaDataBL.GetSaasDataSourceTableAndViewList(_dataSourceId, null, null)
                    .Where(t => t.Name != null && t.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(20)
                    .Select(t => new { t.Name, t.SchemaOwner })
                    .ToList<object>();

                return JsonConvert.SerializeObject(new
                {
                    Query              = q,
                    Applications       = apps,
                    Transactions       = transactions,
                    EntitySources      = entities,
                    DatabaseTables     = tables
                }, Formatting.None);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }

        [AgentFunction("list_applications",
            "Return the full child tree of every application package: " +
            "transactions (with field count and table name) and search screens. " +
            "Use this for modification requests — it tells you exactly what exists under each app " +
            "so you can target the right transaction ID for update_transaction_field, set_field_entity, or delete_transaction. " +
            "More detailed than explore_platform for modification work.")]
        public string ListApplications()
        {
            try
            {
                var apps         = AppSaasUserApplicationPackageBL.GetSaasApplicationList();
                var allTrans     = AppTransactionBL.RetrieveAllAppTransactionDto(false);

                var result = new List<object>();
                foreach (var app in apps)
                {
                    var appId = (int)app.Id;

                    // Transactions belonging to this app
                    var appTrans = allTrans
                        .Where(t => t.SaasApplicationId == appId)
                        .Select(t => new
                        {
                            TransactionId   = (int)t.Id,
                            Name            = t.TransactionName
                        })
                        .ToList();

                    // Searches belonging to this app
                    List<object> searches;
                    try
                    {
                        searches = AppSearchConfigBL.RetrieveSaasApplicationSearchList(appId)
                            .Select(s => (object)new
                            {
                                SearchId  = (int)s.Id,
                                Name      = s.Name,
                                DataSetId = s.DataSetId
                            })
                            .ToList();
                    }
                    catch
                    {
                        searches = new List<object>();
                    }

                    result.Add(new
                    {
                        ApplicationId   = appId,
                        ApplicationName = app.Name,
                        Transactions    = appTrans,
                        TransactionCount = appTrans.Count,
                        Searches        = searches,
                        SearchCount     = searches.Count
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    ApplicationCount = result.Count,
                    Applications     = result
                }, Formatting.None);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }

        [AgentFunction("get_database_tables",
            "List all tables and views in the target database.")]
        public string GetDatabaseTables()
        {
            try
            {
                var tables = AppMetaDataBL.GetSaasDataSourceTableAndViewList(_dataSourceId, null, null)
                    .OrderBy(t => t.SchemaOwner).ThenBy(t => t.Name)
                    .Select(t => new { t.Name, t.SchemaOwner, IsView = t.IsDbView })
                    .ToList();
                return JsonConvert.SerializeObject(tables, Formatting.None);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }

        [AgentFunction("get_existing_transactions",
            "List all configured transaction units (data models / screens) in the application.")]
        public string GetExistingTransactions()
        {
            try
            {
                var list = AppTransactionBL.RetrieveAllAppTransactionDto(false)
                    .Select(t => new { t.Id, Name = t.TransactionName })
                    .ToList();
                return JsonConvert.SerializeObject(list, Formatting.None);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }

        [AgentFunction("get_transaction_details",
            "Get the full configuration of a transaction, including all units, fields, and search views.")]
        public string GetTransactionDetails(
            [AgentParam("The transaction ID to inspect", isRequired: true)] int transactionId)
        {
            try
            {
                // Force a fresh DB fetch — avoids OutOfSync entities left by create_application
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                var transExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                if (transExDto == null)
                    return JsonConvert.SerializeObject(new { Error = $"Transaction {transactionId} not found" });

                var units = transExDto.AppTransactionUnitList?.Select(u => new
                {
                    UnitId      = Convert.ToInt32(u.Id),
                    Name        = u.UnitDisplayName,
                    TableName   = u.DataBaseTableName,
                    u.SchemaOwner,
                    FieldCount      = u.AppTransactionFieldList?.Count ?? 0,
                    Fields          = u.AppTransactionFieldList?
                        .Select(f => new { FieldId = Convert.ToInt32(f.Id), f.DisplayName, ColumnName = f.DataBaseFieldName, f.ControlType, f.EntityId }).ToList(),
                    SearchViewCount = u.AppSearchViewList?.Count ?? 0
                }).ToList();

                return JsonConvert.SerializeObject(new
                {
                    TransactionId = Convert.ToInt32(transExDto.Id),
                    Name          = transExDto.TransactionName,
                    UnitCount     = units?.Count ?? 0,
                    Units         = units
                }, Formatting.None);
            }
            catch (Exception ex) { return JsonConvert.SerializeObject(new { Error = ex.Message }); }
        }
    }
}
