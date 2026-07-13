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

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        public const string MassUpdateViewModeAttach = "MassUpdateViewAttach";
        public const string MassUpdateAppModeSingleTable = "SingleTableUpdate";
        public const string MassUpdateAppModeHierarchical = "HierarchicalTableUpdate";
        public const string MassUpdateListEditActionUseExisting = "UseExisting";
        public const string MassUpdateListEditActionCreateNew = "CreateNew";

        public static OperationCallResult<PlmSearchMassUpdateViewBlueprintDto> LoadSearchMassUpdateViewBlueprint(
            PlmSearchMassUpdateViewLoadRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchMassUpdateViewBlueprintDto>();
            try
            {
                RequirePlmMigrationAdmin();
                if (request == null || string.IsNullOrWhiteSpace(request.BlueprintJson))
                    throw new ArgumentException("BlueprintJson is required.");

                var blueprint = JsonConvert.DeserializeObject<PlmSearchMassUpdateViewBlueprintDto>(
                    request.BlueprintJson,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
                // Also accept PascalCase blueprints (re-uploaded API payloads).
                if (blueprint?.Target == null
                    || (string.IsNullOrWhiteSpace(blueprint.Target.AppSearchIntegrationId)
                        && !(blueprint.Target.AppSearchId > 0)))
                {
                    blueprint = JsonConvert.DeserializeObject<PlmSearchMassUpdateViewBlueprintDto>(request.BlueprintJson);
                }
                if (blueprint == null)
                    throw new InvalidOperationException("Mass Update view blueprint JSON could not be deserialized.");

                if (blueprint.SchemaVersion <= 0)
                    blueprint.SchemaVersion = 1;
                if (string.IsNullOrWhiteSpace(blueprint.Mode))
                    blueprint.Mode = MassUpdateViewModeAttach;

                result.Object = blueprint;
            }
            catch (Exception ex)
            {
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchMassUpdate_Load_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportValidationDto> ValidateSearchMassUpdateViewBlueprint(
            PlmSearchMassUpdateViewBlueprintDto blueprint)
        {
            var result = new OperationCallResult<PlmSearchImportValidationDto>
            {
                Object = new PlmSearchImportValidationDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                EnsurePlmImportSchema();
                string tenantConn = GetTenantConnectionString();
                ValidateSearchMassUpdateViewBlueprintInternal(blueprint, tenantConn, result.Object);
                result.Object.IsValid = result.Object.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Object.Errors.Add(ex.Message);
                result.Object.IsValid = false;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchMassUpdate_Validate_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchImportPreviewDto> PreviewSearchMassUpdateViewConfig(
            PlmSearchMassUpdateViewBlueprintDto blueprint)
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

                EnsurePlmImportSchema();
                string tenantConn = GetTenantConnectionString();
                result.Object.Items = BuildSearchMassUpdateViewPreviewItems(blueprint, tenantConn);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchMassUpdate_Preview_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<PlmSearchMassUpdateViewExecuteResultDto> ExecuteSearchMassUpdateViewConfig(
            PlmSearchMassUpdateViewExecuteRequestDto request)
        {
            var result = new OperationCallResult<PlmSearchMassUpdateViewExecuteResultDto>
            {
                Object = new PlmSearchMassUpdateViewExecuteResultDto()
            };
            try
            {
                RequirePlmMigrationAdmin();
                if (request?.Blueprint == null)
                    throw new ArgumentException("Blueprint is required.");

                EnsurePlmImportSchema();
                string tenantConn = GetTenantConnectionString();
                var validation = new PlmSearchImportValidationDto();
                ValidateSearchMassUpdateViewBlueprintInternal(request.Blueprint, tenantConn, validation);
                if (validation.Errors.Count > 0)
                    throw new InvalidOperationException(string.Join("; ", validation.Errors));

                result.Object = ExecuteSearchMassUpdateViewConfigCore(
                    request.Blueprint, tenantConn, request.SaasApplicationId);
            }
            catch (Exception ex)
            {
                result.Object.IsSuccess = false;
                result.Object.ErrorMessage = ex.Message;
                result.ValidationResult.Items.Add(new ValidationItem(
                    typeof(PlmMigrationBL), "Plm_SearchMassUpdate_Execute_Error", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static void ValidateSearchMassUpdateViewBlueprintInternal(
            PlmSearchMassUpdateViewBlueprintDto blueprint,
            string tenantConn,
            PlmSearchImportValidationDto validation)
        {
            if (blueprint == null)
            {
                validation.Errors.Add("Blueprint is required.");
                return;
            }

            if (!string.Equals(blueprint.Mode, MassUpdateViewModeAttach, StringComparison.OrdinalIgnoreCase))
            {
                validation.Errors.Add($"Mode must be '{MassUpdateViewModeAttach}'.");
            }

            if (blueprint.Target == null
                || (string.IsNullOrWhiteSpace(blueprint.Target.AppSearchIntegrationId)
                    && !(blueprint.Target.AppSearchId > 0)))
            {
                validation.Errors.Add("Target.AppSearchIntegrationId or Target.AppSearchId is required.");
            }

            if (blueprint.MassUpdate == null || string.IsNullOrWhiteSpace(blueprint.MassUpdate.AppMode))
                validation.Errors.Add("MassUpdate.AppMode is required (SingleTableUpdate | HierarchicalTableUpdate).");

            bool isHierarchical = string.Equals(
                blueprint.MassUpdate?.AppMode, MassUpdateAppModeHierarchical, StringComparison.OrdinalIgnoreCase);

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
                        $"AddLeftJoins '{join.AppTableName}' cardinality '{join.Cardinality}' is not allowed (1:1 only).");
                }
            }

            if (isHierarchical)
            {
                var le = blueprint.ListEditCreate;
                if (le == null || string.IsNullOrWhiteSpace(le.Action))
                {
                    validation.Errors.Add(
                        "HierarchicalTableUpdate requires ListEditCreate.Action (UseExisting | CreateNew).");
                }
                else if (string.Equals(le.Action, MassUpdateListEditActionUseExisting, StringComparison.OrdinalIgnoreCase))
                {
                    if (!(le.ExistingTransactionId > 0) && string.IsNullOrWhiteSpace(le.ExistingIntegrationId)
                        && string.IsNullOrWhiteSpace(blueprint.MassUpdate?.UpdateTransactionIntegrationId)
                        && !(blueprint.MassUpdate?.UpdateTransactionId > 0))
                    {
                        validation.Errors.Add(
                            "UseExisting requires ExistingTransactionId / ExistingIntegrationId (or MassUpdate.UpdateTransaction*).");
                    }
                }
                else if (string.Equals(le.Action, MassUpdateListEditActionCreateNew, StringComparison.OrdinalIgnoreCase))
                {
                    if (le.Create == null || string.IsNullOrWhiteSpace(le.Create.IntegrationId))
                        validation.Errors.Add("CreateNew requires listEditCreate.create.integrationId.");
                    if (string.IsNullOrWhiteSpace(le.Create?.UnitStructure?.Root?.AppTableName))
                        validation.Errors.Add("CreateNew requires listEditCreate.create.unitStructure.root.appTableName.");
                }
                else
                {
                    validation.Errors.Add(
                        $"ListEditCreate.Action '{le.Action}' is invalid (UseExisting | CreateNew).");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(blueprint.MassUpdate?.UpdateTransactionIntegrationId)
                    && !(blueprint.MassUpdate?.UpdateTransactionId > 0))
                {
                    validation.Errors.Add(
                        "SingleTableUpdate requires MassUpdate.UpdateTransactionIntegrationId or UpdateTransactionId.");
                }
            }

            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                int? searchId = ResolveMassUpdateTargetSearchId(conn, blueprint.Target, blueprint.Source?.PlmSearchName);
                if (!searchId.HasValue)
                {
                    validation.Errors.Add(
                        $"APP Search not found for IntegrationId '{blueprint.Target?.AppSearchIntegrationId}' / Id '{blueprint.Target?.AppSearchId}' / Name '{blueprint.Source?.PlmSearchName}'. " +
                        "Confirm you are logged into the TenantDB_PLM27 company where this Search exists, or run main Search Import first.");
                    return;
                }

                int? dataSetId = ResolveMassUpdateDataSetId(conn, searchId.Value, blueprint.Target);
                if (!dataSetId.HasValue)
                {
                    validation.Errors.Add(
                        $"Search #{searchId} has no DataSetId (and blueprint Target.AppDataSetId is missing). " +
                        "Open the Search in APP and confirm DataSet is assigned, or set target.appDataSetId in the blueprint.");
                }

                if (!string.IsNullOrWhiteSpace(blueprint.SearchView?.Name) && dataSetId.HasValue)
                {
                    int? existing = GetSearchViewIdByDataSetAndName(conn, dataSetId.Value, blueprint.SearchView.Name);
                    if (existing.HasValue)
                        validation.Warnings.Add(
                            $"Mass Update view '{blueprint.SearchView.Name}' already exists as SearchView #{existing} — execute will update it.");
                }

                if (isHierarchical
                    && string.Equals(blueprint.ListEditCreate?.Action, MassUpdateListEditActionUseExisting, StringComparison.OrdinalIgnoreCase))
                {
                    int? listId = ResolveMassUpdateTransactionId(conn, blueprint, forListEditExisting: true);
                    if (!listId.HasValue)
                    {
                        validation.Errors.Add("Existing ListEdit Transaction not found.");
                    }
                    else
                    {
                        int? organized = GetTransactionOrganizedType(conn, listId.Value);
                        if (organized != (int)EmTransactionOrganizedType.List)
                        {
                            validation.Warnings.Add(
                                $"Transaction #{listId} OrganizedType={organized} (expected List=3). Hierarchical mass update works best with ListEdit.");
                        }
                    }
                }

                if (!isHierarchical)
                {
                    int? txId = ResolveMassUpdateTransactionId(conn, blueprint, forListEditExisting: false);
                    if (!txId.HasValue)
                        validation.Errors.Add("Update Transaction not found for SingleTableUpdate.");
                }

                if (string.Equals(blueprint.ListEditCreate?.Action, MassUpdateListEditActionCreateNew, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(blueprint.ListEditCreate?.Create?.IntegrationId))
                {
                    int? existingList = GetTransactionIdByIntegrationId(
                        conn, null, blueprint.ListEditCreate.Create.IntegrationId);
                    if (existingList.HasValue)
                        validation.Warnings.Add(
                            $"ListEdit IntegrationId '{blueprint.ListEditCreate.Create.IntegrationId}' already exists as Transaction #{existingList} — execute will reuse it (skip create).");
                }
            }
        }

        private static List<PlmSearchImportPreviewItemDto> BuildSearchMassUpdateViewPreviewItems(
            PlmSearchMassUpdateViewBlueprintDto blueprint,
            string tenantConn)
        {
            var items = new List<PlmSearchImportPreviewItemDto>();
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();
                int? searchId = ResolveMassUpdateTargetSearchId(conn, blueprint.Target, blueprint.Source?.PlmSearchName);
                int? dataSetId = searchId.HasValue
                    ? ResolveMassUpdateDataSetId(conn, searchId.Value, blueprint.Target)
                    : null;
                int? defaultViewId = searchId.HasValue ? GetSearchViewIdForSearch(conn, searchId.Value) : null;
                bool isHierarchical = string.Equals(
                    blueprint.MassUpdate?.AppMode, MassUpdateAppModeHierarchical, StringComparison.OrdinalIgnoreCase);

                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "Search",
                    Name = blueprint.Source?.PlmSearchName ?? blueprint.Target?.AppSearchIntegrationId,
                    IntegrationId = blueprint.Target?.AppSearchIntegrationId,
                    Action = SearchImportActionUpdate,
                    ExistingId = searchId,
                    Detail = "Existing Search — display default View unchanged"
                });

                if (blueprint.DataSetPatch != null)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "DataSet",
                        Name = "Enrich DataSet",
                        Action = SearchImportActionUpdate,
                        ExistingId = dataSetId,
                        Detail = DescribeDataSetPatch(blueprint.DataSetPatch)
                    });
                }

                if (isHierarchical && blueprint.ListEditCreate != null)
                {
                    if (string.Equals(blueprint.ListEditCreate.Action, MassUpdateListEditActionCreateNew, StringComparison.OrdinalIgnoreCase))
                    {
                        string integrationId = blueprint.ListEditCreate.Create?.IntegrationId;
                        int? existing = !string.IsNullOrWhiteSpace(integrationId)
                            ? GetTransactionIdByIntegrationId(conn, null, integrationId)
                            : null;
                        string root = blueprint.ListEditCreate.Create?.UnitStructure?.Root?.AppTableName ?? "?";
                        int childCount = blueprint.ListEditCreate.Create?.UnitStructure?.Children?.Count ?? 0;
                        items.Add(new PlmSearchImportPreviewItemDto
                        {
                            ObjectType = "ListEditTransaction",
                            Name = blueprint.ListEditCreate.Create?.Name ?? integrationId,
                            IntegrationId = integrationId,
                            Action = existing.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                            ExistingId = existing,
                            Detail = existing.HasValue
                                ? "Reuse existing ListEdit"
                                : $"Create ListEdit (List) root={root}, children={childCount}"
                        });
                    }
                    else
                    {
                        int? listId = ResolveMassUpdateTransactionId(conn, blueprint, forListEditExisting: true);
                        items.Add(new PlmSearchImportPreviewItemDto
                        {
                            ObjectType = "ListEditTransaction",
                            Name = "Use existing ListEdit",
                            IntegrationId = blueprint.ListEditCreate.ExistingIntegrationId
                                ?? blueprint.MassUpdate?.UpdateTransactionIntegrationId,
                            Action = SearchImportActionUpdate,
                            ExistingId = listId,
                            Detail = "Hierarchical mass update target"
                        });
                    }
                }
                else if (!isHierarchical)
                {
                    int? txId = ResolveMassUpdateTransactionId(conn, blueprint, forListEditExisting: false);
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "Transaction",
                        Name = "Single-table update model",
                        IntegrationId = blueprint.MassUpdate?.UpdateTransactionIntegrationId,
                        Action = SearchImportActionUpdate,
                        ExistingId = txId,
                        Detail = $"Unit table={blueprint.MassUpdate?.UpdateUnitTableName ?? "—"}"
                    });
                }

                int? existingMuView = dataSetId.HasValue && !string.IsNullOrWhiteSpace(blueprint.SearchView?.Name)
                    ? GetSearchViewIdByDataSetAndName(conn, dataSetId.Value, blueprint.SearchView.Name)
                    : null;
                items.Add(new PlmSearchImportPreviewItemDto
                {
                    ObjectType = "MassUpdateSearchView",
                    Name = blueprint.SearchView?.Name,
                    IntegrationId = blueprint.SearchView?.Name,
                    Action = existingMuView.HasValue ? SearchImportActionUpdate : SearchImportActionInsert,
                    ExistingId = existingMuView,
                    Detail =
                        $"IsMassUpdateView=true, AppMode={blueprint.MassUpdate?.AppMode}, fields={blueprint.SearchView?.Fields?.Count ?? 0}; default display View #{defaultViewId} kept"
                });

                bool copyLinks = blueprint.LinkTargets?.CopyFromDefaultSearchView != false;
                if (copyLinks)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "LinkTarget",
                        Name = "Copy from default SearchView",
                        Action = SearchImportActionInsert,
                        ExistingId = defaultViewId,
                        Detail = defaultViewId.HasValue
                            ? $"Copy AppFormLinkTarget rows from default View #{defaultViewId}"
                            : "Default View not found — no links will be copied"
                    });
                }
                else if (blueprint.LinkTargets?.Items != null && blueprint.LinkTargets.Items.Count > 0)
                {
                    items.Add(new PlmSearchImportPreviewItemDto
                    {
                        ObjectType = "LinkTarget",
                        Name = "Explicit link targets",
                        Action = SearchImportActionInsert,
                        Detail = $"{blueprint.LinkTargets.Items.Count} explicit link(s)"
                    });
                }
            }

            return items;
        }

        private static PlmSearchMassUpdateViewExecuteResultDto ExecuteSearchMassUpdateViewConfigCore(
            PlmSearchMassUpdateViewBlueprintDto blueprint,
            string tenantConn,
            int? saasApplicationId)
        {
            var executeResult = new PlmSearchMassUpdateViewExecuteResultDto { IsSuccess = true };
            using (var conn = new SqlConnection(tenantConn))
            {
                conn.Open();

                int searchId = ResolveMassUpdateTargetSearchId(conn, blueprint.Target, blueprint.Source?.PlmSearchName)
                    ?? throw new InvalidOperationException("Target APP Search not found.");
                executeResult.SearchId = searchId;

                int dataSetId = ResolveMassUpdateDataSetId(conn, searchId, blueprint.Target, persistFallback: true)
                    ?? throw new InvalidOperationException(
                        $"Search #{searchId} has no DataSetId (and blueprint Target.AppDataSetId is missing).");
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

                bool isHierarchical = string.Equals(
                    blueprint.MassUpdate?.AppMode, MassUpdateAppModeHierarchical, StringComparison.OrdinalIgnoreCase);

                int updateTransactionId;
                // Prefer table-name resolution when provided — unit ids are tenant-specific.
                int? updateUnitId = null;
                if (!string.IsNullOrWhiteSpace(blueprint.MassUpdate?.UpdateUnitTableName)
                    && !isHierarchical)
                {
                    // Resolved after we know updateTransactionId below.
                }
                else if (blueprint.MassUpdate?.UpdateBaseTransactionUnitId > 0)
                {
                    updateUnitId = blueprint.MassUpdate.UpdateBaseTransactionUnitId;
                }

                if (isHierarchical)
                {
                    updateTransactionId = EnsureMassUpdateListEditTransaction(
                        conn, blueprint, saasApplicationId, executeResult);
                    executeResult.ListEditTransactionId = updateTransactionId;
                    if (string.IsNullOrWhiteSpace(executeResult.ListEditIntegrationId))
                    {
                        executeResult.ListEditIntegrationId =
                            blueprint.ListEditCreate?.Create?.IntegrationId
                            ?? blueprint.ListEditCreate?.ExistingIntegrationId
                            ?? blueprint.MassUpdate?.UpdateTransactionIntegrationId;
                    }

                    if (!updateUnitId.HasValue)
                        updateUnitId = GetRootTransactionUnitId(conn, updateTransactionId);
                }
                else
                {
                    updateTransactionId = ResolveMassUpdateTransactionId(conn, blueprint, forListEditExisting: false)
                        ?? throw new InvalidOperationException("Update Transaction not found.");
                    if (!string.IsNullOrWhiteSpace(blueprint.MassUpdate?.UpdateUnitTableName))
                    {
                        updateUnitId = GetTransactionUnitIdByTableName(
                            conn, updateTransactionId, blueprint.MassUpdate.UpdateUnitTableName);
                    }
                    if (!updateUnitId.HasValue && blueprint.MassUpdate?.UpdateBaseTransactionUnitId > 0)
                        updateUnitId = blueprint.MassUpdate.UpdateBaseTransactionUnitId;
                }

                var viewFields = BuildMassUpdateSearchViewFields(
                    conn, blueprint.SearchView?.Fields, updateTransactionId, updateUnitId,
                    blueprint.MassUpdate?.PkDatabaseFieldName);

                int gridOutputMode = blueprint.SearchView?.GridOutputMode > 0 ? blueprint.SearchView.GridOutputMode : 1;
                int viewType = ResolveSearchViewType(blueprint.SearchView?.ViewType);

                int muViewId = SaveMassUpdateSearchView(
                    conn,
                    dataSetId,
                    blueprint.SearchView.Name,
                    viewFields,
                    gridOutputMode,
                    viewType,
                    saasApplicationId,
                    updateTransactionId,
                    updateUnitId,
                    blueprint.MassUpdate);
                executeResult.MassUpdateSearchViewId = muViewId;
                executeResult.Messages.Add(
                    $"Mass Update SearchView #{muViewId} ('{blueprint.SearchView.Name}') saved with {viewFields.Count} field(s).");

                if (defaultViewId.HasValue)
                    EnsureSearchDefaultView(conn, searchId, defaultViewId.Value, dataSetId);

                // Row Open/Create actions: same as Sibling — copy from default display View by default.
                ClearSearchViewFormLinkTargets(conn, muViewId);
                bool copyLinks = blueprint.LinkTargets?.CopyFromDefaultSearchView != false;
                if (copyLinks && defaultViewId.HasValue)
                {
                    int copied = CopyFormLinkTargetsBetweenSearchViews(conn, defaultViewId.Value, muViewId);
                    executeResult.Messages.Add($"Copied {copied} link target(s) from default View #{defaultViewId}.");
                }
                else if (blueprint.LinkTargets?.Items != null && blueprint.LinkTargets.Items.Count > 0)
                {
                    string rootColumn = blueprint.SearchView.Fields?
                        .FirstOrDefault(f => f.IsTransRootId)?.SysTableFiledPath
                        ?? blueprint.MassUpdate?.PkDatabaseFieldName
                        ?? "ReferenceId";
                    int? rootFieldId = GetSearchViewFieldId(conn, null, muViewId, rootColumn);
                    if (!rootFieldId.HasValue)
                        throw new InvalidOperationException($"Mass Update view is missing root column '{rootColumn}'.");

                    foreach (var link in blueprint.LinkTargets.Items)
                    {
                        int? transactionId = GetTransactionIdByIntegrationId(conn, null, link.TransactionIntegrationId);
                        if (!transactionId.HasValue)
                            throw new InvalidOperationException($"Transaction '{link.TransactionIntegrationId}' not found.");

                        InsertSearchFormLinkTarget(
                            conn,
                            muViewId,
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
                else
                {
                    executeResult.Messages.Add(
                        "No link targets configured (default View missing or copy disabled with empty Items).");
                }

                if (blueprint.MassUpdate?.SetAsDefaultMassUpdateView == true)
                {
                    executeResult.Messages.Add(
                        "SetAsDefaultMassUpdateView requested — platform default Mass Update wiring is best-effort / may require manual Search editor assign.");
                }
            }

            return executeResult;
        }

        private static int EnsureMassUpdateListEditTransaction(
            SqlConnection conn,
            PlmSearchMassUpdateViewBlueprintDto blueprint,
            int? saasApplicationId,
            PlmSearchMassUpdateViewExecuteResultDto executeResult)
        {
            var le = blueprint.ListEditCreate;
            if (le != null
                && string.Equals(le.Action, MassUpdateListEditActionCreateNew, StringComparison.OrdinalIgnoreCase)
                && le.Create != null)
            {
                string integrationId = le.Create.IntegrationId;
                int? existing = GetTransactionIdByIntegrationId(conn, null, integrationId);
                if (existing.HasValue)
                {
                    executeResult.Messages.Add($"ListEdit '{integrationId}' already exists as #{existing} — reused.");
                    // PLM DW tables often lack formal FKs — always (re)apply blueprint parent-key links.
                    EnsureListEditChildParentKeyLinks(conn, existing.Value, le.Create.UnitStructure, executeResult);
                    ApplyListEditUnitFieldMetadataFromBlueprint(conn, existing.Value, le.Create.UnitStructure, executeResult);
                    executeResult.ListEditIntegrationId = integrationId;
                    return existing.Value;
                }

                string rootTable = le.Create.UnitStructure?.Root?.AppTableName
                    ?? throw new InvalidOperationException("ListEdit create root table is required.");
                var childTables = (le.Create.UnitStructure?.Children ?? new List<PlmSearchMassUpdateListEditUnitDto>())
                    .Where(c => !string.IsNullOrWhiteSpace(c?.AppTableName))
                    .Select(c => new HierarchyChildTableDto { TableName = c.AppTableName.Trim() })
                    .ToList();

                int dataSourceId = le.Create.TenantDataSourceRegisterId ?? GetTenantDataSourceId();
                int? saasId = le.Create.SaasApplicationId ?? saasApplicationId;

                var setup = new HierarchyTableSetupDto
                {
                    MasterTableName = rootTable.Trim(),
                    ChildTables = childTables,
                    DataSourceRegisterId = dataSourceId,
                    SchemaOwner = "dbo",
                    TransactionName = string.IsNullOrWhiteSpace(le.Create.Name)
                        ? integrationId
                        : le.Create.Name,
                    SaasApplicationId = saasId
                };

                var createResult = AppTransactionBL.CreateHierarchyTransactionFromTables(
                    setup, isIgnoreValidation: true, skipPostSaveCacheSync: true);
                if (!createResult.IsSuccessfulWithResult || createResult.Object?.Id == null)
                {
                    throw new InvalidOperationException(
                        createResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                        ?? "Failed to create ListEdit hierarchy transaction.");
                }

                int txId = Convert.ToInt32(createResult.Object.Id);
                SetTransactionOrganizedType(conn, txId, (int)EmTransactionOrganizedType.List);
                SetIntegrationId(conn, null, "AppTransaction", "TransactionID", txId, integrationId);

                // CreateHierarchyTransactionFromTables only marks LinkToParent when DB schema has an FK
                // to the master table. PLM imported tables usually share ReferenceId without that FK —
                // apply blueprint FkColumn / ParentPkColumn / IsForeignKey explicitly.
                EnsureListEditChildParentKeyLinks(conn, txId, le.Create.UnitStructure, executeResult);
                // Same for ControlType/EntityId — hierarchy create defaults TextBox with no EntityId.
                ApplyListEditUnitFieldMetadataFromBlueprint(conn, txId, le.Create.UnitStructure, executeResult);

                executeResult.Messages.Add(
                    $"Created ListEdit Transaction #{txId} IntegrationId={integrationId} (OrganizedType=List), root={rootTable}, children={childTables.Count}.");
                executeResult.ListEditIntegrationId = integrationId;
                return txId;
            }

            int? resolved = ResolveMassUpdateTransactionId(conn, blueprint, forListEditExisting: true);
            if (!resolved.HasValue)
                throw new InvalidOperationException("ListEdit Transaction not found for HierarchicalTableUpdate.");
            executeResult.Messages.Add($"Using existing ListEdit Transaction #{resolved}.");
            return resolved.Value;
        }

        private static ObservableSet<AppSearchViewFieldExDto> BuildMassUpdateSearchViewFields(
            SqlConnection conn,
            List<PlmSearchImportSearchViewFieldDto> fields,
            int updateTransactionId,
            int? updateUnitId,
            string pkDatabaseFieldName)
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

                bool wantsMap = field.IsUpdatable != false
                    && (field.MassUpdateTransactionFieldId.HasValue
                        || !string.IsNullOrWhiteSpace(field.MassUpdateDatabaseFieldName)
                        || field.IsTransRootId
                        || (!string.IsNullOrWhiteSpace(pkDatabaseFieldName)
                            && string.Equals(field.SysTableFiledPath, pkDatabaseFieldName, StringComparison.OrdinalIgnoreCase)));

                if (wantsMap && field.IsUpdatable != false)
                {
                    string dbName = !string.IsNullOrWhiteSpace(field.MassUpdateDatabaseFieldName)
                        ? field.MassUpdateDatabaseFieldName
                        : (field.IsTransRootId || string.Equals(field.SysTableFiledPath, pkDatabaseFieldName, StringComparison.OrdinalIgnoreCase)
                            ? (pkDatabaseFieldName ?? field.SysTableFiledPath)
                            : field.SysTableFiledPath);

                    // Prefer resolve-by-name (tenant-safe). Fall back to blueprint field id only if name resolve fails.
                    int? txnFieldId = ResolveTransactionFieldId(conn, updateTransactionId, updateUnitId, dbName);
                    if (txnFieldId.HasValue)
                        dto.MassUpdateTransactionFieldId = txnFieldId;
                    else if (field.MassUpdateTransactionFieldId.HasValue)
                        dto.MassUpdateTransactionFieldId = field.MassUpdateTransactionFieldId;
                }

                result.Add(dto);
            }

            return result;
        }

        private static int SaveMassUpdateSearchView(
            SqlConnection conn,
            int dataSetId,
            string name,
            ObservableSet<AppSearchViewFieldExDto> viewFields,
            int gridOutputMode,
            int viewType,
            int? saasApplicationId,
            int updateTransactionId,
            int? updateUnitId,
            PlmSearchMassUpdateSettingsDto massUpdate)
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

            searchViewDto.IsMassUpdateView = true;
            searchViewDto.UpdateTransctionId = updateTransactionId;
            if (updateUnitId.HasValue)
                searchViewDto.UpdateBaseTranscationUnitId = updateUnitId;
            searchViewDto.IsAllowAddRow = massUpdate?.IsAllowAddRow ?? true;
            searchViewDto.IsAllowDeleteRow = massUpdate?.IsAllowDeleteRow ?? true;
            searchViewDto.IsAllowUpdateRow = massUpdate?.IsAllowAdvancedUpdate ?? true;

            var saveViewResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
            if (!saveViewResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(saveViewResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                    ?? "Failed to save mass update search view.");
            }

            return Convert.ToInt32(saveViewResult.Object.Id);
        }

        private static int? ResolveMassUpdateTargetSearchId(
            SqlConnection conn,
            PlmSearchMassUpdateViewTargetDto target,
            string plmSearchName = null)
        {
            if (target == null && string.IsNullOrWhiteSpace(plmSearchName))
                return null;

            // 1) IntegrationId (when column+value exist)
            if (!string.IsNullOrWhiteSpace(target?.AppSearchIntegrationId))
            {
                int? byIntegration = GetSearchIdByIntegrationId(conn, null, target.AppSearchIntegrationId.Trim());
                if (byIntegration.HasValue)
                    return byIntegration;
            }

            // 2) Numeric AppSearchId when the row exists (IntegrationId may be unset on older imports)
            if (target?.AppSearchId > 0 && SearchRowExists(conn, target.AppSearchId.Value))
                return target.AppSearchId.Value;

            // 3) Match by PLM / APP Search display name
            if (!string.IsNullOrWhiteSpace(plmSearchName))
            {
                int? byName = GetSearchIdByExactName(conn, plmSearchName.Trim());
                if (byName.HasValue)
                    return byName;
            }

            return null;
        }

        private static int? GetSearchIdByExactName(SqlConnection conn, string name)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 SearchID FROM dbo.AppSearch WHERE Name = @Name ORDER BY SearchID";
                cmd.Parameters.AddWithValue("@Name", name);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? ResolveMassUpdateDataSetId(
            SqlConnection conn,
            int searchId,
            PlmSearchMassUpdateViewTargetDto target,
            bool persistFallback = false)
        {
            int? fromSearch = GetDataSetIdForSearch(conn, searchId);
            if (fromSearch.HasValue && fromSearch.Value > 0)
                return fromSearch;

            if (target?.AppDataSetId > 0)
            {
                if (persistFallback)
                    EnsureSearchDataSetId(conn, searchId, target.AppDataSetId.Value);
                return target.AppDataSetId;
            }

            return null;
        }

        private static void EnsureSearchDataSetId(SqlConnection conn, int searchId, int dataSetId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppSearch
SET DataSetID = @DataSetId
WHERE SearchID = @SearchId
  AND (DataSetID IS NULL OR DataSetID = 0)";
                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                cmd.ExecuteNonQuery();
            }
        }

        private static bool SearchRowExists(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM dbo.AppSearch WHERE SearchID = @SearchId";
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static int? ResolveMassUpdateTransactionId(
            SqlConnection conn,
            PlmSearchMassUpdateViewBlueprintDto blueprint,
            bool forListEditExisting)
        {
            if (forListEditExisting && blueprint.ListEditCreate != null)
            {
                if (!string.IsNullOrWhiteSpace(blueprint.ListEditCreate.ExistingIntegrationId))
                {
                    int? byIntegration = GetTransactionIdByIntegrationId(
                        conn, null, blueprint.ListEditCreate.ExistingIntegrationId);
                    if (byIntegration.HasValue)
                        return byIntegration;
                }

                if (blueprint.ListEditCreate.ExistingTransactionId > 0)
                    return blueprint.ListEditCreate.ExistingTransactionId;
            }

            // Prefer IntegrationId over numeric TransactionId (tenant-safe).
            if (!string.IsNullOrWhiteSpace(blueprint.MassUpdate?.UpdateTransactionIntegrationId))
            {
                int? byIntegration = GetTransactionIdByIntegrationId(
                    conn, null, blueprint.MassUpdate.UpdateTransactionIntegrationId);
                if (byIntegration.HasValue)
                    return byIntegration;
            }

            if (blueprint.MassUpdate?.UpdateTransactionId > 0)
                return blueprint.MassUpdate.UpdateTransactionId;

            return null;
        }

        private static int? GetTransactionOrganizedType(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TransactionOrganizedType FROM dbo.AppTransaction WHERE TransactionID = @Id";
                cmd.Parameters.AddWithValue("@Id", transactionId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void SetTransactionOrganizedType(SqlConnection conn, int transactionId, int organizedType)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "UPDATE dbo.AppTransaction SET TransactionOrganizedType = @Type WHERE TransactionID = @Id";
                cmd.Parameters.AddWithValue("@Type", organizedType);
                cmd.Parameters.AddWithValue("@Id", transactionId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Sets child unit FK → root PK as IsLinkToParentPrimaryKey using blueprint FkColumn / ParentPkColumn
        /// (or field IsForeignKey / default ReferenceId). Needed because CreateHierarchyTransactionFromTables
        /// only auto-detects formal database foreign keys.
        /// </summary>
        private static void EnsureListEditChildParentKeyLinks(
            SqlConnection conn,
            int transactionId,
            PlmSearchMassUpdateListEditUnitStructureDto unitStructure,
            PlmSearchMassUpdateViewExecuteResultDto executeResult)
        {
            if (unitStructure?.Root == null
                || unitStructure.Children == null
                || unitStructure.Children.Count == 0)
                return;

            string rootTable = unitStructure.Root.AppTableName;
            string rootPkColumn = !string.IsNullOrWhiteSpace(unitStructure.Root.PkColumn)
                ? unitStructure.Root.PkColumn.Trim()
                : "ReferenceId";

            int? rootUnitId = !string.IsNullOrWhiteSpace(rootTable)
                ? GetTransactionUnitIdByTableName(conn, transactionId, rootTable.Trim())
                : GetRootTransactionUnitId(conn, transactionId);
            if (!rootUnitId.HasValue)
            {
                executeResult.Messages.Add(
                    "ListEdit child parent-key link skipped: root unit not found.");
                return;
            }

            int? rootPkFieldId = ResolveTransactionFieldId(conn, transactionId, rootUnitId, rootPkColumn);
            if (!rootPkFieldId.HasValue)
            {
                executeResult.Messages.Add(
                    $"ListEdit child parent-key link skipped: root PK '{rootPkColumn}' not found on unit #{rootUnitId}.");
                return;
            }

            foreach (var child in unitStructure.Children.Where(c => !string.IsNullOrWhiteSpace(c?.AppTableName)))
            {
                int? childUnitId = GetTransactionUnitIdByTableName(conn, transactionId, child.AppTableName.Trim());
                if (!childUnitId.HasValue)
                {
                    executeResult.Messages.Add(
                        $"ListEdit child parent-key link skipped: unit for '{child.AppTableName}' not found.");
                    continue;
                }

                EnsureChildUnitParent(conn, childUnitId.Value, rootUnitId.Value);

                string fkColumn = !string.IsNullOrWhiteSpace(child.FkColumn)
                    ? child.FkColumn.Trim()
                    : child.Fields?.FirstOrDefault(f => f.IsForeignKey == true && !string.IsNullOrWhiteSpace(f.AppColumnName))
                        ?.AppColumnName?.Trim();
                if (string.IsNullOrWhiteSpace(fkColumn))
                    fkColumn = !string.IsNullOrWhiteSpace(child.ParentPkColumn)
                        ? child.ParentPkColumn.Trim()
                        : rootPkColumn;

                string parentPkColumn = !string.IsNullOrWhiteSpace(child.ParentPkColumn)
                    ? child.ParentPkColumn.Trim()
                    : rootPkColumn;
                int? parentPkFieldId = string.Equals(parentPkColumn, rootPkColumn, StringComparison.OrdinalIgnoreCase)
                    ? rootPkFieldId
                    : (ResolveTransactionFieldId(conn, transactionId, rootUnitId, parentPkColumn) ?? rootPkFieldId);

                int updated = SetChildFieldLinkToParentPrimaryKey(
                    conn, childUnitId.Value, fkColumn, parentPkFieldId.Value);
                if (updated > 0)
                {
                    executeResult.Messages.Add(
                        $"ListEdit child '{child.AppTableName}': set {fkColumn} IsLinkToParentPrimaryKey → root #{parentPkFieldId}.");
                }
                else
                {
                    executeResult.Messages.Add(
                        $"ListEdit child '{child.AppTableName}': FK column '{fkColumn}' not found — LinkToParentPrimaryKey not set.");
                }
            }
        }

        private static void EnsureChildUnitParent(SqlConnection conn, int childUnitId, int rootUnitId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET ParentTransactionUnitID = @RootUnitId,
    IsMasterSiblingUnit = 0
WHERE TransactionUnitID = @ChildUnitId
  AND (ParentTransactionUnitID IS NULL OR ParentTransactionUnitID = 0 OR ParentTransactionUnitID <> @RootUnitId)";
                cmd.Parameters.AddWithValue("@RootUnitId", rootUnitId);
                cmd.Parameters.AddWithValue("@ChildUnitId", childUnitId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int SetChildFieldLinkToParentPrimaryKey(
            SqlConnection conn,
            int childUnitId,
            string fkColumn,
            int parentPkFieldId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @ParentPkFieldId,
    IsReadonly = 1,
    IsVisible = 0
WHERE TransactionUnitID = @ChildUnitId
  AND DataBaseFieldName = @FkColumn";
                cmd.Parameters.AddWithValue("@ParentPkFieldId", parentPkFieldId);
                cmd.Parameters.AddWithValue("@ChildUnitId", childUnitId);
                cmd.Parameters.AddWithValue("@FkColumn", fkColumn);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Applies blueprint unit fields' ControlType / EntityId / DisplayName / visibility / sort
        /// onto AppTransactionField rows. CreateHierarchyTransactionFromTables defaults TextBox + null EntityId.
        /// Columns not listed in the blueprint (except PK / LinkToParent) are hidden.
        /// SortOrder and IsVisible must follow PLM MassUpdateViewField.Sort / IsHide (via blueprint sort / isVisible).
        /// </summary>
        private static void ApplyListEditUnitFieldMetadataFromBlueprint(
            SqlConnection conn,
            int transactionId,
            PlmSearchMassUpdateListEditUnitStructureDto unitStructure,
            PlmSearchMassUpdateViewExecuteResultDto executeResult)
        {
            if (unitStructure == null)
                return;

            var units = new List<PlmSearchMassUpdateListEditUnitDto>();
            if (unitStructure.Root != null)
                units.Add(unitStructure.Root);
            if (unitStructure.Children != null)
                units.AddRange(unitStructure.Children.Where(c => c != null));

            int applied = 0;
            int hidden = 0;

            foreach (var unit in units)
            {
                if (string.IsNullOrWhiteSpace(unit.AppTableName))
                    continue;

                string appTableName = unit.AppTableName.Trim();
                int? unitId = GetTransactionUnitIdByTableName(conn, transactionId, appTableName);
                if (!unitId.HasValue)
                    continue;

                // FieldMapping SubItem / GridColumn → ControlType + Entity for every column on this unit
                // (root TabFields and child GridColumns). MU blueprint then overlays Sort / IsVisible / explicit meta.
                var mappingByColumn = LoadFieldMappingMetaByAppTable(conn, appTableName);

                var blueprintByColumn = new Dictionary<string, PlmSearchMassUpdateListEditFieldDto>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (var f in unit.Fields ?? Enumerable.Empty<PlmSearchMassUpdateListEditFieldDto>())
                {
                    if (string.IsNullOrWhiteSpace(f?.AppColumnName))
                        continue;
                    blueprintByColumn[f.AppColumnName.Trim()] = f;
                }

                bool isRootUnit = unitStructure.Root != null
                    && string.Equals(unit.AppTableName?.Trim(), unitStructure.Root.AppTableName?.Trim(), StringComparison.OrdinalIgnoreCase);

                // Structural PK/FK — always known. Root PK defaults visible (row identity);
                // child PK/FK stay hidden unless blueprint explicitly shows them.
                if (!string.IsNullOrWhiteSpace(unit.PkColumn) && !blueprintByColumn.ContainsKey(unit.PkColumn))
                {
                    blueprintByColumn[unit.PkColumn.Trim()] = new PlmSearchMassUpdateListEditFieldDto
                    {
                        AppColumnName = unit.PkColumn.Trim(),
                        IsPrimaryKey = true,
                        IsVisible = isRootUnit,
                        Sort = 0,
                        IsReadOnly = true,
                        ControlType = (int)EmAppControlType.Numeric
                    };
                }
                if (!string.IsNullOrWhiteSpace(unit.FkColumn) && !blueprintByColumn.ContainsKey(unit.FkColumn))
                {
                    blueprintByColumn[unit.FkColumn.Trim()] = new PlmSearchMassUpdateListEditFieldDto
                    {
                        AppColumnName = unit.FkColumn.Trim(),
                        IsForeignKey = true,
                        IsVisible = false,
                        Sort = 0,
                        ControlType = (int)EmAppControlType.Numeric
                    };
                }

                var dbFields = new List<(int FieldId, string DbName, bool IsPk, bool IsLinkParent)>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TransactionFieldID, DataBaseFieldName,
       ISNULL(IsPrimaryKey, 0), ISNULL(IsLinkToParentPrimaryKey, 0)
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId";
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dbFields.Add((
                                reader.GetInt32(0),
                                reader.IsDBNull(1) ? null : reader.GetString(1),
                                reader.GetBoolean(2),
                                reader.GetBoolean(3)));
                        }
                    }
                }

                foreach (var (fieldId, dbName, isPk, isLinkParent) in dbFields)
                {
                    if (string.IsNullOrWhiteSpace(dbName))
                        continue;

                    bool inMuBlueprint = blueprintByColumn.TryGetValue(dbName, out PlmSearchMassUpdateListEditFieldDto bpField);
                    mappingByColumn.TryGetValue(dbName, out FieldMappingColumnMeta mapMeta);
                    var identityKind = isRootUnit
                        ? ClassifyRootIdentityColumn(dbName, unit.PkColumn)
                        : RootIdentityColumnKind.None;

                    // --- ControlType / EntityId: blueprint wins, else FieldMapping SubItem/GridColumn ---
                    int controlType;
                    if (inMuBlueprint && bpField.ControlType.HasValue)
                        controlType = MapPlmControlTypeToApp(bpField.ControlType.Value);
                    else if (mapMeta != null && mapMeta.PlmControlType.HasValue)
                        controlType = MapPlmControlTypeToApp(mapMeta.PlmControlType.Value);
                    else if (isPk || identityKind == RootIdentityColumnKind.ReferenceId || (inMuBlueprint && bpField.IsPrimaryKey == true))
                        controlType = (int)EmAppControlType.Numeric;
                    else
                        controlType = (int)EmAppControlType.TextBox;

                    int? appEntityId = null;
                    if (inMuBlueprint && !string.IsNullOrWhiteSpace(bpField.EntityIntegrationId))
                        appEntityId = ResolveEntityInfoId(conn, bpField.EntityIntegrationId);
                    else if (inMuBlueprint && (bpField.PlmMetaColumnId.HasValue || bpField.PlmSubItemId.HasValue))
                        appEntityId = ResolveEntityFromFieldMapping(conn, bpField);
                    else if (mapMeta?.PlmEntityId.HasValue == true)
                        appEntityId = GetAppEntityInfoIdByPlmEntityId(conn, null, mapMeta.PlmEntityId.Value);

                    if (controlType != (int)EmAppControlType.DDL
                        && controlType != (int)EmAppControlType.SearchAbleDDL
                        && controlType != (int)EmAppControlType.AutoComplete)
                    {
                        appEntityId = null;
                    }

                    // --- Hide/Show + Sort ---
                    // MU blueprint fields: IsVisible from PLM IsHide / blueprint.
                    // Root identity (ReferenceId + Product Code/Article + Description): always show when present on unit.
                    // Other non-MU columns: hide (CreateHierarchy leftovers), keep FieldMapping ControlType/Entity.
                    bool isVisible;
                    bool isReadonly;
                    int sortOrder;
                    string displayName = null;

                    if (identityKind != RootIdentityColumnKind.None)
                    {
                        isVisible = true;
                        isReadonly = identityKind == RootIdentityColumnKind.ReferenceId
                            || isPk
                            || isLinkParent
                            || (inMuBlueprint && (bpField.IsReadOnly == true || bpField.IsPrimaryKey == true));
                        displayName = inMuBlueprint && !string.IsNullOrWhiteSpace(bpField.DisplayLabel)
                            ? bpField.DisplayLabel.Trim()
                            : DefaultRootIdentityDisplayName(identityKind, dbName);
                        sortOrder = RootIdentitySortOrder(identityKind);
                    }
                    else if (inMuBlueprint)
                    {
                        isVisible = bpField.IsVisible == true;
                        isReadonly = isPk || isLinkParent
                            || bpField.IsReadOnly == true
                            || bpField.IsForeignKey == true
                            || bpField.IsPrimaryKey == true;
                        displayName = !string.IsNullOrWhiteSpace(bpField.DisplayLabel)
                            ? bpField.DisplayLabel.Trim()
                            : null;
                        sortOrder = bpField.Sort.HasValue
                            ? bpField.Sort.Value * 10
                            : (isPk || isLinkParent || bpField.IsPrimaryKey == true || bpField.IsForeignKey == true
                                ? 0
                                : 9990);
                    }
                    else if (isPk || isLinkParent)
                    {
                        isVisible = false;
                        isReadonly = true;
                        sortOrder = 0;
                    }
                    else
                    {
                        isVisible = false;
                        isReadonly = false;
                        sortOrder = 9990;
                        hidden++;
                    }

                    using (var upd = conn.CreateCommand())
                    {
                        upd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    ControlType = @ControlType,
    EntityId = @EntityId,
    DisplayName = COALESCE(@DisplayName, DisplayName),
    SortOrder = @SortOrder,
    IsVisible = @IsVisible,
    IsReadonly = @IsReadonly
WHERE TransactionFieldID = @FieldId";
                        upd.Parameters.AddWithValue("@ControlType", controlType);
                        upd.Parameters.AddWithValue("@EntityId", (object)appEntityId ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@DisplayName", (object)displayName ?? DBNull.Value);
                        upd.Parameters.AddWithValue("@SortOrder", sortOrder);
                        upd.Parameters.AddWithValue("@IsVisible", isVisible);
                        upd.Parameters.AddWithValue("@IsReadonly", isReadonly);
                        upd.Parameters.AddWithValue("@FieldId", fieldId);
                        if (upd.ExecuteNonQuery() > 0)
                            applied++;
                    }
                }
            }

            if (applied > 0 || hidden > 0)
            {
                executeResult.Messages.Add(
                    $"ListEdit field metadata applied: updated={applied}, hiddenExtra={hidden} (MU Sort/IsVisible; ControlType/EntityId from blueprint or FieldMapping).");
                try
                {
                    AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                }
                catch
                {
                    // Best-effort cache refresh; field rows are already persisted.
                }
            }
        }

        private enum RootIdentityColumnKind
        {
            None = 0,
            ReferenceId = 1,
            ProductCode = 2,
            Description = 3
        }

        /// <summary>
        /// Root ListEdit identity columns for hierarchical MU: always show when present on the unit.
        /// Product Code aliases include Article (Trim/Style common naming).
        /// </summary>
        private static RootIdentityColumnKind ClassifyRootIdentityColumn(string dbColumnName, string pkColumn)
        {
            if (string.IsNullOrWhiteSpace(dbColumnName))
                return RootIdentityColumnKind.None;

            string name = dbColumnName.Trim();
            if (!string.IsNullOrWhiteSpace(pkColumn)
                && string.Equals(name, pkColumn.Trim(), StringComparison.OrdinalIgnoreCase))
                return RootIdentityColumnKind.ReferenceId;
            if (string.Equals(name, "ReferenceId", StringComparison.OrdinalIgnoreCase))
                return RootIdentityColumnKind.ReferenceId;

            if (string.Equals(name, "Article", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "ProductCode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Product_Code", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "ItemCode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Item_Code", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "TrimCode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Trim_Code", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "StyleCode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Style_Code", StringComparison.OrdinalIgnoreCase))
                return RootIdentityColumnKind.ProductCode;

            if (string.Equals(name, "Description", StringComparison.OrdinalIgnoreCase))
                return RootIdentityColumnKind.Description;

            return RootIdentityColumnKind.None;
        }

        private static int RootIdentitySortOrder(RootIdentityColumnKind kind)
        {
            switch (kind)
            {
                case RootIdentityColumnKind.ReferenceId: return 0;
                case RootIdentityColumnKind.ProductCode: return 10;
                case RootIdentityColumnKind.Description: return 20;
                default: return 30;
            }
        }

        private static string DefaultRootIdentityDisplayName(RootIdentityColumnKind kind, string dbName)
        {
            switch (kind)
            {
                case RootIdentityColumnKind.ReferenceId: return "Ref No.";
                case RootIdentityColumnKind.ProductCode:
                    return string.Equals(dbName, "Article", StringComparison.OrdinalIgnoreCase)
                        ? "Article"
                        : "Product Code";
                case RootIdentityColumnKind.Description: return "Description";
                default: return dbName;
            }
        }

        private sealed class FieldMappingColumnMeta
        {
            public int? PlmControlType { get; set; }
            public int? PlmEntityId { get; set; }
        }

        /// <summary>
        /// Load Plm_FieldMapping ControlType/Entity by AppColumnName for one APP table
        /// (root TabFields via PlmSubItemId, child GridColumns via PlmMetaColumnId).
        /// </summary>
        private static Dictionary<string, FieldMappingColumnMeta> LoadFieldMappingMetaByAppTable(
            SqlConnection conn,
            string appTableName)
        {
            var map = new Dictionary<string, FieldMappingColumnMeta>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(appTableName) || OBJECT_ID_Exists(conn, "Plm_FieldMapping") == false)
                return map;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT AppColumnName, PlmControlType, PlmEntityId
FROM dbo.Plm_FieldMapping
WHERE AppTableName = @Table
  AND AppColumnName IS NOT NULL";
                cmd.Parameters.AddWithValue("@Table", appTableName.Trim());
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string col = reader.IsDBNull(0) ? null : reader.GetString(0);
                        if (string.IsNullOrWhiteSpace(col))
                            continue;
                        // Prefer rows that carry EntityId when duplicates exist.
                        var meta = new FieldMappingColumnMeta
                        {
                            PlmControlType = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            PlmEntityId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2)
                        };
                        if (!map.TryGetValue(col, out var existing)
                            || (existing.PlmEntityId == null && meta.PlmEntityId != null))
                        {
                            map[col] = meta;
                        }
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Fallback: resolve EntityId from Plm_FieldMapping when blueprint only has PlmMetaColumnId / PlmSubItemId.
        /// </summary>
        private static int? ResolveEntityFromFieldMapping(
            SqlConnection conn,
            PlmSearchMassUpdateListEditFieldDto bpField)
        {
            string mappingTable = "Plm_FieldMapping";
            if (OBJECT_ID_Exists(conn, mappingTable) == false)
                return null;

            using (var cmd = conn.CreateCommand())
            {
                if (bpField.PlmMetaColumnId.HasValue)
                {
                    cmd.CommandText = $@"
SELECT TOP 1 PlmEntityId, PlmControlType
FROM dbo.[{mappingTable}]
WHERE PlmMetaColumnId = @Id AND PlmEntityId IS NOT NULL";
                    cmd.Parameters.AddWithValue("@Id", bpField.PlmMetaColumnId.Value);
                }
                else if (bpField.PlmSubItemId.HasValue)
                {
                    cmd.CommandText = $@"
SELECT TOP 1 PlmEntityId, PlmControlType
FROM dbo.[{mappingTable}]
WHERE PlmSubItemId = @Id AND PlmEntityId IS NOT NULL";
                    cmd.Parameters.AddWithValue("@Id", bpField.PlmSubItemId.Value);
                }
                else
                {
                    return null;
                }

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    int plmEntityId = reader.GetInt32(0);
                    reader.Close();
                    return GetAppEntityInfoIdByPlmEntityId(conn, null, plmEntityId);
                }
            }
        }

        private static bool OBJECT_ID_Exists(SqlConnection conn, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT OBJECT_ID(@Name, 'U')";
                cmd.Parameters.AddWithValue("@Name", "dbo." + tableName);
                var val = cmd.ExecuteScalar();
                return val != null && val != DBNull.Value;
            }
        }

        private static int? GetRootTransactionUnitId(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId
  AND (ParentTransactionUnitID IS NULL OR ParentTransactionUnitID = 0)
ORDER BY TransactionUnitID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetTransactionUnitIdByTableName(SqlConnection conn, int transactionId, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId
  AND (DataBaseTableName = @Table OR UnitDisplayName = @Table)
ORDER BY TransactionUnitID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@Table", tableName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? ResolveTransactionFieldId(
            SqlConnection conn,
            int transactionId,
            int? unitId,
            string databaseFieldName)
        {
            if (string.IsNullOrWhiteSpace(databaseFieldName))
                return null;

            using (var cmd = conn.CreateCommand())
            {
                if (unitId.HasValue)
                {
                    cmd.CommandText = @"
SELECT TOP 1 TransactionFieldID
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = @Col
ORDER BY TransactionFieldID";
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                }
                else
                {
                    cmd.CommandText = @"
SELECT TOP 1 f.TransactionFieldID
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId AND f.DataBaseFieldName = @Col
ORDER BY f.TransactionFieldID";
                    cmd.Parameters.AddWithValue("@TxId", transactionId);
                }

                cmd.Parameters.AddWithValue("@Col", databaseFieldName.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }
    }
}
