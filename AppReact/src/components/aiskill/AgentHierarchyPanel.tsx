import React, { useMemo, useState } from 'react';
import { AppAgentChildMappingDto, AppAgentSkillSetDto } from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

export type AgentRoleFilter = 'all' | 'standalone-orchestrators' | 'child-agents';
export type AgentStatusFilter = 'all' | 'active' | 'inactive';

export interface AgentHierarchyPanelProps {
    agents: AppAgentSkillSetDto[];
    mappings: AppAgentChildMappingDto[];
    selectedSkillKey: string | null;
    onSelect: (skillKey: string) => void;
    onDropOntoOrchestrator: (parentSkillKey: string, childSkillKey: string) => void;
    theme: Theme;
    borderCls: string;
}

type TreeRow =
    | { kind: 'top'; agent: AppAgentSkillSetDto }
    | { kind: 'child'; agent: AppAgentSkillSetDto; parentSkillKey: string };

function agentName(a: AppAgentSkillSetDto): string {
    return (a.DisplayName || a.SkillKey || '').trim();
}

function compareAgentName(a: AppAgentSkillSetDto, b: AppAgentSkillSetDto): number {
    const byName = agentName(a).localeCompare(agentName(b), undefined, { sensitivity: 'base' });
    if (byName !== 0) return byName;
    return a.SkillKey.localeCompare(b.SkillKey);
}

