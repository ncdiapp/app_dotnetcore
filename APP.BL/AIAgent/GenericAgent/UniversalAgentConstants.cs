namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Legacy compatibility alias. New code should use GeneralAgentConstants.
    /// </summary>
    [System.Obsolete("Use GeneralAgentConstants.")]
    public static class UniversalAgentConstants
    {
        public const string SkillKey = GeneralAgentConstants.SkillKey;
        public const int MaxSelectedSkills = GeneralAgentConstants.MaxSelectedSkills;

        public static bool IsUniversalAgentKey(string skillKey) =>
            GeneralAgentConstants.IsGeneralAgentKey(skillKey);
    }
}
