using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// RGB Color App Config via shared AppConfigPack (TX + list Search + main menu).
/// Folder-template search / folder navigation stay in PlmImportEngine (product-specific).
/// </summary>
public static class ColorImportAppConfigPackBuilder
{
    public const string RgbTableName = "Plm_pdmRGBColor";
    public const string TxIntegrationId = "PlmColor_RGB";
    public const string ListSearchIntegrationId = "PlmColor_RGB_List";
    public const string RootIdColumn = "RGBColorID";

    public static AppConfigPackDto Build(int? saasApplicationId, IEnumerable<string> listViewColumns = null)
    {
        var columns = (listViewColumns ?? Enumerable.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!columns.Any(c => string.Equals(c, RootIdColumn, StringComparison.OrdinalIgnoreCase)))
            columns.Insert(0, RootIdColumn);

        var viewFields = new List<AppConfigPackSearchViewFieldDto>();
        int sort = 1;
        foreach (string col in columns)
        {
            bool isRoot = string.Equals(col, RootIdColumn, StringComparison.OrdinalIgnoreCase);
            viewFields.Add(new AppConfigPackSearchViewFieldDto
            {
                DisplayText = col,
                SysTableFiledPath = col,
                ControlType = 20,
                IsTransRootId = isRoot,
                IsVisible = !isRoot,
                Sort = sort++
            });
        }

        return new AppConfigPackDto
        {
            SchemaVersion = 1,
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            Source = new AppConfigPackSourceDto
            {
                GeneratedBy = "plm-color-import",
                ApplicationName = "RGB Color",
                SaasApplicationId = saasApplicationId,
                Notes = "PLM Color import — RGB Color TX + list Search."
            },
            Transactions =
            {
                new AppConfigPackTransactionDto
                {
                    IntegrationId = TxIntegrationId,
                    Name = "RGB Color",
                    Description = "RGB Color",
                    OrganizedType = "MasterDetail",
                    UnitStructure = new AppConfigPackUnitStructureDto
                    {
                        RootTableName = RgbTableName
                    }
                }
            },
            Searches =
            {
                new AppConfigPackSearchDto
                {
                    IntegrationId = ListSearchIntegrationId,
                    Name = "RGB Color",
                    Description = "RGB Color",
                    UsageType = "Management",
                    AutoExecute = true,
                    DataSet = new AppConfigPackDataSetDto
                    {
                        Name = "RGB Color",
                        PrimaryTableName = RgbTableName,
                        QueryText = $"SELECT * FROM [dbo].[{RgbTableName}]"
                    },
                    SearchView = new AppConfigPackSearchViewDto
                    {
                        Name = "RGB Color Grid",
                        IntegrationId = ListSearchIntegrationId + "_View",
                        GridOutputMode = 1,
                        Fields = viewFields
                    },
                    LinkTargets =
                    {
                        new AppConfigPackLinkTargetDto
                        {
                            Name = "Create",
                            ActionType = "Create",
                            TransactionIntegrationId = TxIntegrationId,
                            SourceColumn = RootIdColumn,
                            Sort = 1
                        },
                        new AppConfigPackLinkTargetDto
                        {
                            Name = "Open",
                            ActionType = "Edit",
                            TransactionIntegrationId = TxIntegrationId,
                            SourceColumn = RootIdColumn,
                            Sort = 2
                        },
                        new AppConfigPackLinkTargetDto
                        {
                            Name = "Delete",
                            ActionType = "Delete",
                            TransactionIntegrationId = TxIntegrationId,
                            SourceColumn = RootIdColumn,
                            Sort = 3
                        }
                    },
                    Menu = new AppConfigPackMenuDto
                    {
                        RegisterInMainMenu = true,
                        MenuTitle = "RGB Color",
                        MenuOrder = 100
                    }
                }
            }
        };
    }
}
