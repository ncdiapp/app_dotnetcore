using APP.Framework.Plugin;

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
        /// All agents sharing the same WorkflowId can read it via ReadContext.
        /// </summary>
        public static void WriteContext(string key, string valueJson, AgentToolContext context)
        {
            AppAgentSharedContextBL.WriteContext(context.WorkflowId, key, valueJson, context.DataSourceId);
        }
    }
}
