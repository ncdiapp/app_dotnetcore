namespace App.BL.AIAgent.GenericAgent
{
    public static class UniversalAgentConstants
    {
        public const string SkillKey = "__universal_agent__";
        public const int MaxSelectedSkills = 3;

        public static bool IsUniversalAgentKey(string skillKey) =>
            string.Equals(skillKey?.Trim(), SkillKey, System.StringComparison.OrdinalIgnoreCase);
    }
}
