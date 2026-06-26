import React, { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../redux/hooks/useTheme';

/**
 * Reusable gear-button add-on for any Wijmo FlexGrid.
 * Provides:
 *  - Freeze the first N columns (locked when scrolling horizontally)
 *  - Show / Hide columns (checkbox list)
 *
 * Pass the grid control instance via `grid` (e.g. from FlexGrid `initialized`)
 * or a React ref to the FlexGrid react wrapper via `gridRef`.
 * Optionally pass a `storageKey` to persist the user's choices in localStorage.
 */
export interface FlexGridAddOnProps {
    /** The Wijmo FlexGrid control instance (preferred). */
    grid?: any;
    /** Alternatively, a ref to the FlexGrid react wrapper (reads `.control`). */
    gridRef?: React.RefObject<any>;
    /** Default number of frozen columns applied once the grid is ready. */
    defaultFrozenColumns?: number;
    /** Persist freeze count + hidden columns under this key in localStorage. */
    storageKey?: string;
    /** Extra classes for the gear button. */
    buttonClassName?: string;
    /** Tooltip for the gear button. */
    title?: string;
}

interface PersistedState {
    frozen?: number;
    hidden?: string[];
}

type PanelView = 'menu' | 'freeze' | 'columns';

interface ColumnInfo {
    index: number;
    binding: string;
    header: string;
    visible: boolean;
}

const FlexGridAddOn: React.FC<FlexGridAddOnProps> = ({
    grid,
    gridRef,
    defaultFrozenColumns,
    storageKey,
    buttonClassName,
    title = 'Grid columns',
}) => {
    const { theme } = useTheme();
    const buttonRef = useRef<HTMLButtonElement>(null);
    const panelRef = useRef<HTMLDivElement>(null);
    const appliedDefaultRef = useRef(false);

    const [open, setOpen] = useState(false);
    const [view, setView] = useState<PanelView>('menu');
    const [pos, setPos] = useState<{ top: number; left: number }>({ top: 0, left: 0 });
    const [frozen, setFrozen] = useState(0);
    const [columns, setColumns] = useState<ColumnInfo[]>([]);

    const getGrid = useCallback((): any => {
        return grid || gridRef?.current?.control || null;
    }, [grid, gridRef]);

    const readPersisted = useCallback((): PersistedState | null => {
        if (!storageKey) return null;
        try {
            const raw = localStorage.getItem(`flexGridAddOn:${storageKey}`);
            return raw ? (JSON.parse(raw) as PersistedState) : null;
        } catch {
            return null;
        }
    }, [storageKey]);

    const writePersisted = useCallback((g: any) => {
        if (!storageKey || !g) return;
        try {
            const hidden: string[] = [];
            for (let i = 0; i < g.columns.length; i++) {
                const col = g.columns[i];
                if (col?.binding && col.visible === false) hidden.push(col.binding);
            }
            const state: PersistedState = { frozen: g.frozenColumns || 0, hidden };
            localStorage.setItem(`flexGridAddOn:${storageKey}`, JSON.stringify(state));
        } catch {
            /* ignore */
        }
    }, [storageKey]);

    // Apply default freeze + persisted state once the grid (and its columns) are ready.
    // The grid control may not exist yet on mount, and Wijmo clamps frozenColumns to
    // the current column count, so we wait until columns are registered before applying.
    useEffect(() => {
        if (appliedDefaultRef.current) return;
        let cancelled = false;

        const apply = (attempt: number) => {
            if (cancelled || appliedDefaultRef.current) return;
            const g = getGrid();
            if (!g || !g.columns || g.columns.length === 0) {
                if (attempt < 40) setTimeout(() => apply(attempt + 1), 50);
                return;
            }
            appliedDefaultRef.current = true;

            const persisted = readPersisted();
            if (typeof defaultFrozenColumns === 'number') {
                g.frozenColumns = defaultFrozenColumns;
            }
            if (persisted) {
                if (typeof persisted.frozen === 'number') g.frozenColumns = persisted.frozen;
                if (Array.isArray(persisted.hidden) && persisted.hidden.length) {
                    for (let i = 0; i < g.columns.length; i++) {
                        const col = g.columns[i];
                        if (col?.binding && persisted.hidden.includes(col.binding)) {
                            col.visible = false;
                        }
                    }
                }
            }
        };

        apply(0);
        return () => { cancelled = true; };
    }, [getGrid, defaultFrozenColumns, readPersisted]);

    const refreshFromGrid = useCallback(() => {
        const g = getGrid();
        if (!g) {
            setColumns([]);
            setFrozen(0);
            return;
        }
        setFrozen(g.frozenColumns || 0);
        const cols: ColumnInfo[] = [];
        for (let i = 0; i < g.columns.length; i++) {
            const col = g.columns[i];
            const header = (col?.header ?? '').toString();
            if (!header.trim()) continue; // skip spacer / unnamed columns
            cols.push({
                index: i,
                binding: col?.binding ?? '',
                header,
                visible: col?.visible !== false,
            });
        }
        setColumns(cols);
    }, [getGrid]);

    const updatePosition = useCallback(() => {
        const btn = buttonRef.current;
        if (!btn) return;
        const rect = btn.getBoundingClientRect();
        const panelWidth = 260;
        let left = rect.right - panelWidth;
        if (left < 8) left = 8;
        if (left + panelWidth > window.innerWidth - 8) {
            left = window.innerWidth - panelWidth - 8;
        }
        setPos({ top: rect.bottom + 4, left });
    }, []);

    const openPanel = useCallback(() => {
        refreshFromGrid();
        setView('menu');
        updatePosition();
        setOpen(true);
    }, [refreshFromGrid, updatePosition]);

    useLayoutEffect(() => {
        if (open) updatePosition();
    }, [open, view, updatePosition]);

    // Close on outside click / Escape / scroll-resize reposition.
    useEffect(() => {
        if (!open) return;
        const onDown = (e: MouseEvent) => {
            const target = e.target as Node;
            if (panelRef.current?.contains(target)) return;
            if (buttonRef.current?.contains(target)) return;
            setOpen(false);
        };
        const onKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape') setOpen(false);
        };
        const onReposition = () => updatePosition();
        document.addEventListener('mousedown', onDown);
        document.addEventListener('keydown', onKey);
        window.addEventListener('resize', onReposition);
        window.addEventListener('scroll', onReposition, true);
        return () => {
            document.removeEventListener('mousedown', onDown);
            document.removeEventListener('keydown', onKey);
            window.removeEventListener('resize', onReposition);
            window.removeEventListener('scroll', onReposition, true);
        };
    }, [open, updatePosition]);

    const applyFreeze = useCallback((n: number) => {
        const g = getGrid();
        if (!g) return;
        g.frozenColumns = n;
        setFrozen(n);
        writePersisted(g);
    }, [getGrid, writePersisted]);

    const toggleColumn = useCallback((index: number) => {
        const g = getGrid();
        if (!g) return;
        const col = g.columns[index];
        if (!col) return;
        col.visible = col.visible === false ? true : false;
        writePersisted(g);
        refreshFromGrid();
    }, [getGrid, writePersisted, refreshFromGrid]);

    const setAllColumns = useCallback((visible: boolean) => {
        const g = getGrid();
        if (!g) return;
        for (let i = 0; i < g.columns.length; i++) {
            const col = g.columns[i];
            const header = (col?.header ?? '').toString();
            if (!header.trim()) continue;
            col.visible = visible;
        }
        writePersisted(g);
        refreshFromGrid();
    }, [getGrid, writePersisted, refreshFromGrid]);

    const freezeOptions = (() => {
        const max = Math.min(columns.length, 5);
        const opts: number[] = [];
        for (let i = 0; i <= max; i++) opts.push(i);
        return opts;
    })();

    const renderMenu = () => (
        <div className="py-1">
            <button
                type="button"
                onClick={() => setView('freeze')}
                className={`w-full flex items-center justify-between px-3 py-2 text-xs ${theme.contextMenu} text-left`}
            >
                <span><i className="fa-solid fa-thumbtack mr-2" />Freeze Columns</span>
                <span className="opacity-60">{frozen > 0 ? `${frozen}` : ''} <i className="fa-solid fa-chevron-right ml-1" /></span>
            </button>
            <button
                type="button"
                onClick={() => setView('columns')}
                className={`w-full flex items-center justify-between px-3 py-2 text-xs ${theme.contextMenu} text-left`}
            >
                <span><i className="fa-solid fa-table-columns mr-2" />Show / Hide Columns</span>
                <i className="fa-solid fa-chevron-right ml-1 opacity-60" />
            </button>
        </div>
    );

    const renderHeader = (label: string) => (
        <div className={`flex items-center px-2 py-1.5 border-b ${theme.mainContentSection}`}>
            <button
                type="button"
                onClick={() => setView('menu')}
                className={`px-2 py-1 rounded-[4px] text-xs ${theme.button_default}`}
                title="Back"
            >
                <i className="fa-solid fa-chevron-left" />
            </button>
            <span className={`ml-2 text-xs font-semibold ${theme.title}`}>{label}</span>
        </div>
    );

    const renderFreeze = () => (
        <div>
            {renderHeader('Freeze Columns')}
            <div className="px-3 py-3">
                <div className={`text-xs mb-2 ${theme.label}`}>Lock the first columns while scrolling</div>
                <div className="flex items-center gap-1.5">
                    {freezeOptions.map((n) => {
                        const selected = frozen === n;
                        const label = n === 0 ? 'None' : `${n}`;
                        return (
                            <button
                                key={n}
                                type="button"
                                onClick={() => applyFreeze(n)}
                                title={n === 0 ? 'No frozen columns' : `Freeze first ${n} column${n > 1 ? 's' : ''}`}
                                className={`${n === 0 ? 'px-2.5' : 'w-7'} h-7 flex items-center justify-center text-xs rounded-[4px] border transition-all active:scale-95 ${selected ? theme.button_secondary : theme.button_default}`}
                            >
                                {label}
                            </button>
                        );
                    })}
                </div>
            </div>
        </div>
    );

    const renderColumns = () => (
        <div>
            {renderHeader('Show / Hide Columns')}
            <div className={`flex items-center justify-between px-3 py-1.5 border-b ${theme.mainContentSection}`}>
                <button
                    type="button"
                    onClick={() => setAllColumns(true)}
                    className={`px-2 py-1 rounded-[4px] text-xs ${theme.button_default}`}
                >
                    Show all
                </button>
                <button
                    type="button"
                    onClick={() => setAllColumns(false)}
                    className={`px-2 py-1 rounded-[4px] text-xs ${theme.button_default}`}
                >
                    Hide all
                </button>
            </div>
            <div className="py-1 max-h-72 overflow-auto">
                {columns.map((col) => (
                    <label
                        key={`${col.binding}-${col.index}`}
                        className={`flex items-center px-3 py-1.5 text-xs cursor-pointer ${theme.contextMenu}`}
                    >
                        <input
                            type="checkbox"
                            checked={col.visible}
                            onChange={() => toggleColumn(col.index)}
                            className="mr-2"
                        />
                        <span className="w-1 flex-auto truncate">{col.header}</span>
                    </label>
                ))}
                {columns.length === 0 && (
                    <div className={`px-3 py-2 text-xs ${theme.label}`}>No columns</div>
                )}
            </div>
        </div>
    );

    return (
        <>
            <button
                ref={buttonRef}
                type="button"
                onClick={() => (open ? setOpen(false) : openPanel())}
                className={buttonClassName || `px-2 py-1 ${theme.button_default} rounded-[4px] text-xs hover:shadow-sm active:scale-95`}
                title={title}
            >
                <i className="fa-solid fa-gear" />
            </button>
            {open && createPortal(
                <div
                    ref={panelRef}
                    style={{ position: 'fixed', top: pos.top, left: pos.left, width: 260, zIndex: 10050 }}
                    className={`${theme.mainContentSection} border rounded-[6px] shadow-xl overflow-hidden`}
                >
                    {view === 'menu' && renderMenu()}
                    {view === 'freeze' && renderFreeze()}
                    {view === 'columns' && renderColumns()}
                </div>,
                document.body
            )}
        </>
    );
};

export default FlexGridAddOn;
