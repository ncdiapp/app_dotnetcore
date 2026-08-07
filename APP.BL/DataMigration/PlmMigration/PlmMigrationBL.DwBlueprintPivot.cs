using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private static void ApplyBomColorwayPivotBindingsSql(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            int plmTabId,
            IReadOnlyList<PlmDwBlueprintBomColorwayPivotBindingDto> bindings)
        {
            if (bindings == null || bindings.Count == 0)
                return;

            foreach (var binding in bindings.Where(b => b != null && b.PlmTabId == plmTabId))
            {
                if (string.IsNullOrWhiteSpace(binding.HostAppTableName)
                    || string.IsNullOrWhiteSpace(binding.GrandchildAppTableName)
                    || string.IsNullOrWhiteSpace(binding.SourceAppTableName))
                    continue;

                int? hostUnitId = GetTransactionUnitIdByTableName(conn, tran, transactionId, binding.HostAppTableName);
                if (!hostUnitId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: host unit not found for table {binding.HostAppTableName} on transaction {transactionId}.");

                int? sourceUnitId = GetTransactionUnitIdByTableName(conn, tran, transactionId, binding.SourceAppTableName);
                if (!sourceUnitId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: source unit not found for table {binding.SourceAppTableName} on transaction {transactionId}.");

                string pivotKeyColumn = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn)
                    ? "Color"
                    : binding.SourcePivotKeyColumn.Trim();

                int? sourcePivotFieldId = GetTransactionFieldId(conn, tran, sourceUnitId.Value, pivotKeyColumn);
                if (!sourcePivotFieldId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: source field {binding.SourceAppTableName}.{pivotKeyColumn} not found.");

                int? grandchildUnitId = GetChildTransactionUnitIdByTableName(
                    conn, tran, transactionId, hostUnitId.Value, binding.GrandchildAppTableName);
                if (!grandchildUnitId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: grandchild unit not found for table {binding.GrandchildAppTableName} under host unit {hostUnitId}.");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET EmGridViewDisplayType = @DisplayType
WHERE TransactionUnitID = @UnitId";
                    cmd.Parameters.AddWithValue("@DisplayType", (int)EmAppTransactionGridDisplayType.ChildUnitPivotColumns);
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.ExecuteNonQuery();
                }

                string colorwayField = binding.GrandchildColumns?.ColorwayKey ?? "Colorway";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsPivotColumn = 1,
    MatrixForeignKeyFieldId = @SourceFieldId,
    IsVisible = 1
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@SourceFieldId", sourcePivotFieldId.Value);
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.Parameters.AddWithValue("@FieldName", colorwayField);
                    cmd.ExecuteNonQuery();
                }

                if (binding.GrandchildColumns?.ValueFields != null)
                {
                    foreach (var vf in binding.GrandchildColumns.ValueFields.Where(v => v != null && !string.IsNullOrWhiteSpace(v.Column)))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsPivotValue = @IsPivotValue,
    IsVisible = 1
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                            cmd.Parameters.AddWithValue("@IsPivotValue", vf.IsPivotValue);
                            cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                            cmd.Parameters.AddWithValue("@FieldName", vf.Column.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                string parentLink = binding.GrandchildColumns?.ParentLink ?? "ParentRowId";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsVisible = 0
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.Parameters.AddWithValue("@FieldName", parentLink);
                    cmd.ExecuteNonQuery();
                }

                DeleteHostStagingPivotFields(conn, tran, hostUnitId.Value, binding);
            }
        }

        private static void DeleteHostStagingPivotFields(
            SqlConnection conn,
            SqlTransaction tran,
            int hostUnitId,
            PlmDwBlueprintBomColorwayPivotBindingDto binding)
        {
            var fieldNames = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT DataBaseFieldName
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId";
                cmd.Parameters.AddWithValue("@UnitId", hostUnitId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            fieldNames.Add(reader.GetString(0));
                    }
                }
            }

            foreach (string fieldName in fieldNames)
            {
                if (!IsBomColorwayStagingHostColumn(fieldName, binding))
                    continue;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
DELETE FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@UnitId", hostUnitId);
                    cmd.Parameters.AddWithValue("@FieldName", fieldName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool IsBomColorwayStagingHostColumn(
            string fieldName,
            PlmDwBlueprintBomColorwayPivotBindingDto binding)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            if (Regex.IsMatch(fieldName, @"^Colorway_\d+$", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(fieldName, @"^Image\d+$", RegexOptions.IgnoreCase))
                return true;

            if (binding.StagingHostColumnPatterns != null)
            {
                foreach (string pattern in binding.StagingHostColumnPatterns.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    string regex = "^" + Regex.Escape(pattern).Replace("%", ".*") + "$";
                    if (Regex.IsMatch(fieldName, regex, RegexOptions.IgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static int? GetChildTransactionUnitIdByTableName(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            int parentUnitId,
            string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TransactionId
  AND ParentTransactionUnitID = @ParentUnitId
  AND DataBaseTableName = @TableName";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Parameters.AddWithValue("@ParentUnitId", parentUnitId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetTransactionFieldId(
            SqlConnection conn,
            SqlTransaction tran,
            int unitId,
            string fieldName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 TransactionFieldID
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.Parameters.AddWithValue("@FieldName", fieldName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void AttachBomColorwayHierarchyChildren(
            List<HierarchyChildTableDto> rootChildTables,
            IReadOnlyList<PlmDwBlueprintBomColorwayPivotBindingDto> tabBindings)
        {
            if (tabBindings == null || tabBindings.Count == 0)
                return;

            foreach (var binding in tabBindings)
            {
                if (string.IsNullOrWhiteSpace(binding.HostAppTableName)
                    || string.IsNullOrWhiteSpace(binding.GrandchildAppTableName)
                    || string.IsNullOrWhiteSpace(binding.SourceAppTableName))
                    continue;

                if (!rootChildTables.Any(c => string.Equals(c.TableName, binding.SourceAppTableName, StringComparison.OrdinalIgnoreCase)))
                {
                    rootChildTables.Add(new HierarchyChildTableDto { TableName = binding.SourceAppTableName });
                }

                var hostChild = rootChildTables.FirstOrDefault(c =>
                    string.Equals(c.TableName, binding.HostAppTableName, StringComparison.OrdinalIgnoreCase));
                if (hostChild == null)
                {
                    hostChild = new HierarchyChildTableDto
                    {
                        TableName = binding.HostAppTableName,
                        GrandChildTableNames = new List<string>()
                    };
                    rootChildTables.Add(hostChild);
                }

                if (hostChild.GrandChildTableNames == null)
                    hostChild.GrandChildTableNames = new List<string>();

                if (!hostChild.GrandChildTableNames.Any(g =>
                    string.Equals(g, binding.GrandchildAppTableName, StringComparison.OrdinalIgnoreCase)))
                {
                    hostChild.GrandChildTableNames.Add(binding.GrandchildAppTableName);
                }
            }
        }

        /// <summary>
        /// TechPack Grading: TchpGradeValue → ChildUnitPivotColumns; column domain = View_TchpStyleActiveSizeRunSizes.
        /// MatrixKey → View.IsVisible (DimensionCode filter), matching Style Spec 2298.
        /// </summary>
        private static void ApplyTechPackGradeValuePivotBindingsSql(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            int plmTabId,
            TemplateTabExecutionPlan plan,
            string tablePrefix)
        {
            var bindings = plan?.TechPackGradeValuePivotBindings?
                .Where(b => b != null && b.PlmTabId == plmTabId)
                .ToList() ?? new List<PlmDwBlueprintTechPackGradeValuePivotDto>();

            // Fallback: derive from ChildUnitDefs when blueprint omitted explicit bindings.
            if (bindings.Count == 0 && plan?.ChildUnitDefs != null)
            {
                bool hasView = plan.ChildUnitDefs.Any(c =>
                    c?.AppTableName != null
                    && c.AppTableName.IndexOf("View_TchpStyleActiveSizeRunSizes", StringComparison.OrdinalIgnoreCase) >= 0);
                var pomLine = plan.ChildUnitDefs.FirstOrDefault(c =>
                    c?.AppTableName != null
                    && c.AppTableName.IndexOf("TchpPomSpecLine", StringComparison.OrdinalIgnoreCase) >= 0
                    && c.GrandChildAppTableNames != null
                    && c.GrandChildAppTableNames.Any(g =>
                        g != null && g.IndexOf("TchpGradeValue", StringComparison.OrdinalIgnoreCase) >= 0));
                if (hasView && pomLine != null)
                {
                    bindings.Add(new PlmDwBlueprintTechPackGradeValuePivotDto
                    {
                        PlmTabId = plmTabId,
                        HostAppTableName = "TchpPomSpecLine",
                        GrandchildAppTableName = "TchpGradeValue",
                        SourceAppTableName = "View_TchpStyleActiveSizeRunSizes",
                        SourcePivotKeyColumn = "SizeRunSizeId",
                        PivotColumnField = "SizeRunSizeId",
                        PivotValueField = "GradingDelta",
                        SkipMatrixKeyVisibleFilter = false
                    });
                }
            }

            foreach (var binding in bindings)
            {
                string hostTable = QualifyBlueprintTableName(
                    binding.HostAppTableName ?? "TchpPomSpecLine", tablePrefix, skipTablePrefix: true);
                string gcTable = QualifyBlueprintTableName(
                    binding.GrandchildAppTableName ?? "TchpGradeValue", tablePrefix, skipTablePrefix: true);
                string sourceTable = QualifyBlueprintTableName(
                    binding.SourceAppTableName ?? "View_TchpStyleActiveSizeRunSizes", tablePrefix, skipTablePrefix: true);

                int? hostUnitId = GetAnyTransactionUnitIdByTableName(conn, tran, transactionId, hostTable);
                int? sourceUnitId = GetAnyTransactionUnitIdByTableName(conn, tran, transactionId, sourceTable);
                if (!hostUnitId.HasValue || !sourceUnitId.HasValue)
                    continue;

                int? grandchildUnitId = GetChildTransactionUnitIdByTableName(
                    conn, tran, transactionId, hostUnitId.Value, gcTable);
                if (!grandchildUnitId.HasValue)
                    grandchildUnitId = GetAnyTransactionUnitIdByTableName(conn, tran, transactionId, gcTable);
                if (!grandchildUnitId.HasValue)
                    continue;

                string sourceKeyCol = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn)
                    ? "SizeRunSizeId"
                    : binding.SourcePivotKeyColumn.Trim();
                string pivotColField = string.IsNullOrWhiteSpace(binding.PivotColumnField)
                    ? "SizeRunSizeId"
                    : binding.PivotColumnField.Trim();
                string pivotValField = string.IsNullOrWhiteSpace(binding.PivotValueField)
                    ? "GradingDelta"
                    : binding.PivotValueField.Trim();

                int? sourcePivotFieldId = GetTransactionFieldId(conn, tran, sourceUnitId.Value, sourceKeyCol);
                if (!sourcePivotFieldId.HasValue)
                    continue;

                // Dimension filter column (view CASE); required for MatrixKey like Style Spec 2298.
                int? matrixKeyFieldId = EnsureViewIsVisibleTransactionField(conn, tran, sourceUnitId.Value);
                if (binding.SkipMatrixKeyVisibleFilter)
                    matrixKeyFieldId = null;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET EmGridViewDisplayType = @DisplayType,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                    cmd.Parameters.AddWithValue("@DisplayType", (int)EmAppTransactionGridDisplayType.ChildUnitPivotColumns);
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.ExecuteNonQuery();
                }

                int? sizeRunDetailEntityId = ResolveAppEntityInfoIdByCode(conn, tran, "SizeRunDetail");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    IsPivotColumn = 1,
    IsPivotValue = 0,
    MatrixForeignKeyFieldId = @SourceFieldId,
    MatrixKeyTransactionFieldId = @MatrixKeyFieldId,
    ControlType = @Ddl,
    EntityId = COALESCE(@EntityId, EntityId),
    DisplayWidth = N'150',
    IsVisible = 1,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@SourceFieldId", sourcePivotFieldId.Value);
                    cmd.Parameters.AddWithValue("@MatrixKeyFieldId", (object)matrixKeyFieldId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Ddl", (int)EmAppControlType.DDL);
                    cmd.Parameters.AddWithValue("@EntityId", (object)sizeRunDetailEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.Parameters.AddWithValue("@FieldName", pivotColField);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    IsPivotValue = 1,
    IsPivotColumn = 0,
    DisplayWidth = N'150',
    IsVisible = 1,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.Parameters.AddWithValue("@FieldName", pivotValField);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Ensure View_TchpStyleActiveSizeRunSizes.IsVisible exists as AppTransactionField (hidden filter key).
        /// </summary>
        private static int? EnsureViewIsVisibleTransactionField(
            SqlConnection conn,
            SqlTransaction tran,
            int sourceUnitId)
        {
            int? existing = GetTransactionFieldId(conn, tran, sourceUnitId, "IsVisible");
            if (existing.HasValue)
                return existing;

            int sortOrder = 80;
            using (var max = conn.CreateCommand())
            {
                max.Transaction = tran;
                max.CommandText = @"
SELECT ISNULL(MAX(SortOrder), 0) + 10
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId";
                max.Parameters.AddWithValue("@UnitId", sourceUnitId);
                var val = max.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                    sortOrder = Convert.ToInt32(val);
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
INSERT INTO dbo.AppTransactionField (
    TransactionUnitID, DisplayName, DataBaseFieldName, ControlType, DataType,
    SortOrder, IsPrimaryKey, IsVisible, IsReadonly, IsAllowEmpty,
    DisplayWidth, NBDecimal, IsLinkToParentPrimaryKey, RowIdentityGuid,
    AppCreatedDate, AppModifiedDate)
VALUES (
    @UnitId, N'Is Visible', N'IsVisible', @ControlType, @DataType,
    @SortOrder, 0, 0, 1, 1,
    N'150', 0, 0, NEWID(),
    GETDATE(), GETDATE());";
                cmd.Parameters.AddWithValue("@UnitId", sourceUnitId);
                cmd.Parameters.AddWithValue("@ControlType", (int)EmAppControlType.CheckBox);
                cmd.Parameters.AddWithValue("@DataType", 2);
                cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                cmd.ExecuteNonQuery();
            }

            return GetTransactionFieldId(conn, tran, sourceUnitId, "IsVisible");
        }

        /// <summary>
        /// Grading golden field template (locked from Transaction 2303 hand-tune):
        /// Pom Spec Line widths/sort/entities; StyleSpec SizeRun/BaseSize cascade; UOM stays TextBox.
        /// </summary>
        private static void ApplyTechPackGradingGoldenFieldTemplate(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            TemplateTabExecutionPlan plan,
            string tablePrefix)
        {
            if (plan?.ChildUnitDefs == null
                || !plan.ChildUnitDefs.Any(c =>
                    c?.AppTableName != null
                    && c.AppTableName.IndexOf("TchpPomSpecLine", StringComparison.OrdinalIgnoreCase) >= 0))
                return;

            string pomTable = QualifyBlueprintTableName("TchpPomSpecLine", tablePrefix, skipTablePrefix: true);
            int? pomUnitId = GetAnyTransactionUnitIdByTableName(conn, tran, transactionId, pomTable);
            if (!pomUnitId.HasValue)
                return;

            int? bodyPartEntityId = ResolveAppEntityInfoIdByCode(conn, tran, "TchpBodyPart");
            int? gradeRuleSetEntityId = ResolveAppEntityInfoIdByCode(conn, tran, "TchpGradeRuleSet");

            // Sort — visible row order
            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "Sort",
                controlType: null, entityId: null, width: "100", sortOrder: 25, isVisible: true, groupByLevel: 1);

            // BodyPartId — DDL TchpBodyPart, width 200
            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "BodyPartId",
                controlType: (int)EmAppControlType.DDL, entityId: bodyPartEntityId, width: "200", sortOrder: 30, isVisible: true, groupByLevel: null);

            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "BodypartAliasName",
                controlType: null, entityId: null, width: "200", sortOrder: 35, isVisible: true, groupByLevel: null);

            // GradeRuleSetId — DDL TchpGradeRuleSet (locked 4 yes); IsFixed stays TextBox (locked 4 no)
            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "GradeRuleSetId",
                controlType: (int)EmAppControlType.DDL, entityId: gradeRuleSetEntityId, width: "150", sortOrder: 40, isVisible: true, groupByLevel: null);

            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "BaseValue",
                controlType: null, entityId: null, width: "150", sortOrder: 50, isVisible: true, groupByLevel: null);
            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "Tolerance",
                controlType: null, entityId: null, width: "150", sortOrder: 60, isVisible: true, groupByLevel: null);
            UpdateTechPackFieldMeta(conn, tran, pomUnitId.Value, "IsFixed",
                controlType: (int)EmAppControlType.TextBox, entityId: null, width: "150", sortOrder: 70, isVisible: true, groupByLevel: null);

            // StyleSpec: SizeRun / BaseSize cascade; UnitOfMeasure TextBox + Entity (locked 4 TEXTBOX)
            string styleSpecTable = QualifyBlueprintTableName("TchpStyleSpec", tablePrefix, skipTablePrefix: true);
            int? styleSpecUnitId = GetAnyTransactionUnitIdByTableName(conn, tran, transactionId, styleSpecTable);
            if (!styleSpecUnitId.HasValue)
                return;

            int? sizeRunEntityId = ResolveAppEntityInfoIdByCode(conn, tran, "SizeRun");
            int? sizeRunDetailEntityId = ResolveAppEntityInfoIdByCode(conn, tran, "SizeRunDetail");
            int? uomEntityId = ResolveAppEntityInfoIdByCode(conn, tran, "UnitOfMeasure");
            int? sizeRunFieldId = ResolveTransactionFieldIdSql(conn, tran, styleSpecUnitId.Value, "SizeRunId");

            UpdateTechPackFieldMeta(conn, tran, styleSpecUnitId.Value, "SizeRunId",
                controlType: (int)EmAppControlType.DDL, entityId: sizeRunEntityId, width: "100", sortOrder: 20, isVisible: true, groupByLevel: null);

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    ControlType = @Ddl,
    EntityId = COALESCE(@EntityId, EntityId),
    DDLParentLevelID = @ParentFieldId,
    DisplayWidth = N'100',
    SortOrder = 30,
    IsVisible = 1,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = N'BaseSizeDetailId'";
                cmd.Parameters.AddWithValue("@Ddl", (int)EmAppControlType.DDL);
                cmd.Parameters.AddWithValue("@EntityId", (object)sizeRunDetailEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ParentFieldId",
                    sizeRunFieldId.HasValue ? (object)sizeRunFieldId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitId", styleSpecUnitId.Value);
                cmd.ExecuteNonQuery();
            }

            // UnitOfMeasure: keep TextBox; attach Entity for label if present
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    ControlType = @TextBox,
    EntityId = COALESCE(@EntityId, EntityId),
    DisplayWidth = N'100',
    SortOrder = 40,
    IsVisible = 1,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = N'UnitOfMeasure'";
                cmd.Parameters.AddWithValue("@TextBox", (int)EmAppControlType.TextBox);
                cmd.Parameters.AddWithValue("@EntityId", (object)uomEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitId", styleSpecUnitId.Value);
                cmd.ExecuteNonQuery();
            }

            // VisibleSizes: MultiSelectDDL SizeRunDetail; Depend On SizeRunId; pipe-delimited SizeRunSizeId
            EnsureTechPackStyleSpecVisibleSizesField(conn, tran, styleSpecUnitId.Value, sizeRunDetailEntityId, sizeRunFieldId);
        }

        /// <summary>
        /// Ensure TchpStyleSpec.VisibleSizes exists as MultiSelectDDL (Entity SizeRunDetail, cascade from SizeRunId).
        /// Collapses duplicate AppTransactionField rows for the same DbName (keep lowest Id).
        /// </summary>
        private static void EnsureTechPackStyleSpecVisibleSizesField(
            SqlConnection conn,
            SqlTransaction tran,
            int styleSpecUnitId,
            int? sizeRunDetailEntityId,
            int? sizeRunFieldId)
        {
            // If Add-Existing + G1 both created rows, remove extras before meta update.
            using (var del = conn.CreateCommand())
            {
                del.Transaction = tran;
                del.CommandText = @"
;WITH d AS (
    SELECT TransactionFieldID,
           ROW_NUMBER() OVER (ORDER BY TransactionFieldID) AS rn
    FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @UnitId
      AND DataBaseFieldName = N'VisibleSizes'
)
DELETE FROM dbo.AppTransactionField
WHERE TransactionFieldID IN (SELECT TransactionFieldID FROM d WHERE rn > 1);";
                del.Parameters.AddWithValue("@UnitId", styleSpecUnitId);
                del.ExecuteNonQuery();
            }

            int? existing = GetTransactionFieldId(conn, tran, styleSpecUnitId, "VisibleSizes");
            if (!existing.HasValue)
            {
                int sortOrder = 45;
                using (var max = conn.CreateCommand())
                {
                    max.Transaction = tran;
                    max.CommandText = @"
SELECT ISNULL(MAX(SortOrder), 0) + 5
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId";
                    max.Parameters.AddWithValue("@UnitId", styleSpecUnitId);
                    var val = max.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        sortOrder = Convert.ToInt32(val);
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
INSERT INTO dbo.AppTransactionField (
    TransactionUnitID, DisplayName, DataBaseFieldName, ControlType, DataType,
    EntityId, DDLParentLevelID, SortOrder, IsPrimaryKey, IsVisible, IsReadonly, IsAllowEmpty,
    DisplayWidth, NBDecimal, IsLinkToParentPrimaryKey, RowIdentityGuid,
    DataRetrieveType, CascadingRelationTable, CascadingRelationTableSchemaOwner,
    CascadingRelationTableParentKeyField, CascadingRelationTableChildKeyField,
    AppCreatedDate, AppModifiedDate)
SELECT
    @UnitId, N'Visible Sizes', N'VisibleSizes', @ControlType, @DataType,
    @EntityId, @ParentFieldId, @SortOrder, 0, 1, 0, 1,
    N'200', 0, 0, NEWID(),
    base.DataRetrieveType, base.CascadingRelationTable, base.CascadingRelationTableSchemaOwner,
    base.CascadingRelationTableParentKeyField, base.CascadingRelationTableChildKeyField,
    GETDATE(), GETDATE()
FROM (SELECT 1 AS _) AS dummy
OUTER APPLY (
    SELECT TOP 1
        DataRetrieveType, CascadingRelationTable, CascadingRelationTableSchemaOwner,
        CascadingRelationTableParentKeyField, CascadingRelationTableChildKeyField
    FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = N'BaseSizeDetailId'
) AS base;";
                    cmd.Parameters.AddWithValue("@UnitId", styleSpecUnitId);
                    cmd.Parameters.AddWithValue("@ControlType", (int)EmAppControlType.MultiSelectDDL);
                    cmd.Parameters.AddWithValue("@DataType", (int)EmAppDataType.String);
                    cmd.Parameters.AddWithValue("@EntityId", (object)sizeRunDetailEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ParentFieldId",
                        sizeRunFieldId.HasValue ? (object)sizeRunFieldId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
UPDATE vs SET
    ControlType = @ControlType,
    DataType = @DataType,
    EntityId = COALESCE(@EntityId, vs.EntityId),
    DDLParentLevelID = @ParentFieldId,
    DisplayWidth = N'200',
    SortOrder = 45,
    IsVisible = 1,
    IsReadonly = 0,
    IsAllowEmpty = 1,
    DataRetrieveType = COALESCE(vs.DataRetrieveType, base.DataRetrieveType),
    CascadingRelationTable = COALESCE(vs.CascadingRelationTable, base.CascadingRelationTable),
    CascadingRelationTableSchemaOwner = COALESCE(vs.CascadingRelationTableSchemaOwner, base.CascadingRelationTableSchemaOwner),
    CascadingRelationTableParentKeyField = COALESCE(vs.CascadingRelationTableParentKeyField, base.CascadingRelationTableParentKeyField),
    CascadingRelationTableChildKeyField = COALESCE(vs.CascadingRelationTableChildKeyField, base.CascadingRelationTableChildKeyField),
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField AS vs
OUTER APPLY (
    SELECT TOP 1
        DataRetrieveType, CascadingRelationTable, CascadingRelationTableSchemaOwner,
        CascadingRelationTableParentKeyField, CascadingRelationTableChildKeyField
    FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = N'BaseSizeDetailId'
) AS base
WHERE vs.TransactionUnitID = @UnitId AND vs.DataBaseFieldName = N'VisibleSizes'";
                cmd.Parameters.AddWithValue("@ControlType", (int)EmAppControlType.MultiSelectDDL);
                cmd.Parameters.AddWithValue("@DataType", (int)EmAppDataType.String);
                cmd.Parameters.AddWithValue("@EntityId", (object)sizeRunDetailEntityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ParentFieldId",
                    sizeRunFieldId.HasValue ? (object)sizeRunFieldId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitId", styleSpecUnitId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateTechPackFieldMeta(
            SqlConnection conn,
            SqlTransaction tran,
            int unitId,
            string databaseFieldName,
            int? controlType,
            int? entityId,
            string width,
            int sortOrder,
            bool isVisible,
            int? groupByLevel)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    ControlType = COALESCE(@ControlType, ControlType),
    EntityId = COALESCE(@EntityId, EntityId),
    DisplayWidth = COALESCE(@Width, DisplayWidth),
    SortOrder = @SortOrder,
    IsVisible = @IsVisible,
    GroupByLevel = CASE WHEN @HasGroupBy = 1 THEN @GroupByLevel ELSE GroupByLevel END,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId AND DataBaseFieldName = @FieldName";
                cmd.Parameters.AddWithValue("@ControlType", (object)controlType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EntityId", (object)entityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Width", (object)width ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SortOrder", sortOrder);
                cmd.Parameters.AddWithValue("@IsVisible", isVisible ? 1 : 0);
                cmd.Parameters.AddWithValue("@HasGroupBy", groupByLevel.HasValue ? 1 : 0);
                cmd.Parameters.AddWithValue("@GroupByLevel", (object)groupByLevel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.Parameters.AddWithValue("@FieldName", databaseFieldName);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
