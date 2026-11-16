using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace APP.Components.Dto
{
    // ─────────────────────────────────────────────────────────────────────────────
    // GenericAgent request / result DTOs — used by GenericAgentController
    // ─────────────────────────────────────────────────────────────────────────────

    public class GenericAgentRequestDto
    {
        public string SkillKey    { get; set; } = "";
        public string UserMessage { get; set; } = "";

        /// <summary>
        /// Ephemeral SSE session id (optional on request; server always creates a new run session).
        /// </summary>
        public string SessionId   { get; set; }

        /// <summary>
        /// Stable AppGenericAgentSession.SessionKey.
        /// Empty/null → fixed test key SkillKey:UserId.
        /// GUID → New/Continue Chat for this agent.
        /// </summary>
        public string ChatSessionKey { get; set; }

        /// <summary>
        /// Prior conversation turns in LLM-native format (user/assistant alternating).
        /// Built client-side and passed back so the LLM has full context.
        /// </summary>
        public List<JObject> Messages { get; set; } = new List<JObject>();

        /// <summary>
        /// Current open form snapshot. Injected into this turn's system section only.
        /// </summary>
        public string HostContextJson { get; set; }
    }

    public class FormHostChatRequestDto
    {
        public int TransactionId { get; set; }
        /// <summary>Master Detail root value, "list", or "new:{guid}".</summary>
        public string Scope { get; set; }
    }

    /// <summary>Open or resume Instruction Designer chat for one Agent Management edit target.</summary>
    public class InstructionDesignerChatRequestDto
    {
        /// <summary>Saved target AgentCode. Empty when drafting a new unsaved agent.</summary>
        public string TargetAgentCode { get; set; }
        /// <summary>Client-stable id for unsaved new drafts (used as new:{ClientDraftId} scope).</summary>
        public string ClientDraftId { get; set; }
    }

    public class FormHostToolEvent
    {
        public string RequestId { get; set; }
        public string ToolName { get; set; }
        public string ArgsJson { get; set; }
    }

    public class GenericAgentConfirmHostToolDto
    {
        public string SessionId { get; set; }
        public string ResultJson { get; set; }
    }

    /// <summary>
    /// Calculation-style apply: client posts open FormData; server mutates; no save.
    /// </summary>
    public class FormAgentApplyToolRequestDto
    {
        public string ToolName { get; set; }
        /// <summary>master | list</summary>
        public string Mode { get; set; }
        public JToken FormData { get; set; }
        public JObject Args { get; set; }
    }

    public class FormAgentApplyToolResultDto
    {
        public JToken FormData { get; set; }
        public JObject ToolResult { get; set; }
    }

    public class GenericAgentStartResultDto
    {
        public bool   IsStarted { get; set; }
        public string SessionId { get; set; } = "";
        /// <summary>Stable persistence key used for history + AgentOutput file folder.</summary>
        public string ChatSessionKey { get; set; } = "";
    }

    public class GenericAgentCancelRequestDto
    {
        public string SessionId { get; set; }
    }

    public class GenericAgentConfirmPlanDto
    {
        public string SessionId { get; set; }
        public bool   Confirmed { get; set; }
    }

    public class GenericAgentConfirmSchemaDto
    {
        public string SessionId { get; set; }
        public bool   Confirmed { get; set; }
        public string SchemaJson { get; set; }
        public string Feedback  { get; set; }
    }

    /// <summary>Request body for POST /GenericAgent/ConfirmAskUser.</summary>
    public class GenericAgentConfirmAskUserDto
    {
        public string SessionId { get; set; }
        public bool Cancelled { get; set; }
        public Dictionary<string, string> Answers { get; set; } = new Dictionary<string, string>();
        public List<string> SelectedIds { get; set; } = new List<string>();
        public string FreeText { get; set; }
        public string SkillKey { get; set; }
        public string ChatSessionKey { get; set; }
        /// <summary>Optional UI-computed question title (preferred over re-summarizing pending Prompt).</summary>
        public string QuestionSummary { get; set; }
        /// <summary>Optional UI-computed answer text (preferred over reformatting SelectedIds/Answers).</summary>
        public string AnswerSummary { get; set; }
    }
}
