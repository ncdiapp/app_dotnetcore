using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
    {
        public const string StepSearchImport = "SearchImport";

        private const string SearchImportActionInsert = "Insert";
        private const string SearchImportActionUpdate = "Update";

        public static OperationCallResult<PlmSearchImportBlueprintDto> LoadSearchImportBlueprint(PlmSearchImportLoadRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchImportBlueprintDto>();
            try
            {
                RequirePlmMigrationAdmin();
                if (request == null || string.IsNullOrWhiteSpace(request.BlueprintJson))
                    throw new ArgumentException("BlueprintJson is required.");

                var blueprint = JsonConvert.DeserializeObject<PlmSearchImportBlueprintDto>(request.BlueprintJson);
                if (blueprint == null)
                    throw new InvalidOperationException("Search blueprint JSON could not be deserialized.");

                NormalizeSearchImportBlueprint(blueprint);

                if (blueprint.SchemaVersion <= 0)
                    blueprint.SchemaVersion = 1;

                result.Object = blueprint;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportEngine), "Plm_SearchImport_Load_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportValidationDto> ValidateSearchImportBlueprint(PlmSearchImportBlueprintDto blueprint)
        {
            var result = new OperationCallResult<PlmSearchImportValidationDto>
            {
                Object = new PlmSearchImportValidationDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                string tenantConn = GetTenantConnectionString();
                ValidateSearchImportBlueprintInternal(blueprint, tenantConn, result.Object);
                result.Object.IsValid = result.Object.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Object.Errors.Add(ex.Message);
                result.Object.IsValid = false;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportEngine), "Plm_SearchImport_Validate_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportPreviewDto> PreviewSearchBlueprintConfig(PlmSearchImportBlueprintDto blueprint)
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

                NormalizeSearchImportBlueprint(blueprint);
                string tenantConn = GetTenantConnectionString();
                var validation = new PlmSearchImportValidationDto();
                ValidateSearchImportBlueprintInternal(blueprint, tenantConn, validation);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                var pack = SearchImportAppConfigPackBuilder.Build(
                    blueprint, blueprint.Search?.SaasApplicationId);
                var packPreview = APP.BL.AppConfigPack.AppConfigPackBL.Preview(new AppConfigPackExecuteRequestDto
                {
                    Pack = pack,
                    SaasApplicationId = pack.Source?.SaasApplicationId
                });

                if (packPreview.Object == null || !packPreview.Object.IsSuccess)
                {
                    result.Object.IsSuccess = false;
                    result.Object.ErrorMessage = packPreview.Object?.ErrorMessage
                        ?? "AppConfigPack preview failed.";
                    return result;
                }

                result.Object.Items = (packPreview.Object.Items ?? new List<AppConfigPackPreviewItemDto>())
                    .Select(i => new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = i.ObjectType,
                        Name = i.Name,
                        IntegrationId = i.IntegrationId,
                        Action = i.Action,
                        ExistingId = i.ExistingId,
                        Detail = i.Detail
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportEngine), "Plm_SearchImport_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportExecuteResultDto> ExecuteSearchBlueprintConfig(PlmSearchImportExecuteRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchImportExecuteResultDto>
            {
                Object = new PlmSearchImportExecuteResultDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (request?.Blueprint == null)
                    throw new ArgumentException("Blueprint is required.");

                NormalizeSearchImportBlueprint(request.Blueprint);
                string tenantConn = GetTenantConnectionString();
                var validation = new PlmSearchImportValidationDto();
                ValidateSearchImportBlueprintInternal(request.Blueprint, tenantConn, validation);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                int? saasApplicationId = request.SaasApplicationId
                    ?? request.Blueprint.Search?.SaasApplicationId;

                var pack = SearchImportAppConfigPackBuilder.Build(request.Blueprint, saasApplicationId);
                // Compose → AppConfigPackBL.Execute (shared public steps).
                var packExec = APP.BL.AppConfigPack.AppConfigPackBL.Execute(new AppConfigPackExecuteRequestDto
                {
                    Pack = pack,
                    SaasApplicationId = saasApplicationId
                });

                if (packExec.Object == null || !packExec.Object.IsSuccess)
                {
                    result.Object.IsSuccess = false;
                    result.Object.ErrorMessage = packExec.Object?.ErrorMessage
                        ?? "AppConfigPack execute failed.";
                    if (packExec.Object?.Messages != null)
                        result.Object.Messages.AddRange(packExec.Object.Messages);
                    return result;
                }

                result.Object.IsSuccess = true;
                if (packExec.Object.Messages != null)
                    result.Object.Messages.AddRange(packExec.Object.Messages);
                result.Object.Messages.Add("Applied via AppConfigPackBL (shared JSON→App Config).");

                using (var conn = new SqlConnection(tenantConn))
                {
                    conn.Open();
                    string searchIntegrationId = request.Blueprint.Search.IntegrationId;
                    int? searchId = GetSearchIdByIntegrationId(conn, null, searchIntegrationId);
                    if (searchId.HasValue)
                    {
                        result.Object.SearchId = searchId.Value;
                        var searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId.Value);
                        result.Object.DataSetId = searchDto?.DataSetId;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = FormatSearchImportException(ex);
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmImportEngine), "Plm_SearchImport_Execute_Error", ValidationItemType.Error,
                    result.Object.ErrorMessage));
            }

            return result;
        }

        private static void ValidateSearchImportBlueprintInternal(
            PlmSearchImportBlueprintDto blueprint,
            string tenantConn,
            PlmSearchImportValidationDto validation)
        {
            if (blueprint == null)
            {
                validation.Errors.Add("Blueprint is required.");
                return;
            }

            NormalizeSearchImportBlueprint(blueprint);

            if (blueprint.SchemaVersion <= 0)
                validation.Warnings.Add("SchemaVersion missing — defaulting to 1 at execute time.");

            if (blueprint.Search == null || string.IsNullOrWhiteSpace(blueprint.Search.IntegrationId))
                validation.Errors.Add("search.integrationId is required.");

            if (blueprint.DataSet == null || string.IsNullOrWhiteSpace(blueprint.DataSet.QueryText))
                validation.Errors.Add("dataSet.queryText is required.");

            if (blueprint.SearchView == null || string.IsNullOrWhiteSpace(blueprint.SearchView.IntegrationId))
                validation.Errors.Add("searchView.integrationId is required (or views[] with integrationId).");

            var viewFields = blueprint.SearchView?.Fields;
            if (viewFields == null || viewFields.Count == 0)
                validation.Errors.Add("searchView.fields must contain at least one column (or fieldResolution role=view).");
            else if (!viewFields.Any(f => f != null && f.IsTransRootId))
                validation.Errors.Add("searchView.fields must include one field with isTransRootId=true (typically ReferenceId).");
            else
            {
                int mappedViewCols = viewFields.Count(f => f != null && !f.IsTransRootId);
                bool hasDefaultViewShell = (blueprint.Views ?? new List<PlmSearchImportViewShellDto>())
                    .Any(v => v != null && (v.IsDefault || v.ReferenceViewId.HasValue));
                if (hasDefaultViewShell && mappedViewCols <= 1)
                    validation.Errors.Add(
                        "Default PLM view columns were not copied into searchView.fields (only a views[] shell). "
                        + "Re-run Phase B: probe pdmReferenceViewColumn for SearchTemplate.ReferenceViewID and emit every visible column via FieldMapping.");
            }

            AssertSearchCoverageMatchesEmitted(blueprint, validation);

            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                string primaryTable = blueprint.DataSet?.PrimaryTableName
                    ?? blueprint.Source?.PrimaryTableName
                    ?? blueprint.DataSet?.RootTableName;
                if (!string.IsNullOrWhiteSpace(primaryTable) && !TemplateTableExists(conn, null, primaryTable))
                    validation.Errors.Add($"Primary table dbo.[{primaryTable}] does not exist in tenant database.");

                foreach (var join in EnumerateJoinTables(blueprint))
                {
                    if (!string.IsNullOrWhiteSpace(join) && !TemplateTableExists(conn, null, join))
                        validation.Errors.Add($"JOIN table dbo.[{join}] does not exist in tenant database.");
                }

                foreach (var link in blueprint.LinkTargets ?? Enumerable.Empty<PlmSearchImportLinkTargetDto>())
                {
                    if (string.IsNullOrWhiteSpace(link.TransactionIntegrationId))
                        continue;

                    int? txId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                    if (!txId.HasValue)
                        validation.Errors.Add($"Link target transaction '{link.TransactionIntegrationId}' was not found.");
                }

                int? groupId = ResolveLinkTargetTransactionGroupId(conn, blueprint);
                if (groupId.HasValue)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1 FROM dbo.AppTransactionGroup WHERE TransactionGroupID = @GroupId";
                        cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
                        if (cmd.ExecuteScalar() == null)
                            validation.Errors.Add($"Transaction group id {groupId.Value} was not found.");
                    }
                }

                RewriteSearchImportQuery(blueprint, conn, validation);
            }

            if ((blueprint.CriteriaFields?.Count ?? 0) == 0)
                validation.Warnings.Add("No criteriaFields in blueprint — search will have an empty criteria panel.");

            if (blueprint.UnmappedPlmFields?.Count > 0)
                validation.Warnings.Add($"{blueprint.UnmappedPlmFields.Count} PLM field(s) were intentionally unmapped.");
        }

        private static int CountUnmappedSearchFields(PlmSearchImportBlueprintDto blueprint, string role)
        {
            return blueprint?.UnmappedPlmFields?.Count(u =>
                u != null && string.Equals(u.Role, role, StringComparison.OrdinalIgnoreCase)) ?? 0;
        }

        private static void AssertSearchCoverageMatchesEmitted(
            PlmSearchImportBlueprintDto blueprint,
            PlmSearchImportValidationDto validation)
        {
            if (blueprint == null || validation == null)
                return;

            int criteriaEmitted = blueprint.CriteriaFields?.Count(f => f != null) ?? 0;
            int viewEmitted = blueprint.SearchView?.Fields?.Count(f => f != null && !f.IsTransRootId) ?? 0;
            var coverage = blueprint.Coverage;
            int criteriaMapped = coverage?.Criteria?.Mapped ?? 0;
            int criteriaTotal = coverage?.Criteria?.Total ?? 0;
            int viewMapped = coverage?.View?.Mapped ?? 0;
            int viewTotal = coverage?.View?.Total ?? 0;

            if (criteriaMapped > 0 && criteriaEmitted < criteriaMapped)
            {
                validation.Errors.Add(
                    $"coverage.criteria.mapped={criteriaMapped} but criteriaFields has {criteriaEmitted} item(s). "
                    + "Phase B stubbed the criteria panel. Re-run Phase B: emit every mapped DCU into criteriaFields "
                    + "(coverage.criteria.mapped must equal criteriaFields.Count).");
            }

            if (viewMapped > 0 && viewEmitted < viewMapped)
            {
                validation.Errors.Add(
                    $"coverage.view.mapped={viewMapped} but searchView.fields has {viewEmitted} non-root column(s). "
                    + "Phase B stubbed the default View. Re-run Phase B: emit every visible pdmReferenceViewColumn via FieldMapping "
                    + "(coverage.view.mapped must equal searchView.fields count excluding isTransRootId).");
            }

            int criteriaUnmapped = CountUnmappedSearchFields(blueprint, "criteria");
            int viewUnmapped = CountUnmappedSearchFields(blueprint, "view");

            if (criteriaTotal >= 8
                && criteriaEmitted < Math.Max(2, (criteriaTotal + 1) / 2)
                && (criteriaEmitted + criteriaUnmapped) < criteriaTotal)
            {
                validation.Errors.Add(
                    $"PLM criteria total={criteriaTotal} but only {criteriaEmitted} criteriaFields emitted "
                    + $"(unmappedPlmFields criteria={criteriaUnmapped}). Import every active DCU; list true misses in unmappedPlmFields.");
            }

            if (viewTotal >= 8
                && viewEmitted < Math.Max(2, (viewTotal + 1) / 2)
                && (viewEmitted + viewUnmapped) < viewTotal)
            {
                validation.Errors.Add(
                    $"PLM view total={viewTotal} but only {viewEmitted} searchView.fields emitted "
                    + $"(unmappedPlmFields view={viewUnmapped}). Import every visible pdmReferenceViewColumn.");
            }
        }

        private static IEnumerable<string> EnumerateJoinTables(PlmSearchImportBlueprintDto blueprint)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                var trimmed = name.Trim().Trim('[', ']');
                if (trimmed.StartsWith("dbo.", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed.Substring(4);
                if (!string.IsNullOrWhiteSpace(trimmed))
                    tables.Add(trimmed);
            }

            Add(blueprint.DataSet?.PrimaryTableName);
            Add(blueprint.Source?.PrimaryTableName);
            Add(blueprint.DataSet?.RootTableName);

            foreach (var fr in blueprint.FieldResolution ?? Enumerable.Empty<PlmSearchImportFieldResolutionDto>())
                Add(fr?.Resolved?.AppTableName);

            foreach (var join in blueprint.DataSet?.Joins ?? Enumerable.Empty<PlmSearchImportJoinDto>())
                Add(join?.AppTableName);

            foreach (var planTable in blueprint.JoinPlan?.Tables ?? Enumerable.Empty<PlmSearchImportJoinPlanTableDto>())
                Add(planTable?.AppTableName);

            // Bracketed identifiers only — IndexOf("Plm_Style_Header") must not match Plm_Style_Header_V2K_ERP.
            var sql = blueprint.DataSet?.QueryText;
            if (!string.IsNullOrWhiteSpace(sql))
            {
                foreach (Match m in Regex.Matches(sql, @"\[dbo\]\.\[([^\]]+)\]", RegexOptions.IgnoreCase))
                    Add(m.Groups[1].Value);
                foreach (Match m in Regex.Matches(
                    sql,
                    @"(?:FROM|JOIN)\s+(?:\[?dbo\]?\.)?\[([^\]]+)\]",
                    RegexOptions.IgnoreCase))
                    Add(m.Groups[1].Value);
            }

            return tables;
        }

        /// <summary>
        /// Child Phase B often emits fieldResolution + views[] instead of searchView/criteriaFields.
        /// Fill the execute shape so APPLY does not NullRef.
        /// </summary>
        private static void NormalizeSearchImportBlueprint(PlmSearchImportBlueprintDto blueprint)
        {
            if (blueprint == null)
                return;

            if (blueprint.Source != null
                && !blueprint.Source.PlmSearchTemplateId.HasValue
                && blueprint.Source.PlmSearchId.HasValue)
            {
                blueprint.Source.PlmSearchTemplateId = blueprint.Source.PlmSearchId;
            }

            if (blueprint.DataSet != null
                && string.IsNullOrWhiteSpace(blueprint.DataSet.PrimaryTableName)
                && !string.IsNullOrWhiteSpace(blueprint.DataSet.RootTableName))
            {
                blueprint.DataSet.PrimaryTableName = blueprint.DataSet.RootTableName;
            }

            blueprint.CriteriaFields ??= new List<PlmSearchImportCriteriaFieldDto>();
            blueprint.FieldResolution ??= new List<PlmSearchImportFieldResolutionDto>();
            blueprint.Views ??= new List<PlmSearchImportViewShellDto>();
            blueprint.LinkTargets ??= new List<PlmSearchImportLinkTargetDto>();

            if (blueprint.SearchView == null)
            {
                var shell = blueprint.Views.FirstOrDefault(v => v != null && v.IsDefault)
                    ?? blueprint.Views.FirstOrDefault(v => v != null);
                string searchName = blueprint.Search?.Name ?? "PLM Search";
                string searchKey = blueprint.Search?.IntegrationId ?? "Search";
                blueprint.SearchView = new PlmSearchImportSearchViewDto
                {
                    Name = shell?.Name ?? (searchName + " Grid"),
                    IntegrationId = !string.IsNullOrWhiteSpace(shell?.IntegrationId)
                        ? shell.IntegrationId
                        : searchKey + "_View",
                    ViewType = "GridView",
                    GridOutputMode = 1,
                    Fields = new List<PlmSearchImportSearchViewFieldDto>()
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(blueprint.SearchView.IntegrationId)
                    && !string.IsNullOrWhiteSpace(blueprint.Search?.IntegrationId))
                {
                    blueprint.SearchView.IntegrationId = blueprint.Search.IntegrationId + "_View";
                }
                blueprint.SearchView.Fields ??= new List<PlmSearchImportSearchViewFieldDto>();
            }

            if (blueprint.SearchView.Fields.Count == 0)
            {
                foreach (var fr in blueprint.FieldResolution.Where(IsViewResolution))
                {
                    var field = MapResolutionToViewField(fr);
                    if (field != null)
                        blueprint.SearchView.Fields.Add(field);
                }
            }

            if (blueprint.CriteriaFields.Count == 0)
            {
                foreach (var fr in blueprint.FieldResolution.Where(IsCriteriaResolution))
                {
                    var field = MapResolutionToCriteriaField(fr);
                    if (field != null)
                        blueprint.CriteriaFields.Add(field);
                }
            }

            EnsureSearchViewRootField(blueprint.SearchView);

            foreach (var link in blueprint.LinkTargets)
            {
                if (link == null)
                    continue;
                if (string.IsNullOrWhiteSpace(link.Name))
                    link.Name = link.ActionName ?? link.ActionType ?? "Open";
                if (string.IsNullOrWhiteSpace(link.SourceColumn))
                    link.SourceColumn = string.IsNullOrWhiteSpace(link.RootColumn) ? "ReferenceId" : link.RootColumn;
            }
        }

        private static bool IsViewResolution(PlmSearchImportFieldResolutionDto fr)
        {
            return fr != null
                && string.Equals(fr.Role, "view", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(fr.Resolved?.SysTableFiledPath ?? fr.Resolved?.AppColumnName);
        }

        private static bool IsCriteriaResolution(PlmSearchImportFieldResolutionDto fr)
        {
            return fr != null
                && string.Equals(fr.Role, "criteria", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(fr.Resolved?.SysTableFiledPath ?? fr.Resolved?.AppColumnName);
        }

        private static PlmSearchImportSearchViewFieldDto MapResolutionToViewField(PlmSearchImportFieldResolutionDto fr)
        {
            string path = fr.Resolved?.SysTableFiledPath ?? fr.Resolved?.AppColumnName;
            if (string.IsNullOrWhiteSpace(path))
                return null;
            return new PlmSearchImportSearchViewFieldDto
            {
                DisplayText = fr.PlmSource?.DisplayLabel ?? path,
                SysTableFiledPath = path.Trim(),
                ControlType = fr.ControlType,
                EntityIntegrationId = fr.EntityIntegrationId,
                IsTransRootId = fr.IsTransRootId || IsRootColumnName(path),
                IsVisible = fr.IsVisible,
                Sort = fr.Sort
            };
        }

        private static PlmSearchImportCriteriaFieldDto MapResolutionToCriteriaField(PlmSearchImportFieldResolutionDto fr)
        {
            string path = fr.Resolved?.SysTableFiledPath ?? fr.Resolved?.AppColumnName;
            if (string.IsNullOrWhiteSpace(path))
                return null;
            return new PlmSearchImportCriteriaFieldDto
            {
                IntegrationKey = "criteria_" + path.Trim(),
                DisplayText = fr.PlmSource?.DisplayLabel ?? path,
                SysTableFiledPath = path.Trim(),
                ControlType = fr.ControlType,
                EntityIntegrationId = fr.EntityIntegrationId,
                OperationId = fr.OperationId,
                PositionRow = fr.PositionRow,
                PositionColumn = fr.PositionColumn,
                IsVisible = fr.IsVisible,
                Sort = fr.Sort
            };
        }

        private static void EnsureSearchViewRootField(PlmSearchImportSearchViewDto searchView)
        {
            if (searchView?.Fields == null)
                return;

            if (searchView.Fields.Any(f => f != null && f.IsTransRootId))
                return;

            var inferred = searchView.Fields.FirstOrDefault(f => f != null && IsRootColumnName(f.SysTableFiledPath));
            if (inferred != null)
            {
                inferred.IsTransRootId = true;
                return;
            }

            searchView.Fields.Insert(0, new PlmSearchImportSearchViewFieldDto
            {
                DisplayText = "Ref No.",
                SysTableFiledPath = "ReferenceId",
                ControlType = 20,
                IsTransRootId = true,
                IsVisible = true,
                Sort = 5
            });
        }

        private static bool IsRootColumnName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            var col = path.Trim();
            var dot = col.LastIndexOf('.');
            if (dot >= 0 && dot < col.Length - 1)
                col = col.Substring(dot + 1);
            return col.Equals("ReferenceId", StringComparison.OrdinalIgnoreCase)
                || col.Equals("ReferenceBasicInfoID", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatSearchImportException(Exception ex)
        {
            if (ex == null)
                return "Unknown error.";
            var msg = ex.GetType().Name + ": " + (ex.Message ?? "");
            if (ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message))
                msg += " | " + ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
            return msg;
        }

        private static List<PlmSearchImportPreviewItemDto> BuildSearchImportPreviewItems(
            PlmSearchImportBlueprintDto blueprint,
            string tenantConn)
        {
            var items = new List<PlmSearchImportPreviewItemDto>();
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();

                string searchIntegrationId = blueprint.Search?.IntegrationId;
                int? searchId = GetSearchIdByIntegrationId(conn, null, searchIntegrationId);
                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "Search",
                    Name = blueprint.Search?.Name ?? searchIntegrationId,
                    IntegrationId = searchIntegrationId,
                    Action = searchId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = searchId,
                    Detail = $"Criteria fields: {blueprint.CriteriaFields?.Count ?? 0}"
                });

                int? dataSetId = null;
                if (searchId.HasValue)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DataSetId FROM dbo.AppSearch WHERE SearchID = @SearchId";
                        cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                        var val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            dataSetId = Convert.ToInt32(val);
                    }
                }

                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "DataSet",
                    Name = blueprint.DataSet?.Name ?? "Search DataSet",
                    IntegrationId = null,
                    Action = dataSetId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = dataSetId,
                    Detail = $"Primary: {blueprint.DataSet?.PrimaryTableName ?? blueprint.Source?.PrimaryTableName}"
                });

                int? searchViewId = searchId.HasValue
                    ? GetSearchViewIdForSearch(conn, searchId.Value)
                    : null;
                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "SearchView",
                    Name = blueprint.SearchView?.Name ?? blueprint.SearchView?.IntegrationId,
                    IntegrationId = blueprint.SearchView?.IntegrationId,
                    Action = searchViewId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = searchViewId,
                    Detail = $"View fields: {blueprint.SearchView?.Fields?.Count ?? 0}"
                });

                int? groupId = ResolveLinkTargetTransactionGroupId(conn, blueprint);
                if (groupId.HasValue)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "TransactionGroup",
                        Name = blueprint.TransactionGroup?.GroupName ?? $"Group {groupId}",
                        IntegrationId = null,
                        Action = SearchImportActionUpdate,
                        ExistingId = groupId,
                        Detail = "Open link will use LinkTargetTransactionGroupId"
                    });
                }

                foreach (var link in blueprint.LinkTargets ?? Enumerable.Empty<PlmSearchImportLinkTargetDto>())
                {
                    int? txId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "LinkTarget",
                        Name = link.Name,
                        IntegrationId = link.TransactionIntegrationId,
                        Action = SearchImportActionInsert,
                        ExistingId = txId,
                        Detail = link.ActionType
                    });
                }

                if (blueprint.Menu?.RegisterInMainMenu == true)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "Menu",
                        Name = blueprint.Menu.MenuTitle ?? blueprint.Search?.Name,
                        IntegrationId = searchIntegrationId,
                        Action = searchId.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                        ExistingId = searchId,
                        Detail = "Register in application main menu"
                    });
                }
            }

            return items;
        }

        private static PlmSearchImportExecuteResultDto ExecuteSearchBlueprintConfigCore(
            PlmSearchImportBlueprintDto blueprint,
            string tenantConn,
            int tenantDataSourceId,
            int? saasApplicationId)
        {
            var executeResult = new PlmSearchImportExecuteResultDto { IsSuccess = true };
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();

                string searchIntegrationId = blueprint.Search.IntegrationId;
                string searchName = blueprint.Search.Name ?? blueprint.Source?.PlmSearchName ?? "PLM Search";
                int searchType = ResolveSearchUsageType(blueprint.Search.UsageType);
                bool autoExecute = blueprint.Search.AutoExecute;

                int searchId = EnsureSearchShell(
                    conn, searchIntegrationId, searchName, searchType, saasApplicationId, autoExecute);
                executeResult.SearchId = searchId;
                executeResult.Messages.Add($"Search {searchId} ({searchIntegrationId}) ready.");

                string dataSetName = blueprint.DataSet.Name ?? searchName;
                int dataSetId = SaveSearchDataSet(searchId, dataSetName, blueprint.DataSet.QueryText, tenantDataSourceId, saasApplicationId);
                executeResult.DataSetId = dataSetId;
                executeResult.Messages.Add($"DataSet {dataSetId} saved.");

                var viewFields = BuildSearchImportViewFields(conn, blueprint.SearchView?.Fields);
                string viewName = blueprint.SearchView.Name ?? searchName;
                int gridOutputMode = blueprint.SearchView.GridOutputMode > 0 ? blueprint.SearchView.GridOutputMode : 1;
                int searchViewId = SaveSearchView(searchId, viewName, dataSetId, viewFields, gridOutputMode);
                executeResult.SearchViewId = searchViewId;
                executeResult.Messages.Add($"Search view {searchViewId} saved with {viewFields.Count} field(s).");

                ClearSearchCriteriaFields(conn, searchId);
                SaveSearchCriteriaFields(searchId, blueprint.CriteriaFields, conn);
                executeResult.Messages.Add($"Saved {blueprint.CriteriaFields?.Count ?? 0} criteria field(s).");

                string rootColumn = blueprint.SearchView.Fields?
                    .FirstOrDefault(f => f.IsTransRootId)?.SysTableFiledPath ?? "ReferenceId";
                int? rootFieldId = GetSearchViewFieldId(conn, null, searchViewId, rootColumn);
                if (!rootFieldId.HasValue)
                    throw new InvalidOperationException($"Search view is missing root column '{rootColumn}'.");

                ClearSearchViewFormLinkTargets(conn, searchViewId);
                int? transactionGroupId = ResolveLinkTargetTransactionGroupId(conn, blueprint);
                foreach (var link in blueprint.LinkTargets ?? Enumerable.Empty<PlmSearchImportLinkTargetDto>())
                {
                    int? transactionId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                    if (!transactionId.HasValue)
                        throw new InvalidOperationException($"Transaction '{link.TransactionIntegrationId}' not found for link target '{link.Name}'.");

                    int actionType = ResolveLinkTargetActionType(link.ActionType);
                    int sort = link.Sort ?? 1;
                    int? groupIdForLink = link.LinkTargetTransactionGroupId ?? transactionGroupId;
                    InsertSearchFormLinkTarget(
                        conn,
                        searchViewId,
                        link.Name,
                        actionType,
                        transactionId.Value,
                        rootFieldId.Value,
                        link.SourceColumn ?? rootColumn,
                        sort,
                        groupIdForLink);
                }
                executeResult.Messages.Add($"Configured {blueprint.LinkTargets?.Count ?? 0} link target(s).");

                if (blueprint.Menu?.RegisterInMainMenu == true)
                {
                    string menuTitle = blueprint.Menu.MenuTitle ?? searchName;
                    var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(
                        searchId, saasApplicationId, menuTitle, menuTitle);
                    if (!menuResult.IsSuccessful && menuResult.ValidationResult?.HasErrors == true)
                    {
                        executeResult.Messages.Add(menuResult.ValidationResult.Items?.FirstOrDefault()?.Message
                            ?? "Main menu registration reported errors.");
                    }
                    else
                    {
                        executeResult.Messages.Add($"Registered '{menuTitle}' in main menu.");
                    }
                }
            }

            return executeResult;
        }

        private static int ResolveSearchUsageType(string usageType)
        {
            if (string.Equals(usageType, "DataModelTemplate", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppSearchUsageType.DataModelTemplate;
            return (int)EmAppSearchUsageType.Management;
        }

        private static int ResolveLinkTargetActionType(string actionType)
        {
            if (string.Equals(actionType, "Create", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppLinkTargetActionType.Create;
            if (string.Equals(actionType, "Delete", StringComparison.OrdinalIgnoreCase))
                return (int)EmAppLinkTargetActionType.Delete;
            return (int)EmAppLinkTargetActionType.Edit;
        }

        private static int? ResolveLinkTargetTransactionGroupId(SqlConnection conn, PlmSearchImportBlueprintDto blueprint)
        {
            if (blueprint.TransactionGroup?.TransactionGroupId is int groupId && groupId > 0)
                return groupId;

            int? fromLink = blueprint.LinkTargets?
                .Select(l => l.LinkTargetTransactionGroupId)
                .FirstOrDefault(id => id.HasValue && id.Value > 0);
            if (fromLink.HasValue)
                return fromLink;

            if (!string.IsNullOrWhiteSpace(blueprint.TransactionGroup?.GroupName))
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT TOP 1 TransactionGroupID FROM dbo.AppTransactionGroup WHERE GroupName = @Name";
                    cmd.Parameters.AddWithValue("@Name", blueprint.TransactionGroup.GroupName);
                    var val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        return Convert.ToInt32(val);
                }
            }

            return null;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildSearchImportViewFields(
            SqlConnection conn,
            List<PlmSearchImportSearchViewFieldDto> fields)
        {
            var result = new ObservableSet<AppSearchViewFieldExDto>();
            if (fields == null)
                return result;

            foreach (var field in fields.OrderBy(f => f.Sort ?? int.MaxValue))
            {
                if (string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;

                var dto = new AppSearchViewFieldExDto
                {
                    IsModified = true,
                    IsVisible = field.IsVisible,
                    SysTableFiledPath = field.SysTableFiledPath,
                    DisplayText = string.IsNullOrWhiteSpace(field.DisplayText) ? field.SysTableFiledPath : field.DisplayText,
                    ControlType = field.ControlType ?? (int)EmAppControlType.TextBox,
                    Sort = field.Sort
                };
                if (field.IsTransRootId)
                    dto.IsTransRootId = true;

                int? entityId = ResolveEntityInfoId(conn, field.EntityIntegrationId);
                if (entityId.HasValue)
                    dto.EntityId = entityId;

                result.Add(dto);
            }

            return result;
        }

        private static void SaveSearchCriteriaFields(
            int searchId,
            List<PlmSearchImportCriteriaFieldDto> criteriaFields,
            SqlConnection conn)
        {
            if (criteriaFields == null || criteriaFields.Count == 0)
                return;

            AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            searchDto.AppSearchFieldList = new ObservableSet<AppSearchFieldExDto>();

            foreach (var field in criteriaFields.OrderBy(f => f.Sort ?? int.MaxValue))
            {
                if (string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;

                var dto = new AppSearchFieldExDto
                {
                    IsModified = true,
                    IsVisible = field.IsVisible,
                    IsReadOnly = false,
                    IsAllowMultipleSelect = false,
                    SysTableFiledPath = field.SysTableFiledPath,
                    DisplayText = string.IsNullOrWhiteSpace(field.DisplayText) ? field.SysTableFiledPath : field.DisplayText,
                    ControlType = field.ControlType ?? (int)EmAppControlType.TextBox,
                    OperationId = field.OperationId,
                    PositionRow = field.PositionRow,
                    PositionColumn = field.PositionColumn,
                    Sort = field.Sort,
                    DefaultValue = field.DefaultValue
                };

                int? entityId = ResolveEntityInfoId(conn, field.EntityIntegrationId);
                if (entityId.HasValue)
                    dto.EntityId = entityId;

                searchDto.AppSearchFieldList.Add(dto);
            }

            searchDto.IsModified = true;
            var saveResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to save search criteria fields.");
            }
        }

        private static int? ResolveEntityInfoId(SqlConnection conn, string entityIntegrationId)
        {
            if (string.IsNullOrWhiteSpace(entityIntegrationId))
                return null;

            if (!int.TryParse(entityIntegrationId.Trim(), out int plmEntityId))
                return null;

            return GetAppEntityInfoIdByPlmEntityId(conn, null, plmEntityId);
        }

        private static void ClearSearchCriteriaFields(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM dbo.AppSearchField WHERE SearchID = @SearchId";
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertSearchFormLinkTarget(
            SqlConnection tenantConn,
            int searchViewId,
            string navigationActionName,
            int actionType,
            int linkTargetTransactionId,
            int sourceViewColumnId,
            string targetColumn,
            int sort,
            int? linkTargetTransactionGroupId,
            int? linkTargetUsageType = null)
        {
            // Group id ⇒ Business Template / Form Group (UsageType 2); otherwise Link To Form (1).
            int usageType = linkTargetUsageType
                ?? (linkTargetTransactionGroupId.HasValue && linkTargetTransactionGroupId.Value > 0
                    ? (int)EmAppLinkTargetUsageType.SearchViewLinkToFormGroup
                    : (int)EmAppLinkTargetUsageType.SearchViewLinkToForm);

            using (var cmd = tenantConn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppFormLinkTarget (
    SearchViewID,
    NavigationActionName,
    ActionType,
    LinkTargetTransactionID,
    LinkTargetTransactionGroupID,
    LinkTargetUsageType,
    SourceColumnType,
    SourceViewColumnID1,
    TargetColumn1,
    Sort,
    IsPopup,
    PopupWidth,
    PopupHeight)
VALUES (
    @SearchViewId,
    @NavigationActionName,
    @ActionType,
    @LinkTargetTransactionId,
    @LinkTargetTransactionGroupId,
    @LinkTargetUsageType,
    @SourceColumnType,
    @SourceViewColumnId1,
    @TargetColumn1,
    @Sort,
    @IsPopup,
    @PopupWidth,
    @PopupHeight)";
                cmd.Parameters.AddWithValue("@SearchViewId", searchViewId);
                cmd.Parameters.AddWithValue("@NavigationActionName", navigationActionName);
                cmd.Parameters.AddWithValue("@ActionType", actionType);
                cmd.Parameters.AddWithValue("@LinkTargetTransactionId", linkTargetTransactionId);
                cmd.Parameters.AddWithValue("@LinkTargetTransactionGroupId", (object)linkTargetTransactionGroupId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LinkTargetUsageType", usageType);
                cmd.Parameters.AddWithValue("@SourceColumnType", (int)EmAppLinkTargetSourceColumnType.SearchViewField);
                cmd.Parameters.AddWithValue("@SourceViewColumnId1", sourceViewColumnId);
                cmd.Parameters.AddWithValue("@TargetColumn1", targetColumn);
                cmd.Parameters.AddWithValue("@Sort", sort);
                cmd.Parameters.AddWithValue("@IsPopup", true);
                cmd.Parameters.AddWithValue("@PopupWidth", 1200);
                cmd.Parameters.AddWithValue("@PopupHeight", 700);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
