using System;
using System.Diagnostics;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    internal sealed class AgentStepFilter : IFunctionInvocationFilter
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private readonly GenericAgentCallbacks _callbacks;
        private readonly bool _enableThinkMode;

        public AgentStepFilter(GenericAgentCallbacks callbacks, bool enableThinkMode = false)
        {
            _callbacks = callbacks;
            _enableThinkMode = enableThinkMode;
        }

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext ctx, Func<FunctionInvocationContext, Task> next)
        {
            Log.Info($"[Agent] → {ctx.Function.Name}");

            var toolName = ctx.Function.Name ?? "";
            var isCallAgent = string.Equals(toolName, "call_agent", StringComparison.OrdinalIgnoreCase);
            var targetSkillKey = isCallAgent ? GetArgString(ctx.Arguments, "targetSkillKey", "TargetSkillKey") : null;

            var toolLabel = toolName;
            if (isCallAgent && !string.IsNullOrWhiteSpace(targetSkillKey))
                toolLabel = "call_agent → " + targetSkillKey.Trim();

            var callDetails = isCallAgent
                ? BuildCallAgentArgsPreview(ctx.Arguments, targetSkillKey)
                : ToolIoFormatter.FormatToolCallDetails(toolName, ctx.Arguments);

            if (_enableThinkMode)
            {
                await Fire(_callbacks?.OnStep, new AgentStepEvent
                {
                    Type        = "thinking",
                    Description = AgentThinkNarrator.IntentBeforeTool(toolName, ctx.Arguments, toolLabel),
                    Details     = ToolIoFormatter.FormatThinkDetail(toolName, ctx.Arguments, null, true, afterTool: false),
                    IsSuccess   = true
                });
            }

            await Fire(_callbacks?.OnStep, new AgentStepEvent
            {
                Type        = "tool_call",
                ToolName    = toolName,
                Description = toolLabel,
                IsSuccess   = true,
                Details     = callDetails
            });

            var sw = Stopwatch.StartNew();
            bool ok = true;
            try
            {
                await next(ctx);
            }
            catch
            {
                ok = false;
                throw;
            }
            finally
            {
                sw.Stop();
                var status = ok ? "done" : "FAILED";
                Log.Info($"[Agent] ← {ctx.Function.Name} {status} in {sw.ElapsedMilliseconds}ms");

                var resultText = ok ? TryGetResult(ctx) : null;
                if (ok && (resultText == null || resultText == "null"))
                    resultText = "{\"ok\":true}";

                // execute_sql etc. often return { IsSuccess:false } without throwing
                var effectiveOk = ok && !AgentThinkNarrator.IsSoftFailure(resultText);

                if (_enableThinkMode)
                {
                    await Fire(_callbacks?.OnStep, new AgentStepEvent
                    {
                        Type        = "thinking",
                        Description = AgentThinkNarrator.OutcomeAfterTool(toolName, ctx.Arguments, resultText, effectiveOk),
                        Details     = ToolIoFormatter.FormatThinkDetail(toolName, ctx.Arguments, resultText, effectiveOk, afterTool: true),
                        IsSuccess   = effectiveOk
                    });
                }

                await Fire(_callbacks?.OnStep, new AgentStepEvent
                {
                    Type        = "tool_result",
                    ToolName    = toolName,
                    Description = effectiveOk
                        ? (isCallAgent && !string.IsNullOrWhiteSpace(targetSkillKey)
                            ? $"call_agent → {targetSkillKey.Trim()} — done ({sw.ElapsedMilliseconds}ms)"
                            : $"{toolName} — done ({sw.ElapsedMilliseconds}ms)")
                        : toolName + " failed",
                    IsSuccess   = effectiveOk,
                    Details     = isCallAgent
                        ? Truncate(resultText, 4000)
                        : ToolIoFormatter.FormatToolResultDetails(toolName, ctx.Arguments, resultText, effectiveOk)
                });
            }
        }

        private static string BuildCallAgentArgsPreview(KernelArguments? args, string? targetSkillKey)
        {
            var jo = new JObject();
            if (!string.IsNullOrWhiteSpace(targetSkillKey))
                jo["targetSkillKey"] = targetSkillKey.Trim();
            var message = GetArgString(args, "message", "Message");
            if (!string.IsNullOrEmpty(message))
                jo["message"] = message.Length > 400 ? message.Substring(0, 400) + "…" : message;
            return "## Input\n```json\n" + jo.ToString(Formatting.Indented) + "\n```";
        }

        private static string? GetArgString(KernelArguments? args, params string[] names)
        {
            if (args == null || names == null) return null;
            foreach (var name in names)
            {
                if (args.TryGetValue(name, out var v) && v != null)
                {
                    var s = v as string ?? v.ToString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
            }
            foreach (var kv in args)
            {
                foreach (var name in names)
                {
                    if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                    {
                        var s = kv.Value as string ?? kv.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                    }
                }
            }
            return null;
        }

        private static string? TryGetResult(FunctionInvocationContext ctx)
        {
            try
            {
                if (ctx.Result == null) return null;
                try { return ctx.Result.GetValue<string>(); }
                catch (Exception swallowed) { SwallowLog.Write(swallowed, "not a string"); }
                var obj = ctx.Result.GetValue<object>();
                if (obj == null) return null;
                if (obj is string s) return s;
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch (Exception swallowed) { SwallowLog.Write(swallowed); return null; }
        }

        private static async Task Fire(Func<AgentStepEvent, Task>? cb, AgentStepEvent e)
        {
            if (cb == null) return;
            try { await cb(e).ConfigureAwait(false); } catch (Exception swallowed) { SwallowLog.Write(swallowed); }
        }

        private static string? Truncate(string? s, int max)
        {
            if (s == null || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}
