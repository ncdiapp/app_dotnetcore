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

        public AgentStepFilter(GenericAgentCallbacks callbacks)
        {
            _callbacks = callbacks;
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

            // For call_agent, keep targetSkillKey first so truncation never drops the agent id.
            var details = isCallAgent
                ? BuildCallAgentArgsPreview(ctx.Arguments, targetSkillKey)
                : Truncate(SafeSerialize(ctx.Arguments), 2000);

            await Fire(_callbacks?.OnStep, new AgentStepEvent
            {
                Type        = "tool_call",
                ToolName    = toolName,
                Description = toolLabel,
                IsSuccess   = true,
                Details     = details
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
                await Fire(_callbacks?.OnStep, new AgentStepEvent
                {
                    Type        = "tool_result",
                    ToolName    = toolName,
                    Description = ok
                        ? (isCallAgent && !string.IsNullOrWhiteSpace(targetSkillKey)
                            ? $"call_agent → {targetSkillKey.Trim()} — done ({sw.ElapsedMilliseconds}ms)"
                            : $"{toolName} — done ({sw.ElapsedMilliseconds}ms)")
                        : toolName + " failed",
                    IsSuccess   = ok,
                    Details     = Truncate(resultText, 4000)
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
            return jo.ToString(Formatting.None);
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
            // Case-insensitive fallback
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
                catch { /* not a string */ }
                var obj = ctx.Result.GetValue<object>();
                if (obj == null) return null;
                if (obj is string s) return s;
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch { return null; }
        }

        private static string SafeSerialize(object? obj)
        {
            try { return JsonConvert.SerializeObject(obj); }
            catch { return ""; }
        }

        private static async Task Fire(Func<AgentStepEvent, Task>? cb, AgentStepEvent e)
        {
            if (cb == null) return;
            try { await cb(e).ConfigureAwait(false); } catch { }
        }

        private static string? Truncate(string? s, int max)
        {
            if (s == null || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}
