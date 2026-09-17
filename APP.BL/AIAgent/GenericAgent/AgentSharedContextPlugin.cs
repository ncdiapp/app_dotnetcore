using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// BuiltIn plugin methods for the shared workflow blackboard.
    /// Registered in AppAgentToolRegister under the 'platform-multi-agent' library.
    /// The WorkflowId scope is read from AgentToolContext — invisible to the LLM.
    /// </summary>
    public static class AgentSharedContextPlugin
    {
        /// <summary>
        /// Reads a JSON value from the shared workflow context.
        /// Returns "{}" if the key does not exist.
        /// </summary>
        public static string ReadContext(string key, AgentToolContext context)
        {
            var result = AppAgentSharedContextBL.ReadContext(context.WorkflowId, key, context.DataSourceId);
            return result ?? "{}";
        }

        /// <summary>
        /// Writes a JSON value to the shared workflow context.
        /// Returns pretty-printed confirmation JSON so Tool Activity is human-readable
        /// (void would surface as null; string-embedding valueJson double-escapes and is unreadable).
        /// </summary>
        public static string WriteContext(string key, string valueJson, AgentToolContext context)
        {
            AppAgentSharedContextBL.WriteContext(context.WorkflowId, key, valueJson, context.DataSourceId);

            JToken valueNode;
            try { valueNode = JToken.Parse(string.IsNullOrWhiteSpace(valueJson) ? "{}" : valueJson); }
            catch { valueNode = new JValue(valueJson ?? ""); }

            var payload = new JObject
            {
                ["ok"] = true,
                ["key"] = key ?? "",
                ["value"] = valueNode
            };

            var pretty = payload.ToString(Formatting.Indented);
            if (pretty.Length > 4000)
                pretty = pretty.Substring(0, 4000) + "\n…";
            return pretty;
        }
    }
}