const AgentHierarchyPanel: React.FC<AgentHierarchyPanelProps> = ({
    agents,
    mappings,
    selectedSkillKey,
    onSelect,
    onDropOntoOrchestrator,
    theme,
    borderCls,
}) => {
    const [search, setSearch] = useState('');
    const [roleFilter, setRoleFilter] = useState<AgentRoleFilter>('standalone-orchestrators');
    const [statusFilter, setStatusFilter] = useState<AgentStatusFilter>('all');
    const [expanded, setExpanded] = useState<Set<string>>(() => new Set());
    const [dragSkillKey, setDragSkillKey] = useState<string | null>(null);

    const byKey = useMemo(() => {
        const m = new Map<string, AppAgentSkillSetDto>();
        agents.forEach(a => m.set(a.SkillKey, a));
        return m;
    }, [agents]);

    const childrenByParent = useMemo(() => {
        const m = new Map<string, AppAgentChildMappingDto[]>();
        mappings.forEach(row => {
            const list = m.get(row.ParentSkillKey) ?? [];
            list.push(row);
            m.set(row.ParentSkillKey, list);
        });
        m.forEach((list, parent) => {
            list.sort((a, b) => {
                const aa = byKey.get(a.ChildSkillKey);
                const bb = byKey.get(b.ChildSkillKey);
                if (aa && bb) return compareAgentName(aa, bb);
                return a.ChildSkillKey.localeCompare(b.ChildSkillKey);
            });
            m.set(parent, list);
        });
        return m;
    }, [mappings, byKey]);

    const usedByCount = useMemo(() => {
        const m = new Map<string, number>();
        mappings.forEach(row => m.set(row.ChildSkillKey, (m.get(row.ChildSkillKey) ?? 0) + 1));
        return m;
    }, [mappings]);

    const matchesFilters = (agent: AppAgentSkillSetDto) => {
        const q = search.trim().toLowerCase();
        if (q) {
            const hay = `${agent.SkillKey} ${agent.DisplayName} ${agent.Description || ''}`.toLowerCase();
            if (!hay.includes(q)) return false;
        }
        if (statusFilter === 'active' && !agent.IsActive) return false;
        if (statusFilter === 'inactive' && agent.IsActive) return false;
        const childCount = childrenByParent.get(agent.SkillKey)?.length ?? agent.ChildCount ?? 0;
        const used = usedByCount.get(agent.SkillKey) ?? agent.UsedByCount ?? 0;
        // Standalone & Orchestrators: not a pure Child-Agent (used as child with no children of its own)
        if (roleFilter === 'standalone-orchestrators' && used > 0 && childCount <= 0) return false;
        if (roleFilter === 'child-agents' && used <= 0) return false;
        return true;
    };

    const topAgents = useMemo(
        () => agents
            .filter(a => !a.SkillKey.startsWith('tmpl-'))
            .filter(matchesFilters)
            .slice()
            .sort(compareAgentName),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [agents, search, roleFilter, statusFilter, childrenByParent, usedByCount],
    );

    const toggleExpand = (skillKey: string, e: React.MouseEvent) => {
        e.stopPropagation();
        setExpanded(prev => {
            const next = new Set(prev);
            if (next.has(skillKey)) next.delete(skillKey); else next.add(skillKey);
            return next;
        });
    };

    const rows: TreeRow[] = [];
    for (const agent of topAgents) {
        rows.push({ kind: 'top', agent });
        const kids = childrenByParent.get(agent.SkillKey) ?? [];
        if (kids.length > 0 && expanded.has(agent.SkillKey)) {
            for (const link of kids) {
                const child = byKey.get(link.ChildSkillKey);
                if (!child) continue;
                rows.push({ kind: 'child', agent: child, parentSkillKey: agent.SkillKey });
            }
        }
    }

    const onDragStart = (skillKey: string) => (e: React.DragEvent) => {
        setDragSkillKey(skillKey);
        e.dataTransfer.setData('text/plain', skillKey);
        e.dataTransfer.effectAllowed = 'copyMove';
    };

    const onDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
    };

    const dropOnOrchestrator = (parentSkillKey: string) => (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        const childKey = dragSkillKey || e.dataTransfer.getData('text/plain');
        setDragSkillKey(null);
        if (!childKey || childKey === parentSkillKey) return;
        onDropOntoOrchestrator(parentSkillKey, childKey);
        setExpanded(prev => new Set(prev).add(parentSkillKey));
    };

    const inp = `w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const sel = `h-7 px-1 text-xs border ${theme.inputBox} focus:outline-none`;

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            <div className={`shrink-0 flex flex-col gap-1 px-2 py-2 border-b ${borderCls}`}>
                <input
                    className={inp}
                    placeholder="Search name / code…"
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                />
                <div className="flex gap-1">
                    <select className={`${sel} w-1 flex-auto`} value={roleFilter} onChange={e => setRoleFilter(e.target.value as AgentRoleFilter)}>
                        <option value="all">All roles</option>
                        <option value="standalone-orchestrators">Standalone &amp; Orchestrators</option>
                        <option value="child-agents">Child Agents</option>
                    </select>
                    <select className={`${sel} w-24`} value={statusFilter} onChange={e => setStatusFilter(e.target.value as AgentStatusFilter)}>
                        <option value="all">All</option>
                        <option value="active">Active</option>
                        <option value="inactive">Inactive</option>
                    </select>
                </div>
            </div>
            <div className="w-full h-1 flex-auto overflow-auto">
                {rows.length === 0 && (
                    <div className={`px-3 py-4 text-xs ${theme.label}`}>No agents match filters.</div>
                )}
                {rows.map(row => {
                    const agent = row.agent;
                    const isTop = row.kind === 'top';
                    const childCount = childrenByParent.get(agent.SkillKey)?.length ?? 0;
                    const isOrch = isTop && childCount > 0;
                    const used = usedByCount.get(agent.SkillKey) ?? 0;
                    const selected = selectedSkillKey === agent.SkillKey;
                    const inactive = !agent.IsActive;
                    return (
                        <div
                            key={isTop ? `top:${agent.SkillKey}` : `child:${row.parentSkillKey}:${agent.SkillKey}`}
                            draggable={isTop}
                            onDragStart={isTop ? onDragStart(agent.SkillKey) : undefined}
                            onDragOver={isTop ? onDragOver : undefined}
                            onDrop={isTop ? dropOnOrchestrator(agent.SkillKey) : undefined}
                            onClick={() => onSelect(agent.SkillKey)}
                            className={`flex items-center gap-2 py-1.5 cursor-pointer border-b text-xs ${borderCls} ${
                                selected ? 'font-semibold' : 'hover:opacity-90'
                            } ${inactive ? 'opacity-55' : ''} ${theme.mainContentSection}`}
                            style={{
                                paddingLeft: isTop ? 8 : 28,
                                paddingRight: 8,
                                ...(selected ? { outline: '1px solid currentColor', outlineOffset: -1 } : {}),
                            }}
                            title={agent.SkillKey}
                        >
                            {/* Fixed expand gutter */}
                            <div className="w-5 h-5 shrink-0 flex items-center justify-center">
                                {isOrch ? (
                                    <button
                                        type="button"
                                        className={`w-5 h-5 rounded text-[10px] flex items-center justify-center ${theme.button_default}`}
                                        onClick={e => toggleExpand(agent.SkillKey, e)}
                                        title={expanded.has(agent.SkillKey) ? 'Collapse' : 'Expand Child-Agents'}
                                    >
                                        <i className={`fa-solid fa-chevron-${expanded.has(agent.SkillKey) ? 'down' : 'right'}`} />
                                    </button>
                                ) : null}
                            </div>

                            {/* Single role icon */}
                            <i
                                className={`fa-solid w-4 text-center text-[12px] shrink-0 ${
                                    isOrch ? 'fa-sitemap' : 'fa-robot'
                                } ${inactive ? theme.label : theme.title}`}
                                aria-hidden
                            />

                            <div className="min-w-0 w-1 flex-auto">
                                <div className={`truncate leading-tight ${inactive ? theme.label : theme.title}`}>
                                    {agentName(agent) || agent.SkillKey}
                                </div>
                                <div className={`truncate leading-tight text-[10px] opacity-55 ${theme.label}`}>{agent.SkillKey}</div>
                            </div>

                            <div className={`shrink-0 text-[10px] ${theme.label}`}>
                                {isOrch && (
                                    <span className={`px-1 py-0.5 rounded border ${borderCls}`} title="Orchestrator (Primary) Agent">Orch</span>
                                )}
                                {!isOrch && isTop && used > 0 && (
                                    <span className="opacity-70" title="Used as Child-Agent by N orchestrators">×{used}</span>
                                )}
                                {!isOrch && !isTop && (
                                    <span className="opacity-50">Child</span>
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

export default AgentHierarchyPanel;
