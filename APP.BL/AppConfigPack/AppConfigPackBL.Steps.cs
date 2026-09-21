using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;

namespace APP.BL.AppConfigPack
{
    /// <summary>
    /// Public pipeline steps for App Config Pack apply.
    /// <see cref="Execute"/> runs the full sequence; Ex DLL / other BL may call individual steps
    /// after composing an <see cref="AppConfigPackDto"/> (preferred: full Execute).
    /// </summary>
    public static partial class AppConfigPackBL
    {
        /// <summary>
        /// Create a step context: ensure IntegrationId columns, resolve tenant DS + application id.
        /// Does not validate the pack — call <see cref="Validate"/> first when needed.
        /// </summary>
        public static AppConfigPackStepContext BeginSteps(
            AppConfigPackDto pack,
            int? saasApplicationId = null,
            bool ensureSchema = true)
        {
            if (pack == null)
                throw new ArgumentException("Pack is required.");

            if (ensureSchema)
                EnsurePackSchema();

            return new AppConfigPackStepContext
            {
                Pack = pack,
                TenantDataSourceId = GetTenantDataSourceId(),
                SaasApplicationId = saasApplicationId ?? pack.Source?.SaasApplicationId,
                Result = new AppConfigPackExecuteResultDto(),
                TransactionIdsByIntegration = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>Step 1 — create missing tables / add columns / CREATE OR ALTER VIEW.</summary>
        public static void StepApplyDdl(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            ApplyDdl(ctx.Pack, ctx.TenantDataSourceId, ctx.Result);
        }

        /// <summary>Step 2 — refresh tenant schema fixture cache after DDL.</summary>
        public static void StepRefreshTenantSchemaCache(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(ctx.TenantDataSourceId);
        }

        /// <summary>Step 3 — upsert simple-list entities (before TX so entityCode resolves).</summary>
        public static void StepUpsertSimpleListEntities(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            UpsertSimpleListEntities(ctx.Pack, ctx.SaasApplicationId, ctx.Result);
        }

        /// <summary>
        /// Step 4 — upsert transactions (hierarchy insert / field+unit overlay / commands / form / ListEdit menu).
        /// Fills <see cref="AppConfigPackStepContext.TransactionIdsByIntegration"/>.
        /// </summary>
        public static void StepUpsertTransactions(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            ctx.TransactionIdsByIntegration = UpsertTransactions(
                ctx.Pack, ctx.TenantDataSourceId, ctx.SaasApplicationId, ctx.Result)
                ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Re-apply field metadata + unit display overlays without creating hierarchy.
        /// Use when TX already exist and pack only updates fields / GridDisplayType / pivot flags.
        /// </summary>
        public static void StepApplyFieldAndUnitOverlays(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            ApplyFieldAndUnitOverlays(ctx.Pack, ctx.TransactionIdsByIntegration);
        }

        /// <summary>Same as <see cref="StepApplyFieldAndUnitOverlays"/> without a full context.</summary>
        public static void ApplyFieldAndUnitOverlays(
            AppConfigPackDto pack,
            IDictionary<string, int> txIdsByIntegration)
        {
            if (pack == null || txIdsByIntegration == null || txIdsByIntegration.Count == 0)
                return;

            foreach (var tx in pack.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId))
                    continue;
                if (!txIdsByIntegration.TryGetValue(tx.IntegrationId.Trim(), out int transactionId))
                    continue;

                OverlayTransactionFields(transactionId, tx);
                ApplyUnitOverlays(transactionId, tx);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
            }
        }

        /// <summary>Step 5 — child-grid Create/Edit/Delete link targets (needs all TX ids).</summary>
        public static void StepApplyTransactionChildLinkTargets(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            ApplyTransactionChildLinkTargets(ctx.Pack, ctx.TransactionIdsByIntegration);
        }

        /// <summary>Step 6 — Data Model Template / transaction group.</summary>
        public static void StepUpsertTransactionGroup(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            int? groupId = UpsertTransactionGroup(ctx.Pack, ctx.TransactionIdsByIntegration, ctx.SaasApplicationId);
            if (groupId.HasValue && groupId.Value > 0)
            {
                ctx.TransactionGroupId = groupId;
                ctx.Result.TransactionGroupId = groupId;
                ctx.Result.Messages.Add($"Transaction group {groupId.Value} ready.");
            }
        }

        /// <summary>Step 7 — searches (DataSet / criteria / SearchView / linkTargets / menu) + attach Search assets.</summary>
        public static void StepUpsertSearches(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            UpsertSearches(
                ctx.Pack,
                ctx.TenantDataSourceId,
                ctx.SaasApplicationId,
                ctx.TransactionIdsByIntegration,
                ctx.TransactionGroupId,
                ctx.Result);
        }

        /// <summary>Step 8 — Data Load / Unit Formula / Conditional Action / Linked Search (omit = keep).</summary>
        public static void StepApplyTransactionRuntimeExtras(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            ApplyTransactionRuntimeExtras(
                ctx.Pack,
                ctx.TransactionIdsByIntegration,
                ctx.TenantDataSourceId,
                ctx.SaasApplicationId);
        }

        /// <summary>Step 9 — attach transactions as Application assets.</summary>
        public static void StepAttachApplicationAssets(AppConfigPackStepContext ctx)
        {
            RequireCtx(ctx);
            AttachApplicationAssets(
                ctx.SaasApplicationId,
                ctx.TransactionIdsByIntegration.Values.ToList(),
                ctx.Result);
        }

        /// <summary>
        /// Run the standard full pipeline on an already-validated pack (same order as <see cref="Execute"/>).
        /// </summary>
        public static AppConfigPackExecuteResultDto RunAllSteps(
            AppConfigPackDto pack,
            int? saasApplicationId = null)
        {
            var ctx = BeginSteps(pack, saasApplicationId, ensureSchema: true);
            StepApplyDdl(ctx);
            StepRefreshTenantSchemaCache(ctx);
            StepUpsertSimpleListEntities(ctx);
            StepUpsertTransactions(ctx);
            StepApplyTransactionChildLinkTargets(ctx);
            StepUpsertTransactionGroup(ctx);
            StepUpsertSearches(ctx);
            StepApplyTransactionRuntimeExtras(ctx);
            StepAttachApplicationAssets(ctx);
            ctx.Result.IsSuccess = true;
            return ctx.Result;
        }

        private static void RequireCtx(AppConfigPackStepContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));
            if (ctx.Pack == null)
                throw new ArgumentException("Pack is required.", nameof(ctx));
            if (ctx.Result == null)
                ctx.Result = new AppConfigPackExecuteResultDto();
            if (ctx.TransactionIdsByIntegration == null)
                ctx.TransactionIdsByIntegration = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
