import React, { useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { AgentMarkdown } from '../../aiskill/AgentMarkdown';

export type CommandHelpSectionId = string;

/** Flip to true when Ask Configurator should open/prefill the dock again. */
const SHOW_ASK_CONFIGURATOR_BUTTON = false;

type TocNode = {
  id: string;
  title: string;
  level: number;
  /** Exact markdown heading line without hashes */
  headingText: string;
  children: TocNode[];
};

function slugify(s: string): string {
  return s
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 80);
}

/** Build nested TOC from ## / ### headings (#### stay in body only — avoids noisy Purpose/Pitfalls leaves). */
function buildToc(md: string): TocNode[] {
  const lines = (md || '').replace(/\r\n/g, '\n').split('\n');
  const roots: TocNode[] = [];
  const stack: TocNode[] = [];
  for (const line of lines) {
    const m = /^(#{2,3})\s+(.+?)\s*$/.exec(line);
    if (!m) continue;
    const level = m[1].length;
    const headingText = m[2].replace(/\s+#*$/, '').trim();
    const node: TocNode = {
      id: `${level}-${slugify(headingText)}`,
      title: headingText.replace(/^\d+(\.\d+)*\s+/, ''),
      level,
      headingText,
      children: [],
    };
    while (stack.length && stack[stack.length - 1].level >= level) stack.pop();
    if (!stack.length) roots.push(node);
    else stack[stack.length - 1].children.push(node);
    stack.push(node);
  }
  return roots;
}

/** Extract from a ##/### heading through the next heading of same or higher level. */
function extractByHeading(md: string, headingText: string, level: number): string {
  if (!md || !headingText) return md;
  const prefix = '#'.repeat(level);
  const startRe = new RegExp(`^${prefix}\\s+${escapeRegExp(headingText)}\\s*$`, 'm');
  const match = startRe.exec(md);
  if (!match || match.index == null) return md;
  const start = match.index;
  // Skip past this full heading line first — slicing at start+1 used to leave a lone "#",
  // which then matched nextRe and returned only that character.
  const afterHeading = md.slice(start + match[0].length);
  const nextRe = new RegExp(`^#{1,${level}}\\s+`, 'm');
  const next = afterHeading.search(nextRe);
  if (next < 0) return md.slice(start);
  return md.slice(start, start + match[0].length + next);
}

function escapeRegExp(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function filterToc(nodes: TocNode[], q: string): TocNode[] {
  const needle = q.trim().toLowerCase();
  if (!needle) return nodes;
  const walk = (n: TocNode): TocNode | null => {
    const kids = n.children.map(walk).filter(Boolean) as TocNode[];
    if (n.title.toLowerCase().includes(needle) || n.headingText.toLowerCase().includes(needle) || kids.length) {
      return { ...n, children: kids };
    }
    return null;
  };
  return nodes.map(walk).filter(Boolean) as TocNode[];
}

function TocTree(props: {
  nodes: TocNode[];
  activeId: string | null;
  onSelect: (n: TocNode) => void;
  theme: any;
  depth?: number;
}) {
  const { nodes, activeId, onSelect, theme, depth = 0 } = props;
  return (
    <ul className={depth === 0 ? 'space-y-0.5' : 'mt-0.5 ml-2 border-l pl-2 space-y-0.5'}>
      {nodes.map((n) => {
        const active = activeId === n.id;
        return (
          <li key={n.id}>
            <button
              type="button"
              onClick={() => onSelect(n)}
              className={`w-full text-left px-2 py-1 rounded-[4px] text-[11px] leading-snug ${
                active ? theme.button_default : `${theme.label} hover:opacity-100 opacity-90`
              }`}
              title={n.headingText}
            >
              {n.title}
            </button>
            {n.children.length ? (
              <TocTree nodes={n.children} activeId={activeId} onSelect={onSelect} theme={theme} depth={depth + 1} />
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}

export function CommandHelpModal(props: {
  open: boolean;
  /** Prefer heading text match, e.g. "Execute SQL Statement" or full "## 5.1 …" title body */
  initialSection?: CommandHelpSectionId;
  onClose: () => void;
  onAskConfigurator?: (prompt: string) => void;
  askPrompt?: string;
}) {
  const { theme } = useTheme();
  const { open, initialSection, onClose, onAskConfigurator, askPrompt } = props;
  const [md, setMd] = useState('');
  const [loadError, setLoadError] = useState<string | null>(null);
  const [maximized, setMaximized] = useState(false);
  const [search, setSearch] = useState('');
  const [active, setActive] = useState<TocNode | null>(null);
  const [showFull, setShowFull] = useState(true);

  useEffect(() => {
    if (!open) return;
    setMaximized(false);
    setSearch('');
    setShowFull(true);
    setActive(null);
  }, [open, initialSection]);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    setLoadError(null);
    fetch(`${process.env.PUBLIC_URL || ''}/help/commands-user-help.md`, { cache: 'no-cache' })
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.text();
      })
      .then((t) => {
        if (!cancelled) setMd(t);
      })
      .catch((e) => {
        if (!cancelled) setLoadError(e?.message || 'Failed to load help');
      });
    return () => {
      cancelled = true;
    };
  }, [open]);

  const toc = useMemo(() => buildToc(md), [md]);
  const filteredToc = useMemo(() => filterToc(toc, search), [toc, search]);

  useEffect(() => {
    if (!md || !initialSection || !toc.length) return;
    const needle = String(initialSection).toLowerCase();
    const find = (nodes: TocNode[]): TocNode | null => {
      for (const n of nodes) {
        if (
          n.id === initialSection ||
          n.headingText.toLowerCase().includes(needle) ||
          n.title.toLowerCase().includes(needle)
        ) {
          return n;
        }
        const c = find(n.children);
        if (c) return c;
      }
      return null;
    };
    const hit = find(toc);
    if (hit) {
      setActive(hit);
      setShowFull(false);
    }
  }, [md, toc, initialSection]);

  const body = useMemo(() => {
    if (!md) return '';
    if (showFull || !active) return md;
    return extractByHeading(md, active.headingText, active.level);
  }, [md, showFull, active]);

  if (!open) return null;

  return (
    <div
      className={`fixed inset-0 flex items-center justify-center bg-black/40 ${maximized ? '' : 'px-3 py-3'}`}
      style={{ zIndex: 15000 }}
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
        className={`flex flex-col border shadow-xl overflow-hidden ${theme.mainContentSection} ${
          maximized ? 'h-full w-full rounded-none' : 'rounded-[4px]'
        }`}
        style={
          maximized
            ? { width: '100%', height: '100%', maxWidth: '100vw', maxHeight: '100vh' }
            : { width: 'min(1280px, 96vw)', height: 'min(900px, 94vh)', maxWidth: '96vw', maxHeight: '94vh' }
        }
        role="dialog"
        aria-modal="true"
        aria-label="Data Model Operation help"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-3 py-2 border-b shrink-0 gap-2">
          <div className={`text-sm font-semibold ${theme.title} truncate`}>Data Model Operation — Help</div>
          <div className="flex items-center gap-1 shrink-0">
            {SHOW_ASK_CONFIGURATOR_BUTTON && onAskConfigurator && askPrompt ? (
              <button
                type="button"
                onClick={() => {
                  onAskConfigurator(askPrompt);
                  onClose();
                }}
                className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                title="Copy prefab prompt for Configurator chat"
              >
                <i className="fa-solid fa-robot mr-1" aria-hidden />
                Ask Configurator
              </button>
            ) : null}
            <button
              type="button"
              onClick={() => setMaximized((m) => !m)}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title={maximized ? 'Exit full screen' : 'Full screen'}
              aria-label={maximized ? 'Exit full screen' : 'Full screen'}
            >
              <i className={`fa-solid ${maximized ? 'fa-compress' : 'fa-expand'}`} aria-hidden />
            </button>
            <button
              type="button"
              onClick={onClose}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          </div>
        </div>

        <div className="min-h-0 h-1 flex-auto flex overflow-hidden">
          <aside
            className={`w-[280px] shrink-0 border-r flex flex-col min-h-0 ${theme.mainContentSection}`}
          >
            <div className="p-2 border-b shrink-0 space-y-1.5">
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search chapters…"
                className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              />
              <button
                type="button"
                onClick={() => {
                  setShowFull(true);
                  setActive(null);
                }}
                className={`w-full text-left px-2 py-1 rounded-[4px] text-[11px] ${
                  showFull ? theme.button_default : theme.label
                }`}
              >
                Full manual
              </button>
            </div>
            <div className="min-h-0 h-1 flex-auto overflow-auto p-2">
              {filteredToc.length === 0 ? (
                <p className={`text-[11px] opacity-70 px-1 ${theme.label}`}>No matching chapters.</p>
              ) : (
                <TocTree
                  nodes={filteredToc}
                  activeId={!showFull && active ? active.id : null}
                  onSelect={(n) => {
                    setActive(n);
                    setShowFull(false);
                  }}
                  theme={theme}
                />
              )}
            </div>
          </aside>

          <div className="min-w-0 w-1 flex-auto min-h-0 overflow-auto px-8 py-6">
            {loadError ? (
              <p className={`text-sm ${theme.label}`}>Could not load help: {loadError}</p>
            ) : !md ? (
              <p className={`text-sm opacity-70 ${theme.label}`}>Loading…</p>
            ) : (
              <AgentMarkdown content={body} className="text-sm leading-relaxed max-w-none" />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
