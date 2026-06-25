using System;
using System.Collections.Generic;
using System.Data;
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
    private const string DwBlueprintModeInsert = "Insert";
    private const string DwBlueprintModeUpdate = "Update";
    private const string DwBlueprintModeRepair = "Repair";
    private const string DwFieldPolicyAllMapped = "AllMappedColumns";
    private const string DwFieldPolicyExclusive = "ExclusiveSubItemsOnly";
    private const int PlmControlTypeDdl = 1;
    private const int PlmControlTypeTextBox = 2;

    private sealed class DwFieldMappingRow
    {
      public string AppTableName { get; set; }
      public string AppColumnName { get; set; }
      public string DwTableName { get; set; }
      public string DwColumnName { get; set; }
      public int? PlmTabId { get; set; }
      public int? PlmSubItemId { get; set; }
      public int? PlmGridSubItemId { get; set; }
      public int? PlmGridId { get; set; }
      public int? PlmMetaColumnId { get; set; }
      public string DwFkTarget { get; set; }
      public string FieldKind { get; set; }
      public int? PlmControlType { get; set; }
      public int? PlmEntityId { get; set; }
      public string DwDataType { get; set; }
    }

    public static OperationCallResult<PlmDwImportBlueprintDto> LoadDwImportBlueprint(PlmDwBlueprintLoadRequestDto request)
    {
      var result = new OperationCallResult<PlmDwImportBlueprintDto>();
      try
      {
        RequirePlmMigrationAdmin();
        if (request == null || string.IsNullOrWhiteSpace(request.BlueprintJson))
          throw new ArgumentException("BlueprintJson is required.");

        var blueprint = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(request.BlueprintJson);
        if (blueprint == null)
          throw new InvalidOperationException("Blueprint JSON could not be deserialized.");

        if (blueprint.SchemaVersion <= 0)
          blueprint.SchemaVersion = 1;

        result.Object = blueprint;
      }
      catch (Exception ex)
      {
        result.ValidationResult.Items.Add(new ValidationItem(
          typeof(PlmMigrationBL), "Plm_DwBlueprint_Load_Error", ValidationItemType.Error, ex.Message));
      }

      return result;
    }

    public static OperationCallResult<PlmDwImportBlueprintDto> LoadDwImportBlueprintFromTenantTable(string tablePrefix, string blueprintKey = "default")
    {
      var result = new OperationCallResult<PlmDwImportBlueprintDto>();
      try
      {
        RequirePlmMigrationAdmin();
        if (string.IsNullOrWhiteSpace(tablePrefix))
          tablePrefix = DefaultTablePrefix;
        if (!tablePrefix.EndsWith("_", StringComparison.Ordinal))
          tablePrefix += "_";

        string tenantConn = GetTenantConnectionString();
        string tableName = tablePrefix + "ImportBlueprint";
        using (var conn = new SqlConnection(tenantConn))
        {
          conn.Open();
          if (!TemplateTableExists(conn, null, tableName))
            throw new InvalidOperationException($"Blueprint table dbo.[{tableName}] does not exist. Run PlmDw_ImportBlueprint.sql first.");

          using (var cmd = conn.CreateCommand())
          {
            cmd.CommandText = $@"
SELECT TOP 1 BlueprintJson
FROM dbo.[{tableName}]
WHERE BlueprintKey = @BlueprintKey";
            cmd.Parameters.AddWithValue("@BlueprintKey", blueprintKey ?? "default");
            var json = cmd.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(json))
              throw new InvalidOperationException($"No blueprint found for key '{blueprintKey}' in {tableName}.");

            result.Object = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json);
          }
        }
      }
      catch (Exception ex)
      {
        result.ValidationResult.Items.Add(new ValidationItem(
          typeof(PlmMigrationBL), "Plm_DwBlueprint_LoadTable_Error", ValidationItemType.Error, ex.Message));
      }

      return result;
    }

    public static OperationCallResult<PlmDwBlueprintValidationDto> ValidateDwImportBlueprint(PlmDwImportBlueprintDto blueprint)
    {
      var result = new OperationCallResult<PlmDwBlueprintValidationDto>
      {
        Object = new PlmDwBlueprintValidationDto()
      };
      try
      {
        RequirePlmMigrationAdmin();
        string tenantConn = GetTenantConnectionString();
        ValidateDwBlueprintInternal(blueprint, tenantConn, result.Object);
        result.Object.IsValid = result.Object.Errors.Count == 0;
      }
      catch (Exception ex)
      {
        result.Object.Errors.Add(ex.Message);
        result.Object.IsValid = false;
        result.ValidationResult.Items.Add(new ValidationItem(
          typeof(PlmMigrationBL), "Plm_DwBlueprint_Validate_Error", ValidationItemType.Error, ex.Message));
      }

      return result;
    }

    public static OperationCallResult<PlmDwBlueprintPreviewDto> PreviewDwBlueprintConfig(PlmDwImportBlueprintDto blueprint)
    {
      var result = new OperationCallResult<PlmDwBlueprintPreviewDto>
      {
        Object = new PlmDwBlueprintPreviewDto { IsSuccess = true }
      };
      try
      {
        RequirePlmMigrationAdmin();
        if (blueprint == null)
          throw new ArgumentException("Blueprint is required.");

        string tenantConn = GetTenantConnectionString();
        var validation = new PlmDwBlueprintValidationDto();
        ValidateDwBlueprintInternal(blueprint, tenantConn, validation);
        if (validation.Errors.Count > 0)
          throw new InvalidOperationException(string.Join("; ", validation.Errors));

        string prefix = ResolveBlueprintTablePrefix(blueprint);
        using (var conn = new SqlConnection(tenantConn))
        {
          conn.Open();
          result.Object.Items = BuildDwBlueprintPreviewItems(conn, blueprint, prefix);
        }
      }
      catch (Exception ex)
      {
        result.Object.IsSuccess = false;
        result.Object.ErrorMessage = ex.Message;
        result.ValidationResult.Items.Add(new ValidationItem(
          typeof(PlmMigrationBL), "Plm_DwBlueprint_Preview_Error", ValidationItemType.Error, ex.Message));
      }

      return result;
    }

    public static OperationCallResult<PlmDwBlueprintExecuteResultDto> ExecuteDwBlueprintConfig(PlmDwBlueprintExecuteRequestDto request)
    {
      var result = new OperationCallResult<PlmDwBlueprintExecuteResultDto>
      {
        Object = new PlmDwBlueprintExecuteResultDto()
      };
      try
      {
        RequirePlmMigrationAdmin();
        if (request?.Blueprint == null)
          throw new ArgumentException("Blueprint is required.");

        string mode = string.IsNullOrWhiteSpace(request.Mode) ? DwBlueprintModeInsert : request.Mode.Trim();
        string tenantConn = GetTenantConnectionString();
        var validation = new PlmDwBlueprintValidationDto();
        ValidateDwBlueprintInternal(request.Blueprint, tenantConn, validation);
        if (validation.Errors.Count > 0)
          throw new InvalidOperationException(string.Join("; ", validation.Errors));

        int tenantDataSourceId = GetTenantDataSourceId();
        int? saasApplicationId = request.SaasApplicationId
          ?? request.Blueprint.TransactionGroup?.SaasApplicationId;

        string prefix = ResolveBlueprintTablePrefix(request.Blueprint);
        string rootTable = ResolveBlueprintRootTableName(request.Blueprint, prefix);
        var mappingRows = LoadDwFieldMappingRows(tenantConn, prefix);
        var plans = DwBlueprintExecutionPlanBuilder.BuildExecutionPlans(
          request.Blueprint, prefix, mappingRows);

        using (var conn = new SqlConnection(tenantConn))
        {
          conn.Open();
          RefreshTenantTableSchemaCache(tenantDataSourceId);

          foreach (var plan in plans)
          {
            if (plan.Tab.ImportStatus == TemplateStatusSkipped)
              continue;

            string integrationId = ResolveDwTransactionIntegrationId(request.Blueprint, plan);
            bool isUpdate = GetTransactionIdByIntegrationId(conn, null, integrationId).HasValue;
            if (string.Equals(mode, DwBlueprintModeInsert, StringComparison.OrdinalIgnoreCase) && isUpdate)
              continue;

            bool applyUpsert = string.Equals(mode, DwBlueprintModeRepair, StringComparison.OrdinalIgnoreCase)
              ? isUpdate
              : !string.Equals(mode, DwBlueprintModeInsert, StringComparison.OrdinalIgnoreCase) || !isUpdate;

            if (!applyUpsert && !isUpdate)
              applyUpsert = true;

            if (!applyUpsert)
              continue;

            int? txId = UpsertTabTransactionFromPlan(
              conn, null, plan, rootTable, prefix, tenantDataSourceId, saasApplicationId, isUpdate);

            if (!txId.HasValue)
              throw new InvalidOperationException($"Failed to upsert transaction for tab {plan.Tab.TabId}.");

            SetIntegrationId(conn, null, "AppTransaction", "TransactionID", txId.Value, integrationId);

            result.Object.TransactionIds.Add(txId.Value);
            if (isUpdate)
              result.Object.TransactionsUpdated++;
            else
              result.Object.TransactionsInserted++;
          }

          var tabRows = plans.Select(p => p.Tab).ToList();
          if (!string.Equals(mode, DwBlueprintModeRepair, StringComparison.OrdinalIgnoreCase))
            EnsureTabTransactionForms(conn, tabRows, null);

          if (request.IncludeTransactionGroup)
          {
            var groupTransactionIds = new List<int>();
            foreach (var plan in plans)
            {
              if (plan.Tab.ImportStatus == TemplateStatusSkipped)
                continue;

              string integrationId = ResolveDwTransactionIntegrationId(request.Blueprint, plan);
              int? txId = GetTransactionIdByIntegrationId(conn, null, integrationId);
              if (txId.HasValue)
                groupTransactionIds.Add(txId.Value);
            }

            result.Object.TransactionGroupId = EnsureDwBlueprintTransactionGroup(
              conn, request.Blueprint, groupTransactionIds);
          }

          if (request.IncludeSearchView && request.Blueprint.SearchView != null)
          {
            result.Object.SearchId = EnsureDwBlueprintSearchAndView(
              conn, request.Blueprint, tabRows, rootTable, tenantDataSourceId, saasApplicationId);
          }

          if (request.IncludeNavigation && result.Object.SearchId.HasValue)
          {
            string menuName = request.Blueprint.Navigation?.FolderName
              ?? request.Blueprint.TransactionGroup?.Name
              ?? request.Blueprint.SearchView?.Search?.Name
              ?? "PLM Import";
            string description = request.Blueprint.TransactionGroup?.Name ?? menuName;
            var menuResult = AppDatabaseViewBL.AddSearchToApplicationMainMenu(
              result.Object.SearchId.Value, saasApplicationId, menuName, description);
            if (!menuResult.IsSuccessful && menuResult.ValidationResult?.HasErrors == true)
            {
              result.ValidationResult.Items.Add(new ValidationItem(
                typeof(PlmMigrationBL), "Plm_DwBlueprint_Navigation_Warning", ValidationItemType.Warning,
                menuResult.ValidationResult.Items?.FirstOrDefault()?.Message ?? "Navigation menu update failed."));
            }
          }

          result.Object.IsSuccess = true;
        }
      }
      catch (Exception ex)
      {
        result.Object.IsSuccess = false;
        result.Object.ErrorMessage = ex.Message;
        result.ValidationResult.Items.Add(new ValidationItem(
          typeof(PlmMigrationBL), "Plm_DwBlueprint_Execute_Error", ValidationItemType.Error, ex.Message));
      }

      return result;
    }

    private static string GetTenantConnectionString()
    {
      var tenantRegister = AppDataSourceRegisterBL.RetrieveOneAppDataSourceRegisterEntity(GetTenantDataSourceId());
      if (tenantRegister == null || string.IsNullOrWhiteSpace(tenantRegister.ConnectionString))
        throw new InvalidOperationException("Tenant database connection is not available.");
      return AppConnectionStringEncryptionBL.Decrypt(tenantRegister.ConnectionString);
    }

    private static string ResolveBlueprintTablePrefix(PlmDwImportBlueprintDto blueprint)
    {
      string prefix = blueprint?.Source?.TablePrefix;
      if (string.IsNullOrWhiteSpace(prefix))
        prefix = DefaultTablePrefix;
      if (!prefix.EndsWith("_", StringComparison.Ordinal))
        prefix += "_";
      return prefix;
    }

    private static string ResolveBlueprintRootTableName(PlmDwImportBlueprintDto blueprint, string prefix)
    {
      string appTable = blueprint?.RootUnit?.AppTableName;
      if (!string.IsNullOrWhiteSpace(appTable))
      {
        if (appTable.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          return appTable;
        return prefix + appTable.TrimStart('_');
      }
      return prefix + "ReferenceBasicInfo";
    }

    private static void ValidateDwBlueprintInternal(
      PlmDwImportBlueprintDto blueprint,
      string tenantConn,
      PlmDwBlueprintValidationDto validation)
    {
      if (blueprint == null)
      {
        validation.Errors.Add("Blueprint is required.");
        return;
      }

      if (blueprint.SchemaVersion != 1)
        validation.Warnings.Add($"Blueprint schemaVersion {blueprint.SchemaVersion} is not explicitly supported; using v1 rules.");

      if (blueprint.Transactions == null || blueprint.Transactions.Count == 0)
        validation.Errors.Add("Blueprint must contain at least one transaction.");

      string prefix = ResolveBlueprintTablePrefix(blueprint);
      var mappingRows = LoadDwFieldMappingRows(tenantConn, prefix);
      if (mappingRows.Count == 0)
        validation.Errors.Add($"Field mapping table {prefix}FieldMapping is empty or missing.");

      using (var conn = new SqlConnection(tenantConn))
      {
        conn.Open();
        string rootTable = ResolveBlueprintRootTableName(blueprint, prefix);
        if (!TemplateTableExists(conn, null, rootTable))
          validation.Errors.Add($"Root table dbo.[{rootTable}] does not exist.");

        foreach (var tx in blueprint.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
        {
          if (string.Equals(tx.ImportStatus, TemplateStatusSkipped, StringComparison.OrdinalIgnoreCase))
            continue;

          var siblings = tx.UnitStructure?.SiblingUnits?
            .Where(s => s != null && !string.IsNullOrWhiteSpace(s.AppTableName))
            .ToList() ?? new List<PlmDwBlueprintSiblingUnitDto>();
          var childUnits = tx.UnitStructure?.ChildUnits?
            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.AppTableName))
            .ToList() ?? new List<PlmDwBlueprintChildUnitDto>();
          if (siblings.Count == 0 && childUnits.Count == 0)
          {
            validation.Errors.Add($"Transaction Tab_{tx.PlmTabId} is missing a sibling or child unit table.");
            continue;
          }

          foreach (var sibling in siblings)
          {
            string siblingTable = QualifyBlueprintTableName(sibling.AppTableName, prefix);
            if (!TemplateTableExists(conn, null, siblingTable))
              validation.Errors.Add($"Sibling table dbo.[{siblingTable}] does not exist for Tab_{tx.PlmTabId}.");

            var visibleMappings = FilterMappingForTransaction(tx, sibling, mappingRows, prefix);
            if (visibleMappings.Count == 0)
              validation.Warnings.Add($"Transaction Tab_{tx.PlmTabId} has no field mapping rows for table {siblingTable}.");
          }

          foreach (var child in childUnits)
          {
            string childTable = QualifyBlueprintTableName(child.AppTableName, prefix);
            if (!TemplateTableExists(conn, null, childTable))
              validation.Errors.Add($"Child unit table dbo.[{childTable}] does not exist for Tab_{tx.PlmTabId}.");
          }

          int? existingTx = GetTransactionIdByIntegrationId(conn, null, tx.IntegrationId ?? $"Tab_{tx.PlmTabId}");
          if (existingTx.HasValue)
            validation.Warnings.Add($"Transaction {tx.IntegrationId ?? $"Tab_{tx.PlmTabId}"} already exists (ID {existingTx}).");
        }

        foreach (var grid in blueprint.GridBindings ?? Enumerable.Empty<PlmDwBlueprintGridBindingDto>())
        {
          string gridTable = QualifyBlueprintTableName(grid.AppTableName, prefix);
          if (!TemplateTableExists(conn, null, gridTable))
            validation.Errors.Add($"Grid table dbo.[{gridTable}] does not exist.");
        }
      }

      foreach (var field in blueprint.BlueprintFields ?? Enumerable.Empty<PlmDwBlueprintFieldDto>())
      {
        bool inMapping = mappingRows.Any(m =>
          string.Equals(m.AppTableName, field.AppTableName, StringComparison.OrdinalIgnoreCase)
          && string.Equals(m.AppColumnName, field.AppColumnName, StringComparison.OrdinalIgnoreCase));
        if (!inMapping)
          validation.Warnings.Add($"Blueprint field {field.AppTableName}.{field.AppColumnName} not found in FieldMapping.");
      }

      var templateHeaderTabIds = GetDwBlueprintTemplateHeaderTabIds(blueprint);
      if ((blueprint.PlmTemplate?.TemplateId ?? blueprint.Source?.PlmTemplateId) > 0 && templateHeaderTabIds.Count == 0)
      {
        validation.Warnings.Add(
          "Blueprint plmTemplate has no TemplateHeaderTabIds and no transaction IsTemplateHeaderTab=true; Search links will use MainItem for all tabs.");
      }
    }

    private static List<PlmDwBlueprintPreviewItemDto> BuildDwBlueprintPreviewItems(
      SqlConnection conn,
      PlmDwImportBlueprintDto blueprint,
      string prefix)
    {
      var items = new List<PlmDwBlueprintPreviewItemDto>();

      if (blueprint.TransactionGroup != null)
      {
        int? groupId = GetTransactionGroupIdByName(conn, blueprint.TransactionGroup.Name);
        items.Add(new PlmDwBlueprintPreviewItemDto
        {
          ObjectType = "TransactionGroup",
          Name = blueprint.TransactionGroup.Name,
          IntegrationId = blueprint.TransactionGroup.IntegrationId,
          Action = groupId.HasValue ? TemplateActionUpdate : TemplateActionInsert,
          ExistingId = groupId
        });
      }

      foreach (var tx in blueprint.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
      {
        if (string.Equals(tx.ImportStatus, TemplateStatusSkipped, StringComparison.OrdinalIgnoreCase))
        {
          items.Add(new PlmDwBlueprintPreviewItemDto
          {
            ObjectType = "Transaction",
            Name = tx.TransactionName ?? tx.PlmTabName,
            IntegrationId = tx.IntegrationId ?? $"Tab_{tx.PlmTabId}",
            Action = "Skip"
          });
          continue;
        }

        string integrationId = tx.IntegrationId ?? $"Tab_{tx.PlmTabId}";
        int? existingId = GetTransactionIdByIntegrationId(conn, null, integrationId);
        items.Add(new PlmDwBlueprintPreviewItemDto
        {
          ObjectType = "Transaction",
          Name = tx.TransactionName ?? tx.PlmTabName,
          IntegrationId = integrationId,
          Action = existingId.HasValue ? TemplateActionUpdate : TemplateActionInsert,
          ExistingId = existingId
        });
      }

      if (blueprint.SearchView?.Search != null)
      {
        int? searchId = GetSearchIdByIntegrationId(conn, null, blueprint.SearchView.Search.IntegrationId);
        items.Add(new PlmDwBlueprintPreviewItemDto
        {
          ObjectType = "Search",
          Name = blueprint.SearchView.Search.Name,
          IntegrationId = blueprint.SearchView.Search.IntegrationId,
          Action = searchId.HasValue ? TemplateActionUpdate : TemplateActionInsert,
          ExistingId = searchId
        });
      }

      return items;
    }

    private static List<DwFieldMappingRow> LoadDwFieldMappingRows(string tenantConn, string prefix)
    {
      string mappingTable = prefix + "FieldMapping";
      var rows = new List<DwFieldMappingRow>();
      using (var conn = new SqlConnection(tenantConn))
      {
        conn.Open();
        if (!TemplateTableExists(conn, null, mappingTable))
          return rows;

        bool hasExtended = MappingTableHasColumn(conn, mappingTable, "PlmControlType");
        string sql = hasExtended
          ? $@"SELECT AppTableName, AppColumnName, DwTableName, DwColumnName,
    PlmTabId, PlmSubItemId, PlmGridSubItemId, PlmGridId, PlmMetaColumnId,
    DwFkTarget, FieldKind, PlmControlType, PlmEntityId, DwDataType
FROM dbo.[{mappingTable}]"
          : $@"SELECT AppTableName, AppColumnName, DwTableName, DwColumnName,
    PlmTabId, PlmSubItemId, PlmGridSubItemId, PlmGridId, PlmMetaColumnId,
    DwFkTarget, FieldKind
FROM dbo.[{mappingTable}]";

        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = sql;
          using (var reader = cmd.ExecuteReader())
          {
            while (reader.Read())
            {
              var row = new DwFieldMappingRow
              {
                AppTableName = reader.GetString(0),
                AppColumnName = reader.GetString(1),
                DwTableName = reader.GetString(2),
                DwColumnName = reader.GetString(3),
                PlmTabId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                PlmSubItemId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                PlmGridSubItemId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                PlmGridId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                PlmMetaColumnId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                DwFkTarget = reader.IsDBNull(9) ? null : reader.GetString(9),
                FieldKind = reader.GetString(10)
              };
              if (hasExtended && reader.FieldCount > 11)
              {
                row.PlmControlType = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11);
                row.PlmEntityId = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12);
                row.DwDataType = reader.IsDBNull(13) ? null : reader.GetString(13);
              }
              rows.Add(row);
            }
          }
        }
      }

      return rows;
    }

    private static bool MappingTableHasColumn(SqlConnection conn, string tableName, string columnName)
    {
      using (var cmd = conn.CreateCommand())
      {
        cmd.CommandText = @"
SELECT 1 FROM sys.columns
WHERE object_id = OBJECT_ID(@Table) AND name = @Column";
        cmd.Parameters.AddWithValue("@Table", "dbo." + tableName);
        cmd.Parameters.AddWithValue("@Column", columnName);
        return cmd.ExecuteScalar() != null;
      }
    }

    private static string ResolveDwTransactionIntegrationId(PlmDwImportBlueprintDto blueprint, TemplateTabExecutionPlan plan)
    {
      return ResolveDwTabIntegrationId(blueprint, plan?.Tab) ?? $"Tab_{plan?.Tab?.TabId}";
    }

    private static string QualifyBlueprintTableName(string appTableName, string prefix)
    {
      if (string.IsNullOrWhiteSpace(appTableName))
        return appTableName;
      if (appTableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        return appTableName;
      return prefix + appTableName.TrimStart('_');
    }

    private static List<DwFieldMappingRow> FilterMappingForTransaction(
      PlmDwBlueprintTransactionDto tx,
      PlmDwBlueprintSiblingUnitDto sibling,
      List<DwFieldMappingRow> mappingRows,
      string prefix)
    {
      string siblingTable = QualifyBlueprintTableName(sibling.AppTableName, prefix);
      string logicalTable = sibling.AppTableName;
      var tableMappings = mappingRows.Where(m =>
        string.Equals(QualifyBlueprintTableName(m.AppTableName, prefix), siblingTable, StringComparison.OrdinalIgnoreCase)
        || string.Equals(m.AppTableName, logicalTable, StringComparison.OrdinalIgnoreCase)).ToList();

      if (string.Equals(sibling.FieldPolicy, DwFieldPolicyExclusive, StringComparison.OrdinalIgnoreCase)
          && !string.IsNullOrWhiteSpace(sibling.ExcludeSubItemsFromDwTable))
      {
        var excludeSubItemIds = mappingRows
          .Where(m => string.Equals(m.DwTableName, sibling.ExcludeSubItemsFromDwTable, StringComparison.OrdinalIgnoreCase)
            && m.PlmSubItemId.HasValue)
          .Select(m => m.PlmSubItemId.Value)
          .ToHashSet();
        tableMappings = tableMappings
          .Where(m => !m.PlmSubItemId.HasValue || !excludeSubItemIds.Contains(m.PlmSubItemId.Value))
          .ToList();
      }

      return tableMappings;
    }

    private static int? GetTransactionGroupIdByName(SqlConnection conn, string name)
    {
      if (string.IsNullOrWhiteSpace(name))
        return null;
      using (var cmd = conn.CreateCommand())
      {
        cmd.CommandText = "SELECT TOP 1 TransactionGroupID FROM dbo.AppTransactionGroup WHERE GroupName = @Name";
        cmd.Parameters.AddWithValue("@Name", name);
        var val = cmd.ExecuteScalar();
        return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
      }
    }

    /// <summary>
    /// AppTransactionGroupItem.TransactionItemID FK references AppTransactionItem.AppTransactionItemID.
    /// DW blueprint creates AppTransaction rows only; ensure a library item exists per transaction.
    /// </summary>
    private static int EnsureAppTransactionItemId(SqlConnection conn, SqlTransaction tran, int transactionId)
    {
      using (var cmd = conn.CreateCommand())
      {
        cmd.Transaction = tran;
        cmd.CommandText = @"
SELECT TOP 1 AppTransactionItemID
FROM dbo.AppTransactionItem
WHERE TransactionID = @TransactionId
ORDER BY AppTransactionItemID";
        cmd.Parameters.AddWithValue("@TransactionId", transactionId);
        var existing = cmd.ExecuteScalar();
        if (existing != null && existing != DBNull.Value)
          return Convert.ToInt32(existing);
      }

      string itemName = $"Transaction {transactionId}";
      using (var cmd = conn.CreateCommand())
      {
        cmd.Transaction = tran;
        cmd.CommandText = "SELECT TransactionName FROM dbo.AppTransaction WHERE TransactionID = @TransactionId";
        cmd.Parameters.AddWithValue("@TransactionId", transactionId);
        var name = cmd.ExecuteScalar() as string;
        if (!string.IsNullOrWhiteSpace(name))
          itemName = name;
      }

      using (var cmd = conn.CreateCommand())
      {
        cmd.Transaction = tran;
        cmd.CommandText = @"
INSERT INTO dbo.AppTransactionItem (TransactionID, TransactionItemName, Description)
VALUES (@TransactionId, @Name, @Description);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.AddWithValue("@TransactionId", transactionId);
        cmd.Parameters.AddWithValue("@Name", itemName);
        cmd.Parameters.AddWithValue("@Description", itemName);
        return Convert.ToInt32(cmd.ExecuteScalar());
      }
    }

    private static int EnsureDwBlueprintTransactionGroup(
      SqlConnection conn,
      PlmDwImportBlueprintDto blueprint,
      List<int> transactionIds)
    {
      var groupSpec = blueprint.TransactionGroup;
      if (groupSpec == null || string.IsNullOrWhiteSpace(groupSpec.Name))
        return 0;

      int? groupId = GetTransactionGroupIdByName(conn, groupSpec.Name);
      if (!groupId.HasValue && transactionIds.Count == 0)
        return 0;

      if (!groupId.HasValue)
      {
        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"
INSERT INTO dbo.AppTransactionGroup (GroupName, Description, SaasApplicationID)
VALUES (@Name, @Description, @SaasApplicationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
          cmd.Parameters.AddWithValue("@Name", groupSpec.Name);
          cmd.Parameters.AddWithValue("@Description", (object)groupSpec.Name ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@SaasApplicationId", (object)groupSpec.SaasApplicationId ?? DBNull.Value);
          groupId = Convert.ToInt32(cmd.ExecuteScalar());
        }
      }

      using (var delCmd = conn.CreateCommand())
      {
        delCmd.CommandText = "DELETE FROM dbo.AppTransactionGroupItem WHERE TransactionGroupID = @GroupId";
        delCmd.Parameters.AddWithValue("@GroupId", groupId.Value);
        delCmd.ExecuteNonQuery();
      }

      int order = 0;
      foreach (int txId in transactionIds.Distinct())
      {
        order++;
        int transactionItemId = EnsureAppTransactionItemId(conn, null, txId);
        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"
INSERT INTO dbo.AppTransactionGroupItem (TransactionGroupID, TransactionItemID, TransactionLayoutOrder, TransID)
VALUES (@GroupId, @TransactionItemId, @Order, @TransactionId)";
          cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
          cmd.Parameters.AddWithValue("@TransactionItemId", transactionItemId);
          cmd.Parameters.AddWithValue("@Order", order);
          cmd.Parameters.AddWithValue("@TransactionId", txId);
          cmd.ExecuteNonQuery();
        }
      }

      return groupId.Value;
    }

    private static int EnsureDwBlueprintSearchAndView(
      SqlConnection conn,
      PlmDwImportBlueprintDto blueprint,
      List<PlmTemplateTabRow> tabRows,
      string rootTable,
      int tenantDataSourceId,
      int? saasApplicationId)
    {
      var searchSpec = blueprint.SearchView?.Search;
      if (searchSpec == null || string.IsNullOrWhiteSpace(searchSpec.IntegrationId))
        return 0;

      int searchId;
      int? existingSearchId = GetSearchIdByIntegrationId(conn, null, searchSpec.IntegrationId);
      string searchName = searchSpec.Name ?? blueprint.TransactionGroup?.Name ?? "PLM References";

      if (!existingSearchId.HasValue)
      {
        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"
INSERT INTO dbo.AppSearch (Name, Description, Type, IsAutoExecute, SaasApplicationID, IntegrationId)
VALUES (@Name, @Description, @Type, @IsAutoExecute, @SaasApplicationId, @IntegrationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
          cmd.Parameters.AddWithValue("@Name", searchName);
          cmd.Parameters.AddWithValue("@Description", searchName);
          cmd.Parameters.AddWithValue("@Type", (int)EmAppSearchUsageType.DataModelTemplate);
          cmd.Parameters.AddWithValue("@IsAutoExecute", false);
          cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@IntegrationId", searchSpec.IntegrationId);
          searchId = Convert.ToInt32(cmd.ExecuteScalar());
        }
      }
      else
      {
        searchId = existingSearchId.Value;
        using (var cmd = conn.CreateCommand())
        {
          cmd.CommandText = @"
UPDATE dbo.AppSearch SET Name = @Name, Description = @Description, SaasApplicationID = @SaasApplicationId
WHERE SearchId = @SearchId";
          cmd.Parameters.AddWithValue("@Name", searchName);
          cmd.Parameters.AddWithValue("@Description", searchName);
          cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
          cmd.Parameters.AddWithValue("@SearchId", searchId);
          cmd.ExecuteNonQuery();
        }
      }

      string queryText = BuildReferenceBasicInfoDataSetQuery(rootTable);
      string dataSetName = TruncateDataSetName(searchName);

      AppSearchExDto searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
      AppDataSetExDto dataSetDto;
      if (searchDto.DataSetId.HasValue)
      {
        dataSetDto = AppDataSetBL.RetrieveOneAppDataSetExDto(searchDto.DataSetId.Value);
        dataSetDto.QueryText = queryText;
        dataSetDto.Name = dataSetName;
        dataSetDto.Description = searchName;
        dataSetDto.IsModified = true;
      }
      else
      {
        dataSetDto = new AppDataSetExDto
        {
          Name = dataSetName,
          Description = searchName,
          QueryType = (int)EmAppDataServiceType.QueryText,
          QueryText = queryText,
          DataSourceFrom = tenantDataSourceId,
          SaasApplicationId = saasApplicationId
        };
      }

      var dataSetSaveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
      if (!dataSetSaveResult.IsSuccessfulWithResult)
        throw new InvalidOperationException(dataSetSaveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
          ?? "Failed to save blueprint dataset.");

      int dataSetId = Convert.ToInt32(dataSetSaveResult.Object.Id);

      AppSearchViewExDto searchViewDto;
      if (searchDto.SearchViewId.HasValue)
      {
        ClearSearchViewFields(conn, searchDto.SearchViewId.Value);
        searchViewDto = AppSearchViewConfigBL.RetrieveOneAppSearchViewExDto(searchDto.SearchViewId.Value);
        searchViewDto.Name = dataSetName;
        searchViewDto.Description = searchName;
        searchViewDto.DataSetId = dataSetId;
        searchViewDto.IsModified = true;
        searchViewDto.AppSearchViewFieldList = BuildTemplateSearchViewFields();
      }
      else
      {
        searchViewDto = new AppSearchViewExDto
        {
          Name = dataSetName,
          Description = searchName,
          DataSetId = dataSetId,
          ViewType = (int)EmAppViewType.GridView,
          AppSearchViewFieldList = BuildTemplateSearchViewFields()
        };
      }

      var searchViewSaveResult = AppSearchViewConfigBL.SaveAppSearchViewExDto(searchViewDto);
      if (!searchViewSaveResult.IsSuccessfulWithResult)
        throw new InvalidOperationException(searchViewSaveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
          ?? "Failed to save blueprint search view.");

      searchViewDto = searchViewSaveResult.Object;
      int searchViewId = Convert.ToInt32(searchViewDto.Id);

      searchDto = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
      searchDto.DataSetId = dataSetId;
      searchDto.SearchViewId = searchViewId;
      searchDto.Type = (int)EmAppSearchUsageType.DataModelTemplate;
      searchDto.IsAutoExecute = false;
      searchDto.SaasApplicationId = saasApplicationId;
      searchDto.Name = searchName;
      searchDto.Description = searchName;
      searchDto.IsModified = true;
      if (searchDto.AppSearchFieldList == null)
        searchDto.AppSearchFieldList = new ObservableSet<AppSearchFieldExDto>();

      var searchSaveResult = AppSearchConfigBL.SaveAppSearchExDto(searchDto);
      if (!searchSaveResult.IsSuccessfulWithResult)
        throw new InvalidOperationException(searchSaveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
          ?? "Failed to update blueprint search.");

      ClearSearchViewFormLinkTargets(conn, searchViewId);
      SaveDwBlueprintLinkTargets(searchViewDto, blueprint, tabRows, conn);

      return searchId;
    }

    private static HashSet<int> GetDwBlueprintTemplateHeaderTabIds(PlmDwImportBlueprintDto blueprint)
    {
      var ids = new HashSet<int>();
      if (blueprint?.PlmTemplate?.TemplateHeaderTabIds != null)
      {
        foreach (int id in blueprint.PlmTemplate.TemplateHeaderTabIds)
          ids.Add(id);
      }

      foreach (var tx in blueprint?.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
      {
        if (tx.IsTemplateHeaderTab == true)
          ids.Add(tx.PlmTabId);
      }

      return ids;
    }

    private static bool IsDwBlueprintTemplateHeaderTab(
      PlmDwImportBlueprintDto blueprint,
      PlmTemplateTabRow tab,
      HashSet<int> templateHeaderTabIds)
    {
      if (tab?.IsTemplateHeaderTab == true)
        return true;

      var txSpec = blueprint?.Transactions?
        .FirstOrDefault(t => t.PlmTabId == tab.TabId);
      if (txSpec?.IsTemplateHeaderTab == true)
        return true;

      return tab != null && templateHeaderTabIds.Contains(tab.TabId);
    }

    private static string ResolveDwTabIntegrationId(PlmDwImportBlueprintDto blueprint, PlmTemplateTabRow tab)
    {
      if (tab == null)
        return null;

      var txSpec = blueprint?.Transactions?
        .FirstOrDefault(t => t.PlmTabId == tab.TabId);
      if (!string.IsNullOrWhiteSpace(txSpec?.IntegrationId))
        return txSpec.IntegrationId;

      if (tab.TabId < 0)
      {
        var grid = blueprint?.GridBindings?
          .FirstOrDefault(g => g.PlmGridId == -tab.TabId);
        if (grid != null)
        {
          if (!string.IsNullOrWhiteSpace(grid.TransactionIntegrationId))
            return grid.TransactionIntegrationId;
          if (!string.IsNullOrWhiteSpace(grid.IntegrationId))
            return grid.IntegrationId;
          return $"Grid_{grid.PlmGridId}";
        }
      }

      return $"Tab_{tab.TabId}";
    }

    private static void SaveDwBlueprintLinkTargets(
      AppSearchViewExDto searchViewDto,
      PlmDwImportBlueprintDto blueprint,
      List<PlmTemplateTabRow> tabRows,
      SqlConnection conn)
    {
      int searchViewId = Convert.ToInt32(searchViewDto.Id);
      var rootField = searchViewDto.AppSearchViewFieldList?
        .FirstOrDefault(f => string.Equals(f.SysTableFiledPath, "ReferenceId", StringComparison.OrdinalIgnoreCase));
      if (rootField?.Id == null)
        throw new InvalidOperationException("Blueprint search view is missing ReferenceId field.");

      int sourceViewColumnId = Convert.ToInt32(rootField.Id);
      var templateHeaderTabIds = GetDwBlueprintTemplateHeaderTabIds(blueprint);
      var readyTabs = tabRows
        .Where(t => t.ImportStatus != TemplateStatusSkipped)
        .OrderBy(t => t.TabSort ?? short.MaxValue)
        .ThenBy(t => t.TabId)
        .ToList();

      int sortFallback = 0;
      PlmTemplateTabRow firstMainTab = null;

      foreach (var tab in readyTabs)
      {
        sortFallback++;
        string integrationId = ResolveDwTabIntegrationId(blueprint, tab);
        int? transactionId = GetTransactionIdByIntegrationId(conn, null, integrationId);
        if (!transactionId.HasValue)
          continue;

        bool isTemplateHeader = IsDwBlueprintTemplateHeaderTab(blueprint, tab, templateHeaderTabIds);
        if (!isTemplateHeader && firstMainTab == null)
          firstMainTab = tab;

        string navigationName = string.IsNullOrWhiteSpace(tab.TabName) ? $"Tab_{tab.TabId}" : tab.TabName;
        InsertTemplateFormLinkTarget(
          conn,
          searchViewId,
          navigationName,
          (int)EmAppLinkTargetActionType.Edit,
          transactionId.Value,
          sourceViewColumnId,
          tab.TabSort ?? sortFallback,
          BuildTemplateLinkTargetOtherSettingsJson(isTemplateHeader));
      }

      if (firstMainTab != null)
      {
        string createIntegrationId = ResolveDwTabIntegrationId(blueprint, firstMainTab);
        int? createTransactionId = GetTransactionIdByIntegrationId(conn, null, createIntegrationId);
        if (createTransactionId.HasValue)
        {
          InsertTemplateFormLinkTarget(
            conn,
            searchViewId,
            "New",
            (int)EmAppLinkTargetActionType.Create,
            createTransactionId.Value,
            sourceViewColumnId,
            0,
            BuildTemplateLinkTargetOtherSettingsJson(false));
        }
      }
    }

    private static class DwBlueprintExecutionPlanBuilder
    {
      public static List<TemplateTabExecutionPlan> BuildExecutionPlans(
        PlmDwImportBlueprintDto blueprint,
        string prefix,
        List<DwFieldMappingRow> mappingRows)
      {
        var plans = new List<TemplateTabExecutionPlan>();
        var fieldMetaByKey = (blueprint.BlueprintFields ?? new List<PlmDwBlueprintFieldDto>())
          .GroupBy(f => $"{f.AppTableName}|{f.AppColumnName}", StringComparer.OrdinalIgnoreCase)
          .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var gridsByParentTab = (blueprint.GridBindings ?? new List<PlmDwBlueprintGridBindingDto>())
          .Where(g => g.ParentPlmTabId.HasValue)
          .GroupBy(g => g.ParentPlmTabId.Value)
          .ToDictionary(g => g.Key, g => g.ToList());

        var templateHeaderTabIds = GetDwBlueprintTemplateHeaderTabIds(blueprint);

        foreach (var tx in blueprint.Transactions ?? Enumerable.Empty<PlmDwBlueprintTransactionDto>())
        {
          short? tabSort = null;
          if (tx.PlmTabSort.HasValue)
          {
            int sortVal = tx.PlmTabSort.Value;
            tabSort = sortVal > short.MaxValue ? short.MaxValue : (short)sortVal;
          }

          var tab = new PlmTemplateTabRow
          {
            TemplateId = blueprint.PlmTemplate?.TemplateId ?? blueprint.Source?.PlmTemplateId ?? 0,
            TemplateName = blueprint.PlmTemplate?.TemplateName ?? blueprint.TransactionGroup?.Name,
            TabId = tx.PlmTabId,
            TabName = tx.TransactionName ?? tx.PlmTabName ?? $"Tab_{tx.PlmTabId}",
            TabSort = tabSort,
            ImportStatus = string.IsNullOrWhiteSpace(tx.ImportStatus) ? TemplateStatusReady : tx.ImportStatus
          };
          tab.IsTemplateHeaderTab = IsDwBlueprintTemplateHeaderTab(blueprint, tab, templateHeaderTabIds);

          var siblings = tx.UnitStructure?.SiblingUnits?
            .Where(s => s != null && !string.IsNullOrWhiteSpace(s.AppTableName))
            .ToList() ?? new List<PlmDwBlueprintSiblingUnitDto>();
          var childUnits = tx.UnitStructure?.ChildUnits?
            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.AppTableName))
            .ToList() ?? new List<PlmDwBlueprintChildUnitDto>();
          if (siblings.Count == 0 && childUnits.Count == 0)
          {
            plans.Add(new TemplateTabExecutionPlan { Tab = tab });
            continue;
          }

          string primarySiblingTable = siblings.Count > 0
            ? QualifyBlueprintTableName(siblings[0].AppTableName, prefix)
            : null;
          if (!string.IsNullOrWhiteSpace(primarySiblingTable))
            tab.SiblingTableName = primarySiblingTable;

          var plan = new TemplateTabExecutionPlan
          {
            Tab = tab,
            PrimarySiblingTable = primarySiblingTable
          };

          int sortOrder = 0;
          foreach (var sibling in siblings)
          {
            string siblingTable = QualifyBlueprintTableName(sibling.AppTableName, prefix);
            var tableMappings = FilterMappingForTransaction(tx, sibling, mappingRows, prefix);
            foreach (var mapRow in tableMappings.OrderBy(m => m.AppColumnName))
            {
              sortOrder++;
              string metaKey = $"{mapRow.AppTableName}|{mapRow.AppColumnName}";
              fieldMetaByKey.TryGetValue(metaKey, out PlmDwBlueprintFieldDto fieldMeta);

              int plmControlType = fieldMeta?.PlmControlType
                ?? mapRow.PlmControlType
                ?? InferPlmControlType(mapRow);

              plan.SiblingColumnsByTable.TryGetValue(siblingTable, out var subItems);
              if (subItems == null)
              {
                subItems = new List<PlmTemplateSubItemRow>();
                plan.SiblingColumnsByTable[siblingTable] = subItems;
              }

              subItems.Add(new PlmTemplateSubItemRow
              {
                TabId = mapRow.PlmTabId ?? tx.PlmTabId,
                SubItemId = mapRow.PlmSubItemId ?? mapRow.PlmMetaColumnId ?? 0,
                SubItemName = fieldMeta?.DisplayLabel ?? mapRow.AppColumnName,
                ControlType = plmControlType,
                EntityId = fieldMeta?.PlmEntityId ?? mapRow.PlmEntityId ?? TryParseEntityFromFk(mapRow.DwFkTarget),
                SortOrder = fieldMeta?.DisplayOrder ?? sortOrder,
                ColumnName = mapRow.AppColumnName,
                IsVisible = fieldMeta?.IsVisible == true
              });
            }
          }

          // Child-unit tab tables (unitType "child"): 1:many under root, own identity PK,
          // [ReferenceId] is a plain FK. Columns come from the tab wide table (FieldKind TabField).
          foreach (var child in childUnits)
          {
            string childTable = QualifyBlueprintTableName(child.AppTableName, prefix);
            var childMappings = mappingRows
              .Where(m => (string.Equals(QualifyBlueprintTableName(m.AppTableName, prefix), childTable, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(m.AppTableName, child.AppTableName, StringComparison.OrdinalIgnoreCase))
                          && !string.Equals(m.FieldKind, "GridColumn", StringComparison.OrdinalIgnoreCase))
              .ToList();

            foreach (var mapRow in childMappings.OrderBy(m => m.AppColumnName))
            {
              sortOrder++;
              string metaKey = $"{mapRow.AppTableName}|{mapRow.AppColumnName}";
              fieldMetaByKey.TryGetValue(metaKey, out PlmDwBlueprintFieldDto fieldMeta);

              int plmControlType = fieldMeta?.PlmControlType
                ?? mapRow.PlmControlType
                ?? InferPlmControlType(mapRow);

              plan.ChildColumnsByTable.TryGetValue(childTable, out var childCols);
              if (childCols == null)
              {
                childCols = new List<PlmTemplateSubItemRow>();
                plan.ChildColumnsByTable[childTable] = childCols;
              }

              childCols.Add(new PlmTemplateSubItemRow
              {
                TabId = mapRow.PlmTabId ?? tx.PlmTabId,
                SubItemId = mapRow.PlmSubItemId ?? mapRow.PlmMetaColumnId ?? 0,
                SubItemName = fieldMeta?.DisplayLabel ?? mapRow.AppColumnName,
                ControlType = plmControlType,
                EntityId = fieldMeta?.PlmEntityId ?? mapRow.PlmEntityId ?? TryParseEntityFromFk(mapRow.DwFkTarget),
                SortOrder = fieldMeta?.DisplayOrder ?? sortOrder,
                ColumnName = mapRow.AppColumnName,
                IsVisible = fieldMeta?.IsVisible == true
              });
            }
          }

          if (blueprint.RootUnit?.ReferenceScope != null)
          {
            string rootTableName = QualifyBlueprintTableName(blueprint.RootUnit.AppTableName, prefix);
            string refMetaKey = $"{blueprint.RootUnit.AppTableName}|ReferenceCode";
            if (!fieldMetaByKey.ContainsKey(refMetaKey))
              refMetaKey = $"{rootTableName}|ReferenceCode";
            fieldMetaByKey.TryGetValue(refMetaKey, out PlmDwBlueprintFieldDto refFieldMeta);

            plan.RootSubItems.Add(new PlmTemplateSubItemRow
            {
              TabId = blueprint.RootUnit.ReferenceScope.PlmTabId,
              SubItemId = blueprint.RootUnit.ReferenceScope.PlmSubItemId,
              SubItemName = "ReferenceCode",
              ControlType = PlmControlTypeTextBox,
              ColumnName = "ReferenceCode",
              MapsToRoot = true,
              SortOrder = 1,
              IsVisible = refFieldMeta?.IsVisible == true
            });
          }

          if (gridsByParentTab.TryGetValue(tx.PlmTabId, out var gridBindings))
          {
            foreach (var grid in gridBindings)
            {
              string gridTable = QualifyBlueprintTableName(grid.AppTableName, prefix);
              var gridMappings = mappingRows
                .Where(m => string.Equals(QualifyBlueprintTableName(m.AppTableName, prefix), gridTable, StringComparison.OrdinalIgnoreCase)
                  || string.Equals(m.AppTableName, grid.AppTableName, StringComparison.OrdinalIgnoreCase))
                .Where(m => string.Equals(m.FieldKind, "GridColumn", StringComparison.OrdinalIgnoreCase))
                .ToList();

              int gridColOrder = 0;
              foreach (var mapRow in gridMappings.OrderBy(m => m.AppColumnName))
              {
                gridColOrder++;
                string metaKey = $"{mapRow.AppTableName}|{mapRow.AppColumnName}";
                fieldMetaByKey.TryGetValue(metaKey, out PlmDwBlueprintFieldDto fieldMeta);
                int plmControlType = fieldMeta?.PlmControlType
                  ?? mapRow.PlmControlType
                  ?? InferPlmControlType(mapRow);

                tab.GridColumns.Add(new PlmTemplateGridColumnRow
                {
                  TabId = tx.PlmTabId,
                  SubItemId = mapRow.PlmGridSubItemId ?? 0,
                  GridId = mapRow.PlmGridId ?? grid.PlmGridId,
                  GridColumnId = mapRow.PlmMetaColumnId ?? mapRow.PlmSubItemId ?? 0,
                  ColumnName = fieldMeta?.DisplayLabel ?? mapRow.AppColumnName,
                  ColumnTypeId = plmControlType,
                  EntityId = fieldMeta?.PlmEntityId ?? mapRow.PlmEntityId ?? TryParseEntityFromFk(mapRow.DwFkTarget),
                  ColumnOrder = fieldMeta?.DisplayOrder ?? gridColOrder,
                  TableName = gridTable,
                  ColumnSqlName = mapRow.AppColumnName,
                  IsVisible = fieldMeta?.IsVisible == true
                });
              }
            }
          }

          plans.Add(plan);
        }

        AttachOrphanGridTransactions(blueprint, prefix, mappingRows, fieldMetaByKey, plans);
        return plans;
      }

      private static void AttachOrphanGridTransactions(
        PlmDwImportBlueprintDto blueprint,
        string prefix,
        List<DwFieldMappingRow> mappingRows,
        Dictionary<string, PlmDwBlueprintFieldDto> fieldMetaByKey,
        List<TemplateTabExecutionPlan> plans)
      {
        var tabIds = new HashSet<int>(plans.Select(p => p.Tab.TabId));
        foreach (var grid in blueprint.GridBindings ?? Enumerable.Empty<PlmDwBlueprintGridBindingDto>())
        {
          if (grid.ParentPlmTabId.HasValue && tabIds.Contains(grid.ParentPlmTabId.Value))
            continue;

          int pseudoTabId = grid.PlmGridId > 0 ? -grid.PlmGridId : -(plans.Count + 1);
          string txName = grid.TransactionIntegrationId ?? grid.IntegrationId ?? $"Grid_{grid.PlmGridId}";
          var tab = new PlmTemplateTabRow
          {
            TabId = pseudoTabId,
            TabName = txName,
            ImportStatus = TemplateStatusReady,
            SiblingTableName = QualifyBlueprintTableName(grid.AppTableName, prefix)
          };

          var plan = new TemplateTabExecutionPlan { Tab = tab, PrimarySiblingTable = tab.SiblingTableName };
          string gridTable = tab.SiblingTableName;
          var gridMappings = mappingRows
            .Where(m => string.Equals(QualifyBlueprintTableName(m.AppTableName, prefix), gridTable, StringComparison.OrdinalIgnoreCase))
            .ToList();

          int order = 0;
          foreach (var mapRow in gridMappings)
          {
            order++;
            string metaKey = $"{mapRow.AppTableName}|{mapRow.AppColumnName}";
            fieldMetaByKey.TryGetValue(metaKey, out PlmDwBlueprintFieldDto fieldMeta);
            plan.SiblingColumnsByTable.TryGetValue(gridTable, out var subItems);
            if (subItems == null)
            {
              subItems = new List<PlmTemplateSubItemRow>();
              plan.SiblingColumnsByTable[gridTable] = subItems;
            }

            subItems.Add(new PlmTemplateSubItemRow
            {
              TabId = pseudoTabId,
              SubItemId = mapRow.PlmSubItemId ?? mapRow.PlmMetaColumnId ?? 0,
              SubItemName = fieldMeta?.DisplayLabel ?? mapRow.AppColumnName,
              ControlType = fieldMeta?.PlmControlType ?? mapRow.PlmControlType ?? InferPlmControlType(mapRow),
              EntityId = fieldMeta?.PlmEntityId ?? mapRow.PlmEntityId ?? TryParseEntityFromFk(mapRow.DwFkTarget),
              SortOrder = fieldMeta?.DisplayOrder ?? order,
              ColumnName = mapRow.AppColumnName,
              IsVisible = fieldMeta == null || fieldMeta.IsVisible
            });
          }

          plans.Add(plan);
        }
      }

      private static int InferPlmControlType(DwFieldMappingRow row)
      {
        if (!string.IsNullOrWhiteSpace(row.DwFkTarget))
          return PlmControlTypeDdl;
        if (row.DwDataType != null && row.DwDataType.IndexOf("date", StringComparison.OrdinalIgnoreCase) >= 0)
          return 7;
        return PlmControlTypeTextBox;
      }

      private static int? TryParseEntityFromFk(string dwFkTarget)
      {
        if (string.IsNullOrWhiteSpace(dwFkTarget))
          return null;
        if (int.TryParse(dwFkTarget, out int entityId))
          return entityId;
        return null;
      }
    }
  }
}
