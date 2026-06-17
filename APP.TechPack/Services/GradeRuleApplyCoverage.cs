using System.Collections.Generic;

namespace APP.TechPack.Services;

public class GradeRuleApplyCoverage
{
    public List<string> MatchedBodyPartCodes { get; set; } = new List<string>();
    public List<string> UnmatchedBodyPartCodes { get; set; } = new List<string>();
    public int TotalSpecLines { get; set; }
    public int MatchedSpecLines { get; set; }
}
