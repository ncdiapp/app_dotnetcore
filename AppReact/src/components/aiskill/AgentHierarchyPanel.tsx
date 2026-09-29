import React, { useMemo, useState } from 'react';
import { AppAgentChildMappingDto, AppAgentSkillSetDto } from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

export type AgentRoleFilter = 'all' | 'orchestrators' | 'child-agents' | 'standalone';
export type AgentStatusFilter = 'all' | 'active' | 'inactive';

export interface AgentHierarchyPanelProps {
    agents: AppAgentSkillSetDto[];
    mappings: AppAgentChildMappingDto[];
    selectedSkillKey: string | null;
    onSelect: (skillKey: string) => void;
    onDropOntoOrchestrator: (parentSkillKey: string, childSkillKey: string) => void;
    onReorderChildren: (parentSkillKey: string, orderedChildKeys: string[]) => void;
    theme: Theme;
    borderCls: string;
}

type TreeRow =
    | { kind: 'top'; agent: AppAgentSkillSetDto }
    | { kind: 'child'; agent: AppAgentSkillSetDto; parentSkillKey: string; sortOrder: number };

const AgentHierarchyPanel: React.FC<AgentHierarchyPanelProps> = ({
    agents,
    mappings,
    selectedSkillKey,
    onSelect,
    onDropOntoOrchestrator,
    onReorderChildren,
    theme,
    borderCls,
}) => {
    const [search, setSearch] = useState('');
    const [roleFilter, setRoleFilter] = useState<AgentRoleFilter>('all');
    const [statusFilter, setStatusFilter] = useState<AgentStatusFilter>('all');
    const [expanded, setExpanded] = useState<Set<string>>(() => new Set());
    const [dragSkillKey, setDragSkillKey] = useState<string | null>(null);
    const [dragFromParent, setDragFromParent] = useState<string | null>(null);

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
        m.forEach(list => list.sort((a, b) => a.SortOrder - b.SortOrder || a.ChildSkillKey.localeCompare(b.ChildSkillKey)));
        return m;
    }, [mappings]);

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
        if (roleFilter === 'orchestrators' && childCount <= 0) return false;
        if (roleFilter === 'child-agents' && used <= 0) return false;
        if (roleFilter === 'standalone' && (childCount > 0 || used > 0)) return false;
        return true;
    };

    const topAgents = useMemo(
        () => agents.filter(a => !a.SkillKey.startsWith('tmpl-')).filter(matchesFilters),
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
                rows.push({ kind: 'child', agent: child, parentSkillKey: agent.SkillKey, sortOrder: link.SortOrder });
            }
        }
    }

    const onDragStart = (skillKey: string, fromParent: string | null) => (e: React.DragEvent) => {
        setDragSkillKey(skillKey);
        setDragFromParent(fromParent);
        e.dataTransfer.setData('text/plain', skillKey);
        e.dataTransfer.effectAllowed = 'move';
    };

    const onDragOver = (e: React.DragEvent) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    };

    const dropOnOrchestrator = (parentSkillKey: string) => (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        const childKey = dragSkillKey || e.dataTransfer.getData('text/plain');
        setDragSkillKey(null);
        setDragFromParent(null);
        if (!childKey || childKey === parentSkillKey) return;
        if (dragFromParent === parentSkillKey) return;
        onDropOntoOrchestrator(parentSkillKey, childKey);
        setExpanded(prev => new Set(prev).add(parentSkillKey));
    };

    const dropReorderChild = (parentSkillKey: string, targetChildKey: string) => (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        const moving = dragSkillKey || e.dataTransfer.getData('text/plain');
        setDragSkillKey(null);
        const fromParent = dragFromParent;
        setDragFromParent(null);
        if (!moving || fromParent !== parentSkillKey) return;
        const kids = (childrenByParent.get(parentSkillKey) ?? []).map(x => x.ChildSkillKey);
        const from = kids.indexOf(moving);
        const to = kids.indexOf(targetChildKey);
        if (from < 0 || to < 0 || from === to) return;
        const next = [...kids];
        next.splice(from, 1);
        next.splice(to, 0, moving);
        onReorderChildren(parentSkillKey, next);
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
                        <option value="orchestrators">Orchestrators</option>
                        <option value="child-agents">Child-Agents</option>
                        <option value="standalone">Standalone</option>
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
                    const pad = isTop ? 'pl-2' : 'pl-8';
                    return (
                        <div
                            key={isTop ? `top:${agent.SkillKey}` : `child:${row.parentSkillKey}:${agent.SkillKey}`}
                            draggable
                            onDragStart={onDragStart(agent.SkillKey, isTop ? null : row.parentSkillKey)}
                            onDragOver={onDragOver}
                            onDrop={isTop && isOrch
                                ? dropOnOrchestrator(agent.SkillKey)
                                : isTop
                                    ? dropOnOrchestrator(agent.SkillKey)
                                    : dropReorderChild(row.parentSkillKey, agent.SkillKey)}
                            onClick={() => onSelect(agent.SkillKey)}
                            className={`flex items-center gap-1 px-1 py-1.5 cursor-pointer border-b text-xs ${pad} ${borderCls} ${
                                selected ? 'opacity-100 font-semibold' : 'hover:opacity-90'
                            } ${inactive ? 'opacity-50' : ''} ${theme.mainContentSection}`}
                            style={selected ? { outline: '1px solid currentColor', outlineOffset: -1 } : undefined}
                            title={agent.SkillKey}
                        >
                            {isTop ? (
                                isOrch ? (
                                    <button
                                        type="button"
                                        className={`w-5 h-5 shrink-0 rounded text-[10px] ${theme.button_default}`}
                                        onClick={e => toggleExpand(agent.SkillKey, e)}
                                        title={expanded.has(agent.SkillKey) ? 'Collapse' : 'Expand Child-Agents'}
                                    >
                                        <i className={`fa-solid fa-chevron-${expanded.has(agent.SkillKey) ? 'down' : 'right'}`} />
                                    </button>
                                ) : (
                                    <span className="w-5 shrink-0" />
                                )
                            ) : (
                                <i className={`fa-solid fa-turn-up fa-rotate-90 text-[10px] shrink-0 ${theme.label}`} />
                            )}
                            <i
                                className={`fa-solid ${isOrch ? 'fa-sitemap' : used > 0 ? 'fa-puzzle-piece' : 'fa-robot'} text-[11px] shrink-0 ${
                                    inactive ? theme.label : theme.title
                                }`}
                            />
                            <div className="min-w-0 w-1 flex-auto">
                                <div className={`truncate ${inactive ? theme.label : theme.title}`}>
                                    {agent.DisplayName || agent.SkillKey}
                                </div>
                                <div className={`truncate opacity-60 ${theme.label}`}>{agent.SkillKey}</div>
                            </div>
                            <div className={`shrink-0 flex flex-col items-end gap-0.5 ${theme.label}`}>
                                {isOrch && (
                                    <span className="text-[10px] px-1 rounded border opacity-80" title="Orchestrator (Primary) Agent">Orch</span>
                                )}
                                {!isOrch && used > 0 && isTop && (
                                    <span className="text-[10px] opacity-70" title="Used as Child-Agent">used by {used}</span>
                                )}
                                {inactive && <span className="text-[10px]">○</span>}
                                {agent.IsActive && <span className="text-[10px]">●</span>}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

export default AgentHierarchyPanel;
