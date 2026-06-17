using System;
using System.Collections.Generic;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Plugin;
using APP.TechPack.Services;

namespace APP.TechPack;

/// <summary>
/// Platform entry point for all APP.TechPack operations.
///
/// Registered in AppExternalMethodRegister with TypeName = "APP.TechPack.PluginEntry".
/// Each DB row carries a different MethodName; the platform passes it via PluginContext
/// and Execute dispatches accordingly. No static methods, no ServerContext coupling.
///
/// DB registration:
///   AssemblyName = APP.TechPack
///   TypeName     = APP.TechPack.PluginEntry
///   MethodName   = ApplyGradeRuleSet | GetGradeRuleCoverage
/// </summary>
public sealed class PluginEntry : IAppPlugin
{
    public object? Execute(object? input, PluginContext context)
    {
        var formData = input as AppMasterDetailDto
            ?? throw new ArgumentException(
                $"APP.TechPack.PluginEntry expects AppMasterDetailDto input, got '{input?.GetType().Name ?? "null"}'.");

        return context.MethodName switch
        {
            nameof(ApplyGradeRuleSet)    => ApplyGradeRuleSet(formData, context.ConnectionString),
            nameof(GetGradeRuleCoverage) => GetGradeRuleCoverage(formData, context.ConnectionString),
            _ => throw new InvalidOperationException(
                $"APP.TechPack.PluginEntry: unknown method '{context.MethodName}'. " +
                $"Valid values: {nameof(ApplyGradeRuleSet)}, {nameof(GetGradeRuleCoverage)}.")
        };
    }

    private static OperationCallResult<AppMasterDetailDto> ApplyGradeRuleSet(
        AppMasterDetailDto formData, string connectionString)
    {
        int ruleSetId   = Convert.ToInt32(formData.DictOneToOneFields?["RuleSetId"]);
        int styleSpecId = Convert.ToInt32(formData.DictOneToOneFields?["StyleSpecId"]);

        GradeRuleService.ApplyRuleSetToSpec(connectionString, ruleSetId, styleSpecId);

        return new OperationCallResult<AppMasterDetailDto> { Object = formData };
    }

    private static OperationCallResult<AppMasterDetailDto> GetGradeRuleCoverage(
        AppMasterDetailDto formData, string connectionString)
    {
        int ruleSetId   = Convert.ToInt32(formData.DictOneToOneFields?["RuleSetId"]);
        int styleSpecId = Convert.ToInt32(formData.DictOneToOneFields?["StyleSpecId"]);

        var coverage = GradeRuleService.GetCoverage(connectionString, ruleSetId, styleSpecId);

        formData.DictOneToOneFields ??= new Dictionary<string, object>();
        formData.DictOneToOneFields["MatchedCount"]   = coverage.MatchedSpecLines;
        formData.DictOneToOneFields["TotalCount"]     = coverage.TotalSpecLines;
        formData.DictOneToOneFields["UnmatchedCodes"] = string.Join(", ", coverage.UnmatchedBodyPartCodes);

        return new OperationCallResult<AppMasterDetailDto> { Object = formData };
    }
}
