using System;
using System.Collections.Generic;

namespace APP.Components.EntityDto
{
    public class AppAISkillDto
    {
        public int SkillId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SkillContent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<AppAISkillRefDto> References { get; set; } = new List<AppAISkillRefDto>();
    }

    public class AppAISkillRefDto
    {
        public int RefId { get; set; }
        public int SkillId { get; set; }
        public string FileName { get; set; }
        public string FileContent { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
