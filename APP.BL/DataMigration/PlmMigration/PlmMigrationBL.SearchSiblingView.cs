using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public const string SiblingViewModeEnrichDataSet = "SiblingViewEnrichDataSet";

        public static OperationCallResult<PlmSearchSiblingViewBlueprintDto> LoadSearchSiblingViewBlueprint(
            PlmSearchSiblingViewLoadRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchSiblingViewBlueprintDto>();
            try
            {
                RequirePlmMigrationAdmin();
                if (request == null || string.IsNullOrWhiteSpace(request.BlueprintJson))
                    throw new ArgumentException("BlueprintJson is required.");

                var blueprint = JsonConvert.DeserializeObject<PlmSearchSiblingViewBlueprintDto>(request.BlueprintJson);
                if (blueprint == null)
                    throw new InvalidOperationException("Sibling view blueprint JSON could not be deserialized.");

                if (blueprint.SchemaVersion <= 0)
                    blueprint.SchemaVersion = 1;
                if (string.IsNullOrWhiteSpace(blueprint.Mode))
                    blueprint.Mode = SiblingViewModeEnrichDataSet;

                result.Object = blueprint;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchSibling_Load_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportValidationDto> ValidateSearchSiblingViewBlueprint(
            PlmSearchSiblingViewBlueprintDto blueprint)
        {
            var result = new OperationCallResult<PlmSearchImportValidationDto>
            {
                Object = new PlmSearchImportValidationDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                string tenantConn = GetTenantConnectionString();
                ValidateSearchSiblingViewBlueprintInternal(blueprint, tenantConn, result.Object);
                result.Object.IsValid = result.Object.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Object.Errors.Add(ex.Message);
                result.Object.IsValid = false;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchSibling_Validate_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportPreviewDto> PreviewSearchSiblingViewConfig(
            PlmSearchSiblingViewBlueprintDto blueprint)
        {
            var result = new OperationCallResult<PlmSearchImportPreviewDto>
            {
                Object = new PlmSearchImportPreviewDto { IsSuccess = true }
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (blueprint == null)
                    throw new ArgumentException("Blueprint is required.");

                string tenantConn = GetTenantConnectionString();
                result.Object.Items = BuildSearchSiblingViewPreviewItems(blueprint, tenantConn);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchSibling_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchSiblingViewExecuteResultDto> ExecuteSearchSiblingViewConfig(
            PlmSearchSiblingViewExecuteRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchSiblingViewExecuteResultDto>
            {
                Object = new PlmSearchSiblingViewExecuteResultDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (request?.Blueprint == null)
                    throw new ArgumentException("Blueprint is required.");

                string tenantConn = GetTenantConnectionString();
                var validation = new PlmSearchImportValidationDto();
                ValidateSearchSiblingViewBlueprintInternal(request.Blueprint, tenantConn, validation);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                int? saasApplicationId = request.SaasApplicationId;
                result.Object = ExecuteSearchSiblingViewConfigCore(request.Blueprint, tenantConn, saasApplicationId);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchSibling_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void ValidateSearchSiblingViewBlueprintInternal(
            PlmSearchSiblingViewBlueprintDto blueprint,
            string tenantConn,
            PlmSearchImportValidationDto validation)
        {
            if (blueprint == null)
            {
                validation.Errors.Add("Blueprint is required.");
                return;
            }

            if (!string.Equals(blueprint.Mode, SiblingViewModeEnrichDataSet, StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add(
                    $"Mode must be '{SiblingViewModeEnrichDataSet}' for Option A. Option B uses the main Search Import blueprint.");
            }

            if (blueprint.Coverage != null && blueprint.Coverage.RequiresOneToN > 0)
            {
                validation.Errors.Add(
                    "Coverage.RequiresOneToN > 0 — Option A cannot enrich DataSet with 1:N joins. Use Option B (new Search).");
            }

            if (blueprint.Target == null
                || (string.IsNullOrWhiteSpace(blueprint.Target.AppSearchIntegrationId)
                    && !(blueprint.Target.AppSearchId > 0)))
            {
                validation.Errors.Add("Target.AppSearchIntegrationId or Target.AppSearchId is required.");
            }

            if (blueprint.SearchView == null || string.IsNullOrWhiteSpace(blueprint.SearchView.Name))
                validation.Errors.Add("SearchView.Name is required.");

            if (blueprint.SearchView?.Fields == null || blueprint.SearchView.Fields.Count == 0)
                validation.Errors.Add("SearchView.Fields must contain at least one field.");

            foreach (var join in blueprint.DataSetPatch?.AddLeftJoins ?? Enumerable.Empty<PlmSearchSiblingViewAddJoinDto>())
            {
                if (join == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(join.Cardinality)
                    && !string.Equals(join.Cardinality, "1:1", StringComparison.OrdinalIgnoreCase))
                {
                    validation.Errors.Add(
                        $"AddLeftJoins '{join.AppTableName}' cardinality '{join.Cardinality}' is not allowed in Option A (1:1 only).");
                }
            }

            bool hasResultingQuery = !string.IsNullOrWhiteSpace(blueprint.DataSetPatch?.ResultingQueryText);
            bool hasPatchOps = (blueprint.DataSetPatch?.AddColumns?.Count ?? 0) > 0
                || (blueprint.DataSetPatch?.AddLeftJoins?.Count ?? 0) > 0;
            if (!hasResultingQuery && !hasPatchOps)
            {
                validation.Warnings.Add(
                    "DataSetPatch has no ResultingQueryText and no addColumns/addLeftJoins — DataSet query will be left unchanged.");
            }

            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                int? searchId = ResolveSiblingTargetSearchId(conn, blueprint.Target);
                if (!searchId.HasValue)
                {
                    validation.Errors.Add(
                        $"APP Search not found for IntegrationId '{blueprint.Target?.AppSearchIntegrationId}' / Id '{blueprint.Target?.AppSearchId}'. Run main Search Import first.");
                    return;
                }

                int? dataSetId = GetDataSetIdForSearch(conn, searchId.Value);
                if (!dataSetId.HasValue)
                    validation.Errors.Add($"Search #{searchId} has no DataSetId.");

                if (!string.IsNullOrWhiteSpace(blueprint.SearchView?.Name) && dataSetId.HasValue)
                {
                    int? existingSibling = GetSearchViewIdByDataSetAndName(conn, dataSetId.Value, blueprint.SearchView.Name);
                    if (existingSibling.HasValue)
                        validation.Warnings.Add(
                            $"Sibling view '{blueprint.SearchView.Name}' already exists as SearchView #{existingSibling} — execute will update it.");
                }
            }
        }

        private static List<PlmSearchImportPreviewItemDto> BuildSearchSiblingViewPreviewItems(
            PlmSearchSiblingViewBlueprintDto blueprint,
            string tenantConn)
        {
            var items = new List<PlmSearchImportPreviewItemDto>();
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                int? searchId = ResolveSiblingTargetSearchId(conn, blueprint.Target);
                int? dataSetId = searchId.HasValue ? GetDataSetIdForSearch(conn, searchId.Value) : null;
                int? defaultViewId = searchId.HasValue ? GetSearchViewIdForSearch(conn, searchId.Value) : null;

                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "Search",
                    Name = blueprint.Source?.PlmSearchName ?? blueprint.Target?.AppSearchIntegrationId,
                    IntegrationId = blueprint.Target?.AppSearchIntegrationId,
                    Action = SearchImportActionUpdate,
                    ExistingId = searchId,
                    Detail = "Existing Search — default View unchanged"
                });

                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "DataSet",
                    Name = "Enrich DataSet",
                    IntegrationId = null,
                    Action = SearchImportActionUpdate,
                    ExistingId = dataSetId,
                    Detail = DescribeDataSetPatch(blueprint.DataSetPatch)
                });

                int? siblingId = null;
                if (dataSetId.HasValue && !string.IsNullOrWhiteSpace(blueprint.SearchView?.Name))
                    siblingId = GetSearchViewIdByDataSetAndName(conn, dataSetId.Value, blueprint.SearchView.Name);

                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "SiblingView",
                    Name = blueprint.SearchView?.Name,
                    IntegrationId = blueprint.SearchView?.IntegrationId,
                    Action = siblingId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = siblingId,
                    Detail = $"Fields: {blueprint.SearchView?.Fields?.Count ?? 0}; PLM View {blueprint.Source?.PlmReferenceViewId?.ToString() ?? "n/a"}; default View #{defaultViewId}"
                });

                bool copyLinks = blueprint.LinkTargets?.CopyFromDefaultSearchView != false;
                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "LinkTarget",
                    Name = copyLinks ? "Copy from default View" : "Custom / none",
                    IntegrationId = null,
                    Action = SearchImportActionInsert,
                    ExistingId = defaultViewId,
                    Detail = copyLinks
                        ? "Copy AppFormLinkTarget rows from default SearchView"
                        : $"{blueprint.LinkTargets?.Items?.Count ?? 0} explicit link(s)"
                });
            }

            return items;
        }

        private static string DescribeDataSetPatch(PlmSearchSiblingViewDataSetPatchDto patch)
        {
            if (patch == null)
                return "No patch";
            if (!string.IsNullOrWhiteSpace(patch.ResultingQueryText))
                return "Replace QueryText (ResultingQueryText provided)";
            int cols = patch.AddColumns?.Count ?? 0;
            int joins = patch.AddLeftJoins?.Count ?? 0;
            return $"AddColumn={cols}, AddOneToOneLeftJoin={joins}";
        }

        private static PlmSearchSiblingViewExecuteResultDto ExecuteSearchSiblingViewConfigCore(
            PlmSearchSiblingViewBlueprintDto blueprint,
            string tenantConn,
            int? saasApplicationId)
        {
            var executeResult = new PlmSearchSiblingViewExecuteResultDto { IsSuccess = true };
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();

                int searchId = ResolveSiblingTargetSearchId(conn, blueprint.Target)
                    ?? throw new InvalidOperationException("Target APP Search not found.");
                executeResult.SearchId = searchId;

                int dataSetId = GetDataSetIdForSearch(conn, searchId)
                    ?? throw new InvalidOperationException($"Search #{searchId} has no DataSet.");
                executeResult.DataSetId = dataSetId;

                int? defaultViewId = GetSearchViewIdForSearch(conn, searchId);
                executeResult.DefaultSearchViewId = defaultViewId;

                string newQueryText = ResolveSiblingEnrichedQueryText(conn, dataSetId, blueprint.DataSetPatch);
                if (!string.IsNullOrWhiteSpace(newQueryText))
                {
                    UpdateDataSetQueryText(conn, dataSetId, newQueryText);
                    executeResult.Messages.Add($"DataSet #{dataSetId} QueryText updated.");
                }
                else
                {
                    executeResult.Messages.Add($"DataSet #{dataSetId} QueryText unchanged.");
                }

                var viewFields = BuildSearchImportViewFields(conn, blueprint.SearchView.Fields);
                int gridOutputMode = blueprint.SearchView.GridOutputMode > 0 ? blueprint.SearchView.GridOutputMode : 1;
                int viewType = ResolveSearchViewType(blueprint.SearchView.ViewType);
                int siblingViewId = SaveSiblingSearchView(
                    conn,
                    dataSetId,
                    blueprint.SearchView.Name,
                    viewFields,
                    gridOutputMode,
                    viewType,
                    saasApplicationId);
                executeResult.SiblingSearchViewId = siblingViewId;
                executeResult.Messages.Add(
                    $"Sibling SearchView #{siblingViewId} ('{blueprint.SearchView.Name}') saved with {viewFields.Count} field(s).");

                // Ensure Search still points at original default View
                if (defaultViewId.HasValue)
                {
                    EnsureSearchDefaultView(conn, searchId, defaultViewId.Value, dataSetId);
                }

                ClearSearchViewFormLinkTargets(conn, siblingViewId);
                bool copyLinks = blueprint.LinkTargets?.CopyFromDefaultSearchView != false;
                if (copyLinks && defaultViewId.HasValue)
                {
                    int copied = CopyFormLinkTargetsBetweenSearchViews(conn, defaultViewId.Value, siblingViewId);
                    executeResult.Messages.Add($"Copied {copied} link target(s) from default View #{defaultViewId}.");
                }
                else if (blueprint.LinkTargets?.Items != null && blueprint.LinkTargets.Items.Count > 0)
                {
                    string rootColumn = blueprint.SearchView.Fields?
                        .FirstOrDefault(f => f.IsTransRootId)?.SysTableFiledPath ?? "ReferenceId";
                    int? rootFieldId = GetSearchViewFieldId(conn, null, siblingViewId, rootColumn);
                    if (!rootFieldId.HasValue)
                        throw new InvalidOperationException($"Sibling view is missing root column '{rootColumn}'.");

                    foreach (var link in blueprint.LinkTargets.Items)
                    {
                        int? transactionId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                        if (!transactionId.HasValue)
                            throw new InvalidOperationException($"Transaction '{link.TransactionIntegrationId}' not found.");

                        InsertSearchFormLinkTarget(
                            conn,
                            siblingViewId,
                            link.Name,
                            ResolveLinkTargetActionType(link.ActionType),
                            transactionId.Value,
                            rootFieldId.Value,
                            link.SourceColumn ?? rootColumn,
                            link.Sort ?? 1,
                            link.LinkTargetTransactionGroupId);
                    }
                    executeResult.Messages.Add($"Configured {blueprint.LinkTargets.Items.Count} explicit link target(s).");
                }
            }

            return executeResult;
        }

        private static int? ResolveSiblingTargetSearchId(SqlConnection conn, PlmSearchSiblingViewTargetDto target)
        {
            if (target == null)
                return null;
            // Prefer IntegrationId — numeric AppSearchId may not match the current tenant.
            if (!string.IsNullOrWhiteSpace(target.AppSearchIntegrationId))
            {
                int? byIntegration = GetSearchIdByIntegrationId(conn, null, target.AppSearchIntegrationId);
                if (byIntegration.HasValue)
                    return byIntegration;
            }

            if (target.AppSearchId.HasValue && target.AppSearchId.Value > 0)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT 1 FROM dbo.AppSearch WHERE SearchID = @SearchId";
                    cmd.Parameters.AddWithValue("@SearchId", target.AppSearchId.Value);
                    if (cmd.ExecuteScalar() != null)
                        return target.AppSearchId.Value;
                }
            }

            return null;
        }

        private static int? GetDataSetIdForSearch(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DataSetID FROM dbo.AppSearch WHERE SearchID = @SearchId";
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                var val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value)
                    return null;
                int id = Convert.ToInt32(val);
                return id > 0 ? (int?)id : null;
            }
        }

        private static int? GetSearchViewIdByDataSetAndName(SqlConnection conn, int dataSetId, string name)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 SearchViewID
