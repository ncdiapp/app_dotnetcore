/** PLM Integration skill keys (see app-data-integration-agent-skills.json). */
export const PLM_SKILL_KEYS = ['plm-dw', 'plm-pom-grading', 'plm-search-view'] as const;

export type PlmSkillKey = typeof PLM_SKILL_KEYS[number];

export type PlmSkillSuggestion = {
  key: PlmSkillKey;
  label: string;
};

const PLM_SKILLS: PlmSkillSuggestion[] = [
  { key: 'plm-dw', label: 'Import from PLM Data Warehouse' },
  { key: 'plm-pom-grading', label: 'Import PLM POM and Grading' },
  { key: 'plm-search-view', label: 'Import PLM Search View' },
];

type Scored = PlmSkillSuggestion & { score: number };

function scorePlmSkills(text: string): Scored[] {
  const t = (text || '').trim();
  if (!t) return []; 

  const scores: Scored[] = [
    {
      key: 'plm-dw',
      label: 'Import from PLM Data Warehouse',
      score: 0,
    },
    {
      key: 'plm-pom-grading',
      label: 'Import PLM POM and Grading',
      score: 0,
    },
    {
      key: 'plm-search-view',
      label: 'Import PLM Search View',
      score: 0,
    },
  ];

  const bump = (idx: number, n = 1) => { scores[idx].score += n; };

  // plm-dw
  if (/\bplm\s*data\s*warehouse\b/i.test(t)) bump(0, 3);
  if (/\bplm\s*dw\b/i.test(t)) bump(0, 3);
  if (/data\s*warehouse\s*import/i.test(t)) bump(0, 2);
  if (/importfromplmdw/i.test(t)) bump(0, 3);
  if (/dw\s*tab/i.test(t)) bump(0, 2);
  if (/bom\s*colorway/i.test(t)) bump(0, 2);
  if (/plm\s*数据仓库/i.test(t)) bump(0, 3);
  if (/从\s*plm.*数据仓库/i.test(t)) bump(0, 3);

  // plm-pom-grading
  if (/\bpom\s*(and\s*)?grading\b/i.test(t)) bump(1, 3);
  if (/import\s*plm\s*pom/i.test(t)) bump(1, 3);
  if (/importplmpom/i.test(t)) bump(1, 3);
  if (/\bsize\s*run\b/i.test(t)) bump(1, 2);
  if (/grading\s*import/i.test(t)) bump(1, 2);
  if (/pom\s*评分/i.test(t)) bump(1, 2);
  if (/导入.*pom/i.test(t)) bump(1, 2);
  if (/\bgrading\b/i.test(t) && /\bpom\b/i.test(t)) bump(1, 2);

  // plm-search-view
  if (/\bplm\s*search\s*view\b/i.test(t)) bump(2, 3);
  if (/importplmsearchview/i.test(t)) bump(2, 3);
  if (/plm\s*搜索视图/i.test(t)) bump(2, 3);
  if (/search\s*view\s*import/i.test(t)) bump(2, 2);
  if (/mass\s*update\s*view/i.test(t)) bump(2, 2);
  if (/sibling\s*view/i.test(t)) bump(2, 2);

  const ranked = scores.filter(s => s.score > 0).sort((a, b) => b.score - a.score);
  if (ranked.length > 0) return ranked;

  // Generic PLM Integration intent
  if (
    /\bplm\s*integration\b/i.test(t)
    || /plm\s*集成/i.test(t)
    || /\bimport\s*from\s*plm\b/i.test(t)
    || /\b从\s*plm\s*导入/i.test(t)
    || (/\bplm\b/i.test(t) && /\bimport\b/i.test(t))
  ) {
    return PLM_SKILLS.map(s => ({ ...s, score: 1 }));
  }

  return [];
}

export function isPlmSkillKey(skillKey: string): boolean {
  return PLM_SKILL_KEYS.includes(skillKey as PlmSkillKey);
}

/**
 * When the user message clearly targets PLM Integration but the active skill is wrong,
 * return recommended skills to show as clickable switches (max 3).
 */
export function suggestPlmSkills(userText: string, currentSkillKey: string): PlmSkillSuggestion[] {
  const ranked = scorePlmSkills(userText);
  if (ranked.length === 0) return [];

  const top = ranked[0];
  if (isPlmSkillKey(currentSkillKey)) {
    if (top.key === currentSkillKey) return [];
    return [{ key: top.key, label: top.label }];
  }

  const keys = new Set<PlmSkillKey>();
  const out: PlmSkillSuggestion[] = [];
  for (const s of ranked) {
    if (keys.has(s.key)) continue;
    keys.add(s.key);
    out.push({ key: s.key, label: s.label });
    if (out.length >= 3) break;
  }
  return out;
}
