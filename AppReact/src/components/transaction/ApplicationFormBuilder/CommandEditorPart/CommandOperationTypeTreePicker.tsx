import React, { useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../../../redux/hooks/useTheme';
import appHelper from '../../../../helper/appHelper';
import { getTransactionCommandActionTypeDisplay } from '../../transactionCommandActionTypes';
import {
  filterCommandTypeTaxonomyTreeBySearch,
  getCommandTypeTaxonomyTree,
  type CommandTypeCategory,
  type CommandTypeLeaf,
  type CommandTypeNode,
  type CommandTypeTaxonomyContext,
} from '../../transactionCommandTypeTaxonomy';

/** Above SharedAgentChat dock (z 11000–13000) and normal popup stack (~10000). */
function pickerOverlayZIndex(): number {
  return Math.max(14000, appHelper.getGlobalOverlayZIndex());
}

function resolveActionTypeId(actionType: number | string | null | undefined): number | null {
  if (actionType == null || actionType === '') return null;
  const n = Number(actionType);
  return Number.isFinite(n) ? n : null;
}

/** Single L2 flyout: L1 children, with groups expanded inline (L2 header + L3 leaves). */
type CascadeColumn = {
  key: string;
  nodes: CommandTypeNode[];
  /** Parent row rect — column opens to the right of this */
  anchorTop: number;
  anchorLeft: number;
};

function nodeKey(n: CommandTypeNode, index: number): string {
  if (n.kind === 'type') return `type:${n.actionTypeId}`;
  return `${n.kind}:${n.id}:${index}`;
}

function hasChildren(n: CommandTypeNode): boolean {
  if (n.kind === 'type') return false;
  return Array.isArray(n.children) && n.children.length > 0;
}

function childNodes(n: CommandTypeNode): CommandTypeNode[] {
  if (n.kind === 'type') return [];
  return (n.children as CommandTypeNode[]) || [];
}

function leafLabel(t: CommandTypeLeaf): string {
  return t.display || getTransactionCommandActionTypeDisplay(t.actionTypeId) || String(t.actionTypeId);
}

function nodeLabel(n: CommandTypeNode): string {
  if (n.kind === 'type') return leafLabel(n);
  return n.label;
}

/** L2 flyout may use up to 90% of the viewport height. */
function maxL2PanelHeight(): number {
  return Math.floor(window.innerHeight * 0.9);
}

/** Preferred L2 panel height from content, capped at 90% of screen. */
function estimateMergedPanelHeight(nodes: CommandTypeNode[]): number {
  let rows = 0;
  for (const n of nodes) {
    if (n.kind === 'group') rows += 1 + (n.children?.length ?? 0);
    else rows += 1;
  }
  return Math.min(maxL2PanelHeight(), Math.max(120, rows * 28 + 16));
}

export function CommandOperationTypeTreePicker(props: {
  actionType: number | null | undefined;
  taxonomyContext: CommandTypeTaxonomyContext;
  onChange: (actionTypeId: number) => void;
}) {
  const { theme } = useTheme();
  const { actionType, taxonomyContext, onChange } = props;
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const ignoreOutsideUntilRef = useRef(0);
  const hoverTimerRef = useRef<number | null>(null);

  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [rootStyle, setRootStyle] = useState<React.CSSProperties | null>(null);
  /** At most one L2 flyout (L2+L3 merged). Cleared when search is active. */
  const [cascade, setCascade] = useState<CascadeColumn[]>([]);
  const [hoveredPath, setHoveredPath] = useState<string[]>([]);

  const actionTypeId = resolveActionTypeId(actionType);
  const tree = useMemo(() => getCommandTypeTaxonomyTree(taxonomyContext), [taxonomyContext]);
  const isSearching = query.trim().length > 0;
  /** Search mode: filtered 3-layer tree, all branches expanded inline. */
  const searchTree = useMemo(
    () => (isSearching ? filterCommandTypeTaxonomyTreeBySearch(tree, query) : []),
    [tree, query, isSearching]
  );

  const knownDisplay = actionTypeId != null ? getTransactionCommandActionTypeDisplay(actionTypeId) : '';
  const display =
    knownDisplay && knownDisplay !== String(actionTypeId)
      ? knownDisplay
      : actionTypeId != null
        ? `Unknown type (${actionTypeId})`
        : 'Select operation type…';

  const clearHoverTimer = () => {
    if (hoverTimerRef.current != null) {
      window.clearTimeout(hoverTimerRef.current);
      hoverTimerRef.current = null;
    }
  };

  const close = () => {
    clearHoverTimer();
    setOpen(false);
    setRootStyle(null);
    setQuery('');
    setCascade([]);
    setHoveredPath([]);
  };

  const openPicker = () => {
    const el = triggerRef.current;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const width = Math.max(rect.width, 280);
    const maxH = 360;
    const gap = 4;
    const spaceBelow = window.innerHeight - rect.bottom - 8;
    const spaceAbove = rect.top - 8;
    const openUp = spaceBelow < 180 && spaceAbove > spaceBelow;
    // Fixed height (not only maxHeight) so the L1 list is not collapsed by flex/h-1.
    const height = Math.min(maxH, Math.max(openUp ? spaceAbove : spaceBelow, 260));
    const top = openUp ? Math.max(8, rect.top - height - gap) : rect.bottom + gap;
    const left = Math.min(Math.max(8, rect.left), Math.max(8, window.innerWidth - width - 8));
    ignoreOutsideUntilRef.current = Date.now() + 300;
    setRootStyle({
      position: 'fixed',
      top,
      left,
      width,
      height,
      zIndex: pickerOverlayZIndex(),
      backgroundColor: '#fff',
      boxShadow: '0 8px 24px rgba(0,0,0,0.18)',
    });
    setCascade([]);
    setHoveredPath([]);
    setQuery('');
    setOpen(true);
    window.setTimeout(() => searchRef.current?.focus(), 0);
  };

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (Date.now() < ignoreOutsideUntilRef.current) return;
      const t = e.target as Node;
      if (rootRef.current?.contains(t)) return;
      if (panelRef.current?.contains(t)) return;
      close();
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close();
    };
    document.addEventListener('mousedown', onDoc);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDoc);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  useEffect(() => () => clearHoverTimer(), []);

  const selectLeaf = (id: number) => {
    onChange(Number(id));
    close();
  };

  /**
   * Hover L1 branch → one flyout only.
   * L2 groups + L3 leaves render in that same panel (no third cascade column).
   */
  const onHoverL1Branch = (pathKeys: string[], children: CommandTypeNode[], rowEl: HTMLElement) => {
    clearHoverTimer();
    hoverTimerRef.current = window.setTimeout(() => {
      const rect = rowEl.getBoundingClientRect();
      const colWidth = 300;
      let left = rect.right + 2;
      if (left + colWidth > window.innerWidth - 8) {
        left = Math.max(8, rect.left - colWidth - 2);
      }
      const approxH = estimateMergedPanelHeight(children);
      let top = rect.top;
      // Keep panel fully on-screen; prefer aligning to L1 row, else shift up.
      const maxTop = Math.max(8, window.innerHeight - approxH - 8);
      if (top > maxTop) top = maxTop;
      if (top < 8) top = 8;
      setHoveredPath(pathKeys);
      setCascade([
        {
          key: pathKeys.join('/'),
          nodes: children,
          anchorTop: top,
          anchorLeft: left,
        },
      ]);
    }, 60);
  };

  const renderLeafButton = (t: CommandTypeLeaf, indentClass = '') => {
    const selected = actionTypeId === t.actionTypeId;
    return (
      <button
        key={t.actionTypeId}
        type="button"
        onClick={() => selectLeaf(t.actionTypeId)}
        className={`w-full text-left px-2 py-1.5 text-xs flex items-center gap-1 rounded-[2px] ${indentClass} ${
          selected ? theme.button_default : `hover:bg-gray-50 ${theme.label}`
        }`}
      >
        <span className="truncate flex-auto w-1">{leafLabel(t)}</span>
      </button>
    );
  };

  /**
   * Second menu panel: L2 group headers + L3 leaves in one list (structure stays 3-layer).
   * Direct L1→leaf children also render here as plain selectable rows.
   */
  const renderMergedL2L3Body = (nodes: CommandTypeNode[]) =>
    nodes.map((n, i) => {
      if (n.kind === 'type') {
        return renderLeafButton(n);
      }
      if (n.kind === 'group') {
        return (
          <div key={nodeKey(n, i)} className="mt-0.5 mb-1">
            <div className={`px-2 py-1 text-xs font-semibold ${theme.title}`}>{n.label}</div>
            <div className="flex flex-col">{n.children.map((t) => renderLeafButton(t, 'pl-4'))}</div>
          </div>
        );
      }
      // Nested category under L1 (rare): treat like a group header + recurse once
      if (n.kind === 'category') {
        return (
          <div key={nodeKey(n, i)} className="mt-0.5 mb-1">
            <div className={`px-2 py-1 text-xs font-semibold ${theme.title}`}>{n.label}</div>
            <div className="flex flex-col">{renderMergedL2L3Body(childNodes(n))}</div>
          </div>
        );
      }
      return null;
    });

  /** L1 rows only (root panel). */
  const renderL1Row = (n: CommandTypeNode, index: number) => {
    const key = nodeKey(n, index);
    const pathKeys = [key];
    const pathStr = pathKeys.join('/');
    const isHover = hoveredPath.join('/').startsWith(pathStr) || hoveredPath.join('/') === pathStr;
    const branch = hasChildren(n);
    // Composition (and similar): L1 auto-select type — same bold as other L1 rows, no flyout chevron
    const isL1AutoType =
      n.kind === 'category' && n.autoActionTypeId != null && (!n.children || n.children.length <= 1);

    if (isL1AutoType) {
      const typeId = (n as CommandTypeCategory).autoActionTypeId!;
      const selected = actionTypeId === typeId;
      return (
        <button
          key={key}
          type="button"
          onMouseEnter={() => {
            clearHoverTimer();
            setCascade([]);
            setHoveredPath(pathKeys);
          }}
          onClick={() => selectLeaf(typeId)}
          className={`w-full text-left px-2 py-1.5 text-xs flex items-center gap-1 rounded-[2px] ${
            selected ? theme.button_default : isHover ? 'bg-gray-100' : `hover:bg-gray-50`
          }`}
        >
          <span className={`truncate flex-auto w-1 font-semibold ${theme.title}`}>{nodeLabel(n)}</span>
        </button>
      );
    }

    if (n.kind === 'type') {
      const selected = actionTypeId === n.actionTypeId;
      return (
        <button
          key={key}
          type="button"
          onMouseEnter={() => {
            clearHoverTimer();
            setCascade([]);
            setHoveredPath(pathKeys);
          }}
          onClick={() => selectLeaf(n.actionTypeId)}
          className={`w-full text-left px-2 py-1.5 text-xs flex items-center gap-1 rounded-[2px] ${
            selected ? theme.button_default : isHover ? 'bg-gray-100' : `hover:bg-gray-50 ${theme.label}`
          }`}
        >
          <span className="truncate flex-auto w-1">{nodeLabel(n)}</span>
        </button>
      );
    }

    return (
      <button
        key={key}
        type="button"
        onMouseEnter={(e) => {
          onHoverL1Branch(pathKeys, childNodes(n), e.currentTarget);
        }}
        onClick={(e) => {
          onHoverL1Branch(pathKeys, childNodes(n), e.currentTarget);
        }}
        className={`w-full text-left px-2 py-1.5 text-xs flex items-center gap-1 rounded-[2px] ${
          isHover ? 'bg-gray-100' : `hover:bg-gray-50 ${theme.label}`
        }`}
      >
        <span className={`truncate flex-auto w-1 font-semibold ${theme.title}`}>{nodeLabel(n)}</span>
        {branch ? <i className="fa-solid fa-chevron-right text-[10px] opacity-60 shrink-0" /> : null}
      </button>
    );
  };

  const renderL2MergedColumn = (col: CascadeColumn, style: React.CSSProperties) => (
    <div
      key={col.key}
      style={style}
      className="flex flex-col border border-gray-300 rounded-[4px] overflow-hidden bg-white"
    >
      <div className="overflow-y-auto p-1 h-1 flex-auto">
        {renderMergedL2L3Body(col.nodes)}
      </div>
    </div>
  );

  /** Search: fully expanded 3-layer tree (L1 → L2 → leaves), filtered. */
  const renderExpandedSearchTree = (cats: CommandTypeCategory[]) =>
    cats.map((cat) => {
      const isComposition =
        cat.autoActionTypeId != null && (!cat.children || cat.children.length <= 1);
      if (isComposition) {
        const typeId = cat.autoActionTypeId!;
        const selected = actionTypeId === typeId;
        return (
          <button
            key={cat.id}
            type="button"
            onClick={() => selectLeaf(typeId)}
            className={`w-full text-left px-2 py-1.5 text-xs rounded-[2px] ${
              selected ? theme.button_default : `hover:bg-gray-50 ${theme.label}`
            }`}
          >
            {cat.label}
          </button>
        );
      }
      return (
        <div key={cat.id} className="mb-1">
          <div className={`px-1 py-1 text-xs font-semibold ${theme.title}`}>
            <i className="fa-solid fa-chevron-down text-[10px] opacity-70 mr-1" />
            {cat.label}
          </div>
          {/* L2 leaves indented past L1 label for clear hierarchy while searching */}
          <div className="ml-5 pl-1">
            {cat.children.map((child) => {
              if (child.kind === 'type') {
                const selected = actionTypeId === child.actionTypeId;
                return (
                  <button
                    key={child.actionTypeId}
                    type="button"
                    onClick={() => selectLeaf(child.actionTypeId)}
                    className={`w-full text-left px-2 py-1.5 text-xs rounded-[2px] ${
                      selected ? theme.button_default : `hover:bg-gray-50 ${theme.label}`
                    }`}
                  >
                    {leafLabel(child)}
                  </button>
                );
              }
              if (child.kind === 'group') {
                return (
                  <div key={child.id} className="mt-0.5">
                    <div className={`px-1 py-1 text-xs font-semibold ${theme.label}`}>
                      <i className="fa-solid fa-chevron-down text-[10px] opacity-70 mr-1" />
                      {child.label}
                    </div>
                    <div className="ml-4 flex flex-col gap-0.5">
                      {child.children.map((t) => {
                        const selected = actionTypeId === t.actionTypeId;
                        return (
                          <button
                            key={t.actionTypeId}
                            type="button"
                            onClick={() => selectLeaf(t.actionTypeId)}
                            className={`w-full text-left px-2 py-1.5 text-xs rounded-[2px] ${
                              selected ? theme.button_default : `hover:bg-gray-50 ${theme.label}`
                            }`}
                          >
                            {leafLabel(t)}
                          </button>
                        );
                      })}
                    </div>
                  </div>
                );
              }
              return null;
            })}
          </div>
        </div>
      );
    });

  const z = pickerOverlayZIndex();

  const panel =
    open && rootStyle && typeof document !== 'undefined'
      ? createPortal(
          <>
            <div
              style={{ position: 'fixed', inset: 0, zIndex: z - 1 }}
              onMouseDown={(e) => {
                if (Date.now() < ignoreOutsideUntilRef.current) return;
                e.preventDefault();
                close();
              }}
            />
            <div ref={panelRef} style={{ zIndex: z }}>
              <div style={rootStyle} className="flex flex-col border border-gray-300 rounded-[4px] overflow-hidden">
                <div className="px-2 py-1.5 border-b border-gray-200 shrink-0 bg-white">
                  <input
                    ref={searchRef}
                    type="text"
                    value={query}
                    onChange={(e) => {
                      setQuery(e.target.value);
                      setCascade([]);
                      setHoveredPath([]);
                    }}
                    placeholder="Search operation type…"
                    className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none bg-white`}
                    autoComplete="off"
                  />
                </div>
                <div
                  className="overflow-y-auto p-1 bg-white"
                  style={{ height: Math.max(120, Number(rootStyle.height) - 44) }}
                >
                  {isSearching ? (
                    searchTree.length === 0 ? (
                      <div className={`px-2 py-2 text-xs opacity-70 ${theme.label}`}>No matches</div>
                    ) : (
                      renderExpandedSearchTree(searchTree as CommandTypeCategory[])
                    )
                  ) : tree.length === 0 ? (
                    <div className={`px-2 py-2 text-xs opacity-70 ${theme.label}`}>No categories</div>
                  ) : (
                    tree.map((n, i) => renderL1Row(n, i))
                  )}
                </div>
              </div>

              {/* One flyout: L2 headers + L3 leaves merged (no third cascade column) */}
              {!isSearching &&
                cascade.map((col) => {
                  const panelMaxH = maxL2PanelHeight();
                  const panelH = Math.min(panelMaxH, estimateMergedPanelHeight(col.nodes));
                  return renderL2MergedColumn(col, {
                    position: 'fixed',
                    top: col.anchorTop,
                    left: col.anchorLeft,
                    width: 300,
                    height: panelH,
                    maxHeight: panelMaxH,
                    zIndex: z + 1,
                    backgroundColor: '#fff',
                    boxShadow: '0 8px 24px rgba(0,0,0,0.18)',
                  });
                })}
            </div>
          </>,
          document.body
        )
      : null;

  return (
    <div ref={rootRef} className="relative w-72">
      <button
        ref={triggerRef}
        type="button"
        onClick={() => {
          if (open) close();
          else openPicker();
        }}
        className={`w-full h-7 px-2 text-xs border text-left flex items-center justify-between gap-2 ${theme.inputBox}`}
        aria-expanded={open}
        aria-haspopup="listbox"
        title={actionTypeId != null ? `ActionType=${actionTypeId}` : undefined}
      >
        <span className="truncate">{display}</span>
        <i className={`fa-solid fa-chevron-${open ? 'up' : 'down'} text-[10px] opacity-70 shrink-0`} />
      </button>

      {panel}
    </div>
  );
}