FROM dbo.AppSearchView
WHERE DataSetID = @DataSetId AND Name = @Name
ORDER BY SearchViewID";
                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                cmd.Parameters.AddWithValue("@Name", name);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetDataSetQueryText(SqlConnection conn, int dataSetId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT QueryText FROM dbo.AppDataSet WHERE DataSetID = @DataSetId";
                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? null : Convert.ToString(val);
            }
        }

        private static void UpdateDataSetQueryText(SqlConnection conn, int dataSetId, string queryText)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE dbo.AppDataSet SET QueryText = @QueryText WHERE DataSetID = @DataSetId";
                cmd.Parameters.AddWithValue("@QueryText", queryText);
                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                cmd.ExecuteNonQuery();
            }

            // Refresh via BL so caches / metadata stay consistent when possible
            try
            {
                AppDataSetExDto dataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(dataSetId);
                dataSetDto.QueryText = queryText;
                dataSetDto.IsModified = true;
                AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
            }
            catch
            {
                // SQL update already applied
            }
        }

        private static string ResolveSiblingEnrichedQueryText(
            SqlConnection conn,
            int dataSetId,
            PlmSearchSiblingViewDataSetPatchDto patch)
        {
            if (patch == null)
                return null;

            if (!string.IsNullOrWhiteSpace(patch.ResultingQueryText))
                return patch.ResultingQueryText;

            string existing = GetDataSetQueryText(conn, dataSetId);
            if (string.IsNullOrWhiteSpace(existing))
                return null;

            bool hasOps = (patch.AddColumns?.Count ?? 0) > 0 || (patch.AddLeftJoins?.Count ?? 0) > 0;
            if (!hasOps)
                return null;

            return ApplySiblingDataSetPatchToQuery(existing, patch);
        }

        /// <summary>
        /// Best-effort SQL enrich: insert new SELECT columns before FROM, append LEFT JOINs before WHERE/ORDER/end.
        /// Prefer ResultingQueryText from the agent when available.
        /// </summary>
        private static string ApplySiblingDataSetPatchToQuery(string queryText, PlmSearchSiblingViewDataSetPatchDto patch)
        {
            string sql = queryText.Trim();
            var selectExtras = new List<string>();
            foreach (var col in patch.AddColumns ?? Enumerable.Empty<PlmSearchSiblingViewAddColumnDto>())
            {
                if (string.IsNullOrWhiteSpace(col?.SysTableFiledPath))
                    continue;
                string path = col.SysTableFiledPath.Trim();
                if (sql.IndexOf("[" + path + "]", StringComparison.OrdinalIgnoreCase) >= 0
                    || sql.IndexOf("." + path, StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                string alias = string.IsNullOrWhiteSpace(col.Alias) ? null : col.Alias.Trim();
                if (string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(col.AppTableName))
                {
                    // leave unqualified if no alias — agent should prefer ResultingQueryText
                    selectExtras.Add($"[{path}]");
                }
                else if (!string.IsNullOrWhiteSpace(alias))
                {
                    selectExtras.Add($"[{alias}].[{path}]");
                }
                else
                {
                    selectExtras.Add($"[{path}]");
                }
            }

            if (selectExtras.Count > 0)
            {
                var fromMatch = Regex.Match(sql, @"\bFROM\b", RegexOptions.IgnoreCase);
                if (fromMatch.Success)
                {
                    int insertAt = fromMatch.Index;
                    string before = sql.Substring(0, insertAt).TrimEnd();
                    string after = sql.Substring(insertAt);
                    if (before.EndsWith(",", StringComparison.Ordinal))
                        before = before.TrimEnd().TrimEnd(',');
                    sql = before + "," + Environment.NewLine + "  " + string.Join("," + Environment.NewLine + "  ", selectExtras)
                        + Environment.NewLine + after;
                }
            }

            var joinSql = new StringBuilder();
            foreach (var join in patch.AddLeftJoins ?? Enumerable.Empty<PlmSearchSiblingViewAddJoinDto>())
            {
                if (join == null || string.IsNullOrWhiteSpace(join.AppTableName))
                    continue;
                if (sql.IndexOf(join.AppTableName, StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                string alias = string.IsNullOrWhiteSpace(join.Alias) ? "j" + joinSql.Length : join.Alias.Trim();
                string leftTable = join.LeftTable ?? "";
                string leftCol = string.IsNullOrWhiteSpace(join.LeftColumn) ? "ReferenceId" : join.LeftColumn;
                string rightCol = string.IsNullOrWhiteSpace(join.RightColumn) ? "ReferenceId" : join.RightColumn;
                string leftExpr = string.IsNullOrWhiteSpace(leftTable)
                    ? $"[{leftCol}]"
                    : $"[{leftTable}].[{leftCol}]";

                joinSql.AppendLine(
                    $"LEFT OUTER JOIN [dbo].[{join.AppTableName}] AS [{alias}] ON [{alias}].[{rightCol}] = {leftExpr}");
            }

            if (joinSql.Length > 0)
            {
                var whereMatch = Regex.Match(sql, @"\bWHERE\b|\bORDER\s+BY\b|\bGROUP\s+BY\b", RegexOptions.IgnoreCase);
                if (whereMatch.Success)
                {
                    sql = sql.Substring(0, whereMatch.Index).TrimEnd()
                        + Environment.NewLine + joinSql.ToString().TrimEnd()
                        + Environment.NewLine + sql.Substring(whereMatch.Index);
                }
                else
                {
                    sql = sql.TrimEnd() + Environment.NewLine + joinSql.ToString().TrimEnd();
                }
            }

            return sql;
        }

        private static int ResolveSearchViewType(string viewType)
        {
            if (string.IsNullOrWhiteSpace(viewType))
                return (int)EmAppViewType.GridView;
            if (string.Equals(viewType, "CardView", StringComparison.OrdinalIgnoreCase)
                || string.Equals(viewType, "Card", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppViewType.CardView;
            if (Enum.TryParse(viewType, true, out EmAppViewType parsed))
                return (int)parsed;
            return (int)EmAppViewType.GridView;
        }

        private static int SaveSiblingSearchView(
            SqlConnection conn,
            int dataSetId,
            string name,
            ObservableSet<AppSearchViewFieldExDto> viewFields,
            int gridOutputMode,
            int viewType,
            int? saasApplicationId)
        {
            int? existingId = GetSearchViewIdByDataSetAndName(conn, dataSetId, name);
            AppSearchViewExDto searchViewDto;
            if (existingId.HasValue)
            {
                ClearSearchViewFieldsOnConnection(existingId.Value);
                searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(existingId.Value);
                searchViewDto.Name = name;
                searchViewDto.Description = name;
                searchViewDto.DataSetId = dataSetId;
                searchViewDto.GridOutputMode = gridOutputMode;
                searchViewDto.ViewType = viewType;
                searchViewDto.IsModified = true;
                searchViewDto.AppSearchViewFieldList = viewFields;
            }
            else
            {
                searchViewDto = new AppSearchViewExDto
                {
                    Name = name,
                    Description = name,
                    DataSetId = dataSetId,
                    GridOutputMode = gridOutputMode,
                    ViewType = viewType,
                    SaasApplicationId = saasApplicationId,
                    AppSearchViewFieldList = viewFields
                };
            }

            var saveViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
            if (!saveViewResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveViewResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to save sibling search view.");
            }

            return Convert.ToInt32(saveViewResult.Object.Id);
        }

        private static void EnsureSearchDefaultView(SqlConnection conn, int searchId, int defaultViewId, int dataSetId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppSearch
SET SearchViewID = @ViewId, DataSetId = @DataSetId
WHERE SearchID = @SearchId";
                cmd.Parameters.AddWithValue("@ViewId", defaultViewId);
                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int CopyFormLinkTargetsBetweenSearchViews(
            SqlConnection conn,
            int sourceSearchViewId,
            int targetSearchViewId)
        {
            var rows = new List<(string Name, int ActionType, int TxId, int? GroupId, string TargetCol, int Sort, int SourceFieldId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    NavigationActionName,
    ActionType,
    LinkTargetTransactionID,
    LinkTargetTransactionGroupID,
    TargetColumn1,
    Sort,
    SourceViewColumnID1
FROM dbo.AppFormLinkTarget
WHERE SearchViewID = @ViewId
ORDER BY Sort, LinkTargetID";
                cmd.Parameters.AddWithValue("@ViewId", sourceSearchViewId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add((
                            reader["NavigationActionName"]?.ToString() ?? "Open",
                            Convert.ToInt32(reader["ActionType"]),
                            Convert.ToInt32(reader["LinkTargetTransactionID"]),
                            reader["LinkTargetTransactionGroupID"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(reader["LinkTargetTransactionGroupID"]),
                            reader["TargetColumn1"]?.ToString() ?? "ReferenceId",
                            reader["Sort"] == DBNull.Value ? 1 : Convert.ToInt32(reader["Sort"]),
                            reader["SourceViewColumnID1"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["SourceViewColumnID1"])));
                    }
                }
            }

            int copied = 0;
            foreach (var row in rows)
            {
                string sourcePath = null;
                if (row.SourceFieldId > 0)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT SysTableFiledPath FROM dbo.AppSearchViewField WHERE SearchViewFieldID = @Id";
                        cmd.Parameters.AddWithValue("@Id", row.SourceFieldId);
                        sourcePath = cmd.ExecuteScalar()?.ToString();
                    }
                }

                string path = !string.IsNullOrWhiteSpace(sourcePath) ? sourcePath : row.TargetCol;
                int? targetFieldId = GetSearchViewFieldId(conn, null, targetSearchViewId, path);
                if (!targetFieldId.HasValue)
                    targetFieldId = GetSearchViewFieldId(conn, null, targetSearchViewId, row.TargetCol);
                if (!targetFieldId.HasValue)
                    continue;

                InsertSearchFormLinkTarget(
                    conn,
                    targetSearchViewId,
                    row.Name,
                    row.ActionType,
                    row.TxId,
                    targetFieldId.Value,
                    row.TargetCol,
                    row.Sort,
                    row.GroupId);
                copied++;
            }

            return copied;
        }
    }
}
