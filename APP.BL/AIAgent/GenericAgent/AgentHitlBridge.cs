using System.Threading;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// AsyncLocal bridge so BuiltIn ask_user (and other HITL tools) can reach the
    /// run's <see cref="GenericAgentCallbacks"/> without putting DTO-typed Funcs on
    /// <c>AgentToolContext</c> in APP.Framework.
    /// Set at the start of <see cref="GenericAgentEngine.RunAsync"/>; restored in finally.
    /// </summary>
    public static class AgentHitlBridge
    {
        private static readonly AsyncLocal<GenericAgentCallbacks> CurrentLocal = new();

        public static GenericAgentCallbacks Current
        {
            get => CurrentLocal.Value;
            set => CurrentLocal.Value = value;
        }
    }
}
