using System;
using System.Diagnostics;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;

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

            await Fire(_callbacks?.OnStep, new AgentStepEvent
            {
                Type        = "tool_call",
                ToolName    = ctx.Function.Name,
                Description = ctx.Function.Name,
                IsSuccess   = true,
                Details     = Truncate(SafeSerialize(ctx.Arguments), 400)
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
                await Fire(_callbacks?.OnStep, new AgentStepEvent
                {
                    Type        = "tool_result",
                    ToolName    = ctx.Function.Name,
                    Description = ok ? $"{ctx.Function.Name} — done ({sw.ElapsedMilliseconds}ms)" : ctx.Function.Name + " failed",
                    IsSuccess   = ok,
                    Details     = Truncate(resultText, 600)
                });
            }
        }

        private static string? TryGetResult(FunctionInvocationContext ctx)
        {
            try { return ctx.Result?.GetValue<string>(); }
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
