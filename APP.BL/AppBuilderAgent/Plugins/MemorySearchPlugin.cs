using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// RAG plugin — lets the agent retrieve relevant memory sections on demand
    /// instead of having the full memory dump injected into every system prompt.
    /// </summary>
    public class MemorySearchPlugin
    {
        [AgentFunction("search_memory",
            "Search past build history, platform notes, and agent observations for entries " +
            "that match the given keywords. " +
            "Use this at the start of a session to recall what was built previously, " +
            "or when you need to find historical context about a specific application or table.")]
        public string SearchMemory(
            [AgentParam("One or more keywords describing what you are looking for (e.g. application name, table name, topic).", true)]
            string query)
        {
            try
            {
                var result = AppBuilderAgentMemoryBL.SearchMemory(query);
                return JsonConvert.SerializeObject(new { Result = result }, Formatting.None);
            }
            catch (System.Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }
    }
}
