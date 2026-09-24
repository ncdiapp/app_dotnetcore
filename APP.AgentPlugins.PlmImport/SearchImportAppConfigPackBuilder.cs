using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Maps PLM Search Import Blueprint → <see cref="AppConfigPackDto"/> (searches section)
/// for <c>AppConfigPackBL</c>.
/// </summary>
public static class SearchImportAppConfigPackBuilder
{
    public static AppConfigPackDto Build(PlmSearchImportBlueprintDto blueprint, int? saasApplicationId = null)
    {
        if (blueprint == null)
            throw new ArgumentNullException(nameof(blueprint));
        if (blueprint.Search == null || string.IsNullOrWhiteSpace(blueprint.Search.IntegrationId))
            throw new ArgumentException("Search.IntegrationId is required.");
        if (string.IsNullOrWhiteSpace(blueprint.DataSet?.QueryText))
            throw new ArgumentException("DataSet.QueryText is required.");

        var pack = new AppConfigPackDto
        {
            SchemaVersion = Math.Max(1, blueprint.SchemaVersion),
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Source = new AppConfigPackSourceDto
            {
                GeneratedBy = "plm-search-blueprint",
                ApplicationName = blueprint.Search.Name ?? blueprint.Source?.PlmSearchName ?? "PLM Search",
                SaasApplicationId = saasApplicationId ?? blueprint.Search.SaasApplicationId,
                Notes = $"Converted from PlmSearchImportBlueprint (plmSearchTemplateId={blueprint.Source?.PlmSearchTemplateId})."
            }
        };

        if (blueprint.TransactionGroup != null
            && !string.IsNullOrWhiteSpace(blueprint.TransactionGroup.GroupName))
        {
            var members = new List<string>();
            if (!string.IsNullOrWhiteSpace(blueprint.TransactionGroup.PrimaryTransactionIntegrationId))
                members.Add(blueprint.TransactionGroup.PrimaryTransactionIntegrationId);

            pack.TransactionGroup = new AppConfigPackTransactionGroupDto
            {
                Name = blueprint.TransactionGroup.GroupName,
                IntegrationId = $"TG_{Sanitize(blueprint.TransactionGroup.GroupName)}",
                PrimaryTransactionIntegrationId = blueprint.TransactionGroup.PrimaryTransactionIntegrationId,
                HeaderTransactionIntegrationIds = new List<string>(),
                MemberTransactionIntegrationIds = members
            };
        }

        var search = new AppConfigPackSearchDto
        {
            IntegrationId = blueprint.Search.IntegrationId,
            Name = blueprint.Search.Name ?? "PLM Search",
            Description = blueprint.Search.Description ?? blueprint.Search.Name,
            UsageType = string.IsNullOrWhiteSpace(blueprint.Search.UsageType)
                ? "Management"
                : blueprint.Search.UsageType,
            AutoExecute = blueprint.Search.AutoExecute,
            DataSet = new AppConfigPackDataSetDto
            {
                Name = Truncate(blueprint.DataSet?.Name ?? blueprint.Search.Name ?? "DataSet", 80),
                PrimaryTableName = blueprint.DataSet?.PrimaryTableName
                    ?? blueprint.DataSet?.RootTableName,
                QueryText = blueprint.DataSet.QueryText
            },
            CriteriaFields = (blueprint.CriteriaFields ?? new List<PlmSearchImportCriteriaFieldDto>())
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.SysTableFiledPath))
                .Select(c => new AppConfigPackCriteriaFieldDto
                {
                    DisplayText = c.DisplayText ?? c.SysTableFiledPath,
                    SysTableFiledPath = c.SysTableFiledPath,
                    ControlType = c.ControlType,
                    EntityCode = c.EntityIntegrationId,
                    IsVisible = c.IsVisible,
                    Sort = c.Sort ?? 0,
                    PositionRow = c.PositionRow,
                    PositionColumn = c.PositionColumn,
                    OperationId = c.OperationId
                })
                .ToList(),
            SearchView = MapSearchView(blueprint),
            LinkTargets = (blueprint.LinkTargets ?? new List<PlmSearchImportLinkTargetDto>())
                .Where(l => l != null && !string.IsNullOrWhiteSpace(l.TransactionIntegrationId))
                .Select(l => new AppConfigPackLinkTargetDto
                {
                    Name = l.Name ?? l.ActionType ?? "Open",
                    ActionType = l.ActionType ?? "Edit",
                    UsageType = pack.TransactionGroup != null ? "FormGroup" : "Form",
                    TransactionIntegrationId = l.TransactionIntegrationId,
                    SourceColumn = l.SourceColumn,
                    Sort = l.Sort ?? 0
                })
                .ToList()
        };

        if (blueprint.Menu != null && blueprint.Menu.RegisterInMainMenu)
        {
            search.Menu = new AppConfigPackMenuDto
            {
                RegisterInMainMenu = true,
                MenuTitle = blueprint.Menu.MenuTitle ?? search.Name,
                MenuOrder = 100
            };
        }

        pack.Searches.Add(search);
        return pack;
    }

    private static AppConfigPackSearchViewDto MapSearchView(PlmSearchImportBlueprintDto blueprint)
    {
        var sv = blueprint.SearchView;
        if (sv == null)
        {
            return new AppConfigPackSearchViewDto
            {
                Name = (blueprint.Search.Name ?? "Search") + " Grid",
                IntegrationId = blueprint.Search.IntegrationId + "_View",
                GridOutputMode = 1,
                Fields = new List<AppConfigPackSearchViewFieldDto>()
            };
        }

        return new AppConfigPackSearchViewDto
        {
            Name = sv.Name ?? ((blueprint.Search.Name ?? "Search") + " Grid"),
            IntegrationId = sv.IntegrationId ?? (blueprint.Search.IntegrationId + "_View"),
            GridOutputMode = sv.GridOutputMode,
            Fields = (sv.Fields ?? new List<PlmSearchImportSearchViewFieldDto>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.SysTableFiledPath))
                .Select(f => new AppConfigPackSearchViewFieldDto
                {
                    DisplayText = f.DisplayText ?? f.SysTableFiledPath,
                    SysTableFiledPath = f.SysTableFiledPath,
                    ControlType = f.ControlType,
                    EntityCode = f.EntityIntegrationId,
                    IsTransRootId = f.IsTransRootId,
                    IsVisible = f.IsVisible,
                    Sort = f.Sort ?? 0
                })
                .ToList()
        };
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
