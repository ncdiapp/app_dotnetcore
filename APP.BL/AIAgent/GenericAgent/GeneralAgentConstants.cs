namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Canonical identity for the built-in general-purpose agent.
    /// </summary>
    public static class GeneralAgentConstants
    {
        public const string SkillKey = "__general_agent__";
        public const string LegacySkillKey = "__universal_agent__";
        public const string StayInGeneralMarker = "[stay_in_general]";
        public const int MaxSelectedSkills = 3;

        /// <summary>0–100. Specialist must score at least this to appear as a hand-off candidate. Tune here.</summary>
        public const int HandoffMinScore = 85;
        /// <summary>Max Specialist cards per General turn, highest scores first.</summary>
        public const int HandoffMaxSuggestions = 3;

        public static bool IsGeneralAgentKey(string skillKey)
        {
            var key = skillKey?.Trim();
            return string.Equals(key, SkillKey, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, LegacySkillKey, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
