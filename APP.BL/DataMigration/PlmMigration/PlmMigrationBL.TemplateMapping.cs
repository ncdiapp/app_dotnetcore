using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private const double TemplateMappingJaccardThreshold = 0.80;
        private const string BlockStorageRoot = "Root";
        private const string BlockStorageSharedSibling = "SharedSibling";

        private sealed class TemplateTabExecutionPlan
        {
            public PlmTemplateTabRow Tab { get; set; }
            public string PrimarySiblingTable { get; set; }
            public Dictionary<string, List<PlmTemplateSubItemRow>> SiblingColumnsByTable { get; } =
                new Dictionary<string, List<PlmTemplateSubItemRow>>(StringComparer.OrdinalIgnoreCase);
            public List<PlmTemplateSubItemRow> RootSubItems { get; } = new List<PlmTemplateSubItemRow>();
        }

        public static OperationCallResult<PlmTemplateMappingGridDto> GetTemplateTabMappingGrid(int? sessionId)
        {
            var result = new OperationCallResult<PlmTemplateMappingGridDto> { Object = new PlmTemplateMappingGridDto() };
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
                var preview = BuildTemplateMappingPreview(session.PlmConnectionString.Trim(), tenantConn, prefixes.TablePrefix);
                if (!preview.IsSuccess)
                {
                    result.Object.IsSuccess = false;
                    result.Object.ErrorMessage = preview.ErrorMessage;
                    result.ValidationResult.Items.Add(new ValidationItem(
                        typeof(PlmMigrationBL), "Plm_Template_Grid_Error", ValidationItemType.Error, preview.ErrorMessage));
                    return result;
                }

                var analysis = AnalyzeTemplateMapping(
                    session.PlmConnectionString.Trim(),
                    preview.Tabs,
                    prefixes.TablePrefix);

                result.Object = BuildMappingGridDto(preview, analysis);
                result.Object.SavedSetting = LoadTemplateImportSetting(session.StepStateJson)
                    ?? BuildDefaultImportSetting(result.Object.Rows);
                result.Object.IsSuccess = true;

                if (result.Object.BlockerCount > 0)
                {
                    WriteTemplateIssuesToLog(fixture, sessionId.Value, null, PlmTemplateActionPreview, "Warning",
                        null, result.Object.Blockers, result.Object.Warnings);
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Grid_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmTemplateImportSettingDto> SaveTemplateMapping(int? sessionId, PlmTemplateImportSettingDto setting)
        {
            var result = new OperationCallResult<PlmTemplateImportSettingDto>();
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");
                if (setting == null)
                    throw new ArgumentException("Import setting is required.");

                int companyId = ResolveCompanyId(null);
                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null)
                    throw new InvalidOperationException("Import session not found.");

                var validation = ValidateTemplateImportSettingInternal(
                    session.PlmConnectionString?.Trim(),
                    session.StepStateJson,
                    setting,
                    strict: false);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                PersistTemplateImportSetting(fixture, sessionId.Value, companyId, setting);
                result.Object = setting;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Save_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmTemplateMappingValidationDto> ValidateTemplateMapping(int? sessionId, PlmTemplateImportSettingDto setting)
        {
            var result = new OperationCallResult<PlmTemplateMappingValidationDto>
            {
                Object = new PlmTemplateMappingValidationDto { IsValid = true }
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (!sessionId.HasValue || sessionId.Value <= 0)
                    throw new ArgumentException("SessionId is required.");

                var fixture = GetTenantFixture();
                var session = LoadSessionById(fixture, sessionId.Value, includeConnection: true);
                if (session == null)
                    throw new InvalidOperationException("Import session not found.");

                var effectiveSetting = setting ?? LoadTemplateImportSetting(session.StepStateJson);
                if (effectiveSetting == null)
                    throw new InvalidOperationException("No template import setting found. Run Analyze and Save first.");

                result.Object = ValidateTemplateImportSettingInternal(
                    session.PlmConnectionString?.Trim(),
                    session.StepStateJson,
                    effectiveSetting,
                    strict: true);
                result.Object.IsValid = result.Object.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Object.IsValid = false;
                result.Object.Errors.Add(ex.Message);
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_Template_Validate_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private sealed class TemplateMappingAnalysis
        {
            public List<PlmTemplateBlockAnalysisDto> Blocks { get; } = new List<PlmTemplateBlockAnalysisDto>();
            public List<PlmTemplateSimilarTabGroupDto> SimilarTabGroups { get; } = new List<PlmTemplateSimilarTabGroupDto>();
            public Dictionary<int, string> SimilarTabGroupByTabId { get; } = new Dictionary<int, string>();
            public HashSet<int> MultiReferencedBlockIds { get; } = new HashSet<int>();
            public HashSet<int> TabsWithMultiReferencedBlocks { get; } = new HashSet<int>();
        }

        private static TemplateMappingAnalysis AnalyzeTemplateMapping(
            string plmConnectionString,
            List<PlmTemplatePreviewItemDto> previewTabs,
            string tablePrefix)
        {
            var analysis = new TemplateMappingAnalysis();
            var readyTabIds = previewTabs
                .Where(t => t.ImportStatus == TemplateStatusReady)
                .Select(t => t.PlmTabId)
                .Distinct()
                .ToList();
            if (readyTabIds.Count == 0)
                return analysis;

            var tabBlocks = LoadTabBlockMap(plmConnectionString, readyTabIds);
            var blockNames = LoadBlockNames(plmConnectionString, tabBlocks.Values.SelectMany(v => v).Distinct());

            var blockTabRefs = new Dictionary<int, HashSet<int>>();
            foreach (var pair in tabBlocks)
            {
                foreach (int blockId in pair.Value)
                {
                    if (!blockTabRefs.TryGetValue(blockId, out var tabs))
                    {
                        tabs = new HashSet<int>();
                        blockTabRefs[blockId] = tabs;
                    }
                    tabs.Add(pair.Key);
                }
            }

            foreach (var pair in blockTabRefs.Where(p => p.Value.Count > 1).OrderBy(p => p.Key))
            {
                analysis.MultiReferencedBlockIds.Add(pair.Key);
                foreach (int tabId in pair.Value)
                    analysis.TabsWithMultiReferencedBlocks.Add(tabId);
                var labels = pair.Value
                    .Select(tabId =>
                    {
                        var row = previewTabs.FirstOrDefault(t => t.PlmTabId == tabId);
                        return row == null ? $"Tab {tabId}" : $"{row.PlmTemplateName} / {row.PlmTabName}";
                    })
                    .Distinct()
                    .ToList();

                analysis.Blocks.Add(new PlmTemplateBlockAnalysisDto
                {
                    BlockId = pair.Key,
                    BlockName = blockNames.TryGetValue(pair.Key, out string name) ? name : $"Block_{pair.Key}",
                    ReferencedTabCount = pair.Value.Count,
                    ReferencedTabLabels = labels
                });
            }

            var templateIds = previewTabs
                .Where(t => t.ImportStatus == TemplateStatusReady)
                .Select(t => t.PlmTemplateId)
                .Distinct();

            foreach (int templateId in templateIds)
            {
                var tabsInTemplate = previewTabs
                    .Where(t => t.PlmTemplateId == templateId && t.ImportStatus == TemplateStatusReady)
                    .GroupBy(t => t.PlmTabId)
                    .Select(g => g.First())
                    .ToList();

                for (int i = 0; i < tabsInTemplate.Count; i++)
                {
                    for (int j = i + 1; j < tabsInTemplate.Count; j++)
                    {
                        int tabA = tabsInTemplate[i].PlmTabId;
                        int tabB = tabsInTemplate[j].PlmTabId;
                        if (!tabBlocks.TryGetValue(tabA, out var setA) || !tabBlocks.TryGetValue(tabB, out var setB))
                            continue;

                        double jaccard = ComputeJaccard(setA, setB);
                        if (jaccard < TemplateMappingJaccardThreshold)
                            continue;

                        string groupId = $"sim_{templateId}_{Math.Min(tabA, tabB)}_{Math.Max(tabA, tabB)}";
                        string suggestedName = tablePrefix + TemplateSanitizeSqlIdentifier(
                            $"{tabsInTemplate[i].PlmTabName}_{tabsInTemplate[j].PlmTabName}",
                            80,
                            "SharedTab_",
                            tabA);

                        analysis.SimilarTabGroups.Add(new PlmTemplateSimilarTabGroupDto
                        {
                            GroupId = groupId,
                            PlmTemplateId = templateId,
                            SuggestedSharedTableName = suggestedName,
                            JaccardScore = Math.Round(jaccard, 4),
                            TabIds = new List<int> { tabA, tabB },
                            TabLabels = new List<string>
                            {
                                tabsInTemplate[i].PlmTabName,
                                tabsInTemplate[j].PlmTabName
                            }
                        });

                        analysis.SimilarTabGroupByTabId[tabA] = groupId;
                        analysis.SimilarTabGroupByTabId[tabB] = groupId;
                    }
                }
            }

            return analysis;
        }

        private static PlmTemplateMappingGridDto BuildMappingGridDto(
            PlmTemplatePreviewDto preview,
            TemplateMappingAnalysis analysis)
        {
            var grid = new PlmTemplateMappingGridDto
            {
                TemplateCount = preview.TemplateCount,
                ReadyCount = preview.ReadyCount,
                SkippedCount = preview.SkippedCount,
                BlockerCount = preview.BlockerCount,
                WarningCount = preview.WarningCount,
                Blockers = preview.Blockers,
                Warnings = preview.Warnings,
                Blocks = analysis.Blocks,
                SimilarTabGroups = analysis.SimilarTabGroups
            };

            foreach (var tab in preview.Tabs)
            {
                bool showWarning = tab.ImportStatus == TemplateStatusReady
                    && (analysis.SimilarTabGroupByTabId.ContainsKey(tab.PlmTabId)
                        || analysis.TabsWithMultiReferencedBlocks.Contains(tab.PlmTabId));

                grid.Rows.Add(new PlmTemplateMappingGridRowDto
                {
                    PlmTemplateId = tab.PlmTemplateId,
                    PlmTemplateName = tab.PlmTemplateName,
                    PlmTabId = tab.PlmTabId,
                    PlmTabName = tab.PlmTabName,
                    TabType = tab.TabType,
                    ImportStatus = tab.ImportStatus,
                    ImportAction = tab.ImportAction,
                    TransactionGroupName = tab.PlmTemplateName,
                    TransactionName = tab.PlmTabName,
                    IntegrationId = $"Tab_{tab.PlmTabId}",
                    SiblingTableName = tab.SiblingTableName,
                    ChildTableNames = tab.ChildTableNames,
                    SiblingFieldCount = tab.SiblingFieldCount,
                    GridFieldCount = tab.GridFieldCount,
                    WarningCount = tab.WarningCount,
                    SkipReason = tab.SkipReason,
                    ShowTabWarning = showWarning,
                    SimilarTabGroupId = analysis.SimilarTabGroupByTabId.TryGetValue(tab.PlmTabId, out string gid) ? gid : null,
                    SimilarTabJaccard = analysis.SimilarTabGroups
                        .FirstOrDefault(g => g.TabIds.Contains(tab.PlmTabId))?.JaccardScore
                });
            }

            return grid;
        }

        private static PlmTemplateImportSettingDto BuildDefaultImportSetting(List<PlmTemplateMappingGridRowDto> rows)
        {
            return new PlmTemplateImportSettingDto
            {
                Rows = rows.Select(r => new PlmTemplateImportSettingRowDto
                {
                    PlmTemplateId = r.PlmTemplateId,
                    PlmTabId = r.PlmTabId,
                    TransactionGroupName = r.TransactionGroupName,
                    TransactionName = r.TransactionName,
                    IntegrationId = r.IntegrationId,
                    SiblingTableName = r.SiblingTableName,
                    ImportStatus = r.ImportStatus
                }).ToList()
            };
        }

        private static PlmTemplateMappingValidationDto ValidateTemplateImportSettingInternal(
            string plmConnectionString,
            string stepStateJson,
            PlmTemplateImportSettingDto setting,
            bool strict)
        {
            var validation = new PlmTemplateMappingValidationDto { IsValid = true };
            if (setting?.Rows == null || setting.Rows.Count == 0)
            {
                validation.Errors.Add("Import setting has no rows.");
                return validation;
            }

            var prefixes = ResolveImportPrefixes(stepStateJson);
            var readyRows = setting.Rows.Where(r => r.ImportStatus == TemplateStatusReady).ToList();
            if (readyRows.Count == 0)
            {
                validation.Errors.Add("No ready tabs in import setting.");
                return validation;
            }

            foreach (var group in setting.TabSharedTableGroups ?? new List<PlmTemplateTabSharedTableGroupDto>())
            {
                if (string.IsNullOrWhiteSpace(group.SharedTableName))
                    validation.Errors.Add($"Shared table group {group.GroupId} has no table name.");
                else if (!group.SharedTableName.StartsWith(prefixes.TablePrefix, StringComparison.OrdinalIgnoreCase))
                    validation.Warnings.Add($"Shared table {group.SharedTableName} does not use prefix {prefixes.TablePrefix}.");
                if (group.TabIds == null || group.TabIds.Count < 2)
                    validation.Errors.Add($"Shared table group {group.GroupId} must include at least two tabs.");
            }

            foreach (var row in readyRows)
            {
                if (string.IsNullOrWhiteSpace(row.SiblingTableName))
                    validation.Errors.Add($"Tab {row.PlmTabId} has no sibling table name.");
                if (string.IsNullOrWhiteSpace(row.TransactionName))
                    validation.Errors.Add($"Tab {row.PlmTabId} has no transaction name.");
                if (row.IntegrationId != $"Tab_{row.PlmTabId}")
                    validation.Warnings.Add($"Tab {row.PlmTabId} IntegrationId differs from Tab_{{TabId}} convention.");
            }

            var siblingByTabId = ResolveSiblingTableByTabId(setting);
            foreach (var group in siblingByTabId.GroupBy(p => p.Key).Where(g => g.Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1))
                validation.Errors.Add($"Tab {group.Key} has conflicting sibling table names in setting rows.");

            if (strict && !string.IsNullOrWhiteSpace(plmConnectionString))
            {
                var conflicts = DetectSharedTableColumnConflicts(plmConnectionString, setting, prefixes.TablePrefix);
                foreach (var conflict in conflicts)
                    validation.Errors.Add(conflict);
            }

            validation.IsValid = validation.Errors.Count == 0;
            return validation;
        }

        private static Dictionary<int, string> ResolveSiblingTableByTabId(PlmTemplateImportSettingDto setting)
        {
            var map = new Dictionary<int, string>();
            var sharedGroups = setting.TabSharedTableGroups ?? new List<PlmTemplateTabSharedTableGroupDto>();
            foreach (var row in setting.Rows.Where(r => r.ImportStatus == TemplateStatusReady))
            {
                string table = row.SiblingTableName;
                var group = sharedGroups.FirstOrDefault(g => g.TabIds != null && g.TabIds.Contains(row.PlmTabId));
                if (group != null && !string.IsNullOrWhiteSpace(group.SharedTableName))
                    table = group.SharedTableName;
                map[row.PlmTabId] = table;
            }
            return map;
        }

        private static List<string> DetectSharedTableColumnConflicts(
            string plmConnectionString,
            PlmTemplateImportSettingDto setting,
            string tablePrefix)
        {
            var errors = new List<string>();
            var tabs = LoadPlmTemplateTabs(plmConnectionString, tablePrefix)
                .Where(t => t.ImportStatus == TemplateStatusReady)
                .GroupBy(t => t.TabId)
                .Select(g => g.First())
                .ToList();

            foreach (var tab in tabs)
            {
                ClassifyTemplateTab(tab);
                if (tab.ImportStatus == TemplateStatusSkipped)
                    continue;
                ResolveSubItemColumns(tab, tablePrefix);
                LoadGridColumnsForTab(plmConnectionString, tab, tablePrefix);
            }

            var plans = BuildTemplateTabExecutionPlans(tabs, setting, tablePrefix);
            var columnTypesByTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var plan in plans)
            {
                foreach (var pair in plan.SiblingColumnsByTable)
                {
                    if (!columnTypesByTable.TryGetValue(pair.Key, out var cols))
                    {
                        cols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        columnTypesByTable[pair.Key] = cols;
                    }

                    foreach (var subItem in pair.Value)
                    {
                        string sqlType = MapControlTypeToSqlType(subItem.ControlType, subItem.Nbdecimal);
                        if (cols.TryGetValue(subItem.ColumnName, out string existing) &&
                            !string.Equals(existing, sqlType, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add($"Column [{subItem.ColumnName}] on table {pair.Key} has conflicting types ({existing} vs {sqlType}).");
                        }
                        else
                        {
                            cols[subItem.ColumnName] = sqlType;
                        }
                    }
                }
            }

            return errors;
        }

        private static List<TemplateTabExecutionPlan> BuildTemplateTabExecutionPlans(
            List<PlmTemplateTabRow> uniqueTabs,
            PlmTemplateImportSettingDto setting,
            string tablePrefix)
        {
            var plans = new List<TemplateTabExecutionPlan>();
            if (uniqueTabs == null || uniqueTabs.Count == 0)
                return plans;

            setting = setting ?? new PlmTemplateImportSettingDto();
            var siblingByTabId = ResolveSiblingTableByTabId(setting);
            var blockOverrides = (setting.BlockStorageOverrides ?? new List<PlmTemplateBlockStorageOverrideDto>())
                .GroupBy(b => b.BlockId)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var tab in uniqueTabs)
            {
                if (tab.ImportStatus == TemplateStatusSkipped)
                    continue;

                var plan = new TemplateTabExecutionPlan { Tab = tab };
                string primarySibling = siblingByTabId.TryGetValue(tab.TabId, out string resolved) && !string.IsNullOrWhiteSpace(resolved)
                    ? resolved
                    : tab.SiblingTableName;
                plan.PrimarySiblingTable = primarySibling;

                var rowSetting = setting.Rows?
                    .FirstOrDefault(r => r.PlmTabId == tab.TabId && r.ImportStatus == TemplateStatusReady);
                if (rowSetting != null && !string.IsNullOrWhiteSpace(rowSetting.TransactionName))
                    tab.TabName = rowSetting.TransactionName;

                foreach (var subItem in tab.SubItems)
                {
                    if (subItem.ControlType == ControlTypeGrid || subItem.ControlType == ControlTypeLabel || subItem.ControlType == ControlTypeEmpty)
                        continue;

                    blockOverrides.TryGetValue(subItem.BlockId, out var blockOverride);

                    if (blockOverride != null && string.Equals(blockOverride.StorageTarget, BlockStorageRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        if (TryMapReferenceStaticField(subItem.ReferenceStaticFieldId, out string rootColumn))
                        {
                            subItem.MapsToRoot = true;
                            subItem.ColumnName = rootColumn;
                            plan.RootSubItems.Add(subItem);
                        }
                        else
                        {
                            tab.Warnings.Add(new PlmTemplateWarningDto
                            {
                                PlmTemplateId = tab.TemplateId,
                                PlmTabId = tab.TabId,
                                PlmSubItemId = subItem.SubItemId,
                                Issue = $"Block {subItem.BlockId} override to root skipped non-header field {subItem.SubItemName}."
                            });
                            AddSubItemToSiblingTable(plan, primarySibling, subItem);
                        }

                        continue;
                    }

                    if (blockOverride != null && string.Equals(blockOverride.StorageTarget, BlockStorageSharedSibling, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(blockOverride.SharedTableName))
                    {
                        AddSubItemToSiblingTable(plan, blockOverride.SharedTableName.Trim(), subItem);
                        continue;
                    }

                    if (subItem.MapsToRoot)
                    {
                        plan.RootSubItems.Add(subItem);
                        continue;
                    }

                    AddSubItemToSiblingTable(plan, primarySibling, subItem);
                }

                plans.Add(plan);
            }

            return plans;
        }

        private static void AddSubItemToSiblingTable(TemplateTabExecutionPlan plan, string tableName, PlmTemplateSubItemRow subItem)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return;

            if (!plan.SiblingColumnsByTable.TryGetValue(tableName, out var list))
            {
                list = new List<PlmTemplateSubItemRow>();
                plan.SiblingColumnsByTable[tableName] = list;
            }

            if (!list.Any(s => s.SubItemId == subItem.SubItemId)
                && (string.IsNullOrWhiteSpace(subItem.ColumnName)
                    || !list.Any(s => string.Equals(s.ColumnName, subItem.ColumnName, StringComparison.OrdinalIgnoreCase))))
                list.Add(subItem);
        }

        private static Dictionary<int, HashSet<int>> LoadTabBlockMap(string plmConnectionString, List<int> tabIds)
        {
            var map = tabIds.ToDictionary(id => id, _ => new HashSet<int>());
            if (tabIds.Count == 0)
                return map;

            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
SELECT TabID, BlockID
FROM dbo.PdmTabBlock
WHERE TabID IN ({string.Join(",", tabIds)})";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int tabId = reader.GetInt32(0);
                            int blockId = reader.GetInt32(1);
                            if (map.TryGetValue(tabId, out var set))
                                set.Add(blockId);
                        }
                    }
                }
            }

            return map;
        }

        private static Dictionary<int, string> LoadBlockNames(string plmConnectionString, IEnumerable<int> blockIds)
        {
            var ids = blockIds.Distinct().ToList();
            var names = new Dictionary<int, string>();
            if (ids.Count == 0)
                return names;

            using (var conn = new SqlConnection(plmConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $@"
SELECT BlockID, Name
FROM dbo.pdmBlock
WHERE BlockID IN ({string.Join(",", ids)})";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            names[reader.GetInt32(0)] = reader.IsDBNull(1)
                                ? $"Block_{reader.GetInt32(0)}"
                                : reader.GetString(1);
                        }
                    }
                }
            }

            return names;
        }

        private static double ComputeJaccard(HashSet<int> setA, HashSet<int> setB)
        {
            if (setA.Count == 0 && setB.Count == 0)
                return 1.0;
            if (setA.Count == 0 || setB.Count == 0)
                return 0.0;

            int intersection = setA.Intersect(setB).Count();
            int union = setA.Union(setB).Count();
            return union == 0 ? 0.0 : (double)intersection / union;
        }
    }
}
