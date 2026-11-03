using System;
using System.Threading;
using NLog;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Structured [AgentLoop] file logs for one RunAsync (see Document/AgentDesign/SharedAgentEngine-AgentLoop-Log-Plan.md).
    /// Identity (run/skill/session) is on RUN_START only; later lines are seq + KIND.
    /// </summary>
    internal static class AgentLoopLog
    {
        public const int MaxField = 4000;

        private static readonly Logger Log = LogManager.GetLogger("AgentLoop");
        private static readonly AsyncLocal<State> Current = new();

        internal sealed class State
        {
            public string RunId { get; init; } = "";
            public string Skill { get; init; } = "";
            public string Session { get; init; } = "";
            public int Seq { get; set; }
            public int LastLlmOutSeq { get; set; } = -1;
        }

        public static IDisposable Begin(string runId, string skillKey, string chatSessionKey)
        {
            var prev = Current.Value;
            Current.Value = new State
            {
                RunId = runId ?? "",
                Skill = skillKey ?? "",
                Session = chatSessionKey ?? ""
            };
            return new Pop(prev);
        }

        public static void SetSeq(int seq)
        {
            var s = Current.Value;
            if (s != null) s.Seq = seq < 0 ? 0 : seq;
        }

        public static bool TryMarkLlmOut(int seq)
        {
            var s = Current.Value;
            if (s == null) return true;
            if (s.LastLlmOutSeq == seq) return false;
            s.LastLlmOutSeq = seq;
            s.Seq = seq;
            return true;
        }

        public static void Write(string kind, string detail)
        {
            var line = OneLine(detail, MaxField * 2);
            if (string.Equals(kind, "LLM_OUT", StringComparison.OrdinalIgnoreCase)
                && IsNoiseLlmOut(line))
                return;

            var s = Current.Value;
            var seq = s?.Seq ?? 0;
            var run8 = ShortRun(s?.RunId);
            if (string.Equals(kind, "RUN_START", StringComparison.OrdinalIgnoreCase))
            {
                Log.Info("[AgentLoop] {0} RUN_START run={1} skill=\"{2}\" session={3} {4}",
                    run8, s?.RunId ?? "", s?.Skill ?? "", s?.Session ?? "", line);
            }
            else
            {
                Log.Info("[AgentLoop] {0} s{1} {2} {3}", run8, seq, kind ?? "", line);
            }
        }

        private static string ShortRun(string runId)
        {
            if (string.IsNullOrEmpty(runId)) return "--------";
            return runId.Length <= 8 ? runId : runId.Substring(0, 8);
        }

        public static string OneLine(string text, int max = MaxField)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var t = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            while (t.Contains("  ", StringComparison.Ordinal))
                t = t.Replace("  ", " ", StringComparison.Ordinal);
            t = t.Trim();
            if (t.Length <= max) return t;
            return t.Substring(0, max) + "…";
        }

        /// <summary>SK often fires an empty assistant message after a tool call.</summary>
        private static bool IsNoiseLlmOut(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            if (!line.StartsWith("next=TEXT", StringComparison.OrdinalIgnoreCase)) return false;
            var i = line.IndexOf("content=", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return true;
            return string.IsNullOrWhiteSpace(line.Substring(i + 8).Trim());
        }

        private sealed class Pop : IDisposable
        {
            private readonly State _prev;
            public Pop(State prev) { _prev = prev; }
            public void Dispose() => Current.Value = _prev;
        }
    }
}
