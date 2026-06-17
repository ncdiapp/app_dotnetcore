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
            PlmExportProgressCallback progressCallback)
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
                }

                var uniqueTabs = tabRows
                    .Where(t => t.ImportStatus != TemplateStatusSkipped)
                    .GroupBy(t => t.TabId)
                    .Select(g => g.First())
                    .ToList();

                int gridTableCount = uniqueTabs
                    .SelectMany(t => t.GridColumns.Select(g => g.TableName))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                progressCallback?.Invoke(8,
                    $"Table plan: 1 root, {uniqueTabs.Count} sibling tab table(s), {gridTableCount} grid table(s).");

                // Phase 1 — physical tables for every tab (DDL only).
                int tabIndex = 0;
                foreach (var tab in uniqueTabs)
                {
                    tabIndex++;
                    int pct = 10 + (int)(45.0 * tabIndex / Math.Max(1, uniqueTabs.Count));
                    progressCallback?.Invoke(pct, $"Creating tables for tab {tab.TabName} ({tabIndex}/{uniqueTabs.Count})…");

                    using (var ddlTran = tenantConn.BeginTransaction())
                    {
                        try
                        {
                            EnsureTabPhysicalTables(tenantConn, ddlTran, tab, rootTable, tablePrefix);
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
                }

                progressCallback?.Invoke(58, "Refreshing tenant table schema cache…");
                RefreshTenantTableSchemaCache(tenantDataSourceId);

                // Phase 2 — AppTransaction per tab (after all tables exist in cache).
                tabIndex = 0;
                foreach (var tab in uniqueTabs)
                {
                    tabIndex++;
                    int pct = 60 + (int)(25.0 * tabIndex / Math.Max(1, uniqueTabs.Count));
                    progressCallback?.Invoke(pct, $"Importing transaction for tab {tab.TabName} ({tabIndex}/{uniqueTabs.Count})…");

                    try
                    {
                        bool isUpdate = TabTransactionExists(tenantConn, null, tab.TabId);
                        int? transactionId = UpsertTabTransaction(
                            tenantConn, null, tab, rootTable, tablePrefix, tenantDataSourceId, saasApplicationId, isUpdate);
                        if (!transactionId.HasValue)
                            throw new InvalidOperationException($"Failed to upsert transaction for tab {tab.TabId}.");

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

                progressCallback?.Invoke(90, "Upserting Data Model Templates…");
                using (var searchTran = tenantConn.BeginTransaction())
                {
                    try
                    {
                        var templates = LoadPlmTemplateHeaders(plmConnectionString);
                        foreach (var template in templates)
                        {
                            UpsertDataModelTemplateSearch(
                                tenantConn, searchTran, template, saasApplicationId, rootTable, tenantDataSourceId);
                            result.TemplatesProcessed++;
                        }
                        searchTran.Commit();
                        result.IsSuccess = true;
                        progressCallback?.Invoke(100, "Template structure import completed.");
                    }
                    catch (Exception ex)
                    {
                        searchTran.Rollback();
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                    }
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
                                IsTemplateHeaderTab = !reader.IsDBNull(5) && reader.GetBoolean(5),
                                IsMasterReferenceHeaderTab = !reader.IsDBNull(6) && reader.GetBoolean(6),
                                IsAllowProductTabCopy = !reader.IsDBNull(7) && reader.GetBoolean(7)
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
SELECT bsi.SubItemID, bsi.SubItemName, bsi.ControlType, bsi.EntityId, bsi.SortOrder,
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
                            SubItemId = reader.GetInt32(0),
                            SubItemName = reader.IsDBNull(1) ? $"SubItem_{reader.GetInt32(0)}" : reader.GetString(1),
                            ControlType = reader.GetInt32(2),
                            EntityId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            SortOrder = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            GridId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            ReferenceStaticFieldId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            Nbdecimal = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7)
                        });
                    }
                }
            }
            return list;
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
                                    TableName = tablePrefix + TemplateSanitizeSqlIdentifier(subItem.SubItemName, 80, "Grid_", subItem.SubItemId)
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
            var siblingColumns = tab.SubItems
                .Where(s => s.ControlType != ControlTypeGrid && s.ControlType != ControlTypeLabel && s.ControlType != ControlTypeEmpty && !s.MapsToRoot)
                .ToList();

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
                if (!TemplateTableExists(conn, tran, gridTable))
                {
                    var sb = new StringBuilder();
                    sb.Append($"CREATE TABLE dbo.[{gridTable}] (RowId int IDENTITY(1,1) NOT NULL PRIMARY KEY, ReferenceId int NOT NULL, Sort int NULL");
                    foreach (var col in gridGroup)
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
                    foreach (var col in gridGroup)
                        TemplateEnsureColumn(conn, tran, gridTable, col.ColumnSqlName, MapControlTypeToSqlType(col.ColumnTypeId, col.Nbdecimal));
                }
            }

            var rootColumns = tab.SubItems.Where(s => s.MapsToRoot).Select(s => s.ColumnName).Distinct();
            foreach (var rootCol in rootColumns)
                TemplateEnsureColumn(conn, tran, rootTable, rootCol, "nvarchar(255) NULL");
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
            var childTables = new List<HierarchyChildTableDto>
            {
                new HierarchyChildTableDto { TableName = tab.SiblingTableName }
            };
            foreach (var gridTable in tab.GridColumns.Select(g => g.TableName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(gridTable, tab.SiblingTableName, StringComparison.OrdinalIgnoreCase))
                    continue;
                childTables.Add(new HierarchyChildTableDto { TableName = gridTable });
            }

            var setup = new HierarchyTableSetupDto
            {
                MasterTableName = rootTable,
                ChildTables = childTables,
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
            SetIntegrationId(conn, tran, "AppTransaction", "TransactionID", txId, $"Tab_{tab.TabId}");
            return txId;
        }

        private static void UpsertDataModelTemplateSearch(
            SqlConnection conn,
            SqlTransaction tran,
            PlmTemplateHeaderRow template,
            int? saasApplicationId,
            string rootTable,
            int tenantDataSourceId)
        {
            string integrationId = $"Template_{template.TemplateId}";
            int? searchId = GetSearchIdByIntegrationId(conn, tran, integrationId);
            if (!searchId.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
INSERT INTO dbo.AppSearch (Name, Description, Type, SaasApplicationID, IntegrationId)
VALUES (@Name, @Description, @Type, @SaasApplicationId, @IntegrationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@Name", template.TemplateName ?? $"Template_{template.TemplateId}");
                    cmd.Parameters.AddWithValue("@Description", (object)template.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Type", (int)EmAppSearchUsageType.DataModelTemplate);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IntegrationId", integrationId);
                    searchId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppSearch SET
    Name = @Name,
    Description = @Description,
    SaasApplicationID = @SaasApplicationId
WHERE SearchId = @SearchId";
                    cmd.Parameters.AddWithValue("@Name", template.TemplateName ?? $"Template_{template.TemplateId}");
                    cmd.Parameters.AddWithValue("@Description", (object)template.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
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
