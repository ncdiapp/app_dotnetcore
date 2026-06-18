using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const string TemplateStatusReady = "Ready";
        private const string TemplateStatusSkipped = "Skipped";
        private const string TemplateStatusBlocked = "Blocked";
        private const string TemplateActionInsert = "Insert";
        private const string TemplateActionUpdate = "Update";
        private const string TemplateTabTypeMain = "MainItem";
        private const string TemplateTabTypeHeader = "TemplateHeader";
        private const string TemplateTabTypeSkipped = "Skipped";
        private const string RootUnitIntegrationId = "Unit_ReferenceBasicInfo";
        private const int ControlTypeGrid = 6;
        private const int ControlTypeLabel = 10;
        private const int ControlTypeEmpty = 17;
        private const int GridTypeRegular = 1;

        private sealed class PlmTemplateHeaderRow
        {
            public int TemplateId { get; set; }
            public string TemplateName { get; set; }
            public string Description { get; set; }
            public int? FolderId { get; set; }
        }

        private sealed class PlmTemplateTabRow
        {
            public int TemplateId { get; set; }
            public string TemplateName { get; set; }
            public int TabId { get; set; }
            public string TabName { get; set; }
            public short? TabSort { get; set; }
            public bool IsTemplateHeaderTab { get; set; }
            public bool IsMasterReferenceHeaderTab { get; set; }
            public bool IsAllowProductTabCopy { get; set; }
            public string TabType { get; set; }
            public string SiblingTableName { get; set; }
            public List<PlmTemplateSubItemRow> SubItems { get; } = new List<PlmTemplateSubItemRow>();
            public List<PlmTemplateGridColumnRow> GridColumns { get; } = new List<PlmTemplateGridColumnRow>();
            public string ImportStatus { get; set; } = TemplateStatusReady;
            public string ImportAction { get; set; }
            public string SkipReason { get; set; }
            public List<PlmTemplateWarningDto> Warnings { get; } = new List<PlmTemplateWarningDto>();
        }

        private sealed class PlmTemplateSubItemRow
        {
            public int TabId { get; set; }
            public int BlockId { get; set; }
            public int SubItemId { get; set; }
            public string SubItemName { get; set; }
            public int ControlType { get; set; }
            public int? EntityId { get; set; }
            public int? SortOrder { get; set; }
            public int? GridId { get; set; }
            public int? ReferenceStaticFieldId { get; set; }
            public int? Nbdecimal { get; set; }
            public string ColumnName { get; set; }
            public bool MapsToRoot { get; set; }
        }

        private sealed class PlmTemplateGridColumnRow
        {
            public int TabId { get; set; }
            public int SubItemId { get; set; }
            public string SubItemName { get; set; }
            public int GridId { get; set; }
            public int GridType { get; set; }
            public int GridColumnId { get; set; }
            public string ColumnName { get; set; }
            public int ColumnTypeId { get; set; }
            public int? EntityId { get; set; }
            public int? ColumnOrder { get; set; }
            public int? Nbdecimal { get; set; }
            public string TableName { get; set; }
            public string ColumnSqlName { get; set; }
        }

        public static OperationCallResult<PlmTemplatePreviewDto> PreviewTemplateMapping(int? sessionId)
        {
            var result = new OperationCallResult<PlmTemplatePreviewDto> { Object = new PlmTemplatePreviewDto() };
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null || string.IsNullOrWhiteSpace(session.PlmConnectionString))
                    throw new InvalidOperationException("PLM connection is not available on this session.");

                var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(GetTenantDataSourceId());
                if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
                    throw new InvalidOperationException("Tenant database connection is not available.");

                string tenantConn = AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);
                var prefixes = ResolveImportPrefixes(session.StepStateJson);
                result.Object = BuildTemplateMappingPreview(
                    session.PlmConnectionString.Trim(),
                    tenantConn,
                    prefixes.TablePrefix);

                if (!result.Object.IsSuccess)
                {
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Template_Preview_Error", ValidationItemType.Error,
                        result.Object.ErrorMessage));
                }
                else if (result.Object.BlockerCount > 0)
                {
                    WriteTemplateIssuesToLog(
                        fixture, sessionId.Value, null, PlmTemplateActionPreview, "Warning",
                        null, result.Object.Blockers, result.Object.Warnings);
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Template_Preview_Blocker", ValidationItemType.Warning,
                        $"{result.Object.BlockerCount} template tab blocker(s) found."));
                }
                else if (result.Object.WarningCount > 0)
                {
                    WriteTemplateIssuesToLog(
                        fixture, sessionId.Value, null, PlmTemplateActionPreview, "Warning",
                        null, null, result.Object.Warnings);
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Preview_Error", ValidationItemType.Error, ex.Message));
            }
            return result;
        }

        public static OperationCallResult<PlmImportJobDto> ExecuteTemplateImport(int? sessionId)
        {
            return StartTemplateImportJob(sessionId);
        }

        internal static PlmTemplatePreviewDto BuildTemplateMappingPreview(
            string plmConnectionString,
            string tenantConnectionString,
            string tablePrefix)
        {
            var preview = new PlmTemplatePreviewDto();
            try
            {
                var tabs = LoadPlmTemplateTabs(plmConnectionString, tablePrefix);
                if (tabs.Count == 0)
                {
                    preview.IsSuccess = false;
                    preview.ErrorMessage = "No PLM templates/tabs were found in pdmTemplate.";
                    return preview;
                }

                using (var tenantConn = new SqlConnection(tenantConnectionString))
                {
                    tenantConn.Open();
                    foreach (var tab in tabs)
                    {
                        ClassifyTemplateTab(tab);
                        if (tab.ImportStatus == TemplateStatusSkipped)
                            continue;

                        ResolveSubItemColumns(tab, tablePrefix);
                        LoadGridColumnsForTab(plmConnectionString, tab, tablePrefix);
                        ValidateTabEntities(tenantConn, tab);
                        AssignTabImportAction(tenantConn, tab);
                    }
                }

                preview.TemplateCount = tabs.Select(t => t.TemplateId).Distinct().Count();
                preview.Tabs = tabs.Select(MapTemplatePreviewItem).ToList();
                preview.ReadyCount = tabs.Count(t => t.ImportStatus == TemplateStatusReady);
                preview.SkippedCount = tabs.Count(t => t.ImportStatus == TemplateStatusSkipped);
                preview.Blockers = tabs
                    .Where(t => t.ImportStatus == TemplateStatusBlocked)
                    .Select(t => new PlmTemplateBlockerDto
                    {
                        PlmTemplateId = t.TemplateId,
                        PlmTabId = t.TabId,
                        PlmTabName = t.TabName,
                        Issue = t.SkipReason
                    })
                    .ToList();
                preview.BlockerCount = preview.Blockers.Count;
                preview.Warnings = tabs.SelectMany(t => t.Warnings).ToList();
                preview.WarningCount = preview.Warnings.Count;
                preview.IsSuccess = true;
            }
            catch (Exception ex)
            {
                preview.IsSuccess = false;
                preview.ErrorMessage = ex.Message;
            }

            return preview;
        }

        internal static PlmTemplateImportResultDto ImportTemplateStructure(
            string plmConnectionString,
            string tenantConnectionString,
            int tenantDataSourceId,
            int? saasApplicationId,
            string tablePrefix,
            PlmExportProgressCallback progressCallback,
            PlmTemplateImportSettingDto importSetting = null)
        {
            var result = new PlmTemplateImportResultDto();
            var preview = BuildTemplateMappingPreview(plmConnectionString, tenantConnectionString, tablePrefix);
            if (!preview.IsSuccess)
            {
                result.IsSuccess = false;
                result.ErrorMessage = preview.ErrorMessage;
                return result;
            }

            result.Blockers = preview.Blockers;
            result.Warnings.AddRange(preview.Warnings);
            var readyTabs = preview.Tabs.Where(t => t.ImportStatus == TemplateStatusReady).ToList();
            result.SkippedTabs = preview.Tabs.Where(t => t.ImportStatus == TemplateStatusSkipped).ToList();
            result.TabsSkipped = result.SkippedTabs.Count;

            if (preview.BlockerCount > 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"{preview.BlockerCount} tab(s) blocked. Resolve blockers before import.";
                return result;
            }

            if (readyTabs.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "No ready template tabs to import.";
                return result;
            }

            string rootTable = tablePrefix + "ReferenceBasicInfo";
            using (var tenantConn = new SqlConnection(tenantConnectionString))
            {
                tenantConn.Open();

                progressCallback?.Invoke(5, "Ensuring ReferenceBasicInfo table…");
                using (var ddlTran = tenantConn.BeginTransaction())
                {
                    try
                    {
                        EnsureReferenceBasicInfoTable(tenantConn, ddlTran, rootTable);
                        ddlTran.Commit();
                    }
                    catch (Exception ex)
                    {
                        ddlTran.Rollback();
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                        return result;
                    }
                }

                if (!TemplateTableExists(tenantConn, null, rootTable))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Failed to create master table {rootTable}.";
                    return result;
                }

                var tabRows = LoadPlmTemplateTabs(plmConnectionString, tablePrefix)
                    .Where(t => t.ImportStatus != TemplateStatusSkipped)
                    .ToList();
                foreach (var tab in tabRows)
                {
                    ClassifyTemplateTab(tab);
                    if (tab.ImportStatus == TemplateStatusSkipped)
                        continue;
                    ResolveSubItemColumns(tab, tablePrefix);
                    LoadGridColumnsForTab(plmConnectionString, tab, tablePrefix);
                    ValidateTabEntities(tenantConn, tab);
                }

                if (importSetting == null)
                {
                    importSetting = new PlmTemplateImportSettingDto
                    {
                        Rows = preview.Tabs.Select(t => new PlmTemplateImportSettingRowDto
                        {
                            PlmTemplateId = t.PlmTemplateId,
                            PlmTabId = t.PlmTabId,
                            TransactionGroupName = t.PlmTemplateName,
                            TransactionName = t.PlmTabName,
                            IntegrationId = $"Tab_{t.PlmTabId}",
                            SiblingTableName = t.SiblingTableName,
                            ImportStatus = t.ImportStatus
                        }).ToList()
                    };
                }

                var uniqueTabs = tabRows
                    .Where(t => t.ImportStatus != TemplateStatusSkipped)
                    .GroupBy(t => t.TabId)
                    .Select(g => g.First())
                    .ToList();

                var executionPlans = BuildTemplateTabExecutionPlans(uniqueTabs, importSetting, tablePrefix);
                result.Warnings.AddRange(uniqueTabs.SelectMany(t => t.Warnings));

                int siblingTableCount = executionPlans
                    .SelectMany(p => p.SiblingColumnsByTable.Keys)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
                int gridTableCount = uniqueTabs
                    .SelectMany(t => t.GridColumns.Select(g => g.TableName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                progressCallback?.Invoke(8,
                    $"Table plan: 1 root, {siblingTableCount} sibling table(s), {gridTableCount} grid table(s).");

                progressCallback?.Invoke(10, "Creating physical tables from import plan…");
                using (var ddlTran = tenantConn.BeginTransaction())
                {
                    try
                    {
                        EnsurePhysicalTablesFromPlans(tenantConn, ddlTran, rootTable, executionPlans, uniqueTabs);
                        ddlTran.Commit();
                    }
                    catch (Exception ex)
                    {
                        ddlTran.Rollback();
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                        return result;
                    }
                }

                progressCallback?.Invoke(58, "Refreshing tenant table schema cache…");
                RefreshTenantTableSchemaCache(tenantDataSourceId);

                int tabIndex = 0;
                foreach (var plan in executionPlans)
                {
                    tabIndex++;
                    int pct = 60 + (int)(18.0 * tabIndex / Math.Max(1, executionPlans.Count));
                    progressCallback?.Invoke(pct, $"Importing transaction for tab {plan.Tab.TabName} ({tabIndex}/{executionPlans.Count})…");

                    try
                    {
                        bool isUpdate = TabTransactionExists(tenantConn, null, plan.Tab.TabId);
                        int? transactionId = UpsertTabTransactionFromPlan(
                            tenantConn, null, plan, rootTable, tablePrefix, tenantDataSourceId, saasApplicationId, isUpdate);
                        if (!transactionId.HasValue)
                            throw new InvalidOperationException($"Failed to upsert transaction for tab {plan.Tab.TabId}.");

                        if (isUpdate)
                            result.TabsUpdated++;
                        else
                            result.TabsInserted++;
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                        return result;
                    }
                }

                progressCallback?.Invoke(78, "Generating default form layouts for tab transactions…");
                try
                {
                    EnsureTabTransactionForms(tenantConn, uniqueTabs, progressCallback);
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = ex.Message;
                    return result;
                }

                var templates = LoadPlmTemplateHeaders(plmConnectionString);
                var tabsByTemplate = tabRows
                    .Where(t => t.ImportStatus != TemplateStatusSkipped)
                    .GroupBy(t => t.TemplateId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<PlmTemplateTabRow>)g.OrderBy(t => t.TabSort ?? short.MaxValue).ThenBy(t => t.TabId).ToList());

                progressCallback?.Invoke(88, "Building Data Model Templates (dataset, view, link targets)…");
                try
                {
                    var templatesToBuild = templates
                        .Where(t => tabsByTemplate.ContainsKey(t.TemplateId))
                        .ToList();
                    int templateIndex = 0;
                    foreach (var template in templatesToBuild)
                    {
                        templateIndex++;
                        var templateTabs = tabsByTemplate[template.TemplateId];
                        BuildCompleteDataModelTemplates(
                            tenantConn, plmConnectionString, template, templateTabs,
                            saasApplicationId, rootTable, tenantDataSourceId,
                            progressCallback, templateIndex, templatesToBuild.Count);
                        result.TemplatesProcessed++;
                    }
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = ex.Message;
                    return result;
                }

                progressCallback?.Invoke(97, "Adding Data Model Templates to main menu…");
                try
                {
                    AddTemplateSearchesToMainMenu(
                        tenantConn,
                        templates.Where(t => tabsByTemplate.ContainsKey(t.TemplateId)),
                        saasApplicationId,
                        progressCallback);
                    result.IsSuccess = true;
                    progressCallback?.Invoke(100, "Template structure import completed.");
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = ex.Message;
                }
            }

            return result;
        }

        private static List<PlmTemplateHeaderRow> LoadPlmTemplateHeaders(string plmConnectionString)
        {
            var list = new List<PlmTemplateHeaderRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT t.TemplateID, t.TemplateName, t.Description, t.FolderId
FROM dbo.pdmTemplate t
ORDER BY t.TemplateID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new PlmTemplateHeaderRow
                            {
                                TemplateId = reader.GetInt32(0),
                                TemplateName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                FolderId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
                            });
                        }
                    }
                }
            }
            return list;
        }

        private static List<PlmTemplateTabRow> LoadPlmTemplateTabs(string plmConnectionString, string tablePrefix)
        {
            var tabs = new List<PlmTemplateTabRow>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT t.TemplateID, t.TemplateName, tt.TabID, tab.TabName, tt.Sort,
       tab.IsTemplateHeaderTab, tab.IsMasterReferenceHeaderTab, tab.IsAllowProductTabCopy
FROM dbo.pdmTemplate t
INNER JOIN dbo.pdmTemplateTab tt ON tt.TemplateID = t.TemplateID
INNER JOIN dbo.pdmTab tab ON tab.TabID = tt.TabID
ORDER BY t.TemplateID, tt.Sort, tt.TabID";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tab = new PlmTemplateTabRow
                            {
                                TemplateId = reader.GetInt32(0),
                                TemplateName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                TabId = reader.GetInt32(2),
                                TabName = reader.IsDBNull(3) ? $"Tab_{reader.GetInt32(2)}" : reader.GetString(3),
                                TabSort = reader.IsDBNull(4) ? (short?)null : reader.GetInt16(4),
                                IsTemplateHeaderTab = ReadPlmSqlBooleanStrict(reader.GetValue(5)),
                                IsMasterReferenceHeaderTab = ReadPlmSqlBooleanStrict(reader.GetValue(6)),
                                IsAllowProductTabCopy = ReadPlmSqlBooleanStrict(reader.GetValue(7))
                            };
                            tab.SiblingTableName = tablePrefix + TemplateSanitizeSqlIdentifier(tab.TabName, 80, "Tab_", tab.TabId);
                            tabs.Add(tab);
                        }
                    }
                }

                foreach (var tab in tabs)
                {
                    tab.SubItems.AddRange(LoadSubItemsForTab(conn, tab.TabId));
                }
            }

            foreach (var tab in tabs)
                ClassifyTemplateTab(tab);

            return tabs;
        }

        private static List<PlmTemplateSubItemRow> LoadSubItemsForTab(SqlConnection plmConn, int tabId)
        {
            var list = new List<PlmTemplateSubItemRow>();
            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT tb.BlockID, bsi.SubItemID, bsi.SubItemName, bsi.ControlType, bsi.EntityId, bsi.SortOrder,
       bsi.GridId, bsi.ReferenceStaticFiledId, bsi.Nbdecimal
FROM dbo.PdmTabBlock tb
INNER JOIN dbo.pdmBlockSubItem bsi ON bsi.BlockID = tb.BlockID
WHERE tb.TabID = @TabId
ORDER BY tb.OrderId, bsi.SortOrder, bsi.SubItemID";
                cmd.Parameters.AddWithValue("@TabId", tabId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PlmTemplateSubItemRow
                        {
                            TabId = tabId,
                            BlockId = reader.GetInt32(0),
                            SubItemId = reader.GetInt32(1),
                            SubItemName = reader.IsDBNull(2) ? $"SubItem_{reader.GetInt32(1)}" : reader.GetString(2),
                            ControlType = reader.GetInt32(3),
                            EntityId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            SortOrder = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            GridId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            ReferenceStaticFieldId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                            Nbdecimal = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8)
                        });
                    }
                }
            }

            return DeduplicateSubItemsBySubItemId(list);
        }

        private static List<PlmTemplateSubItemRow> DeduplicateSubItemsBySubItemId(List<PlmTemplateSubItemRow> items)
        {
            if (items == null || items.Count <= 1)
                return items ?? new List<PlmTemplateSubItemRow>();

            var seen = new HashSet<int>();
            var deduped = new List<PlmTemplateSubItemRow>(items.Count);
            foreach (var item in items)
            {
                if (seen.Add(item.SubItemId))
                    deduped.Add(item);
            }

            return deduped;
        }

        private static List<PlmTemplateSubItemRow> DistinctSubItemsByColumnName(IEnumerable<PlmTemplateSubItemRow> items)
        {
            var dict = new Dictionary<string, PlmTemplateSubItemRow>(StringComparer.OrdinalIgnoreCase);
            if (items == null)
                return new List<PlmTemplateSubItemRow>();

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item?.ColumnName))
                    continue;
                dict[item.ColumnName] = item;
            }

            return dict.Values.ToList();
        }

        private static List<PlmTemplateGridColumnRow> DistinctGridColumnsBySqlName(IEnumerable<PlmTemplateGridColumnRow> columns)
        {
            var dict = new Dictionary<string, PlmTemplateGridColumnRow>(StringComparer.OrdinalIgnoreCase);
            if (columns == null)
                return new List<PlmTemplateGridColumnRow>();

            foreach (var col in columns)
            {
                if (string.IsNullOrWhiteSpace(col?.ColumnSqlName))
                    continue;
                dict[col.ColumnSqlName] = col;
            }

            return dict.Values.ToList();
        }

        private static void LoadGridColumnsForTab(string plmConnectionString, PlmTemplateTabRow tab, string tablePrefix)
        {
            var gridSubItems = tab.SubItems.Where(s => s.ControlType == ControlTypeGrid && s.GridId.HasValue).ToList();
            if (gridSubItems.Count == 0)
                return;

            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                foreach (var subItem in gridSubItems)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
SELECT g.GridType, gmc.GridColumnID, gmc.ColumnName, gmc.ColumnTypeId, gmc.EntityId, gmc.ColumnOrder, gmc.Nbdecimal
FROM dbo.pdmGrid g
INNER JOIN dbo.pdmGridMetaColumn gmc ON gmc.GridID = g.GridID
WHERE g.GridID = @GridId
ORDER BY gmc.ColumnOrder, gmc.GridColumnID";
                        cmd.Parameters.AddWithValue("@GridId", subItem.GridId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int gridType = reader.GetInt32(0);
                                if (gridType != GridTypeRegular)
                                {
                                    tab.Warnings.Add(new PlmTemplateWarningDto
                                    {
                                        PlmTemplateId = tab.TemplateId,
                                        PlmTabId = tab.TabId,
                                        PlmSubItemId = subItem.SubItemId,
                                        Issue = $"Grid type {gridType} is not RegularGrid(1); special behavior deferred."
                                    });
                                }

                                string colName = reader.IsDBNull(2) ? $"Col_{reader.GetInt32(1)}" : reader.GetString(2);
                                var row = new PlmTemplateGridColumnRow
                                {
                                    TabId = tab.TabId,
                                    SubItemId = subItem.SubItemId,
                                    SubItemName = subItem.SubItemName,
                                    GridId = subItem.GridId.Value,
                                    GridType = gridType,
                                    GridColumnId = reader.GetInt32(1),
                                    ColumnName = colName,
                                    ColumnTypeId = reader.GetInt32(3),
                                    EntityId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                                    ColumnOrder = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                    Nbdecimal = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                    TableName = BuildGridTableName(tablePrefix, subItem)
                                };
                                row.ColumnSqlName = TemplateSanitizeSqlIdentifier(colName, 90, "Col_", row.GridColumnId) + "_" + row.GridColumnId;
                                tab.GridColumns.Add(row);
                            }
                        }
                    }
                }
            }
        }

        private static void ClassifyTemplateTab(PlmTemplateTabRow tab)
        {
            if (tab.IsMasterReferenceHeaderTab)
            {
                tab.ImportStatus = TemplateStatusSkipped;
                tab.TabType = TemplateTabTypeSkipped;
                tab.SkipReason = "Master Reference Header tab — skipped in v1.";
                return;
            }

            if (tab.IsAllowProductTabCopy)
            {
                tab.ImportStatus = TemplateStatusSkipped;
                tab.TabType = TemplateTabTypeSkipped;
                tab.SkipReason = "Copy tab — skipped in v1.";
                return;
            }

            tab.TabType = tab.IsTemplateHeaderTab ? TemplateTabTypeHeader : TemplateTabTypeMain;
            tab.ImportStatus = TemplateStatusReady;
        }

        /// <summary>
        /// Reads PLM bit/flag columns strictly: only numeric value 1 or true is treated as true.
        /// Avoids SqlDataReader.GetBoolean() treating any non-zero int as true.
        /// </summary>
        private static bool ReadPlmSqlBooleanStrict(object value)
        {
            if (value == null || value == DBNull.Value)
                return false;
            if (value is bool b)
                return b;
            if (value is byte by)
                return by == 1;
            if (value is short s)
                return s == 1;
            if (value is int i)
                return i == 1;
            if (value is long l)
                return l == 1;
            if (value is string str)
            {
                str = str.Trim();
                if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Per-tab IsTemplateHeaderTab for one PLM template (authoritative source for Template Shared Items).
        /// </summary>
        private static Dictionary<int, bool> LoadPlmTemplateTabHeaderFlags(string plmConnectionString, int templateId)
        {
            var flags = new Dictionary<int, bool>();
            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT tt.TabID, tab.IsTemplateHeaderTab
FROM dbo.pdmTemplateTab tt
INNER JOIN dbo.pdmTab tab ON tab.TabID = tt.TabID
WHERE tt.TemplateID = @TemplateId";
                    cmd.Parameters.AddWithValue("@TemplateId", templateId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            flags[reader.GetInt32(0)] = ReadPlmSqlBooleanStrict(reader.GetValue(1));
                        }
                    }
                }
            }

            return flags;
        }

        /// <summary>
        /// Tab ids that are template shared headers for one PLM template (same rule as PdmTemplateBL.GetAllTempalteTabHedersId).
        /// </summary>
        private static HashSet<int> LoadPlmTemplateHeaderTabIds(string plmConnectionString, int templateId)
        {
            return new HashSet<int>(
                LoadPlmTemplateTabHeaderFlags(plmConnectionString, templateId)
                    .Where(kv => kv.Value)
                    .Select(kv => kv.Key));
        }

        private static void ResolveSubItemColumns(PlmTemplateTabRow tab, string tablePrefix)
        {
            foreach (var subItem in tab.SubItems)
            {
                if (subItem.ControlType == ControlTypeGrid || subItem.ControlType == ControlTypeLabel || subItem.ControlType == ControlTypeEmpty)
                    continue;

                if (TryMapReferenceStaticField(subItem.ReferenceStaticFieldId, out string rootColumn))
                {
                    subItem.MapsToRoot = true;
                    subItem.ColumnName = rootColumn;
                }
                else
                {
                    subItem.ColumnName = TemplateSanitizeSqlIdentifier(subItem.SubItemName, 90, "Sub_", subItem.SubItemId)
                        + "_" + subItem.SubItemId;
                }
            }
        }

        private static bool TryMapReferenceStaticField(int? staticFieldId, out string columnName)
        {
            columnName = null;
            if (!staticFieldId.HasValue)
                return false;

            switch (staticFieldId.Value)
            {
                case 1: columnName = "ReferenceCode"; return true;
                case 2: columnName = "Description"; return true;
                case 3: columnName = "Description2"; return true;
                case 4: columnName = "Image"; return true;
                case 5: columnName = "FolderId"; return true;
                case 6: columnName = "MasterReferenceId"; return true;
                default: return false;
            }
        }

        private static void ValidateTabEntities(SqlConnection tenantConn, PlmTemplateTabRow tab)
        {
            foreach (var subItem in tab.SubItems.Where(s => s.EntityId.HasValue && s.ControlType == 1))
            {
                if (!PlmEntityExistsInApp(tenantConn, null, subItem.EntityId.Value))
                {
                    tab.Warnings.Add(new PlmTemplateWarningDto
                    {
                        PlmTemplateId = tab.TemplateId,
                        PlmTabId = tab.TabId,
                        PlmSubItemId = subItem.SubItemId,
                        Issue = $"PLM EntityId {subItem.EntityId} not imported in AppEntityInfo."
                    });
                }
            }

            foreach (var col in tab.GridColumns.Where(c => c.EntityId.HasValue && c.ColumnTypeId == 1))
            {
                if (!PlmEntityExistsInApp(tenantConn, null, col.EntityId.Value))
                {
                    tab.Warnings.Add(new PlmTemplateWarningDto
                    {
                        PlmTemplateId = tab.TemplateId,
                        PlmTabId = tab.TabId,
                        PlmGridColumnId = col.GridColumnId,
                        Issue = $"PLM EntityId {col.EntityId} not imported in AppEntityInfo."
                    });
                }
            }
        }

        private static void AssignTabImportAction(SqlConnection tenantConn, PlmTemplateTabRow tab)
        {
            if (tab.ImportStatus != TemplateStatusReady)
                return;

            tab.ImportAction = TabTransactionExists(tenantConn, null, tab.TabId)
                ? TemplateActionUpdate
                : TemplateActionInsert;
        }

        private static bool TabTransactionExists(SqlConnection conn, SqlTransaction tran, int tabId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT TOP 1 1 FROM dbo.AppTransaction WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", $"Tab_{tabId}");
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool PlmEntityExistsInApp(SqlConnection conn, SqlTransaction tran, int plmEntityId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT TOP 1 1 FROM dbo.AppEntityInfo WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", plmEntityId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void EnsureReferenceBasicInfoTable(SqlConnection conn, SqlTransaction tran, string tableName)
        {
            if (TemplateTableExists(conn, tran, tableName))
            {
                TemplateEnsureColumn(conn, tran, tableName, "ReferenceCode", "nvarchar(255) NULL");
                TemplateEnsureColumn(conn, tran, tableName, "Description", "nvarchar(255) NULL");
                TemplateEnsureColumn(conn, tran, tableName, "Description2", "nvarchar(255) NULL");
                TemplateEnsureColumn(conn, tran, tableName, "Image", "nvarchar(255) NULL");
                TemplateEnsureColumn(conn, tran, tableName, "MasterReferenceId", "int NULL");
                TemplateEnsureColumn(conn, tran, tableName, "FolderId", "int NULL");
                TemplateEnsureColumn(conn, tran, tableName, "AppCreatedByID", "int NULL");
                TemplateEnsureColumn(conn, tran, tableName, "AppCreatedDate", "datetime NULL");
                TemplateEnsureColumn(conn, tran, tableName, "AppModifiedByID", "int NULL");
                TemplateEnsureColumn(conn, tran, tableName, "AppModifiedDate", "datetime NULL");
                return;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $@"
CREATE TABLE dbo.[{tableName}] (
    ReferenceId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReferenceCode nvarchar(255) NULL,
    Description nvarchar(255) NULL,
    Description2 nvarchar(255) NULL,
    Image nvarchar(255) NULL,
    MasterReferenceId int NULL,
    FolderId int NULL,
    AppCreatedByID int NULL,
    AppCreatedDate datetime NULL,
    AppModifiedByID int NULL,
    AppModifiedDate datetime NULL
)";
                cmd.ExecuteNonQuery();
            }
        }

        private static void EnsureTabPhysicalTables(
            SqlConnection conn, SqlTransaction tran, PlmTemplateTabRow tab, string rootTable, string tablePrefix)
        {
            var siblingColumns = DistinctSubItemsByColumnName(tab.SubItems
                .Where(s => s.ControlType != ControlTypeGrid && s.ControlType != ControlTypeLabel && s.ControlType != ControlTypeEmpty && !s.MapsToRoot));

            if (!TemplateTableExists(conn, tran, tab.SiblingTableName))
            {
                var sb = new StringBuilder();
                sb.Append($"CREATE TABLE dbo.[{tab.SiblingTableName}] (ReferenceId int NOT NULL PRIMARY KEY");
                foreach (var col in siblingColumns)
                    sb.Append($", [{col.ColumnName}] {MapControlTypeToSqlType(col.ControlType, col.Nbdecimal)}");
                sb.Append(")");
                TemplateExecuteSql(conn, tran, sb.ToString());
                TemplateExecuteSql(conn, tran,
                    $"ALTER TABLE dbo.[{tab.SiblingTableName}] ADD CONSTRAINT FK_{tab.SiblingTableName}_Ref FOREIGN KEY (ReferenceId) REFERENCES dbo.[{rootTable}](ReferenceId)");
            }
            else
            {
                TemplateEnsureColumn(conn, tran, tab.SiblingTableName, "ReferenceId", "int NOT NULL");
                foreach (var col in siblingColumns)
                    TemplateEnsureColumn(conn, tran, tab.SiblingTableName, col.ColumnName, MapControlTypeToSqlType(col.ControlType, col.Nbdecimal));
            }

            foreach (var gridGroup in tab.GridColumns.GroupBy(g => g.TableName))
            {
                string gridTable = gridGroup.Key;
                var gridCols = DistinctGridColumnsBySqlName(gridGroup);
                if (!TemplateTableExists(conn, tran, gridTable))
                {
                    var sb = new StringBuilder();
                    sb.Append($"CREATE TABLE dbo.[{gridTable}] (RowId int IDENTITY(1,1) NOT NULL PRIMARY KEY, ReferenceId int NOT NULL, Sort int NULL");
                    foreach (var col in gridCols)
                        sb.Append($", [{col.ColumnSqlName}] {MapControlTypeToSqlType(col.ColumnTypeId, col.Nbdecimal)}");
                    sb.Append(")");
                    TemplateExecuteSql(conn, tran, sb.ToString());
                    TemplateExecuteSql(conn, tran,
                        $"ALTER TABLE dbo.[{gridTable}] ADD CONSTRAINT FK_{gridTable}_Ref FOREIGN KEY (ReferenceId) REFERENCES dbo.[{rootTable}](ReferenceId)");
                }
                else
                {
                    TemplateEnsureColumn(conn, tran, gridTable, "ReferenceId", "int NOT NULL");
                    TemplateEnsureColumn(conn, tran, gridTable, "Sort", "int NULL");
                    foreach (var col in gridCols)
                        TemplateEnsureColumn(conn, tran, gridTable, col.ColumnSqlName, MapControlTypeToSqlType(col.ColumnTypeId, col.Nbdecimal));
                }
            }

            var rootColumns = tab.SubItems.Where(s => s.MapsToRoot).Select(s => s.ColumnName).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var rootCol in rootColumns)
                TemplateEnsureColumn(conn, tran, rootTable, rootCol, "nvarchar(255) NULL");
        }

        private static void EnsurePhysicalTablesFromPlans(
            SqlConnection conn,
            SqlTransaction tran,
            string rootTable,
            List<TemplateTabExecutionPlan> executionPlans,
            List<PlmTemplateTabRow> uniqueTabs)
        {
            var siblingColumnsUnion = new Dictionary<string, Dictionary<string, PlmTemplateSubItemRow>>(StringComparer.OrdinalIgnoreCase);
            var rootColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var plan in executionPlans)
            {
                foreach (var rootSubItem in plan.RootSubItems)
                    rootColumnNames.Add(rootSubItem.ColumnName);

                foreach (var pair in plan.SiblingColumnsByTable)
                {
                    if (!siblingColumnsUnion.TryGetValue(pair.Key, out var cols))
                    {
                        cols = new Dictionary<string, PlmTemplateSubItemRow>(StringComparer.OrdinalIgnoreCase);
                        siblingColumnsUnion[pair.Key] = cols;
                    }

                    foreach (var subItem in pair.Value)
                        cols[subItem.ColumnName] = subItem;
                }
            }

            foreach (var rootCol in rootColumnNames)
                TemplateEnsureColumn(conn, tran, rootTable, rootCol, "nvarchar(255) NULL");

            foreach (var pair in siblingColumnsUnion)
            {
                EnsureSiblingTableColumns(conn, tran, pair.Key, rootTable, DistinctSubItemsByColumnName(pair.Value.Values));
            }

            foreach (var tab in uniqueTabs)
            {
                foreach (var gridGroup in tab.GridColumns.GroupBy(g => g.TableName))
                {
                    string gridTable = gridGroup.Key;
                    var gridCols = DistinctGridColumnsBySqlName(gridGroup);
                    if (!TemplateTableExists(conn, tran, gridTable))
                    {
                        var sb = new StringBuilder();
                        sb.Append($"CREATE TABLE dbo.[{gridTable}] (RowId int IDENTITY(1,1) NOT NULL PRIMARY KEY, ReferenceId int NOT NULL, Sort int NULL");
                        foreach (var col in gridCols)
                            sb.Append($", [{col.ColumnSqlName}] {MapControlTypeToSqlType(col.ColumnTypeId, col.Nbdecimal)}");
                        sb.Append(")");
                        TemplateExecuteSql(conn, tran, sb.ToString());
                        TemplateExecuteSql(conn, tran,
                            $"ALTER TABLE dbo.[{gridTable}] ADD CONSTRAINT FK_{gridTable}_Ref FOREIGN KEY (ReferenceId) REFERENCES dbo.[{rootTable}](ReferenceId)");
                    }
                    else
                    {
                        TemplateEnsureColumn(conn, tran, gridTable, "ReferenceId", "int NOT NULL");
                        TemplateEnsureColumn(conn, tran, gridTable, "Sort", "int NULL");
                        foreach (var col in gridCols)
                            TemplateEnsureColumn(conn, tran, gridTable, col.ColumnSqlName, MapControlTypeToSqlType(col.ColumnTypeId, col.Nbdecimal));
                    }
                }
            }
        }

        private static void EnsureSiblingTableColumns(
            SqlConnection conn,
            SqlTransaction tran,
            string tableName,
            string rootTable,
            List<PlmTemplateSubItemRow> columns)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return;

            columns = DistinctSubItemsByColumnName(columns);

            if (!TemplateTableExists(conn, tran, tableName))
            {
                var sb = new StringBuilder();
                sb.Append($"CREATE TABLE dbo.[{tableName}] (ReferenceId int NOT NULL PRIMARY KEY");
                foreach (var col in columns)
                    sb.Append($", [{col.ColumnName}] {MapControlTypeToSqlType(col.ControlType, col.Nbdecimal)}");
                sb.Append(")");
                TemplateExecuteSql(conn, tran, sb.ToString());
                TemplateExecuteSql(conn, tran,
                    $"ALTER TABLE dbo.[{tableName}] ADD CONSTRAINT FK_{tableName}_Ref FOREIGN KEY (ReferenceId) REFERENCES dbo.[{rootTable}](ReferenceId)");
            }
            else
            {
                TemplateEnsureColumn(conn, tran, tableName, "ReferenceId", "int NOT NULL");
                foreach (var col in columns)
                    TemplateEnsureColumn(conn, tran, tableName, col.ColumnName, MapControlTypeToSqlType(col.ControlType, col.Nbdecimal));
            }
        }

        private static int? UpsertTabTransactionFromPlan(
            SqlConnection conn,
            SqlTransaction tran,
            TemplateTabExecutionPlan plan,
            string rootTable,
            string tablePrefix,
            int tenantDataSourceId,
            int? saasApplicationId,
            bool isUpdate)
        {
            var tab = plan.Tab;
            var siblingTableNames = plan.SiblingColumnsByTable.Keys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (siblingTableNames.Count == 0 && !string.IsNullOrWhiteSpace(plan.PrimarySiblingTable))
                siblingTableNames.Add(plan.PrimarySiblingTable);

            var gridChildTables = new List<HierarchyChildTableDto>();
            foreach (var gridTable in tab.GridColumns.Select(g => g.TableName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (siblingTableNames.Any(s => string.Equals(s, gridTable, StringComparison.OrdinalIgnoreCase)))
                    continue;
                gridChildTables.Add(new HierarchyChildTableDto { TableName = gridTable });
            }

            var setup = new HierarchyTableSetupDto
            {
                MasterTableName = rootTable,
                SiblingTableNames = siblingTableNames,
                ChildTables = gridChildTables,
                DataSourceRegisterId = tenantDataSourceId,
                SchemaOwner = "dbo",
                TransactionName = tab.TabName,
                SaasApplicationId = saasApplicationId
            };

            OperationCallResult<AppTransactionExDto> saveResult;
            int txId;
            if (isUpdate)
            {
                int? existingId = GetTransactionIdByIntegrationId(conn, tran, $"Tab_{tab.TabId}");
                if (!existingId.HasValue)
                    isUpdate = false;
            }

            if (!isUpdate)
            {
                saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(
                    setup, isIgnoreValidation: true, skipPostSaveCacheSync: true);
                if (!saveResult.IsSuccessfulWithResult)
                    throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? $"Failed to create transaction for tab {tab.TabId}.");

                txId = Convert.ToInt32(saveResult.Object.Id);
                ApplyTransactionFieldSubsetSql(conn, tran, txId, rootTable, plan, tab);
            }
            else
            {
                txId = GetTransactionIdByIntegrationId(conn, tran, $"Tab_{tab.TabId}").Value;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransaction SET
    TransactionName = @Name,
    Description = @Description,
    SaasApplicationID = @SaasApplicationId
WHERE TransactionID = @TransactionId";
                    cmd.Parameters.AddWithValue("@Name", tab.TabName);
                    cmd.Parameters.AddWithValue("@Description", tab.TabName + " Data Edit");
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TransactionId", txId);
                    cmd.ExecuteNonQuery();
                }

                ApplyTransactionFieldSubsetSql(conn, tran, txId, rootTable, plan, tab);
            }

            FixTabTransactionUnitStructure(
                conn, tran, txId, rootTable,
                siblingTableNames,
                gridChildTables.Select(g => g.TableName).ToList());

            SetIntegrationId(conn, tran, "AppTransaction", "TransactionID", txId, $"Tab_{tab.TabId}");
            return txId;
        }

        /// <summary>
        /// Ensures tab table units are master-siblings (not root children) and grid units are root children.
        /// Repairs transactions created before sibling/grid split was implemented.
        /// </summary>
        private static void FixTabTransactionUnitStructure(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            string rootTable,
            IReadOnlyList<string> siblingTableNames,
            IReadOnlyList<string> gridTableNames)
        {
            int? rootUnitId = GetTransactionUnitIdByTableName(conn, tran, transactionId, rootTable);
            if (!rootUnitId.HasValue)
                return;

            if (siblingTableNames != null)
            {
                foreach (string siblingTable in siblingTableNames.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;
                        cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit SET
    IsMasterSiblingUnit = 1,
    ParentTransactionUnitID = NULL
WHERE TransactionID = @TransactionId
  AND DataBaseTableName = @TableName";
                        cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                        cmd.Parameters.AddWithValue("@TableName", siblingTable);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (gridTableNames != null)
            {
                foreach (string gridTable in gridTableNames.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tran;
                        cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit SET
    IsMasterSiblingUnit = 0,
    ParentTransactionUnitID = @RootUnitId
WHERE TransactionID = @TransactionId
  AND DataBaseTableName = @TableName";
                        cmd.Parameters.AddWithValue("@RootUnitId", rootUnitId.Value);
                        cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                        cmd.Parameters.AddWithValue("@TableName", gridTable);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static int? GetTransactionUnitIdByTableName(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TransactionId
  AND DataBaseTableName = @TableName
  AND ISNULL(IsMasterSiblingUnit, 0) = 0";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void ApplyTransactionFieldSubsetSql(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            string rootTable,
            TemplateTabExecutionPlan plan,
            PlmTemplateTabRow tab)
        {
            var visibleColumnsByTable = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in plan.SiblingColumnsByTable)
            {
                visibleColumnsByTable[pair.Key] = new HashSet<string>(
                    pair.Value.Select(v => v.ColumnName),
                    StringComparer.OrdinalIgnoreCase);
            }

            var rootColumns = new HashSet<string>(
                plan.RootSubItems.Select(r => r.ColumnName),
                StringComparer.OrdinalIgnoreCase);

            var gridColumns = new HashSet<string>(
                tab.GridColumns.Select(g => g.ColumnSqlName),
                StringComparer.OrdinalIgnoreCase);

            var units = new List<(int UnitId, string TableName)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TransactionUnitID, DataBaseTableName
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TransactionId";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        units.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1)));
                    }
                }
            }

            foreach (var (unitId, unitTable) in units)
            {
                HashSet<string> allowed = null;
                if (!string.IsNullOrWhiteSpace(unitTable))
                {
                    if (visibleColumnsByTable.TryGetValue(unitTable, out var siblingVisible))
                        allowed = siblingVisible;
                    else if (string.Equals(unitTable, rootTable, StringComparison.OrdinalIgnoreCase))
                        allowed = rootColumns;
                    else if (tab.GridColumns.Any(g => string.Equals(g.TableName, unitTable, StringComparison.OrdinalIgnoreCase)))
                        allowed = gridColumns;
                }

                SqlSetUnitFieldVisibility(conn, tran, unitId, allowed);
            }
        }

        private static void SqlSetUnitFieldVisibility(
            SqlConnection conn,
            SqlTransaction tran,
            int unitId,
            HashSet<string> visibleColumnsOrNull)
        {
            using (var hideCmd = conn.CreateCommand())
            {
                hideCmd.Transaction = tran;
                hideCmd.CommandText = @"
UPDATE dbo.AppTransactionField SET IsVisible = 0
WHERE TransactionUnitID = @UnitId
  AND ISNULL(IsPrimaryKey, 0) = 0
  AND ISNULL(IsLinkToParentPrimaryKey, 0) = 0
  AND DataBaseFieldName NOT IN ('ReferenceId', 'Sort')";
                hideCmd.Parameters.AddWithValue("@UnitId", unitId);
                hideCmd.ExecuteNonQuery();
            }

            if (visibleColumnsOrNull == null)
            {
                using (var showCmd = conn.CreateCommand())
                {
                    showCmd.Transaction = tran;
                    showCmd.CommandText = @"
UPDATE dbo.AppTransactionField SET IsVisible = 1
WHERE TransactionUnitID = @UnitId
  AND ISNULL(IsPrimaryKey, 0) = 0
  AND ISNULL(IsLinkToParentPrimaryKey, 0) = 0
  AND DataBaseFieldName NOT IN ('ReferenceId', 'Sort')";
                    showCmd.Parameters.AddWithValue("@UnitId", unitId);
                    showCmd.ExecuteNonQuery();
                }
                return;
            }

            if (visibleColumnsOrNull.Count == 0)
                return;

            var names = visibleColumnsOrNull.ToList();
            using (var showCmd = conn.CreateCommand())
            {
                showCmd.Transaction = tran;
                var inClause = new StringBuilder();
                for (int i = 0; i < names.Count; i++)
                {
                    if (i > 0)
                        inClause.Append(", ");
                    string paramName = "@Col" + i;
                    inClause.Append(paramName);
                    showCmd.Parameters.AddWithValue(paramName, names[i]);
                }

                showCmd.CommandText = $@"
UPDATE dbo.AppTransactionField SET IsVisible = 1
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName IN ({inClause})";
                showCmd.Parameters.AddWithValue("@UnitId", unitId);
                showCmd.ExecuteNonQuery();
            }
        }

        private static void ApplyTransactionFieldSubset(
            AppTransactionExDto transaction,
            TemplateTabExecutionPlan plan,
            PlmTemplateTabRow tab)
        {
            if (transaction?.AppTransactionUnitList == null)
                return;

            var visibleColumnsByTable = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in plan.SiblingColumnsByTable)
            {
                visibleColumnsByTable[pair.Key] = new HashSet<string>(
                    pair.Value.Select(v => v.ColumnName),
                    StringComparer.OrdinalIgnoreCase);
            }

            var rootColumns = new HashSet<string>(
                plan.RootSubItems.Select(r => r.ColumnName),
                StringComparer.OrdinalIgnoreCase);

            var gridColumns = new HashSet<string>(
                tab.GridColumns.Select(g => g.ColumnSqlName),
                StringComparer.OrdinalIgnoreCase);

            void ApplyToUnits(IEnumerable<AppTransactionUnitExDto> units, string tableName, HashSet<string> visible)
            {
                if (units == null)
                    return;

                foreach (var unit in units)
                {
                    string unitTable = unit.DataBaseTableName;
                    HashSet<string> allowed = visible;
                    if (!string.IsNullOrWhiteSpace(unitTable))
                    {
                        if (visibleColumnsByTable.TryGetValue(unitTable, out var siblingVisible))
                            allowed = siblingVisible;
                        else if (string.Equals(unitTable, transaction.AppTransactionUnitList.FirstOrDefault()?.DataBaseTableName, StringComparison.OrdinalIgnoreCase))
                            allowed = rootColumns;
                        else if (tab.GridColumns.Any(g => string.Equals(g.TableName, unitTable, StringComparison.OrdinalIgnoreCase)))
                            allowed = gridColumns;
                    }

                    if (unit.AppTransactionFieldList != null)
                    {
                        foreach (var field in unit.AppTransactionFieldList)
                        {
                            if (field.IsPrimaryKey == true)
                                continue;
                            if (string.Equals(field.DataBaseFieldName, "ReferenceId", StringComparison.OrdinalIgnoreCase))
                                continue;
                            if (string.Equals(field.DataBaseFieldName, "Sort", StringComparison.OrdinalIgnoreCase))
                                continue;

                            bool visibleField = allowed == null
                                || allowed.Contains(field.DataBaseFieldName);
                            field.IsVisible = visibleField;
                        }
                    }

                    ApplyToUnits(unit.Children, unitTable, visible);
                }
            }

            ApplyToUnits(transaction.AppTransactionUnitList, null, null);
        }

        private static int? UpsertTabTransaction(
            SqlConnection conn,
            SqlTransaction tran,
            PlmTemplateTabRow tab,
            string rootTable,
            string tablePrefix,
            int tenantDataSourceId,
            int? saasApplicationId,
            bool isUpdate)
        {
            // Grid tables FK to ReferenceBasicInfo (root), not the sibling tab table.
            // CreateHierarchyTransactionFromTables only links direct children to root; grandchildren
            // would be linked to the sibling PK and break DictCurrentPKOrFKLinkToParentKeyGuidMap on save.
            var gridChildTables = new List<HierarchyChildTableDto>();
            foreach (var gridTable in tab.GridColumns.Select(g => g.TableName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(gridTable, tab.SiblingTableName, StringComparison.OrdinalIgnoreCase))
                    continue;
                gridChildTables.Add(new HierarchyChildTableDto { TableName = gridTable });
            }

            var setup = new HierarchyTableSetupDto
            {
                MasterTableName = rootTable,
                SiblingTableNames = new List<string> { tab.SiblingTableName },
                ChildTables = gridChildTables,
                DataSourceRegisterId = tenantDataSourceId,
                SchemaOwner = "dbo",
                TransactionName = tab.TabName,
                SaasApplicationId = saasApplicationId
            };

            OperationCallResult<AppTransactionExDto> saveResult;
            if (isUpdate)
            {
                int? existingId = GetTransactionIdByIntegrationId(conn, tran, $"Tab_{tab.TabId}");
                if (!existingId.HasValue)
                    isUpdate = false;
            }

            if (!isUpdate)
            {
                saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(setup);
                if (!saveResult.IsSuccessfulWithResult)
                    throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? $"Failed to create transaction for tab {tab.TabId}.");
            }
            else
            {
                int transactionId = GetTransactionIdByIntegrationId(conn, tran, $"Tab_{tab.TabId}").Value;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransaction SET
    TransactionName = @Name,
    Description = @Description,
    SaasApplicationID = @SaasApplicationId
WHERE TransactionID = @TransactionId";
                    cmd.Parameters.AddWithValue("@Name", tab.TabName);
                    cmd.Parameters.AddWithValue("@Description", tab.TabName + " Data Edit");
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                    cmd.ExecuteNonQuery();
                }
                saveResult = new OperationCallResult<AppTransactionExDto>
                {
                    Object = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId)
                };
            }

            int txId = Convert.ToInt32(saveResult.Object.Id);
            FixTabTransactionUnitStructure(
                conn, tran, txId, rootTable,
                new List<string> { tab.SiblingTableName },
                gridChildTables.Select(g => g.TableName).ToList());
            SetIntegrationId(conn, tran, "AppTransaction", "TransactionID", txId, $"Tab_{tab.TabId}");
            return txId;
        }

        private static int? GetTransactionIdByIntegrationId(SqlConnection conn, SqlTransaction tran, string integrationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT TransactionID FROM dbo.AppTransaction WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", integrationId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetSearchIdByIntegrationId(SqlConnection conn, SqlTransaction tran, string integrationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT SearchId FROM dbo.AppSearch WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", integrationId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void SetIntegrationId(SqlConnection conn, SqlTransaction tran, string table, string pkColumn, int id, string integrationId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = $"UPDATE dbo.[{table}] SET IntegrationId = @IntegrationId WHERE [{pkColumn}] = @Id";
                cmd.Parameters.AddWithValue("@IntegrationId", integrationId);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static void RefreshTenantTableSchemaCache(int tenantDataSourceId)
        {
            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(tenantDataSourceId);
        }

        private static bool TemplateTableExists(SqlConnection conn, SqlTransaction tran, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME=@Table";
                cmd.Parameters.AddWithValue("@Table", tableName);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void TemplateEnsureColumn(SqlConnection conn, SqlTransaction tran, string tableName, string columnName, string sqlType)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME=@Table AND COLUMN_NAME=@Column)
    ALTER TABLE dbo.[" + tableName + "] ADD [" + columnName + "] " + sqlType;
                cmd.Parameters.AddWithValue("@Table", tableName);
                cmd.Parameters.AddWithValue("@Column", columnName);
                cmd.ExecuteNonQuery();
            }
        }

        private static void TemplateExecuteSql(SqlConnection conn, SqlTransaction tran, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private static string MapControlTypeToSqlType(int controlType, int? nbdecimal)
        {
            switch (controlType)
            {
                case 1:
                case 23:
                    return "int NULL";
                case 20:
                    return nbdecimal.HasValue && nbdecimal.Value > 0
                        ? $"decimal(18, {Math.Min(nbdecimal.Value, 18)}) NULL"
                        : "int NULL";
                case 13:
                    return "bit NULL";
                case 7:
                case 27:
                    return "datetime NULL";
                default:
                    return "nvarchar(255) NULL";
            }
        }

        private static PlmTemplatePreviewItemDto MapTemplatePreviewItem(PlmTemplateTabRow tab)
        {
            int siblingFields = tab.SubItems.Count(s =>
                s.ControlType != ControlTypeGrid && s.ControlType != ControlTypeLabel && s.ControlType != ControlTypeEmpty && !s.MapsToRoot);
            return new PlmTemplatePreviewItemDto
            {
                PlmTemplateId = tab.TemplateId,
                PlmTemplateName = tab.TemplateName,
                PlmTabId = tab.TabId,
                PlmTabName = tab.TabName,
                TabType = tab.TabType,
                ImportStatus = tab.ImportStatus,
                ImportAction = tab.ImportAction,
                SiblingTableName = tab.SiblingTableName,
                ChildTableNames = string.Join(", ", tab.GridColumns.Select(g => g.TableName).Distinct()),
                SiblingFieldCount = siblingFields,
                GridFieldCount = tab.GridColumns.Count,
                WarningCount = tab.Warnings.Count,
                SkipReason = tab.SkipReason
            };
        }

        private static string BuildGridTableName(string tablePrefix, PlmTemplateSubItemRow subItem)
        {
            string prefix = tablePrefix ?? string.Empty;
            string suffix = "_" + subItem.SubItemId;
            const int sqlIdentifierMaxLength = 128;
            int maxSanitizedLength = Math.Max(1, sqlIdentifierMaxLength - prefix.Length - suffix.Length);
            string sanitized = TemplateSanitizeSqlIdentifier(
                subItem.SubItemName,
                maxSanitizedLength,
                "Grid",
                subItem.SubItemId);
            return prefix + sanitized + suffix;
        }

        private static string TemplateSanitizeSqlIdentifier(string input, int maxLength, string fallbackPrefix, int fallbackId)
        {
            if (string.IsNullOrWhiteSpace(input))
                return TemplateTruncate($"{fallbackPrefix}{fallbackId}", maxLength);

            var sb = new StringBuilder();
            foreach (char ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
                else if (sb.Length == 0 || sb[sb.Length - 1] != '_')
                    sb.Append('_');
            }

            string result = sb.ToString().Trim('_');
            if (result.Length == 0)
                result = $"{fallbackPrefix}{fallbackId}";
            if (char.IsDigit(result[0]))
                result = "T_" + result;
            return TemplateTruncate(result, maxLength);
        }

        private static string TemplateTruncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength);
        }

        internal static void WriteTemplateIssuesToLog(
            DatabaseFixture fixture,
            int sessionId,
            int? jobId,
            string action,
            string status,
            IEnumerable<PlmTemplatePreviewItemDto> skipped,
            IEnumerable<PlmTemplateBlockerDto> blockers,
            IEnumerable<PlmTemplateWarningDto> warnings)
        {
            if (skipped != null)
            {
                foreach (var item in skipped)
                {
                    WriteImportLog(fixture, sessionId, jobId, StepTemplate, action, status,
                        item.PlmTabName, item.PlmTabId.ToString(), null, null,
                        $"Skipped: {item.SkipReason}");
                }
            }

            if (blockers != null)
            {
                foreach (var blocker in blockers)
                {
                    WriteImportLog(fixture, sessionId, jobId, StepTemplate, action, status,
                        blocker.PlmTabName, blocker.PlmTabId?.ToString(), null, null,
                        blocker.Issue);
                }
            }

            if (warnings != null)
            {
                foreach (var warning in warnings)
                {
                    WriteImportLog(fixture, sessionId, jobId, StepTemplate, action, status,
                        warning.PlmTabId?.ToString(), warning.PlmSubItemId?.ToString() ?? warning.PlmGridColumnId?.ToString(),
                        null, null, warning.Issue);
                }
            }
        }
    }
}
