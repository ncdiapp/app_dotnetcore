using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Maps Mass Update Hierarchical ListEdit create-spec → AppConfigPack (OrganizedType=List).
/// SearchView attach / DataSet patch remain in PlmImportEngine (existing Search enrichment).
/// </summary>
public static class MassUpdateListEditAppConfigPackBuilder
{
    public static AppConfigPackDto Build(
        PlmSearchMassUpdateListEditCreateSpecDto create,
        int? saasApplicationId = null)
    {
        if (create == null)
            throw new ArgumentNullException(nameof(create));
        if (string.IsNullOrWhiteSpace(create.IntegrationId))
            throw new ArgumentException("ListEdit Create.IntegrationId is required.");
        if (create.UnitStructure?.Root == null || string.IsNullOrWhiteSpace(create.UnitStructure.Root.AppTableName))
            throw new ArgumentException("ListEdit Create root table is required.");

        string rootTable = create.UnitStructure.Root.AppTableName.Trim();
        var pack = new AppConfigPackDto
        {
            SchemaVersion = 1,
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Source = new AppConfigPackSourceDto
            {
                GeneratedBy = "plm-search-massupdate-listedit",
                ApplicationName = create.Name ?? create.IntegrationId,
                SaasApplicationId = saasApplicationId ?? create.SaasApplicationId,
                Notes = "Mass Update Hierarchical ListEdit via AppConfigPack."
            }
        };

        var unit = new AppConfigPackUnitStructureDto
        {
            RootTableName = rootTable,
            RootDisplayName = string.IsNullOrWhiteSpace(create.UnitStructure.Root.UnitDisplayName)
                ? AppTransactionBL.ConvertDbNameToDisplayName(rootTable)
                : create.UnitStructure.Root.UnitDisplayName,
            ChildUnits = new List<AppConfigPackChildUnitDto>()
        };

        foreach (var child in create.UnitStructure.Children ?? Enumerable.Empty<PlmSearchMassUpdateListEditUnitDto>())
        {
            if (child == null || string.IsNullOrWhiteSpace(child.AppTableName))
                continue;
            string childTable = child.AppTableName.Trim();
            unit.ChildUnits.Add(new AppConfigPackChildUnitDto
            {
                TableName = childTable,
                DisplayName = string.IsNullOrWhiteSpace(child.UnitDisplayName)
                    ? AppTransactionBL.ConvertDbNameToDisplayName(childTable)
                    : child.UnitDisplayName
            });
        }

        var tx = new AppConfigPackTransactionDto
        {
            IntegrationId = create.IntegrationId.Trim(),
            Name = string.IsNullOrWhiteSpace(create.Name) ? create.IntegrationId.Trim() : create.Name.Trim(),
            Description = create.Name,
            OrganizedType = "List",
            UnitStructure = unit,
            Fields = new List<AppConfigPackFieldDto>()
        };

        AppendUnitFields(tx, create.UnitStructure.Root, isRoot: true);
        foreach (var child in create.UnitStructure.Children ?? Enumerable.Empty<PlmSearchMassUpdateListEditUnitDto>())
            AppendUnitFields(tx, child, isRoot: false);

        pack.Transactions.Add(tx);
        return pack;
    }

    private static void AppendUnitFields(
        AppConfigPackTransactionDto tx,
        PlmSearchMassUpdateListEditUnitDto unit,
        bool isRoot)
    {
        if (unit == null || string.IsNullOrWhiteSpace(unit.AppTableName))
            return;

        string table = unit.AppTableName.Trim();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string column, Action<AppConfigPackFieldDto> apply)
        {
            if (string.IsNullOrWhiteSpace(column) || !seen.Add(column.Trim()))
                return;
            var field = new AppConfigPackFieldDto
            {
                TableName = table,
                ColumnName = column.Trim(),
                DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(column.Trim())
            };
            apply(field);
            tx.Fields.Add(field);
        }

        if (!string.IsNullOrWhiteSpace(unit.PkColumn))
        {
            Add(unit.PkColumn, f =>
            {
                f.IsPrimaryKey = true;
                f.IsReadOnly = true;
                f.IsVisible = isRoot;
                f.ControlType = 20;
                f.SortOrder = 0;
            });
        }

        if (!isRoot && !string.IsNullOrWhiteSpace(unit.FkColumn))
        {
            Add(unit.FkColumn, f =>
            {
                f.IsLinkToParentPrimaryKey = true;
                f.IsVisible = false;
                f.IsReadOnly = true;
                f.ControlType = 20;
                f.SortOrder = 0;
            });
        }

        foreach (var f in unit.Fields ?? Enumerable.Empty<PlmSearchMassUpdateListEditFieldDto>())
        {
            if (f == null || string.IsNullOrWhiteSpace(f.AppColumnName))
                continue;
            string col = f.AppColumnName.Trim();
            if (seen.Contains(col))
            {
                var existing = tx.Fields.FirstOrDefault(x =>
                    string.Equals(x.TableName, table, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.ColumnName, col, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                    OverlayField(existing, f, unit, isRoot);
                continue;
            }

            Add(col, field => OverlayField(field, f, unit, isRoot));
        }
    }

    private static void OverlayField(
        AppConfigPackFieldDto field,
        PlmSearchMassUpdateListEditFieldDto src,
        PlmSearchMassUpdateListEditUnitDto unit,
        bool isRoot)
    {
        if (!string.IsNullOrWhiteSpace(src.DisplayLabel))
            field.DisplayName = src.DisplayLabel;
        if (src.ControlType.HasValue)
            field.ControlType = src.ControlType;
        field.IsVisible = src.IsVisible;
        if (src.IsReadOnly == true)
            field.IsReadOnly = true;
        if (src.IsPrimaryKey == true)
        {
            field.IsPrimaryKey = true;
            field.IsReadOnly = true;
        }
        if (src.IsForeignKey == true
            || (!isRoot
                && !string.IsNullOrWhiteSpace(unit.FkColumn)
                && string.Equals(src.AppColumnName, unit.FkColumn, StringComparison.OrdinalIgnoreCase)))
        {
            field.IsLinkToParentPrimaryKey = true;
            if (!src.IsVisible)
                field.IsVisible = false;
        }
        if (!string.IsNullOrWhiteSpace(src.EntityIntegrationId))
            field.EntityCode = src.EntityIntegrationId.Trim();
        if (src.Sort.HasValue)
            field.SortOrder = src.Sort;
    }
}
