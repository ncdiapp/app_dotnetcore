import React from 'react';
import { RecommendedToolDto } from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

interface Props {
    theme: Theme;
    tools: RecommendedToolDto[];
    /** accepted tools keyed "libraryKey\u0001toolName" */
    accepted: Set<string>;
    onToggle: (key: string, checked: boolean) => void;
}

export const recommendedToolKey = (t: { LibraryKey: string; ToolName: string }) => `${t.LibraryKey}\u0001${t.ToolName}`;

const riskBadge = (risk: string) => {
    if (risk === 'delete') return 'text-red-600 border-red-300';
    if (risk === 'write') return 'text-orange-600 border-orange-300';
    return 'text-green-700 border-green-300';
};

// Tools the AI picked for the workflow, grouped by library. Ticked tools are kept; unticked ones are
// excluded for this agent when the design is applied.
const AiRecommendedTools: React.FC<Props> = ({ theme, tools, accepted, onToggle }) => {
    const groups = tools.reduce<Record<string, RecommendedToolDto[]>>((acc, t) => {
        (acc[t.LibraryKey] ??= []).push(t);
        return acc;
    }, {});

    return (
        <div className="flex flex-col gap-1">
            <div className={`text-xs font-semibold ${theme.title}`}>
                <i className="fa-solid fa-list-check mr-1" />Recommended Tools for the workflow:
            </div>
            {tools.length === 0 ? (
                <div className={`text-xs opacity-40 ${theme.label} px-1`}>No individual tools recommended</div>
            ) : Object.entries(groups).map(([lib, list]) => (
                <div key={lib} className="mb-1">
                    <div className={`px-1 text-xs font-mono font-semibold opacity-70 ${theme.label}`}>{lib}</div>
                    {list.map(t => {
                        const key = recommendedToolKey(t);
                        return (
                            <label key={key} className={`flex items-start gap-2 px-2 py-0.5 text-xs cursor-pointer ${theme.label}`} title={t.Description}>
                                <input type="checkbox" className="mt-0.5" checked={accepted.has(key)} onChange={e => onToggle(key, e.target.checked)} />
                                <span className="font-mono font-semibold">{t.ToolName}</span>
                                <span className={`px-1 border rounded text-[10px] shrink-0 ${riskBadge(t.Risk)}`}>{t.Risk}</span>
                                {t.Reason && <span className="opacity-60">— {t.Reason}</span>}
                            </label>
                        );
                    })}
                </div>
            ))}
        </div>
    );
};

export default AiRecommendedTools;
