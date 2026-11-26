using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BL;
using App.BL.AIAgent.GenericAgent.ApiProvider;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tools for API Management configuration (App + 3rd-party + OpenAPI docs).
    /// LibraryKey: platform-api-integration-config
    /// </summary>
    public class ApiIntegrationConfigPlugin
    {
        // ── Providers ──────────────────────────────────────────────────────────

        [AgentTool("api_cfg_list_providers",
            "List App API Provider (id=1) and all 3rd-party providers. Secrets redacted. Optional includeApis.")]
        public string ListProviders(
            AgentToolContext context = null,
            [AgentParam("Include nested API summaries (default false).")] bool includeApis = false,
            [AgentParam("Include App built-in provider id=1 (default true).")] bool includeAppBuiltIn = true)
        {
            _ = context;
            try
            {
                AppIntergrationSettingBL.EnsureAppBuiltInProviderExists();
                var list = AppIntergrationSettingBL.RetrieveAllAppIntergrationSettingDto(includeAppBuiltIn)
                    ?? new List<AppIntergrationSettingExDto>();
                var items = list
                    .Where(p => p != null)
                    .Select(p => ApiIntegrationConfigBL.SummarizeProvider(p, includeApis))
                    .ToList();
                return ApiIntegrationConfigBL.Ok(new { count = items.Count, providers = items });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_get_provider",
            "Get one provider by providerId (1 = App API Provider). Includes API list summaries. Secrets redacted.")]
        public string GetProvider(
            AgentToolContext context = null,
            [AgentParam("Provider id", isRequired: true)] int? providerId = null)
        {
            _ = context;
            try
            {
                if (!TryResolveProviderId(context, providerId, out var id, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                if (id == AppIntergrationSettingBL.AppBuiltInProviderId)
                    AppIntergrationSettingBL.EnsureAppBuiltInProviderExists();
                var p = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(id);
                if (p == null) return ApiIntegrationConfigBL.Fail($"Provider {id} not found.");
                return ApiIntegrationConfigBL.Ok(ApiIntegrationConfigBL.SummarizeProvider(p, includeApis: true));
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_upsert_provider",
            "Create or update a 3rd-party API provider. dtoJson: name, internalCode (stable key — required for idempotent create), " +
            "description, restApiUrl, environmentVariables, otherSettings/apicredentialConfig object " +
            "(databaseTablePrefix, dictEnvironmentVariable, dictCookieSetting), dataSourceRegisterId. " +
            "Omit providerId to resolve by internalCode then name (update if one match; Fail if many). " +
            "Returns providerId — pass it to every api_cfg_upsert_api. Preview → ask_user → confirmed=true.")]
        public string UpsertProvider(
            AgentToolContext context = null,
            [AgentParam("Existing provider id for update; omit/0 to resolve by internalCode/name or create.")] int? providerId = null,
            [AgentParam("JSON object of provider fields", isRequired: true)] string dtoJson = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dtoJson))
                    return ApiIntegrationConfigBL.Fail("dtoJson is required.");
                JObject patch;
                try { patch = JObject.Parse(dtoJson); }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid dtoJson: " + ex.Message); }

                var id = providerId ?? 0;
                if (id <= 0)
                    TryResolveProviderId(context, null, out id, out _);

                // Idempotent resolve: no explicit id → match by internalCode (then name)
                if (id <= 0)
                {
                    var code = (patch["internalCode"] ?? patch["InternalCode"])?.ToString();
                    var name = (patch["name"] ?? patch["Name"])?.ToString();
                    var matches = ApiIntegrationConfigBL.FindThirdPartyProviders(code, name);
                    if (matches.Count == 1)
                        id = Convert.ToInt32(matches[0].Id);
                    else if (matches.Count > 1)
                    {
                        var ids = string.Join(", ", matches.Select(m => Convert.ToInt32(m.Id)));
                        return ApiIntegrationConfigBL.Fail(
                            $"Multiple providers match internalCode/name. Pass providerId explicitly. Candidates: {ids}");
                    }
                    // else 0 → create
                }

                if (id == AppIntergrationSettingBL.AppBuiltInProviderId)
                {
                    // Allow limited metadata-only updates on built-in; block delete path elsewhere.
                }

                AppIntergrationSettingExDto target;
                if (id > 0)
                {
                    target = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(id);
                    if (target == null) return ApiIntegrationConfigBL.Fail($"Provider {id} not found.");
                }
                else
                {
                    target = new AppIntergrationSettingExDto();
                    var code = (patch["internalCode"] ?? patch["InternalCode"])?.ToString();
                    if (string.IsNullOrWhiteSpace(code))
                        return ApiIntegrationConfigBL.Fail(
                            "internalCode is required when creating a provider (stable key for later upserts).");
                }

                ApiIntegrationConfigBL.ApplyProviderPatch(target, patch);

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        providerId = id > 0 ? (int?)id : null,
                        isCreate = id <= 0,
                        name = target.Name,
                        internalCode = target.InternalCode,
                        restApiUrl = target.RestApiurl,
                        hasEnvironmentVariables =
                            target.OtherSettingsDto?.DictEnvironmentVariable != null
                            && target.OtherSettingsDto.DictEnvironmentVariable.Count > 0
                    });
                }

                var result = AppIntergrationSettingBL.SaveAppIntergrationSettingExDto(target);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));

                var saved = result?.Object ?? AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(target.Id);
                var savedId = saved != null ? Convert.ToInt32(saved.Id) : Convert.ToInt32(target.Id);
                return ApiIntegrationConfigBL.Ok(new
                {
                    saved = true,
                    providerId = savedId,
                    provider = ApiIntegrationConfigBL.SummarizeProvider(saved, includeApis: false),
                    nextStep = "Pass providerId=" + savedId + " in every api_cfg_upsert_api dtoJson for this provider's HTTP APIs."
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_delete_provider",
            "Delete a 3rd-party provider. Forbidden for App built-in provider id=1. Preview → ask_user → confirmed=true.")]
        public string DeleteProvider(
            AgentToolContext context = null,
            [AgentParam("Provider id", isRequired: true)] int? providerId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (!TryResolveProviderId(context, providerId, out var id, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                if (id == AppIntergrationSettingBL.AppBuiltInProviderId)
                    return ApiIntegrationConfigBL.Fail("Cannot delete App API Provider (id=1). Delete individual APIs instead.");

                var p = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(id);
                if (p == null) return ApiIntegrationConfigBL.Fail($"Provider {id} not found.");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        providerId = id,
                        name = p.Name,
                        apiCount = p.AppIntergrationSettingParameterList?.Count ?? 0
                    }, "Will delete provider and its APIs. ask_user then confirmed=true.");
                }

                var result = AppIntergrationSettingBL.DeleteOneAppIntergrationSetting(id);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));
                return ApiIntegrationConfigBL.Ok(new { deleted = true, providerId = id });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        // ── APIs / operations ──────────────────────────────────────────────────

        [AgentTool("api_cfg_list_apis",
            "List API operations. Filter by providerId, apiType keyword, or free-text query on ActionCode/description.")]
        public string ListApis(
            AgentToolContext context = null,
            [AgentParam("Provider id; omit = all providers (loads each). Prefer a providerId.")] int? providerId = null,
            [AgentParam("Substring match on APIType / ActionCode / description.")] string query = null,
            [AgentParam("Max rows (default 80, max 200).")] int take = 80)
        {
            try
            {
                take = Math.Clamp(take <= 0 ? 80 : take, 1, 200);
                var hostPid = ReadHostInt(context, "providerId", "ProviderId");
                var pid = providerId ?? hostPid;

                var rows = new List<object>();
                if (pid.HasValue && pid.Value > 0)
                {
                    if (pid.Value == AppIntergrationSettingBL.AppBuiltInProviderId)
                        AppIntergrationSettingBL.EnsureAppBuiltInProviderExists();
                    var p = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(pid.Value);
                    if (p == null) return ApiIntegrationConfigBL.Fail($"Provider {pid} not found.");
                    rows.AddRange(FilterApis(p, query).Take(take));
                }
                else
                {
                    AppIntergrationSettingBL.EnsureAppBuiltInProviderExists();
                    foreach (var p in AppIntergrationSettingBL.RetrieveAllAppIntergrationSettingDto(true) ?? Enumerable.Empty<AppIntergrationSettingExDto>())
                    {
                        rows.AddRange(FilterApis(p, query));
                        if (rows.Count >= take) break;
                    }
                    rows = rows.Take(take).ToList();
                }

                return ApiIntegrationConfigBL.Ok(new { count = rows.Count, take, providerId = pid, apis = rows });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_get_api",
            "Get one API by settingParameterId or actionCode. Config sanitized (no credentials).")]
        public string GetApi(
            AgentToolContext context = null,
            [AgentParam("Setting parameter id")] int? settingParameterId = null,
            [AgentParam("ActionCode under DataIntegration")] string actionCode = null)
        {
            try
            {
                if (!TryResolveApiIds(context, settingParameterId, actionCode, out var sid, out var code, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                var dto = ApiIntegrationConfigBL.ResolveApi(sid, code, out err);
                if (dto == null) return ApiIntegrationConfigBL.Fail(err);
                return ApiIntegrationConfigBL.Ok(ApiIntegrationConfigBL.SummarizeApiDetail(dto));
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_upsert_api",
            "Create or update one API operation. dtoJson: actionCode, description|actionDescription (UI Description column), httpMethod, providerId, " +
            "apiConfig{apiUrl|baseUrl+url, method, headers, queryParams, pathParams}. " +
            "3rd-party HTTP REQUIRES providerId (>1) AND absolute apiUrl (or baseUrl+url) AND httpMethod — " +
            "empty shells are rejected. Prefer typed create_* for Data Model/Search/SP/Excel. " +
            "Preview → ask_user → confirmed=true.")]
        public string UpsertApi(
            AgentToolContext context = null,
            [AgentParam("Existing settingParameterId for update; omit to create.")] int? settingParameterId = null,
            [AgentParam("JSON patch / create fields", isRequired: true)] string dtoJson = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dtoJson))
                    return ApiIntegrationConfigBL.Fail("dtoJson is required.");
                JObject patch;
                try { patch = JObject.Parse(dtoJson); }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid dtoJson: " + ex.Message); }

                AppIntergrationSettingParameterExDto target;
                var isCreate = !(settingParameterId.HasValue && settingParameterId.Value > 0);
                if (!isCreate)
                {
                    target = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(
                        settingParameterId.Value, true);
                    if (target == null)
                        return ApiIntegrationConfigBL.Fail($"API {settingParameterId} not found.");
                }
                else
                {
                    // Do not default HttpMethd=Post — App typed tools set their own; 3rd-party must pass method.
                    target = new AppIntergrationSettingParameterExDto
                    {
                        MappingInternalCode = EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString()
                    };
                    if (ApiIntegrationConfigBL.TryGetInt(patch, out var patchPid,
                            "providerId", "intergrationSettingId", "IntergrationSettingId")
                        && patchPid > 0)
                    {
                        target.IntergrationSettingId = patchPid;
                    }
                    else
                    {
                        var hostPid = ReadHostInt(context, "providerId", "ProviderId");
                        if (hostPid.HasValue && hostPid.Value > 0)
                            target.IntergrationSettingId = hostPid.Value;
                    }
                }

                ApiIntegrationConfigBL.ApplyApiPatch(target, patch);

                if (isCreate)
                {
                    var bindErr = ResolveCreateProviderBinding(target, patch, context);
                    if (bindErr != null) return ApiIntegrationConfigBL.Fail(bindErr);
                }

                if (string.IsNullOrWhiteSpace(target.MappingInternalCode))
                    target.MappingInternalCode = EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString();

                // App Standard create without method → Post (legacy App default); 3rd-party validated below.
                if (isCreate
                    && string.IsNullOrWhiteSpace(target.HttpMethd)
                    && (target.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId
                    && !ApiIntegrationConfigBL.LooksLikeThirdPartyHttpOp(patch))
                {
                    target.HttpMethd = "Post";
                }

                ApiIntegrationConfigBL.FinalizeHttpApiConfig(target);

                var cfgErr = ApiIntegrationConfigBL.ValidateThirdPartyHttpConfig(target);
                if (cfgErr != null) return ApiIntegrationConfigBL.Fail(cfgErr);

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    var cfg = target.APIConfigParameters;
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        isCreate,
                        settingParameterId,
                        actionCode = target.ActionCode,
                        providerId = target.IntergrationSettingId,
                        httpMethod = target.HttpMethd,
                        apiUrl = ApiIntegrationConfigBL.JoinApiUrl(cfg?.BaseUrl, cfg?.Url),
                        queryParamKeys = cfg?.QueryParams?.Keys?.ToList(),
                        headerKeys = cfg?.Headers?.Keys?.Where(k => k != "CurrentUserSessionId").ToList(),
                        apiType = ApiIntegrationConfigBL.ResolveApiType(target)
                    });
                }

                if (string.IsNullOrWhiteSpace(target.ActionCode))
                    return ApiIntegrationConfigBL.Fail("actionCode is required.");
                if (!target.IntergrationSettingId.HasValue || target.IntergrationSettingId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("providerId (IntergrationSettingId) is required.");

                var result = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(target);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));

                var saved = result?.Object
                    ?? AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(target.Id, true);
                return ApiIntegrationConfigBL.Ok(new
                {
                    saved = true,
                    providerId = saved?.IntergrationSettingId ?? target.IntergrationSettingId,
                    api = ApiIntegrationConfigBL.SummarizeApiDetail(saved)
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        /// <summary>
        /// Bind create to the correct provider. Never silently park 3rd-party HTTP under App Provider (id=1).
        /// </summary>
        static string ResolveCreateProviderBinding(
            AppIntergrationSettingParameterExDto target,
            JObject patch,
            AgentToolContext context)
        {
            var pid = target.IntergrationSettingId ?? 0;
            var hostPid = ReadHostInt(context, "providerId", "ProviderId");
            var looks3p = ApiIntegrationConfigBL.LooksLikeThirdPartyHttpOp(patch);
            var looksApp = ApiIntegrationConfigBL.LooksLikeAppProviderOp(patch);

            if (pid <= 0)
            {
                if (looksApp)
                {
                    target.IntergrationSettingId = AppIntergrationSettingBL.AppBuiltInProviderId;
                    return null;
                }
                return "providerId is required in dtoJson when creating an API. " +
                       "For 3rd-party HTTP: call api_cfg_upsert_provider first, then pass the returned providerId. " +
                       "For App APIs prefer typed create_* tools (they bind to provider id=1).";
            }

            if (pid == AppIntergrationSettingBL.AppBuiltInProviderId && looks3p && !looksApp)
            {
                // HostContext on App API page often injects id=1 — refuse mis-filing external HTTP ops
                if (!hostPid.HasValue || hostPid.Value == AppIntergrationSettingBL.AppBuiltInProviderId)
                {
                    return "This looks like a 3rd-party HTTP API (apiConfig url/baseUrl) but providerId is 1 (App API Provider). " +
                           "Pass the 3rd-party providerId from api_cfg_upsert_provider. Do not attach external REST ops to App Provider.";
                }
            }

            return null;
        }

        [AgentTool("api_cfg_delete_api",
            "Delete one API by settingParameterId. Preview → ask_user → confirmed=true.")]
        public string DeleteApi(
            AgentToolContext context = null,
            [AgentParam("Setting parameter id", isRequired: true)] int? settingParameterId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (!settingParameterId.HasValue || settingParameterId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("settingParameterId is required.");
                var dto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(
                    settingParameterId.Value, false);
                if (dto == null) return ApiIntegrationConfigBL.Fail($"API {settingParameterId} not found.");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        settingParameterId,
                        actionCode = dto.ActionCode,
                        description = dto.ActionDescription
                    });
                }

                var result = AppIntergrationSettingBL.DeleteOneAppIntergrationSettingParameter(settingParameterId.Value);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));
                return ApiIntegrationConfigBL.Ok(new { deleted = true, settingParameterId });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_batch_delete_apis",
            "Batch delete APIs by idsJson array of settingParameterIds. Preview → ask_user → confirmed=true.")]
        public string BatchDeleteApis(
            AgentToolContext context = null,
            [AgentParam("JSON array of ints, e.g. [1,2,3]", isRequired: true)] string idsJson = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            _ = context;
            try
            {
                if (string.IsNullOrWhiteSpace(idsJson))
                    return ApiIntegrationConfigBL.Fail("idsJson is required.");
                List<int> ids;
                try
                {
                    ids = JArray.Parse(idsJson).Select(t => t.Value<int>()).Where(i => i > 0).Distinct().ToList();
                }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid idsJson: " + ex.Message); }
                if (ids.Count == 0) return ApiIntegrationConfigBL.Fail("idsJson has no valid ids.");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                    return ApiIntegrationConfigBL.Preview(new { ids, count = ids.Count });

                var req = new AppStoredProcedureApiBL.SpApiBatchDeleteRequest { Ids = ids };
                var result = AppStoredProcedureApiBL.BatchDelete(req);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));
                return ApiIntegrationConfigBL.Ok(new { deleted = true, ids, result = result?.Object });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_create_from_datamodel",
            "Idempotent: ensure one GET + one POST Data Model API for transactionId on App API Provider. " +
            "If APIs already exist for that transaction, returns them — does not insert duplicates. " +
            "Call once after confirm (do not parallel double-call). For recreate: delete old APIs first, then call once. " +
            "Preview → ask_user → confirmed=true.")]
        public string CreateFromDataModel(
            AgentToolContext context = null,
            [AgentParam("Transaction id", isRequired: true)] int? transactionId = null,
            [AgentParam("Optional app base URL for generated links")] string appBaseUrl = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            _ = context;
            try
            {
                if (!transactionId.HasValue || transactionId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("transactionId is required.");
                var tx = AppTransactionBL.RetrieveOneAppTransactionExDto(transactionId.Value);
                if (tx == null) return ApiIntegrationConfigBL.Fail($"Transaction {transactionId} not found.");

                var existing = ApiIntegrationConfigBL.FindExistingDataModelApis(transactionId.Value);
                var existingGet = existing.FirstOrDefault(a =>
                    string.Equals(a.HttpMethd, "Get", StringComparison.OrdinalIgnoreCase));
                var existingPost = existing.FirstOrDefault(a =>
                    string.Equals(a.HttpMethd, "Post", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(a.HttpMethd, "Put", StringComparison.OrdinalIgnoreCase));

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        transactionId,
                        transactionName = tx.TransactionName,
                        alreadyExists = existing.Count > 0,
                        existingApis = existing.Select(ApiIntegrationConfigBL.SummarizeApiBrief).ToList(),
                        note = existingGet != null && existingPost != null
                            ? "GET and POST already exist — confirmed call will return them (no new rows)."
                            : "Will ensure GET and POST Data Model APIs. Call this tool only once after confirm."
                    });
                }

                lock (ApiIntegrationConfigBL.EnterEnsureGate("datamodel:" + transactionId.Value))
                {
                    existing = ApiIntegrationConfigBL.FindExistingDataModelApis(transactionId.Value);
                    existingGet = existing.FirstOrDefault(a =>
                        string.Equals(a.HttpMethd, "Get", StringComparison.OrdinalIgnoreCase));
                    existingPost = existing.FirstOrDefault(a =>
                        string.Equals(a.HttpMethd, "Post", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(a.HttpMethd, "Put", StringComparison.OrdinalIgnoreCase));

                    if (existingGet != null && existingPost != null)
                    {
                        return ApiIntegrationConfigBL.Ok(new
                        {
                            created = false,
                            alreadyExisted = true,
                            transactionId,
                            message = "Data Model APIs already exist for this transaction; no new rows inserted.",
                            apis = existing.Select(ApiIntegrationConfigBL.SummarizeApiDetail).ToList()
                        });
                    }

                    // Any rows already present → do not call BL generate (it keys off TransactionApiSettings
                    // and can insert Get…1 / Post duplicates). Return what exists + missing methods.
                    if (existing.Count > 0)
                    {
                        var missing = new List<string>();
                        if (existingGet == null) missing.Add("Get");
                        if (existingPost == null) missing.Add("Post");
                        return ApiIntegrationConfigBL.Ok(new
                        {
                            created = false,
                            alreadyExisted = true,
                            partial = true,
                            missingHttpMethods = missing,
                            transactionId,
                            message = "Partial Data Model APIs already exist; skipped insert to avoid duplicates. "
                                + "Delete extras then call once, or upsert_api for missing method only.",
                            apis = existing.Select(ApiIntegrationConfigBL.SummarizeApiDetail).ToList()
                        });
                    }

                    string resolvedBase = ResolveCreateAppBaseUrl(context, appBaseUrl);
                    List<AppIntergrationSettingParameterExDto> created;
                    try
                    {
                        created = AppIntergrationSettingBL.GetAppTransactionDefaultProviderApi(
                            transactionId.Value, true, resolvedBase);
                    }
                    catch (Exception enrichEx)
                    {
                        // Sample GET URI/network failures must not fail the tool if rows were saved.
                        var partial = ApiIntegrationConfigBL.FindExistingDataModelApis(transactionId.Value);
                        if (partial.Count > 0)
                        {
                            return ApiIntegrationConfigBL.Ok(new
                            {
                                created = true,
                                alreadyExisted = false,
                                transactionId,
                                appBaseUrlUsed = resolvedBase,
                                sampleWarning = enrichEx.Message,
                                apis = partial.Select(ApiIntegrationConfigBL.SummarizeApiDetail).ToList()
                            });
                        }
                        throw;
                    }
                    var after = ApiIntegrationConfigBL.FindExistingDataModelApis(transactionId.Value);
                    if (after.Count == 0 && created != null)
                        after = created.Where(a => a != null).ToList();

                    return ApiIntegrationConfigBL.Ok(new
                    {
                        created = true,
                        alreadyExisted = false,
                        transactionId,
                        appBaseUrlUsed = resolvedBase,
                        apis = after.Select(ApiIntegrationConfigBL.SummarizeApiDetail).ToList()
                    });
                }
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_create_from_search",
            "Idempotent: ensure one Search/Presentation API for searchId on App API Provider. " +
            "If an API already exists for that search, returns it — no duplicate insert. " +
            "Call once after confirm. For recreate: delete old API first, then call once. " +
            "Preview → ask_user → confirmed=true.")]
        public string CreateFromSearch(
            AgentToolContext context = null,
            [AgentParam("Search id", isRequired: true)] int? searchId = null,
            [AgentParam("Optional app base URL")] string appBaseUrl = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            _ = context;
            try
            {
                if (!searchId.HasValue || searchId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("searchId is required.");
                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId.Value);
                if (search == null) return ApiIntegrationConfigBL.Fail($"Search {searchId} not found.");

                var existing = ApiIntegrationConfigBL.FindExistingSearchApi(searchId.Value);

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        searchId,
                        searchName = search.Name,
                        alreadyExists = existing != null,
                        existingApi = existing == null ? null : ApiIntegrationConfigBL.SummarizeApiBrief(existing),
                        note = existing != null
                            ? "Search API already exists — confirmed call will return it (no new row)."
                            : "Will ensure App Data Presentation API. Call this tool only once after confirm."
                    });
                }

                lock (ApiIntegrationConfigBL.EnterEnsureGate("search:" + searchId.Value))
                {
                    existing = ApiIntegrationConfigBL.FindExistingSearchApi(searchId.Value);
                    if (existing != null)
                    {
                        return ApiIntegrationConfigBL.Ok(new
                        {
                            created = false,
                            alreadyExisted = true,
                            searchId,
                            message = "Search API already exists for this searchId; no new row inserted.",
                            api = ApiIntegrationConfigBL.SummarizeApiDetail(existing)
                        });
                    }

                    string resolvedBase = ResolveCreateAppBaseUrl(context, appBaseUrl);
                    AppIntergrationSettingParameterExDto created;
                    try
                    {
                        created = AppIntergrationSettingBL.GetAppSearchDefaultProviderApi(
                            searchId.Value, true, resolvedBase);
                    }
                    catch (Exception enrichEx)
                    {
                        var partial = ApiIntegrationConfigBL.FindExistingSearchApi(searchId.Value);
                        if (partial != null)
                        {
                            return ApiIntegrationConfigBL.Ok(new
                            {
                                created = true,
                                alreadyExisted = false,
                                searchId,
                                appBaseUrlUsed = resolvedBase,
                                sampleWarning = enrichEx.Message,
                                api = ApiIntegrationConfigBL.SummarizeApiDetail(partial)
                            });
                        }
                        throw;
                    }
                    if (created == null)
                        return ApiIntegrationConfigBL.Fail("Failed to create/retrieve Search API.");

                    // Prefer re-read in case parallel call already inserted
                    var after = ApiIntegrationConfigBL.FindExistingSearchApi(searchId.Value) ?? created;
                    return ApiIntegrationConfigBL.Ok(new
                    {
                        created = true,
                        alreadyExisted = false,
                        searchId,
                        appBaseUrlUsed = resolvedBase,
                        api = ApiIntegrationConfigBL.SummarizeApiDetail(after)
                    });
                }
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_create_sql_query",
            "Create or update a SQL JSON Query API (IsSimpleQuery) on App API Provider. " +
            "dtoJson: actionCode, actionDescription, jsonQuery, dataSourceId, whereClauseFormat, httpMethod. " +
            "Optional settingParameterId to update. Preview → ask_user → confirmed=true.")]
        public string CreateSqlQuery(
            AgentToolContext context = null,
            [AgentParam("JSON fields", isRequired: true)] string dtoJson = null,
            [AgentParam("Update existing id")] int? settingParameterId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dtoJson))
                    return ApiIntegrationConfigBL.Fail("dtoJson is required.");
                JObject patch;
                try { patch = JObject.Parse(dtoJson); }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid dtoJson: " + ex.Message); }

                patch["isSimpleQuery"] = true;
                if (patch["providerId"] == null && patch["IntergrationSettingId"] == null)
                    patch["providerId"] = AppIntergrationSettingBL.AppBuiltInProviderId;

                return UpsertApi(context, settingParameterId, patch.ToString(Formatting.None), confirmed, dryRun);
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_create_tx_simple_query",
            "Generate Transaction Provide Simple Query API for transactionId. Preview → ask_user → confirmed=true.")]
        public string CreateTxSimpleQuery(
            AgentToolContext context = null,
            [AgentParam("Transaction id", isRequired: true)] int? transactionId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            _ = context;
            try
            {
                if (!transactionId.HasValue || transactionId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("transactionId is required.");
                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                    return ApiIntegrationConfigBL.Preview(new { transactionId });

                var result = AppIntergrationSettingBL.GenerateTransactionProvideSimpleQueryAPI(transactionId);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));
                return ApiIntegrationConfigBL.Ok(new
                {
                    created = true,
                    api = ApiIntegrationConfigBL.SummarizeApiDetail(result?.Object)
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_list_sp_catalog",
            "List stored procedures available for SP API builder on a data source.")]
        public string ListSpCatalog(
            AgentToolContext context = null,
            [AgentParam("DataSourceRegister / fixture id", isRequired: true)] int? dataSourceId = null,
            [AgentParam("Optional schema filter")] string schema = null,
            [AgentParam("Max rows (default 100, max 500).")] int take = 100)
        {
            _ = context;
            try
            {
                if (!dataSourceId.HasValue || dataSourceId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("dataSourceId is required.");
                take = Math.Clamp(take <= 0 ? 100 : take, 1, 500);
                var list = AppStoredProcedureApiBL.ListProceduresForApiBuilder(dataSourceId.Value, schema, take);
                var items = list.Select(p => new
                {
                    p.Schema,
                    p.Name,
                    p.FullName,
                    p.SuggestedActionCode,
                    p.DbComment,
                    parameterCount = p.Parameters?.Count ?? 0
                }).ToList();
                return ApiIntegrationConfigBL.Ok(new { count = items.Count, dataSourceId, procedures = items });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_create_from_sp_batch",
            "Batch-create Stored Procedure APIs. requestJson matches SpApiCreateRequest: " +
            "{ dataSourceId, generateAiDescription?, captureSampleOnGenerate?, items:[{ schema, spName, actionCode?, actionDescription?, parameters? }] }. " +
            "Preview → ask_user → confirmed=true.")]
        public string CreateFromSpBatch(
            AgentToolContext context = null,
            [AgentParam("SpApiCreateRequest JSON", isRequired: true)] string requestJson = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            _ = context;
            try
            {
                if (string.IsNullOrWhiteSpace(requestJson))
                    return ApiIntegrationConfigBL.Fail("requestJson is required.");
                AppStoredProcedureApiBL.SpApiCreateRequest request;
                try
                {
                    request = JsonConvert.DeserializeObject<AppStoredProcedureApiBL.SpApiCreateRequest>(requestJson);
                }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid requestJson: " + ex.Message); }

                if (request == null || request.DataSourceId <= 0 || request.Items == null || request.Items.Count == 0)
                    return ApiIntegrationConfigBL.Fail("requestJson needs dataSourceId and items[].");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        dataSourceId = request.DataSourceId,
                        itemCount = request.Items.Count,
                        items = request.Items.Select(i => new
                        {
                            i.Schema,
                            i.SpName,
                            i.ActionCode,
                            i.Description
                        }).Take(40).ToList()
                    });
                }

                var result = AppStoredProcedureApiBL.BatchCreate(request);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));
                return ApiIntegrationConfigBL.Ok(new { created = true, result = result?.Object });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_create_excel_import",
            "Create or update Excel Import Update Data API. dtoJson must include actionCode and " +
            "apiConfig.excelDataImportDataSetId (plus optional mapping fields). Preview → ask_user → confirmed=true.")]
        public string CreateExcelImport(
            AgentToolContext context = null,
            [AgentParam("JSON fields", isRequired: true)] string dtoJson = null,
            [AgentParam("Update existing id")] int? settingParameterId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dtoJson))
                    return ApiIntegrationConfigBL.Fail("dtoJson is required.");
                JObject patch;
                try { patch = JObject.Parse(dtoJson); }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid dtoJson: " + ex.Message); }

                if (patch["providerId"] == null && patch["IntergrationSettingId"] == null)
                    patch["providerId"] = AppIntergrationSettingBL.AppBuiltInProviderId;

                var cfg = patch["apiConfig"] as JObject ?? patch["APIConfigParameters"] as JObject;
                if (cfg == null)
                {
                    cfg = new JObject();
                    patch["apiConfig"] = cfg;
                }
                if (cfg["excelDataImportDataSetId"] == null && cfg["ExcelDataImportDataSetId"] == null)
                    return ApiIntegrationConfigBL.Fail("apiConfig.excelDataImportDataSetId is required.");

                return UpsertApi(context, settingParameterId, patch.ToString(Formatting.None), confirmed, dryRun);
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_test_api",
            "Test an API with stored test parameters (UI Send Request). isSimulate=true for dry simulate when supported. " +
            "Mutating APIs: ask_user then confirmed=true.")]
        public string TestApi(
            AgentToolContext context = null,
            [AgentParam("Setting parameter id")] int? settingParameterId = null,
            [AgentParam("Or ActionCode")] string actionCode = null,
            [AgentParam("Simulate only")] bool isSimulate = false,
            [AgentParam("true after ask_user for non-simulate")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (!TryResolveApiIds(context, settingParameterId, actionCode, out var sid, out var code, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                var dto = ApiIntegrationConfigBL.ResolveApi(sid, code, out err);
                if (dto == null) return ApiIntegrationConfigBL.Fail(err);
                var id = Convert.ToInt32(dto.Id);

                if (!isSimulate && ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        settingParameterId = id,
                        actionCode = dto.ActionCode,
                        httpMethod = dto.HttpMethd,
                        note = "Will execute with saved test parameters. Pass isSimulate=true to skip confirm when only simulating."
                    });
                }

                var result = AppIntergrationSettingBL.ExecuteOneOperationWithTestParameters(id, isSimulate);
                if (result?.ValidationResult?.HasErrors == true)
                    return ApiIntegrationConfigBL.Fail(ApiIntegrationConfigBL.ValidationMessages(result.ValidationResult));
                return ApiIntegrationConfigBL.Ok(new
                {
                    executed = true,
                    isSimulate,
                    settingParameterId = id,
                    actionCode = dto.ActionCode,
                    success = result?.Object
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("api_cfg_api_where_used",
            "Find Transactions and Searches that reference an API (by settingParameterId).")]
        public string ApiWhereUsed(
            AgentToolContext context = null,
            [AgentParam("API settingParameterId", isRequired: true)] int? settingParameterId = null)
        {
            _ = context;
            try
            {
                if (!settingParameterId.HasValue || settingParameterId.Value <= 0)
                    return ApiIntegrationConfigBL.Fail("settingParameterId is required.");
                var txs = AppIntergrationSettingBL.RetrieveApiWhereUsedOnTransactions(settingParameterId)
                    ?? new List<AppTransactionDto>();
                var searches = AppIntergrationSettingBL.RetrieveApiWhereUsedOnSearches(settingParameterId)
                    ?? new List<AppSearchDto>();
                return ApiIntegrationConfigBL.Ok(new
                {
                    settingParameterId,
                    transactions = txs.Select(t => new { id = t.Id, name = t.TransactionName }).ToList(),
                    searches = searches.Select(s => new { id = s.Id, name = s.Name }).ToList()
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        // ── OpenAPI documents ──────────────────────────────────────────────────

        [AgentTool("openapi_list", "List OpenAPI (Swagger) documents (no JSON body).")]
        public string OpenApiList(AgentToolContext context = null)
        {
            _ = context;
            try
            {
                var list = AppOpenApiDocumentBL.List() ?? new List<AppOpenApiDocumentBL.DocumentDto>();
                var items = list.Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Code,
                    d.Version,
                    d.Status,
                    d.ApiCount,
                    d.LastGenerated,
                    d.IsUsedByMcp,
                    description = ApiIntegrationConfigBL.Truncate(d.Description, 240)
                }).ToList();
                return ApiIntegrationConfigBL.Ok(new { count = items.Count, documents = items });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("openapi_get",
            "Get one OpenAPI document by id (or HostContext openApiDocId). includeJson=true returns truncated OpenApiJson.")]
        public string OpenApiGet(
            AgentToolContext context = null,
            [AgentParam("Document id")] int? documentId = null,
            [AgentParam("Include OpenApiJson (truncated)")] bool includeJson = false)
        {
            try
            {
                if (!TryResolveDocId(context, documentId, out var id, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                var doc = AppOpenApiDocumentBL.Get(id);
                if (doc == null) return ApiIntegrationConfigBL.Fail($"OpenAPI document {id} not found.");
                return ApiIntegrationConfigBL.Ok(new
                {
                    doc.Id,
                    doc.Name,
                    doc.Code,
                    doc.Version,
                    doc.Description,
                    doc.Status,
                    doc.ApiCount,
                    doc.LastGenerated,
                    doc.IsUsedByMcp,
                    members = doc.Members,
                    openApiJson = includeJson ? ApiIntegrationConfigBL.Truncate(doc.OpenApiJson, 8000) : null
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("openapi_list_selectable",
            "List ActionCodes selectable for an OpenAPI document. mcpOnly=true filters MCP-marked APIs.")]
        public string OpenApiListSelectable(
            AgentToolContext context = null,
            [AgentParam("Filter to IsUsedByMcp APIs")] bool mcpOnly = false)
        {
            _ = context;
            try
            {
                var list = AppOpenApiDocumentBL.ListSelectableApis(mcpOnly)
                    ?? new List<AppOpenApiDocumentBL.SelectableApiDto>();
                return ApiIntegrationConfigBL.Ok(new
                {
                    count = list.Count,
                    mcpOnly,
                    apis = list.Select(a => new
                    {
                        a.Id,
                        a.ActionCode,
                        a.Description,
                        a.ApiType,
                        a.HttpMethod,
                        a.ProviderName,
                        a.ProviderKind,
                        a.IsUsedByMcp,
                        a.Application,
                        a.DataModelName
                    }).ToList()
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("openapi_upsert",
            "Create/update OpenAPI document. dtoJson: id?, name, code, version, description, isUsedByMcp, actionCodes[]. " +
            "Preview → ask_user → confirmed=true.")]
        public string OpenApiUpsert(
            AgentToolContext context = null,
            [AgentParam("SaveRequest JSON", isRequired: true)] string dtoJson = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            _ = context;
            try
            {
                if (string.IsNullOrWhiteSpace(dtoJson))
                    return ApiIntegrationConfigBL.Fail("dtoJson is required.");
                AppOpenApiDocumentBL.SaveRequest request;
                try
                {
                    request = JsonConvert.DeserializeObject<AppOpenApiDocumentBL.SaveRequest>(dtoJson);
                }
                catch (Exception ex) { return ApiIntegrationConfigBL.Fail("Invalid dtoJson: " + ex.Message); }
                if (request == null || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
                    return ApiIntegrationConfigBL.Fail("dtoJson needs name and code.");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                {
                    return ApiIntegrationConfigBL.Preview(new
                    {
                        request.Id,
                        request.Name,
                        request.Code,
                        request.Version,
                        memberCount = request.ActionCodes?.Count ?? 0,
                        actionCodes = request.ActionCodes?.Take(40)
                    });
                }

                var saved = AppOpenApiDocumentBL.Save(request);
                return ApiIntegrationConfigBL.Ok(new
                {
                    saved = true,
                    document = new
                    {
                        saved.Id,
                        saved.Name,
                        saved.Code,
                        saved.Version,
                        saved.Status,
                        saved.ApiCount,
                        saved.RemovedActionCodes,
                        members = saved.Members
                    }
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("openapi_delete",
            "Delete an OpenAPI document. Preview → ask_user → confirmed=true.")]
        public string OpenApiDelete(
            AgentToolContext context = null,
            [AgentParam("Document id", isRequired: true)] int? documentId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (!TryResolveDocId(context, documentId, out var id, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                var doc = AppOpenApiDocumentBL.Get(id);
                if (doc == null) return ApiIntegrationConfigBL.Fail($"OpenAPI document {id} not found.");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                    return ApiIntegrationConfigBL.Preview(new { documentId = id, doc.Name, doc.Code });

                AppOpenApiDocumentBL.Delete(id);
                return ApiIntegrationConfigBL.Ok(new { deleted = true, documentId = id });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("openapi_set_published",
            "Publish or unpublish an OpenAPI document. Preview → ask_user → confirmed=true.")]
        public string OpenApiSetPublished(
            AgentToolContext context = null,
            [AgentParam("Document id", isRequired: true)] int? documentId = null,
            [AgentParam("true = Published, false = Draft", isRequired: true)] bool? published = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (!TryResolveDocId(context, documentId, out var id, out var err))
                    return ApiIntegrationConfigBL.Fail(err);
                if (!published.HasValue)
                    return ApiIntegrationConfigBL.Fail("published is required.");

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                    return ApiIntegrationConfigBL.Preview(new { documentId = id, published });

                var doc = AppOpenApiDocumentBL.SetPublished(id, published.Value);
                return ApiIntegrationConfigBL.Ok(new
                {
                    saved = true,
                    documentId = doc.Id,
                    doc.Status,
                    doc.Code
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("openapi_regenerate",
            "Regenerate OpenAPI JSON from current members. Preview → ask_user → confirmed=true.")]
        public string OpenApiRegenerate(
            AgentToolContext context = null,
            [AgentParam("Document id", isRequired: true)] int? documentId = null,
            [AgentParam("true after ask_user")] bool? confirmed = null,
            [AgentParam("true = preview only")] bool? dryRun = null)
        {
            try
            {
                if (!TryResolveDocId(context, documentId, out var id, out var err))
                    return ApiIntegrationConfigBL.Fail(err);

                if (ApiIntegrationConfigBL.NeedsConfirm(confirmed, dryRun))
                    return ApiIntegrationConfigBL.Preview(new { documentId = id, note = "Rebuilds OpenApiJson from members." });

                var result = AppOpenApiDocumentBL.Regenerate(id);
                return ApiIntegrationConfigBL.Ok(new
                {
                    regenerated = true,
                    documentId = result?.Document?.Id,
                    status = result?.Document?.Status,
                    apiCount = result?.Document?.ApiCount,
                    skipped = result?.Skipped,
                    NextStep = "Call request_api_mgmt_reload when hosted in API MANAGEMENT."
                });
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        [AgentTool("request_api_mgmt_reload",
            "ONLY when HostContext.surface=api-management (API MANAGEMENT Configurator dock). " +
            "After durable API writes, signal the host Reload prompt. " +
            "If not in that dock (New Chat / no surface), do NOT call — or call is a soft no-op success.")]
        public async Task<string> RequestApiMgmtReload(AgentToolContext context = null)
        {
            // Soft no-op outside API MANAGEMENT dock (New Chat has no OnHostTool / wrong host).
            string surface = ReadHostString(context, "surface", "Surface");
            bool looksLikeApiDock = string.Equals(surface, "api-management", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ReadHostString(context, "mode", "Mode"), "api-configurator", StringComparison.OrdinalIgnoreCase);

            var callbacks = AgentHitlBridge.Resolve(context?.ChatSessionKey);
            if (callbacks?.OnHostTool == null || !looksLikeApiDock)
            {
                return ApiIntegrationConfigBL.Ok(new
                {
                    skipped = true,
                    reloadRequested = false,
                    message = "Reload skipped — open the API MANAGEMENT Configurator dock (HostContext.surface=api-management) if the list must refresh."
                });
            }

            var result = await AskHost(context, "request_api_mgmt_reload", new { }).ConfigureAwait(false);
            // Host stub may return "open transaction form" when dock handler missing — treat as soft skip.
            if (!string.IsNullOrWhiteSpace(result)
                && result.IndexOf("only runs inside", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ApiIntegrationConfigBL.Ok(new
                {
                    skipped = true,
                    reloadRequested = false,
                    message = "Reload skipped — API MANAGEMENT dock host tool not wired for this chat."
                });
            }
            return result;
        }

        // ── helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tool arg → HostContext.serverRoot → AgentToolContext.AppBaseUrl (captured on request thread).
        /// Replaces bare http://localhost (no port) when a better root is available.
        /// </summary>
        static string ResolveCreateAppBaseUrl(AgentToolContext context, string appBaseUrlFromTool)
        {
            string fromHost = ReadHostString(context, "serverRoot", "ServerRoot", "appBaseUrl", "AppBaseUrl");
            string preferred = !string.IsNullOrWhiteSpace(fromHost)
                ? fromHost
                : (context?.AppBaseUrl ?? "");
            return AppIntergrationSettingBL.ResolveAppBaseUrl(appBaseUrlFromTool, preferred);
        }

        static async Task<string> AskHost(AgentToolContext context, string toolName, object args)
        {
            var callbacks = AgentHitlBridge.Resolve(context?.ChatSessionKey);
            if (callbacks?.OnHostTool == null)
                return ApiIntegrationConfigBL.Fail("This tool only runs inside an open API MANAGEMENT Configurator dock.");

            var evt = new FormHostToolEvent
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                ArgsJson = JsonConvert.SerializeObject(args)
            };
            try
            {
                var result = await callbacks.OnHostTool(evt).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(result)
                    ? ApiIntegrationConfigBL.Fail("Empty host result.")
                    : result;
            }
            catch (Exception ex) { return ApiIntegrationConfigBL.Fail(ex.Message); }
        }

        static IEnumerable<object> FilterApis(AppIntergrationSettingExDto p, string query)
        {
            if (p?.AppIntergrationSettingParameterList == null)
                yield break;
            foreach (var a in p.AppIntergrationSettingParameterList)
            {
                if (a == null) continue;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var q = query.Trim();
                    var blob = $"{a.ActionCode} {a.ActionDescription} {a.APIType} {a.HttpMethd}";
                    if (blob.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                yield return ApiIntegrationConfigBL.SummarizeApiBrief(a);
            }
        }

        static bool TryResolveProviderId(AgentToolContext context, int? clientId, out int id, out string error)
        {
            id = 0;
            error = null;
            var hostId = ReadHostInt(context, "providerId", "ProviderId");
            if (hostId.HasValue && hostId.Value > 0)
            {
                if (clientId.HasValue && clientId.Value > 0 && clientId.Value != hostId.Value)
                {
                    error = $"providerId {clientId} does not match HostContext providerId {hostId}.";
                    return false;
                }
                id = hostId.Value;
                return true;
            }
            if (clientId.HasValue && clientId.Value > 0)
            {
                id = clientId.Value;
                return true;
            }
            error = "providerId is required (or open a provider-scoped host).";
            return false;
        }

        static bool TryResolveDocId(AgentToolContext context, int? clientId, out int id, out string error)
        {
            id = 0;
            error = null;
            var hostId = ReadHostInt(context, "openApiDocId", "OpenApiDocId", "documentId", "DocumentId");
            if (hostId.HasValue && hostId.Value > 0)
            {
                if (clientId.HasValue && clientId.Value > 0 && clientId.Value != hostId.Value)
                {
                    error = $"documentId {clientId} does not match HostContext openApiDocId {hostId}.";
                    return false;
                }
                id = hostId.Value;
                return true;
            }
            if (clientId.HasValue && clientId.Value > 0)
            {
                id = clientId.Value;
                return true;
            }
            error = "documentId is required.";
            return false;
        }

        static bool TryResolveApiIds(
            AgentToolContext context,
            int? settingParameterId,
            string actionCode,
            out int? sid,
            out string code,
            out string error)
        {
            sid = settingParameterId;
            code = actionCode;
            error = null;

            var hostSid = ReadHostInt(context, "settingParameterId", "SettingParameterId");
            var hostCode = ReadHostString(context, "actionCode", "ActionCode");

            if (hostSid.HasValue && hostSid.Value > 0)
            {
                if (settingParameterId.HasValue && settingParameterId.Value > 0
                    && settingParameterId.Value != hostSid.Value)
                {
                    error = "settingParameterId does not match HostContext.";
                    return false;
                }
                sid = hostSid;
            }
            if (!string.IsNullOrWhiteSpace(hostCode))
            {
                if (!string.IsNullOrWhiteSpace(actionCode)
                    && !string.Equals(actionCode.Trim(), hostCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    error = "actionCode does not match HostContext.";
                    return false;
                }
                code = hostCode;
            }

            if ((!sid.HasValue || sid.Value <= 0) && string.IsNullOrWhiteSpace(code))
            {
                error = "Provide settingParameterId or actionCode.";
                return false;
            }
            return true;
        }

        static int? ReadHostInt(AgentToolContext context, params string[] keys)
        {
            var root = ReadHostRoot(context);
            if (root == null) return null;
            foreach (var k in keys)
            {
                var t = root[k];
                if (t == null || t.Type == JTokenType.Null) continue;
                if (t.Type == JTokenType.Integer) return t.Value<int>();
                if (int.TryParse(t.ToString(), out var v) && v > 0) return v;
            }
            return null;
        }

        static string ReadHostString(AgentToolContext context, params string[] keys)
        {
            var root = ReadHostRoot(context);
            if (root == null) return null;
            foreach (var k in keys)
            {
                var t = root[k];
                if (t == null || t.Type == JTokenType.Null) continue;
                var s = t.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            }
            return null;
        }

        static JObject ReadHostRoot(AgentToolContext context)
        {
            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            if (string.IsNullOrWhiteSpace(hostJson)) return null;
            try { return JObject.Parse(hostJson); }
            catch { return null; }
        }
    }
}
