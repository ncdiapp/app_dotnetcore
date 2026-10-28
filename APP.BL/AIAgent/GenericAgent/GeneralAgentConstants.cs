namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Canonical identity for the built-in general-purpose agent.
    /// </summary>
    public static class GeneralAgentConstants
    {
        public const string SkillKey = "__general_agent__";
        public const string LegacySkillKey = "__universal_agent__";
        public const int MaxSelectedSkills = 3;

        public static bool IsGeneralAgentKey(string skillKey)
        {
            var key = skillKey?.Trim();
            return string.Equals(key, SkillKey, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, LegacySkillKey, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
