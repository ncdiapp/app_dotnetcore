using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL;
using App.BL.AppBuilderAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Shared formula + form-layout helpers for FormConfigPlugin and TransactionModifierPlugin.
    /// </summary>
    public static class FormConfigFormulaLayoutService
    {
        static readonly Regex BracketToken = new Regex(@"\[[^\]]+\]", RegexOptions.Compiled);
        static readonly Regex QuotedString = new Regex(@"""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", RegexOptions.Compiled);
        static readonly HashSet<string> FormulaKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "true", "false", "null", "and", "or", "not", "if", "else", "sum", "avg", "min", "max", "count",
            "today", "now", "days", "hours", "minutes", "months", "seconds", "weeks", "years"
        };

        // ── Formulas ──────────────────────────────────────────────────────────

        public static string GetUnitFormulas(int transactionId, int? unitId, string unitName)
        {
            try
            {
                var tx = LoadTxn(transactionId, out var err);
                if (tx == null) return Fail(err);
                if (!TryResolveUnit(tx, unitId, unitName, out var unit, out err))
                    return Fail(err);

                AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(
                    tx, tx.DictAllTransactionField?.Values.ToList() ?? new List<AppTransactionFieldExDto>());

                // Formula Editor shows RootMasterUnit formulas only. Sibling-unit formula rows
                // are also "root level" — include them when listing the true root unit.
                var formulaSourceUnits = FormulaListSourceUnits(tx, unit);
                var formulas = new List<object>();
                foreach (var srcUnit in formulaSourceUnits)
                {
                    var set = AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto(
                        Convert.ToInt32(srcUnit.Id), transactionId);
                    foreach (var f in set?.ListAppTransactionUnitFormula ?? Enumerable.Empty<AppTransactionUnitFormulaDto>())
                    {
                        if (f.ChildTransactionUnitId.HasValue && string.IsNullOrWhiteSpace(f.FormulaExpression))
                            continue; // grid placeholder
                        if (f.OperationType == (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                            || f.OperationType == (int)EmAppFormularType.SubscribeFromParentLevelField)
                            continue; // reconstructed below
                        formulas.Add(SummarizeFormula(f, Convert.ToInt32(srcUnit.Id)));
                    }
                }

                // Reconstruct Aggregate / Subscribe from field cross-relation settings (UI parity).
                foreach (var field in unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                {
                    var fieldId = Convert.ToInt32(field.Id);
                    var left = field.FormulaDisplayName
                        ?? $"[{field.DataBaseFieldName}_{fieldId}]";

                    if (field.ParentUnitSubscribeChildAggFunctionId.HasValue)
                    {
                        AppTransactionFieldCrossRelationSettingDto cross = null;
                        tx.DictTransFieldIdAndCrossRelationSettingDto?.TryGetValue(fieldId, out cross);
                        var srcId = cross?.SubscribeToTransFieldId;
                        var aggType = cross?.AggregationType;
                        string right = "?";
                        AppTransactionFieldExDto src = null;
                        if (srcId.HasValue && tx.DictAllTransactionField != null)
                            tx.DictAllTransactionField.TryGetValue(srcId.Value, out src);
                        if (src != null)
                            right = $"{AggLabel(aggType)}({src.FormulaDisplayName ?? $"[{src.DataBaseFieldName}_{src.Id}]"})";
                        else
                            right = $"{AggLabel(aggType)}(?)";

                        formulas.Add(new
                        {
                            formulaId = (int?)null,
                            operationType = (int)EmAppFormularType.SubscribeFromGridColumnAggregation,
                            operationName = "Aggregate",
                            assignToFieldId = fieldId,
                            assignTo = left,
                            formulaExpression = $"{left} = {right}",
                            aggregationType = aggType,
                            sourceFieldId = srcId,
                            aggFunctionId = field.ParentUnitSubscribeChildAggFunctionId,
                            sort = "After Grid Calculation"
                        });
                    }
                    else if (field.ChildUnitSubscribeParentFieldId.HasValue)
                    {
                        var parentId = field.ChildUnitSubscribeParentFieldId.Value;
                        string right = $"[Field_{parentId}]";
                        AppTransactionFieldExDto parent = null;
                        if (tx.DictAllTransactionField != null)
                            tx.DictAllTransactionField.TryGetValue(parentId, out parent);
                        if (parent != null)
                            right = parent.FormulaDisplayName ?? $"[{parent.DataBaseFieldName}_{parent.Id}]";

                        formulas.Add(new
                        {
                            formulaId = (int?)null,
                            operationType = (int)EmAppFormularType.SubscribeFromParentLevelField,
                            operationName = "Subscribe",
                            assignToFieldId = fieldId,
                            assignTo = left,
                            formulaExpression = $"{left} = {right}",
                            sourceFieldId = parentId,
                            sort = "Before Grid Calculation"
                        });
                    }
                }

                return Ok(new
                {
                    ok = true,
                    transactionId,
                    unitId = unit.Id,
                    unitName = unit.UnitDisplayName,
                    formulas,
                    operationTypes = new
                    {
                        Assignment = 1,
                        Warning = 2,
                        Error = 3,
                        Aggregate = 4,
                        Subscribe = 5
                    },
                    note = "defaultValue is NOT a Calculation Flow formula. Use update_unit_formula for Assignment/Warning/Error/Aggregate/Subscribe."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        /// <summary>
        /// action=add|update|delete. For Assignment/Warning/Error pass expression (display names OK).
        /// Requires confirmed=true to write. Ambiguous field names return needsDisambiguation.
        /// Aggregate: assignToFieldId + sourceFieldId + aggregationType (1=SUM..5=COUNT).
        /// Subscribe: assignToFieldId (child) + sourceFieldId (parent).
        /// </summary>
        public static string UpdateUnitFormula(
            int transactionId,
            string action,
            int? unitId,
            string unitName,
            int? formulaId,
            int? operationType,
            string expression,
            int? assignToFieldId,
            string assignToFieldName,
            int? sourceFieldId,
            string sourceFieldName,
            int? aggregationType,
            string warningMessage,
            int? warningHighlightFieldId,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                action = (action ?? "").Trim().ToLowerInvariant();
                if (action != "add" && action != "update" && action != "delete")
                    return Fail("action must be add, update, or delete.");

                var tx = LoadTxn(transactionId, out var err);
                if (tx == null) return Fail(err);
                if (!TryResolveUnit(tx, unitId, unitName, out var unit, out err))
                    return Fail(err);

                AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(
                    tx, tx.DictAllTransactionField?.Values.ToList() ?? new List<AppTransactionFieldExDto>());

                // Root-level Assignments (root + master-sibling fields) must live on True RootMasterUnit —
                // Formula Editor only renders that unit's formula grid. Remap before write.
                unit = ResolveFormulaOwnerUnit(tx, unit, assignToFieldId, assignToFieldName);

                var op = operationType ?? (int)EmAppFormularType.Assignment;
                if (action == "delete")
                    return DeleteFormula(transactionId, unit, formulaId, assignToFieldId, op, confirmed, dryRun);

                if (op == (int)EmAppFormularType.SubscribeFromGridColumnAggregation)
                    return UpsertAggregate(transactionId, unit, assignToFieldId, assignToFieldName,
                        sourceFieldId, sourceFieldName, aggregationType, confirmed, dryRun, action);

                if (op == (int)EmAppFormularType.SubscribeFromParentLevelField)
                    return UpsertSubscribe(transactionId, unit, assignToFieldId, assignToFieldName,
                        sourceFieldId, sourceFieldName, confirmed, dryRun, action);

                return UpsertExpressionFormula(transactionId, unit, tx, action, formulaId, op,
                    expression, assignToFieldId, assignToFieldName, warningMessage, warningHighlightFieldId,
                    confirmed, dryRun);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static string UpsertExpressionFormula(
            int transactionId, AppTransactionUnitExDto unit, AppTransactionExDto tx,
            string action, int? formulaId, int op, string expression,
            int? assignToFieldId, string assignToFieldName,
            string warningMessage, int? warningHighlightFieldId,
            bool? confirmed, bool? dryRun)
        {
            if (string.IsNullOrWhiteSpace(expression) && action != "delete")
                return Fail("expression is required for Assignment/Warning/Error.");

            var fields = tx.DictAllTransactionField?.Values.ToList() ?? new List<AppTransactionFieldExDto>();
            var resolve = ResolveExpression(expression, fields, assignToFieldId, assignToFieldName, op);
            if (!resolve.Ok) return resolve.ErrorJson;

            // Remap again after expression resolve (assignTo may only appear in expression).
            unit = ResolveFormulaOwnerUnit(tx, unit, resolve.AssignToFieldId, assignToFieldName);
            var root = TrueRootMasterUnit(tx);

            var preview = new
            {
                ok = true,
                needsConfirmation = !(confirmed == true),
                dryRun = dryRun == true,
                action,
                operationType = op,
                operationName = OpName(op),
                unitId = unit.Id,
                formulaOwnerUnitId = unit.Id,
                rootMasterUnitId = root?.Id,
                note = "Root-level formulas (including master-sibling fields) are stored on RootMasterUnit — not null UnitId, and not the sibling unit id.",
                assignToFieldId = resolve.AssignToFieldId,
                assignToToken = resolve.AssignToToken,
                resolvedExpression = resolve.ResolvedExpression,
                fieldMap = resolve.FieldMap,
                warningMessage,
                message = confirmed == true
                    ? null
                    : "Resolved formula ready. ask_user Yes/No, then call again with confirmed=true (and same resolvedExpression / fieldIds)."
            };

            if (confirmed != true || dryRun == true)
                return Ok(preview);

            // If a prior bad write put the same Assignment on a sibling unit, remove it there first.
            if (resolve.AssignToFieldId.HasValue && root != null)
                RemoveDuplicateRootLevelFormulaFromSiblings(tx, transactionId, root, resolve.AssignToFieldId.Value, op);

            var set = AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto(
                Convert.ToInt32(unit.Id), transactionId)
                ?? new AppTransactionUnitFormulaSetDto
                {
                    TransactionId = transactionId,
                    TransactionUnitId = Convert.ToInt32(unit.Id),
                    ListAppTransactionUnitFormula = new List<AppTransactionUnitFormulaDto>()
                };
            set.TransactionId = transactionId;
            set.TransactionUnitId = Convert.ToInt32(unit.Id);
            set.TransactionUnitName = unit.UnitDisplayName;
            set.ListAppTransactionUnitFormula = set.ListAppTransactionUnitFormula
                ?? new List<AppTransactionUnitFormulaDto>();

            // Drop reconstructed Aggregate/Subscribe rows if any leaked into the list.
            var working = set.ListAppTransactionUnitFormula
                .Where(f => f.OperationType != (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                            && f.OperationType != (int)EmAppFormularType.SubscribeFromParentLevelField)
                .ToList();

            AppTransactionUnitFormulaDto target = null;
            if (action == "update")
            {
                if (formulaId.HasValue)
                    target = working.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == formulaId.Value);
                if (target == null && resolve.AssignToFieldId.HasValue)
                    target = working.FirstOrDefault(f =>
                        f.AssignToTransFieldId == resolve.AssignToFieldId
                        && f.OperationType == op);
                if (target == null)
                    return Fail("Formula to update not found. Pass formulaId from get_unit_formulas.");
            }
            else
            {
                // Idempotent add: update existing Assignment for same assign-to if present.
                if (resolve.AssignToFieldId.HasValue)
                {
                    target = working.FirstOrDefault(f =>
                        f.AssignToTransFieldId == resolve.AssignToFieldId
                        && f.OperationType == op
                        && !f.ChildTransactionUnitId.HasValue);
                }
                if (target == null)
                {
                    var maxRootSort = working
                        .Where(f => !f.ChildTransactionUnitId.HasValue && f.CaculationFlowSort.HasValue)
                        .Select(f => f.CaculationFlowSort.Value)
                        .DefaultIfEmpty(0)
                        .Max();
                    target = new AppTransactionUnitFormulaDto
                    {
                        TransactionUnitId = Convert.ToInt32(unit.Id),
                        CaculationFlowSort = maxRootSort + 1
                    };
                    working.Add(target);
                }
            }

            // ValidateDto requires Assignment expressions to contain [field] display tokens
            // (or transactionfieldid_N). Keep display form here; SaveAppTransactionUnitFormulaSetDto
            // runs InFormat → transactionfieldid_N for runtime storage.
            target.OperationType = op;
            target.FormulaExpression = resolve.ResolvedExpression;
            target.AssignToTransFieldId = resolve.AssignToFieldId;
            target.TransactionUnitId = Convert.ToInt32(unit.Id);
            // Prefer sort among root (non-grid-placeholder) formulas so Assignment runs with root flow.
            if (!target.CaculationFlowSort.HasValue || target.CaculationFlowSort.Value <= 0)
            {
                var maxRootSort = working
                    .Where(f => !f.ChildTransactionUnitId.HasValue && f.CaculationFlowSort.HasValue)
                    .Select(f => f.CaculationFlowSort.Value)
                    .DefaultIfEmpty(0)
                    .Max();
                target.CaculationFlowSort = maxRootSort + 1;
            }
            if (!string.IsNullOrWhiteSpace(warningMessage))
                target.WarningMessage = warningMessage;
            if (warningHighlightFieldId.HasValue)
                target.WarningHighlightTransFieldId = warningHighlightFieldId;

            // If converting assign-to field away from Aggregate/Subscribe, clear cross flags.
            if (resolve.AssignToFieldId.HasValue)
                ClearCrossRelationOnField(transactionId, resolve.AssignToFieldId.Value);

            set.ListAppTransactionUnitFormula = working;
            var save = AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDto(set);
            if (save?.ValidationResult?.HasErrors == true)
                return Fail(JoinErrors(save.ValidationResult));

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);

            // Preview what runtime storage looks like (InFormat + sibling _id fallback).
            var runtimePreview = ToRuntimeFormulaExpression(resolve.ResolvedExpression, fields);

            return Ok(new
            {
                IsSuccess = true,
                ok = true,
                transactionId,
                unitId = unit.Id,
                formulaOwnerUnitId = unit.Id,
                rootMasterUnitId = root?.Id,
                action,
                operationType = op,
                resolvedExpression = resolve.ResolvedExpression,
                runtimeExpression = runtimePreview,
                assignToFieldId = resolve.AssignToFieldId,
                NextStep = "Call request_form_reload, then on the open form click Calculate (or Save) — formulas do not auto-run on every keystroke."
            });
        }

        static string DeleteFormula(
            int transactionId, AppTransactionUnitExDto unit,
            int? formulaId, int? assignToFieldId, int op,
            bool? confirmed, bool? dryRun)
        {
            if (confirmed != true)
            {
                return Ok(new
                {
                    ok = true,
                    needsConfirmation = true,
                    action = "delete",
                    formulaId,
                    assignToFieldId,
                    operationType = op,
                    message = "ask_user Yes/No, then call again with confirmed=true."
                });
            }
            if (dryRun == true)
                return Ok(new { ok = true, dryRun = true, wouldDelete = new { formulaId, assignToFieldId, operationType = op } });

            // Aggregate / Subscribe delete = clear field cross flags
            if (op == (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                || op == (int)EmAppFormularType.SubscribeFromParentLevelField)
            {
                if (!assignToFieldId.HasValue)
                    return Fail("assignToFieldId required to delete Aggregate/Subscribe.");
                ClearCrossRelationOnField(transactionId, assignToFieldId.Value);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new { IsSuccess = true, ok = true, action = "delete", assignToFieldId, operationType = op });
            }

            var set = AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto(
                Convert.ToInt32(unit.Id), transactionId);
            if (set?.ListAppTransactionUnitFormula == null)
                return Fail("No formulas on unit.");

            var working = set.ListAppTransactionUnitFormula
                .Where(f => f.OperationType != (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                            && f.OperationType != (int)EmAppFormularType.SubscribeFromParentLevelField)
                .ToList();

            AppTransactionUnitFormulaDto target = null;
            if (formulaId.HasValue)
                target = working.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == formulaId.Value);
            if (target == null && assignToFieldId.HasValue)
                target = working.FirstOrDefault(f => f.AssignToTransFieldId == assignToFieldId && f.OperationType == op);
            if (target == null)
                return Fail("Formula not found for delete.");

            working.Remove(target);
            set.ListAppTransactionUnitFormula = working;
            set.TransactionUnitName = unit.UnitDisplayName;
            var save = AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDto(set);
            if (save?.ValidationResult?.HasErrors == true)
                return Fail(JoinErrors(save.ValidationResult));

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
            return Ok(new { IsSuccess = true, ok = true, action = "delete", formulaId, assignToFieldId });
        }

        static string UpsertAggregate(
            int transactionId, AppTransactionUnitExDto unit,
            int? assignToFieldId, string assignToFieldName,
            int? sourceFieldId, string sourceFieldName,
            int? aggregationType, bool? confirmed, bool? dryRun, string action)
        {
            if (!TryResolveField(unit, assignToFieldId, assignToFieldName, out var assignField, out var err)
                && !TryResolveFieldAcrossTxn(transactionId, assignToFieldId, assignToFieldName, out assignField, out err))
                return Fail(err ?? "assignToFieldId required for Aggregate.");
            if (!TryResolveFieldAcrossTxn(transactionId, sourceFieldId, sourceFieldName, out var sourceField, out err))
                return Fail(err ?? "sourceFieldId (child grid column) required for Aggregate.");
            if (!aggregationType.HasValue || aggregationType.Value < 1 || aggregationType.Value > 7)
                return Fail("aggregationType required: 1=SUM, 2=AVG, 3=MIN, 4=MAX, 5=COUNT, 6=BooleanSum, 7=ConcatenateString.");

            var preview = new
            {
                ok = true,
                needsConfirmation = !(confirmed == true),
                dryRun = dryRun == true,
                action,
                operationType = 4,
                operationName = "Aggregate",
                assignToFieldId = Convert.ToInt32(assignField.Id),
                sourceFieldId = Convert.ToInt32(sourceField.Id),
                aggregationType,
                resolvedExpression =
                    $"{Token(assignField)} = {AggLabel(aggregationType)}({Token(sourceField)})",
                message = confirmed == true ? null : "ask_user Yes/No, then call again with confirmed=true."
            };
            if (confirmed != true || dryRun == true) return Ok(preview);

            // Ensure AggFunction exists on source (child) field.
            var aggSet = AppTransactionFormulaSetupBL.RetrieveAppTransactionFieldAggFunctionSetDto(Convert.ToInt32(sourceField.Id));
            aggSet.TransactionId = transactionId;
            aggSet.ListAppTransactionFieldAggFunction = aggSet.ListAppTransactionFieldAggFunction
                ?? new List<AppTransactionFieldAggFunctionDto>();
            var existing = aggSet.ListAppTransactionFieldAggFunction
                .FirstOrDefault(a => a.AggregationFunctionType == aggregationType.Value);
            if (existing == null)
            {
                aggSet.ListAppTransactionFieldAggFunction.Add(new AppTransactionFieldAggFunctionDto
                {
                    AggregationFunctionType = aggregationType.Value,
                    TransactionFieldId = Convert.ToInt32(sourceField.Id)
                });
                aggSet = AppTransactionFormulaSetupBL.SaveAppTransactionFieldAggFunctionSetDto(aggSet);
                existing = aggSet.ListAppTransactionFieldAggFunction
                    .FirstOrDefault(a => a.AggregationFunctionType == aggregationType.Value);
            }
            if (existing?.Id == null)
                return Fail("Failed to create aggregation function on source field.");

            var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(Convert.ToInt32(assignField.Id));
            if (field == null) return Fail("Could not load assign-to field.");
            field.IsModified = true;
            field.TransactionId = transactionId;
            field.ParentUnitSubscribeChildAggFunctionId = Convert.ToInt32(existing.Id);
            field.ChildUnitSubscribeParentFieldId = null;
            var save = AppTransactionBL.SaveAppTransactionFieldExDto(field);
            if (save?.ValidationResult?.HasErrors == true)
                return Fail(JoinErrors(save.ValidationResult));

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
            return Ok(new
            {
                IsSuccess = true,
                ok = true,
                action,
                operationType = 4,
                assignToFieldId = Convert.ToInt32(assignField.Id),
                sourceFieldId = Convert.ToInt32(sourceField.Id),
                aggregationType,
                aggFunctionId = existing.Id,
                NextStep = "Call request_form_reload / refresh Formula Editor."
            });
        }

        static string UpsertSubscribe(
            int transactionId, AppTransactionUnitExDto unit,
            int? assignToFieldId, string assignToFieldName,
            int? sourceFieldId, string sourceFieldName,
            bool? confirmed, bool? dryRun, string action)
        {
            if (!TryResolveField(unit, assignToFieldId, assignToFieldName, out var assignField, out var err)
                && !TryResolveFieldAcrossTxn(transactionId, assignToFieldId, assignToFieldName, out assignField, out err))
                return Fail(err ?? "assignToFieldId (child field) required for Subscribe.");
            if (!TryResolveFieldAcrossTxn(transactionId, sourceFieldId, sourceFieldName, out var sourceField, out err))
                return Fail(err ?? "sourceFieldId (parent field) required for Subscribe.");

            var preview = new
            {
                ok = true,
                needsConfirmation = !(confirmed == true),
                dryRun = dryRun == true,
                action,
                operationType = 5,
                operationName = "Subscribe",
                assignToFieldId = Convert.ToInt32(assignField.Id),
                sourceFieldId = Convert.ToInt32(sourceField.Id),
                resolvedExpression = $"{Token(assignField)} = {Token(sourceField)}",
                message = confirmed == true ? null : "ask_user Yes/No, then call again with confirmed=true."
            };
            if (confirmed != true || dryRun == true) return Ok(preview);

            var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(Convert.ToInt32(assignField.Id));
            if (field == null) return Fail("Could not load assign-to field.");
            field.IsModified = true;
            field.TransactionId = transactionId;
            field.ChildUnitSubscribeParentFieldId = Convert.ToInt32(sourceField.Id);
            field.ParentUnitSubscribeChildAggFunctionId = null;
            var save = AppTransactionBL.SaveAppTransactionFieldExDto(field);
            if (save?.ValidationResult?.HasErrors == true)
                return Fail(JoinErrors(save.ValidationResult));

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
            return Ok(new
            {
                IsSuccess = true,
                ok = true,
                action,
                operationType = 5,
                assignToFieldId = Convert.ToInt32(assignField.Id),
                sourceFieldId = Convert.ToInt32(sourceField.Id),
                NextStep = "Call request_form_reload / refresh Formula Editor."
            });
        }

        // ── Layout ────────────────────────────────────────────────────────────

        public static string GetFormLayout(int transactionId)
        {
            try
            {
                var tx = LoadTxn(transactionId, out var err);
                if (tx == null) return Fail(err);
                if (!tx.FormId.HasValue) return Fail("Transaction has no FormId.");

                var form = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(tx.FormId.Value);
                return Ok(new
                {
                    ok = true,
                    transactionId,
                    formId = form?.Id,
                    layoutType = form?.LayoutType,
                    defaultNbColumns = form?.DefaultNbColumns,
                    hasLayoutItems = AppFormFlexLayoutBL.FormHasLayoutItems(tx.FormId.Value),
                    items = SummarizeLayoutTree(form?.AppFormLayoutItemList)
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        /// <summary>
        /// mode=generate|patch.
        /// generate: nbColumns optional; overwrites existing layout only when confirmed=true.
        /// patch: patchesJson array of ops (setHeight, moveField, setContainer, setColumns→generate).
        /// </summary>
        public static string DesignFormLayout(
            int transactionId,
            string mode,
            int? nbColumns,
            string patchesJson,
            bool? confirmed,
            bool? dryRun,
            bool? backup)
        {
            try
            {
                mode = (mode ?? "generate").Trim().ToLowerInvariant();
                var tx = LoadTxn(transactionId, out var err);
                if (tx == null) return Fail(err);
                if (!tx.FormId.HasValue) return Fail("Transaction has no FormId.");
                var formId = tx.FormId.Value;

                if (mode == "generate")
                    return GenerateLayout(transactionId, formId, nbColumns, confirmed, dryRun, backup);

                if (mode == "patch")
                    return PatchLayout(transactionId, formId, patchesJson, confirmed, dryRun, backup);

                return Fail("mode must be generate or patch.");
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static string GenerateLayout(
            int transactionId, int formId, int? nbColumns,
            bool? confirmed, bool? dryRun, bool? backup)
        {
            var hasItems = AppFormFlexLayoutBL.FormHasLayoutItems(formId);
            object backupPayload = null;
            if (backup == true && hasItems)
            {
                var current = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(formId);
                backupPayload = SummarizeLayoutTree(current?.AppFormLayoutItemList);
            }

            if (hasItems && confirmed != true)
            {
                return Ok(new
                {
                    ok = true,
                    needsConfirmation = true,
                    mode = "generate",
                    formId,
                    nbColumns,
                    hasExistingLayout = true,
                    backup = backupPayload,
                    message = "Generate will REPLACE the existing Flex layout. ask_user Yes/No, then call again with confirmed=true. Optional: backup=true, dryRun=true."
                });
            }

            if (dryRun == true)
            {
                var preview = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(formId, nbColumns);
                return Ok(new
                {
                    ok = true,
                    dryRun = true,
                    mode = "generate",
                    formId,
                    nbColumns = nbColumns ?? preview?.DefaultNbColumns,
                    previewItems = SummarizeLayoutTree(preview?.AppFormLayoutItemList),
                    backup = backupPayload
                });
            }

            if (hasItems)
            {
                var reset = AppFormBL.ResetFormLayout(formId, (int)EmAppFormLayoutType.Flex, false);
                if (reset?.ValidationResult?.HasErrors == true)
                    return Fail(JoinErrors(reset.ValidationResult));
            }

            var built = AppFormFlexLayoutBL.BuildAppFormDefaultLayout(formId, nbColumns);
            var save = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(built);
            if (save?.ValidationResult?.HasErrors == true)
                return Fail(JoinErrors(save.ValidationResult));

            return Ok(new
            {
                IsSuccess = true,
                ok = true,
                mode = "generate",
                transactionId,
                formId,
                nbColumns = nbColumns ?? built?.DefaultNbColumns,
                backup = backupPayload,
                itemCount = CountLayoutItems(built?.AppFormLayoutItemList),
                NextStep = "Call request_form_reload so the form picks up the new layout."
            });
        }

        static string PatchLayout(
            int transactionId, int formId, string patchesJson,
            bool? confirmed, bool? dryRun, bool? backup)
        {
            if (string.IsNullOrWhiteSpace(patchesJson))
                return Fail("patchesJson is required for mode=patch.");

            JArray patches;
            try { patches = JArray.Parse(patchesJson); }
            catch (Exception ex) { return Fail("patchesJson is not valid JSON array: " + ex.Message); }

            // setColumns → treat as generate (full rebuild)
            var setColumns = patches.FirstOrDefault(p =>
                string.Equals((string)p["op"], "setColumns", StringComparison.OrdinalIgnoreCase)
                || string.Equals((string)p["op"], "set_columns", StringComparison.OrdinalIgnoreCase));
            if (setColumns != null && patches.Count == 1)
            {
                var cols = setColumns["nbColumns"]?.Value<int?>() ?? setColumns["columns"]?.Value<int?>();
                return GenerateLayout(transactionId, formId, cols, confirmed, dryRun, backup);
            }

            var form = AppFormFlexLayoutBL.RetrieveOneAppFormFlexLayoutExDto(formId);
            if (form == null) return Fail($"Form {formId} not found.");

            object backupPayload = null;
            if (backup == true)
                backupPayload = SummarizeLayoutTree(form.AppFormLayoutItemList);

            var flat = FlattenLayout(form.AppFormLayoutItemList).ToList();
            var applied = new List<object>();
            var errors = new List<string>();

            foreach (var p in patches)
            {
                var op = ((string)p["op"] ?? "").Trim().ToLowerInvariant();
                switch (op)
                {
                    case "setheight":
                    case "set_height":
                        {
                            var item = FindLayoutItem(flat, p);
                            if (item == null) { errors.Add("setHeight: item not found"); break; }
                            EnsureDom(item);
                            if (p["height"] != null) item.DomAttribute.HeightValue = p["height"].Value<int>();
                            if (p["isUnlimitedHeight"] != null)
                                item.DomAttribute.IsUnlimitedHeight = p["isUnlimitedHeight"].Value<bool>();
                            MarkDirty(item);
                            applied.Add(new { op, layoutItemId = item.Id, fieldId = item.TransactionFieldId, height = item.DomAttribute.HeightValue });
                            break;
                        }
                    case "setcontainer":
                    case "set_container":
                        {
                            var item = FindLayoutItem(flat, p);
                            if (item == null) { errors.Add("setContainer: item not found"); break; }
                            EnsureDom(item);
                            if (p["label"] != null || p["displayName"] != null)
                                item.DomAttribute.DisplayName = (string)(p["label"] ?? p["displayName"]);
                            if (p["isCollapsible"] != null)
                                item.DomAttribute.IsCollapsible = p["isCollapsible"].Value<bool>();
                            if (p["isDefaultCollapsed"] != null)
                                item.DomAttribute.IsDefaultCollapsed = p["isDefaultCollapsed"].Value<bool>();
                            if (p["isTab"] != null)
                                item.DomAttribute.IsTab = p["isTab"].Value<bool>();
                            if (p["widgetDisplayType"] != null)
                                item.DomAttribute.WidgetDisplayType = p["widgetDisplayType"].Value<int>();
                            MarkDirty(item);
                            applied.Add(new
                            {
                                op,
                                layoutItemId = item.Id,
                                label = item.DomAttribute.DisplayName,
                                isCollapsible = item.DomAttribute.IsCollapsible,
                                isTab = item.DomAttribute.IsTab
                            });
                            break;
                        }
                    case "movefield":
                    case "move_field":
                        {
                            var moveErr = ApplyMoveField(form, flat, p);
                            if (moveErr != null) { errors.Add(moveErr); break; }
                            applied.Add(new { op, fieldId = p["fieldId"]?.Value<int?>(), beforeFieldId = p["beforeFieldId"]?.Value<int?>(), afterFieldId = p["afterFieldId"]?.Value<int?>() });
                            flat = FlattenLayout(form.AppFormLayoutItemList).ToList();
                            break;
                        }
                    case "setcolumns":
                    case "set_columns":
                        errors.Add("setColumns with other patches: call mode=generate with nbColumns (full rebuild), or pass setColumns alone.");
                        break;
                    default:
                        errors.Add($"Unknown op '{op}'. Supported: setHeight, setContainer, moveField, setColumns.");
                        break;
                }
            }

            if (errors.Count > 0)
                return Fail(string.Join("; ", errors));

            if (confirmed != true)
            {
                return Ok(new
                {
                    ok = true,
                    needsConfirmation = true,
                    mode = "patch",
                    formId,
                    wouldApply = applied,
                    backup = backupPayload,
                    message = "ask_user Yes/No, then call again with confirmed=true (same patchesJson)."
                });
            }

            if (dryRun == true)
            {
                return Ok(new
                {
                    ok = true,
                    dryRun = true,
                    mode = "patch",
                    wouldApply = applied,
                    backup = backupPayload
                });
            }

            form.IsModified = true;
            var save = AppFormFlexLayoutBL.SaveAppFormFlexLayoutExDto(form);
            if (save?.ValidationResult?.HasErrors == true)
                return Fail(JoinErrors(save.ValidationResult));

            return Ok(new
            {
                IsSuccess = true,
                ok = true,
                mode = "patch",
                transactionId,
                formId,
                applied,
                backup = backupPayload,
                NextStep = "Call request_form_reload so the form picks up layout changes."
            });
        }

        static string ApplyMoveField(AppFormExDto form, List<AppFormLayoutItemExDto> flat, JToken p)
        {
            var fieldId = p["fieldId"]?.Value<int?>();
            if (!fieldId.HasValue) return "moveField requires fieldId.";

            var fieldItem = flat.FirstOrDefault(i => i.TransactionFieldId == fieldId.Value);
            if (fieldItem == null) return $"moveField: fieldId {fieldId} not in layout.";

            // Field items live inside a LayoutRow; move the parent row among siblings of the target's parent.
            var fieldRow = FindParent(form.AppFormLayoutItemList, fieldItem);
            if (fieldRow == null) return "moveField: could not find parent row of field.";

            AppFormLayoutItemExDto targetField = null;
            var beforeId = p["beforeFieldId"]?.Value<int?>();
            var afterId = p["afterFieldId"]?.Value<int?>();
            if (beforeId.HasValue)
                targetField = flat.FirstOrDefault(i => i.TransactionFieldId == beforeId.Value);
            else if (afterId.HasValue)
                targetField = flat.FirstOrDefault(i => i.TransactionFieldId == afterId.Value);
            if (targetField == null) return "moveField requires beforeFieldId or afterFieldId.";

            var targetRow = FindParent(form.AppFormLayoutItemList, targetField);
            if (targetRow == null) return "moveField: could not find parent row of target.";

            var targetParent = FindParent(form.AppFormLayoutItemList, targetRow);
            var sourceParent = FindParent(form.AppFormLayoutItemList, fieldRow);
            if (targetParent == null || sourceParent == null) return "moveField: parent containers not found.";

            // Remove from source
            sourceParent.AppFormLayoutItem_List?.Remove(fieldRow);
            // Insert into target parent near target row
            var list = targetParent.AppFormLayoutItem_List ?? new ObservableSet<AppFormLayoutItemExDto>();
            targetParent.AppFormLayoutItem_List = list;
            IList<AppFormLayoutItemExDto> editable = list;
            var idx = editable.ToList().FindIndex(x => ReferenceEquals(x, targetRow) || (x.Id != null && targetRow.Id != null && Equals(x.Id, targetRow.Id)));
            if (idx < 0) idx = editable.Count;
            if (afterId.HasValue) idx = Math.Min(idx + 1, editable.Count);
            fieldRow.ParentHostId = targetParent.CurrentHostId;
            editable.Insert(Math.Min(idx, editable.Count), fieldRow);
            // Re-number sort
            int sort = 1;
            foreach (var child in editable)
            {
                child.FlowOrGridLayoutSortOrder = sort++;
                MarkDirty(child);
            }
            MarkDirty(fieldRow);
            MarkDirty(targetParent);
            MarkDirty(sourceParent);
            form.IsModified = true;
            return null;
        }

        // ── Expression resolution ─────────────────────────────────────────────

        sealed class ResolveResult
        {
            public bool Ok;
            public string ErrorJson;
            public string ResolvedExpression;
            public int? AssignToFieldId;
            public string AssignToToken;
            public List<object> FieldMap;
        }

        static ResolveResult ResolveExpression(
            string expression,
            List<AppTransactionFieldExDto> fields,
            int? assignToFieldId,
            string assignToFieldName,
            int op)
        {
            var result = new ResolveResult { FieldMap = new List<object>() };
            var raw = expression?.Trim() ?? "";

            // Split AssignTo = RHS for Assignment
            string left = null, right = raw;
            if (op == (int)EmAppFormularType.Assignment)
            {
                var eq = FindTopLevelEquals(raw);
                if (eq >= 0)
                {
                    left = raw.Substring(0, eq).Trim();
                    right = raw.Substring(eq + 1).Trim();
                }
            }

            AppTransactionFieldExDto assignField = null;
            if (assignToFieldId.HasValue)
                assignField = fields.FirstOrDefault(f => Convert.ToInt32(f.Id) == assignToFieldId.Value);
            else if (!string.IsNullOrWhiteSpace(assignToFieldName) || !string.IsNullOrWhiteSpace(left))
            {
                var name = !string.IsNullOrWhiteSpace(assignToFieldName) ? assignToFieldName : StripBrackets(left);
                var matches = MatchFields(fields, name);
                if (matches.Count == 0)
                {
                    result.ErrorJson = Fail($"Assign-to field '{name}' not found. Use find_fields / get_unit_fields.");
                    return result;
                }
                if (matches.Count > 1)
                {
                    result.ErrorJson = Ok(new
                    {
                        ok = false,
                        needsDisambiguation = true,
                        role = "assignTo",
                        name,
                        candidates = matches.Select(f => new
                        {
                            fieldId = f.Id,
                            displayName = f.DisplayName,
                            dbName = f.DataBaseFieldName,
                            unitId = f.TransactionUnitId,
                            token = Token(f)
                        }),
                        message = "ask_user which field (single_choice), then retry with assignToFieldId."
                    });
                    return result;
                }
                assignField = matches[0];
            }

            if (op == (int)EmAppFormularType.Assignment && assignField == null)
            {
                result.ErrorJson = Fail("Assignment requires assignToFieldId/assignToFieldName or 'Field = expr'.");
                return result;
            }

            var resolveRhs = ReplaceFieldNames(right, fields, result);
            if (!resolveRhs.Ok) return resolveRhs;

            if (op == (int)EmAppFormularType.Assignment)
            {
                result.AssignToFieldId = Convert.ToInt32(assignField.Id);
                result.AssignToToken = Token(assignField);
                result.ResolvedExpression = $"{result.AssignToToken} = {resolveRhs.ResolvedExpression}";
                result.FieldMap.Insert(0, new { role = "assignTo", fieldId = assignField.Id, token = result.AssignToToken, displayName = assignField.DisplayName });
            }
            else
            {
                result.ResolvedExpression = resolveRhs.ResolvedExpression;
                if (assignField != null)
                {
                    result.AssignToFieldId = Convert.ToInt32(assignField.Id);
                    result.AssignToToken = Token(assignField);
                }
            }

            result.Ok = true;
            return result;
        }

        static ResolveResult ReplaceFieldNames(string expr, List<AppTransactionFieldExDto> fields, ResolveResult accumulate)
        {
            var result = new ResolveResult { FieldMap = accumulate.FieldMap ?? new List<object>(), Ok = true };
            if (string.IsNullOrEmpty(expr))
            {
                result.ResolvedExpression = expr;
                return result;
            }

            // Protect quoted strings
            var protectedMap = new Dictionary<string, string>();
            var work = QuotedString.Replace(expr, m =>
            {
                var key = $"__Q{protectedMap.Count}__";
                protectedMap[key] = m.Value;
                return key;
            });

            // Already-bracketed tokens: map display-ish content to FormulaDisplayName if possible
            work = BracketToken.Replace(work, m =>
            {
                var inner = m.Value;
                var hit = fields.FirstOrDefault(f =>
                    string.Equals(f.FormulaDisplayName, inner, StringComparison.OrdinalIgnoreCase)
                    || string.Equals($"[{f.DataBaseFieldName}_{f.Id}]", inner, StringComparison.OrdinalIgnoreCase)
                    || string.Equals($"[{f.DisplayName}]", inner, StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                {
                    var tok = Token(hit);
                    result.FieldMap.Add(new { role = "ref", fieldId = hit.Id, from = inner, token = tok });
                    return tok;
                }
                return inner; // leave as-is (may already be correct)
            });

            // Bare identifiers / multi-word display names: longest-first match
            var candidates = fields
                .SelectMany(f => new[]
                {
                    new { Field = f, Alias = f.DisplayName },
                    new { Field = f, Alias = f.DataBaseFieldName }
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Alias))
                .OrderByDescending(x => x.Alias.Length)
                .ToList();

            foreach (var c in candidates)
            {
                var alias = c.Alias.Trim();
                if (alias.Length < 2) continue;
                if (FormulaKeywords.Contains(alias)) continue;
                // Match as whole word-ish, allowing spaces in display names
                var pattern = @"\b" + Regex.Escape(alias).Replace(@"\ ", @"\s+") + @"\b";
                var rx = new Regex(pattern, RegexOptions.IgnoreCase);
                if (!rx.IsMatch(work)) continue;

                // Ambiguity: same alias on multiple fields
                var same = MatchFields(fields, alias);
                if (same.Count > 1)
                {
                    result.Ok = false;
                    result.ErrorJson = Ok(new
                    {
                        ok = false,
                        needsDisambiguation = true,
                        role = "expressionField",
                        name = alias,
                        candidates = same.Select(f => new
                        {
                            fieldId = f.Id,
                            displayName = f.DisplayName,
                            dbName = f.DataBaseFieldName,
                            unitId = f.TransactionUnitId,
                            token = Token(f)
                        }),
                        message = "ask_user which field, then retry with expression using [DbName_Id] tokens or assignToFieldId + RHS only."
                    });
                    return result;
                }

                var tok = Token(c.Field);
                work = rx.Replace(work, tok);
                result.FieldMap.Add(new { role = "ref", fieldId = c.Field.Id, from = alias, token = tok });
            }

            foreach (var kv in protectedMap)
                work = work.Replace(kv.Key, kv.Value);

            result.ResolvedExpression = work;
            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static AppTransactionExDto LoadTxn(int transactionId, out string error)
        {
            error = null;
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx == null) error = $"Transaction {transactionId} not found.";
            return tx;
        }

        static bool TryResolveUnit(AppTransactionExDto tx, int? unitId, string unitName,
            out AppTransactionUnitExDto unit, out string error)
        {
            unit = null;
            error = null;
            var units = EntityMapWiring.FlattenUnits(
                tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            if (unitId.HasValue)
                unit = units.FirstOrDefault(u => Convert.ToInt32(u.Id) == unitId.Value);
            else if (!string.IsNullOrWhiteSpace(unitName))
                unit = units.FirstOrDefault(u =>
                    EntityMapWiring.UnitOrTableMatches(u.UnitDisplayName, unitName)
                    || EntityMapWiring.UnitOrTableMatches(u.DataBaseTableName, unitName));
            else
                unit = tx.RootMasterUnit ?? units.FirstOrDefault();

            if (unit == null)
            {
                error = "Unit not found. Pass unitId from get_current_transaction_config / get_unit_fields.";
                return false;
            }
            return true;
        }

        static bool TryResolveField(AppTransactionUnitExDto unit, int? fieldId, string fieldName,
            out AppTransactionFieldExDto field, out string error)
        {
            field = null;
            error = null;
            var list = unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>();
            if (fieldId.HasValue)
            {
                field = list.FirstOrDefault(f => Convert.ToInt32(f.Id) == fieldId.Value);
                if (field == null) error = $"Field {fieldId} not on unit {unit.Id}.";
                return field != null;
            }
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                var matches = MatchFields(list.ToList(), fieldName);
                if (matches.Count == 1) { field = matches[0]; return true; }
                if (matches.Count > 1)
                {
                    error = $"Field '{fieldName}' ambiguous on unit.";
                    return false;
                }
                error = $"Field '{fieldName}' not found on unit.";
                return false;
            }
            error = "fieldId or fieldName required.";
            return false;
        }

        static bool TryResolveFieldAcrossTxn(int transactionId, int? fieldId, string fieldName,
            out AppTransactionFieldExDto field, out string error)
        {
            field = null;
            error = null;
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId)
                ?? AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
            if (tx == null) { error = "Transaction not found."; return false; }
            var all = tx.DictAllTransactionField?.Values.ToList() ?? new List<AppTransactionFieldExDto>();
            if (fieldId.HasValue)
            {
                field = all.FirstOrDefault(f => Convert.ToInt32(f.Id) == fieldId.Value);
                if (field == null) error = $"Field {fieldId} not found.";
                return field != null;
            }
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                var matches = MatchFields(all, fieldName);
                if (matches.Count == 1) { field = matches[0]; return true; }
                if (matches.Count > 1)
                {
                    error = Ok(new
                    {
                        ok = false,
                        needsDisambiguation = true,
                        name = fieldName,
                        candidates = matches.Select(f => new { fieldId = f.Id, displayName = f.DisplayName, dbName = f.DataBaseFieldName, unitId = f.TransactionUnitId }),
                        message = "ask_user which field, then retry with fieldId."
                    });
                    // Return false with ErrorJson-style message already JSON — Fail will wrap; use Fail with plain text instead.
                    error = $"Field '{fieldName}' ambiguous ({matches.Count} matches). Pass fieldId.";
                    return false;
                }
                error = $"Field '{fieldName}' not found.";
                return false;
            }
            error = "fieldId or fieldName required.";
            return false;
        }

        static List<AppTransactionFieldExDto> MatchFields(List<AppTransactionFieldExDto> fields, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<AppTransactionFieldExDto>();
            var needle = name.Trim();
            return fields.Where(f =>
                EntityMapWiring.UnitOrTableMatches(f.DisplayName, needle)
                || EntityMapWiring.ColumnNameMatches(f.DataBaseFieldName, needle)
                || string.Equals(f.FormulaDisplayName, needle, StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.FormulaDisplayName, $"[{needle}]", StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        static void ClearCrossRelationOnField(int transactionId, int fieldId)
        {
            var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(fieldId);
            if (field == null) return;
            if (!field.ParentUnitSubscribeChildAggFunctionId.HasValue && !field.ChildUnitSubscribeParentFieldId.HasValue)
                return;
            field.IsModified = true;
            field.TransactionId = transactionId;
            field.ParentUnitSubscribeChildAggFunctionId = null;
            field.ChildUnitSubscribeParentFieldId = null;
            AppTransactionBL.SaveAppTransactionFieldExDto(field);
        }

        static string Token(AppTransactionFieldExDto f) =>
            !string.IsNullOrWhiteSpace(f.FormulaDisplayName)
                ? f.FormulaDisplayName
                : $"[{f.DataBaseFieldName}_{f.Id}]";

        /// <summary>
        /// Convert Formula Editor / agent display tokens to runtime storage form.
        /// Runtime AssignmentLeftSideFieldId uses the substring after the FIRST '_' —
        /// so [Table.Field_124] must become transactionfieldid_124 before save.
        /// </summary>
        static string ToRuntimeFormulaExpression(string expression, List<AppTransactionFieldExDto> fields)
        {
            if (string.IsNullOrEmpty(expression)) return expression;
            var prefix = AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix;
            if (fields == null) fields = new List<AppTransactionFieldExDto>();

            // Protect quoted string literals
            var protectedMap = new Dictionary<string, string>();
            var work = QuotedString.Replace(expression, m =>
            {
                var key = $"__Q{protectedMap.Count}__";
                protectedMap[key] = m.Value;
                return key;
            });

            work = BracketToken.Replace(work, m =>
            {
                var tok = m.Value.Trim();
                var byDisplay = fields.FirstOrDefault(f =>
                    !string.IsNullOrWhiteSpace(f.FormulaDisplayName)
                    && string.Equals(f.FormulaDisplayName.Trim(), tok, StringComparison.OrdinalIgnoreCase));
                if (byDisplay != null)
                    return prefix + byDisplay.Id;

                // [..._123] → id from trailing _digits (handles sibling [Table.Db_123])
                var inner = tok.Length >= 2 ? tok.Substring(1, tok.Length - 2) : tok;
                var us = inner.LastIndexOf('_');
                if (us >= 0 && us < inner.Length - 1
                    && int.TryParse(inner.Substring(us + 1), out var id)
                    && fields.Any(f => Convert.ToInt32(f.Id) == id))
                {
                    return prefix + id;
                }

                return tok;
            });

            foreach (var kv in protectedMap)
                work = work.Replace(kv.Key, kv.Value);

            return work;
        }

        static string StripBrackets(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Trim();
            if (s.StartsWith("[") && s.EndsWith("]")) return s.Substring(1, s.Length - 2);
            return s;
        }

        static int FindTopLevelEquals(string s)
        {
            // Prefer " = " assignment, not == != >= <=
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '=') continue;
                var prev = i > 0 ? s[i - 1] : '\0';
                var next = i + 1 < s.Length ? s[i + 1] : '\0';
                if (prev == '=' || prev == '!' || prev == '<' || prev == '>' || next == '=') continue;
                return i;
            }
            return -1;
        }

        static string AggLabel(int? t) =>
            t == 1 ? "SUM" : t == 2 ? "AVG" : t == 3 ? "MIN" : t == 4 ? "MAX" : t == 5 ? "COUNT"
            : t == 6 ? "BooleanSum" : t == 7 ? "Concat" : t != null ? $"Agg({t})" : "AGG";

        static string OpName(int op) =>
            op == 1 ? "Assignment" : op == 2 ? "Warning" : op == 3 ? "Error"
            : op == 4 ? "Aggregate" : op == 5 ? "Subscribe" : op.ToString();

        static object SummarizeFormula(AppTransactionUnitFormulaDto f, int? storedUnitId = null) => new
        {
            formulaId = f.Id,
            storedUnitId = storedUnitId ?? f.TransactionUnitId,
            operationType = f.OperationType,
            operationName = OpName(f.OperationType ?? 0),
            assignToFieldId = f.AssignToTransFieldId,
            formulaExpression = f.FormulaExpression,
            warningMessage = f.WarningMessage,
            warningHighlightFieldId = f.WarningHighlightTransFieldId,
            sort = f.CaculationFlowSort,
            childUnitId = f.ChildTransactionUnitId,
            formulaName = f.FormulaName
        };

        /// <summary>
        /// True root master (ParentTransactionUnitId null, not master-sibling).
        /// Formula Editor main grid only shows this unit's formulas.
        /// </summary>
        static AppTransactionUnitExDto TrueRootMasterUnit(AppTransactionExDto tx)
        {
            if (tx == null) return null;
            var units = EntityMapWiring.FlattenUnits(
                tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            return units.FirstOrDefault(u =>
                       !u.ParentTransactionUnitId.HasValue
                       && !(u.IsMasterSiblingUnit.HasValue && u.IsMasterSiblingUnit.Value))
                   ?? tx.RootMasterUnit
                   ?? units.FirstOrDefault();
        }

        static bool IsMasterSiblingUnit(AppTransactionUnitExDto u) =>
            u != null && u.IsMasterSiblingUnit.HasValue && u.IsMasterSiblingUnit.Value;

        static bool IsRootLevelUnit(AppTransactionUnitExDto u) =>
            u != null && !u.ParentTransactionUnitId.HasValue;

        /// <summary>
        /// Root + master-sibling field formulas must be owned by TrueRootMasterUnit.
        /// Child/grandchild grid formulas stay on their own unit.
        /// </summary>
        static AppTransactionUnitExDto ResolveFormulaOwnerUnit(
            AppTransactionExDto tx,
            AppTransactionUnitExDto requestedUnit,
            int? assignToFieldId,
            string assignToFieldName)
        {
            var root = TrueRootMasterUnit(tx);
            if (root == null) return requestedUnit;

            AppTransactionFieldExDto assignField = null;
            if (assignToFieldId.HasValue && tx.DictAllTransactionField != null)
                tx.DictAllTransactionField.TryGetValue(assignToFieldId.Value, out assignField);
            else if (!string.IsNullOrWhiteSpace(assignToFieldName))
            {
                var matches = MatchFields(tx.DictAllTransactionField?.Values.ToList() ?? new List<AppTransactionFieldExDto>(), assignToFieldName);
                if (matches.Count == 1) assignField = matches[0];
            }

            if (assignField != null
                && tx.DictAllTransactionUnitIdExDto != null
                && tx.DictAllTransactionUnitIdExDto.TryGetValue(assignField.TransactionUnitId.ToString(), out var fieldUnit))
            {
                // Field on root or master-sibling → Formula Editor root grid → RootMasterUnit
                if (IsRootLevelUnit(fieldUnit))
                    return root;
                // Field on child/grandchild → that unit's formula list
                return fieldUnit;
            }

            // No assign field yet: if caller passed a sibling unit id, remap to root
            if (requestedUnit != null && IsMasterSiblingUnit(requestedUnit))
                return root;

            return requestedUnit ?? root;
        }

        static List<AppTransactionUnitExDto> FormulaListSourceUnits(AppTransactionExDto tx, AppTransactionUnitExDto unit)
        {
            var list = new List<AppTransactionUnitExDto>();
            if (unit == null) return list;
            list.Add(unit);

            var root = TrueRootMasterUnit(tx);
            if (root != null && Convert.ToInt32(unit.Id) == Convert.ToInt32(root.Id)
                && tx.SibLineTransactionUnitIdExDtoList != null)
            {
                foreach (var sib in tx.SibLineTransactionUnitIdExDtoList)
                {
                    if (sib?.Id == null) continue;
                    if (list.Any(u => Convert.ToInt32(u.Id) == Convert.ToInt32(sib.Id))) continue;
                    list.Add(sib);
                }
            }
            return list;
        }

        static bool FormulaAssignsToField(AppTransactionUnitFormulaDto f, int assignToFieldId)
        {
            if (f == null) return false;
            if (f.AssignToTransFieldId == assignToFieldId) return true;
            var expr = f.FormulaExpression ?? "";
            // Stored or OutFormat tokens: transactionfieldid_124 / [..._124]
            if (expr.IndexOf(AppTransactionFormulaSetupBL.TransactionFieldFormulaPrefix + assignToFieldId,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (expr.Contains("_" + assignToFieldId + "]") || expr.Contains("_" + assignToFieldId + " "))
                return true;
            return false;
        }

        static void RemoveDuplicateRootLevelFormulaFromSiblings(
            AppTransactionExDto tx, int transactionId, AppTransactionUnitExDto root, int assignToFieldId, int op)
        {
            if (tx.SibLineTransactionUnitIdExDtoList == null) return;
            foreach (var sib in tx.SibLineTransactionUnitIdExDtoList)
            {
                if (sib?.Id == null) continue;
                if (Convert.ToInt32(sib.Id) == Convert.ToInt32(root.Id)) continue;

                var set = AppTransactionFormulaSetupBL.RetrieveAppTransactionUnitFormulaSetDto(
                    Convert.ToInt32(sib.Id), transactionId);
                if (set?.ListAppTransactionUnitFormula == null || set.ListAppTransactionUnitFormula.Count == 0)
                    continue;

                var working = set.ListAppTransactionUnitFormula
                    .Where(f => f.OperationType != (int)EmAppFormularType.SubscribeFromGridColumnAggregation
                                && f.OperationType != (int)EmAppFormularType.SubscribeFromParentLevelField)
                    .Where(f => f.ChildTransactionUnitId.HasValue
                                || f.OperationType != op
                                || !FormulaAssignsToField(f, assignToFieldId))
                    .ToList();

                if (working.Count == set.ListAppTransactionUnitFormula.Count) continue;

                set.ListAppTransactionUnitFormula = working;
                set.TransactionUnitId = Convert.ToInt32(sib.Id);
                set.TransactionId = transactionId;
                set.TransactionUnitName = sib.UnitDisplayName;
                AppTransactionFormulaSetupBL.SaveAppTransactionUnitFormulaSetDto(set);
            }
        }

        static IEnumerable<object> SummarizeLayoutTree(ObservableSet<AppFormLayoutItemExDto> items) =>
            SummarizeLayoutTree((IEnumerable<AppFormLayoutItemExDto>)items);

        static IEnumerable<object> SummarizeLayoutTree(IEnumerable<AppFormLayoutItemExDto> items)
        {
            if (items == null) yield break;
            foreach (var i in items.OrderBy(x => x.FlowOrGridLayoutSortOrder ?? 0))
            {
                var dom = i.DomAttribute;
                yield return new
                {
                    layoutItemId = i.Id,
                    hostId = i.CurrentHostId,
                    parentHostId = i.ParentHostId,
                    widgetDisplayType = dom?.WidgetDisplayType,
                    label = dom?.DisplayName,
                    fieldId = i.TransactionFieldId,
                    gridUnitId = i.GridTransactionUnitId,
                    colSpan = dom?.ColSpanValue,
                    columns = dom?.DefaultNbColumns,
                    height = dom?.HeightValue,
                    isCollapsible = dom?.IsCollapsible,
                    isDefaultCollapsed = dom?.IsDefaultCollapsed,
                    isTab = dom?.IsTab,
                    children = SummarizeLayoutTree(i.AppFormLayoutItem_List).ToList()
                };
            }
        }

        static IEnumerable<AppFormLayoutItemExDto> FlattenLayout(IEnumerable<AppFormLayoutItemExDto> items)
        {
            if (items == null) yield break;
            foreach (var i in items)
            {
                yield return i;
                foreach (var c in FlattenLayout(i.AppFormLayoutItem_List))
                    yield return c;
            }
        }

        static AppFormLayoutItemExDto FindLayoutItem(List<AppFormLayoutItemExDto> flat, JToken p)
        {
            var id = p["layoutItemId"]?.Value<int?>() ?? p["id"]?.Value<int?>();
            if (id.HasValue)
                return flat.FirstOrDefault(i => i.Id != null && Convert.ToInt32(i.Id) == id.Value);
            var fieldId = p["fieldId"]?.Value<int?>();
            if (fieldId.HasValue)
                return flat.FirstOrDefault(i => i.TransactionFieldId == fieldId.Value);
            var gridUnitId = p["gridUnitId"]?.Value<int?>();
            if (gridUnitId.HasValue)
                return flat.FirstOrDefault(i => i.GridTransactionUnitId == gridUnitId.Value);
            var hostId = (string)p["hostId"];
            if (!string.IsNullOrWhiteSpace(hostId))
                return flat.FirstOrDefault(i => string.Equals(i.CurrentHostId, hostId, StringComparison.OrdinalIgnoreCase));
            return null;
        }

        static AppFormLayoutItemExDto FindParent(ObservableSet<AppFormLayoutItemExDto> roots, AppFormLayoutItemExDto child)
        {
            if (roots == null || child == null) return null;
            foreach (var r in roots)
            {
                if (r.AppFormLayoutItem_List != null && r.AppFormLayoutItem_List.Any(c => ReferenceEquals(c, child)
                    || (c.Id != null && child.Id != null && Equals(c.Id, child.Id))
                    || (!string.IsNullOrEmpty(c.CurrentHostId) && c.CurrentHostId == child.CurrentHostId)))
                    return r;
                var nested = FindParent(r.AppFormLayoutItem_List, child);
                if (nested != null) return nested;
            }
            return null;
        }

        static void EnsureDom(AppFormLayoutItemExDto item)
        {
            if (item.DomAttribute == null)
                item.DomAttribute = new AppFormDomAttributeDto();
        }

        static void MarkDirty(AppFormLayoutItemExDto item)
        {
            if (item == null) return;
            item.IsModified = true;
        }

        static int CountLayoutItems(IEnumerable<AppFormLayoutItemExDto> items) =>
            FlattenLayout(items).Count();

        static string JoinErrors(APP.Framework.Validation.ValidationResult vr) =>
            vr?.Items == null ? "Save failed"
            : string.Join("; ", vr.Items.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m)));

        static string Ok(object payload) => JsonConvert.SerializeObject(payload, Formatting.Indented);

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });
    }
}
