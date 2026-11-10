using System;
using App.BL.AppBuilderAgent;
using APP.Framework.Plugin;
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
        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("load_memory_context",
            "Call this tool at the start of a session to recall what was built in previous sessions: existing tables, transactions, and prior user requests. "
            + "Use it when the user references prior work or asks what currently exists in the platform. For targeted recall by keyword, use search_memory instead.")]
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
