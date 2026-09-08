using System;
using App.BL.AppBuilderAgent;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tool that loads the full AppBuilder memory context on demand.
    /// Companion to MemorySearchPlugin.SearchMemory (keyword search).
    /// Use this at session start to orient the agent; use SearchMemory for targeted recall.
    ///
    /// ToolConfig: {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.MemoryContextPlugin","MethodName":"LoadMemoryContext"}
    /// </summary>
    public class MemoryContextPlugin
    {
        public string LoadMemoryContext()
        {
            try
            {
                var text = AppBuilderAgentMemoryBL.LoadMemoryContext();
                return string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.SerializeObject(new { Result = "No memory recorded yet. This is a fresh session." })
                    : text;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }
    }
}
