using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Maps PLM DW Import Blueprint → platform <see cref="AppConfigPackDto"/> so
/// Transaction / Search / Menu / TransactionGroup are applied via <c>AppConfigPackBL</c>
/// (shared App enhancement), not ad-hoc PLM SQL.
/// </summary>
public static class DwBlueprintAppConfigPackBuilder
{
    public sealed class Options
    {
        public int? SaasApplicationId { get; set; }
        public bool IncludeSearchView { get; set; } = true;
        public bool IncludeNavigation { get; set; } = true;
        public bool IncludeTransactionGroup { get; set; } = true;
        /// <summary>Insert | Update | Repair — filters which transactions enter the pack.</summary>
        public string Mode { get; set; } = "Insert";
        /// <summary>Existing AppTransaction IntegrationIds (for Insert/Repair filtering).</summary>
        public HashSet<string> ExistingTransactionIntegrationIds { get; set; }
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static AppConfigPackDto Build(PlmDwImportBlueprintDto blueprint, Options options = null)
    {
        options ??= new Options();
        if (blueprint == null)
            throw new ArgumentNullException(nameof(blueprint));

        string prefix = ResolvePrefix(blueprint);
        string rootTable = Qualify(
            blueprint.RootUnit?.AppTableName
            ?? blueprint.Transactions?.FirstOrDefault()?.UnitStructure?.RootTableName,
            prefix,
            skipPrefix: false);

        var pack = new AppConfigPackDto
        {
            SchemaVersion = 1,
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Source = new AppConfigPackSourceDto
            {
                GeneratedBy = "plm-dw-blueprint",
                ApplicationName = blueprint.TransactionGroup?.Name
                    ?? blueprint.PlmTemplate?.TemplateName
                    ?? "PLM DW Import",
                SaasApplicationId = options.SaasApplicationId
                    ?? blueprint.TransactionGroup?.SaasApplicationId,
                Notes = $"Converted from PlmDwImportBlueprint (templateId={blueprint.PlmTemplate?.TemplateId})."
            }
        };

        string mode = string.IsNullOrWhiteSpace(options.Mode) ? "Insert" : options.Mode.Trim();
        var existing = options.ExistingTransactionIntegrationIds
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tx in blueprint.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
        {
            if (tx == null)
                continue;
            if (string.Equals(tx.ImportStatus, "Skipped", StringComparison.OrdinalIgnoreCase))
                continue;

            string integrationId = string.IsNullOrWhiteSpace(tx.IntegrationId)
                ? $"Tab_{tx.PlmTabId}"
                : tx.IntegrationId.Trim();

            bool exists = existing.Contains(integrationId);
            if (string.Equals(mode, "Repair", StringComparison.OrdinalIgnoreCase) && !exists)
                continue;

            pack.Transactions.Add(MapTransaction(tx, prefix, rootTable, blueprint));
        }

        AttachOrphanGridTransactions(pack, blueprint, prefix, rootTable, mode, existing);

        // Blueprint-level field overlays (optional explicit list)
        MergeBlueprintFields(pack, blueprint, prefix);

        // Pivot grids + FitRound link targets expressed in pack (AppConfigPackBL applies flags).
        ApplyPivotOverlays(pack, blueprint, prefix);

        if (options.IncludeTransactionGroup && blueprint.TransactionGroup != null
            && !string.IsNullOrWhiteSpace(blueprint.TransactionGroup.Name))
        {
            var memberIds = pack.Transactions
                .Select(t => t.IntegrationId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            var headerIds = GetHeaderTransactionIntegrationIds(blueprint)
                .Where(id => memberIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();
            string primary = headerIds.FirstOrDefault() ?? memberIds.FirstOrDefault();

            pack.TransactionGroup = new AppConfigPackTransactionGroupDto
            {
                Name = blueprint.TransactionGroup.Name,
                IntegrationId = string.IsNullOrWhiteSpace(blueprint.TransactionGroup.IntegrationId)
                    ? $"TG_{Sanitize(blueprint.TransactionGroup.Name)}"
                    : blueprint.TransactionGroup.IntegrationId,
                PrimaryTransactionIntegrationId = primary,
                HeaderTransactionIntegrationIds = headerIds,
                MemberTransactionIntegrationIds = memberIds
            };
        }

        if (options.IncludeSearchView && blueprint.SearchView?.Search != null
            && !string.IsNullOrWhiteSpace(blueprint.SearchView.Search.IntegrationId))
        {
            pack.Searches.Add(MapSearch(blueprint, rootTable, prefix, options));
        }

        return pack;
    }

    private static AppConfigPackTransactionDto MapTransaction(
        PlmDwBlueprintTransactionDto tx,
        string prefix,
        string fallbackRoot,
        PlmDwImportBlueprintDto blueprint)
    {
        var us = tx.UnitStructure;
        string root = Qualify(
            us?.RootTableName ?? fallbackRoot,
            prefix,
            skipPrefix: false);

        var unit = new AppConfigPackUnitStructureDto
        {
            RootTableName = root,
            SiblingUnits = new List<AppConfigPackSiblingUnitDto>(),
            SiblingTableNames = new List<string>(),
            ChildUnits = new List<AppConfigPackChildUnitDto>()
        };

        foreach (var sib in us?.SiblingUnits ?? Enumerable.Empty<PlmDwBlueprintSiblingUnitDto>())
        {
            if (sib == null || string.IsNullOrWhiteSpace(sib.AppTableName))
                continue;
            string table = Qualify(sib.AppTableName, prefix, sib.SkipTablePrefix);
            unit.SiblingUnits.Add(new AppConfigPackSiblingUnitDto
            {
                TableName = table,
                DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(table)
            });
            unit.SiblingTableNames.Add(table);
        }

        foreach (var child in us?.ChildUnits ?? Enumerable.Empty<PlmDwBlueprintChildUnitDto>())
        {
            if (child == null || string.IsNullOrWhiteSpace(child.AppTableName))
                continue;
            string table = Qualify(child.AppTableName, prefix, child.SkipTablePrefix
                || child.AppTableName.StartsWith("Tchp", StringComparison.OrdinalIgnoreCase));
            var grandNames = (child.GrandChildAppTableNames ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => Qualify(n, prefix, skipPrefix: n.Trim().StartsWith("Tchp", StringComparison.OrdinalIgnoreCase)
                    || n.Trim().StartsWith("View_", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var childDto = new AppConfigPackChildUnitDto
            {
                TableName = table,
                DisplayName = string.IsNullOrWhiteSpace(child.UnitDisplayName)
                    ? AppTransactionBL.ConvertDbNameToDisplayName(table)
                    : child.UnitDisplayName,
                IsReadOnly = child.IsReadOnly ? true : null,
                IsDisableAddButton = child.IsReadOnly ? true : null,
                IsDisableDeleteButton = child.IsReadOnly ? true : null,
                GrandChildTableNames = grandNames,
                GrandChildUnits = grandNames.Select(g => new AppConfigPackChildUnitDto
                {
                    TableName = g,
                    DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(g)
                }).ToList()
            };
            if (!string.IsNullOrWhiteSpace(child.LinkTargetIntegrationId))
            {
                bool isFitRound = table.IndexOf("TchpFitRound", StringComparison.OrdinalIgnoreCase) >= 0;
                childDto.LinkTargets = new List<AppConfigPackLinkTargetDto>
                {
                    new AppConfigPackLinkTargetDto
                    {
                        Name = isFitRound ? "Open Fit Round" : "Open",
                        ActionType = "Edit",
                        TransactionIntegrationId = child.LinkTargetIntegrationId.Trim(),
                        SourceColumn = isFitRound ? "FitRoundId" : "ReferenceId",
                        TargetColumn = isFitRound ? "FitRoundId" : "ReferenceId",
                        Sort = isFitRound ? 10 : 1
                    }
                };
            }

            // Read-only view children: hide fields not in VisibleFieldNames (except applied later via UpsertField).
            if (child.IsReadOnly && child.VisibleFieldNames != null && child.VisibleFieldNames.Count > 0)
            {
                // Visibility whitelist applied in ApplyReadOnlyChildVisibility after structure exists in Fields.
            }

            unit.ChildUnits.Add(childDto);
        }

        AttachParentTabGridBindings(unit, tx, blueprint, prefix);

        var txDto = new AppConfigPackTransactionDto
        {
            IntegrationId = string.IsNullOrWhiteSpace(tx.IntegrationId)
                ? $"Tab_{tx.PlmTabId}"
                : tx.IntegrationId.Trim(),
            Name = tx.TransactionName ?? tx.PlmTabName ?? $"Tab {tx.PlmTabId}",
            Description = tx.PlmTabName,
            OrganizedType = "MasterDetail",
            UnitStructure = unit,
            Fields = new List<AppConfigPackFieldDto>()
        };

        // Stash PlmTabId in Description space? Use IntegrationId matching only — tab id via blueprint lookup.
        return txDto;
    }

    /// <summary>
    /// Official generator puts grids in top-level gridBindings, not unitStructure.childUnits.
    /// Attach those host-grid tables as Child units on the parent Tab TX.
    /// </summary>
    private static void AttachParentTabGridBindings(
        AppConfigPackUnitStructureDto unit,
        PlmDwBlueprintTransactionDto tx,
        PlmDwImportBlueprintDto blueprint,
        string prefix)
    {
        if (unit == null || tx == null)
            return;
        unit.ChildUnits ??= new List<AppConfigPackChildUnitDto>();
        foreach (var grid in blueprint.GridBindings ?? Enumerable.Empty<PlmDwBlueprintGridBindingDto>())
        {
            if (grid == null || string.IsNullOrWhiteSpace(grid.AppTableName))
                continue;
            if (!grid.ParentPlmTabId.HasValue || grid.ParentPlmTabId.Value != tx.PlmTabId)
                continue;

            string table = Qualify(grid.AppTableName, prefix, skipPrefix: false);
            var existing = unit.ChildUnits.FirstOrDefault(c =>
                c != null && string.Equals(c.TableName, table, StringComparison.OrdinalIgnoreCase));
            var grands = (grid.GrandChildAppTableNames ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => Qualify(n, prefix, skipPrefix: false))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (existing == null)
            {
                existing = new AppConfigPackChildUnitDto
                {
                    TableName = table,
                    DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(table)
                };
                unit.ChildUnits.Add(existing);
            }
            if (grands.Count == 0)
                continue;
            existing.GrandChildTableNames ??= new List<string>();
            existing.GrandChildUnits ??= new List<AppConfigPackChildUnitDto>();
            foreach (string grandTable in grands)
            {
                if (!existing.GrandChildTableNames.Any(g =>
                    string.Equals(g, grandTable, StringComparison.OrdinalIgnoreCase)))
                    existing.GrandChildTableNames.Add(grandTable);
                if (!existing.GrandChildUnits.Any(g =>
                    g != null && string.Equals(g.TableName, grandTable, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.GrandChildUnits.Add(new AppConfigPackChildUnitDto
                    {
                        TableName = grandTable,
                        DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(grandTable)
                    });
                }
            }
        }
    }

    /// <summary>
    /// Grids whose parent Tab is not in this blueprint become standalone Grid_{id} TXs (Root + Child).
    /// </summary>
    private static void AttachOrphanGridTransactions(
        AppConfigPackDto pack,
        PlmDwImportBlueprintDto blueprint,
        string prefix,
        string rootTable,
        string mode,
        HashSet<string> existing)
    {
        var tabIds = new HashSet<int>(
            (blueprint.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
                .Where(t => t != null && !string.Equals(t.ImportStatus, "Skipped", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.PlmTabId));

        foreach (var grid in blueprint.GridBindings ?? Enumerable.Empty<PlmDwBlueprintGridBindingDto>())
        {
            if (grid == null || string.IsNullOrWhiteSpace(grid.AppTableName))
                continue;
            if (grid.ParentPlmTabId.HasValue && tabIds.Contains(grid.ParentPlmTabId.Value))
                continue;

            string integrationId = !string.IsNullOrWhiteSpace(grid.IntegrationId)
                ? grid.IntegrationId.Trim()
                : $"Grid_{grid.PlmGridId}";
            bool exists = existing != null && existing.Contains(integrationId);
            if (string.Equals(mode, "Repair", StringComparison.OrdinalIgnoreCase) && !exists)
                continue;
            if (pack.Transactions.Any(t =>
                t != null && string.Equals(t.IntegrationId, integrationId, StringComparison.OrdinalIgnoreCase)))
                continue;

            string gridTable = Qualify(grid.AppTableName, prefix, skipPrefix: false);
            pack.Transactions.Add(new AppConfigPackTransactionDto
            {
                IntegrationId = integrationId,
                Name = AppTransactionBL.ConvertDbNameToDisplayName(gridTable),
                Description = integrationId,
                OrganizedType = "MasterDetail",
                UnitStructure = new AppConfigPackUnitStructureDto
                {
                    RootTableName = rootTable,
                    ChildUnits = new List<AppConfigPackChildUnitDto>
                    {
                        new AppConfigPackChildUnitDto
                        {
                            TableName = gridTable,
                            DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(gridTable)
                        }
                    }
                },
                Fields = new List<AppConfigPackFieldDto>()
            });
        }
    }

    private static void ApplyPivotOverlays(
        AppConfigPackDto pack,
        PlmDwImportBlueprintDto blueprint,
        string prefix)
    {
        const int childUnitPivotColumns = (int)EmAppTransactionGridDisplayType.ChildUnitPivotColumns;

        foreach (var binding in blueprint.BomColorwayPivotBindings ?? Enumerable.Empty<PlmDwBlueprintBomColorwayPivotBindingDto>())
        {
            if (binding == null)
                continue;
            var tx = FindTransactionByPlmTabId(pack, blueprint, binding.PlmTabId);
            if (tx == null)
                continue;

            string host = Qualify(binding.HostAppTableName, prefix, skipPrefix: false);
            string grandchild = Qualify(binding.GrandchildAppTableName, prefix, skipPrefix: false);
            string source = Qualify(binding.SourceAppTableName, prefix, skipPrefix: false);
            string pivotKey = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn) ? "Color" : binding.SourcePivotKeyColumn.Trim();
            string colorway = binding.GrandchildColumns?.ColorwayKey ?? "Colorway";
            string parentLink = binding.GrandchildColumns?.ParentLink ?? "ParentRowId";

            EnsureChildUnit(tx, source);
            EnsureGrandchildUnit(tx, host, grandchild, childUnitPivotColumns);
            UpsertPackField(tx, grandchild, colorway, f =>
            {
                f.IsPivotColumn = true;
                f.IsVisible = true;
                f.MatrixSourceTable = source;
                f.MatrixSourceColumn = pivotKey;
            });
            foreach (var vf in binding.GrandchildColumns?.ValueFields ?? Enumerable.Empty<PlmDwBlueprintGrandchildPivotValueFieldDto>())
            {
                if (vf == null || string.IsNullOrWhiteSpace(vf.Column))
                    continue;
                UpsertPackField(tx, grandchild, vf.Column.Trim(), f =>
                {
                    f.IsPivotValue = vf.IsPivotValue;
                    f.IsVisible = true;
                });
            }
            UpsertPackField(tx, grandchild, parentLink, f => f.IsVisible = false);
        }

        foreach (var binding in blueprint.TechPackGradeValuePivotBindings
            ?? Enumerable.Empty<PlmDwBlueprintTechPackGradeValuePivotDto>())
        {
            if (binding == null)
                continue;
            var tx = FindTransactionByPlmTabId(pack, blueprint, binding.PlmTabId);
            if (tx == null)
                continue;

            string host = Qualify(binding.HostAppTableName, prefix, skipPrefix: true);
            string grandchild = Qualify(binding.GrandchildAppTableName, prefix, skipPrefix: true);
            string source = Qualify(binding.SourceAppTableName, prefix, skipPrefix: true);
            string pivotCol = string.IsNullOrWhiteSpace(binding.PivotColumnField) ? "SizeRunSizeId" : binding.PivotColumnField.Trim();
            string pivotVal = string.IsNullOrWhiteSpace(binding.PivotValueField) ? "GradingDelta" : binding.PivotValueField.Trim();
            string sourceKey = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn) ? "SizeRunSizeId" : binding.SourcePivotKeyColumn.Trim();

            EnsureGrandchildUnit(tx, host, grandchild, childUnitPivotColumns);
            UpsertPackField(tx, grandchild, pivotCol, f =>
            {
                f.IsPivotColumn = true;
                f.IsVisible = true;
                f.MatrixSourceTable = source;
                f.MatrixSourceColumn = sourceKey;
            });
            UpsertPackField(tx, grandchild, pivotVal, f =>
            {
                f.IsPivotColumn = false;
                f.IsPivotValue = true;
                f.IsVisible = true;
            });
        }

        foreach (var binding in blueprint.TechPackFitMeasurementPivotBindings
            ?? Enumerable.Empty<PlmDwBlueprintTechPackFitMeasurementPivotDto>())
        {
            if (binding == null)
                continue;
            var tx = FindTransactionByPlmTabId(pack, blueprint, binding.PlmTabId);
            if (tx == null)
                continue;

            string host = Qualify(binding.HostAppTableName, prefix, skipPrefix: true);
            string grandchild = Qualify(binding.GrandchildAppTableName, prefix, skipPrefix: true);
            string source = Qualify(binding.SourceAppTableName, prefix, skipPrefix: true);
            string pivotCol = string.IsNullOrWhiteSpace(binding.PivotColumnField) ? "RoundNumber" : binding.PivotColumnField.Trim();
            string pivotVal = string.IsNullOrWhiteSpace(binding.PivotValueField) ? "ActualValue" : binding.PivotValueField.Trim();
            string sourceKey = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn) ? "RoundNumber" : binding.SourcePivotKeyColumn.Trim();

            EnsureGrandchildUnit(tx, host, grandchild, childUnitPivotColumns);
            UpsertPackField(tx, grandchild, pivotCol, f =>
            {
                f.IsPivotColumn = true;
                f.IsVisible = true;
                f.MatrixSourceTable = source;
                f.MatrixSourceColumn = sourceKey;
            });
            UpsertPackField(tx, grandchild, pivotVal, f =>
            {
                f.IsPivotColumn = false;
                f.IsPivotValue = true;
                f.IsVisible = true;
            });
        }

        foreach (var binding in blueprint.TechPackSimpleQcPivotBindings
            ?? Enumerable.Empty<PlmDwBlueprintTechPackSimpleQcPivotDto>())
        {
            if (binding == null)
                continue;
            var tx = FindTransactionByPlmTabId(pack, blueprint, binding.PlmTabId);
            if (tx == null)
                continue;

            string host = Qualify(binding.HostAppTableName, prefix, binding.SkipTablePrefixOnHost);
            string grandchild = Qualify(binding.GrandchildAppTableName, prefix, binding.SkipTablePrefixOnGrandchild);
            string source = Qualify(binding.SourceAppTableName, prefix, binding.SkipTablePrefixOnSource);
            string pivotCol = string.IsNullOrWhiteSpace(binding.PivotColumnField) ? "SizeRunSizeId" : binding.PivotColumnField.Trim();
            string sourceKey = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn) ? "SizeRunSizeId" : binding.SourcePivotKeyColumn.Trim();

            var pivotVals = (binding.PivotValueFields ?? new List<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();
            if (pivotVals.Count == 0 && !string.IsNullOrWhiteSpace(binding.PivotValueField))
                pivotVals.Add(binding.PivotValueField.Trim());
            if (pivotVals.Count == 0)
                pivotVals.Add("QCSize");

            EnsureGrandchildUnit(tx, host, grandchild, childUnitPivotColumns);
            UpsertPackField(tx, grandchild, pivotCol, f =>
            {
                f.IsPivotColumn = true;
                f.IsVisible = true;
                f.MatrixSourceTable = source;
                f.MatrixSourceColumn = sourceKey;
            });
            foreach (string pivotVal in pivotVals)
            {
                string label = binding.PivotValueLabels?
                    .FirstOrDefault(l => l != null && string.Equals(l.FieldName, pivotVal, StringComparison.OrdinalIgnoreCase))
                    ?.DisplayLabel;
                UpsertPackField(tx, grandchild, pivotVal, f =>
                {
                    f.IsPivotColumn = false;
                    f.IsPivotValue = true;
                    f.IsVisible = true;
                    if (!string.IsNullOrWhiteSpace(label))
                        f.DisplayName = label;
                });
            }
        }
    }

    private static AppConfigPackTransactionDto FindTransactionByPlmTabId(
        AppConfigPackDto pack,
        PlmDwImportBlueprintDto blueprint,
        int plmTabId)
    {
        var bpTx = blueprint.Transactions?.FirstOrDefault(t => t != null && t.PlmTabId == plmTabId);
        if (bpTx == null)
            return null;
        string integrationId = string.IsNullOrWhiteSpace(bpTx.IntegrationId)
            ? $"Tab_{bpTx.PlmTabId}"
            : bpTx.IntegrationId.Trim();
        return pack.Transactions.FirstOrDefault(t =>
            t != null && string.Equals(t.IntegrationId, integrationId, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureChildUnit(AppConfigPackTransactionDto tx, string table)
    {
        if (tx?.UnitStructure == null || string.IsNullOrWhiteSpace(table))
            return;
        tx.UnitStructure.ChildUnits ??= new List<AppConfigPackChildUnitDto>();
        if (tx.UnitStructure.SiblingTableNames?.Any(s =>
            string.Equals(s, table, StringComparison.OrdinalIgnoreCase)) == true)
            return;
        if (tx.UnitStructure.ChildUnits.Any(c =>
            c != null && string.Equals(c.TableName, table, StringComparison.OrdinalIgnoreCase)))
            return;
        tx.UnitStructure.ChildUnits.Add(new AppConfigPackChildUnitDto
        {
            TableName = table,
            DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(table)
        });
    }

    private static void EnsureGrandchildUnit(
        AppConfigPackTransactionDto tx,
        string hostTable,
        string grandchildTable,
        int gridDisplayType)
    {
        if (tx?.UnitStructure == null || string.IsNullOrWhiteSpace(hostTable) || string.IsNullOrWhiteSpace(grandchildTable))
            return;

        tx.UnitStructure.ChildUnits ??= new List<AppConfigPackChildUnitDto>();
        var host = tx.UnitStructure.ChildUnits.FirstOrDefault(c =>
            c != null && string.Equals(c.TableName, hostTable, StringComparison.OrdinalIgnoreCase));
        if (host == null)
        {
            host = new AppConfigPackChildUnitDto
            {
                TableName = hostTable,
                DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(hostTable)
            };
            tx.UnitStructure.ChildUnits.Add(host);
        }

        host.GrandChildTableNames ??= new List<string>();
        if (!host.GrandChildTableNames.Any(g => string.Equals(g, grandchildTable, StringComparison.OrdinalIgnoreCase)))
            host.GrandChildTableNames.Add(grandchildTable);

        host.GrandChildUnits ??= new List<AppConfigPackChildUnitDto>();
        var grand = host.GrandChildUnits.FirstOrDefault(g =>
            g != null && string.Equals(g.TableName, grandchildTable, StringComparison.OrdinalIgnoreCase));
        if (grand == null)
        {
            grand = new AppConfigPackChildUnitDto
            {
                TableName = grandchildTable,
                DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(grandchildTable)
            };
            host.GrandChildUnits.Add(grand);
        }
        grand.GridDisplayType = gridDisplayType;
    }

    private static void UpsertPackField(
        AppConfigPackTransactionDto tx,
        string tableName,
        string columnName,
        Action<AppConfigPackFieldDto> apply)
    {
        if (tx == null || string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName))
            return;
        tx.Fields ??= new List<AppConfigPackFieldDto>();
        var field = tx.Fields.FirstOrDefault(f =>
            f != null
            && string.Equals(f.TableName, tableName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(f.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
        if (field == null)
        {
            field = new AppConfigPackFieldDto
            {
                TableName = tableName,
                ColumnName = columnName,
                DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(columnName)
            };
            tx.Fields.Add(field);
        }
        apply?.Invoke(field);
    }

    private static void MergeBlueprintFields(
        AppConfigPackDto pack,
        PlmDwImportBlueprintDto blueprint,
        string prefix)
    {
        if (blueprint.BlueprintFields == null || blueprint.BlueprintFields.Count == 0)
            return;

        // Attach fields to first matching transaction by plmTabId / table name when possible;
        // otherwise distribute onto any transaction that owns the table in unitStructure.
        foreach (var f in blueprint.BlueprintFields)
        {
            if (f == null || string.IsNullOrWhiteSpace(f.AppTableName) || string.IsNullOrWhiteSpace(f.AppColumnName))
                continue;

            string table = Qualify(f.AppTableName, prefix, skipPrefix: false);
            var tx = pack.Transactions.FirstOrDefault(t =>
                UnitOwnsTable(t.UnitStructure, table));
            if (tx == null)
                continue;

            UpsertPackField(tx, table, f.AppColumnName, field =>
            {
                field.DisplayName = string.IsNullOrWhiteSpace(f.DisplayLabel)
                    ? AppTransactionBL.ConvertDbNameToDisplayName(f.AppColumnName)
                    : f.DisplayLabel;
                field.ControlType = f.AppControlType ?? f.PlmControlType;
                field.IsVisible = f.IsVisible;
                field.EntityCode = f.EntityIntegrationId
                    ?? (f.PlmEntityId.HasValue ? f.PlmEntityId.Value.ToString() : null);
                if (f.DisplayOrder.HasValue)
                    field.SortOrder = f.DisplayOrder;
            });
        }
    }

    private static bool UnitOwnsTable(AppConfigPackUnitStructureDto us, string table)
    {
        if (us == null || string.IsNullOrWhiteSpace(table))
            return false;
        if (string.Equals(us.RootTableName, table, StringComparison.OrdinalIgnoreCase))
            return true;
        if (us.SiblingTableNames?.Any(t => string.Equals(t, table, StringComparison.OrdinalIgnoreCase)) == true)
            return true;
        if (us.SiblingUnits?.Any(s => string.Equals(s.TableName, table, StringComparison.OrdinalIgnoreCase)) == true)
            return true;
        if (us.ChildUnits == null)
            return false;
        foreach (var c in us.ChildUnits)
        {
            if (string.Equals(c.TableName, table, StringComparison.OrdinalIgnoreCase))
                return true;
            if (c.GrandChildTableNames?.Any(g => string.Equals(g, table, StringComparison.OrdinalIgnoreCase)) == true)
                return true;
            if (c.GrandChildUnits?.Any(g => string.Equals(g.TableName, table, StringComparison.OrdinalIgnoreCase)) == true)
                return true;
        }
        return false;
    }

    private static AppConfigPackSearchDto MapSearch(
        PlmDwImportBlueprintDto blueprint,
        string rootTable,
        string prefix,
        Options options)
    {
        var searchSpec = blueprint.SearchView.Search;
        string masterSibling = ResolveMasterSiblingTable(blueprint, prefix);
        string query = BuildReferenceBasicInfoQuery(rootTable, masterSibling);
        string searchName = searchSpec.Name
            ?? blueprint.TransactionGroup?.Name
            ?? "PLM References";

        var primaryTx = packPrimaryTransactionIntegrationId(blueprint);

        var search = new AppConfigPackSearchDto
        {
            IntegrationId = searchSpec.IntegrationId,
            Name = searchName,
            Description = searchName,
            UsageType = string.IsNullOrWhiteSpace(searchSpec.UsageType)
                ? "DataModelTemplate"
                : searchSpec.UsageType,
            AutoExecute = false,
            DataSet = new AppConfigPackDataSetDto
            {
                Name = Truncate(searchName, 80),
                PrimaryTableName = rootTable,
                QueryText = query
            },
            SearchView = new AppConfigPackSearchViewDto
            {
                Name = searchName + " Grid",
                IntegrationId = blueprint.SearchView.SearchView?.IntegrationId
                    ?? (searchSpec.IntegrationId + "_View"),
                GridOutputMode = 1,
                Fields = new List<AppConfigPackSearchViewFieldDto>
                {
                    new AppConfigPackSearchViewFieldDto
                    {
                        DisplayText = "Reference Id",
                        SysTableFiledPath = "ReferenceId",
                        ControlType = 20,
                        IsTransRootId = true,
                        IsVisible = false,
                        Sort = 1
                    }
                }
            },
            LinkTargets = BuildDataModelTemplateLinkTargets(blueprint, primaryTx)
        };

        if (options.IncludeNavigation)
        {
            string menuTitle = blueprint.Navigation?.FolderName
                ?? blueprint.TransactionGroup?.Name
                ?? searchName;
            search.Menu = new AppConfigPackMenuDto
            {
                RegisterInMainMenu = true,
                MenuTitle = menuTitle,
                MenuOrder = blueprint.Navigation?.MenuOrder ?? 100
            };
        }

        return search;
    }

    private static string packPrimaryTransactionIntegrationId(PlmDwImportBlueprintDto blueprint)
    {
        var headers = GetHeaderTransactionIntegrationIds(blueprint);
        if (headers.Count > 0)
            return headers[0];
        return blueprint.Transactions?
            .FirstOrDefault(t => t != null && !string.Equals(t.ImportStatus, "Skipped", StringComparison.OrdinalIgnoreCase))
            ?.IntegrationId;
    }

    /// <summary>
    /// Header tabs only. Never falls back to the first/only transaction — that is Primary, not Header.
    /// </summary>
    private static List<string> GetHeaderTransactionIntegrationIds(PlmDwImportBlueprintDto blueprint)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headerTabIds = new HashSet<int>();
        if (blueprint?.PlmTemplate?.TemplateHeaderTabIds != null)
        {
            foreach (int tabId in blueprint.PlmTemplate.TemplateHeaderTabIds)
                headerTabIds.Add(tabId);
        }

        var readyCount = 0;
        foreach (var tx in blueprint?.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
        {
            if (tx == null || string.Equals(tx.ImportStatus, "Skipped", StringComparison.OrdinalIgnoreCase))
                continue;
            readyCount++;
            if (tx.IsTemplateHeaderTab != true && !headerTabIds.Contains(tx.PlmTabId))
                continue;
            string integrationId = string.IsNullOrWhiteSpace(tx.IntegrationId)
                ? $"Tab_{tx.PlmTabId}"
                : tx.IntegrationId.Trim();
            if (seen.Add(integrationId))
                ids.Add(integrationId);
        }

        // Single-tab templates (e.g. Graphic Requests) often list the only tab as PLM header.
        // APP Data Model Template needs that tab as MainItem, not Shared Header.
        if (readyCount > 0 && ids.Count >= readyCount)
            return new List<string>();

        return ids;
    }

    private static List<AppConfigPackLinkTargetDto> BuildDataModelTemplateLinkTargets(
        PlmDwImportBlueprintDto blueprint,
        string primaryTx)
    {
        var links = new List<AppConfigPackLinkTargetDto>();
        var headerIds = new HashSet<string>(GetHeaderTransactionIntegrationIds(blueprint), StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(primaryTx))
        {
            links.Add(new AppConfigPackLinkTargetDto
            {
                Name = "Edit",
                ActionType = "Edit",
                UsageType = "FormGroup",
                TransactionIntegrationId = primaryTx,
                SourceColumn = "ReferenceId",
                Sort = 0
            });
        }

        var ready = (blueprint?.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
            .Where(t => t != null && !string.Equals(t.ImportStatus, "Skipped", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.PlmTabSort ?? int.MaxValue)
            .ThenBy(t => t.PlmTabId)
            .ToList();

        string firstMain = null;
        int sort = 0;
        foreach (var tx in ready)
        {
            string integrationId = string.IsNullOrWhiteSpace(tx.IntegrationId)
                ? $"Tab_{tx.PlmTabId}"
                : tx.IntegrationId.Trim();
            bool isHeader = headerIds.Contains(integrationId);
            sort++;
            links.Add(new AppConfigPackLinkTargetDto
            {
                Name = string.IsNullOrWhiteSpace(tx.TransactionName)
                    ? (tx.PlmTabName ?? integrationId)
                    : tx.TransactionName,
                ActionType = "Edit",
                UsageType = "Form",
                TemplateItemType = isHeader
                    ? (int)EmAppTransactionTemplateItemType.TemplateHeader
                    : (int)EmAppTransactionTemplateItemType.MainItem,
                TransactionIntegrationId = integrationId,
                SourceColumn = "ReferenceId",
                Sort = tx.PlmTabSort ?? sort
            });
            if (!isHeader && firstMain == null)
                firstMain = integrationId;
        }

        var readyTabIds = new HashSet<int>(ready.Select(t => t.PlmTabId));
        foreach (var grid in blueprint.GridBindings ?? Enumerable.Empty<PlmDwBlueprintGridBindingDto>())
        {
            if (grid == null)
                continue;
            if (grid.ParentPlmTabId.HasValue && readyTabIds.Contains(grid.ParentPlmTabId.Value))
                continue;
            string gridIntegrationId = !string.IsNullOrWhiteSpace(grid.IntegrationId)
                ? grid.IntegrationId.Trim()
                : $"Grid_{grid.PlmGridId}";
            if (links.Any(l => string.Equals(l.TransactionIntegrationId, gridIntegrationId, StringComparison.OrdinalIgnoreCase)))
                continue;
            sort++;
            links.Add(new AppConfigPackLinkTargetDto
            {
                Name = string.IsNullOrWhiteSpace(grid.AppTableName)
                    ? gridIntegrationId
                    : AppTransactionBL.ConvertDbNameToDisplayName(Qualify(grid.AppTableName, ResolvePrefix(blueprint), skipPrefix: false)),
                ActionType = "Edit",
                UsageType = "Form",
                TemplateItemType = (int)EmAppTransactionTemplateItemType.MainItem,
                TransactionIntegrationId = gridIntegrationId,
                SourceColumn = "ReferenceId",
                Sort = sort
            });
        }

        if (!string.IsNullOrWhiteSpace(firstMain))
        {
            links.Add(new AppConfigPackLinkTargetDto
            {
                Name = "New",
                ActionType = "Create",
                UsageType = "Form",
                TemplateItemType = (int)EmAppTransactionTemplateItemType.MainItem,
                TransactionIntegrationId = firstMain,
                SourceColumn = "ReferenceId",
                Sort = 0
            });
        }

        return links;
    }

    private static string ResolveMasterSiblingTable(PlmDwImportBlueprintDto blueprint, string prefix)
    {
        var header = blueprint.Transactions?.FirstOrDefault(t => t?.IsTemplateHeaderTab == true)
            ?? blueprint.Transactions?.FirstOrDefault();
        var master = header?.UnitStructure?.SiblingUnits?
            .FirstOrDefault(s => s != null && s.IsMasterSibling);
        if (master == null || string.IsNullOrWhiteSpace(master.AppTableName))
            return null;
        return Qualify(master.AppTableName, prefix, master.SkipTablePrefix);
    }

    private static readonly string[] ReferenceBasicInfoColumns =
    {
        "ReferenceId", "ReferenceCode", "FolderId", "MasterReferenceId"
    };

    /// <summary>
    /// Same column list as official template Search (TemplatePostProcess).
    /// Do not SELECT * from both joined tables — they share ReferenceId and
    /// AppConfigPackBL wraps the query as SELECT * FROM (query) AS subQuery.
    /// </summary>
    private static string BuildReferenceBasicInfoQuery(string rootTable, string masterSiblingTable)
    {
        if (string.IsNullOrWhiteSpace(rootTable))
            throw new InvalidOperationException("Root table is required for the Reference Basic Info search query.");

        if (string.IsNullOrWhiteSpace(masterSiblingTable))
            return "SELECT " + string.Join(", ", ReferenceBasicInfoColumns)
                + " FROM [dbo].[" + rootTable + "]";

        string selectList = string.Join(",\r\n",
            ReferenceBasicInfoColumns.Select(c => "[" + rootTable + "].[" + c + "]"));
        return "SELECT\r\n" + selectList
            + "\r\nFROM [dbo].[" + rootTable + "]"
            + "\r\nINNER JOIN [dbo].[" + masterSiblingTable + "]"
            + " ON [" + rootTable + "].[ReferenceId] = [" + masterSiblingTable + "].[ReferenceId]";
    }

    private static string ResolvePrefix(PlmDwImportBlueprintDto blueprint)
    {
        string prefix = blueprint.Source?.TablePrefix;
        if (string.IsNullOrWhiteSpace(prefix))
            prefix = "Plm_";
        if (!prefix.EndsWith("_", StringComparison.Ordinal))
            prefix += "_";
        return prefix;
    }

    private static string Qualify(string tableName, string prefix, bool skipPrefix)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return tableName;
        tableName = tableName.Trim();
        if (skipPrefix)
            return tableName;
        if (tableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || tableName.StartsWith("Tchp", StringComparison.OrdinalIgnoreCase)
            || tableName.StartsWith("View_", StringComparison.OrdinalIgnoreCase)
            || tableName.StartsWith("Plm_", StringComparison.OrdinalIgnoreCase))
            return tableName;
        return prefix + tableName;
    }

    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Group";
        var chars = name.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "Group" : new string(chars);
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value.Substring(0, max);
    }
}
