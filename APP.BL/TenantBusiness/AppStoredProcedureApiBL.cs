using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL.AIAgent.GenericAgent.StoredProcedure;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL
{
    /// <summary>
    /// Batch create / delete App API Provider operations that wrap stored procedures.
    /// </summary>
    public static class AppStoredProcedureApiBL
    {
        public const string ApiTypeDisplayName = "Stored Procedure API";

        public class SpApiParameterItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Direction { get; set; }
            public int? MaxLength { get; set; }
            public int Ordinal { get; set; }
            public bool HasDefault { get; set; }
            public string DefaultValue { get; set; }
        }

        public class SpApiCreateItem
        {
            public string Schema { get; set; }
            public string SpName { get; set; }
            public string ActionCode { get; set; }
            public string Description { get; set; }
            public bool CaptureSample { get; set; }
            public List<SpApiParameterItem> Parameters { get; set; }
        }

        public class SpApiCreateRequest
        {
            public int DataSourceId { get; set; }
            public List<SpApiCreateItem> Items { get; set; }
        }

        public class SpApiBatchDeleteRequest
        {
            public List<int> Ids { get; set; }
        }

        public class SpCatalogListItemDto
        {
            public string Schema { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Description { get; set; }
            public string SuggestedActionCode { get; set; }
            public List<SpApiParameterItem> Parameters { get; set; }
        }

        public static bool IsStoredProcedureApi(AppIntergrationSettingParameterExDto dto)
        {
            return dto?.APIConfigParameters != null && dto.APIConfigParameters.IsStoredProcedureApi;
        }

        public static List<SpCatalogListItemDto> ListProceduresForApiBuilder(int dataSourceId, string schema = null, int take = 500)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
            // Reuse catalog list via public ListJson parse, or call internal through Detail.
            var raw = StoredProcedureCatalogBL.ListJson(dataSourceId, schema, 0, Math.Clamp(take, 1, 500));
            var jo = JObject.Parse(raw);
            if (jo["ok"]?.Value<bool>() != true)
                throw new InvalidOperationException(jo["error"]?.ToString() ?? "Failed to list stored procedures.");

            var result = new List<SpCatalogListItemDto>();
            var procs = jo["procedures"] as JArray;
            if (procs == null) return result;

            foreach (var p in procs)
            {
                var sch = p["Schema"]?.ToString() ?? p["schema"]?.ToString();
                var name = p["Name"]?.ToString() ?? p["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var paramMeta = StoredProcedureCatalogBL.LoadParameters(fixture, engine, sch, name);
                var parameters = paramMeta.Select(ToApiParamWithDefault).ToList();

                result.Add(new SpCatalogListItemDto
                {
                    Schema = sch,
                    Name = name,
                    FullName = p["FullName"]?.ToString() ?? StoredProcedureCatalogBL.FormatFull(sch, name),
                    Description = p["Description"]?.ToString(),
                    SuggestedActionCode = BuildDefaultActionCode(name),
                    Parameters = parameters,
                });
            }

            return result;
        }

        public static OperationCallResult<object> BatchCreate(SpApiCreateRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request == null || request.DataSourceId <= 0 || request.Items == null || request.Items.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_BatchCreate_Invalid", ValidationItemType.Error, "DataSourceId and at least one item are required."));
                return result;
            }

            AppIntergrationSettingBL.EnsureAppBuiltInProviderExists();

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(request.DataSourceId);
            var engine = (fixture.SqlServerType ?? EmSqlType.SqlServer).ToString();

            var created = new List<object>();
            var warnings = new List<string>();

            foreach (var item in request.Items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.SpName))
                {
                    warnings.Add("Skipped item with empty SpName.");
                    continue;
                }

                var actionCode = string.IsNullOrWhiteSpace(item.ActionCode)
                    ? BuildDefaultActionCode(item.SpName)
                    : item.ActionCode.Trim();

                var parameters = (item.Parameters ?? new List<SpApiParameterItem>())
                    .Select(NormalizeParam)
                    .ToList();

                var apiConfig = new APIConfigParameterDTO
                {
                    IsStoredProcedureApi = true,
                    SpName = item.SpName.Trim(),
                    SpSchema = string.IsNullOrWhiteSpace(item.Schema) ? null : item.Schema.Trim(),
                    SpEngine = engine,
                    Method = EnumHttpMethod.Post,
                    SpParameters = parameters.Select(p => new StoredProcedureApiParameterDTO
                    {
                        Name = p.Name,
                        Type = p.Type,
                        Direction = p.Direction,
                        MaxLength = p.MaxLength,
                        Ordinal = p.Ordinal,
                        HasDefault = p.HasDefault,
                        DefaultValue = p.DefaultValue,
                    }).ToList(),
                };

                var dto = new AppIntergrationSettingParameterExDto
                {
                    IntergrationSettingId = AppIntergrationSettingBL.AppBuiltInProviderId,
                    IsSimpleQuery = false,
                    HttpMethd = "Post",
                    DataSourceId = request.DataSourceId,
                    ActionCode = actionCode,
                    ActionDescription = string.IsNullOrWhiteSpace(item.Description)
                        ? $"Stored procedure {StoredProcedureCatalogBL.FormatFull(item.Schema, item.SpName)}"
                        : item.Description.Trim(),
                    JsonQuery = StoredProcedureCatalogBL.FormatFull(item.Schema, item.SpName),
                    MappingInternalCode = EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString(),
                    APIConfigParameters = apiConfig,
                    ApiconfigParameters = JsonConvert.SerializeObject(apiConfig),
                };

                if (item.CaptureSample)
                {
                    try
                    {
                        dto.JsonSampleData = CaptureSampleJson(request.DataSourceId, item.Schema, item.SpName, parameters);
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"{actionCode}: sample capture failed — {ex.Message}");
                    }
                }

                var saveResult = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
                validation.Merge(saveResult.ValidationResult);
                if (saveResult.IsSuccessfulWithResult)
                {
                    created.Add(new
                    {
                        Id = saveResult.Object.Id,
                        ActionCode = saveResult.Object.ActionCode,
                        CapturedSample = item.CaptureSample && !string.IsNullOrWhiteSpace(saveResult.Object.JsonSampleData),
                    });
                }
                else
                {
                    warnings.Add($"{actionCode}: save failed.");
                }
            }

            foreach (var w in warnings)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_BatchCreate_Warning", ValidationItemType.Warning, w));
            }

            result.Object = new { createdCount = created.Count, created, warnings };
            return result;
        }

        public static OperationCallResult<object> BatchDelete(SpApiBatchDeleteRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request?.Ids == null || request.Ids.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_BatchDelete_Invalid", ValidationItemType.Error, "Ids are required."));
                return result;
            }

            var deleted = 0;
            foreach (var id in request.Ids.Distinct())
            {
                var one = AppIntergrationSettingBL.DeleteOneAppIntergrationSettingParameter(id);
                validation.Merge(one.ValidationResult);
                if (!one.ValidationResult.HasErrors)
                    deleted++;
            }

            result.Object = new { deletedCount = deleted };
            return result;
        }

        /// <summary>
        /// Merge request args over stored defaults for SP invoke.
        /// </summary>
        public static string BuildArgsJsonFromDefaultsAndBody(AppIntergrationSettingParameterExDto setting, string requestBody)
        {
            var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (setting?.APIConfigParameters?.SpParameters != null)
            {
                foreach (var p in setting.APIConfigParameters.SpParameters)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
                    var bare = p.Name.TrimStart('@', ':');
                    if (IsOutputOnly(p.Direction) && string.IsNullOrEmpty(p.DefaultValue))
                        continue;
                    if (p.HasDefault && (p.DefaultValue == null))
                        continue;
                    map[bare] = ParseDefaultToken(p.DefaultValue);
                }
            }

            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                try
                {
                    var jo = JObject.Parse(requestBody);
                    var args = jo["args"] as JObject ?? jo;
                    foreach (var prop in args.Properties())
                    {
                        map[prop.Name.TrimStart('@', ':')] = prop.Value?.Type == JTokenType.Null
                            ? null
                            : prop.Value?.ToObject<object>();
                    }
                }
                catch
                {
                    // keep defaults only
                }
            }

            return JsonConvert.SerializeObject(map);
        }

        public static string BuildDefaultActionCode(string spName)
        {
            var bare = (spName ?? "Proc").Trim();
            var idx = bare.LastIndexOf('.');
            if (idx >= 0 && idx < bare.Length - 1) bare = bare.Substring(idx + 1);
            bare = Regex.Replace(bare, @"[^0-9a-zA-Z]+", "_");
            if (string.IsNullOrWhiteSpace(bare)) bare = "Proc";
            return "AppSp_" + bare;
        }

        public static string PlaceholderDefault(string type, bool hasDefault)
        {
            // When SP declares a default, leave null so execute can omit and let the engine apply it.
            if (hasDefault) return null;

            var t = (type ?? "").ToLowerInvariant();
            if (t.Contains("int") || t.Contains("decimal") || t.Contains("numeric") || t.Contains("float")
                || t.Contains("real") || t.Contains("money") || t.Contains("number") || t.Contains("double"))
                return "0";
            if (t.Contains("bit") || t.Contains("bool"))
                return "false";
            if (t.Contains("date") || t.Contains("time"))
                return "";
            if (t.Contains("char") || t.Contains("text") || t.Contains("xml") || t.Contains("json") || t.Contains("clob") || t.Contains("uuid") || t.Contains("uniqueidentifier"))
                return "";
            return null;
        }

        private static SpApiParameterItem ToApiParamWithDefault(StoredProcedureParameterDto p)
        {
            var item = new SpApiParameterItem
            {
                Name = p.Name,
                Type = p.Type,
                Direction = p.Direction,
                MaxLength = p.MaxLength,
                Ordinal = p.Ordinal,
                HasDefault = p.HasDefault,
            };
            item.DefaultValue = PlaceholderDefault(p.Type, p.HasDefault);
            return item;
        }

        private static SpApiParameterItem NormalizeParam(SpApiParameterItem p)
        {
            if (p == null) return new SpApiParameterItem();
            if (p.DefaultValue == null && !p.HasDefault)
                p.DefaultValue = PlaceholderDefault(p.Type, false);
            return p;
        }

        private static string CaptureSampleJson(int dataSourceId, string schema, string spName, List<SpApiParameterItem> parameters)
        {
            var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parameters ?? new List<SpApiParameterItem>())
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
                if (IsOutputOnly(p.Direction) && p.DefaultValue == null) continue;
                if (p.HasDefault && p.DefaultValue == null) continue;
                args[p.Name.TrimStart('@', ':')] = ParseDefaultToken(p.DefaultValue);
            }

            return StoredProcedureExecuteBL.ExecuteJson(
                dataSourceId,
                spName,
                schema,
                JsonConvert.SerializeObject(args));
        }

        private static bool IsOutputOnly(string direction)
        {
            var d = (direction ?? "IN").ToUpperInvariant();
            return d == "OUT" || d == "OUTPUT";
        }

        private static object ParseDefaultToken(string value)
        {
            if (value == null) return null;
            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (long.TryParse(value, out var l)) return l;
            if (decimal.TryParse(value, out var d)) return d;
            return value;
        }
    }
}
