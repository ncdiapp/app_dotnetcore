using System;
using System.Collections.Generic;
using System.Linq;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// POM Body Part + POM Template App Config via AppConfigPack (TX + list Search).
/// Folder-nav searches stay in PlmImportEngine.
/// </summary>
public static class PomImportAppConfigPackBuilder
{
    public const string BodyPartTable = "Plm_PdmV2kBodyPart";
    public const string BodyPartTxId = "PlmPom_BodyPart";
    public const string BodyPartListSearchId = "PlmPom_BodyPart_List";
    public const string BodyPartPk = "BodyPartID";

    public const string TemplateTable = "TchpPomTemplate";
    public const string TemplateTxId = "TchpPom_Template";
    public const string TemplateListSearchId = "TchpPom_Template_List";
    public const string TemplatePk = "PomTemplateId";

    public static AppConfigPackDto BuildBodyPart(int? saasApplicationId, IEnumerable<string> listViewColumns = null)
        => BuildSingleTable(
            saasApplicationId,
            BodyPartTable,
            BodyPartTxId,
            BodyPartListSearchId,
            BodyPartPk,
            "POM",
            "POM Management",
            listViewColumns);

    public static AppConfigPackDto BuildTemplate(int? saasApplicationId, IEnumerable<string> listViewColumns = null)
        => BuildSingleTable(
            saasApplicationId,
            TemplateTable,
            TemplateTxId,
            TemplateListSearchId,
            TemplatePk,
            "POM Template",
            "POM Template Management",
            listViewColumns);

    private static AppConfigPackDto BuildSingleTable(
        int? saasApplicationId,
        string tableName,
        string txIntegrationId,
        string listSearchIntegrationId,
        string rootIdColumn,
        string txName,
        string searchName,
        IEnumerable<string> listViewColumns)
    {
        var columns = (listViewColumns ?? Enumerable.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!columns.Any(c => string.Equals(c, rootIdColumn, StringComparison.OrdinalIgnoreCase)))
            columns.Insert(0, rootIdColumn);

        var viewFields = new List<AppConfigPackSearchViewFieldDto>();
        int sort = 1;
        foreach (string col in columns)
        {
            bool isRoot = string.Equals(col, rootIdColumn, StringComparison.OrdinalIgnoreCase);
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
                GeneratedBy = "plm-pom-import",
                ApplicationName = txName,
                SaasApplicationId = saasApplicationId
            },
            Transactions =
            {
                new AppConfigPackTransactionDto
                {
                    IntegrationId = txIntegrationId,
                    Name = txName,
                    Description = txName,
                    OrganizedType = "MasterDetail",
                    UnitStructure = new AppConfigPackUnitStructureDto { RootTableName = tableName }
                }
            },
            Searches =
            {
                new AppConfigPackSearchDto
                {
                    IntegrationId = listSearchIntegrationId,
                    Name = searchName,
                    Description = searchName,
                    UsageType = "Management",
                    AutoExecute = true,
                    DataSet = new AppConfigPackDataSetDto
                    {
                        Name = searchName,
                        PrimaryTableName = tableName,
                        QueryText = $"SELECT * FROM [dbo].[{tableName}]"
                    },
                    SearchView = new AppConfigPackSearchViewDto
                    {
                        Name = searchName + " Grid",
                        IntegrationId = listSearchIntegrationId + "_View",
                        GridOutputMode = 1,
                        Fields = viewFields
                    },
                    LinkTargets =
                    {
                        new AppConfigPackLinkTargetDto
                        {
                            Name = "Create",
                            ActionType = "Create",
                            TransactionIntegrationId = txIntegrationId,
                            SourceColumn = rootIdColumn,
                            Sort = 1
                        },
                        new AppConfigPackLinkTargetDto
                        {
                            Name = "Open",
                            ActionType = "Edit",
                            TransactionIntegrationId = txIntegrationId,
                            SourceColumn = rootIdColumn,
                            Sort = 2
                        },
                        new AppConfigPackLinkTargetDto
                        {
                            Name = "Delete",
                            ActionType = "Delete",
                            TransactionIntegrationId = txIntegrationId,
                            SourceColumn = rootIdColumn,
                            Sort = 3
                        }
                    },
                    Menu = new AppConfigPackMenuDto
                    {
                        RegisterInMainMenu = true,
                        MenuTitle = searchName,
                        MenuOrder = 100
                    }
                }
            }
        };
    }
}
