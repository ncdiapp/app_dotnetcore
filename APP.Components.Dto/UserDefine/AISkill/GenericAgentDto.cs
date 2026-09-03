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
        /// Pass to continue an existing multi-turn session.
        /// When null a new session is created.
        /// </summary>
        public string SessionId   { get; set; }

        /// <summary>
        /// Prior conversation turns in LLM-native format (user/assistant alternating).
        /// Built client-side and passed back so the LLM has full context.
        /// </summary>
        public List<JObject> Messages { get; set; } = new List<JObject>();
    }

    public class GenericAgentStartResultDto
    {
        public bool   IsStarted { get; set; }
        public string SessionId { get; set; } = "";
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
}
